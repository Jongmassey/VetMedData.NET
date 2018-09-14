using System;
using System.Collections.Generic;
using System.Linq;

namespace VetMedData.NET.Model
{
    /// <summary>
    /// Defines structure of Product Information Database
    /// from Veterinary Medicines Directorate and provides
    /// data access methods.
    /// </summary>

    [Serializable]
    public class VMDPID
    {
        public DateTime CreatedDateTime { get; set; }
        public List<CurrentlyAuthorisedProduct> CurrentlyAuthorisedProducts { get; set; } =
            new List<CurrentlyAuthorisedProduct>();
        public List<SuspendedProduct> SuspendedProducts { get; set; } =
            new List<SuspendedProduct>();
        public List<ExpiredProduct> ExpiredProducts { get; set; } =
            new List<ExpiredProduct>();
        public List<HomoeopathicProduct> HomoeopathicProducts { get; set; } =
            new List<HomoeopathicProduct>();

        /// <summary>
        /// Unions together all product types 
        /// </summary>
        public IEnumerable<ReferenceProduct> AllProducts => RealProducts
            .Union(HomoeopathicProducts.Select(s => (ReferenceProduct)s));

        /// <summary>
        /// Unions together all product types except Homoeopathic
        /// </summary>
        public IEnumerable<RealProduct> RealProducts => CurrentlyAuthorisedProducts
            .Union(SuspendedProducts.Select(s => (RealProduct)s))
            .Union(ExpiredProducts.Select(s => (RealProduct)s));

        /// <summary>
        /// Unique list of all Pharmaceutical Forms across all products
        /// </summary>
        public IEnumerable<string> PharmaceuticalForms => CurrentlyAuthorisedProducts.Select(s => s.PharmaceuticalForm)
            .Union(SuspendedProducts.Select(s => s.PharmaceuticalForm))
            .Union(HomoeopathicProducts.Select(s => s.PharmaceuticalForm))
            .Distinct();

        /// <summary>
        /// Unique list of all MA Holders across all products
        /// </summary>
        public IEnumerable<string> MAHolders => CurrentlyAuthorisedProducts.Select(s => s.MAHolder)
            .Union(SuspendedProducts.Select(s => s.MAHolder))
            .Union(ExpiredProducts.Select(s => s.MAHolder))
            .Union(HomoeopathicProducts.Select(s => s.MAHolder))
            .Distinct();

        /// <summary>
        /// Unique list of all Authorisation Routes across all products
        /// </summary>
        public IEnumerable<string> AuthorisationRoutes => CurrentlyAuthorisedProducts.Select(s => s.AuthorisationRoute)
            .Union(SuspendedProducts.Select(s => s.AuthorisationRoute))
            .Union(ExpiredProducts.Select(s => s.AuthorisationRoute))
            .Union(HomoeopathicProducts.Select(s => s.AuthorisationRoute))
            .Distinct();

        /// <summary>
        /// Unique list of all Therapeutic Groups across all products
        /// </summary>
        public IEnumerable<string> TherapeuticGroups => CurrentlyAuthorisedProducts.Select(s => s.TherapeuticGroup)
            .Union(SuspendedProducts.Select(s => s.TherapeuticGroup))
            .Union(HomoeopathicProducts.Select(s => s.TherapeuticGroup))
            .Distinct();

        /// <summary>
        /// Unique list of all Active Substances across all products
        /// </summary>
        public IEnumerable<string> ActiveSubstances => CurrentlyAuthorisedProducts.SelectMany(s => s.ActiveSubstances)
            .Union(SuspendedProducts.SelectMany(s => s.ActiveSubstances))
            .Union(ExpiredProducts.SelectMany(s => s.ActiveSubstances))
            .Union(HomoeopathicProducts.SelectMany(s => s.ActiveSubstances))
            .Distinct();

