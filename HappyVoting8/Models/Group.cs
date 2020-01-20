using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HappyVoting8
{
	public class GroupInfo
	{
		public DateTime LastActiveTime;
		public string RoomName;

		public GroupInfo(string room_name)
		{
			Refresh(room_name);
		}
		public void Refresh(string room_name)
		{
			this.RoomName = room_name;
			this.Refresh();
		}
		public void Refresh()
		{
			this.LastActiveTime = DateTime.Now;
		}
		public bool Expired()
		{
			return DateTime.Now > this.LastActiveTime.AddMinutes(5);
		}
	}
	public static class GroupManager
	{
		private static ConcurrentDictionary<string, GroupInfo> groupDict = new ConcurrentDictionary<string, GroupInfo>();
		private static DateTime lastPurgeTime;

		public static void EnterRoom(IGroupManager groups, string conn_id, string room_name)
		{
			purge(groups);

			groupDict.AddOrUpdate(conn_id, c_id =>
									{
										groups.Add(c_id, room_name);
										return new GroupInfo(room_name);
									}, (c_id, info) =>
									{
										if (info.RoomName != room_name)
										{
											groups.Remove(c_id, info.RoomName);

											groups.Add(c_id, room_name);
										}
										info.Refresh(room_name);

										return info;
									});
		}
		public static string GetRoomName(string conn_id)
		{
			GroupInfo info;

			if (groupDict.TryGetValue(conn_id, out info))
				return info.RoomName;
			return null;
		}
		private static void purge(IGroupManager groups)
		{
			DateTime now = DateTime.Now;
			if (now <= lastPurgeTime.AddMinutes(3))
				return;
			lastPurgeTime = now;

			GroupInfo dummy;

			foreach (KeyValuePair<string, GroupInfo> pair in groupDict)
			{
				if (pair.Value.Expired())
				{
					if (groupDict.TryRemove(pair.Key, out dummy))
						groups.Remove(pair.Key, pair.Value.RoomName);
				}
			}
		}
	}
}
