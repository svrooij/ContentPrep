using System;
using System.Xml.Serialization;

namespace SvR.ContentPrep.Models
{
    /// <summary>
    /// MSI information for xml serialization
    /// </summary>
    [XmlRoot("MsiInfo")]
    [Serializable]
    public class MsiInfo
    {
        /// <summary>
        /// Gets or sets the MSI product code.
        /// </summary>
        public string? MsiProductCode { get; set; }

        /// <summary>
        /// Gets or sets the MSI product version.
        /// </summary>
        public string? MsiProductVersion { get; set; }

        /// <summary>
        /// Gets or sets the MSI package code.
        /// </summary>
        public string? MsiPackageCode { get; set; }

        /// <summary>
        /// Gets or sets the MSI publisher
        /// </summary>
        public string? MsiPublisher { get; set; }

        /// <summary>
        /// Gets or sets the MSI upgrade code
        /// </summary>
        public string? MsiUpgradeCode { get; set; }

        /// <summary>
        /// Gets or sets the MSI MsiExecutionContext
        /// </summary>
        public ExecutionContext MsiExecutionContext { get; set; }

        /// <summary>
        /// Gets or sets the MSI RequiresLogon
        /// </summary>
        public bool MsiRequiresLogon { get; set; }

        /// <summary>
        /// Gets or sets the MSI RequiresReboot
        /// </summary>
        public bool MsiRequiresReboot { get; set; }

        /// <summary>
        /// Gets or sets the MSI IsMachineInstall
        /// </summary>
        public bool MsiIsMachineInstall { get; set; }

        /// <summary>
        /// Gets or sets the MSI IsUserInstall
        /// </summary>
        public bool MsiIsUserInstall { get; set; }

        /// <summary>
        /// Gets or sets the MSI IncludesServices
        /// </summary>
        public bool MsiIncludesServices { get; set; }

        /// <summary>
        /// Gets or sets the MSI IncludesODBCDataSource
        /// </summary>
        public bool MsiIncludesODBCDataSource { get; set; }

        /// <summary>
        /// Gets or sets the MSI ContainsSystemRegistryKeys
        /// </summary>
        public bool MsiContainsSystemRegistryKeys { get; set; }

        /// <summary>
        /// Gets or sets the MSI ContainsSystemFolders
        /// </summary>
        public bool MsiContainsSystemFolders { get; set; }
    }
}
