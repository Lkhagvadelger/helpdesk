using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using helpDesk.Entity;

namespace helpDesc.Controllers
{
    [Authorize]
    public class ComplainController : Controller
    {
        private helpDescEntities db = new helpDescEntities();
        public ActionResult Index()
        {
            return View();
        }
        // Showing the details of complain and status can be changed by user based roles 
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Complain complain = db.Complains.Find(id);
            if (complain == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            if (!User.IsInRole("Admin"))
                if (complain.org_id != db.AspNetUsers.Single(r => r.UserName == User.Identity.Name).organisation)
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            if (complain == null)
            {
                return HttpNotFound();
            }
            ViewBag.statusList = new SelectList(db.Status.Where(r => r.is_client == "0").ToList(), "id", "status_value", 2);

            return View(complain);
        }
        public ActionResult DetailsStatus(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Complain complain = db.Complains.Find(id);
            if (complain == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            return PartialView("", db.ComplainStatus.Where(r => r.complain_id == id).ToList());
        }
        public ActionResult Create()
        {
            var org_id = db.AspNetUsers.Single(r => r.UserName == User.Identity.Name).organisation;
            ViewBag.complain_type_id = new SelectList(db.ComplainTypes, "id", "complainType1");
            ViewBag.system_id = new SelectList(db.SystemModules.Where(r => r.is_module == false).Where(r => r.organisation_id == org_id).Where(r => r.is_enabled == true), "id", "name");
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Subadmin, Moderator, Superadmin, Admin")]
        public ActionResult Create([Bind(Include = "id,complain_date,system_id,complain_text,complain_type_id,status_id")] Complain complain, HttpPostedFileBase file, int module_id = 0)
        {
            if (ModelState.IsValid && module_id != 0 && complain.complain_text != null && complain.complain_text != "")
            {
                complain.system_id = module_id;
                complain.inserted_date = DateTime.Now;
                complain.user_id = db.AspNetUsers.Single(r => r.UserName == User.Identity.Name).Id;
                complain.status_id = 5;
                complain.is_delete = false;
                Guid guid = Guid.NewGuid();
                if (UploadImages(file, guid) == true)
                {
                    complain.document_name = file.FileName;
                    complain.document_url = guid + file.FileName;
                }
                complain.org_id = db.AspNetUsers.Single(r => r.UserName == User.Identity.Name).organisation;
                db.Complains.Add(complain);
                db.SaveChanges();

                return RedirectToAction("Index");
            }

            ViewBag.user_id = new SelectList(db.AspNetUsers, "Id", "UserName", complain.user_id);
            ViewBag.complain_type_id = new SelectList(db.ComplainTypes, "id", "complainType1", complain.complain_type_id);
            ViewBag.org_id = new SelectList(db.Organisations, "id", "org_name", complain.org_id);
            ViewBag.status_id = new SelectList(db.Status, "id", "status_value", complain.status_id);
            ViewBag.system_id = new SelectList(db.SystemModules.Where(r => r.is_module == false), "id", "name", complain.system_id);
            return View(complain);
        }
        public ActionResult Edit(int? id)
        {

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Complain complain = db.Complains.Find(id);

            if (complain.org_id != db.AspNetUsers.Single(r => r.UserName == User.Identity.Name).organisation)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            ViewBag.currentUser = "";
            if (complain.user_id != db.AspNetUsers.Single(r => r.UserName == User.Identity.Name).Id)
                ViewBag.currentUser = complain.AspNetUser.UserName;

            if (complain.status_id != 5)
                return RedirectToAction("Details", new { id = id });
            if (!User.IsInRole("Admin"))
                if (complain.org_id != db.AspNetUsers.Single(r => r.UserName == User.Identity.Name).organisation)
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            if (complain == null)
            {
                return HttpNotFound();
            }
            ViewBag.module_id = new SelectList(db.SystemModules.Where(r => r.parent_id == complain.SystemModule.parent_id), "id", "name", complain.system_id);
            ViewBag.user_id = new SelectList(db.AspNetUsers, "Id", "UserName", complain.user_id);
            ViewBag.complain_type_id = new SelectList(db.ComplainTypes, "id", "complainType1", complain.complain_type_id);
            ViewBag.org_id = new SelectList(db.Organisations, "id", "org_name", complain.org_id);
            ViewBag.status_id = new SelectList(db.Status, "id", "status_value", complain.status_id);
            ViewBag.system_id = new SelectList(db.SystemModules.Where(r => r.is_module == false), "id", "name", complain.SystemModule.parent_id);
            return View(complain);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "id,complain_text,system_id,complain_type_id")] Complain complain, int module_id, HttpPostedFileBase file)
        {
            var item = db.Complains.Find(complain.id);
            if (item.org_id != db.AspNetUsers.Single(r => r.UserName == User.Identity.Name).organisation)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            if (item.user_id != db.AspNetUsers.Single(r => r.UserName == User.Identity.Name).Id)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            if (ModelState.IsValid)
            {
                Guid guid = Guid.NewGuid();
                if (file != null)
                    if (item.document_url != null)
                    {
                        var path = Path.Combine(Server.MapPath("/Content/file/"), complain.document_url);
                        FileInfo fileDelete = new FileInfo(path);
                        if (fileDelete.Exists)
                            fileDelete.Delete();
                        item.document_url = null;
                        item.document_name = null;
                    }

                if (UploadImages(file, guid) == true)
                {
                    item.document_name = file.FileName;
                    item.document_url = guid + file.FileName;
                }
                item.system_id = module_id;
                db.Entry(item).State = EntityState.Modified;
                db.SaveChanges();

                return RedirectToAction("Index");
            }

            ViewBag.user_id = new SelectList(db.AspNetUsers, "Id", "UserName", complain.user_id);
            ViewBag.complain_type_id = new SelectList(db.ComplainTypes, "id", "complainType1", complain.complain_type_id);
            ViewBag.org_id = new SelectList(db.Organisations, "id", "org_name", complain.org_id);
            ViewBag.status_id = new SelectList(db.Status, "id", "status_value", complain.status_id);
            ViewBag.system_id = new SelectList(db.SystemModules.Where(r => r.is_module == false), "id", "name", complain.system_id);


            return View(complain);
        }
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Complain complain = db.Complains.Find(id);
            if (complain == null)
            {
                return HttpNotFound();
            }
            return View(complain);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
        [HttpPost]
        public ActionResult ModuleList(int id)
        {
            var org_id = db.AspNetUsers.Single(r => r.UserName == User.Identity.Name).organisation;

            ViewBag.module_id = new SelectList(db.SystemModules.Where(r => r.parent_id == id).Where(r => r.organisation_id == org_id).Where(r => r.is_enabled == true), "id", "name");
            return PartialView("ModuleList");
        }
        public bool UploadImages(HttpPostedFileBase file, Guid guid)
        {
            if (file != null)
            {
                int isFileExist = 0;
                try
                {
                    if (file != null || file.ContentLength > 0)
                        isFileExist = 1;
                }
                catch { isFileExist = 0; return false; }
                // file iin extension shalgah
                if (isFileExist == 1)
                {
                    if (Path.GetExtension(file.FileName).ToLower() == ".jpg" || Path.GetExtension(file.FileName).ToLower() == ".png"
                        || Path.GetExtension(file.FileName).ToLower() == ".gif" || Path.GetExtension(file.FileName).ToLower() == ".jpeg"
                        || Path.GetExtension(file.FileName).ToLower() == ".docx" || Path.GetExtension(file.FileName).ToLower() == ".doc"
                        || Path.GetExtension(file.FileName).ToLower() == ".pdf"
                        )
                    {
                        isFileExist = 1;
                    }
                    else isFileExist = 0;
                }
                else return false;

                if (isFileExist == 1)
                {
                    var fileName = Path.GetFileName(file.FileName);
                    file.SaveAs(Server.MapPath("~/Content/file/" + guid + fileName));
                }
                else return false;
                return true;
            }
            return false;
        }
        public ActionResult ComplainList(string value = "", int page = 1)
        {
            int skip = (page - 1) * 15;
            int take = 15;
            if (value == "")
            {
                if (User.IsInRole("Superadmin"))
                {
                    var complains = db.Complains.Where(r => r.status_id != 5);
                    ViewBag.maxSize = complains.Count();
                    return PartialView("ComplainList", complains.OrderByDescending(r => r.id).Skip(skip).Take(take).ToList());
                }
                else
                {
                    int org_id = (int)db.AspNetUsers.Single(r => r.UserName == User.Identity.Name).organisation;
                    var complains = db.Complains.Where(r => r.org_id == org_id).Include(c => c.AspNetUser).Include(c => c.ComplainType)
                                    .Include(c => c.Organisation).Include(c => c.Status).Include(c => c.SystemModule);
                    ViewBag.maxSize = complains.Count();
                    return PartialView("ComplainList", complains.OrderByDescending(r => r.id).Skip(skip).Take(take).ToList());
                }
            }
            else
            {
                if (User.IsInRole("Superadmin"))
                {
                    var list = db.Complains.Where(r => r.status_id != 5).Where(r => r.Organisation.org_name.Contains(value) || r.AspNetUser.UserName.Contains(value)
                        || r.SystemModule.name.Contains(value) || r.SystemModule.SystemModule2.name.Contains(value) || r.ComplainType.complainType1.Contains(value) ||
                        r.complain_text.Contains(value) || r.Status.status_value.Contains(value));
                    ViewBag.maxSize = list.Count();
                    return PartialView("ComplainList", list.OrderByDescending(r => r.id).Skip(skip).Take(take).ToList());
                }
                else
                {
                    int org_id = (int)db.AspNetUsers.Single(r => r.UserName == User.Identity.Name).organisation;
                    var list = db.Complains.Where(r => r.org_id == org_id).Where(r => r.status_id != 5).Where(r => r.Organisation.org_name.Contains(value) || r.AspNetUser.UserName.Contains(value)
                       || r.SystemModule.name.Contains(value) || r.SystemModule.SystemModule2.name.Contains(value) || r.ComplainType.complainType1.Contains(value) ||
                       r.complain_text.Contains(value) || r.Status.status_value.Contains(value));
                    ViewBag.maxSize = list.Count();
                    return PartialView("ComplainList", list.ToList());
                }
            }
        }
        [Authorize(Roles = "Subadmin")]
        [HttpPost]
        public ActionResult Confirm(string value)
        {
            try
            {
                var id = Int32.Parse(value.Split('_')[1]);
                var item = db.Complains.Find(id);
                if (item == null)
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

                if (item.org_id != db.AspNetUsers.Single(r => r.UserName == User.Identity.Name).organisation)
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

                if (item.status_id == 5)
                {
                    item.status_id = 1;
                    db.Entry(item).State = EntityState.Modified;
                    db.SaveChanges();

                    var newstatus = new ComplainStatu()
                    {
                        complain_id = id,
                        status_date = DateTime.Now,
                        status_id = 1
                    };
                    db.ComplainStatus.Add(newstatus);
                    db.SaveChanges();

                    return RedirectToAction("Details", new { id = id });
                }
                else
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }
            }
            catch { return new HttpStatusCodeResult(HttpStatusCode.BadRequest); }
        }
        [Authorize(Roles = "Superadmin")]
        public ActionResult Replay(int id, string replay, int statusList)
        {
            var item = db.Complains.Find(id);
            item.status_id = statusList;
            item.replay = replay;

            var newstatus = new ComplainStatu();
            newstatus.complain_id = id;
            newstatus.status_date = DateTime.Now;
            newstatus.status_id = statusList;
            newstatus.replay = replay;
            db.ComplainStatus.Add(newstatus);
            db.SaveChanges();
            return RedirectToAction("Details", new { id = id });
        }
        [HttpPost]
        public ActionResult DeleteAttachedFile(int fileid, string url)
        {
            var item = db.Complains.Find(fileid);
            if (item.document_url == url)
            {
                var path = Path.Combine(Server.MapPath("/Content/file/"), item.document_url);
                FileInfo file = new FileInfo(path);
                if (file.Exists)
                    file.Delete();
                item.document_url = null;
                item.document_name = null;
                db.SaveChanges();
            }
            return RedirectToAction("Edit", new { id = fileid });

        }
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Complain complain = db.Complains.Find(id);
            db.Complains.Remove(complain);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

    }
}
