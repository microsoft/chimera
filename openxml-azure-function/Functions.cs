using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;

namespace OpenXMLFunction
{
    public static class WordDocManagement
    {
        private const string StorageConnectionStringName = "MyStorageConnectionString";
        private const string InboundContainerName = "inbound";
        private const string OutboundContainerName = "outbound";
        private const string TemplateContainerName = "templates";
        private const string TemplateFileName = "R&D DMPK-PKPD-Report.docx";


        [FunctionName("ParseDocument")]
        public static async Task<IActionResult> ParseDocument(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req)
        {
            string fileName = req.Query["filename"];
            if (string.IsNullOrEmpty(fileName))
            {
                return new BadRequestObjectResult("Please pass a fileName on the query string");
            }

            string connectionString = GetConnectionString();

            (var content, var headers) = await OpenWordDoc.ReadWordDocumentFromBlobStorage(connectionString, InboundContainerName, fileName);

            var result = new { content = content, headers = headers };
            return new OkObjectResult(result);
        }


        [FunctionName("GenerateDocument")]
        public static async Task<IActionResult> GenerateDocument(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req)
        {
            string connectionString = GetConnectionString();
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);

                var content = data?.content.ToObject<Dictionary<string, string>>();
                var headers = data?.headers.ToObject<Dictionary<string, string>>();

                List<Dictionary<string, string>> abbreviationsList = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(data?.abbreviations.ToString());
                var abbreviations = abbreviationsList.SelectMany(dict => dict)
                                                     .ToDictionary(pair => pair.Key, pair => pair.Value);

                if (content == null || headers == null || abbreviations == null)
                {
                    return new BadRequestObjectResult("Please pass content, abbreviations and headers in the request body");
                }

                await UpdateTemplate.UpdateDocumentTemplate(content, headers, abbreviations, connectionString, TemplateContainerName, TemplateFileName, OutboundContainerName, $"changedFile_{Guid.NewGuid()}.docx");

                return new OkResult(); 
            }
            catch (Exception ex)
            {
                return new BadRequestErrorMessageResult(ex.Message);
            }
            
            
        }

        private static string GetConnectionString()
        {
            return System.Environment.GetEnvironmentVariable(StorageConnectionStringName, EnvironmentVariableTarget.Process);
        }
    }
}
