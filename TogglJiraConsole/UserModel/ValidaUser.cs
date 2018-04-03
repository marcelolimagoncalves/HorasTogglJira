using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TogglJiraConsole.JiraModel;
using TogglJiraConsole.LogModel;
using TogglJiraConsole.TogglModel;
using TogglJiraConsole.UtilModel;

namespace TogglJiraConsole.UserModel
{
    public class ValidaUser
    {
        private UserDbContext userDb;
        private Toggl toggl;
        private Jira jira;

        public ValidaUser()
        {
            userDb = new UserDbContext();
            toggl = new Toggl();
            jira = new Jira();
        }

        public Retorno<int> ValidarDadosUsuario(User user)
        {
            var mensagemErro = string.Empty;
            var retorno = new Retorno<int>(tipo: 1, erros: new List<LogInfo>());
            try
            {
                var retornoDbUser = userDb.ValidarDadosUsuario(user);
                if (retornoDbUser.bError)
                {
                    retorno.lErros.AddRange(retornoDbUser.lErros);
                }

                var retornoToggl = toggl.GetUserToggl(XTokenToggl: user.xTogglToken);
                if (retornoToggl.bError)
                {
                    retorno.lErros.AddRange(retornoToggl.lErros);
                }

                var xTokenJira = user.xJiraToken;
                var retornoJira = jira.GetUser(xTokenJira: xTokenJira);
                if (retornoJira.bError)
                {
                    retorno.lErros.AddRange(retornoJira.lErros);
                }

                return retorno;
            }
            catch(Exception ex)
            {
                mensagemErro = ex.GetAllMessages();
                retorno.lErros.Add(new LogInfo() { dtLog = DateTime.Now, logLevel = LogLevel.Error, mensagem = mensagemErro });
                return retorno;
            }
        }
    }
}