        /// <summary>
        /// Unique list of all Target Species across all products
        /// </summary>
        public IEnumerable<string> TargetSpecies => CurrentlyAuthorisedProducts.SelectMany(s => s.TargetSpecies)
            .Union(SuspendedProducts.SelectMany(s => s.TargetSpecies))
            .Union(HomoeopathicProducts.SelectMany(s => s.TargetSpecies))
            .Distinct();

    }

    /// <summary>
    /// Defines properties common to all reference product types
    /// </summary>
    [Serializable]
    public abstract class ReferenceProduct :Product
    {
        public string MAHolder { get; set; }
        public string VMNo { get; set; }
        public string AuthorisationRoute { get; set; }
        public IEnumerable<string> TargetSpecies { get; set; }
        public IEnumerable<string> ActiveSubstances { get; set; }
        public IEnumerable<TargetSpecies> TargetSpeciesTyped { get; set; }
        public DateTime DateOfIssue { get; set; }

        public override string ToString()
        {
            return $"{GetType()}: Name:{Name} VMNo:{VMNo}";
        }

        public Tuple<DateTime, DateTime> GetProductActiveRange()
        {
            DateTime endDate;
            //if (p.GetType() == typeof(CurrentlyAuthorisedProduct) || p.GetType() == typeof(HomoeopathicProduct))
            //{
            //    endDate = DateTime.Now;
            //}

            if (this.GetType() == typeof(ExpiredProduct))
            {
                endDate = ((ExpiredProduct)this).DateofExpiration;
            }

            else if (this.GetType() == typeof(SuspendedProduct))
            {
                endDate = ((SuspendedProduct)this).DateOfSuspension;
            }
            else
            {
                endDate = DateTime.Now;
            }
            return new Tuple<DateTime, DateTime>(this.DateOfIssue, endDate);
        }
    }
    /// <inheritdoc />
    /// <summary>
    /// ReferenceProduct with a current, active marketing authorisation
    /// </summary>
    [Serializable]
    public class CurrentlyAuthorisedProduct : RealProduct
    {
        public IEnumerable<string> Distributors { get; set; }
        //public DateTime DateOfIssue { get; set; }
        public string ControlledDrug { get; set; }
        public string DistributionCategory { get; set; }
        public string PharmaceuticalForm { get; set; }
        public string TherapeuticGroup { get; set; }
        public string UKPAR_Link { get; set; }
        public string PAAR_Link { get; set; }
    }
    /// <inheritdoc />
    /// <summary>
    /// ReferenceProduct whose marketing authorisation has been suspended
    /// </summary>
    [Serializable]
    public class SuspendedProduct : RealProduct
    {
        public DateTime DateOfSuspension { get; set; }
       // public DateTime DateOfIssue { get; set; }
        public string ControlledDrug { get; set; }
        public string DistributionCategory { get; set; }
        public string PharmaceuticalForm { get; set; }
        public string TherapeuticGroup { get; set; }
        public string UKPAR_Link { get; set; }
        public string PAAR_Link { get; set; }
    }
    /// <inheritdoc />
    /// <summary>
    /// ReferenceProduct whose marketing authorisation has expired
    /// </summary>
    [Serializable]
    public class ExpiredProduct : RealProduct
    {
        public DateTime DateofExpiration { get; set; }
    }

    ///<inheritdoc />
    [Serializable]
    public class RealProduct : ReferenceProduct
    {
        public string SPC_Link { get; set; }
        public IEnumerable<Tuple<string, double, string>> PharmaceuticalComposition { get; set; }
    }

    /// <inheritdoc />
    /// <summary>
    /// Authorised homoepathic product
    /// </summary>
    [Serializable]
    public class HomoeopathicProduct : ReferenceProduct
    {
        //public DateTime DateOfIssue { get; set; }
        public string ControlledDrug { get; set; }
        public string DistributionCategory { get; set; }
        public string PharmaceuticalForm { get; set; }
        public string TherapeuticGroup { get; set; }
    }
}