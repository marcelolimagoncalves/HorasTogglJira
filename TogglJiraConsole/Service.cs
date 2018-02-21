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
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace TogglJiraConsole
{
    public class Service
    {
        /// <summary>
        /// Instância para registro de logs.
        /// </summary>
        private static Logger Log = LogManager.GetCurrentClassLogger();

        private System.Timers.Timer _timer;

        public Service()
        {
            _timer = new System.Timers.Timer(60000);
            //_timer = new System.Timers.Timer(10000);
            _timer.Elapsed += timer_Elapsed;
        }

        static DateTime TimeStarterRun = DateTime.ParseExact(ConfigurationManager.AppSettings["TimeStarterRun"], "HH:mm", CultureInfo.InvariantCulture);
        static DateTime TimeEndRun = DateTime.ParseExact(ConfigurationManager.AppSettings["TimeEndRun"], "HH:mm", CultureInfo.InvariantCulture);
        private void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {       
            //EventLog.WriteEntry(sSource, sEvent,EventLogEntryType.Warning);

            
            var dataInicio = new DateTime(day: DateTime.Now.Day, month: DateTime.Now.Month, year: DateTime.Now.Year, hour: TimeStarterRun.Hour,
                minute: TimeStarterRun.Minute, second: TimeStarterRun.Second);


            var sSource = "dotNET Sample App";
            var sLog = "Application";
            var sEvent = $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")} == {dataInicio.ToString("dd/MM/yyyy HH:mm")}";
            if (!EventLog.SourceExists(sSource))
                EventLog.CreateEventSource(sSource, sLog);
            EventLog.WriteEntry(sSource, sEvent, EventLogEntryType.Warning);

            //if (DateTime.Now.ToString("dd/MM/yyyy HH:mm") == dataInicio.ToString("dd/MM/yyyy HH:mm"))
            //{
            //    EventLog.WriteEntry(sSource, "Sim é igual!!!", EventLogEntryType.Warning);
            //    RunAsync().GetAwaiter().GetResult();
            //}
            RunAsync().GetAwaiter().GetResult();
        }

        static string UrlBaseJira = ConfigurationManager.AppSettings["UrlBaseJira"];
        static string UrlBaseToggl = ConfigurationManager.AppSettings["UrlBaseToggl"];
        static async Task RunAsync()
        {

            var sSource = "dotNET Sample App";
            var sLog = "Application";
            var sEvent = $"RunAsync()";
            if (!EventLog.SourceExists(sSource))
                EventLog.CreateEventSource(sSource, sLog);
            EventLog.WriteEntry(sSource, sEvent, EventLogEntryType.Warning);

            var TimeEndRun = DateTime.ParseExact(ConfigurationManager.AppSettings["TimeEndRun"], "HH:mm", CultureInfo.InvariantCulture);
            var dataFim = new DateTime(day: DateTime.Now.Day, month: DateTime.Now.Month, year: DateTime.Now.Year, hour: TimeEndRun.Hour,
                minute: TimeEndRun.Minute, second: TimeEndRun.Second);
            try
            {
                Log.Debug("Iniciando a sincronizacao");
                var usuarios = await GetUsuarios();
                if (usuarios.User.Count() > 0)
                {
                    foreach (var usu in usuarios.User)
                    {
                        if (DateTime.Now >= dataFim)
                        {
                            Log.Info("A Sincronização foi finalizada porque atingiu o tempo limite.");
                            break;
                        }
                        Environment.SetEnvironmentVariable("CLIENT_NAME", usu.XNome);
                        Log.Info("Iniciando a sincronizacao");
                        var toggl = await GetToggl(user: usu);
                        if (toggl.Count() > 0)
                        {
                            foreach (var t in toggl)
                            {
                                var iJira = await PostJira(user: usu, infoWorklog: t);
                            }
                        }
                        Log.Info("Fim da sincronizacao");
                        
                    }
                }
                Log.Debug("Fim da sincronizacao");
            }
            catch (Exception ex)
            {
                Log.Error(ex, String.Format("Ocorreram erro(s): {0}", ex.ToString()));
            }

            Console.ReadLine();
        }

        static async Task<List<InfoWorklog>> GetToggl(User user)
        {
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
                    var result = await client.GetAsync(URI);
                    var retUserToggl = await result.Content.ReadAsAsync<UserToggl>();
                    var email = retUserToggl.data.email;
                    var default_wid = retUserToggl.data.default_wid;

                    var tagsPendentes = await GetTagsPendente();

                    Thread.Sleep(1000);

                    URI = String.Format("{0}/api/v8/workspaces/{1}/tags", UrlBaseToggl, default_wid);
                    result = await client.GetAsync(URI);
                    List<WorkspaceTags> retWorkspaceTags = await result.Content.ReadAsAsync<List<WorkspaceTags>>();
                    string xidTagsPendente = string.Empty;
                    if (retWorkspaceTags.Count > 0)
                    {
                        //var idTagsPendente = retWorkspaceTags.Where(i => i.name == "_Pendente")
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
                    
                    URI = String.Format("{0}/reports/api/v2/details?user_agent={1}&workspace_id={2}&tag_ids={3}&since={4}&until={5}",
                        UrlBaseToggl, email, default_wid, xidTagsPendente, since, until);
                    result = await client.GetAsync(URI);
                    var retDetailedReport = await result.Content.ReadAsAsync<DetailedReport>();
                    foreach (var data in retDetailedReport.data)
                    {
                        InfoWorklog infoWorklog = new InfoWorklog();
                        infoWorklog.key = data.description.Substring(0, data.description.IndexOf(" - "));
                        infoWorklog.comment = data.description.Substring(data.description.IndexOf(" - ") + 3);
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
                Log.Error(ex, String.Format("Algum erro aconteceu no GetToggl: {0}", ex.ToString()));
                return new List<InfoWorklog>();
            }

        }

        static async Task<int> PostJira(User user, InfoWorklog infoWorklog)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    //Jira
                    var URI = String.Format("{0}/rest/api/2/issue/{1}/worklog", UrlBaseJira, infoWorklog.key);
                    var param = new { comment = infoWorklog.comment, started = infoWorklog.started, timeSpent = infoWorklog.timeSpent };
                    var token = user.xTokenJira;
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
                    HttpResponseMessage response = await client.PostAsJsonAsync(URI, param);
                    var returnValue = await response.Content.ReadAsAsync<WorklogPost>();
                    if (returnValue.started != infoWorklog.dtStarted)
                    {
                        URI = String.Format("{0}/rest/api/2/issue/{1}/worklog/{2}", UrlBaseJira, infoWorklog.key, returnValue.id);
                        var startedAux = (Newtonsoft.Json.JsonConvert.SerializeObject(infoWorklog.dtStarted)).Replace("\"", "");
                        startedAux = startedAux.Replace(startedAux.Substring(19), ".000-0200");
                        var paramPut = new { started = startedAux };
                        response = await client.PutAsJsonAsync(URI, paramPut);
                        returnValue = await response.Content.ReadAsAsync<WorklogPost>();
                    }
                    Log.Info($"{infoWorklog.key} - {infoWorklog.comment} | {infoWorklog.timeSpent} | {infoWorklog.started} | {infoWorklog.dtStarted} ");
                    if (response.IsSuccessStatusCode)
                    {
                        var tagsPendentes = await GetTagsPendente();

                        //Toggl
                        var URIToggl = String.Format("{0}/api/v8/time_entries/{1}", UrlBaseToggl, infoWorklog.time_entry_id);
                        //var t = infoWorklog.tags.Where(i => i.ToString() != "_Pendente").ToArray();
                        var t = infoWorklog.tags.Where(i => !tagsPendentes.Tag.Contains(i.ToString().ToUpper())).ToArray();
                        var xTags = String.Join(",", t);
                        var paramToggl = new { time_entry = new { tags = t } };
                        var tokenAuxToggl = String.Format("{0}:api_token", user.XTokenToggl);
                        var tokenBytesToggl = System.Text.Encoding.UTF8.GetBytes(tokenAuxToggl);
                        var tokenToggl = Convert.ToBase64String(tokenBytesToggl);
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", tokenToggl);
                        HttpResponseMessage responseToggl = await client.PutAsJsonAsync(URIToggl, paramToggl);

                        Thread.Sleep(1000);
                    }
                }
                return 1;
            }
            catch (Exception ex)
            {

                Log.Error(ex, String.Format("Algum erro aconteceu no PostJira/UpdateTagsToggl: {0}", ex.ToString()));
                return 0;

            }

        }

        static async Task<Users> GetUsuarios()
        {
            try
            {
                string caminhoArquivo = Path.GetDirectoryName(System.AppDomain.CurrentDomain.BaseDirectory);
                caminhoArquivo = Directory.GetParent(Directory.GetParent(caminhoArquivo).FullName).FullName;
                caminhoArquivo += @"\Users.xml";

                XmlSerializer ser = new XmlSerializer(typeof(Users));
                TextReader textReader = (TextReader)new StreamReader(caminhoArquivo);
                XmlTextReader reader = new XmlTextReader(textReader);
                reader.Read();

                Users usu = (Users)ser.Deserialize(reader);

                return usu;
            }
            catch (Exception ex)
            {

                Log.Error(ex, String.Format("Algum erro aconteceu na leitura dos usuarios: {0}", ex.ToString()));
                return new Users();
            }

        }

        static async Task<TagsPendente> GetTagsPendente()
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
                Log.Error(ex, String.Format("Algum erro aconteceu na leitura das tags pendentes: {0}", ex.ToString()));
                return new TagsPendente();
            }

        }

        static string MilisecondsToJiraFormat(int mili)
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