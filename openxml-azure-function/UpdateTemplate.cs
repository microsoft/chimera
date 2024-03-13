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
                        case "TESTING FACILITY":
                            ReplaceSections(ref body, "##TESTING FACILITY##", section.Value);
                            break;
                        case "Test Article":
                            ReplaceSections(ref body, "##Test Article##", section.Value);
                            break;
                        case "Good Laboratory Practice Compliance":
                            string glp = "";
                            if (section.Value.ToLower().Contains("this study was not conducted in accordance") || section.Value.ToLower().Contains("this study will not be conducted in accordance"))
                            {
                                glp = "This study was not conducted in accordance with the Code of Federal Regulations (CFR), Title 21, Part 58: Good Laboratory Practice (GLP) for Nonclinical Laboratory Studies, issued by the United States Food and Drug Administration (FDA). This study was conducted as a basic exploratory study and as such, the data from this study were not audited by Quality Assurance (QA). However, all data were recorded appropriately and documentation necessary for reconstruction of this study is available in the study files at Takeda Development Center Americas, Inc. (TDCA) (San Diego, CA, USA). The data are accurately reflected in the report.";
                            }
                            else
                            {
                                glp = "This study was conducted in accordance with the Code of Federal Regulations, Title 21, Part 58: Good Laboratory Practice for Nonclinical Laboratory Studies, issued by the United States Food and Drug Administration. The study was conducted to meet GLP standards, and all data were audited by Quality Assurance to ensure accuracy and compliance. The necessary documentation for reconstruction of this study is available in the study files at Takeda Development Center Americas, Inc. (TDCA) (CITY, ST, USA). The data presented in this report accurately reflect the findings of the study.";
                            }
                            ReplaceSections(ref body, "##GOODLABCOMPLIANCE##", glp);
                            break;
                        case "ARCHIVING":
                            ReplaceSections(ref body, "##ARCHIVING##", section.Value);
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

