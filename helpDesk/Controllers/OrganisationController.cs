using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using helpDesk.Entity;
using helpDesk.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace helpDesk.Controllers
{
    [Authorize(Roles = "Subadmin")]
    public class OrganisationController : Controller
    {
        helpDescEntities db = new helpDescEntities();
        public OrganisationController()
            : this(new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(new ApplicationDbContext())))
        {
        }
        public OrganisationController(UserManager<ApplicationUser> userManager)
        {
            UserManager = userManager;
        }
        public UserManager<ApplicationUser> UserManager { get; private set; }
        public ActionResult Index()
        {
            int org_id = (int)db.AspNetUsers.Single(r => r.UserName == User.Identity.Name).organisation;
            return View(db.AspNetUsers.Where(r => r.organisation == org_id).ToList());
        }
        [Authorize(Roles = "Subadmin")]
        public ActionResult Register()
        {
            var org = db.AspNetUsers.Single(e => e.UserName == User.Identity.Name).organisation;
            ViewBag.organisation = new SelectList(db.Organisations.Where(r => r.id == org).ToList(), "id", "org_name", org);
            return View();
        }
        //
        // POST: /Account/Register
        [HttpPost]
        [Authorize(Roles = "Subadmin")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model, int role_id)
        {
            int org = (int)db.AspNetUsers.Single(e => e.UserName == User.Identity.Name).organisation;

            if (ModelState.IsValid && (role_id == 1 || role_id == 2))
            {
                var user = new ApplicationUser() { UserName = model.UserName, organisation = org };
                var result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    string id = new helpDescEntities().AspNetUsers.Single(r => r.UserName == model.UserName).Id;
                    if (role_id == 1)
                        UserManager.AddToRole(id, "Moderator");
                    if (role_id == 2)
                        UserManager.AddToRole(id, "QAAnswer");
                    return RedirectToAction("Index", "Organisation");
                }
                else
                {
                    AddErrors(result);
                }
            }
            ViewBag.organisation = new SelectList(db.Organisations.Where(r => r.id == org).ToList(), "id", "org_name", org);
            return View(model);
        }
        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }
        public static string OrganisationName(string name)
        {
            helpDescEntities db = new helpDescEntities();
            string org_name = db.Organisations.Find(db.AspNetUsers.Single(r => r.UserName == name).organisation).org_name;
            return org_name;
        }
        public static string OrganisationServiceType(string name)
        {
            helpDescEntities db = new helpDescEntities();
            return db.Organisations.Find(db.AspNetUsers.Single(r => r.UserName == name).organisation).service_type;
        }
        public ActionResult Manage(ManageMessageId? message, Guid userid)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.ChangePasswordSuccess ? "Нууц үг солигдлоо."
                : message == ManageMessageId.SetPasswordSuccess ? "Нууц үг хадгалагдлаа."
                : message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed."
                : message == ManageMessageId.Error ? "Алдаа гарлаа."
                : "";
            ViewBag.ReturnUrl = Url.Action("Manage");
            ViewBag.userName = db.AspNetUsers.Find(userid.ToString()).UserName;
            ViewBag.userid = userid;
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Manage(ManageUserViewModel model, string userid)
        {
            ViewBag.userid = userid;
            ModelState state = ModelState["OldPassword"];
            if (state != null)
            {
                state.Errors.Clear();
            }
            if (ModelState.IsValid)
            {
                UserManager.RemovePassword(userid.ToString());
                IdentityResult result = await UserManager.AddPasswordAsync(userid.ToString(), model.NewPassword);
                if (result.Succeeded)
                {

                    return RedirectToAction("Manage", new { userid = userid, Message = ManageMessageId.SetPasswordSuccess });
                }
                else
                {
                    AddErrors(result);
                }
            }
            return View(model);
        }
        public enum ManageMessageId
        {
            ChangePasswordSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            Error
        }
        public string ShowRoles(string userid)
        {
            string roles = "";
            foreach (var item in UserManager.GetRoles(userid))
                roles += "<span class='label label-info'>" + item + "</span>" + " ";
            return roles;
        }
        public ActionResult RoleRemove(string userId, string role)
        {
            UserManager.RemoveFromRole(userId, role);
            return RedirectToAction("RoleContainer", new { userid = userId });
        }
        public ActionResult RoleAdd(string userId, string role)
        {
            UserManager.AddToRole(userId, role);
            return RedirectToAction("RoleContainer", new { userid = userId });
        }
        public ActionResult RoleContainer(string userid)
        {
            ViewBag.roles = new SelectList(db.AspNetRoles.Where(r => r.Id != "1" && r.Id != "4" && r.Id != "5" && r.Id != "7").ToList(), "Name", "Name");
            string userRoles = "<select id='userCurrentRole' name='userCurrentRole' class='form-control'>";
            foreach (var item in UserManager.GetRoles(userid))
                userRoles += "<option value='" + item + "'>" + item + "</option>";
            userRoles += "</select>";
            ViewBag.userRoles = userRoles;
            ViewBag.userid = userid;
            ViewBag.username = db.AspNetUsers.Find(userid).UserName;
            return PartialView("RoleContainer");
        }
        public static int NewComplainCount()
        {
            helpDescEntities db = new helpDescEntities();
            return db.Complains.Where(r => r.status_id == 1).Count();
        }
        public static int isInCompanyUser(int org_id, string UserName)
        {
            helpDescEntities db = new helpDescEntities();
            if (db.AspNetUsers.Where(r => r.organisation == org_id).Where(r=>r.UserName == UserName).Count() > 0)
            {
                return 1;
            }
            else return 0;
        }
        public static int NewQACount(string UserName)
        {
            helpDescEntities db = new helpDescEntities();
            var org_id = db.AspNetUsers.Single(r => r.UserName == UserName).organisation;
            return db.QAs.Where(r => r.org_id == org_id).Where(r => r.answer_date == null || r.answer == null || r.answer == "").Count();
        }
    }
}