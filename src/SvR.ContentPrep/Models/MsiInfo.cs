using System;
using System.Xml.Serialization;

namespace SvR.ContentPrep.Models
{
    [XmlRoot("MsiInfo")]
    [Serializable]
    public class MsiInfo
    {
        public string? MsiProductCode { get; set; }

        public string? MsiProductVersion { get; set; }

        public string? MsiPackageCode { get; set; }

        public string? MsiPublisher { get; set; }

        public string? MsiUpgradeCode { get; set; }

        public ExecutionContext MsiExecutionContext { get; set; }

        public bool MsiRequiresLogon { get; set; }

        public bool MsiRequiresReboot { get; set; }

        public bool MsiIsMachineInstall { get; set; }

        public bool MsiIsUserInstall { get; set; }

        public bool MsiIncludesServices { get; set; }

        public bool MsiIncludesODBCDataSource { get; set; }

        public bool MsiContainsSystemRegistryKeys { get; set; }

        public bool MsiContainsSystemFolders { get; set; }
    }
}
