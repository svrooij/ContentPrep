using System;
using System.Xml.Serialization;

namespace SvR.ContentPrep.Models
{
    [XmlRoot("ApplicationInfo")]
    [Serializable]
    public class MsiApplicationInfo : ApplicationInfo
    {
        public MsiApplicationInfo() => MsiInfo = new MsiInfo();

        public MsiInfo MsiInfo { get; set; }
    }
}
