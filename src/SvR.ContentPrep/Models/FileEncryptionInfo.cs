using System;
using System.Xml.Serialization;

namespace SvR.ContentPrep.Models
{
    /// <summary>
    /// File encryption info.
    /// </summary>
    [XmlRoot("EncryptionInfo")]
    [Serializable]
    public class FileEncryptionInfo
    {
        /// <summary>
        /// Gets or sets the encryption key.
        /// </summary>
        public string? EncryptionKey { get; set; }

        /// <summary>
        /// Gets or sets the mac key.
        /// </summary>
        public string? MacKey { get; set; }

        /// <summary>
        /// Gets or sets the initialization vector.
        /// </summary>
        public string? InitializationVector { get; set; }

        /// <summary>
        /// Gets or sets the mac.
        /// </summary>
        public string? Mac { get; set; }

        /// <summary>
        /// Gets or sets the profile identifier.
        /// </summary>
        public string? ProfileIdentifier { get; set; }

        /// <summary>
        /// Gets or sets the file digest.
        /// </summary>
        public string? FileDigest { get; set; }

        /// <summary>
        /// Gets or sets the file digest algorithm.
        /// </summary>
        public string? FileDigestAlgorithm { get; set; }
    }
}
