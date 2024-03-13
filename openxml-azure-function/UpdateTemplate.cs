using Azure.Storage.Blobs;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OpenXMLFunction
{
    public static class UpdateTemplate
    {
        public static async Task UpdateDocumentTemplate(Dictionary<string, string> sections, Dictionary<string, string> headers, string connectionString, string sourceContainerName, string sourceBlobName, string destinationContainerName, string destinationBlobName)
        {
            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);

            BlobClient sourceBlobClient = blobServiceClient.GetBlobContainerClient(sourceContainerName).GetBlobClient(sourceBlobName);

            var memoryStream = new MemoryStream();
            await sourceBlobClient.DownloadToAsync(memoryStream);
            memoryStream.Position = 0;

            using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(memoryStream, true))
            {

                MainDocumentPart mainDocumentPart = wordDoc.MainDocumentPart;
                //Repalce headers
                ReplaceHeader(ref mainDocumentPart, headers);

                mainDocumentPart.Document.Save();

                //Replace sections
                Body body = mainDocumentPart.Document.Body;

                foreach (var section in sections)
                {

                    switch (section.Key)
                    {
                        case "INTRODUCTION":
                            ReplaceSections(ref body, "##INTRODUCTION##", section.Value);
                            break;
                        case "STUDY OBJECTIVE":
                            ReplaceSections(ref body, "##OBJECTIVES##", section.Value);
                            break;
                        case "MATERIALS":
                            ReplaceSections(ref body, "##MATERIALS##", section.Value);
                            break;
                        case "Test System":
                            ReplaceSections(ref body, "##TESTSYSTEM##", section.Value);
                            break;
                        default:
                            //Non standard headers to be evaluated here.
                            if (section.Key.ToLower().StartsWith("document title"))
                            {
                                ReplaceSections(ref body, "TITLE:##TITLE##", $"TITLE: {section.Key.Substring(16)}");
                                ReplaceSections(ref body, "##TITLE##", section.Key.Substring(16));
                            }
                            break;
                    }
                }
                mainDocumentPart.Document.Save();
            }

            memoryStream.Position = 0;
            BlobClient destinationBlobClient = blobServiceClient.GetBlobContainerClient(destinationContainerName).GetBlobClient(destinationBlobName);
            await destinationBlobClient.UploadAsync(memoryStream, overwrite: true);
        }

        private static void ReplaceSections(ref Body wordDoc, string searchText, string updateText)
        {
            var paragraphs = wordDoc.Elements<Paragraph>()
                    .Where(p => p.InnerText.Contains(searchText)).ToList();

            foreach (var paragraph in paragraphs)
            {
                // Remove the existing runs
                paragraph.RemoveAllChildren<Run>();

                // Add a new run with the new text
                paragraph.Append(new Run(new Text(updateText)));

            }
        }

        private static void ReplaceHeader(ref MainDocumentPart mainDocumentPart, Dictionary<string, string> headerValues)
        {
            // Get the header parts
            IEnumerable<HeaderPart> headerParts = mainDocumentPart.HeaderParts;

            // Loop through each header part
            foreach (HeaderPart headerPart in headerParts)
            {
        
                foreach (var currentText in headerPart.RootElement.Descendants<Text>())
                {
                    if (currentText.Text.Contains("TAK-"))
                    {
                        currentText.Text = currentText.Text.Replace("TAK-", headerValues["TAK"]);
                    }
                    if (currentText.Text.Contains("XXX"))
                    {
                        currentText.Text = currentText.Text.Replace("XXX", "");
                    }
                    if (currentText.Text.Contains("TKD-BCS-"))
                    {
                        currentText.Text = currentText.Text.Replace("TKD-BCS-", headerValues["TKD"]);
                    }
                    if (currentText.Text.Contains("XX-RX"))//Three X's have already been removed
                    {
                        currentText.Text = currentText.Text.Replace("XX-RX", string.Empty);
                    }
                }

            }
        }

    }
}

