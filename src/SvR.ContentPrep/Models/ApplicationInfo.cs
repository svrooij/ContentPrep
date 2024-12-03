using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace SvRooij.ContentPrep.Models
{
    /// <summary>
    /// Application information for xml serialization
    /// </summary>
    [XmlRoot("ApplicationInfo")]
    [XmlInclude(typeof(MsiApplicationInfo))]
    [Serializable]
    public class ApplicationInfo
    {
        /// <summary>
        /// Version of the tool that will be used to create the package (copied from original tool)
        /// </summary>
        [XmlAttribute]
        public string? ToolVersion { get; set; }

        /// <summary>
        /// Gets or sets the application name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the application description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the unencrypted content size.
        /// </summary>
        public long UnencryptedContentSize { get; set; }

        /// <summary>
        /// Gets or sets the filename inside the package.
        /// </summary>
        public string? FileName { get; set; }

        /// <summary>
        /// Gets or sets the setup file.
        /// </summary>
        public string? SetupFile { get; set; }

        /// <summary>
        /// Gets or sets the encryption info or the package.
        /// </summary>
        public FileEncryptionInfo? EncryptionInfo { get; set; }

        /// <summary>
        /// Writes the application info to xml.
        /// </summary>
        /// <returns>serialized output</returns>
        public string ToXml()
        {
            using (StringWriter output = new StringWriter())
            {
                XmlWriter xmlWriter = XmlWriter.Create(output, new XmlWriterSettings()
                {
                    OmitXmlDeclaration = true,
                    Indent = true,
                    NewLineHandling = NewLineHandling.Entitize
                });
                XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
                new XmlSerializer(GetType()).Serialize(xmlWriter, this, namespaces);
                output.Flush();
                return output.ToString();
            }
        }

        internal static ApplicationInfo FromXml(string xml)
        {
            using (StringReader input = new StringReader(xml))
            {
                return ApplicationInfo.FromXml(input)!;
            }
        }

        internal static ApplicationInfo? FromXml(TextReader textReader)
        {
            return new XmlSerializer(typeof(ApplicationInfo)).Deserialize(textReader) as ApplicationInfo;
        }

        internal static ApplicationInfo? FromXml(Stream xml)
        {
            return new XmlSerializer(typeof(ApplicationInfo)).Deserialize(xml) as ApplicationInfo;
        }
    }
}
