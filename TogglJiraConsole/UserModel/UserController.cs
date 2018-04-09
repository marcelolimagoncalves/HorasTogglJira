using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.IO;

namespace TogglJiraConsole.UserModel
{
    public class UserController : ApiController
    {
        [HttpGet]
        public IHttpActionResult Get()
        {
            string caminhoArquivo = System.AppDomain.CurrentDomain.BaseDirectory;
            caminhoArquivo += @"\View\cadastro.html";
            string responseString = File.ReadAllText(caminhoArquivo);
            return Json(responseString);
        }
    }
}
