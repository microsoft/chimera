using Azure.Storage.Blobs;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OpenXMLFunction
{

    public static class OpenWordDoc
    {

        public static async Task<(Dictionary<string, string>, Dictionary<string, string>)> ReadWordDocumentFromBlobStorage(string connectionString, string containerName, string blobName)
        {
            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);

            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            BlobClient blobClient = containerClient.GetBlobClient(blobName);

            var memoryStream = new MemoryStream();
            await blobClient.DownloadToAsync(memoryStream);
            memoryStream.Position = 0;

            using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(memoryStream, false))
            {
                Body body = wordDoc.MainDocumentPart.Document.Body;
                var values = SplitSections(body);
                var headers = PullHeaders(wordDoc.MainDocumentPart);
                //Add a summary value to store the summary result from the AI service
                values.Add("Summary", "");
                return (values, headers);
                //Dictionary<string, string> smaller = new Dictionary<string, string>();
                //for (int i = 0; i < 3; i++)
                //{
                //    smaller.Add(values.Keys.ElementAt(i), values.Values.ElementAt(i));
                //}

                //return (smaller, headers);
            }
        }
               
        private static Dictionary<string, string> PullHeaders(MainDocumentPart mainPart)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>();
            
            // Get the header parts
            IEnumerable<HeaderPart> headerParts = mainPart.HeaderParts;
            // Loop through each header part
            foreach (HeaderPart headerPart in headerParts)
            {
                // Get the header
                Header header = headerPart.Header;
                // Get the paragraphs in the header
                IEnumerable<Paragraph> paragraphs = header.Elements<Paragraph>();
                
                // Define a regular expression that matches "TAK-" followed by one or more digits
                Regex regTAK = new Regex(@"TAK-(\d+)");
                //Define a regular expression that matches "TKD-" followed one or more letters and then one or more digits
                Regex regTKD = new Regex(@"TKD-\w+-(\d+)");

                foreach (Paragraph paragraph in paragraphs)
                {
                    Match match = regTAK.Match(paragraph.InnerText);
                    if (match.Success)
                    {
                        headers.Add("TAK", match.Value);
                    }
                    match = regTKD.Match(paragraph.InnerText);
                    if (match.Success)
                    {
                        headers.Add("TKD", match.Value);
                    }                    
                }
            }
            return headers;
        }


        private static Dictionary<string, string> SplitSections(Body wordDoc)
        {

            Dictionary<string, string> sections = new Dictionary<string, string>();
            string currentKey = string.Empty;
            string paragraphText = string.Empty;
            bool isSection = false;

            foreach (var paragraph in wordDoc.Elements<Paragraph>())
            {
                ParagraphProperties properties = paragraph.ParagraphProperties;
                if (properties != null)
                {
                    ParagraphStyleId style = properties.ParagraphStyleId;
                    if (style != null && style.Val != null && style.Val.Value.StartsWith("Heading"))
                    {
                        if (isSection) //next heading found, save the previous section
                        {
                            sections.Add(currentKey, paragraphText.TrimEnd('|'));
                            paragraphText = string.Empty;
                        }
                        currentKey = paragraph.InnerText;
                        isSection = true;
                    }else if (isSection && !string.IsNullOrEmpty(paragraph.InnerText))
                    {
                        paragraphText = paragraphText  + paragraph.InnerText + "||";
                    }
                }
            }
            // add the last section
            if (isSection)
            {
                sections.Add(currentKey, paragraphText.TrimEnd('|'));
            }
            //Remove trailing ||

            return sections;
        }
      

    }
}
