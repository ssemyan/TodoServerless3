using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace ServerlessTodoApi
{
    public static class TodoItems
	{
        const string devname = "Dev User";
        const string upn = "dev@localhost";

		// Add new item
		[FunctionName("TodoItemAdd")]
		public static HttpResponseMessage AddItem(
			[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "todoitem")]HttpRequestMessage req,
            [Table("apitest")] out TodoItem newTodoItem,
            ILogger log)
		{
			// Get request body
			TodoItem newItem = req.Content.ReadAsAsync<TodoItem>().Result;
            log.LogInformation("Upserting item: " + newItem.ItemName + " for user " + devname);

			if (string.IsNullOrEmpty(newItem.id))
			{
				// New Item so add ID and date
				log.LogInformation("Item is new.");
				newItem.id = Guid.NewGuid().ToString();
				newItem.ItemCreateDate = DateTime.Now;
				newItem.ItemOwner = upn;
                newItem.PartitionKey = upn;
                newItem.RowKey = newItem.id;
			}
			newTodoItem = newItem;

			return req.CreateResponse(HttpStatusCode.OK, newItem);
		}

		// Get all items
		[FunctionName("TodoItemGetAll")]
		public static HttpResponseMessage GetAll(
		   [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "todoitem")]HttpRequestMessage req,
           [Table("apitest")] CloudTable cloudTable,
           ILogger log)
		{
			log.LogInformation("Getting all Todo items for user: " + devname);

            TableQuery<TodoItem> rangeQuery = new TableQuery<TodoItem>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, upn));
            cloudTable.CreateIfNotExistsAsync().Wait();

            var itemQuery = cloudTable.ExecuteQuerySegmentedAsync(rangeQuery, null).Result;

			var ret = new { UserName = devname, Items = itemQuery.ToArray() };

			return req.CreateResponse(HttpStatusCode.OK, ret);
		}
        
		// Delete item by id
		[FunctionName("TodoItemDelete")]
		public static async Task<HttpResponseMessage> DeleteItem(
		   [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "todoitem/{id}")]HttpRequestMessage req,
           [Table("apitest")] CloudTable cloudTable,
           string id,
           ILogger log)
		{
			log.LogInformation("Deleting document with ID " + id + " for user " + devname);

            var entity = new DynamicTableEntity(upn, id);
            entity.ETag = "*";

			try
			{
                // Verify the user owns the document and can delete it
                await cloudTable.ExecuteAsync(TableOperation.Delete(entity));
            }
            catch (StorageException ex)
			{
				if (ex.Message == "Not Found")
				{
					// Document does not exist, is not owned by the current user, or was already deleted
					log.LogInformation("Document with ID: " + id + " not found for " + upn);
				}
				else
				{
					// Something else happened
					throw ex;
				}
			}
            
            return req.CreateResponse(HttpStatusCode.NoContent);
		}
	}
}
