using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Web.Http;

namespace OpenXMLFunction
{
    public static class WordDocManagement
    {
        [FunctionName("ReadWordDoc")]
        public static async Task<IActionResult> ReadWordDoc(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req)
        {
            string connectionString = System.Environment.GetEnvironmentVariable("MyStorageConnectionString", EnvironmentVariableTarget.Process);

            (var content, var headers)=  await OpenWordDoc.ReadWordDocumentFromBlobStorage(connectionString, "inbound", "TKD-BCS-01991 Protocol-DMPK.docx");

            return new OkObjectResult(content);
        }

        [FunctionName("UpdateWordDoc")]
        public static async Task<IActionResult> UpdateWordDoc(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req)
        {

            string connectionString = System.Environment.GetEnvironmentVariable("MyStorageConnectionString", EnvironmentVariableTarget.Process);
            try
            {
                (var content, var headers) = await OpenWordDoc.ReadWordDocumentFromBlobStorage(connectionString, "inbound", "TKD-BCS-01991 Protocol-DMPK.docx");
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
