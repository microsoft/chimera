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
                ReplaceHeader(ref mainDocumentPart, headers);
                FindAndReplace(ref mainDocumentPart, "TAKXXX", headers["TAK"].Substring(4));//TAK-XXX doesn't show the hyphen in InnerText
                FindAndReplace(ref mainDocumentPart, "TKD-BCS-XXXXX-RX", headers["TKD"]);
                FindAndReplace(ref mainDocumentPart, "TKD-BCS-XXXXX", headers["TKD"]);

                //Replace sections
                Body body = mainDocumentPart.Document.Body;

                foreach (var section in sections)
                {

                    switch (section.Key.ToUpper())
                    {
                        case "INTRODUCTION":
                            ReplaceSections(ref body, "##INTRODUCTION##", section.Value.Replace("||", ""));//Remove the double pipes
                            break;
                        case "STUDY OBJECTIVE":
                            ReplaceSections(ref body, "##OBJECTIVES##", section.Value.Replace("||", ""));
                            break;
                        case "MATERIALS":
                            ReplaceSections(ref body, "##MATERIALS##", section.Value.Replace("||", ""));
                            break;
                        case "TEST SYSTEM":
                            ReplaceSections(ref body, "##TESTSYSTEM##", section.Value.Replace("||", ""));
                            break;
                        case "TESTING FACILITY":
                            ReplaceSections(ref body, "##TESTING FACILITY##", section.Value.Replace("||", ""));
                            break;
                        case "TEST ARTICLE":
                            ReplaceSections(ref body, "##Test Article##", section.Value.Replace("||", ""));
                            break;
                        case "ARCHIVING":
                            ReplaceSections(ref body, "##ARCHIVING##", section.Value.Replace("||", ""));
                            break;
                        case "GOOD LABORATORY PRACTICE COMPLIANCE":
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
                        case "AMENDMENTS TO, AND DEVIATIONS FROM, THE PROTOCOL":
                            ReplaceSections(ref body, "##DEVIATIONSFROMPROTOCOL##", section.Value.Replace("||", ""));
                            break;
                        default:
                            //Non standard headers to be evaluated here.
                            if (section.Key.ToUpper().StartsWith("DOCUMENT TITLE"))
                            {
                                DocumentTitleReplace(ref mainDocumentPart, section);
                            }
                            if (section.Key.ToUpper().StartsWith("PROTOCOL APPROVAL"))
                            {
                                //need to split the section value into parts
                                var parts = section.Value.Split("||").ToList();
                                AddApprovals(ref body, parts);
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

        private static void DocumentTitleReplace(ref MainDocumentPart mainDocumentPart, KeyValuePair<string, string> section)
        {
            var titlePage = section.Value.Split("||").ToList();
            var body = mainDocumentPart.Document.Body;

            ReplaceSections(ref body, "TITLE:##TITLE##", $"TITLE: {section.Key.Substring(16)}");
            ReplaceSections(ref body, "##TITLE##", section.Key.Substring(16));
            //Replace Study Director
            string studyDir = titlePage.Where(t => t.Contains("Study Director")).FirstOrDefault();
            if (!string.IsNullOrEmpty(studyDir))
            {
                string[] splitValue = studyDir.Split(":");
                if (splitValue.Length > 1)
                {
                    FindAndReplace(ref mainDocumentPart, "STUDYDIRECTOR", splitValue[1]);
                }
            }
        }

        private static void FindAndReplace(ref MainDocumentPart mainDocumentPart, string searchText, string replaceValue)
        {
            //Method that looks for specific text in the document and replaces it with the replace value
            var body = mainDocumentPart.Document.Body;

            List<Text> previousTextElements = new List<Text>();
            foreach (var text in body.Descendants<Text>())
            {
                previousTextElements.Add(text);
                string combinedText = string.Concat(previousTextElements.Select(t => t.Text));

                if (combinedText.Contains(searchText))
                {
                    int index = combinedText.IndexOf(searchText);
                    string remainingTextAfterReplacement = combinedText.Substring(index + searchText.Length);
                                       
                    // Add the replaced text
                    previousTextElements.Last().Text = replaceValue + remainingTextAfterReplacement;

                    // Clear the list of previous elements
                    previousTextElements.Clear();
                }
            }
            mainDocumentPart.Document.Save();
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

        private static void AddApprovals(ref Body wordDoc, List<string> updateTextList)
        {
            var oldTable = wordDoc.Elements<Table>()
                    .FirstOrDefault(t => t.InnerText.Contains("##APPROVED BY##"));

            if (oldTable != null)
            {
                Table newTable = new Table();
                // Add rows and cells to the newTable as needed...
                for (int i = 0; i < updateTextList.Count; i = i + 4)
                {
                    newTable.Append(CreateNewRow(updateTextList[i], true));
                    newTable.Append(CreateNewRow(updateTextList[i + 2]));
                    newTable.Append(CreateNewRow(updateTextList[i + 3]));
                    newTable.Append(CreateNewRow(" "));
                }

                wordDoc.ReplaceChild(newTable, oldTable);               
            }
        }

        private static TableRow CreateNewRow(string text, bool highlighted = false)
        {
            Run run = new Run(new Text(text));
            if (highlighted)
            {
                run.RunProperties = new RunProperties();
                run.RunProperties.Color = new Color() { Val = "FF0000" }; // Red color
                run.RunProperties.FontSize = new FontSize() { Val = "22" }; // Font size
                run.RunProperties.Italic = new Italic(); // Italic text
            }
            TableRow newRow = new TableRow();
            TableCell newCell = new TableCell(new Paragraph(run));
            if (highlighted)
            {
                // Add bottom border to the cell
                TableCellProperties cellProperties = new TableCellProperties();
                TableCellBorders cellBorders = new TableCellBorders();
                cellBorders.BottomBorder = new BottomBorder() { Val = BorderValues.Single, Size = 12 };
                cellProperties.Append(cellBorders);
                newCell.Append(cellProperties);
            }
            newRow.Append(newCell);
            return newRow;
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
            mainDocumentPart.Document.Save();
        }


    }
}

