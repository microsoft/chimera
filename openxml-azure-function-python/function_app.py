import os
import json
import azure.functions as func
from azure.storage.blob import BlobServiceClient
from openpyxl import load_workbook
from uuid import uuid4
from dotenv import load_dotenv
from open_word_doc import read_word_document_from_blob_storage
from update_template import update_document_template

load_dotenv()  # take environment variables from .env.
app = func.FunctionApp(http_auth_level=func.AuthLevel.ANONYMOUS)
STORAGE_CONNECTION_STRING_NAME = "MyStorageConnectionString"
INBOUND_CONTAINER_NAME = "inbound"
OUTBOUND_CONTAINER_NAME = "outbound"
TEMPLATE_CONTAINER_NAME = "templates"
TEMPLATE_FILE_NAME = "R&D DMPK-PKPD-Report.docx"

def get_connection_string():
    return os.getenv(STORAGE_CONNECTION_STRING_NAME)

@app.route(route="ParseDocument")
def parse_document(req: func.HttpRequest) -> func.HttpResponse:
     
    file_name = req.params.get('filename')
    if not file_name:
        return func.HttpResponse("Please pass a fileName on the query string", status_code=400)

    connection_string = get_connection_string()

    content, headers = read_word_document_from_blob_storage(connection_string, INBOUND_CONTAINER_NAME, file_name)

    result = {"content": content, "headers": headers}
    return func.HttpResponse(json.dumps(result), status_code=200)

@app.route(route="GenerateDocument")
def generate_document(req: func.HttpRequest) -> func.HttpResponse:
    connection_string = get_connection_string()

    try:
        req_body = req.get_json()

        content = req_body.get('content')
        headers = req_body.get('headers')

        if not content or not headers:
            return func.HttpResponse("Please pass content and headers in the request body", status_code=400)

        update_document_template(content, headers, connection_string, TEMPLATE_CONTAINER_NAME, TEMPLATE_FILE_NAME, OUTBOUND_CONTAINER_NAME, f"changedFile_{uuid4()}.docx")

        return func.HttpResponse(status_code=200)
    except Exception as ex:
        return func.HttpResponse(str(ex), status_code=400)