﻿using System.Web.Optimization;

namespace Thinkage.MainBoss.WebAccess {
	public static class BundleConfig {
		// For more information on Bundling, visit http://go.microsoft.com/fwlink/?LinkId=254725
		public static void RegisterBundles(BundleCollection bundles) {
			bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
						"~/Scripts/jquery-{version}.js"));
			bundles.Add(new ScriptBundle("~/bundles/tablesorter").Include(
						"~/Scripts/tablesorter/jquery.tablesorter.combined.*"));
			bundles.Add(new StyleBundle("~/Content/css").Include("~/Content/Site.css"));
			bundles.Add(new ScriptBundle("~/bundles/jqueryui").Include(
						"~/Scripts/jquery-ui-{version}.js",
						"~/Scripts/jquery-ui-timepicker-addon.js",
						"~/Scripts/jquery-ui.unobtrusive-{version}.js"
						));
			bundles.Add(new StyleBundle("~/Content/themes/base/css").Include(
						"~/Content/themes/base/core.css",
						"~/Content/themes/base/resizable.css",
						"~/Content/themes/base/selectable.css",
						"~/Content/themes/base/accordion.css",
						"~/Content/themes/base/autocomplete.css",
						"~/Content/themes/base/dialog.css",
						"~/Content/themes/base/slider.css",
						"~/Content/themes/base/tabs.css",
						"~/Content/themes/base/datepicker.css",
						"~/Content/themes/base/progressbar.css",
						"~/Content/themes/base/theme.css",
						"~/Content/jquery-ui-timepicker-addon.css"
						));

#if NOTUSED

			bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
						"~/Scripts/jquery.unobtrusive*",
						"~/Scripts/jquery.validate*"));

			// Use the development version of Modernizr to develop with and learn from. Then, when you're
			// ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
			bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
						"~/Scripts/modernizr-*"));

			bundles.Add(new StyleBundle("~/Content/css").Include("~/Content/site.css"));

#endif
		}
	}
}