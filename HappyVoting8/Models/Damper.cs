using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading;

namespace HappyVoting8
{
	public static class TotalsDamper
	{
		private static Timer timer;
		private static Dictionary<string, List<TallyInfo>> dict = new Dictionary<string, List<TallyInfo>>();

		static TotalsDamper()
		{
			timer = new Timer(broadcast, null, 1000/*ms*/, 1000/*ms*/);
		}
		private static void broadcast(object state)
		{
			lock (dict)
			{
				foreach (KeyValuePair<string, List<TallyInfo>> pair in dict)
				{
					if (pair.Value.Count != 0)
					{
						VoteBroadcaster.HubContext.Clients.Group(pair.Key).onUpdateTotals(pair.Value);
						pair.Value.Clear();
					}
				}
			}
		}
		public static void Update(string vote_id, TallyInfo tally_info)
		{
			lock (dict)
			{
				List<TallyInfo> list;

				if (!dict.TryGetValue(vote_id, out list))
				{
					list = new List<TallyInfo>();
					dict.Add(vote_id, list);
				}
				list.Add(tally_info);
			}
		}
	}
}