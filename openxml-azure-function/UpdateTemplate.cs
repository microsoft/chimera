using Azure.Storage.Blobs;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Vml.Office;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OpenXMLFunction
{
    public static class UpdateTemplate
    {
        public static async Task UpdateDocumentTemplate(Dictionary<string, string> sections, string connectionString, string sourceContainerName, string sourceBlobName, string destinationContainerName, string destinationBlobName)
        {
            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);

            BlobClient sourceBlobClient = blobServiceClient.GetBlobContainerClient(sourceContainerName).GetBlobClient(sourceBlobName);

            var memoryStream = new MemoryStream();
            await sourceBlobClient.DownloadToAsync(memoryStream);
            memoryStream.Position = 0;

            using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(memoryStream, true))
            {
                Body body = wordDoc.MainDocumentPart.Document.Body;
                FindAndUpdateSections(ref body, "«IntroductionParagraph»", sections["INTRODUCTION"]);
                wordDoc.MainDocumentPart.Document.Save();                
            }

           

            memoryStream.Position = 0;
            BlobClient destinationBlobClient = blobServiceClient.GetBlobContainerClient(destinationContainerName).GetBlobClient(destinationBlobName);
            await destinationBlobClient.UploadAsync(memoryStream, overwrite: true);
        }

        public static void FindAndUpdateSections(ref Body wordDoc, string header, string updateText)
        {
            bool beginSwap = false;
            foreach (var paragraph in wordDoc.Elements<Paragraph>())
            {
                ParagraphProperties properties = paragraph.ParagraphProperties;
                if (properties != null)
                {
                    ParagraphStyleId style = properties.ParagraphStyleId;
                    if (style != null && style.Val != null && style.Val.Value.StartsWith("Heading"))
                    {
                        if (beginSwap)
                        {
                            beginSwap = false;
                        }
                    }
                    else if (paragraph.InnerText.ToLower().Contains(header.ToLower()))
                    {
                        beginSwap = true;
                        paragraph.RemoveAllChildren<Run>();
                        paragraph.AppendChild(new Run(new Text(updateText)));

                    }
                }
            }
        }
      

        //public static async Task ModifyAndSaveWordDocument(string connectionString, string sourceContainerName, string sourceBlobName, string destinationContainerName, string destinationBlobName)
        //{
        //    BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);

        //    BlobClient sourceBlobClient = blobServiceClient.GetBlobContainerClient(sourceContainerName).GetBlobClient(sourceBlobName);

        //    var memoryStream = new MemoryStream();
        //    await sourceBlobClient.DownloadToAsync(memoryStream);
        //    memoryStream.Position = 0;

        //    using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(memoryStream, true))
        //    {
        //        Body body = wordDoc.MainDocumentPart.Document.Body;
        //        body.Append(new Paragraph(new Run(new Text("New text"))));

        //        wordDoc.MainDocumentPart.Document.Save();
        //    }

        //    memoryStream.Position = 0;

        //    BlobClient destinationBlobClient = blobServiceClient.GetBlobContainerClient(destinationContainerName).GetBlobClient(destinationBlobName);

        //    await destinationBlobClient.UploadAsync(memoryStream, overwrite: true);
        //}

    }
}
