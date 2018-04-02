using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TogglJiraConsole.UserModel
{
    public class User
    {
        public int idUser { get; set; }
        public string xNome { get; set; }
        public string xJiraLogin { get; set; }
        public string xJiraSenha { get; set; }
        public string xTogglToken { get; set; }
    }
}
