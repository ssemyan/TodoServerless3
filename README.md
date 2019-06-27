
# TodoServerless3 - Azure Functions Todo List Sample

This sample demonstrates a simple single page application web frontend and Azure Functions 2.0 api backend. It uses Azure Table Storage to store data. It does not use authentication.

This code can be run locally (using the Azure Functions CLI and Azure Storage emulator) as well as in Azure. Instructions for both are below.

The application is a simple Todo list where users can add items "todo". The items are stored in a single Azure Storage Table. Because there is no authentication, all users will share the same items. 

The SPA is very simple with Bootstrap for styles, Knockout.js for data binding, and JQuery for ajax calls. 

Users can add new items to their list, or mark existing items as complete (which deletes them). The inital call to the API pulls the current list of items for the user, along with the user's display name. 

This sample is similar to https://github.com/ssemyan/TodoServerless but uses Functions 2.0 and instead of serving the content from a storage account via a proxy, it uses an App Service to host the HTML and JS. Costs will be higher due to the use of CosmosDB.

This sample is also similar to https://github.com/ssemyan/TodoServerless2 but it uses Azure Storage Tables instead of CosmosDB and does not make use of Authentication. Costs will be lower due to the use of table storage. 

Note: the connection string to the storage account is automatically added to the Functions app. Another approach would be to use Managed Service Identity and retrieve it from a KeyVault using the technique described here: https://docs.microsoft.com/en-us/azure/app-service/app-service-key-vault-references

## Setup steps on Localhost

1. Install the Azure CLI tools from here: https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local

1. If you want to use the emulator for local development, install the Storage emulator from here: https://docs.microsoft.com/en-us/azure/storage/common/storage-use-emulator

1. If you are not using the emulator, create a storage account in Azure. 

1. Update the connection string in **_local.settings.json_** to the one for the emulator or Azure

1. Right click the solution, choose properties, and set both the ui and api project to start. 

## Setup steps on Azure - Manual

1. Create a new Azure App Service 

1. Create a new Azure Functions app

1. Add a CORS setting in the Azure Functions app to allow origins from the App Service

## Setup steps on Azure - Automated using ARM Template

1. Create a resource group to hold the resources. For example: 

```
New-AzResourceGroup -Name mygroup -Location westeurope
```

2. Update the values in the *parameter.json* file to match your values: 
    * *web_ui_name* (app service for the UI)
    * *web_api_name* (app service for the API)
    * *app_service_plan_name* (app service plan shared by both UI and API)
    * *storage_act_name* (storage account used by the funcion)

1. Create a new deployment by running ARM template with the parameter file in the resource group created above:

```
New-AzResourceGroupDeployment -ResourceGroupName mygroup -TemplateFile .\template.json -TemplateParameterFile .\parameters.json -Verbose
```

## Final setup steps

1. Update the remoteUrl locations in **_vars.js_** to point to the functions endpoint

1. Push the code for the front and backend and then navigate to the UI. You should be prompted to log in and then the API will run. 