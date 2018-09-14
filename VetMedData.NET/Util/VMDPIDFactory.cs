using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using VetMedData.NET.Model;

namespace VetMedData.NET.Util
{
    /// <summary>
    /// Options for storage and processing of <see cref="VMDPID"/> by <see cref="VMDPIDFactory"/>
    /// </summary>
    [Flags]
    public enum PidFactoryOptions : byte
    {
        None = 0,
        GetTargetSpeciesForExpiredVmdProduct = 1,
        GetTargetSpeciesForExpiredEmaProduct = 2,
        PersistentPid = 4,
        OverrideCachedCopy = 8,
        GetPharmaceuticalComposition = 16
    }

    /// <summary>
    /// Factory class for <see cref="VMDPID"/>, handles GETting and parsing of XML.
    /// Has singleton-like behaviour for VMDPID class as to reduce
    /// number of HTTP GETs to VMD servers.
    /// </summary>

    // ReSharper disable once InconsistentNaming
    public static class VMDPIDFactory
    {
        private static readonly IFormatter Formatter = new BinaryFormatter();
        private static readonly string TempFile = Path.GetTempPath() + Path.DirectorySeparatorChar + "VMDPID.bin";

        private const string VmdUrl = @"http://www.vmd.defra.gov.uk/ProductInformationDatabase/downloads/VMD_ProductInformationDatabase.xml";
        private const string DateTimeFormat = @"dd/MM/yyyy HH:mm:ss";

        private static readonly HttpClient Client = new HttpClient();
        private static readonly XmlSerializer Xser = new XmlSerializer(typeof(VMDPID_Raw), new XmlRootAttribute("VMD_PIDProducts"));
        private static VMDPID _vmdpid;
        private static PidFactoryOptions _cachedCopyOptions;

        /// <summary>
        /// Gets Product Information Database (PID) from Veterinary Medicines Directorate (VMD)
        /// </summary>
        /// <param name="options"><see cref="PidFactoryOptions"/>Options for processing and storage of PID</param>
        /// <returns></returns>
        public static async Task<VMDPID> GetVmdPid(PidFactoryOptions options = PidFactoryOptions.None)
        {
            var pidUpdated = false;

            //do we have an in-memory copy?
            if (_vmdpid == null || (options & PidFactoryOptions.OverrideCachedCopy) != 0)
            {
                //is there one available on disk?
                var pidDeserialiseSuccess = false;
                if ((options & PidFactoryOptions.PersistentPid) != 0)
                {
                    pidDeserialiseSuccess = GetPidFromDisk();
                    // ReSharper disable once PossibleNullReferenceException
                    if (pidDeserialiseSuccess && await GetPidUpdateDate() > _vmdpid.CreatedDateTime)
                    {
                        _vmdpid = await GetPidFromVmd();
                        pidUpdated = true;
                        _cachedCopyOptions = options;
                    }
                }

                if (!pidDeserialiseSuccess)
                {
                    _vmdpid = await GetPidFromVmd();
                    pidUpdated = true;
                    _cachedCopyOptions = options;
                }
            }

            //process target species options
            if ((options & PidFactoryOptions.GetTargetSpeciesForExpiredVmdProduct) != 0 &&
                ((_cachedCopyOptions & PidFactoryOptions.GetTargetSpeciesForExpiredVmdProduct) == 0
                 || pidUpdated)
                 )
            {
                _vmdpid = await PopulateExpiredProductTargetSpecies(_vmdpid);
            }

            if ((options & PidFactoryOptions.GetTargetSpeciesForExpiredEmaProduct) != 0 &&
                ((_cachedCopyOptions & PidFactoryOptions.GetTargetSpeciesForExpiredEmaProduct) == 0)
                || pidUpdated)
            {
                _vmdpid = await PopulateExpiredProductTargetSpeciesFromEma(_vmdpid);
            }

            //process active ingredient options
            if ((options & PidFactoryOptions.GetPharmaceuticalComposition) != 0 &&
                ((_cachedCopyOptions & PidFactoryOptions.GetPharmaceuticalComposition) == 0)
                || pidUpdated)
            {
                _vmdpid = await PopulateProductPharmaceuticalComposition(_vmdpid);
            }


            //persist new copy 
            if ((options & PidFactoryOptions.PersistentPid) != 0 && pidUpdated)
            {
                Serialize(_vmdpid);
            }

            return _vmdpid;
        }

