import os
import logging
import semantic_kernel as sk
from semantic_kernel.connectors.ai.open_ai import AzureChatCompletion

class KernelFactory:
    @staticmethod
    def create_kernel() -> sk.Kernel:
        kernel = sk.Kernel()

        deployment = os.getenv('AZURE_OPENAI_DEPLOYMENT_NAME')
        api_key = os.getenv('AZURE_OPENAI_API_KEY') 
        endpoint = os.getenv('AZURE_OPENAI_ENDPOINT')
        script_directory = os.path.dirname(__file__)
        plugins_directory = os.path.join(script_directory, "plugins")
        
        service_id=None
        
        plugin_names = [plugin for plugin in os.listdir(plugins_directory) if os.path.isdir(os.path.join(plugins_directory, plugin))]
        
        # for each plugin, add the plugin to the kernel
        try:
            for plugin_name in plugin_names:
                kernel.import_plugin_from_prompt_directory(plugins_directory, plugin_name)
        except ValueError as e:
            logging.exception(f"Plugin {plugin_name} not found")

        #add the chat service
        service = AzureChatCompletion(
              service_id=service_id,
              deployment_name=deployment,
              endpoint=endpoint,
              api_key=api_key
            #   api_version="2024-02-15-preview"
        )

        kernel.add_service(service)

        return kernel