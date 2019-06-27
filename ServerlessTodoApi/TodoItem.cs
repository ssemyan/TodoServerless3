using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace ServerlessTodoApi
{
    public class TodoItem : TableEntity
    {
		public string id { get; set; }
		public string ItemName { get; set; }
		public string ItemOwner { get; set; }
		public DateTime? ItemCreateDate { get; set; }
	}
}
