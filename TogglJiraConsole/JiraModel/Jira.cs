using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TogglJiraConsole.LogModel;
using TogglJiraConsole.TogglModel;
using TogglJiraConsole.UtilModel;
using TogglJiraConsole.XmlModel;

namespace TogglJiraConsole.JiraModel
{
    public class Jira
    {
        private Log log;
        private Toggl toggl;
        private RequisicaoHttp requisicaoHttp;
        private DbJiraContext dbJira;
        public Jira()
        {
            log = new Log();
            toggl = new Toggl();
            requisicaoHttp = new RequisicaoHttp();
            dbJira = new DbJiraContext();
        }

        static string UrlBaseJira = ConfigurationManager.AppSettings["UrlBaseJira"];

        public Retorno<WorklogPost> PostJira(User user, InfoWorklog infoWorklog)
        {
            var retorno = new Retorno<WorklogPost>(tipo: new WorklogPost(), erros: new List<LogInfo>());
            string message = string.Empty;
            try
            {
                var strUser = Util.Base64Decode(user.xTokenJira);
                strUser = strUser.Split(':')[0];
                var ret = dbJira.InserirJira(infoWorklog: infoWorklog, user: strUser);
                if (!ret.bError)
                {
                    message = $"Jira - Registro de trabalho foi inserido com sucesso.";
                    log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Info);

                    retorno.obj = ret.obj;
                }
                else
                {
                    retorno.lErros.AddRange(ret.lErros);
                }

                return retorno;                
            }
            catch (Exception ex)
            {
                message = $"Jira - Ocorreu algum erro ao inserir o Registro de trabalho.";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Error);
               
                message = $"Jira - Ocorreu algum erro ao inserir o Registro de trabalho: {ex.GetAllMessages()}";
                retorno.lErros.Add(new LogInfo() { dtLog = DateTime.Now, logLevel = LogLevel.Error, mensagem = message });
                return retorno;

            }

        }

        public Retorno<WorklogPost> DeleteWorklog(User user, WorklogPost worklogPost, InfoWorklog infoWorklog)
        {
            var retorno = new Retorno<WorklogPost>(tipo: new WorklogPost(), erros: new List<LogInfo>());
            string message = string.Empty;
            try
            {

                var url = $"/rest/api/2/issue/{infoWorklog.key}/worklog/{worklogPost.id}";
                var token = user.XTokenToggl;
                var ret = requisicaoHttp.ExecReqJira(tipo: new WorklogPost(), url: url, token: token,
                    metodoHttp: MetodoHttp.DeleteAsync, param: new object());
                if (!ret.bError)
                {
                    message = $"Jira - Registro de trabalho foi deletado com sucesso.";
                    log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Info);

                    retorno.obj = ret.obj;
                }
                else
                {
                    retorno.lErros.AddRange(ret.lErros);
                }

                return retorno;
            }
            catch (Exception ex)
            {
                message = $"Jira - Ocorreu algum erro ao deletar o Registro de trabalho.";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Error);

                message = $"Jira - Ocorreu algum erro ao deletar o Registro de trabalho: {ex.GetAllMessages()}";
                retorno.lErros.Add(new LogInfo() { dtLog = DateTime.Now, logLevel = LogLevel.Error, mensagem = message });
                return retorno;
            }

        }

        public Retorno<WorklogPost> PutTimeStarted(User user, WorklogPost worklogPost, InfoWorklog infoWorklog)
        {
            var retorno = new Retorno<WorklogPost>(tipo: new WorklogPost(), erros: new List<LogInfo>());
            string message = string.Empty;
            try
            {
                var url = $"/rest/api/2/issue/{infoWorklog.key}/worklog/{worklogPost.id}";
                var token = user.XTokenToggl;
                var startedAux = (Newtonsoft.Json.JsonConvert.SerializeObject(infoWorklog.dtStarted)).Replace("\"", "");
                startedAux = startedAux.Replace(startedAux.Substring(19), ".000-0200");
                var param = new { started = startedAux };
                var ret = requisicaoHttp.ExecReqJira(tipo: new WorklogPost(), url: url, token: token,
                    metodoHttp: MetodoHttp.PutAsJsonAsync, param: param);
                if (!ret.bError)
                {
                    message = $"Jira - Registro de trabalho foi atualizado com sucesso.";
                    log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Info);

                    retorno.obj = ret.obj;
                }
                else
                {
                    retorno.lErros.AddRange(ret.lErros);
                }

                return retorno;

            }
            catch (Exception ex)
            {
                message = $"Jira - Ocorreu algum erro ao atualizar o horário de início do Registro de trabalho.";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Error);

                message = $"Jira - Ocorreu algum erro ao atualizar o horário de início do Registro de trabalho: {ex.GetAllMessages()}";
                retorno.lErros.Add(new LogInfo() { dtLog = DateTime.Now, logLevel = LogLevel.Error, mensagem = message });
                return retorno;
            }

        }
    }
}
