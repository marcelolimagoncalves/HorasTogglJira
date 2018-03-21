using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TogglJiraConsole.JiraModel;
using TogglJiraConsole.LogModel;
using TogglJiraConsole.UtilModel;
using TogglJiraConsole.XmlModel;

namespace TogglJiraConsole.TogglModel
{
    public class Toggl
    {
        private Log log;
        private Util util;
        private ArquivoXml xml;
        private RequisicaoHttp requisicaoHttp;
        public Toggl()
        {
            log = new Log();
            util = new Util();
            xml = new ArquivoXml();
            requisicaoHttp = new RequisicaoHttp();
        }

        private static string UrlBaseToggl = ConfigurationManager.AppSettings["UrlBaseToggl"];

        public Retorno<List<InfoWorklog>> ConverterParaInfoWorklog(List<Datum> lDt)
        {
            var retorno = new Retorno<List<InfoWorklog>>(tipo: new List<InfoWorklog>(), erros: new List<LogInfo>());
            string message;
            try
            {
                List<InfoWorklog> lToggl = new List<InfoWorklog>();
                foreach (var data in lDt)
                {
                    InfoWorklog infoWorklog = new InfoWorklog();
                    Match numJira = Regex.Match(data.description, @"(\w+)\-\d{1,4}");
                    if (numJira.Success)
                    {
                        infoWorklog.key = numJira.Value;
                        infoWorklog.comment = data.description.Replace(numJira.Value, "");
                        if (infoWorklog.comment.Substring(0, 2).Contains("- "))
                        {
                            infoWorklog.comment = infoWorklog.comment.Substring(2);
                        }
                        else if (infoWorklog.comment.Substring(0, 1).Contains("-"))
                        {
                            infoWorklog.comment = infoWorklog.comment.Substring(1);
                        }
                        else if (infoWorklog.comment.Substring(0, 3).Contains(" - "))
                        {
                            infoWorklog.comment = infoWorklog.comment.Substring(3);
                        }
                    }
                    infoWorklog.dtStarted = data.start;
                    var startedAux = (Newtonsoft.Json.JsonConvert.SerializeObject(data.start)).Replace("\"", "");
                    infoWorklog.started = startedAux.Replace(startedAux.Substring(19), ".000-0300");
                    infoWorklog.timeSpent = util.MilisecondsToJiraFormat(mili: data.dur);
                    infoWorklog.timeSpentSeconds = data.dur / 1000;
                    infoWorklog.time_entry_id = data.id;
                    infoWorklog.tags = data.tags;

                    lToggl.Add(infoWorklog);
                }

                retorno.obj = lToggl;
                return retorno;
            }
            catch (Exception ex)
            {
                message = $"Toggl - Algum erro aconteceu ao converter os dados para o formato Jira.";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Error);
                
                message = $"Toggl - Algum erro aconteceu ao converter os dados para o formato Jira: {ex.GetAllMessages()}";
                retorno.lErros.Add(new LogInfo() { dtLog = DateTime.Now, logLevel = LogLevel.Error, mensagem = message });
                return retorno;
            }

        }

        public Retorno<UserToggl> GetUserToggl(User user)
        {
            var retorno = new Retorno<UserToggl>(tipo: new UserToggl(), erros: new List<LogInfo>());
            string message;
            try
            {
                var url = $"/api/v8/me";
                var token = user.XTokenToggl;
                var ret = requisicaoHttp.ExecReqToggl(tipo: new UserToggl(), url: url, token: token, 
                    metodoHttp: MetodoHttp.GetAsync, param: new object());
                if (!ret.bError)
                {
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
                message = $"Toggl - Ocorreu algum erro ao buscar as informações do usuário.";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Error);
               
                message = $"Toggl - Ocorreu algum erro ao buscar as informações do usuário: {ex.GetAllMessages()}";
                retorno.lErros.Add(new LogInfo() { dtLog = DateTime.Now, logLevel = LogLevel.Error, mensagem = message });
                return retorno;
            }

        }

        public Retorno<string> GetWorkspaceTags(UserToggl user, string xTokenToggl, TagsPendente tagsPendentes)
        {
            var retorno = new Retorno<string>(tipo: string.Empty, erros: new List<LogInfo>());
            string message;
            try
            {

                var url = $"/api/v8/workspaces/{user.data.default_wid}/tags";
                var token = xTokenToggl;
                var ret = requisicaoHttp.ExecReqToggl(tipo: new List<WorkspaceTags>(), url: url, token: token,
                    metodoHttp: MetodoHttp.GetAsync, param: new object());
                if (!ret.bError)
                {
                    string xidTagsPendente = string.Empty;
                    if (ret.obj.Count() > 0)
                    {
                        var idTagsPendente = ret.obj.Where(i => tagsPendentes.Tag.Contains(i.name.ToUpper()))
                               .Select(i => i.id).ToArray();
                        xidTagsPendente = String.Join(",", idTagsPendente);
                    }

                    message = $"Tags foram buscadas com sucesso sucesso.";
                    log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Debug);

                    retorno.obj = xidTagsPendente;
                }
                else
                {
                    retorno.lErros.AddRange(ret.lErros);
                }

                return retorno;
            }
            catch (Exception ex)
            {
                message = $"Toggl - Ocorreu algum erro ao buscar as informações das tags pendentes.";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Error);
               
