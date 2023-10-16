using System;
using System.Xml.Serialization;

namespace SvR.ContentPrep.Models
{
    /// <summary>
    /// Application information with MSI info for xml serialization
    /// </summary>
    [XmlRoot("ApplicationInfo")]
    [Serializable]
    public class MsiApplicationInfo : ApplicationInfo
    {
        /// <summary>
        /// Msi Application Info constructor
        /// </summary>
        public MsiApplicationInfo() => MsiInfo = new MsiInfo();

        /// <summary>
        /// Gets or sets the MSI info
        /// </summary>
        public MsiInfo MsiInfo { get; set; }
    }
}
