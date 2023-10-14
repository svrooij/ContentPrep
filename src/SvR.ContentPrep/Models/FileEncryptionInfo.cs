using System;
using System.Xml.Serialization;

namespace SvR.ContentPrep.Models
{
    [XmlRoot("EncryptionInfo")]
    [Serializable]
    public class FileEncryptionInfo
    {
        public string? EncryptionKey { get; set; }

        public string? MacKey { get; set; }

        public string? InitializationVector { get; set; }

        public string? Mac { get; set; }

        public string? ProfileIdentifier { get; set; }

        public string? FileDigest { get; set; }

        public string? FileDigestAlgorithm { get; set; }
    }
}