        /// <summary>
        /// Helper method designed for testing to download SPC document
        /// </summary>
        /// <param name="p"><see cref="ReferenceProduct"/> to get SPC link for</param>
        /// <returns>Path to downloaded file in temp folder</returns>
        public static async Task<string> GetSpc(Product p)
        {
            var uri = ((ExpiredProduct)p).SPC_Link;

            var tf = Path.GetTempPath() +
                     Path.DirectorySeparatorChar +
                     uri.Split('/')[uri.Split('/').Length - 1];
            if (!File.Exists(tf))
            {
                using (var fs = File.OpenWrite(tf))
                {
                    (await GetHttpStream(uri)).CopyTo(fs);
                }
            }

            return tf;
        }

        /// <summary>
        /// Downloads and parses XML PID from VMD
        /// </summary>
        /// <returns><see cref="VMDPID"/>output of cleansing and parsing</returns>
        private static async Task<VMDPID> GetPidFromVmd()
        {
            var dt = await GetPidUpdateDate();
            var xe = XDocument.Load(await GetHttpStream(VmdUrl));

            var raw = (VMDPID_Raw)Xser.Deserialize(xe.CreateReader());
            return CleanAndParse(raw, dt == default(DateTime) ? (DateTime?)null : dt);
        }

        /// <summary>
        /// Writes <see cref="VMDPID"/> to temp file defined in _tf in binary format
        /// </summary>
        /// <param name="pid"></param>
        private static void Serialize(VMDPID pid)
        {
            using (var fs = File.Open(TempFile, FileMode.Create, FileAccess.Write))
            {
                Formatter.Serialize(fs, pid);
            }
        }