                message = $"Toggl - Ocorreu algum erro ao buscar as informações das tags pendentes: {ex.GetAllMessages()}";
                retorno.lErros.Add(new LogInfo() { dtLog = DateTime.Now, logLevel = LogLevel.Error, mensagem = message });
                return retorno;
            }
        }

        public Retorno<List<Datum>> GetDetailedReport(UserToggl user, string xIdTagsPendente, string xTokenToggl)
        {
            var retorno = new Retorno<List<Datum>>(tipo: new List<Datum>(), erros: new List<LogInfo>());
            string message;
            try
            {
                var since = DateTime.Now.AddYears(-1).ToString("yyyy-MM-dd");
                var until = DateTime.Now.ToString("yyyy-MM-dd");
                var configSince = ConfigurationManager.AppSettings["since"];
                var configUntil = ConfigurationManager.AppSettings["until"];
                if (!string.IsNullOrEmpty(configSince) && !string.IsNullOrEmpty(configUntil))
                {
                    since = configSince;
                    until = configUntil;
                }
                
                var contPage = 1;
                List<Datum> lData = new List<Datum>();
                var url = $"/reports/api/v2/details?user_agent={user.data.email}&workspace_id={user.data.default_wid}&tag_ids={xIdTagsPendente}&since={since}&until={until}&page={contPage}";
                var token = xTokenToggl;
                var ret = requisicaoHttp.ExecReqToggl(tipo: new DetailedReport(), url: url, token: token,
                    metodoHttp: MetodoHttp.GetAsync, param: new object());
                if (!ret.bError)
                {
                    lData.AddRange(ret.obj.data);

                    while (lData.Count < ret.obj.total_count)
                    {
                        contPage++;
                        url = $"/reports/api/v2/details?user_agent={user.data.email}&workspace_id={user.data.default_wid}&tag_ids={xIdTagsPendente}&since={since}&until={until}&page={contPage}";
                        ret = requisicaoHttp.ExecReqToggl(tipo: new DetailedReport(), url: url, token: token,
                            metodoHttp: MetodoHttp.GetAsync, param: new object());
                        if (!ret.bError)
                        {
                            lData.AddRange(ret.obj.data);
                        }
                        else
                        {
                            retorno.lErros.AddRange(ret.lErros);
                        }
                    }

                    message = $"Toggl - Os detalhes dos registros de trabalho foram buscados com sucesso.";
                    log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Debug);

                    retorno.obj = lData.OrderBy(i => i.start).ToList();
                }
                else
                {
                    retorno.lErros.AddRange(ret.lErros);
                }

                return retorno;
            }
            catch (Exception ex)
            {
                message = $"Toggl - Ocorreu algum erro ao buscar os Registros de trabalho.";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Error);
                
                message = $"Toggl - Ocorreu algum erro ao buscar os Registros de trabalho: {ex.GetAllMessages()}";
                retorno.lErros.Add(new LogInfo() { dtLog = DateTime.Now, logLevel = LogLevel.Error, mensagem = message });
                return retorno;
            }
        }

        public Retorno<TogglPost> PutTogglTags(User user, InfoWorklog infoWorklog, TagsPendente tagsPendentes)
        {
            var retorno = new Retorno<TogglPost>(tipo: new TogglPost(), erros: new List<LogInfo>());
            string message = string.Empty;
            try
            {

                var url = $"/api/v8/time_entries/{infoWorklog.time_entry_id}";
                var token = user.XTokenToggl;
                var t = infoWorklog.tags.Where(i => !tagsPendentes.Tag.Contains(i.ToString().ToUpper())).ToArray();
                var xTags = String.Join(",", t);
                var param = new { time_entry = new { tags = t } };
                var ret = requisicaoHttp.ExecReqToggl(tipo: new TogglPost(), url: url, token: token,
                    metodoHttp: MetodoHttp.PutAsJsonAsync, param: param);
                if (!ret.bError)
                {
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
                message = $"Toggl - Ocorreu algum erro ao atualizar o Registro de trabalho retirando as tags Pendentes.";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Error);

                message = $"Toggl - Ocorreu algum erro ao atualizar o Registro de trabalho retirando as tags Pendentes: {ex.GetAllMessages()}";
                retorno.lErros.Add(new LogInfo() { dtLog = DateTime.Now, logLevel = LogLevel.Error, mensagem = message });
                return retorno;
            }

        }

    }
}
