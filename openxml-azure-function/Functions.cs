using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Web.Http;

namespace OpenXMLFunction
{
    public static class WordDocManagement
    {
        [FunctionName("ParseDocument")]
        public static async Task<IActionResult> ParseDocument(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req)
        {
            string fileName = req.Query["filename"];
            if (string.IsNullOrEmpty(fileName))
            {
                return new BadRequestObjectResult("Please pass a fileName on the query string");
            }
            string connectionString = System.Environment.GetEnvironmentVariable("MyStorageConnectionString", EnvironmentVariableTarget.Process);

            (var content, var headers) = await OpenWordDoc.ReadWordDocumentFromBlobStorage(connectionString, "inbound", fileName);// "TKD-BCS-01991 Protocol-DMPK.docx");

            //return JSON object with {content: content, headers: headers}
            var result = new { content = content, headers = headers };
            return new OkObjectResult(result);
        }


        [FunctionName("GenerateDocument")]
        public static async Task<IActionResult> GenerateDocument(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req)
        {
            //read in the updated content and headers from the request body
            string connectionString = System.Environment.GetEnvironmentVariable("MyStorageConnectionString", EnvironmentVariableTarget.Process);
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);

                var content = data?.content.ToObject<Dictionary<string, string>>();
                var headers = data?.headers.ToObject<Dictionary<string, string>>();

                if (content == null || headers == null)
                {
                    return new BadRequestObjectResult("Please pass content and headers in the request body");
                }
                
                await UpdateTemplate.UpdateDocumentTemplate(content, headers, connectionString, "templates", "R&D DMPK-PKPD-Report.docx", "outbound", $"changedFile_{Guid.NewGuid()}.docx");

                return new OkResult(); 
            }
            catch (Exception ex)
            {
                return new BadRequestErrorMessageResult(ex.Message);
            }
            
            
        }
    }


}
