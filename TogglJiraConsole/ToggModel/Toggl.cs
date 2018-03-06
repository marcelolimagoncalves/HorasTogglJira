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
using TogglJiraConsole.ToggModel;
using TogglJiraConsole.UtilModel;
using TogglJiraConsole.XmlModel;

namespace TogglJiraConsole.TogglModel
{
    public class Toggl
    {
        private Log log;
        private Util util;
        private ArquivoXml xml;
        private TagsPendente tagsPendentes;
        public Toggl()
        {
            //log = new Log();
            log = Log.Instance;
            util = new Util();
            xml = new ArquivoXml();
            tagsPendentes = xml.LerArqTagsPendente();
        }

        private static string UrlBaseToggl = ConfigurationManager.AppSettings["UrlBaseToggl"];

        public List<InfoWorklog> GetToggl(User user)
        {
            string message = string.Empty;
            try
            {
                List<InfoWorklog> lToggl = new List<InfoWorklog>();
                using (var client = new HttpClient())
                {
                    //Toggl
                    var URI = String.Format("{0}/api/v8/me", UrlBaseToggl);
                    var tokenAux = String.Format("{0}:api_token", user.XTokenToggl);
                    var tokenBytes = System.Text.Encoding.UTF8.GetBytes(tokenAux);
                    var token = Convert.ToBase64String(tokenBytes);
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
                    var result = client.GetAsync(URI).Result;
                    var retUserToggl = result.Content.ReadAsAsync<UserToggl>().Result;
                    var email = retUserToggl.data.email;
                    var default_wid = retUserToggl.data.default_wid;
                    Thread.Sleep(1000);

                    
                    URI = String.Format("{0}/api/v8/workspaces/{1}/tags", UrlBaseToggl, default_wid);
                    result = client.GetAsync(URI).Result;
                    List<WorkspaceTags> retWorkspaceTags = result.Content.ReadAsAsync<List<WorkspaceTags>>().Result;
                    string xidTagsPendente = string.Empty;
                    if (retWorkspaceTags.Count > 0)
                    {
                        var idTagsPendente = retWorkspaceTags.Where(i => tagsPendentes.Tag.Contains(i.name.ToUpper()))
                            .Select(i => i.id).ToArray();
                        xidTagsPendente = String.Join(",", idTagsPendente);
                    }
                    Thread.Sleep(1000);

                    var since = DateTime.Now.AddYears(-1).ToString("yyyy-MM-dd");
                    var until = DateTime.Now.ToString("yyyy-MM-dd");
                    if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["since"]) && !string.IsNullOrEmpty(ConfigurationManager.AppSettings["until"]))
                    {
                        since = ConfigurationManager.AppSettings["since"];
                        until = ConfigurationManager.AppSettings["until"];
                    }

                    var contPage = 1;
                    List<Datum> ldata = new List<Datum>();
                    URI = String.Format("{0}/reports/api/v2/details?user_agent={1}&workspace_id={2}&tag_ids={3}&since={4}&until={5}&page={6}",
                        UrlBaseToggl, email, default_wid, xidTagsPendente, since, until, contPage);
                    result = client.GetAsync(URI).Result;
                    var retDetailedReport = result.Content.ReadAsAsync<DetailedReport>().Result;
                    ldata.AddRange(retDetailedReport.data.OrderBy(i => i.start).ToList());
                    while (ldata.Count < retDetailedReport.total_count)
                    {
                        contPage++;
                        URI = String.Format("{0}/reports/api/v2/details?user_agent={1}&workspace_id={2}&tag_ids={3}&since={4}&until={5}&page={6}",
                        UrlBaseToggl, email, default_wid, xidTagsPendente, since, until, contPage);
                        result = client.GetAsync(URI).Result;
                        retDetailedReport = result.Content.ReadAsAsync<DetailedReport>().Result;
                        ldata.AddRange(retDetailedReport.data.OrderBy(i => i.start).ToList());
                        Thread.Sleep(1000);
                    }
                    Thread.Sleep(1000);
                    foreach (var data in ldata)
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

                    Thread.Sleep(1000);
                }
                return lToggl;
            }
            catch (Exception ex)
            {
                message = $"Toggl - Algum erro aconteceu ao buscar os Registros de trabalho pendentes.";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Error);
                message = $"Toggl - Algum erro aconteceu ao buscar os Registros de trabalho pendentes: {ex.GetAllMessages()}";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Erro, logLevel: LogLevel.Error);
                
                return new List<InfoWorklog>();
            }

        }

        public int PutTogglTags(User user, InfoWorklog infoWorklog)
        {
            string message = string.Empty;
            try
            {
                using (var client = new HttpClient())
                {
                    message = $"Buscando as tags que serão consideradas como pendente no arquivo TagsPendente.xml";
                    log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Debug);
                    
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
                message = $"Toggl - Ocorreu algum erro ao atualizar o Registro de trabalho retirando as tags Pendentes: {ex.GetAllMessages()}";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Erro, logLevel: LogLevel.Error);

                return 0;
            }

        }

    }
}