        /// <summary>
        /// Tries to deserialise VMDPID from temp file into _vmdpid
        /// </summary>
        /// <returns>Success</returns>
        private static bool GetPidFromDisk()
        {
            if (!File.Exists(TempFile) || new FileInfo(TempFile).Length == 0)
            {
                return false;
            }

            using (var fs = File.OpenRead(TempFile))
            {
                try
                {
                    _vmdpid = (VMDPID)Formatter.Deserialize(fs);
                }
                catch (Exception)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Converts autogen-from-xsd (VMD_PIDProducts.xsd)
        /// class hierarchy into clean human-created one
        /// </summary>
        /// <param name="raw">As-parsed VMDPID_Raw</param>
        /// <param name="createdDateTime">Created DateTime from xml comment</param>
        /// <returns>Instance of VMDPID containing cleaned data from raw</returns>
        private static VMDPID CleanAndParse(VMDPID_Raw raw, DateTime? createdDateTime)
        {
            var output = new VMDPID
            {
                //TODO: refactor repetitive product processing logic
                CurrentlyAuthorisedProducts = raw.CurrentAuthorisedProducts.Select(rcp =>
                    new CurrentlyAuthorisedProduct
                    {
                        ActiveSubstances = rcp.ActiveSubstances.Split(',').Select(a => a.Trim().ToLowerInvariant()).ToArray(),
                        AuthorisationRoute = rcp.AuthorisationRoute.Trim().ToLowerInvariant(),
                        ControlledDrug = rcp.ControlledDrug,
                        DateOfIssue = rcp.DateOfIssue,
                        DistributionCategory = rcp.DistributionCategory.Trim().ToLowerInvariant(),
                        //remove stray html tags in Distributor field
                        Distributors = rcp.Distributors.Replace("&lt;span&gt", "").Split(';').Select(a => a.Trim().ToLowerInvariant()).ToArray(),
                        MAHolder = rcp.MAHolder.Trim().ToLowerInvariant(),
                        Name = rcp.Name.Trim().ToLowerInvariant(),
                        PAAR_Link = rcp.PAAR_Link.Trim().ToLowerInvariant(),
                        PharmaceuticalForm = rcp.PharmaceuticalForm.Trim().ToLowerInvariant(),
                        SPC_Link = rcp.SPC_Link.Trim().ToLowerInvariant(),
                        TargetSpecies = rcp.TargetSpecies.Split(',').Select(a => a.Trim().ToLowerInvariant()).ToArray(),
                        TherapeuticGroup = rcp.TherapeuticGroup.Trim().ToLowerInvariant(),
                        UKPAR_Link = rcp.UKPAR_Link.Trim().ToLowerInvariant(),
                        VMNo = rcp.VMNo.Trim().ToLowerInvariant()

                    }).ToList(),
                ExpiredProducts = raw.ExpiredProducts.Select(rep =>
                    new ExpiredProduct
                    {
                        ActiveSubstances = rep.ActiveSubstances.Split(',').Select(a => a.Trim().ToLowerInvariant()).ToArray(),
                        AuthorisationRoute = rep.AuthorisationRoute.Trim().ToLowerInvariant(),
                        MAHolder = rep.MAHolder.Trim().ToLowerInvariant(),
                        Name = rep.Name.Trim().ToLowerInvariant(),
                        SPC_Link = rep.SPC_Link.Trim().ToLowerInvariant(),
                        VMNo = rep.VMNo.Trim().ToLowerInvariant(),
                        DateofExpiration = rep.DateOfExpiration
                    }).ToList(),
                SuspendedProducts = raw.SuspendedProducts.Select(rsp =>
                    new SuspendedProduct()
                    {
                        ActiveSubstances = rsp.ActiveSubstances.Split(',').Select(a => a.Trim().ToLowerInvariant()).ToArray(),
                        AuthorisationRoute = rsp.AuthorisationRoute.Trim().ToLowerInvariant(),
                        ControlledDrug = rsp.ControlledDrug,
                        DateOfIssue = rsp.DateOfIssue,
                        DistributionCategory = rsp.DistributionCategory.Trim().ToLowerInvariant(),
                        MAHolder = rsp.MAHolder.Trim().ToLowerInvariant(),
                        Name = rsp.Name.Trim().ToLowerInvariant(),
                        PAAR_Link = rsp.PAAR_Link.Trim().ToLowerInvariant(),
                        PharmaceuticalForm = rsp.PharmaceuticalForm.Trim().ToLowerInvariant(),
                        SPC_Link = rsp.SPC_Link.Trim().ToLowerInvariant(),
                        TargetSpecies = rsp.TargetSpecies.Split(',').Select(a => a.Trim().ToLowerInvariant()).ToArray(),
                        TherapeuticGroup = rsp.TherapeuticGroup.Trim().ToLowerInvariant(),
                        UKPAR_Link = rsp.UKPAR_Link.Trim().ToLowerInvariant(),
                        VMNo = rsp.VMNo.Trim().ToLowerInvariant(),
                        DateOfSuspension = rsp.DateOfSuspension

                    }).ToList(),
                HomoeopathicProducts = raw.HomeopathicProducts.Select(rhp =>
                    new HomoeopathicProduct
                    {
                        ActiveSubstances = rhp.ActiveSubstances.Split(',').Select(a => a.Trim().ToLowerInvariant()).ToArray(),
                        AuthorisationRoute = rhp.AuthorisationRoute.Trim().ToLowerInvariant(),
                        ControlledDrug = rhp.ControlledDrug,
                        DateOfIssue = rhp.DateOfIssue,
                        DistributionCategory = rhp.DistributionCategory.Trim().ToLowerInvariant(),
                        MAHolder = rhp.MAHolder.Trim().ToLowerInvariant(),
                        Name = rhp.Name.Trim().ToLowerInvariant(),
                        PharmaceuticalForm = rhp.PharmaceuticalForm.Trim().ToLowerInvariant(),
                        TargetSpecies = rhp.TargetSpecies.Split(',').Select(a => a.Trim().ToLowerInvariant()).ToArray(),
                        TherapeuticGroup = rhp.TherapeuticGroup.Trim().ToLowerInvariant(),
                        VMNo = rhp.VMNo.Trim().ToLowerInvariant()
                    }).ToList(),
                CreatedDateTime = createdDateTime ?? default(DateTime)

            };

            foreach (var product in output.AllProducts)
            {
                PopulateStaticTypedTargetSpecies(product);
            }

            return output;
        }

        private static void PopulateStaticTypedTargetSpecies(ReferenceProduct p)
        {
            if (p.TargetSpecies == null || !p.TargetSpecies.Any()) { return; }
            p.TargetSpeciesTyped = p.TargetSpecies.SelectMany(TargetSpecies.Find).Distinct().ToArray();
        }

        /// <summary>
        /// HTTP GETs XML PID from VMD as stream
        /// </summary>
        /// <returns></returns>
        private static async Task<Stream> GetHttpStream(string url)
        {
            var ms = new MemoryStream();
            using (var resp = await Client.GetAsync(url))
            using (var instream = await resp.Content.ReadAsStreamAsync())
            {
                await instream.CopyToAsync(ms);
            }

            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }

        /// <summary>
        /// Downloads SPC documents from VMD website for <see cref="ExpiredProduct"/>s, then uses
        /// <see cref="SPCParser"/> to extract target species 
        /// </summary>
        /// <param name="inpid"></param>
        /// <returns>Provided <see cref="VMDPID"/> with populated target species for expired products</returns>
        private static async Task<VMDPID> PopulateExpiredProductTargetSpecies(VMDPID inpid)
        {
            var expiredProducts =
                new ConcurrentBag<ExpiredProduct>(inpid.ExpiredProducts.Where(
                ep => ep.SPC_Link.ToLower().EndsWith(".doc") ||
                      ep.SPC_Link.ToLower().EndsWith(".docx")));

            var tasks = expiredProducts.Select(async ep => await PopulateTargetSpecies(ep));
            await Task.WhenAll(tasks);
            inpid.ExpiredProducts = inpid.ExpiredProducts.Where(ep => !(ep.SPC_Link.ToLower().EndsWith(".doc") ||
                                                                      ep.SPC_Link.ToLower().EndsWith(".docx")))
                    .Union(expiredProducts).ToList();
            return inpid;
        }

        private static async Task PopulateTargetSpecies(ExpiredProduct expiredProduct)
        {
            var tdoc = "";
            // ReSharper disable once RedundantAssignment
            var tdocx = "";
            // ReSharper disable once RedundantAssignment
            var tf = "";
            var doc = expiredProduct.SPC_Link.EndsWith(".doc", StringComparison.InvariantCultureIgnoreCase);
            if (doc)
            {
                tf = Path.GetTempFileName();
                tdoc = $"{tf}.doc";
                File.Move(tf, tdoc);

                using (var fs = File.OpenWrite(tdoc))
                {
                    (await GetHttpStream(expiredProduct.SPC_Link)).CopyTo(fs);
                    fs.Flush();
                }

                tdocx = WordConverter.ConvertDocToDocx(tdoc);
            }
            else
            {
                tf = Path.GetTempFileName();
                tdocx = $"{tf}.docx";
                File.Move(tf, tdocx);
                using (var fs = File.OpenWrite(tdocx))
                {
                    (await GetHttpStream(expiredProduct.SPC_Link)).CopyTo(fs);
                }
            }

            using (var spcStream = File.Open(tdocx, FileMode.Open))
            {
                var ts = SPCParser.GetTargetSpecies(spcStream);
                expiredProduct.TargetSpecies = ts;
            }

            if (!string.IsNullOrEmpty(tdoc) && File.Exists(tdoc))
            {
                File.Delete(tdoc);
            }

            if (!string.IsNullOrEmpty(tdocx) && File.Exists(tdocx))
            {
                File.Delete(tdocx);
            }

            PopulateStaticTypedTargetSpecies(expiredProduct);
        }

        /// <summary>
        /// Uses <see cref="EPARTools"/> to ascertain which <see cref="ExpiredProduct"/>s in a <see cref="VMDPID"/> are
        /// EMA-authorised, search for an SPC document on the EMA website (as no direct links to documents
        /// are provided in the PID) and then uses <see cref="SPCParser"/> to extract a dictionary of
        /// products and target species. The product in question is matched to this dictionary by name.
        /// </summary>
        /// <param name="inpid"></param>
        /// <returns>The provided <see cref="VMDPID"/> with EMA-authorised expired products' target species populated</returns>
        private static async Task<VMDPID> PopulateExpiredProductTargetSpeciesFromEma(VMDPID inpid)
        {
            var expiredProducts =
                new ConcurrentBag<ExpiredProduct>(inpid.ExpiredProducts.Where(
                    ep => EPARTools.IsEPAR(ep.SPC_Link)));

            var tasks = expiredProducts.Select(async ep => await PopulateTargetSpeciesFromEma(ep));
            await Task.WhenAll(tasks);
            inpid.ExpiredProducts = inpid.ExpiredProducts.Where(ep => !EPARTools.IsEPAR(ep.SPC_Link))
                .Union(expiredProducts).ToList();

            return inpid;
        }

        private static async Task PopulateTargetSpeciesFromEma(ReferenceProduct expiredProduct)
        {
            var possibleTargetSpecies = new Dictionary<string, string[]>();
            var searchResults = await EPARTools.GetSearchResults(expiredProduct.Name);
            foreach (var result in searchResults)
            {
                var tf = Path.GetTempFileName();
                using (var tfs = File.OpenWrite(tf))
                {
                    (await GetHttpStream(result)).CopyTo(tfs);
                }

                var targetSpecies = SPCParser.GetTargetSpeciesFromMultiProductPdf(tf);
                foreach (var ts in targetSpecies)
                {
                    possibleTargetSpecies[ts.Key.ToLowerInvariant()] = ts.Value;
                }
                File.Delete(tf);
            }

            var productKey = expiredProduct.Name.ToLowerInvariant();

            //exact name match
            if (possibleTargetSpecies.ContainsKey(productKey))
            {
                expiredProduct.TargetSpecies = possibleTargetSpecies[productKey];
            }

            //todo:smarter nonexact matching

            //name starts with
            else if (possibleTargetSpecies.Keys.Any(k => k.StartsWith(productKey)))
            {
                productKey = possibleTargetSpecies.Keys.Single(k => k.StartsWith(productKey));
                expiredProduct.TargetSpecies = possibleTargetSpecies[productKey];
            }

            //resolve inconsistent spacing in name between VMD and EMA
            else if (possibleTargetSpecies.Keys.Any(k => k.Replace(" ", "").Equals(productKey.Replace(" ", ""))))
            {
                productKey =
                    possibleTargetSpecies.Keys.Single(k => k.Replace(" ", "").Equals(productKey.Replace(" ", "")));
                expiredProduct.TargetSpecies = possibleTargetSpecies[productKey];
            }

            //get the bit after "for" in the name. Risky - e.g. "solution for injection"
            //could maybe do with species lookup for validation
            else if (productKey.Contains(" for "))
            {
                var forSplit = productKey.Split(new[] { "for" }, StringSplitOptions.None);
                var postFor = forSplit[forSplit.Length - 1].Replace("and", ",").Split(',')
                    .Select(t => t.Trim().ToLowerInvariant())
                    .Where(t => !string.IsNullOrWhiteSpace(t));

                expiredProduct.TargetSpecies = postFor.ToArray();
            }
            else
            {
                Debug.WriteLine($"{expiredProduct.Name} ReferenceProduct not found");
            }
            PopulateStaticTypedTargetSpecies(expiredProduct);
        }

        /// <summary>
        /// Downloads VMD PID and extracts creation date time from xml comment
        /// </summary>
        /// <returns>Creation <see cref="DateTime"/></returns>
        private static async Task<DateTime> GetPidUpdateDate()
        {
            //load incoming stream from HTTP as LINQ to XML element
            var xe = XDocument.Load(await GetHttpStream(VmdUrl));
            var comments = xe.DescendantNodes().OfType<XComment>();

            //get first comment which ends in a valid datetime as per DateTimeFormat
            var dt = default(DateTime);
            foreach (var comment in comments)
            {
                if (DateTime.TryParseExact(comment.Value.Substring(comment.Value.Length - DateTimeFormat.Length)
                    , DateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                {
                    break;
                }
            }

            return dt;
        }

        private static async Task<VMDPID> PopulateProductPharmaceuticalComposition(VMDPID inpid)
        {
            var products = new ConcurrentBag<RealProduct>(inpid.RealProducts.Where(p=>!EPARTools.IsEPAR(p.SPC_Link)));
            var tasks = products.Select(async p => await PopulatePharmaceuticalComposition(p));
            await Task.WhenAll(tasks);
            inpid.ExpiredProducts = products.Where(p => p.GetType() == typeof(ExpiredProduct)).Select(ep=>(ExpiredProduct) ep)
                .Union(inpid.ExpiredProducts.Where(ep => !EPARTools.IsEPAR(ep.SPC_Link))).ToList();
            inpid.CurrentlyAuthorisedProducts = products.Where(p => p.GetType() == typeof(CurrentlyAuthorisedProduct)).Select(ep => (CurrentlyAuthorisedProduct)ep)
                .Union(inpid.CurrentlyAuthorisedProducts.Where(ep => !EPARTools.IsEPAR(ep.SPC_Link))).ToList();
            inpid.SuspendedProducts = products.Where(p => p.GetType() == typeof(SuspendedProduct)).Select(ep => (SuspendedProduct)ep)
                .Union(inpid.SuspendedProducts.Where(ep => !EPARTools.IsEPAR(ep.SPC_Link))).ToList();
            return inpid;
        }

        private static async Task PopulatePharmaceuticalComposition(RealProduct product)
        {
            var tempDoc = "";
            // ReSharper disable once RedundantAssignment
            var tempDocx = "";
            // ReSharper disable once RedundantAssignment
            var tf = "";
            var isDoc = product.SPC_Link.EndsWith(".doc", StringComparison.InvariantCultureIgnoreCase);
            try
            {
                if (isDoc)
                {
                    tf = Path.GetTempFileName();
                    tempDoc = $"{tf}.doc";
                    File.Move(tf, tempDoc);

                    using (var fs = File.OpenWrite(tempDoc))
                    {
                        (await GetHttpStream(product.SPC_Link)).CopyTo(fs);
                        fs.Flush();
                    }

                    tempDocx = WordConverter.ConvertDocToDocx(tempDoc);
                }
                else
                {
                    tf = Path.GetTempFileName();
                    tempDocx = $"{tf}.docx";
                    File.Move(tf, tempDocx);
                    using (var fs = File.OpenWrite(tempDocx))
                    {
                        (await GetHttpStream(product.SPC_Link)).CopyTo(fs);
                    }
                }
            }
            catch (Exception)
            {
                return;
            }

            using (var spcStream = File.Open(tempDocx, FileMode.Open))
            {
                try
                {
                    var pc = SPCParser.GetPharmaceuticalComposition(spcStream);
                    product.PharmaceuticalComposition = pc;
                }
                catch (Exception e)
                {
                    
                    throw new AggregateException($"Error encountered for {product.Name}",e);
                }
                
            }

            if (!string.IsNullOrEmpty(tempDoc) && File.Exists(tempDoc))
            {
                File.Delete(tempDoc);
            }

            if (!string.IsNullOrEmpty(tempDocx) && File.Exists(tempDocx))
            {
                File.Delete(tempDocx);
            }
        }

    }
}
