using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TogglJiraConsole.JiraModel
{
    public class RetPostJira
    {
        public RetPostJira()
        {
            worklogPost = new WorklogPost();
        }

        public bool bError { get; set; }
        public WorklogPost worklogPost { get; set; }
    }
}
