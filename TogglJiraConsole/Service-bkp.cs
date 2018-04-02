using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using TogglJiraConsole.JiraModel;
using TogglJiraConsole.TogglModel;
using TogglJiraConsole.UserModel;
using TogglJiraConsole.UtilModel;
using TogglJiraConsole.XmlModel;

namespace TogglJiraConsole
{
    public class Service_bkp
    {
        private static bool running = false;
        private static bool setinterval = true;

        /// <summary>
        /// Instância para registro de logs.
        /// </summary>
        //private static Logger Log = LogManager.GetCurrentClassLogger();
        private static Logger Log = LogManager.GetLogger("ArquivoUser");
        private static Logger LogArqErros = LogManager.GetLogger("ArquivoUserErros");
        private static Logger LogArqSucesso = LogManager.GetLogger("ArquivoUserSucesso");

        private System.Timers.Timer _timer;

        //public Service()
        //{
        //    _timer = new System.Timers.Timer(1000);
        //    _timer.Elapsed += timer_Elapsed;
        //}

        static DateTime TimeStarterRun = DateTime.ParseExact(ConfigurationManager.AppSettings["TimeStarterRun"], "HH:mm", CultureInfo.InvariantCulture);
        static DateTime TimeEndRun = DateTime.ParseExact(ConfigurationManager.AppSettings["TimeEndRun"], "HH:mm", CultureInfo.InvariantCulture);
        private void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (setinterval)
            {
                _timer.Interval = 60000;
                setinterval = false;
            }

            var dataInicio = new DateTime(day: DateTime.Now.Day, month: DateTime.Now.Month, year: DateTime.Now.Year, hour: TimeStarterRun.Hour,
                minute: TimeStarterRun.Minute, second: TimeStarterRun.Second);

            if (Convert.ToDateTime(DateTime.Now.ToString("dd/MM/yyyy HH:mm")) == Convert.ToDateTime(dataInicio.ToString("dd/MM/yyyy HH:mm")))
            {
                if (!running)
                {
                    Run();
                }
            }
        }

        static string UrlBaseJira = ConfigurationManager.AppSettings["UrlBaseJira"];
        static string UrlBaseToggl = ConfigurationManager.AppSettings["UrlBaseToggl"];
        static string LogInfoJira = string.Empty;
        static List<string> lError = new List<string>();
        public void Run()
        {
            running = true;
            var TimeEndRun = DateTime.ParseExact(ConfigurationManager.AppSettings["TimeEndRun"], "HH:mm", CultureInfo.InvariantCulture);
            var dataFim = new DateTime(day: DateTime.Now.Day, month: DateTime.Now.Month, year: DateTime.Now.Year, hour: TimeEndRun.Hour,
                minute: TimeEndRun.Minute, second: TimeEndRun.Second);

            bool parar = false;
            try
            {

                Log.Debug("Buscando os usuários no arquivo Users.xml");
                var usuarios = GetUsuarios();
                if (usuarios.Count() > 0)
                {
                    foreach (var usu in usuarios)
                    {
                        Log.Debug($"Iniciando a sincronização do usuário {usu.xNome}");
                        if (parar == true)
                        {
                            break;
                        }

                        Environment.SetEnvironmentVariable("CLIENT_NAME", usu.xNome);
                        Log.Info("Iniciando a sincronização");
                        
                        Log.Debug($"Buscando as horas lançadas no toggl");
                        var toggl = GetToggl(user: usu);
                        Log.Info($"Toggl - Foram encontrados {toggl.Count()} Registros de trabalho.");
                        var cont = 1;
                        if (toggl.Count() > 0)
                        {
                            foreach (var t in toggl)
                            {
                                if (DateTime.Now >= dataFim)
                                {
                                    Log.Info("A Sincronização foi finalizada porque atingiu o tempo limite.");
                                    parar = true;
                                    break;
                                }

                                LogInfoJira = $"({cont}) Jira - Inserindo Registro de trabalho: {t.key} - {t.comment} | {t.timeSpent} | {t.started} | {t.dtStarted} ";
                                var iJira = PostJira(user: usu, infoWorklog: t);
                                Thread.Sleep(1000);

                                if(lError.Count() > 0)
                                {
                                    LogArqErros.Info(LogInfoJira);
                                    foreach (var i in lError)
                                    {
                                        LogArqErros.Error(i);
                                    }

                                    lError.Clear();
                                }
                                else
                                {
                                    LogArqSucesso.Info(LogInfoJira);
                                    LogArqSucesso.Info($"Jira - Registro de trabalho foi inserido com sucesso");

                                }
                                cont++;
                            }
                            
                        }
                        Log.Info("Fim da sincronização");
                        
                    }
                }
                Log.Debug("Fim da sincronização");
            }
            catch (Exception ex)
            {
                Log.Error($"Ocorreram erro(s) durante o processo.");
                lError.Add(String.Format("Ocorreram erro(s): {0}", ex.GetAllMessages()));
            }

            Console.ReadLine();
        }

