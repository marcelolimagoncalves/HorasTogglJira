using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace TogglJiraConsole.JiraModel
{
    public class RetConverterParaInfoWorklog
    {
        public RetConverterParaInfoWorklog()
        {
            infoWorklog = new List<InfoWorklog>();
        }

        public bool bError { get; set; }
        public List<InfoWorklog> infoWorklog { get; set; }
    }
}
