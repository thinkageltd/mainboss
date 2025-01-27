﻿using System;
using System.Web.Mvc;
using System.Web.Security;
using Thinkage.MainBoss.WebAccess.Models;
using System.DirectoryServices.AccountManagement;

namespace Thinkage.MainBoss.WebAccess.Controllers {
	[HandleError]
	public class HomeController : ErrorHandlingController { // Not based on FormCollectionWithType collection as we are not using the database for storage
		#region Logout
		[HttpGet]
		[MainBossAuthorization(MainBossAuthorized.Anyone)]
		public ActionResult Logout() {
			var application = (Thinkage.MainBoss.WebAccess.MainBossWebAccessApplication)Thinkage.Libraries.Application.Instance;
			if (application != null && application.IsValid && application.IsMainBossUser) {
				Session.Abandon();
				application.IsLoggedOut = true;
			}
			return View("Logout");
		}
		#endregion
		#region WebAccess
		[HttpGet]
		[MainBossAuthorization(MainBossAuthorized.MainBossUser)]
		public ActionResult WebAccess() {
			if (!((Thinkage.MainBoss.WebAccess.MainBossWebAccessApplication)Thinkage.Libraries.Application.Instance).HasWebAccessLicense)
				throw new NoPermissionException(KB.K("A Web Access license is required to work with MainBoss.").Translate());
			return View("WebAccess");
		}
		#endregion
		#region Index (GET)
		[HttpGet]
		[MainBossAuthorization(MainBossAuthorized.Anyone)]
		public ActionResult Index() {
			var application = (Thinkage.MainBoss.WebAccess.MainBossWebAccessApplication)Thinkage.Libraries.Application.Instance;
			if (!application.IsValid) {
				try {
					application.SetupApplication();
					application.IsLoggedOut = false; // as far as we are concerned, we have been authenticated by now.
				}
				catch (System.Exception ex) {
					Session.Add("Exception", ex);
					return RedirectToAction("UnHandledException", "Error");
				}
			}
			if (application.IsMainBossUser && application.HasWebAccessLicense && application.HasWebRequestsLicense == false) {
				return RedirectToAction("WebAccess", "Home");
			}
			var model = new HomeModel();
			try {
				string emailAddress = null;
				if (User.Identity.IsAuthenticated) {
					try {
						var domain = new PrincipalContext(ContextType.Domain);
						var currentUser = UserPrincipal.FindByIdentity(domain, User.Identity.Name);
						emailAddress = currentUser.EmailAddress;
					}
					catch (System.Exception) { } // if we cann't read active directory, he will have to type the email address in
				}
				else if (emailAddress == null)
					emailAddress = Cookies.GetRequestorEmail(Request);
				if (emailAddress != null)
					model.EmailAddress = emailAddress;
			}
			catch (System.Exception gex) {
				Session.Add("Exception", gex);
				return RedirectToAction("UnHandledException", "Error");
			}
			return View(model);
		}
		#endregion
		#region Index (POST)
		[ValidateAntiForgeryToken]
		[MainBossAuthorization(MainBossAuthorized.Requestor)]
		[AcceptVerbs(HttpVerbs.Post)]
		public ActionResult Index(FormCollection formValues) {
			var model = new HomeModel();
			try {
				UpdateModel<HomeModel>(model);
				if (!model.IsValid)
					throw new Exception();
				Cookies.CreateRequestorEmail(Response, model.EmailAddress);
				if (formValues.GetValue("SeeRequests") != null)
					return RedirectToAction("RequestorList", "Request", new {
						model.RequestorID,
						ResultMessage = ""
					});
				else
					return RedirectToAction("Create", "Request", new {
						model.RequestorID
					});
			}
			catch {
				CollectRuleViolations(model);
				return View(model);
			}
		}
		#endregion
		#region About
		[HttpGet]
		[MainBossAuthorization(MainBossAuthorized.Anyone)]
		public ActionResult About() {
			Version v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
			return Redirect(Thinkage.Libraries.Strings.IFormat("http://mainboss.com/info/aboutMainBossWebAccess.htm?version={0}.{1}", v.Major, v.Minor));
		}
		#endregion
		#region NoPermission
		[HttpGet]
		public ActionResult NoPermission(NoPermissionException exception) {
			return View("NoPermission", exception);
		}
		#endregion
	}
}
