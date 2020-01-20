using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.WindowsAzure.Storage.Table;
using System.Text;

namespace HappyVoting8
{
	public class ActionData
	{
		public string mat { get; set; }
		public string value { get; set; }
		public string type { get; set; }
		public string pare { get; set; }
	}
	public static class PaperStore
	{
		public static string CreatePaper(string user_name)
		{
			for (; ; )
			{
				string vote_id = Util.RandomAlphaNumericString(8);

				if (NextIdStore.CreateIfNotExists(Warehouse.PapersTable, vote_id, 0))
				{
					createAction(vote_id, user_name, new ActionData() { mat = "infos-create" });

					return vote_id;
				}
			}
		}
		public static string[] AddActions(string vote_id, string user_name, ActionData[] actions)
		{
			string[] hps = new string[actions.Length];

			for (int i = 0; i < actions.Length; i++)
			{
				ActionData action_data = actions[i];
				string hp = createAction(vote_id, user_name, action_data);
				hps[i] = hp;
			}
			return hps;
		}
		private static string createAction(string vote_id, string user_name, ActionData mat_data)
		{
			int next_id = NextIdStore.Next(Warehouse.PapersTable, vote_id);
			string row_key = Consts.ACTION_ID_CHAR + next_id.ToString("D" + Consts.ACTION_ID_DIGITS);

			DynamicTableEntity entity = new DynamicTableEntity(vote_id, row_key);

			entity[Consts.MAT_PROP_NAME] = new EntityProperty(mat_data.mat);
			entity[Consts.WHO_PROP_NAME] = new EntityProperty(user_name);
			entity[Consts.TIME_PROP_NAME] = new EntityProperty(DateTimeOffset.UtcNow);

			if (mat_data.value != null)
				entity[Consts.VALUE_PROP_NAME] = new EntityProperty(mat_data.value);
			if (mat_data.type != null)
				entity[Consts.TYPE_PROP_NAME] = new EntityProperty(mat_data.type);
			if (mat_data.pare != null)
				entity[Consts.PARE_PROP_NAME] = new EntityProperty(mat_data.pare);

			string hp = PaperPond.Get(vote_id).AddMat(entity);		// If put this line after inserting into db, when cache miss and the roll is initialized from db, the mat will be added twice.
			Warehouse.PapersTable.Execute(TableOperation.Insert(entity));

			return hp;
		}
		public static string ActionToHtmlPresentation(DynamicTableEntity entity)
		{
			// HTML format for google crawler.
			StringBuilder builder = new StringBuilder("<p");
			string value = "";

			foreach (KeyValuePair<string, EntityProperty> pair in entity.Properties)
			{
				if (pair.Key == Consts.VALUE_PROP_NAME)
					value = HttpContext.Current.Server.HtmlEncode(pair.Value.StringValue);
				else if (pair.Value.PropertyType == EdmType.DateTime)
					builder.AppendFormat(" data-{0}=\"{1}\"", pair.Key, Convertor.ToString(pair.Value.DateTimeOffsetValue));
				else
					builder.AppendFormat(" data-{0}=\"{1}\"", pair.Key, HttpUtility.HtmlAttributeEncode(pair.Value.StringValue));
			}
			builder.AppendFormat(">{0}</p>", value);
			return builder.ToString();
		}
		public static void GetAllActions(string vote_id, Action<DynamicTableEntity> callback)
		{
			TableQuery query = new TableQuery().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, vote_id));

