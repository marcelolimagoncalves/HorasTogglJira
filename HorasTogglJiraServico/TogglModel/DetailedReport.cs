using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HorasTogglJiraServico.TogglModel
{
    public class TotalCurrency
    {
        public object currency { get; set; }
        public object amount { get; set; }
    }

    public class Datum
    {
        public int id { get; set; }
        public int? pid { get; set; }
        public object tid { get; set; }
        public int uid { get; set; }
        public string description { get; set; }
        public DateTime start { get; set; }
        public DateTime end { get; set; }
        public DateTime updated { get; set; }
        public int dur { get; set; }
        public string user { get; set; }
        public bool use_stop { get; set; }
        public object client { get; set; }
        public string project { get; set; }
        public string project_color { get; set; }
        public string project_hex_color { get; set; }
        public object task { get; set; }
        public object billable { get; set; }
        public bool is_billable { get; set; }
        public object cur { get; set; }
        public List<object> tags { get; set; }
    }

    public class DetailedReport
    {
        public int total_grand { get; set; }
        public object total_billable { get; set; }
        public List<TotalCurrency> total_currencies { get; set; }
        public int total_count { get; set; }
        public int per_page { get; set; }
        public List<Datum> data { get; set; }
    }
}
