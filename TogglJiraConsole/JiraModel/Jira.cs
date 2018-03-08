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
        public Jira()
        {
            log = new Log();
            toggl = new Toggl();
        }

        static string UrlBaseJira = ConfigurationManager.AppSettings["UrlBaseJira"];

        public RetPostJira PostJira(User user, InfoWorklog infoWorklog)
        {
            string message = string.Empty;
            try
            {
                
                //Log.Info(LogInfoJira);

                using (var client = new HttpClient())
                {
                    //Jira
                    var URI = String.Format("{0}/rest/api/2/issue/{1}/worklog", UrlBaseJira, infoWorklog.key);
                    var param = new { comment = infoWorklog.comment, started = infoWorklog.started, timeSpent = infoWorklog.timeSpent };
                    var token = user.xTokenJira;
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
                    HttpResponseMessage response = client.PostAsJsonAsync(URI, param).Result;
                    Thread.Sleep(1000);
                    if (response.IsSuccessStatusCode)
                    {
                        message = $"Jira - Registro de trabalho foi inserido com sucesso.";
                        log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Info);
                        
                        var returnValue = response.Content.ReadAsAsync<WorklogPost>().Result;

                        message = $"Verificando se hora de inicio da atividade está correta.";
                        log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Debug);

                        return new RetPostJira() { bError = false, worklogPost = returnValue };

                    }
                    else
                    {
                        var ret = response.Content.ReadAsStringAsync().Result;
                        int pFrom = ret.IndexOf("<h1>") + "<h1>".Length;
                        int pTo = ret.LastIndexOf("</h1>");
                        String result = ret.Substring(pFrom, pTo - pFrom);

                        message = $"Jira - Ocorreu algum erro ao inserir o Registro de trabalho.";
                        log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Error);

                        message = $"Jira - Inserindo Registro de trabalho: {infoWorklog.key} - {infoWorklog.comment} | {infoWorklog.timeSpent} | {infoWorklog.started} | {infoWorklog.dtStarted} ";
                        log.InserirSalvarLog(message: message, arqLog: ArqLog.Erro, logLevel: LogLevel.Info);
                        message = $"Jira - Ocorreu algum erro ao inserir o Registro de trabalho. StatusCode: {(int)response.StatusCode}. Message: {result}";
                        log.InserirSalvarLog(message: message, arqLog: ArqLog.Erro, logLevel: LogLevel.Error);

                        return new RetPostJira() { bError = true };

                    }

                }
                
            }
            catch (Exception ex)
            {
                message = $"Jira - Ocorreu algum erro ao inserir o Registro de trabalho.";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Error);

                message = $"Jira - Inserindo Registro de trabalho: {infoWorklog.key} - {infoWorklog.comment} | {infoWorklog.timeSpent} | {infoWorklog.started} | {infoWorklog.dtStarted} ";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Erro, logLevel: LogLevel.Info);
                message = $"Jira - Ocorreu algum erro ao inserir o Registro de trabalho: {ex.GetAllMessages()}";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Erro, logLevel: LogLevel.Error);

                return new RetPostJira() { bError = true };

            }

        }

        public int DeleteWorklog(User user, WorklogPost worklogPost, InfoWorklog infoWorklog)
        {
            string message = string.Empty;
            try
            {
                using (var client = new HttpClient())
                {

                    var URI = String.Format("{0}/rest/api/2/issue/{1}/worklog/{2}", UrlBaseJira, infoWorklog.key, worklogPost.id);
                    var token = user.xTokenJira;
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
                    var response = client.DeleteAsync(URI).Result;
                    Thread.Sleep(1000);
                    if (response.IsSuccessStatusCode)
                    {
                        message = $"O horário foi deletado com sucesso";
                        log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Debug);
                        
                        return 1;
                    }
                    else
                    {
                        var ret = response.Content.ReadAsStringAsync().Result;
                        int pFrom = ret.IndexOf("<h1>") + "<h1>".Length;
                        int pTo = ret.LastIndexOf("</h1>");
                        String result = ret.Substring(pFrom, pTo - pFrom);

                        message = $"Jira - Ocorreu algum erro ao deletar o Registro de trabalho.";
                        log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Error);

                        message = $"Jira - Inserindo Registro de trabalho: {infoWorklog.key} - {infoWorklog.comment} | {infoWorklog.timeSpent} | {infoWorklog.started} | {infoWorklog.dtStarted} ";
                        log.InserirSalvarLog(message: message, arqLog: ArqLog.Erro, logLevel: LogLevel.Info);
                        message = $"Jira - Ocorreu algum erro ao deletar o Registro de trabalho: {(int)response.StatusCode}. Message: {result}";
                        log.InserirSalvarLog(message: message, arqLog: ArqLog.Erro, logLevel: LogLevel.Error);
                        
                        return 0;
                    }

                }
            }
            catch (Exception ex)
            {
                message = $"Jira - Ocorreu algum erro ao deletar o Registro de trabalho.";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Error);

                message = $"Jira - Inserindo Registro de trabalho: {infoWorklog.key} - {infoWorklog.comment} | {infoWorklog.timeSpent} | {infoWorklog.started} | {infoWorklog.dtStarted} ";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Erro, logLevel: LogLevel.Info);
                message = $"Jira - Ocorreu algum erro ao deletar o Registro de trabalho: {ex.GetAllMessages()}";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Erro, logLevel: LogLevel.Error);
                
                return 0;
            }

        }

        public int PutTimeStarted(User user, WorklogPost worklogPost, InfoWorklog infoWorklog)
        {
            string message = string.Empty;
            try
            {
                using (var client = new HttpClient())
                {

                    message = $"Jira - Horario de início do Registro de trabalho está incorreto. Horário inserido: {infoWorklog.dtStarted} - Horário retornado: {worklogPost.started}";
                    log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Debug);
                    message = $"Atualizando o horário de início do Registro de trabalho.";
                    log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Debug);

                    var URI = String.Format("{0}/rest/api/2/issue/{1}/worklog/{2}", UrlBaseJira, infoWorklog.key, worklogPost.id);
                    var startedAux = (Newtonsoft.Json.JsonConvert.SerializeObject(infoWorklog.dtStarted)).Replace("\"", "");
                    startedAux = startedAux.Replace(startedAux.Substring(19), ".000-0200");
                    var paramPut = new { started = startedAux };
                    var token = user.xTokenJira;
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
                    var response = client.PutAsJsonAsync(URI, paramPut).Result;
                    Thread.Sleep(1000);
                    if (response.IsSuccessStatusCode)
                    {
                        message = $"Horario atualizado com sucesso.";
                        log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Debug);

                        return 1;
                    }
                    else
                    {
                        var ret = response.Content.ReadAsStringAsync().Result;
                        int pFrom = ret.IndexOf("<h1>") + "<h1>".Length;
                        int pTo = ret.LastIndexOf("</h1>");
                        String result = ret.Substring(pFrom, pTo - pFrom);

                        message = $"Jira - Ocorreu algum erro ao atualizar o horário de início do Registro de trabalho.";
                        log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Error);

                        message = $"Jira - Inserindo Registro de trabalho: {infoWorklog.key} - {infoWorklog.comment} | {infoWorklog.timeSpent} | {infoWorklog.started} | {infoWorklog.dtStarted} ";
                        log.InserirSalvarLog(message: message, arqLog: ArqLog.Erro, logLevel: LogLevel.Info);
                        message = $"Jira - Ocorreu algum erro ao atualizar o horário de início do Registro de trabalho. StatusCode: {(int)response.StatusCode}. Message: {result}";
                        log.InserirSalvarLog(message: message, arqLog: ArqLog.Erro, logLevel: LogLevel.Error);

                        return 0;
                    }
                }
            }
            catch (Exception ex)
            {
                message = $"Jira - Ocorreu algum erro ao atualizar o horário de início do Registro de trabalho.";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Error);

                message = $"Jira - Inserindo Registro de trabalho: {infoWorklog.key} - {infoWorklog.comment} | {infoWorklog.timeSpent} | {infoWorklog.started} | {infoWorklog.dtStarted} ";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Erro, logLevel: LogLevel.Info);
                message = $"Jira - Ocorreu algum erro ao atualizar o horário de início do Registro de trabalho: {ex.GetAllMessages()}";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Erro, logLevel: LogLevel.Error);
                
                return 0;
            }

        }
    }
}
