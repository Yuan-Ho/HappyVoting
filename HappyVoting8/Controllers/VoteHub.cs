using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;

namespace HappyVoting8
{
	public class VoteBroadcaster
	{
		private readonly static Lazy<VoteBroadcaster> _instance = new Lazy<VoteBroadcaster>(() => new VoteBroadcaster());
		private readonly IHubContext _hubContext;

		public static VoteBroadcaster Instance
		{
			get { return _instance.Value; }
		}
		public static IHubContext HubContext
		{
			get { return Instance._hubContext; }
		}
		public VoteBroadcaster()
		{
			_hubContext = GlobalHost.ConnectionManager.GetHubContext<VoteHub>();
		}
	}
	public class VoteHub : Hub
	{
		public void EnterVoteRoom(string vote_id)
		{
			GroupManager.EnterRoom(Groups, Context.ConnectionId, vote_id);
			//string room_name = GroupManager.GetRoomName(Context.ConnectionId);
		}
	}
}