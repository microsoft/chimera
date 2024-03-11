import azure.functions as func
import logging
import semantic_kernel as sk
from semantic_kernel.connectors.ai.open_ai import (
    AzureChatCompletion,
    # OpenAIChatCompletion,
)
from semantic_kernel.functions.kernel_arguments import KernelArguments

app = func.FunctionApp(http_auth_level=func.AuthLevel.ANONYMOUS)

@app.route(route="plugins/{pluginName}/functions/{functionName}")
async def ExecuteFunction(req: func.HttpRequest) -> func.HttpResponse:
    logging.info('Python Http trigger ExecuteFunction processed a request.')

    plugin_name = req.route_params.get('pluginName')
    function_name = req.route_params.get('functionName')
    
    if not plugin_name or not function_name:
        logging.error("No plugin name or function name provided.")
        return func.HttpResponse(
             "Pass a plugin name and function name in the route for a personalized response.",
             status_code=400
        )

    kernel = create_kernel()

    plugins_directory = "./plugins"
    try:
        plugin = kernel.import_plugin_from_prompt_directory(plugins_directory, plugin_name)
    except ValueError as e:
        logging.exception(f"Plugin {plugin_name} not found")
        return func.HttpResponse(f"Plugin {plugin_name} not found", status_code=404)

    sk_function = plugin[function_name]

    req_body = {}
    try:
        req_body = req.get_json()
    except ValueError:
        logging.warning("No JSON body provided in request.")
    

    kernel_args = KernelArguments(**req_body)
    for k, v in req_body.items():
        kernel_args[k] = v
    
    
    # result = kernel.invoke(sk_function, 
    #                              sk.KernelArguments(
    #                                  input="""Homo Sapiens (HS) is a common cause of hospital-acquired infection. 
    #                                  Nosocomial HS infection is also a source of community-acquired infection.
    #                                  The same can be said of the Apis Mellifera. As everyone knows AM is a well known predator"""))
    result = await kernel.invoke(sk_function, kernel_args)

    logging.debug(f"Model response {result.get_inner_content().choices[0].message.content}")    
    return func.HttpResponse(str(result))



    
def create_kernel() -> sk.Kernel:

    kernel = sk.Kernel()

    deployment, api_key, endpoint = sk.azure_openai_settings_from_dot_env()
    service_id="default"
    
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