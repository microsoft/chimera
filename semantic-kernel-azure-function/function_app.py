import azure.functions as func
import logging
import semantic_kernel as sk
import os, json
from semantic_kernel.connectors.ai.open_ai import (
    AzureChatCompletion,
)
from semantic_kernel.functions.kernel_arguments import KernelArguments
from semantic_kernel.planners.sequential_planner import SequentialPlanner

app = func.FunctionApp(http_auth_level=func.AuthLevel.ANONYMOUS)

@app.route(route="plugins/{pluginName}/functions/{functionName}")
async def ExecutePluginFunction(req: func.HttpRequest) -> func.HttpResponse:
    logging.info('Python Http trigger ExecutePluginFunction processed a request.')

    plugin_name = req.route_params.get('pluginName')
    function_name = req.route_params.get('functionName')
    
    if not plugin_name or not function_name:
        logging.error("No plugin name or function name provided.")
        return func.HttpResponse(
             "Pass a plugin name and function name in the route for a personalized response.",
             status_code=400
        )

    kernel = create_kernel()
    
    sk_function = kernel.plugins[plugin_name][function_name]

    req_body = {}
    try:
        req_body = req.get_json()
    except ValueError:
        logging.warning("No JSON body provided in request.")
    

    kernel_args = KernelArguments(**req_body)
    for k, v in req_body.items():
        kernel_args[k] = v
    
    result = await kernel.invoke(sk_function, kernel_args)

    logging.debug(f"Model response {result.get_inner_content().choices[0].message.content}")
    return func.HttpResponse(str(result))

@app.route(route="planner")
async def ExecutePlannerFunction(req: func.HttpRequest) -> func.HttpResponse:
    logging.info('Python Http trigger ExecutePlannerFunction processed a request.')
    
    req_body = req.get_json()
    
    return func.HttpResponse("Hello World!")
    
 
@app.route(route="transform")
async def ExecuteTransformFunction(req: func.HttpRequest) -> func.HttpResponse:
    logging.info('Python Http trigger ExecuteTransformFunction processed a request.')
    
    kernel = create_kernel()
    
    req_body = req.get_json()
    
    headers = req_body["headers"]
    sections = [(name, value) for name, value in req_body["content"].items()]
    contents = {}
    
    for k, v in sections:
        contents[k] = await transform_content(kernel, v)

    results = []
    results.append(("headers", headers))
    results.append(("content", contents))

    return func.HttpResponse(json.dumps(dict(results)))


async def transform_content(kernel: sk.Kernel, content: str) -> str:
    # 1. Call ChangeTense plugin
    change_tense_sk_function = kernel.plugins["EditingPlugin"]["ChangeTense"]
    change_tense_args = KernelArguments()
    change_tense_args["input"] = content
    change_tense_args["tense"] = "past"
    
    result = await kernel.invoke(change_tense_sk_function, change_tense_args)
    result = result.value[0].content
    
    # 2. Call RunningText plugin
    running_text_sk_function = kernel.plugins["EditingPlugin"]["RunningText"]
    running_text_args = KernelArguments()
    running_text_args["input"] = str(result)
    
    result = await kernel.invoke(running_text_sk_function, running_text_args)    
    result = result.value[0].content
    
    # 3. Return results
    
    return str(result)
    
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