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
        public Toggl()
        {
            log = new Log();
            util = new Util();
            xml = new ArquivoXml();
        }

        private static string UrlBaseToggl = ConfigurationManager.AppSettings["UrlBaseToggl"];

        public RetConverterParaInfoWorklog ConverterParaInfoWorklog(List<Datum> lDt)
        {
            string message = string.Empty;
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
                    infoWorklog.time_entry_id = data.id;
                    infoWorklog.tags = data.tags;

                    lToggl.Add(infoWorklog);
                }
                return new RetConverterParaInfoWorklog() { bError = false, infoWorklog = lToggl};
            }
            catch (Exception ex)
            {
                message = $"Toggl - Algum erro aconteceu ao converter os dados para o formato Jira.";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Error);
                message = $"Toggl - Algum erro aconteceu ao converter os dados para o formato Jira: {ex.GetAllMessages()}";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Erro, logLevel: LogLevel.Error);

                return new RetConverterParaInfoWorklog() { bError = true };
            }

        }

        public RetGetUserToggl GetUserToggl(User user)
        {
            string message;
            try
            {
                using (var client = new HttpClient())
                {
                    var URI = String.Format("{0}/api/v8/me", UrlBaseToggl);
                    var tokenAux = String.Format("{0}:api_token", user.XTokenToggl);
                    var tokenBytes = System.Text.Encoding.UTF8.GetBytes(tokenAux);
                    var token = Convert.ToBase64String(tokenBytes);
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
                    var result = client.GetAsync(URI).Result;
                    var retUserToggl = result.Content.ReadAsAsync<UserToggl>().Result;
                    Thread.Sleep(1000);
                    if (result.IsSuccessStatusCode)
                    {

                        message = $"Usuário buscado com sucesso";
                        log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Debug);

                        return new RetGetUserToggl() { bError = false, userToggl = retUserToggl };
                    }
                    else
                    {
                        var ret = result.Content.ReadAsStringAsync().Result;
                        int pFrom = ret.IndexOf("<h1>") + "<h1>".Length;
                        int pTo = ret.LastIndexOf("</h1>");
                        String xRet = ret.Substring(pFrom, pTo - pFrom);

                        message = $"Toggl - Ocorreu algum erro ao buscar as informações do usuário.";
                        log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Error);
                        message = $"Toggl - Ocorreu algum erro ao buscar as informações do usuário. StatusCode: {(int)result.StatusCode}. Message: {xRet}";
                        log.InserirSalvarLog(message: message, arqLog: ArqLog.Erro, logLevel: LogLevel.Error);

                        return new RetGetUserToggl() { bError = true };
                    }

                }
            }
            catch (Exception ex)
            {
                message = $"Toggl - Ocorreu algum erro ao atualizar ao buscar as informações do usuário.";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Error);
                message = $"Toggl - Ocorreu algum erro ao buscar as informações do usuário: {ex.GetAllMessages()}";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Erro, logLevel: LogLevel.Error);

                return new RetGetUserToggl() { bError = true };
            }
            
        }

        public RetGetWorkspaceTags GetWorkspaceTags(UserToggl user, string xTokenToggl, TagsPendente tagsPendentes)
        {
            string message;
            try
            {
                using (var client = new HttpClient())
                {
                    var URI = String.Format("{0}/api/v8/workspaces/{1}/tags", UrlBaseToggl, user.data.default_wid);
                    var tokenAux = String.Format("{0}:api_token", xTokenToggl);
                    var tokenBytes = System.Text.Encoding.UTF8.GetBytes(tokenAux);
                    var token = Convert.ToBase64String(tokenBytes);
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
                    var result = client.GetAsync(URI).Result;
                    List<WorkspaceTags> retWorkspaceTags = result.Content.ReadAsAsync<List<WorkspaceTags>>().Result;
                    Thread.Sleep(1000);
                    if (result.IsSuccessStatusCode)
                    {
                        string xidTagsPendente = string.Empty;
                        if (retWorkspaceTags.Count > 0)
                        {
                            var idTagsPendente = retWorkspaceTags.Where(i => tagsPendentes.Tag.Contains(i.name.ToUpper()))
                                .Select(i => i.id).ToArray();
                            xidTagsPendente = String.Join(",", idTagsPendente);
                        }

                        message = $"Tags foram buscadas com sucesso sucesso";
                        log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Debug);

                        return new RetGetWorkspaceTags() { bError = false, xIdTagsPendente = xidTagsPendente };
                    }
                    else
                    {
                        var ret = result.Content.ReadAsStringAsync().Result;
                        int pFrom = ret.IndexOf("<h1>") + "<h1>".Length;
                        int pTo = ret.LastIndexOf("</h1>");
                        String xRet = ret.Substring(pFrom, pTo - pFrom);

                        message = $"Toggl - Ocorreu algum erro ao atualizar o Registro de trabalho retirando as tags Pendentes.";
                        log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Error);
                        message = $"Toggl - Ocorreu algum erro ao atualizar o Registro de trabalho retirando as tags Pendentes. StatusCode: {(int)result.StatusCode}. Message: {xRet}";
                        log.InserirSalvarLog(message: message, arqLog: ArqLog.Erro, logLevel: LogLevel.Error);

                        return new RetGetWorkspaceTags() { bError = true };
                    }

                }
            }
            catch (Exception ex)
            {
                message = $"Toggl - Ocorreu algum erro ao atualizar o Registro de trabalho retirando as tags Pendentes.";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Error);
                message = $"Toggl - Ocorreu algum erro ao atualizar o Registro de trabalho retirando as tags Pendentes: {ex.GetAllMessages()}";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Erro, logLevel: LogLevel.Error);

                return new RetGetWorkspaceTags() { bError = true };
            }
        }

        public RetGetDetailedReport GetDetailedReport(UserToggl user, string xIdTagsPendente, string xTokenToggl)
        {
            string message;
            try
            {

                var since = DateTime.Now.AddYears(-1).ToString("yyyy-MM-dd");
                var until = DateTime.Now.ToString("yyyy-MM-dd");
                if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["since"]) && !string.IsNullOrEmpty(ConfigurationManager.AppSettings["until"]))
                {
                    since = ConfigurationManager.AppSettings["since"];
                    until = ConfigurationManager.AppSettings["until"];
                }

                using (var client = new HttpClient())
                {
                    var contPage = 1;
                    List<Datum> ldata = new List<Datum>();
                    var URI = String.Format("{0}/reports/api/v2/details?user_agent={1}&workspace_id={2}&tag_ids={3}&since={4}&until={5}&page={6}",
                        UrlBaseToggl, user.data.email, user.data.default_wid, xIdTagsPendente, since, until, contPage);
                    var tokenAux = String.Format("{0}:api_token", xTokenToggl);
                    var tokenBytes = System.Text.Encoding.UTF8.GetBytes(tokenAux);
                    var token = Convert.ToBase64String(tokenBytes);
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
                    var result = client.GetAsync(URI).Result;
                    Thread.Sleep(1000);
                    if (result.IsSuccessStatusCode)
                    {
                        var retDetailedReport = result.Content.ReadAsAsync<DetailedReport>().Result;
                        Thread.Sleep(1000);
                        ldata.AddRange(retDetailedReport.data.OrderBy(i => i.start).ToList());
                        while (ldata.Count < retDetailedReport.total_count)
                        {
                            contPage++;
                            URI = String.Format("{0}/reports/api/v2/details?user_agent={1}&workspace_id={2}&tag_ids={3}&since={4}&until={5}&page={6}",
                            UrlBaseToggl, user.data.email, user.data.default_wid, xIdTagsPendente, since, until, contPage);
                            result = client.GetAsync(URI).Result;
                            Thread.Sleep(1000);
                            if (result.IsSuccessStatusCode)
                            {
                                retDetailedReport = result.Content.ReadAsAsync<DetailedReport>().Result;
                                ldata.AddRange(retDetailedReport.data.OrderBy(i => i.start).ToList());
                            }
                            else
                            {
                                var ret = result.Content.ReadAsStringAsync().Result;
                                int pFrom = ret.IndexOf("<h1>") + "<h1>".Length;
                                int pTo = ret.LastIndexOf("</h1>");
                                String xRet = ret.Substring(pFrom, pTo - pFrom);

                                message = $"Toggl - Ocorreu algum erro ao buscar os Registros de trabalho.";
                                log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Error);
                                message = $"Toggl - Ocorreu algum erro ao buscar os Registros de trabalho. StatusCode: {(int)result.StatusCode}. Message: {xRet}";
                                log.InserirSalvarLog(message: message, arqLog: ArqLog.Erro, logLevel: LogLevel.Error);

                                return new RetGetDetailedReport() { bError = true };
                            }

                        }

                        message = $"Toggl - Os detalhes dos registros de trabalho foram buscados com sucesso.";
                        log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Debug);

                        return new RetGetDetailedReport() { bError = false, lDatum = ldata };
                    }
                    else
                    {
                        var ret = result.Content.ReadAsStringAsync().Result;
                        int pFrom = ret.IndexOf("<h1>") + "<h1>".Length;
                        int pTo = ret.LastIndexOf("</h1>");
                        String xRet = ret.Substring(pFrom, pTo - pFrom);

                        message = $"Toggl - Ocorreu algum erro ao buscar os Registros de trabalho.";
                        log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Error);
                        message = $"Toggl - Ocorreu algum erro ao buscar os Registros de trabalho. StatusCode: {(int)result.StatusCode}. Message: {xRet}";
                        log.InserirSalvarLog(message: message, arqLog: ArqLog.Erro, logLevel: LogLevel.Error);

                        return new RetGetDetailedReport() { bError = true };
                    }
                }
            }
            catch (Exception ex)
            {
                message = $"Toggl - Ocorreu algum erro ao buscar os Registros de trabalho.";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Error);
                message = $"Toggl - Ocorreu algum erro ao buscar os Registros de trabalho: {ex.GetAllMessages()}";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Erro, logLevel: LogLevel.Error);

                return new RetGetDetailedReport() { bError = true };
            }
        }

        public int PutTogglTags(User user, InfoWorklog infoWorklog, TagsPendente tagsPendentes)
        {
            string message = string.Empty;
            try
            {
                using (var client = new HttpClient())
                {
                    
                    //Toggl
                    var URIToggl = String.Format("{0}/api/v8/time_entries/{1}", UrlBaseToggl, infoWorklog.time_entry_id);
                    var t = infoWorklog.tags.Where(i => !tagsPendentes.Tag.Contains(i.ToString().ToUpper())).ToArray();
                    var xTags = String.Join(",", t);
                    var paramToggl = new { time_entry = new { tags = t } };
                    var tokenAuxToggl = String.Format("{0}:api_token", user.XTokenToggl);
                    var tokenBytesToggl = System.Text.Encoding.UTF8.GetBytes(tokenAuxToggl);
                    var tokenToggl = Convert.ToBase64String(tokenBytesToggl);
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", tokenToggl);
                    HttpResponseMessage responseToggl = client.PutAsJsonAsync(URIToggl, paramToggl).Result;
                    Thread.Sleep(1000);
                    if (responseToggl.IsSuccessStatusCode)
                    {

                        message = $"Tags excluidas com sucesso";
                        log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Debug);

                        return 1;
                    }
                    else
                    {
                        var ret = responseToggl.Content.ReadAsStringAsync().Result;
                        int pFrom = ret.IndexOf("<h1>") + "<h1>".Length;
                        int pTo = ret.LastIndexOf("</h1>");
                        String result = ret.Substring(pFrom, pTo - pFrom);

                        message = $"Toggl - Ocorreu algum erro ao atualizar o Registro de trabalho retirando as tags Pendentes.";
                        log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Error);

                        message = $"Jira - Inserindo Registro de trabalho: {infoWorklog.key} - {infoWorklog.comment} | {infoWorklog.timeSpent} | {infoWorklog.started} | {infoWorklog.dtStarted} ";
                        log.InserirSalvarLog(message: message, arqLog: ArqLog.Erro, logLevel: LogLevel.Info);
                        message = $"Toggl - Ocorreu algum erro ao atualizar o Registro de trabalho retirando as tags Pendentes. StatusCode: {(int)responseToggl.StatusCode}. Message: {result}";
                        log.InserirSalvarLog(message: message, arqLog: ArqLog.Erro, logLevel: LogLevel.Error);

                        return 0;
                    }


                }
            }
            catch (Exception ex)
            {
                message = $"Toggl - Ocorreu algum erro ao atualizar o Registro de trabalho retirando as tags Pendentes.";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Error);

                message = $"Jira - Inserindo Registro de trabalho: {infoWorklog.key} - {infoWorklog.comment} | {infoWorklog.timeSpent} | {infoWorklog.started} | {infoWorklog.dtStarted} ";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Erro, logLevel: LogLevel.Info);
                message = $"Toggl - Ocorreu algum erro ao atualizar o Registro de trabalho retirando as tags Pendentes: {ex.GetAllMessages()}";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Erro, logLevel: LogLevel.Error);

                return 0;
            }

        }

    }
}
