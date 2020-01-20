using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Configuration;
using Microsoft.WindowsAzure.Storage.Blob;

namespace HappyVoting8
{
	public static class NextIdStore
	{
		private const string INFO_ROW_KEY = "!info1";
		private const string INFO_PAR_KEY = INFO_ROW_KEY;
		private const string EMPTY_ROW_KEY = "";

		private static DynamicTableEntity retrieveEntity(CloudTable table, string partition_key)
		{
			string row_key = partition_key == null ? EMPTY_ROW_KEY : INFO_ROW_KEY;
			if (partition_key == null)
				partition_key = INFO_PAR_KEY;

#if OLD
			TableResult result = table.Execute(TableOperation.Retrieve(partition_key, row_key));
			return (DynamicTableEntity)result.Result;
#else
			return TableRowPond.Get(table, partition_key, row_key);		// May be null.
#endif
		}
		public static int GetLastId(CloudTable table, string partition_key)
		{
			DynamicTableEntity entity = retrieveEntity(table, partition_key);

			if (entity == null)
				return -1;
			return (int)entity["nextid"].Int32Value - 1;
		}
		public static int Next(CloudTable table, string partition_key)
		{
			lock (table)
			{
				DynamicTableEntity entity = retrieveEntity(table, partition_key);		// null for nonexistent board.
				TableRowPond.Notify(table, entity.PartitionKey, entity.RowKey);

				int n_value = (int)entity["nextid"].Int32Value;
				entity["nextid"].Int32Value = n_value + 1;

				table.Execute(TableOperation.Replace(entity));      // Throws StorageException ((412) Precondition Failed) if the entity is modified in between.

				return n_value;
			}
		}
		private static void create(CloudTable table, string partition_key, int initial_value)
		{
			string row_key = partition_key == null ? EMPTY_ROW_KEY : INFO_ROW_KEY;
			if (partition_key == null)
				partition_key = INFO_PAR_KEY;

			DynamicTableEntity entity = new DynamicTableEntity(partition_key, row_key);

			entity["nextid"] = new EntityProperty(initial_value);
			table.Execute(TableOperation.Insert(entity));
		}
		public static bool CreateIfNotExists(CloudTable table, string partition_key, int initial_value)
		{
			if (retrieveEntity(table, partition_key) == null)
			{
				create(table, partition_key, initial_value);
				return true;
			}
			return false;
		}
	}
	public static class TableEntityHelper
	{
		public static string GetString(this DynamicTableEntity entity, string key, string default_value)
		{
			EntityProperty ep;

			if (entity.Properties.TryGetValue(key, out ep))
			{
				// ep.StringValue may be null when using "TableQuery().Select(new string[] { "flags" })" and the property does not exist.
				return ep.StringValue ?? default_value;
			}
			else
				return default_value;
		}
		public static string GetIntOrString(this DynamicTableEntity entity, string key)
		{
			EntityProperty ep;

			if (entity.Properties.TryGetValue(key, out ep))
			{
				if (ep.PropertyType == EdmType.String)
					return ep.StringValue;

				return ep.Int32Value.ToString();
			}
			else
				return null;
		}
		public static int GetInt(this DynamicTableEntity entity, string key, int default_value)
		{
			EntityProperty ep;

			if (entity.Properties.TryGetValue(key, out ep))
			{
				return ep.Int32Value ?? default_value;
			}
			else
				return default_value;
		}
		public static DateTimeOffset? GetDateTimeOffset(this DynamicTableEntity entity, string key, DateTimeOffset? default_value)
		{
			EntityProperty ep;

			if (entity.Properties.TryGetValue(key, out ep))
				return ep.DateTimeOffsetValue;
			else
				return default_value;
		}
	}
	public static class CloudTableHelper
	{
		public static string GetStringProperty(this CloudTable table, string partition_key, string row_key, string property_name)
		{
			TableResult result = table.Execute(TableOperation.Retrieve(partition_key, row_key));
			DynamicTableEntity entity = (DynamicTableEntity)result.Result;

			if (entity != null)
				return entity[property_name].StringValue;
			else
				return null;
		}
		public static void SetStringProperty(this CloudTable table, string partition_key, string row_key, string property_name, string value)
		{
			DynamicTableEntity entity = new DynamicTableEntity(partition_key, row_key);

			entity[property_name] = new EntityProperty(value);

			table.Execute(TableOperation.InsertOrReplace(entity));
		}
		public static void EnumerateRowPrefix(this CloudTable table, string par_key, string row_key_prefix, Action<DynamicTableEntity> act)
		{
			string pkFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, par_key);

