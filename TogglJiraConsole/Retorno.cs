using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TogglJiraConsole.LogModel;

namespace TogglJiraConsole
{
    public class Retorno<T>
    {
        public Retorno(T tipo, List<LogInfo> erros)
        {
            obj = tipo;
            lErros = new List<LogInfo>();
            lErros.AddRange(erros);
        }

        public bool bError
        {
            get
            {
                if (lErros.Count > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        public List<LogInfo> lErros { get; set; }
        public T obj { get; set; }
    }

}
