using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Data.Entity;
using System.Web;
using System.Web.Mvc;
using helpDesk.Entity;
using helpDesk.Models;
using System.Net;

namespace helpDesk.Controllers
{
    public class ReportController : Controller
    {
        helpDescEntities db = new helpDescEntities();
        public ActionResult Index()
        {
            ViewBag.organisation_id = new SelectList(db.Organisations, "id", "org_name");
            return View();
        }
        public ActionResult ReportPieSingleSystem(int org_id = 0, int system_id = 0)
        {
            ViewBag.system_id = "chartContainer" + system_id;
            var list = db.SystemModules.Where(r => r.organisation_id == org_id).Where(r => r.id == system_id);
            return PartialView();
        }
        public JsonResult SystemByOrgId(int org_id)
        {
            List<Report> list = new List<Report>();
            foreach (var item in db.SystemModules.Include(q => q.Organisation).Include(q => q.SystemModule2).Include("Organisation").Where(r => r.organisation_id == org_id))
            {
                Report report = new Report();
                if (item.is_module == true)
                {
                    report.org_name = item.Organisation.org_name;
                    report.system_name = item.SystemModule2.name;
                    report.module_name = item.name;
                    report.is_module = (bool)item.is_module;
                    report.totalComplain = item.Complains.Count();
                }
                else
                {
                    report.org_name = item.Organisation.org_name;
                    report.system_name = item.name;
                    report.is_module = (bool)item.is_module;
                    report.totalComplain = item.SystemModule1.Sum(r => r.Complains.Count());
                }
                list.Add(report);
            }
            return Json(list, JsonRequestBehavior.AllowGet);
        }
        public ActionResult OrganisationDetail(int? org_id)
        {
            if (org_id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var list = db.Organisations.Find(org_id);
            ViewBag.org_id = org_id;
            ViewBag.type = db.ComplainTypes.ToList();
            ViewBag.status = db.Status.Where(r => r.is_client == "0").ToList();
            return PartialView(list);
        }
        public ActionResult ReportOrganisationChart(int? org_id)
        {
            List<OrganisationChart> orgchart = new List<OrganisationChart>();
            foreach (var item in db.SystemModules.Where(r => r.is_module == false).Where(r => r.organisation_id == org_id))
            {
                orgchart.Add(new OrganisationChart
                {
                    system = item.name,
                    aldaa = item.SystemModule1.Sum(r => r.Complains.Where(e => e.Status.is_client == "0").Where(e => e.complain_type_id == 1).Count()),
                    sanal = item.SystemModule1.Sum(r => r.Complains.Where(e => e.Status.is_client == "0").Where(e => e.complain_type_id == 2).Count()),
                    gomdol = item.SystemModule1.Sum(r => r.Complains.Where(e => e.Status.is_client == "0").Where(e => e.complain_type_id == 3).Count()),
                    talarhal = item.SystemModule1.Sum(r => r.Complains.Where(e => e.Status.is_client == "0").Where(e => e.complain_type_id == 4).Count()),
                    busad = item.SystemModule1.Sum(r => r.Complains.Where(e => e.Status.is_client == "0").Where(e => e.complain_type_id == 5).Count()),
                });
            }
            ViewBag.stringlist = orgchart;
            ViewBag.sourceId = org_id;
            ViewBag.title = db.Organisations.Find(org_id).org_name;
            ViewBag.titleId = db.Organisations.Find(org_id).org_name.ToString().Replace(" ", "");
            return PartialView("ReportOrganisationChart", orgchart);
        }
        public ActionResult ReportSystemChart(int? system_id)
        {
            List<SystemChart> systemList = new List<SystemChart>();
            ViewBag.systemTitleId = "system" + system_id;
            foreach (var item in db.SystemModules.Where(r => r.is_module == true).Where(r => r.parent_id == system_id))
            {
                systemList.Add(new SystemChart
                {
                    moduleName = item.name,
                    totalComplain = item.Complains.Where(e => e.Status.is_client == "0").Count(),
                });
            }
            return PartialView("ReportSystemChart", systemList);
        }
        public ActionResult ReportComplainStatus(int? system_id)
        {
            List<SystemStatusChart> chartlist = new List<SystemStatusChart>();

            var list = db.SystemModules.Where(r => r.is_module == true).Where(r => r.parent_id == system_id);
            ViewBag.statusTitleId = "status" + system_id;
            foreach (var item in db.Status.Where(r => r.is_client == "0"))
            {
                chartlist.Add(new SystemStatusChart
                {
                    statusName = item.status_value,
                    total = list.Sum(r => r.Complains.Where(e => e.status_id == item.id).Count()),
                });
            }
            return PartialView("ReportComplainStatus", chartlist);
        }
        public ActionResult ReportOrganisationStatus(int? org_id)
        {
            List<OrganisationStatusChart> orgchart = new List<OrganisationStatusChart>();
            foreach (var item in db.SystemModules.Where(r => r.is_module == false).Where(r => r.organisation_id == org_id))
            {
                orgchart.Add(new OrganisationStatusChart
                {
                    system = item.name,
                    ilgeegdsen = item.SystemModule1.Sum(r => r.Complains.Where(e => e.Status.is_client == "0").Where(e => e.status_id == 1).Count()),
                    shalgagdajbna = item.SystemModule1.Sum(r => r.Complains.Where(e => e.Status.is_client == "0").Where(e => e.status_id == 2).Count()),
                    shiidverlegdsen = item.SystemModule1.Sum(r => r.Complains.Where(e => e.Status.is_client == "0").Where(e => e.status_id == 3).Count()),
                    butsaagdsan = item.SystemModule1.Sum(r => r.Complains.Where(e => e.Status.is_client == "0").Where(e => e.status_id == 4).Count()),
                });
            }
            ViewBag.stringlist = orgchart;
            ViewBag.sourceId = "status" + org_id;
            ViewBag.title = db.Organisations.Find(org_id).org_name;
            ViewBag.titleId = "status" + db.Organisations.Find(org_id).org_name.ToString().Replace(" ", "");
            return PartialView("ReportOrganisationStatus", orgchart);
        }
        public ActionResult ReportQA(int? org_id)
        {
            var list = db.QAs.Where(r => r.org_id == org_id).ToList();
            ViewBag.asked = list.Count();
            ViewBag.answered = list.Count(r => r.answer_date != null);
            return PartialView("ReportQA");
        }
    }
}