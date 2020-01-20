using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HappyVoting8
{
	public class HomeController : Controller
	{
		[OutputCache(Duration = Consts.SHORT_CACHE_SECONDS)]
		[HttpGet]
		public ActionResult Index()
		{
			return View("landing");
			//return File("/Views/landing.html", "text/html");
		}
		[HttpGet]
		public ActionResult Create(string language)
		{
			return File("/Views/create." + language + ".html", "text/html");
		}
		[OutputCache(Duration = Consts.SHORT_CACHE_SECONDS)]
		[HttpGet]
		public ActionResult About()
		{
			return View("About");
		}
		//[OutputCache(Duration = Consts.DEFAULT_CACHE_SECONDS)]
		[HttpGet]
		public ActionResult Test()
		{
			return View("Test");
		}
		[OutputCache(Duration = Consts.SHORT_CACHE_SECONDS)]
		[HttpGet]
		public ActionResult VotePage(string vote_id)
		{
			// Todo: non-existent vote.
			PaperRoll roll = PaperPond.Get(vote_id);
			
			string title = roll.GetSetting("headings-title");
			if (title == null)
				title = "新投票";

			ViewBag.Title = title/* + " | www.fotous.net"*/;
			// Razor will do html encode to prevent xss attack.

			return View("VotePage");
			//return File("/Views/vote.html", "text/html");
		}
		[ChildActionOnly]
		//[OutputCache(Duration = Consts.SHORT_CACHE_SECONDS)]
		[HttpGet]
		public ActionResult PreloadPaper(string vote_id)
		{
			return new PreloadPaperResult(vote_id);
		}
		[ChildActionOnly]
		//[OutputCache(Duration = Consts.SHORT_CACHE_SECONDS)]
		[HttpGet]
		public ActionResult PreloadTotals(string vote_id)
		{
			return new PreloadTotalsResult(vote_id);
		}
		[ChildActionOnly]
		//[OutputCache(Duration = Consts.SHORT_CACHE_SECONDS)]
		[HttpGet]
		public ActionResult PreloadTagScroll(string tag)
		{
			return new PreloadTagScrollResult(tag);
		}
		[OutputCache(Duration = Consts.SHORT_CACHE_SECONDS)]
		[HttpGet]
		public ActionResult GetTallies(string session_key, string vote_id)
		{
			string user_name = UserCenter.Act(session_key);
			if (user_name == null)
				return Json(new { code = ResultCode.InvalidSession }, JsonRequestBehavior.AllowGet);

			IEnumerable<TallyInfo> tallies = TallyStore.GetTallies(vote_id, user_name);

			return Json(new
						{
							code = ResultCode.Success,
							tallies = tallies,
						}, JsonRequestBehavior.AllowGet);
		}
		[OutputCache(Duration = Consts.SHORT_CACHE_SECONDS)]
		[HttpGet]
		public ActionResult GetTotals(string vote_id)
		{
			IEnumerable<TallyInfo> totals = TotalStore.GetTotals(vote_id);

			return Json(new
						{
							code = ResultCode.Success,
							totals = totals,
						}, JsonRequestBehavior.AllowGet);
		}
	}
}
