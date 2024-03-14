import os
import logging
import re
import semantic_kernel as sk
from semantic_kernel.connectors.ai.open_ai import AzureChatCompletion
from semantic_kernel.functions.kernel_arguments import KernelArguments

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
    
class Transform:
    @staticmethod
    async def transform_content(kernel: sk.Kernel, content: str) -> str:
        # 1. Call ChangeTense plugin
        change_tense_sk_function = kernel.plugins["EditingPlugin"]["ChangeTense"]
        change_tense_args = KernelArguments(input=content, tense="past")
        result = await kernel.invoke(change_tense_sk_function, change_tense_args)
        
        # 2. Call RunningText plugin
        running_text_sk_function = kernel.plugins["EditingPlugin"]["RunningText"]
        running_text_args = KernelArguments(input=str(result))
        result = await kernel.invoke(running_text_sk_function, running_text_args)    
        
        # Call ArticlesPluralPeriods
        articles_plural_periods_sk_function = kernel.plugins["EditingPlugin"]["ArticlesPluralPeriods"]
        articles_plural_periods_args = KernelArguments(input=str(result))
        result = await kernel.invoke(articles_plural_periods_sk_function, articles_plural_periods_args)
        
        # Call HeadingsTitles
        headings_titles_sk_function = kernel.plugins["EditingPlugin"]["HeadingsTitles"]
        headings_titles_args = KernelArguments(input=str(result))
        result = await kernel.invoke(headings_titles_sk_function, headings_titles_args)
        
        # Call TablesFigures
        tables_figures_sk_function = kernel.plugins["EditingPlugin"]["TablesFigures"]
        tables_figures_args = KernelArguments(input=str(result))
        result = await kernel.invoke(tables_figures_sk_function, tables_figures_args)

        # 3. Return results
        
        return str(result)
    
    @staticmethod
    def findAbbreviations(content: str):
        # need to use regex to find all occurrenes of word-like strings containing at least two capital letters
        # pattern = r'\b[A-Z]{2,}\b'
        # pattern for any string containing at least two captital letters, with all connected non-whitespace characters
        pattern2 = r'\w*[A-Z]{2,}\w*'
        matches = re.findall(pattern2, content)
        return matches