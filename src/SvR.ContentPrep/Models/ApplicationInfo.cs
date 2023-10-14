using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace SvR.ContentPrep.Models
{
    [XmlRoot("ApplicationInfo")]
    [XmlInclude(typeof(MsiApplicationInfo))]
    [XmlInclude(typeof(CustomApplicationInfo))]
    [Serializable]
    public class ApplicationInfo
    {
        [XmlAttribute]
        public string? ToolVersion { get; set; }

        public string? Name { get; set; }

        public string? Description { get; set; }

        public long UnencryptedContentSize { get; set; }

        public string? FileName { get; set; }

        public string? SetupFile { get; set; }

        public FileEncryptionInfo? EncryptionInfo { get; set; }

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
                return ApplicationInfo.FromXml(input);
            }
        }

        internal static ApplicationInfo FromXml(TextReader textReader)
        {
            return (ApplicationInfo)new XmlSerializer(typeof(ApplicationInfo)).Deserialize(textReader);
        }

        internal static ApplicationInfo FromXml(Stream xml)
        {
            return (ApplicationInfo)new XmlSerializer(typeof(ApplicationInfo)).Deserialize(xml);
        }
    }
}