        static List<InfoWorklog> GetToggl(User user)
        {
            try
            {
                List<InfoWorklog> lToggl = new List<InfoWorklog>();
                using (var client = new HttpClient())
                {
                    //Toggl
                    var URI = String.Format("{0}/api/v8/me", UrlBaseToggl);
                    var tokenAux = String.Format("{0}:api_token", user.xTogglToken);
                    var tokenBytes = System.Text.Encoding.UTF8.GetBytes(tokenAux);
                    var token = Convert.ToBase64String(tokenBytes);
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
                    var result = client.GetAsync(URI).Result;
                    var retUserToggl = result.Content.ReadAsAsync<UserToggl>().Result;
                    var email = retUserToggl.data.email;
                    var default_wid = retUserToggl.data.default_wid;
                    Thread.Sleep(1000);

                    var tagsPendentes = GetTagsPendente();
                                        
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
                    if(!string.IsNullOrEmpty(ConfigurationManager.AppSettings["since"]) && !string.IsNullOrEmpty(ConfigurationManager.AppSettings["until"]))
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
                            if (infoWorklog.comment.Substring(0,2).Contains("- "))
                            {
                                infoWorklog.comment = infoWorklog.comment.Substring(2);
                            }
                            else if(infoWorklog.comment.Substring(0, 1).Contains("-"))
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
                        infoWorklog.timeSpent = MilisecondsToJiraFormat(mili: data.dur);
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
                Log.Error($"Toggl - Algum erro aconteceu ao buscar os Registros de trabalho pendentes.");
                lError.Add(String.Format("Toggl - Algum erro aconteceu ao buscar os Registros de trabalho pendentes: {0}", ex.GetAllMessages()));
                
                return new List<InfoWorklog>();
            }

        }

        static int PostJira(User user, InfoWorklog infoWorklog)
        {
            try
            {
                
                Log.Info(LogInfoJira);
                                
                using (var client = new HttpClient())
                {
                    //Jira
                    var URI = String.Format("{0}/rest/api/2/issue/{1}/worklog", UrlBaseJira, infoWorklog.key);
                    var param = new { comment = infoWorklog.comment, started = infoWorklog.started, timeSpent = infoWorklog.timeSpent };
                    var token = user.xJiraToken;
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
                    HttpResponseMessage response = client.PostAsJsonAsync(URI, param).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        Log.Info($"Jira - Registro de trabalho foi inserido com sucesso");
                        
                        var returnValue = response.Content.ReadAsAsync<WorklogPost>().Result;

                        Log.Debug($"Verificando se hora de inicio da atividade está correta");
                        var iTimeStarted = PutTimeStarted(user: user, worklogPost: returnValue, infoWorklog: infoWorklog);
                        if(iTimeStarted == 0)
                        {
                            Log.Debug($"Tentando deletar o horário inserido anteriormente");
                            var iDeleteWorklog = DeleteWorklog(user: user, worklogPost: returnValue, infoWorklog: infoWorklog);
                        }
                        else
                        {
                            
                            Log.Debug($"Tentando retirar as tags pendentes do toggl referente");
                            var iToggl = PutTogglTags(user: user, infoWorklog: infoWorklog);
                            Thread.Sleep(1000);
                        }
                        
                    }
                    else
                    {
                        var message = response.Content.ReadAsStringAsync().Result;
                        int pFrom = message.IndexOf("<h1>") + "<h1>".Length;
                        int pTo = message.LastIndexOf("</h1>");
                        String result = message.Substring(pFrom, pTo - pFrom);

                        Log.Error($"Jira - Ocorreu algum erro ao inserir o Registro de trabalho.");
                        lError.Add($"Jira - Ocorreu algum erro ao inserir o Registro de trabalho. StatusCode: {(int)response.StatusCode}. Message: {result}");
                        
                    }
                    
                }
                return 1;
            }
            catch (Exception ex)
            {
                Log.Error($"Jira - Ocorreu algum erro ao inserir o Registro de trabalho.");
                lError.Add(String.Format("Jira - Ocorreu algum erro ao inserir o Registro de trabalho: {0}", ex.GetAllMessages()));
                
                return 0;
                
            }

        }

