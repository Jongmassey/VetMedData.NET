using System;

namespace VetMedData.NET.Model
{
    /// <inheritdoc />
    /// <summary>
    /// Record of administration of portion of a packaged product
    /// </summary>
    public class AdministeredProduct : ActionedProduct
    {
        public string ReasonForAdministration { get; set; }
        public double ProportionOfPackageAdministered { get; set; }
    }

    /// <inheritdoc />
    /// <summary>
    /// Record of sale of packaged product
    /// </summary>
    public class SoldProduct : ActionedProduct
    {

    }

    public class ActionedProduct : PackagedProduct
    {
        public DateTime ActionDate { get; set; }
        public string ActionedBy { get; set; }
        public TargetSpecies[] TargetSpecies { get; set; }
    }


    /// <summary>
    /// Properties of package of a product
    /// </summary>
    public abstract class PackagedProduct
    {
        public Product Product { get; set; }
        public DateTime Expiry { get; set; }
        public string Batch { get; set; }
        public Package Package { get; set; }
    }
}
