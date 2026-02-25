using System.IO.Compression;
using System.Text;
using System.Xml;

namespace VectorRagDemo.BLL
{
    public static class DocumentParser
    {
        public static string ParseToText(byte[] bytes, string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".txt" => Encoding.UTF8.GetString(bytes),
                ".docx" => ParseDocx(bytes),
                _ => throw new NotSupportedException($"File type '{extension}' is not supported.")
            };
        }

        private static string ParseDocx(byte[] bytes)
        {
            using var stream = new MemoryStream(bytes);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

            var documentEntry = archive.GetEntry("word/document.xml");
            if (documentEntry == null)
                throw new InvalidOperationException("Invalid .docx file: word/document.xml not found.");

            using var docStream = documentEntry.Open();
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(docStream);

            var nsManager = new XmlNamespaceManager(xmlDoc.NameTable);
            nsManager.AddNamespace("w", "http://schemas.openxmlformats.org/wordprocessingml/2006/main");

            var paragraphs = xmlDoc.SelectNodes("//w:p", nsManager);
            if (paragraphs == null)
                return string.Empty;

            var sb = new StringBuilder();
            foreach (XmlNode para in paragraphs)
            {
                var textNodes = para.SelectNodes(".//w:t", nsManager);
                if (textNodes == null) continue;

                var paraText = new StringBuilder();
                foreach (XmlNode textNode in textNodes)
                    paraText.Append(textNode.InnerText);

                var paraStr = paraText.ToString().Trim();
                if (!string.IsNullOrEmpty(paraStr))
                    sb.AppendLine(paraStr);
            }

            return sb.ToString();
        }
    }
}