        static int DeleteWorklog(User user, WorklogPost worklogPost, InfoWorklog infoWorklog)
        {
            try
            {
                using (var client = new HttpClient())
                {

                    var URI = String.Format("{0}/rest/api/2/issue/{1}/worklog/{2}", UrlBaseJira, infoWorklog.key, worklogPost.id);
                    var token = user.xJiraToken;
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
                    var response = client.DeleteAsync(URI).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        Log.Debug($"O horário foi deletado com sucesso");
                        return 1;
                    }
                    else
                    {
                        var message = response.Content.ReadAsStringAsync().Result;
                        int pFrom = message.IndexOf("<h1>") + "<h1>".Length;
                        int pTo = message.LastIndexOf("</h1>");
                        String result = message.Substring(pFrom, pTo - pFrom);
                        Log.Error($"Jira - Ocorreu algum erro ao deletar o Registro de trabalho.");
                        lError.Add($"Jira - Ocorreu algum erro ao deletar o Registro de trabalho: {(int)response.StatusCode}. Message: {result}");
                        
                        return 0;
                    }

                }
            }
            catch (Exception ex)
            {
                Log.Error(String.Format("Jira - Ocorreu algum erro ao deletar o Registro de trabalho."));
                lError.Add(String.Format("Jira - Ocorreu algum erro ao deletar o Registro de trabalho: {0}", ex.GetAllMessages()));
                
                return 0;
            }

        }

        static int PutTimeStarted(User user, WorklogPost worklogPost, InfoWorklog infoWorklog)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    
                    if (worklogPost.started != infoWorklog.dtStarted)
                    {
                        Log.Debug($"Jira - Horario de início do Registro de trabalho está incorreto. Horário inserido: {infoWorklog.dtStarted} - Horário retornado: {worklogPost.started}");
                        Log.Debug("Atualizando o horário de início do Registro de trabalho.");
                        var URI = String.Format("{0}/rest/api/2/issue/{1}/worklog/{2}", UrlBaseJira, infoWorklog.key, worklogPost.id);
                        var startedAux = (Newtonsoft.Json.JsonConvert.SerializeObject(infoWorklog.dtStarted)).Replace("\"", "");
                        startedAux = startedAux.Replace(startedAux.Substring(19), ".000-0200");
                        var paramPut = new { started = startedAux };
                        var token = user.xJiraToken;
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
                        var response = client.PutAsJsonAsync(URI, paramPut).Result;
                        if (response.IsSuccessStatusCode)
                        {
                            Log.Debug($"Horario atualizado com sucesso");
                            return 1;
                        }
                        else
                        {
                            var message = response.Content.ReadAsStringAsync().Result;
                            int pFrom = message.IndexOf("<h1>") + "<h1>".Length;
                            int pTo = message.LastIndexOf("</h1>");
                            String result = message.Substring(pFrom, pTo - pFrom);
                            Log.Error($"Jira - Ocorreu algum erro ao atualizar o horário de início do Registro de trabalho.");
                            lError.Add($"Jira - Ocorreu algum erro ao atualizar o horário de início do Registro de trabalho. StatusCode: {(int)response.StatusCode}. Message: {result}");
                            
                            return 0;
                        }
                    }

                    Log.Debug($"Horario não está incorreto");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Jira - Ocorreu algum erro ao atualizar o horário de início do Registro de trabalho.");
                lError.Add(String.Format("Jira - Ocorreu algum erro ao atualizar o horário de início do Registro de trabalho: {0}", ex.GetAllMessages()));
                
                return 0;
            }

        }

        static int PutTogglTags(User user, InfoWorklog infoWorklog)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    Log.Debug($"Buscando as tags que serão consideradas como pendente no arquivo TagsPendente.xml");
                    var tagsPendentes = GetTagsPendente();

                    //Toggl
                    var URIToggl = String.Format("{0}/api/v8/time_entries/{1}", UrlBaseToggl, infoWorklog.time_entry_id);
                    var t = infoWorklog.tags.Where(i => !tagsPendentes.Tag.Contains(i.ToString().ToUpper())).ToArray();
                    var xTags = String.Join(",", t);
                    var paramToggl = new { time_entry = new { tags = t } };
                    var tokenAuxToggl = String.Format("{0}:api_token", user.xTogglToken);
                    var tokenBytesToggl = System.Text.Encoding.UTF8.GetBytes(tokenAuxToggl);
                    var tokenToggl = Convert.ToBase64String(tokenBytesToggl);
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", tokenToggl);
                    HttpResponseMessage responseToggl = client.PutAsJsonAsync(URIToggl, paramToggl).Result;

                    if (responseToggl.IsSuccessStatusCode)
                    {
                        Log.Debug($"Tags excluidas com sucesso");
                        return 1;
                    }
                    else
                    {
                        var message = responseToggl.Content.ReadAsStringAsync().Result;
                        int pFrom = message.IndexOf("<h1>") + "<h1>".Length;
                        int pTo = message.LastIndexOf("</h1>");
                        String result = message.Substring(pFrom, pTo - pFrom);
                        Log.Error($"Toggl - Ocorreu algum erro ao atualizar o Registro de trabalho retirando as tags Pendentes.");
                        Log.Error($"Toggl - Ocorreu algum erro ao atualizar o Registro de trabalho retirando as tags Pendentes. StatusCode: {(int)responseToggl.StatusCode}. Message: {result}");
                        return 0;
                    }


                }
            }
            catch(Exception ex)
            {
                Log.Error($"Toggl - Ocorreu algum erro ao atualizar o Registro de trabalho retirando as tags Pendentes.");
                lError.Add(String.Format("Toggl - Ocorreu algum erro ao atualizar o Registro de trabalho retirando as tags Pendentes: {0}", ex.GetAllMessages()));
                
                return 0;
            }

        }

        static List<User> GetUsuarios()
        {
            try
            {
                string caminhoArquivo = Path.GetDirectoryName(System.AppDomain.CurrentDomain.BaseDirectory);
                caminhoArquivo = Directory.GetParent(Directory.GetParent(caminhoArquivo).FullName).FullName;
                caminhoArquivo += @"\Users.xml";

                XmlSerializer ser = new XmlSerializer(typeof(List<User>));
                TextReader textReader = (TextReader)new StreamReader(caminhoArquivo);
                XmlTextReader reader = new XmlTextReader(textReader);
                reader.Read();

                List<User> usu = (List<User>)ser.Deserialize(reader);
                
                return usu;
            }
            catch (Exception ex)
            {
                Log.Error(String.Format("Usuarios - Algum erro aconteceu na leitura dos usuarios."));
                lError.Add(String.Format("Usuarios - Algum erro aconteceu na leitura dos usuarios: {0}", ex.GetAllMessages()));
                
                return new List<User>();
            }

        }

        static TagsPendente GetTagsPendente()
        {
            try
            {
                string caminhoArquivo = Path.GetDirectoryName(System.AppDomain.CurrentDomain.BaseDirectory);
                caminhoArquivo = Directory.GetParent(Directory.GetParent(caminhoArquivo).FullName).FullName;
                caminhoArquivo += @"\TagsPendente.xml";

                XmlSerializer ser = new XmlSerializer(typeof(TagsPendente));
                TextReader textReader = (TextReader)new StreamReader(caminhoArquivo);
                XmlTextReader reader = new XmlTextReader(textReader);
                reader.Read();

                TagsPendente tags = (TagsPendente)ser.Deserialize(reader);

                tags.Tag = tags.Tag.ConvertAll(d => d.ToUpper());

                return tags;
            }
            catch (Exception ex)
            {
                Log.Error(String.Format("Tags - Algum erro aconteceu na leitura das tags pendentes."));
                lError.Add(String.Format("Tags - Algum erro aconteceu na leitura das tags pendentes: {0}", ex.GetAllMessages()));
                
                return new TagsPendente();
            }

        }

        static string MilisecondsToJiraFormat(int mili)
        {

            try
            {
                var weeks = 0;
                var days = 0;
                var hours = 0;
                var minutes = (mili / 1000) / 60;
                if (minutes < 1)
                {
                    return string.Empty;
                }

                if (minutes >= 10080)
                {
                    while (minutes >= 10080)
                    {
                        weeks++;
                        minutes = minutes - 10080;
                    }
                }

                if (minutes >= 1440)
                {
                    while (minutes >= 1440)
                    {
                        days++;
                        minutes = minutes - 1440;
                    }
                }

                if (minutes >= 60)
                {

                    while (minutes >= 60)
                    {
                        hours++;
                        minutes = minutes - 60;
                    }
                }

                string ret = string.Empty;
                if (weeks > 0)
                {
                    ret = string.Format("{0}w ", weeks);
                }
                if (days > 0)
                {
                    ret = ret + string.Format("{0}d ", days);
                }
                if (hours > 0)
                {
                    ret = ret + string.Format("{0}h ", hours);
                }
                if (minutes >= 1)
                {
                    ret = ret + string.Format("{0}m", minutes);
                }

                return ret;
            }
            catch(Exception ex)
            {
                Log.Error(String.Format("Formatar horário - Algum erro aconteceu na conversão do tempo total gasto do toggl para o jira."));
                lError.Add(String.Format("Formatar horário - Algum erro aconteceu na conversão do tempo total gasto do toggl para o jira: {0}", ex.GetAllMessages()));
                
                return string.Empty;
            }
        }

        public void Start()
        {
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }
    }
}