using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TogglJiraConsole.ViewModel
{
    public class MensagensRetorno
    {
        public bool bErro { get; set; }
        public string erros { get; set; }
        public bool bSucesso { get; set; }
        public string sucessos { get; set; }
    }
}
