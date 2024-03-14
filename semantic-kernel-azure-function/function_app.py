import logging
import json
import semantic_kernel as sk
import azure.functions as func

from semantic_kernel.functions.kernel_arguments import KernelArguments
from semantic_kernel.planners.sequential_planner import SequentialPlanner
from durable_blueprints import bp
from helpers import (
    KernelFactory,
    Transform
)

app = func.FunctionApp(http_auth_level=func.AuthLevel.FUNCTION)
app.register_functions(bp) # register the DF functions

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

    kernel = KernelFactory.create_kernel()
    
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
    
    kernel = KernelFactory.create_kernel()
    
    req_body = req.get_json()
    
    headers = req_body["headers"]
    sections = [(name, value) for name, value in req_body["content"].items()]
    contents = {}
    
    for k, v in sections:
        contents[k] = await Transform.transform_content(kernel, v)

    results = []
    results.append(("content", contents))
    results.append(("headers", headers))
    
    return func.HttpResponse(json.dumps(dict(results)))
