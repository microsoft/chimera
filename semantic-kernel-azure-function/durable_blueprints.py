import json
import logging
import azure.functions as func
import azure.durable_functions as df

from helpers import (
    KernelFactory,
    Transform
)
from semantic_kernel.functions.kernel_arguments import KernelArguments

# To learn more about blueprints in the Python prog model V2,
# see: https://learn.microsoft.com/en-us/azure/azure-functions/functions-reference-python?tabs=asgi%2Capplication-level&pivots=python-mode-decorators#blueprints

# Note, the `func` namespace does not contain Durable Functions triggers and bindings, so to register blueprints of
# DF we need to use the `df` package's version of blueprints.
bp = df.Blueprint()

# We define a standard function-chaining DF pattern

@bp.route(route="startOrchestrator")
@bp.durable_client_input(client_name="client")
async def start_orchestrator(req: func.HttpRequest, client):
    payload: str = json.dumps(req.get_json())
    instance_id = await client.start_new("my_orchestrator", client_input=payload)
    
    logging.info(f"Started orchestration with ID = '{instance_id}'.")
    return client.create_check_status_response(req, instance_id)

@bp.orchestration_trigger(context_name="context")
def my_orchestrator(context: df.DurableOrchestrationContext):
    payload = json.loads(context.get_input())
    headers = payload["headers"]
    
    sections = yield context.call_activity("get_sections", json.dumps(payload))
    tasks = []
    for s in sections:
        tasks.append(context.call_activity("transform_content", s))
    
    outputs = yield context.task_all(tasks)
    
    content = {}
    for k, v in outputs:
        content[k] = v    
    # Add a new activity to the content for Summary
    content["Summary"] = yield context.call_activity("summarize_content", content)

    response = []
    response.append(("content", content))
    response.append(("headers", headers))
    
    return dict(response)

@bp.activity_trigger(input_name="payload")
def get_sections(payload: str) -> list:
    sections = json.loads(payload)["content"]
    return [(name, value) for name, value in sections.items()]
    
@bp.activity_trigger(input_name="content")
async def transform_content(content: tuple) -> tuple:
    kernel = KernelFactory.create_kernel()
    
    key = content[0]
    start_value = content[1]
    
    result = await Transform.transform_content(kernel, start_value)
    
    return (key, result)

@bp.activity_trigger(input_name="summary")
async def summarize_content(content: list) -> str:
    kernel = KernelFactory.create_kernel()
    keys = ("INTRODUCTION", "STUDY OBJECTIVE", "MATERIALS", "Test Article", "Test System", "Plasma incubation", "Table 3Incubation Conditions", "BIOANALYSIS")

    start_value = content[0][1]
    
    for k, v in content:
        if (k in keys):
             start_value =  start_value + ' ' + v


    running_text_sk_function = kernel.plugins["EditingPlugin"]["Summary"]
    running_text_args = KernelArguments(input=str(start_value))
    result = await kernel.invoke(running_text_sk_function, running_text_args)    

    return (result)