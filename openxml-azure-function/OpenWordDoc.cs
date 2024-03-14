using Azure.Storage.Blobs;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OpenXMLFunction
{

    public static class OpenWordDoc
    {
        // Define a regular expression that matches "TAK-" followed by one or more digits
        private const string TAKRegexPattern = @"TAK-(\d+)";
        //Define a regular expression that matches "TKD-" followed one or more letters and then one or more digits
        private const string TKDRegexPattern = @"TKD-\w+-(\d+)";

        public static async Task<(Dictionary<string, string>, Dictionary<string, string>)> ReadWordDocumentFromBlobStorage(string connectionString, string containerName, string blobName)
        {
            BlobClient blobClient = CreateBlobClient(connectionString, containerName, blobName);

            var memoryStream = new MemoryStream();
            await blobClient.DownloadToAsync(memoryStream);
            memoryStream.Position = 0;

            using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(memoryStream, false))
            {
                Body body = wordDoc.MainDocumentPart.Document.Body;
                var values = SplitSections(body);
                var headers = PullHeaders(wordDoc.MainDocumentPart);
                return (values, headers);               
            }
        }

        private static BlobClient CreateBlobClient(string connectionString, string containerName, string blobName)
        {
            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            return containerClient.GetBlobClient(blobName);
        }

        private static Dictionary<string, string> PullHeaders(MainDocumentPart mainPart)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>();
            Regex regTAK = new Regex(TAKRegexPattern);
            Regex regTKD = new Regex(TKDRegexPattern);

            foreach (HeaderPart headerPart in mainPart.HeaderParts)
            {
                foreach (Paragraph paragraph in headerPart.Header.Elements<Paragraph>())
                {
                    AddMatchToHeaders(headers, "TAK", regTAK.Match(paragraph.InnerText));
                    AddMatchToHeaders(headers, "TKD", regTKD.Match(paragraph.InnerText));
                }
            }
            return headers;
        }

        private static void AddMatchToHeaders(Dictionary<string, string> headers, string key, Match match)
        {
            if (match.Success)
            {
                headers.Add(key, match.Value);
            }
        }

        private static Dictionary<string, string> SplitSections(Body wordDoc)
        {

            Dictionary<string, string> sections = new Dictionary<string, string>();
            string currentKey = null;
            string paragraphText = string.Empty;

            foreach (var paragraph in wordDoc.Elements<Paragraph>())
            {
                ParagraphProperties properties = paragraph.ParagraphProperties;
                if (properties != null)
                {
                    ParagraphStyleId style = properties.ParagraphStyleId;
                    if (style != null && style.Val != null && style.Val.Value.StartsWith("Heading"))
                    {
                        if (currentKey != null) //next heading found, save the previous section
                        {
                            sections.Add(currentKey, paragraphText.TrimEnd('|'));
                            paragraphText = string.Empty;
                        }
                        currentKey = paragraph.InnerText;
                    }else if (currentKey != null && !string.IsNullOrEmpty(paragraph.InnerText))
                    {
                        paragraphText = paragraphText  + paragraph.InnerText + "||";
                    }
                }
            }
            // add the last section
            if (currentKey != null)
            {
                sections.Add(currentKey, paragraphText.TrimEnd('|'));
            }
            //Remove trailing ||

            return sections;
        }
      

    }
}
