using System.Web;
using System.Web.Optimization;

namespace HappyVoting8
{
	public class BundleConfig
	{
		// 如需 Bundling 的詳細資訊，請造訪 http://go.microsoft.com/fwlink/?LinkId=254725
		public static void RegisterBundles(BundleCollection bundles)
		{
			// The path of css bundles should match that of css files so that relative paths of image files are correct.
			//bundles.Add(new StyleBundle("~/bundles/landing/css").Include(
			// For bootstrap.css to load glyphicons-halflings-regular font.
			bundles.Add(new StyleBundle("~/assets/bootstrap/css/landing").Include(
						"~/assets/bootstrap/css/bootstrap.min.css",
						"~/assets/bootstrap/css/bootstrap-theme.min.css",
						"~/assets/js/vegas/jquery.vegas.min.css",
						"~/assets/js/owl-carousel/owl.carousel.css",
						"~/assets/js/owl-carousel/owl.theme.css",
						"~/assets/js/owl-carousel/owl.transitions.css",
						"~/assets/js/wow/animate.css",
						"~/assets/css/font-awesome/css/font-awesome.min.css",
						"~/assets/js/lightbox/css/lightbox.css"));

			bundles.Add(new ScriptBundle("~/bundles/landing/js").Include(
						"~/assets/js/jquery-1.11.2.min.js",
						"~/assets/bootstrap/js/bootstrap.min.js",
						"~/Scripts/knockout-3.4.0.js",
						"~/assets/global/plugins/moment.min.js",
						"~/assets/js/jquery.easing.1.3.js",
						"~/assets/js/vegas/jquery.vegas.min.js",
						"~/assets/js/detectmobilebrowser.js",
						"~/assets/js/jquery.scrollstop.min.js",
						"~/assets/js/owl-carousel/owl.carousel.min.js",
						"~/assets/js/lightbox/js/lightbox.min.js",
						"~/assets/js/wow/wow.min.js",
						"~/assets/js/jquery.fitvids.js",
						"~/assets/js/functions.js",
						"~/assets/js/initialise-functions.js",
						"~/Scripts/CryptoJS/rollups/md5.js",
						"~/Scripts/json.date-extensions-1.2.2/json.date-extensions.min.js",
						"~/Content/helper.js",
						"~/Content/ankle.js",
						"~/Content/sidebar.js",
						"~/Content/landing.js"));

			bundles.Add(new ScriptBundle("~/bundles/main/js").Include(
						// BEGIN CORE PLUGINS
						"~/assets/global/plugins/jquery.min.js",
						"~/assets/global/plugins/bootstrap/js/bootstrap.min.js",
						"~/assets/global/plugins/js.cookie.min.js",
						"~/assets/global/plugins/bootstrap-hover-dropdown/bootstrap-hover-dropdown.min.js",
						"~/assets/global/plugins/jquery-slimscroll/jquery.slimscroll.min.js",
						"~/assets/global/plugins/jquery.blockui.min.js",
						"~/assets/global/plugins/bootstrap-switch/js/bootstrap-switch.min.js",
						"~/Scripts/knockout-3.4.0.js",
						// END CORE PLUGINS
						// BEGIN PAGE LEVEL PLUGINS
						"~/assets/global/plugins/moment.min.js",
						"~/assets/global/plugins/bootstrap-daterangepicker/daterangepicker.min.js",
						"~/assets/global/plugins/amcharts/amcharts/amcharts.js",
						"~/assets/global/plugins/amcharts/amcharts/serial.js",
						"~/assets/global/plugins/amcharts/amcharts/pie.js",
						"~/assets/global/plugins/jquery.pulsate.min.js",
						//"~/assets/global/plugins/icheck/icheck.min.js",
						"~/assets/global/plugins/icheck/icheck_Tom.js",
						"~/assets/global/plugins/bootstrap-editable/bootstrap-editable/js/bootstrap-editable.min.js",
						"~/Scripts/CryptoJS/rollups/md5.js",
						"~/assets/global/plugins/bootbox/bootbox.min.js",
						"~/assets/global/plugins/autosize/autosize.min.js",
						"~/assets/global/plugins/bootstrap-maxlength/bootstrap-maxlength.min.js",
						"~/Scripts/json.date-extensions-1.2.2/json.date-extensions.min.js",
						"~/Scripts/autolink-js-1.0.2/autolink-min.js",
						// END PAGE LEVEL PLUGINS
						// BEGIN THEME GLOBAL SCRIPTS
						"~/assets/global/scripts/app.min.js",
						// END THEME GLOBAL SCRIPTS
						// BEGIN THEME LAYOUT SCRIPTS
						"~/assets/layouts/layout/scripts/layout.min.js",
						"~/assets/layouts/layout/scripts/demo.min.js",
						// END THEME LAYOUT SCRIPTS
						"~/Scripts/jquery.signalR-2.2.0.min.js",
						"~/Content/helper.js",
						"~/Content/ankle.js"));

			bundles.Add(new StyleBundle("~/bundles/main/css").Include(
						"~/assets/global/plugins/bootstrap/css/bootstrap.min.css",
						"~/assets/global/plugins/bootstrap-switch/css/bootstrap-switch.min.css"));

			// For icheck to get grey.png and red.png.
			bundles.Add(new StyleBundle("~/assets/global/plugins/icheck/skins/minimal/vote").Include(
						"~/assets/global/plugins/bootstrap-daterangepicker/daterangepicker.min.css",
						"~/assets/global/plugins/icheck/skins/minimal/_all.css",
						"~/assets/global/plugins/bootstrap-editable/bootstrap-editable/css/bootstrap-editable.css"));

			bundles.Add(new ScriptBundle("~/bundles/vote/js").Include(
						"~/Content/sidebar.js",
						"~/Content/vote.js",
						"~/Content/chart.js",
						"~/Content/tally.js",
						"~/Content/picture.js"));

			#if !DEBUG
			BundleTable.EnableOptimizations = true;
			#endif
		}
	}
}