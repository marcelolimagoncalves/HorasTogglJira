using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TogglJiraConsole.TogglModel;

namespace TogglJiraConsole.TogglModel
{
    public class RetGetUserToggl
    {
        public RetGetUserToggl()
        {
            userToggl = new UserToggl();
        }

        public bool bError { get; set; }
        public UserToggl userToggl { get; set; }
    }
}
