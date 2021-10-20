using System;
using System.Text;
using System.Xml;

using XslMapper.FunctionApp.Exceptions;

namespace Aliencube.XslMapper.FunctionApp.Extensions
{
    /// <summary>
    /// This represents the extension of the <see cref="XmlDocument"/> class.
    /// </summary>
    public static class XmlDocumentExtensions
    {
        /// <summary>
        /// Loads the XML document from string.
        /// </summary>
        /// <param name="xml"><see cref="XmlDocument"/> instance.</param>
        /// <param name="doc">XML string.</param>
        /// <returns><see cref="XmlDocument"/> loaded with XML.</returns>
        public static XmlNode LoadXmlDocument(this XmlDocument xml, string doc)
        {
            if (xml == null)
            {
                throw new ArgumentNullException(nameof(xml));
            }
            if (string.IsNullOrWhiteSpace(doc))
            {
                throw new ArgumentNullException(nameof(doc));
            }
            // Remove the possible BOM characters
            // https://stackoverflow.com/questions/17795167/xml-loaddata-data-at-the-root-level-is-invalid-line-1-position-1#27743515
            string bomMark = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
            if (string.IsNullOrEmpty(bomMark))
            {
                if (doc.StartsWith(bomMark))
                {
                    doc = doc.Remove(0, bomMark.Length);
                }
            }
            xml.LoadXml(doc);
            // Load TED
            XmlNodeList nodeTED = xml.GetElementsByTagName("TED");
            if (nodeTED.Count != 1)
            {
                throw new NodeXmlNotFoundException();
            }
            return nodeTED[0];
        }
    }
}
