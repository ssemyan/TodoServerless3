using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
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
        private static AuthorizedUser GetCurrentUserName(ILogger log, ClaimsPrincipal principal)
        {
            // On localhost claims will be empty
            string name = "Dev User";
            string upn = "dev@localhost";

            if (principal != null)
            {
                foreach (Claim claim in principal.Claims)
                {
                    if (claim.Type == "name")
                    {
                        name = claim.Value;
                    }
                    if (claim.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")
                    {
                        upn = claim.Value;
                    }
                    //Uncomment to print all claims to log output for debugging
                    //log.LogInformation("Claim: " + claim.Type + " Value: " + claim.Value);
                }
            }
            else
            {
                log.LogInformation("No claims information - assuming localhost dev user");
            }
            return new AuthorizedUser() { DisplayName = name, UniqueName = upn };
        }

        // Add new item
        [FunctionName("TodoItemAdd")]
		public static HttpResponseMessage AddItem(
			[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "todoitem")]HttpRequestMessage req,
            [Table("apitest")] out TodoItem newTodoItem,
            ILogger log,
            ClaimsPrincipal principal)
		{
            // Get request body
            var currUser = GetCurrentUserName(log, principal);

            TodoItem newItem = req.Content.ReadAsAsync<TodoItem>().Result;
            log.LogInformation("Upserting item: " + newItem.ItemName + " for user " + currUser.DisplayName);

			if (string.IsNullOrEmpty(newItem.id))
			{
				// New Item so add ID and date
				log.LogInformation("Item is new.");
				newItem.id = Guid.NewGuid().ToString();
				newItem.ItemCreateDate = DateTime.Now;
				newItem.ItemOwner = currUser.UniqueName;
                newItem.PartitionKey = currUser.UniqueName;
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
           ILogger log,
           ClaimsPrincipal principal)
		{
            var currUser = GetCurrentUserName(log, principal);

            log.LogInformation("Getting all Todo items for user: " + currUser.DisplayName);

            TableQuery<TodoItem> rangeQuery = new TableQuery<TodoItem>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, currUser.UniqueName));
            cloudTable.CreateIfNotExistsAsync().Wait();

            var itemQuery = cloudTable.ExecuteQuerySegmentedAsync(rangeQuery, null).Result;

			var ret = new { UserName = currUser.DisplayName, Items = itemQuery.ToArray() };

			return req.CreateResponse(HttpStatusCode.OK, ret);
		}
        
		// Delete item by id
		[FunctionName("TodoItemDelete")]
		public static async Task<HttpResponseMessage> DeleteItem(
		   [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "todoitem/{id}")]HttpRequestMessage req,
           [Table("apitest")] CloudTable cloudTable,
           string id,
           ILogger log,
           ClaimsPrincipal principal)
		{
            var currUser = GetCurrentUserName(log, principal);

            log.LogInformation("Deleting document with ID " + id + " for user " + currUser.DisplayName);

            var entity = new DynamicTableEntity(currUser.UniqueName, id);
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
					log.LogInformation("Document with ID: " + id + " not found for " + currUser.UniqueName);
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
