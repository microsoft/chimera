[![Deploy Python project to Azure Function App](https://github.com/microsoft/chimera/actions/workflows/azure-functions-app-python.yml/badge.svg)](https://github.com/microsoft/chimera/actions/workflows/azure-functions-app-python.yml)

# Project Chimera

This repo contains 3 Azure Function projects which demonstrate how to:
1. Pull content from a Word Document.
1. Use Azure OpenAI to validate and transform extracted content against known style guides.
1. Re-assemble a template document with generated contents.

## Projects
- [semantic-kernal-azure-function](semantic-kernal-azure-function) - A Durable Python Azure Function which accepts extracted document contents and uses a selection of Semantic Kernel functions to validate and transform content.
- [openxml-azure-function](openxml-azure-function) - A Dotnet Core Azure Function project which extracts content from a word document and re-assembles a template document using supplied content sections.
- [openxml-azure-function-python](openxml-azure-function-python) - A partial example of implementing the dotnet core document assembly functions using python.

# Getting Started
## semantic-kernal-azure-function
1. Clone this repo to your machine
1. Rename the [local.settings.example.json](semantic-kernal-azure-function/local.settings.example.json) file to `local.settings.json`
1. Edit the file and update with your settings:
```
{
  "IsEncrypted": false,
  "Values": {
    "FUNCTIONS_WORKER_RUNTIME": "python",
    "AzureWebJobsFeatureFlags": "EnableWorkerIndexing",
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "AZURE_OPENAI_DEPLOYMENT_NAME": "gpt-35-turbo-16k",
    "AZURE_OPENAI_ENDPOINT": "",
    "AZURE_OPENAI_API_KEY": "",
    "AZURE_STORAGE_ACCOUNT_URL": "",
    "AZURE_STORAGE_CONTAINER_NAME": "files",
    "AZURE_STORAGE_BLOB_NAME": "Abbreviations.csv",
    "AZURE_CLIENT_ID": "",
    "AZURE_CLIENT_SECRET": "",
    "AZURE_TENANT_ID": ""
  }
}
```


## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft 
trademarks or logos is subject to and must follow 
[Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.
Any use of third-party trademarks or logos are subject to those third-party's policies.
