using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace helpDesk.Models
{
    public class Report
    {
        public string org_name { get; set; }
        public string system_name { get; set; }
        public string module_name { get; set; }
        public bool is_module { get; set; }
        public int totalComplain { get; set; }
        public string sanal { get; set; }
        public string aldaa { get; set; }
        public string gomdol { get; set; }
        public string talarhal { get; set; }
        public string busad { get; set; }
    }
}