using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TogglJiraConsole
{
    public class InfoWorklog
    {
        public string started { get; set; }
        public DateTime dtStarted { get; set; }
        public string timeSpent { get; set; }
        public string key { get; set; }
        public string comment { get; set; }
        public int time_entry_id { get; set; }
        public List<object> tags { get; set; }
    }
}