			string begin = row_key_prefix + Consts.NAME_CONCAT;
			string end = row_key_prefix + Consts.NAME_CONCAT_LIMIT;

			string rkLowerFilter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThan, begin);
			string rkUpperFilter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThan, end);
			string combinedRowKeyFilter = TableQuery.CombineFilters(rkLowerFilter, TableOperators.And, rkUpperFilter);
			string combinedFilter = TableQuery.CombineFilters(pkFilter, TableOperators.And, combinedRowKeyFilter);

			TableQuery query = new TableQuery().Where(combinedFilter);

			foreach (DynamicTableEntity entity in table.ExecuteQuery(query))
				act(entity);
		}
	}
	public static class Warehouse
	{
		public static CloudTable PapersTable { get; private set; }
		public static CloudTable TalliesTable { get; private set; }
		public static CloudTable UsersTable { get; private set; }
		public static CloudTable TotalsTable { get; private set; }
		public static CloudTable RecordsTable { get; private set; }
		public static CloudTable TagScrollsTable { get; private set; }

		public static CloudBlobContainer PictureContainer { get; private set; }

		public static TablePartitionPond<TallyInfo> TalliesPartitionPond { get; private set; }
		public static TablePartitionPond2<TallyInfo> TotalsPartitionPond { get; private set; }
		public static TablePartitionPond2<TaggedVoteInfo> TagScrollsPond { get; private set; }

		public static Random Random { get; private set; }

		public static void Initialize()
		{
			PapersTable = getTable("vtPapers");
			if (PapersTable != null)
				PapersTable.CreateIfNotExists();

			TalliesTable = getTable("vtTallies");
			if (TalliesTable != null)
				TalliesTable.CreateIfNotExists();

			UsersTable = getTable("vtUsers");
			if (UsersTable != null)
				UsersTable.CreateIfNotExists();

			TotalsTable = getTable("vtTotals");
			if (TotalsTable != null)
				TotalsTable.CreateIfNotExists();

			RecordsTable = getTable("vtRecords");
			if (RecordsTable != null)
				RecordsTable.CreateIfNotExists();

			TagScrollsTable = getTable("vtTagScrolls");
			if (TagScrollsTable != null)
				TagScrollsTable.CreateIfNotExists();

			PictureContainer = getContainer("picture1");

			TalliesPartitionPond = new TablePartitionPond<TallyInfo>(TalliesTable);
			TotalsPartitionPond = new TablePartitionPond2<TallyInfo>(TotalsTable);
			TagScrollsPond = new TablePartitionPond2<TaggedVoteInfo>(TagScrollsTable);

			Random = new Random();
		}
		private static CloudBlobContainer getContainer(string container_name)
		{
			string conn_str = ConfigurationManager.AppSettings["StorageConnectionString"];
			CloudStorageAccount storageAccount = CloudStorageAccount.Parse(conn_str);

			CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

			CloudBlobContainer container = blobClient.GetContainerReference(container_name);

			container.CreateIfNotExists();

			container.SetPermissions(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });
			return container;
		}
		private static CloudTable getTable(string table_name)
		{
			try
			{
				string conn_str = ConfigurationManager.AppSettings["StorageConnectionString"];
				CloudStorageAccount storageAccount = CloudStorageAccount.Parse(conn_str);

				CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

				CloudTable table = tableClient.GetTableReference(table_name);
				return table;
			}
			catch (System.Configuration.ConfigurationErrorsException)		// table is not available while running as a website.
			{
				return null;
			}
			catch (ArgumentNullException)		// running unit test.
			{
				return null;
			}
		}
	}
}