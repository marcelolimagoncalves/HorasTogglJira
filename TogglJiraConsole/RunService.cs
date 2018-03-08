using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TogglJiraConsole.JiraModel;
using TogglJiraConsole.LogModel;
using TogglJiraConsole.TogglModel;
using TogglJiraConsole.UtilModel;
using TogglJiraConsole.XmlModel;

namespace TogglJiraConsole
{
    public class RunService
    {
        private Log log;
        private ArquivoXml xml;
        private Users usuarios;
        private TagsPendente tagsPendentes;
        private Toggl toggl;
        private Jira jira;
        public RunService()
        {
            log = new Log();
            xml = new ArquivoXml();
            usuarios = xml.LerArqUsuarios();
            tagsPendentes = xml.LerArqTagsPendente();
            toggl = new Toggl();
            jira = new Jira();

        }

        public void Run()
        {
            string message = string.Empty;
            var TimeEndRun = DateTime.ParseExact(ConfigurationManager.AppSettings["TimeEndRun"], "HH:mm", CultureInfo.InvariantCulture);
            var dataFim = new DateTime(day: DateTime.Now.Day, month: DateTime.Now.Month, year: DateTime.Now.Year, hour: TimeEndRun.Hour,
                minute: TimeEndRun.Minute, second: TimeEndRun.Second);

            bool parar = false;
            try
            {
                message = $"Buscando os usuários no arquivo Users.xml.";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Debug);
                               
                if (usuarios.User.Count() > 0)
                {
                    foreach (var usu in usuarios.User)
                    {
                        message = $"Iniciando a sincronização do usuário {usu.XNome}.";
                        log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Debug);
                        
                        if (parar == true)
                        {
                            break;
                        }

                        Environment.SetEnvironmentVariable("CLIENT_NAME", usu.XNome);
                        message = $"Iniciando a sincronização.";
                        log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Info);

                        message = $"Buscando as horas lançadas no toggl.";
                        log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Debug);

                        var retUserToggl = toggl.GetUserToggl(user: usu);
                        var retWorkspaceTags = toggl.GetWorkspaceTags(user: retUserToggl.userToggl, xTokenToggl: usu.XTokenToggl, tagsPendentes: tagsPendentes);
                        var retGetDetailedReport = toggl.GetDetailedReport(user: retUserToggl.userToggl, 
                            xIdTagsPendente: retWorkspaceTags.xIdTagsPendente, xTokenToggl: usu.XTokenToggl);
                        var toggls = toggl.ConverterParaInfoWorklog(lDt: retGetDetailedReport.lDatum);

                        message = $"Toggl - Foram encontrados {toggls.infoWorklog.Count()} Registros de trabalho.";
                        log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Info);
                        
                        var cont = 1;
                        var contError = 0;
                        if (toggls.infoWorklog.Count() > 0)
                        {
                            foreach (var t in toggls.infoWorklog)
                            {
                                if (DateTime.Now >= dataFim)
                                {
                                    message = $"A Sincronização foi finalizada porque atingiu o tempo limite.";
                                    log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Info);
                                    
                                    parar = true;
                                    break;
                                }

                                message = $"({cont}) Jira - Inserindo Registro de trabalho: {t.key} - {t.comment} | {t.timeSpent} | {t.started} | {t.dtStarted} ";
                                log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Info);
                                //log.InserirSalvarLog(message: message, arqLog: ArqLog.Erro, logLevel: LogLevel.Info);

                                var retPostJira = jira.PostJira(user: usu, infoWorklog: t);
                                if (retPostJira.bError == true) contError++;

                                if (retPostJira.worklogPost.started != t.dtStarted)
                                {
                                    var iTimeStarted = jira.PutTimeStarted(user: usu, worklogPost: retPostJira.worklogPost, infoWorklog: t);
                                    if (iTimeStarted == 0) contError++;

                                    if (iTimeStarted == 0)
                                    {
                                        message = $"Tentando deletar o horário inserido anteriormente.";
                                        log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Debug);

                                        var iDeleteWorklog = jira.DeleteWorklog(user: usu, worklogPost: retPostJira.worklogPost, infoWorklog: t);
                                        if (iDeleteWorklog == 0) contError++;
                                    }
                                }
                               
                                message = $"Tentando retirar as tags pendentes do toggl referente.";
                                log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Debug);

                                var iToggl = toggl.PutTogglTags(user: usu, infoWorklog: t, tagsPendentes: tagsPendentes);
                                if (iToggl == 0) contError++;

                                if (contError <= 0)
                                {
                                    message = $"({cont}) Jira - Inserindo Registro de trabalho: {t.key} - {t.comment} | {t.timeSpent} | {t.started} | {t.dtStarted} ";
                                    log.InserirSalvarLog(message: message, arqLog: ArqLog.Sucesso, logLevel: LogLevel.Info);
                                    message = $"Jira - Registro de trabalho foi inserido com sucesso.";
                                    log.InserirSalvarLog(message: message, arqLog: ArqLog.Sucesso, logLevel: LogLevel.Info);
                                }
                                
                                cont++;
                            }

                        }
                        message = $"Fim da sincronização.";
                        log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Info);
                        
                    }
                }
                message = $"Fim da sincronização.";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Debug);
                
            }
            catch (Exception ex)
            {
                message = $"Ocorreram erro(s) durante o processo de sincronização.";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Error);
                message = $"Ocorreram erro(s): {ex.GetAllMessages()}";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Erro, logLevel: LogLevel.Error);
            }

            Console.ReadLine();
        }
    }
}
