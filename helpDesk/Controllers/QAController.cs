using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using helpDesk.Entity;

namespace helpDesk.Controllers
{
    public class QAController : Controller
    {
        private helpDescEntities db = new helpDescEntities();
        [Authorize(Roles = "QAAsk, QAAnswer, QAAdmin")]
        public ActionResult Index()
        {
            var qas = db.QAs.Include(q => q.AspNetUser).Include(q => q.AspNetUser1).OrderByDescending(r => r.id);
            return View(qas.ToList());
        }
        [Authorize(Roles = "QAAsk, QAAnswer, QAAdmin")]
        public ActionResult QAList(string value = "", int page = 1)
        {
            int skip = (page - 1) * 4;
            int take = 4;
            if (value == "")
            {
                if (User.IsInRole("Superadmin"))
                {
                    var QAs = db.QAs.Include(c => c.AspNetUser).Include(c => c.Organisation);
                    ViewBag.maxSize = QAs.Count();
                    return PartialView("QAList", QAs.OrderByDescending(r => r.id).Skip(skip).Take(take).ToList());
                }
                else
                {
                    int org_id = (int)db.AspNetUsers.Single(r => r.UserName == User.Identity.Name).organisation;
                    var complains = db.QAs.Include(c => c.AspNetUser).Include(c => c.Organisation).Where(r => r.org_id == org_id);
                    ViewBag.maxSize = complains.Count();
                    return PartialView("QAList", complains.OrderByDescending(r => r.id).Skip(skip).Take(take).ToList());
                }
            }
            else
            {
                if (User.IsInRole("Superadmin"))
                {
                    var list = db.QAs.Where(r => r.question.Contains(value) || r.AspNetUser.UserName.Contains(value) || r.answer.Contains(value) || r.Organisation.org_name.Contains(value));
                    ViewBag.maxSize = list.Count();
                    return PartialView("QAList", list.OrderByDescending(r => r.id).Skip(skip).Take(take).ToList());
                }
                else
                {
                    int org_id = (int)db.AspNetUsers.Single(r => r.UserName == User.Identity.Name).organisation;
                    var list = db.QAs.Where(r => r.question.Contains(value) || r.AspNetUser.UserName.Contains(value) || r.answer.Contains(value) ||
                        r.Organisation.org_name.Contains(value)).Where(r => r.org_id == org_id);
                    ViewBag.maxSize = list.Count();
                    return PartialView("QAList", list.ToList());
                }
            }
        }
        [Authorize(Roles = "QAAsk, QAAnswer, QAAdmin")]
        public ActionResult Create()
        {
            if (User.IsInRole("Superadmin"))
                ViewBag.org_id = new SelectList(db.Organisations, "id", "org_name");
            else
                ViewBag.org_id = new SelectList(db.Organisations.Where(r => r.id == 1), "id", "org_name");

            return View();
        }
        [HttpPost]
        [Authorize(Roles = "QAAsk, QAAdmin")]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "question,question_file,org_id")] QA qa, HttpPostedFileBase question_file)
        {
            if (ModelState.IsValid && qa.question != null && qa.org_id != null)
            {
                qa.ask_date = DateTime.Now;
                qa.question_user_id = db.AspNetUsers.Single(r => r.UserName == User.Identity.Name).Id;
                db.QAs.Add(qa);
                Guid guid = Guid.NewGuid();
                if (UploadImages(question_file, guid) == true)
                {
                    qa.question_file = guid + question_file.FileName;
                }
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.org_id = new SelectList(db.Organisations, "id", "org_name");
            return View(qa);
        }
        [Authorize(Roles = "QAAsk, QAAdmin")]
        public ActionResult Question(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            QA qa = db.QAs.Find(id);
            if (qa == null)
            {
                return HttpNotFound();
            }
            return View(qa);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "QAAsk, QAAdmin")]
        public ActionResult Question([Bind(Include = "id,question,question_file")] QA qa, HttpPostedFileBase question_file)
        {
            if (ModelState.IsValid)
            {
                var item = db.QAs.Find(qa.id);
                item.ask_date = DateTime.Now;
                item.question = qa.question;
                item.question_user_id = db.AspNetUsers.Single(r => r.UserName == User.Identity.Name).Id;
                db.Entry(item).State = EntityState.Modified;
                Guid guid = Guid.NewGuid();
                if (UploadImages(question_file, guid) == true)
                {
                    item.question_file = guid + question_file.FileName;
                }
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(qa);
        }
        [Authorize(Roles = "QAAnswer, QAAdmin")]
        public ActionResult Answer(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            QA qa = db.QAs.Find(id);


            if (qa == null || qa.answer_date > DateTime.Now.AddDays(-2))
            {
                return RedirectToAction("Index");
            }
            return View(qa);
        }
        [HttpPost]
        [Authorize(Roles = "QAAnswer, QAAdmin")]
        [ValidateAntiForgeryToken]
        public ActionResult Answer([Bind(Include = "id,answer,answer_file")] QA qa, HttpPostedFileBase answer_file)
        {
            if (ModelState.IsValid && qa.answer != "" && qa.answer != null)
            {
                var item = db.QAs.Find(qa.id);
                item.answer_date = DateTime.Now;
                item.answer = qa.answer;
                item.answer_user_id = db.AspNetUsers.Single(r => r.UserName == User.Identity.Name).Id;
                db.Entry(item).State = EntityState.Modified;
                Guid guid = Guid.NewGuid();
                if (UploadImages(answer_file, guid) == true)
                {
                    item.answer_file = guid + answer_file.FileName;
                }
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(qa);
        }
        [Authorize(Roles = "QAAdmin")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            QA qa = db.QAs.Find(id);
            if (qa == null)
            {
                return HttpNotFound();
            }
            return View(qa);
        }
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "QAAdmin")]
        public ActionResult DeleteConfirmed(int id)
        {
            QA qa = db.QAs.Find(id);
            db.QAs.Remove(qa);
            db.SaveChanges();
            return RedirectToAction("Index");
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
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
        [HttpPost]
        [Authorize(Roles = "QAask, QAAdmin")]
        public ActionResult DeleteAttachedFileQuestion(int fileid, string url)
        {
            var item = db.QAs.Find(fileid);
            if (item.question_file == url)
            {
                var path = Path.Combine(Server.MapPath("/Content/file/"), item.question_file);
                FileInfo file = new FileInfo(path);
                if (file.Exists)
                    file.Delete();
                item.question_file = null;
                db.SaveChanges();
            }
            return RedirectToAction("Question", new { id = fileid });

        }
        [HttpPost]
        [Authorize(Roles = "QAAnswer, QAAdmin")]
        public ActionResult DeleteAttachedFileAnswer(int fileid, string url)
        {
            var item = db.QAs.Find(fileid);
            if (item.answer_file == url)
            {
                var path = Path.Combine(Server.MapPath("/Content/file/"), item.answer_file);
                FileInfo file = new FileInfo(path);
                if (file.Exists)
                    file.Delete();
                item.answer_file = null;
                db.SaveChanges();
            }
            return RedirectToAction("Answer", new { id = fileid });

        }
        public static bool IsInRoleUserQA(string answer_user_id, string username)
        {
            helpDescEntities db = new helpDescEntities();
            var user = db.AspNetUsers.Single(r => r.UserName == username);
            if (user.Id == answer_user_id)
                return true;

            return false;
        }
    }
}
