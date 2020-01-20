using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Collections.Concurrent;
using System.Threading;

namespace HappyVoting8
{
	public static class UserStore
	{
		public static bool Register(string user_name, string pwd_token)
		{
			try
			{
				DynamicTableEntity entity = new DynamicTableEntity(user_name, "na");

				entity["pwd_token"] = new EntityProperty(pwd_token);
				entity["create_time"] = new EntityProperty(DateTime.Now);

				Warehouse.UsersTable.Execute(TableOperation.Insert(entity));

				return true;
			}
			catch (StorageException)
			{
				// "遠端伺服器傳回一個錯誤: (409) 衝突。" when inserting duplicated entities.
				// "遠端伺服器傳回一個錯誤: (412) Precondition Failed。" when entity is modified in between.
				return false;
			}
		}
		public static bool? Login(string user_name, string temp_key, string pwd_hash)
		{
			TableResult result = Warehouse.UsersTable.Execute(TableOperation.Retrieve(user_name, "na"));
			DynamicTableEntity entity = (DynamicTableEntity)result.Result;

			if (entity == null)
				return null;

			string pwd_token = entity["pwd_token"].StringValue;
			string hash = Util.ToDumpStringCompact(Util.ToMd5(pwd_token + temp_key));

			return pwd_hash.Equals(hash, StringComparison.OrdinalIgnoreCase);
		}
	}
	public static class OneTimeKeyCenter
	{
		private static ConcurrentDictionary<string, DateTime> dict = new ConcurrentDictionary<string, DateTime>();
		private static Timer timer;

		static OneTimeKeyCenter()
		{
			timer = new Timer(purge, null, 2 * 60 * 1000/*ms*/, 2 * 60 * 1000/*ms*/);
		}
		public static string Create()
		{
			while (true)
			{
				string key = Util.RandomAlphaNumericString(8);

				if (dict.TryAdd(key, DateTime.Now))
					return key;
			}
		}
		public static bool Use(string key)
		{
			DateTime dt;
			bool suc = dict.TryRemove(key, out dt);

			return suc;
		}
		private static void purge(object state)
		{
			DateTime dt = DateTime.Now.AddMinutes(-3);

			foreach (KeyValuePair<string, DateTime> pair in dict)
			{
				DateTime dummy;
				if (pair.Value < dt)
					dict.TryRemove(pair.Key, out dummy);
			}
		}
	}
	public class UserSession
	{
		public string UserName;
		public string SessionKey;
		public DateTime LastActiveTime;

		public UserSession(string user_name)
		{
			this.UserName = user_name;
			this.SessionKey = Util.RandomAlphaNumericString(6);
			this.LastActiveTime = DateTime.Now;
		}
	}
	public static class UserCenter
	{
		private static ConcurrentDictionary<string, UserSession> dict = new ConcurrentDictionary<string, UserSession>();
		private static Timer timer;

		static UserCenter()
		{
			timer = new Timer(purge, null, 3 * 60 * 1000/*ms*/, 3 * 60 * 1000/*ms*/);
		}
		public static string Create(string user_name)
		{
			UserSession us = new UserSession(user_name);
			dict[us.SessionKey] = us;
			return us.SessionKey;
		}
		public static string Act(string session_key)
		{
			UserSession us;
			if (dict.TryGetValue(session_key, out us))
			{
				us.LastActiveTime = DateTime.Now;
				return us.UserName;
			}
			return null;
		}
		private static void purge(object state)
		{
			DateTime dt = DateTime.Now.AddMinutes(-30);
			UserSession dummy;

			foreach (KeyValuePair<string, UserSession> pair in dict)
			{
				if (pair.Value.LastActiveTime < dt)
					dict.TryRemove(pair.Key, out dummy);
			}
		}
	}
}