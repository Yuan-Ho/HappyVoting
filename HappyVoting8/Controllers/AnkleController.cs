using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Diagnostics;
using System.Web;

namespace HappyVoting8
{
	public enum ResultCode
	{
		Success = 0,
		InvalidLoginToken = -1,
		InvalidUserName = -2,
		WrongPassword = -3,
		InvalidTempKey = -4,
		UserNameOccupied = -5,
		UserDoesNotExist = -6,
		InvalidSession = -7,
	}
	public class AnkleController : ApiController
	{
		public class RegisterData
		{
			public string user_name { get; set; }
			public string pwd_token { get; set; }
		}
		[HttpPost]
		public object Register(RegisterData data)
		{
			Trace.TraceInformation("Register. user_name={0}, pwd_token={1}.", data.user_name, data.pwd_token);

			bool is_lau = data.user_name[0] == '_';

			string check_name = is_lau ? data.user_name.Substring(1) : data.user_name;

			if (check_name.Length < 2 ||
				check_name.Length > 100 ||
				!Util.WithinCharSetUserName(check_name))
				return new { code = ResultCode.InvalidUserName };

			if (!UserStore.Register(data.user_name, data.pwd_token))
				return new { code = ResultCode.UserNameOccupied };

			string session_key = UserCenter.Create(data.user_name);

			return new
			{
				code = ResultCode.Success,
				session_key = session_key
			};
		}
		[HttpPost]
		public object GetTempKey()
		{
			string key = OneTimeKeyCenter.Create();
			return new
			{
				code = ResultCode.Success,
				temp_key = key
			};
		}
		public class LoginData
		{
			public string user_name { get; set; }
			public string temp_key { get; set; }
			public string pwd_hash { get; set; }
		}
		[HttpPost]
		public object Login(LoginData data)
		{
			if (!OneTimeKeyCenter.Use(data.temp_key))
				return new { code = ResultCode.InvalidTempKey };

			bool? result = UserStore.Login(data.user_name, data.temp_key, data.pwd_hash);
			if (result == null)
				return new { code = ResultCode.UserDoesNotExist };
			else if (result == false)
				return new { code = ResultCode.WrongPassword };

			string session_key = UserCenter.Create(data.user_name);

			return new
			{
				code = ResultCode.Success,
				session_key = session_key
			};
		}
		public class TestData
		{
			public string session_key { get; set; }
		}
		[HttpPost]
		public object Test(TestData data)
		{
			if (UserCenter.Act(data.session_key) == null)
				return new { code = ResultCode.InvalidSession };

			return new { code = ResultCode.Success };
		}
		public class CreateVoteData
		{
			public string session_key { get; set; }
		}
		[HttpPost]
		public object CreateVote(CreateVoteData data)
		{
			string user_name = UserCenter.Act(data.session_key);
			if (user_name == null)
				return new { code = ResultCode.InvalidSession };

			string vote_id = PaperStore.CreatePaper(user_name);

			return new
			{
				code = ResultCode.Success,
				vote_id = vote_id
			};
		}
		public class AddActionData
		{
			public string session_key { get; set; }
			public ActionData[] actions { get; set; }
			public string vote_id { get; set; }
		}
		[HttpPost]
		public object AddActions(AddActionData data)
		{
			string user_name = UserCenter.Act(data.session_key);
			if (user_name == null)
				return new { code = ResultCode.InvalidSession };

			// Prevent XSS attack.
			//for (int i = 0; i < data.actions.Length; i++)
			//	data.actions[i].value = HttpContext.Current.Server.HtmlEncode(data.actions[i].value);
			// Encode at output instead of input.

			string[] hps = PaperStore.AddActions(data.vote_id, user_name, data.actions);

			VoteBroadcaster.HubContext.Clients.Group(data.vote_id).onNewActions(hps);

			return new
			{
				code = ResultCode.Success,
			};
		}
		public class AddTallyData
		{
			public string session_key { get; set; }
			public TallyData[] tallies { get; set; }
			public string vote_id { get; set; }
			public string sbjt_mat { get; set; }
		}
		[HttpPost]
		public object AddTallies(AddTallyData data)
		{
			string user_name = UserCenter.Act(data.session_key);
			if (user_name == null)
				return new { code = ResultCode.InvalidSession };

			string client_ip = Util.ClientIp();

			if (RecordStore.CheckUser(data.vote_id, data.sbjt_mat, user_name))
				return new { err_msg = "您(" + user_name + ")已經投過票了。" };
#if !DEBUG
			if (RecordStore.CheckIp(data.vote_id, data.sbjt_mat, client_ip))
				return new { err_msg = "您的IP已經投過票了。" };
				// return new { err_msg = "您的IP位址(" + client_ip + ")已經投過票了。" };
#endif
			TallyStore.AddTallies(data.vote_id, user_name, data.sbjt_mat, data.tallies);
			RecordStore.AddRecord(data.vote_id, data.sbjt_mat, user_name, client_ip);

			return new
			{
				code = ResultCode.Success,
			};
		}
		[HttpPost]
		public object ResetTallies(AddTallyData data)
		{
			string user_name = UserCenter.Act(data.session_key);
			if (user_name == null)
				return new { code = ResultCode.InvalidSession };

			RecordStore.ResetRecord(data.vote_id, data.sbjt_mat, user_name);
			TallyStore.ResetTallies(data.vote_id, user_name, data.sbjt_mat);

			return new
			{
				code = ResultCode.Success,
			};
		}
		public class TaggedVoteData
		{
			public string session_key { get; set; }
			public string[] tags { get; set; }
			public string vote_id { get; set; }
			public string title { get; set; }
		}
		[HttpPost]
		public object AddVoteToTags(TaggedVoteData data)
		{
			string user_name = UserCenter.Act(data.session_key);
			if (user_name == null)
				return new { code = ResultCode.InvalidSession };

			foreach (string tag in data.tags)
				TagScrollStore.AddVote(data.vote_id, tag, data.title, user_name);

			var data2 = new
						{
							vote_id = data.vote_id,
							title = data.title,
							time = DateTimeOffset.UtcNow,
							who = user_name,
							tags = data.tags
						};

			VoteBroadcaster.HubContext.Clients.All.onNewTaggedVote(data2);

			return new
			{
				code = ResultCode.Success,
			};
		}
		[HttpPost]
		public object Picture()
		{
			HttpFileCollection files = HttpContext.Current.Request.Files;

			string uri = PictureStore.ProcessUploadFiles(files);

			return new
			{
				code = ResultCode.Success,
				uri = uri
			};
		}
	}
}
