using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Azure.Storage.Blobs;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace OpenXMLFunction
{

    public static class OpenWordDoc
    {

        public static async Task<Dictionary<string, string>> ReadWordDocumentFromBlobStorage(string connectionString, string containerName, string blobName)
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
                return values;
            }
        }
               

        public static Dictionary<string, string> SplitSections(Body wordDoc)
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
                            sections.Add(currentKey, paragraphText);
                            paragraphText = string.Empty;
                        }
                        currentKey = paragraph.InnerText;
                        isSection = true;
                    }else if (isSection)
                    {
                        paragraphText += paragraph.InnerText;
                    }
                }
            }
            // add the last section
            if (isSection)
            {
                sections.Add(currentKey, paragraphText);
            }
            return sections;
        }
      

    }
}