			foreach (DynamicTableEntity entity in Warehouse.PapersTable.ExecuteQuery(query))
			{
				if (entity.RowKey[0] != Consts.SPECIAL_KEY_PREFIX)
					callback(entity);
			}
		}
	}
	public class TallyData
	{
		public string mat { get; set; }
		public int tpt { get; set; }		// Tally point
	}
	public class TallyInfo : IInitializeByEntity
	{
		public string sbjt_mat;
		public string ansr_mat;
		public int tpt;

		public TallyInfo()
		{
		}
		public TallyInfo(DynamicTableEntity entity)
			: this()
		{
			this.Initialize(entity);
		}
		public void Initialize(DynamicTableEntity entity)
		{
			string[] mats = entity.RowKey.Split(Consts.NAME_CONCAT);

			this.sbjt_mat = mats[0];
			this.ansr_mat = mats[1];

			this.tpt = (int)entity[Consts.TPT_PROP_NAME].Int32Value;
		}
	}
	public static class TallyStore
	{
		public static IEnumerable<TallyInfo> GetTallies(string vote_id, string user_name)
		{
			string partition_key = vote_id + Consts.NAME_CONCAT + user_name;
			IEnumerable<TallyInfo> tallies = Warehouse.TalliesPartitionPond.Get(partition_key);
			return tallies;
		}
		public static void ResetTallies(string vote_id, string user_name, string sbjt_mat)
		{
			string partition_key = vote_id + Consts.NAME_CONCAT + user_name;

			Warehouse.TalliesTable.EnumerateRowPrefix(partition_key, sbjt_mat,
				entity =>
				{
					Warehouse.TalliesTable.Execute(TableOperation.Delete(entity));

					TotalStore.SubtractTotal(vote_id, sbjt_mat, entity);
				});
			Warehouse.TalliesPartitionPond.Notify(partition_key);
		}
		public static void AddTallies(string vote_id, string user_name, string sbjt_mat, TallyData[] tallies)
		{
			foreach (TallyData tally_data in tallies)
			{
				addTally(vote_id, user_name, sbjt_mat, tally_data);

				TotalStore.AddTotal(vote_id, sbjt_mat, tally_data);
			}
		}
		private static void addTally(string vote_id, string user_name, string sbjt_mat, TallyData tally_data)
		{
			string partition_key = vote_id + Consts.NAME_CONCAT + user_name;
			string row_key = sbjt_mat + Consts.NAME_CONCAT + tally_data.mat;

			DynamicTableEntity entity = new DynamicTableEntity(partition_key, row_key);

			entity[Consts.TPT_PROP_NAME] = new EntityProperty(tally_data.tpt);

			Warehouse.TalliesTable.Execute(TableOperation.Insert(entity));
			Warehouse.TalliesPartitionPond.Notify(partition_key);
		}
	}
	public static class TotalStore
	{
		public static IEnumerable<TallyInfo> GetTotals(string vote_id)
		{
			string partition_key = vote_id;
			IEnumerable<TallyInfo> totals = Warehouse.TotalsPartitionPond.Get(partition_key);
			return totals;
		}
		public static void AddTotal(string vote_id, string sbjt_mat, TallyData tally_data)
		{
			string partition_key = vote_id;
			string row_key = sbjt_mat + Consts.NAME_CONCAT + tally_data.mat;

			TableResult result = Warehouse.TotalsTable.Execute(TableOperation.Retrieve(partition_key, row_key));
			DynamicTableEntity entity = (DynamicTableEntity)result.Result;

			if (entity != null)
			{
				entity[Consts.TPT_PROP_NAME].Int32Value += tally_data.tpt;
				Warehouse.TotalsTable.Execute(TableOperation.Replace(entity));
			}
			else
			{
				entity = new DynamicTableEntity(partition_key, row_key);
				entity[Consts.TPT_PROP_NAME] = new EntityProperty(tally_data.tpt);
				Warehouse.TotalsTable.Execute(TableOperation.Insert(entity));
			}
			Warehouse.TotalsPartitionPond.AddOrUpdate(partition_key, row_key, entity);
			TotalsDamper.Update(vote_id,
				new TallyInfo()
				{
					sbjt_mat = sbjt_mat,
					ansr_mat = tally_data.mat,
					tpt = (int)entity[Consts.TPT_PROP_NAME].Int32Value
				});
		}
		public static void SubtractTotal(string vote_id, string sbjt_mat, DynamicTableEntity e)
		{
			string partition_key = vote_id;
			string row_key = e.RowKey;

			TallyInfo info = new TallyInfo(e);

			TableResult result = Warehouse.TotalsTable.Execute(TableOperation.Retrieve(partition_key, row_key));
			DynamicTableEntity entity = (DynamicTableEntity)result.Result;

			if (entity != null)
			{
				//int tpt = (int)e[Consts.TPT_PROP_NAME].Int32Value;
				int tpt = info.tpt;

				entity[Consts.TPT_PROP_NAME].Int32Value -= tpt;
				Warehouse.TotalsTable.Execute(TableOperation.Replace(entity));

				Warehouse.TotalsPartitionPond.AddOrUpdate(partition_key, row_key, entity);
				TotalsDamper.Update(vote_id,
					new TallyInfo()
					{
						sbjt_mat = sbjt_mat,
						ansr_mat = info.ansr_mat,
						tpt = (int)entity[Consts.TPT_PROP_NAME].Int32Value
					});
			}
		}
	}
	public static class RecordStore
	{
		public static void AddRecord(string vote_id, string sbjt_mat, string user_name, string client_ip)
		{
			string partition_key = vote_id + Consts.NAME_CONCAT + sbjt_mat;

			DynamicTableEntity entity = new DynamicTableEntity(partition_key, user_name);

			entity["name"] = new EntityProperty(user_name);
			entity["ip"] = new EntityProperty(client_ip);

			Warehouse.RecordsTable.Execute(TableOperation.Insert(entity));		// StorageException (遠端伺服器傳回一個錯誤: (409) 衝突。) if already exists.

			entity.RowKey = client_ip;
			Warehouse.RecordsTable.Execute(TableOperation.InsertOrReplace(entity));		// Use InsertOrReplace to allow same IP vote twice.
		}
		public static bool CheckUser(string vote_id, string sbjt_mat, string user_name)
		{
			string partition_key = vote_id + Consts.NAME_CONCAT + sbjt_mat;
			TableResult result = Warehouse.RecordsTable.Execute(TableOperation.Retrieve(partition_key, user_name));

			return result.Result != null;
		}
		public static bool CheckIp(string vote_id, string sbjt_mat, string client_ip)
		{
			string partition_key = vote_id + Consts.NAME_CONCAT + sbjt_mat;
			TableResult result = Warehouse.RecordsTable.Execute(TableOperation.Retrieve(partition_key, client_ip));

			return result.Result != null;
		}
		public static void ResetRecord(string vote_id, string sbjt_mat, string user_name)
		{
			string partition_key = vote_id + Consts.NAME_CONCAT + sbjt_mat;

			TableResult result = Warehouse.RecordsTable.Execute(TableOperation.Retrieve(partition_key, user_name));
			DynamicTableEntity entity = (DynamicTableEntity)result.Result;

			if (entity != null)
			{
				string client_ip = entity["ip"].StringValue;
				Warehouse.RecordsTable.Execute(TableOperation.Delete(entity));

				TableResult result2 = Warehouse.RecordsTable.Execute(TableOperation.Retrieve(partition_key, client_ip));
				DynamicTableEntity entity2 = (DynamicTableEntity)result2.Result;
				if (entity2 != null)
					Warehouse.RecordsTable.Execute(TableOperation.Delete(entity2));

				//DynamicTableEntity entity2 = new DynamicTableEntity(partition_key, client_ip);
				//entity2.ETag = "*";
			}
		}
	}
	public class TaggedVoteInfo : IInitializeByEntity
	{
		public string VoteId;
		public string Title;
		public DateTimeOffset Time;
		public string Who;

		public TaggedVoteInfo()
		{
		}
		public TaggedVoteInfo(DynamicTableEntity entity)
			: this()
		{
			this.Initialize(entity);
		}
		public void Initialize(DynamicTableEntity entity)
		{
			this.Title = entity["title"].StringValue;
			this.VoteId = entity["voteid"].StringValue;

			this.Who = entity[Consts.WHO_PROP_NAME].StringValue;
			this.Time = (DateTimeOffset)entity[Consts.TIME_PROP_NAME].DateTimeOffsetValue;
		}
		public string ToHtmlPresentation()
		{
			// HTML format for google crawler.
			return string.Format("<li><a href='{4}{0}'>{1}</a> by <b>{2}</b> at <time>{3}</time></li>",
				this.VoteId, HttpContext.Current.Server.HtmlEncode(this.Title), this.Who,
				Convertor.ToString(this.Time), Consts.VOTE_PAGE_PATH);
		}
	}
	public static class TagScrollStore
	{
		private static DynamicTableEntity buildEntity(string tag, int next_id)
		{
			int page_id = (int)(next_id / Consts.NUM_VOTES_IN_A_PAGE);
			int inner_id = next_id - page_id * Consts.NUM_VOTES_IN_A_PAGE;

			string partition_key = Consts.TAG_NAME_KEY_PREFIX + tag.ToLowerInvariant() + Consts.NAME_CONCAT +
								Consts.PAGE_KEY_PREFIX + page_id.ToString();
			string row_key = Consts.INNER_KEY_PREFIX + inner_id.ToString("D" + Consts.TAGSCROLL_INNER_KEY_DIGITS);

			return new DynamicTableEntity(partition_key, row_key);
		}
		public static void AddVote(string vote_id, string tag, string title, string user_name)
		{
			string partition_key = Consts.TAG_NAME_KEY_PREFIX + tag.ToLowerInvariant();

			NextIdStore.CreateIfNotExists(Warehouse.TagScrollsTable, partition_key, 0);
			int next_id = NextIdStore.Next(Warehouse.TagScrollsTable, partition_key);

			DynamicTableEntity entity = buildEntity(tag, next_id);

			entity["voteid"] = new EntityProperty(vote_id);
			entity["title"] = new EntityProperty(title);
			entity[Consts.WHO_PROP_NAME] = new EntityProperty(user_name);
			entity[Consts.TIME_PROP_NAME] = new EntityProperty(DateTimeOffset.UtcNow);

			Warehouse.TagScrollsTable.Execute(TableOperation.Insert(entity));
			Warehouse.TagScrollsPond.AddOrUpdate(entity.PartitionKey, entity.RowKey, entity);
		}
		public static int GetLastPageId(string tag)
		{
			string partition_key = Consts.TAG_NAME_KEY_PREFIX + tag.ToLowerInvariant();
			int last_id = NextIdStore.GetLastId(Warehouse.TagScrollsTable, partition_key);

			if (last_id == -1)
				return -1;

			int page_id = (int)(last_id / Consts.NUM_VOTES_IN_A_PAGE);
			return page_id;
		}
		public static IEnumerable<TaggedVoteInfo> GetTagScrollPage(string tag, int page_id)
		{
			string partition_key = Consts.TAG_NAME_KEY_PREFIX + tag.ToLowerInvariant() + Consts.NAME_CONCAT +
								Consts.PAGE_KEY_PREFIX + page_id.ToString();

			IEnumerable<TaggedVoteInfo> list = Warehouse.TagScrollsPond.Get(partition_key);
			return list;
		}
	}
}