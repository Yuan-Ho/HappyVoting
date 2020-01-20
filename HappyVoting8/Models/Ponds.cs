using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Caching;
using Microsoft.WindowsAzure.Storage.Table;
using System.Collections.Concurrent;

namespace HappyVoting8
{
	public static class PaperPond
	{
		private static PaperRoll insertNew(string key, string vote_id)
		{
			PaperRoll obj = new PaperRoll(vote_id);		// if concurrency happens, this will be done more than once.

			HttpRuntime.Cache.Insert(key, obj, null, DateTime.Now.AddSeconds(Consts.DEFAULT_CACHE_SECONDS),
											Cache.NoSlidingExpiration, CacheItemPriority.Default, removedCallback);

			// expires after some time for multi-node synchronization (unreliable).
			return obj;
		}
		private static void removedCallback(string key, Object value, CacheItemRemovedReason reason)
		{
			if (reason == CacheItemRemovedReason.Expired)
			{
				PaperRoll obj = (PaperRoll)value;
				//insertNew(key, obj.VoteId);
			}
		}
		public static PaperRoll Get(string vote_id)
		{
			string key = "PaperPond." + vote_id;

			PaperRoll obj = (PaperRoll)HttpRuntime.Cache.Get(key);

			if (obj == null)
				obj = insertNew(key, vote_id);

			return obj;
		}
	}
	public static class TableRowPond
	{
		private static string key(CloudTable table, string partition_key, string row_key)
		{
			string key = "tr" + Consts.NAME_CONCAT + table.Name + Consts.NAME_CONCAT + partition_key + Consts.NAME_CONCAT + row_key;
			return key;
		}
		private static DynamicTableEntity insertNew(CloudTable table, string partition_key, string row_key)
		{
			TableResult result = table.Execute(TableOperation.Retrieve(partition_key, row_key));
			DynamicTableEntity obj = (DynamicTableEntity)result.Result;

			if (obj != null)
			{
				string cache_key = key(table, partition_key, row_key);

				HttpRuntime.Cache.Insert(cache_key, obj, null, DateTime.Now.AddSeconds(Consts.DEFAULT_CACHE_SECONDS),
											Cache.NoSlidingExpiration);
			}
			return obj;
		}
		public static DynamicTableEntity Get(CloudTable table, string partition_key, string row_key)
		{
			string cache_key = key(table, partition_key, row_key);
			DynamicTableEntity obj = (DynamicTableEntity)HttpRuntime.Cache.Get(cache_key);

			if (obj == null)
				obj = insertNew(table, partition_key, row_key);

			return obj;		// The etag may have expired and cannot be used to update table.
		}
		public static void Notify(CloudTable table, string partition_key, string row_key)
		{
			string cache_key = key(table, partition_key, row_key);
			DynamicTableEntity obj = (DynamicTableEntity)HttpRuntime.Cache.Remove(cache_key);
		}
	}
	public interface IInitializeByEntity
	{
		void Initialize(DynamicTableEntity entity);
	}
	public class TablePartitionPond<T> where T : IInitializeByEntity, new()
	{
		private CloudTable table;

		public TablePartitionPond(CloudTable table)
		{
			this.table = table;
		}
		private string key(string partition_key)
		{
			string key = "tp" + Consts.NAME_CONCAT + table.Name + Consts.NAME_CONCAT + partition_key;
			return key;
		}
		private ConcurrentQueue<T> insertNew(string partition_key)
		{
			ConcurrentQueue<T> queue = new ConcurrentQueue<T>();

			TableQuery query = new TableQuery().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partition_key));

			foreach (DynamicTableEntity entity in table.ExecuteQuery(query))
			{
				T t = new T();
				t.Initialize(entity);
				queue.Enqueue(t);
			}
			string cache_key = key(partition_key);

			HttpRuntime.Cache.Insert(cache_key, queue, null, DateTime.Now.AddSeconds(Consts.DEFAULT_CACHE_SECONDS),
										Cache.NoSlidingExpiration);
			return queue;
		}
		public IEnumerable<T> Get(string partition_key)
		{
			string cache_key = key(partition_key);
			ConcurrentQueue<T> obj = (ConcurrentQueue<T>)HttpRuntime.Cache.Get(cache_key);

			if (obj == null)
				obj = insertNew(partition_key);

			return obj;		// The etag may have expired and cannot be used to update table.
			// It is not O(1) when converting queue to enumerable.
		}
		public void Notify(string partition_key)
		{
			string cache_key = key(partition_key);
			ConcurrentQueue<T> obj = (ConcurrentQueue<T>)HttpRuntime.Cache.Remove(cache_key);
		}
	}
	public class TablePartitionPond2<T> where T : IInitializeByEntity, new()
	{
		private CloudTable table;

		public TablePartitionPond2(CloudTable table)
		{
			this.table = table;
		}
		private string key(string partition_key)
		{
			string key = "tp2" + Consts.NAME_CONCAT + table.Name + Consts.NAME_CONCAT + partition_key;
			return key;
		}
		private ConcurrentDictionary<string, T> insertNew(string partition_key)
		{
			ConcurrentDictionary<string, T> dict = new ConcurrentDictionary<string, T>();

			TableQuery query = new TableQuery().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partition_key));

			foreach (DynamicTableEntity entity in table.ExecuteQuery(query))
			{
				T t = new T();
				t.Initialize(entity);
				dict[entity.RowKey] = t;
			}
			string cache_key = key(partition_key);

			HttpRuntime.Cache.Insert(cache_key, dict, null, DateTime.Now.AddSeconds(Consts.DEFAULT_CACHE_SECONDS),
										Cache.NoSlidingExpiration);
			return dict;
		}
		public IEnumerable<T> Get(string partition_key)
		{
			string cache_key = key(partition_key);
			ConcurrentDictionary<string, T> obj = (ConcurrentDictionary<string, T>)HttpRuntime.Cache.Get(cache_key);

			if (obj == null)
				obj = insertNew(partition_key);

			return obj.Values;		// The etag may have expired and cannot be used to update table.
			// It is not O(1) when converting to enumerable.
		}
		public void AddOrUpdate(string partition_key, string row_key, DynamicTableEntity entity)
		{
			string cache_key = key(partition_key);
			ConcurrentDictionary<string, T> obj = (ConcurrentDictionary<string, T>)HttpRuntime.Cache.Get(cache_key);

			if (obj != null)
			{
				obj.AddOrUpdate(row_key,
					rk =>
					{
						T t = new T();
						t.Initialize(entity);
						return t;
					},
					(rk, t) =>
					{
						t.Initialize(entity);
						return t;
					});
			}
		}
		public void Notify(string partition_key)
		{
			string cache_key = key(partition_key);
			ConcurrentDictionary<string, T> obj = (ConcurrentDictionary<string, T>)HttpRuntime.Cache.Remove(cache_key);
		}
	}
}