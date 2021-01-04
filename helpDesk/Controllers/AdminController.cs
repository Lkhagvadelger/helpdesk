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
    [Authorize(Roles = "Superadmin")]
    public class AdminController : Controller
    {
        helpDescEntities db = new helpDescEntities();
        public AdminController()
            : this(new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(new ApplicationDbContext())))
        {
        }
        public AdminController(UserManager<ApplicationUser> userManager)
        {
            UserManager = userManager;
        }
        public UserManager<ApplicationUser> UserManager { get; private set; }
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult admins()
        {
            ViewBag.orgUserList = db.AspNetUsers.ToList();
            ViewBag.role_id = new SelectList(db.AspNetRoles.ToList(), "id", "Name");
            return View(db.Organisations.ToList());
        }
        public ActionResult RoleContainer(string userid)
        {
            ViewBag.roles = new SelectList(db.AspNetRoles.ToList(), "Name", "Name");
            string userRoles = "<select id='userCurrentRole' name='userCurrentRole' class='form-control'>";
            foreach (var item in UserManager.GetRoles(userid))
                userRoles += String.Format("<option value='{0}'>{0}</option>", item);
            userRoles += "</select>";
            ViewBag.userRoles = userRoles;
            ViewBag.userid = userid;
            ViewBag.username = db.AspNetUsers.Find(userid).UserName;
            return PartialView("RoleContainer");
        }
        public string ShowRoles(string userid)
        {
            string roles = "";
            foreach (var item in UserManager.GetRoles(userid))
                roles += String.Format("<span class='label label-info'>{0}</span> ", item);

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
        public ActionResult Organisation()
        {
            ViewBag.orgUserList = db.AspNetUsers.ToList();
            return View(db.Organisations.Where(r => r.id != 1).ToList());
        }
        public ActionResult OrganisationAdd()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult OrganisationAdd([Bind(Include = "id,org_name,service_type")] Organisation organisation)
        {
            if (db.Organisations.Any(r => r.org_name == organisation.org_name))
                return View(organisation);
            if (ModelState.IsValid && organisation.org_name != null && organisation.org_name != "")
            {
                db.Organisations.Add(organisation);
                db.SaveChanges();
                return RedirectToAction("Organisation");
            }

            return View(organisation);
        }
        public ActionResult OrganisationEdit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Organisation organisation = db.Organisations.Find(id);
            if (organisation == null)
            {
                return HttpNotFound();
            }
            return View(organisation);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult OrganisationEdit([Bind(Include = "id,org_name,service_type")] Organisation organisation)
        {
            if (ModelState.IsValid)
            {
                db.Entry(organisation).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Organisation");
            }
            return View(organisation);
        }
        public ActionResult Register()
        {
            helpDescEntities db = new helpDescEntities();
            ViewBag.organisation = new SelectList(db.Organisations.ToList(), "id", "org_name");
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model, string role = "")
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser() { UserName = model.UserName, organisation = model.organisation };
                var result = await UserManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    string id = new helpDescEntities().AspNetUsers.Single(r => r.UserName == model.UserName).Id;
                    if (role == "")
                        UserManager.AddToRole(id, "Subadmin");
                    else UserManager.AddToRole(id, role);
                    return RedirectToAction("Organisation", "Admin");
                }
                else
                {
                    AddErrors(result);
                }
            }
            helpDescEntities db = new helpDescEntities();
            ViewBag.organisation = new SelectList(db.Organisations.ToList(), "id", "org_name");
            // If we got this far, something failed, redisplay form
            return View(model);
        }
        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }
        public ActionResult System(int? id)
        {
            if (id == null)
            {
                ViewBag.list = true;
                return View();
            }
            else
            {
                ViewBag.list = false;
                ViewBag.id = id;
                return View();
            }
        }
        public ActionResult SystemEdit(int id)
        {
            var item = db.SystemModules.Find(id);
            if (item.is_module == true)
                ViewBag.parent = item.SystemModule2.name;

            return View(item);
        }
        [HttpPost]
        public ActionResult SystemEdit(int id, string name)
        {
            var item = db.SystemModules.Find(id);
            item.name = name;
            db.SaveChanges();
            return RedirectToAction("System");
        }
        public ActionResult SystemList()
        {
            var list = db.Organisations.Include(r => r.SystemModules).ToList();
            return PartialView(list);
        }
        [HttpPost]
        public void SystemModuleStatus(int id, bool status)
        {
            var item = db.SystemModules.Find(id);
            item.is_enabled = status;
            db.SaveChanges();
        }
        public ActionResult SystemAdd()
        {
            ViewBag.organisation_id = new SelectList(db.Organisations, "id", "org_name");
            ViewBag.parent_id = new SelectList(db.SystemModules.Where(r => r.is_module == false).Where(r => r.is_enabled == false), "id", "name");
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SystemAdd([Bind(Include = "id,organisation_id,parent_id,name,is_module,is_enabled")] SystemModule systemmodule)
        {
            if (ModelState.IsValid && systemmodule.name != null)
            {
                if (systemmodule.is_module == false)
                    systemmodule.parent_id = null;
                db.SystemModules.Add(systemmodule);
                db.SaveChanges();
                return RedirectToAction("System", "Admin");
            }

            ViewBag.organisation_id = new SelectList(db.Organisations, "id", "org_name", systemmodule.organisation_id);
            ViewBag.parent_id = new SelectList(db.SystemModules, "id", "name", systemmodule.parent_id);
            return View(systemmodule);
        }
        public ActionResult OrganisationSystemList(int id)
        {
            ViewBag.parent_id = new SelectList(db.SystemModules.Where(r => r.organisation_id == id).Where(r => r.is_module == false), "id", "name");
            return PartialView();
        }

    }
}