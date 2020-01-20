using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace HappyVoting8
{
	public class RouteConfig
	{
		public static void RegisterRoutes(RouteCollection routes)
		{
			routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

			routes.MapRoute(
				name: "Default",
				url: "",
				defaults: new { controller = "Home", action = "Index" }
			);
			routes.MapRoute(
				name: "GetApi",
				url: "gapi/{action}",
				defaults: new { controller = "Home" }
			);
			routes.MapRoute(
				name: "Vote pages",
				url: Consts.VOTE_PAGE_PATH.TrimStart('/') + "{vote_id}",
				defaults: new { controller = "Home", action = "VotePage" }
			);
			routes.MapRoute(
				name: "Misc pages",
				url: "p/{action}",
				defaults: new { controller = "Home" }
			);
			routes.MapRoute(
				name: "Child actions",
				url: "{action}",
				defaults: new { controller = "Home" }
			);
		}
	}
}
