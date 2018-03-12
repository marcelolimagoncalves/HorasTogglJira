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
        private List<LogInfo> lErros;
        public RunService()
        {
            lErros = new List<LogInfo>();
            log = new Log();
            xml = new ArquivoXml();
            toggl = new Toggl();
            jira = new Jira();
            var retUsu = xml.LerArqUsuarios();
            if(!retUsu.bError)
            {
                usuarios = retUsu.obj;
            }
            else
            {
                lErros.AddRange(retUsu.lErros);
            }
            var retTags = xml.LerArqTagsPendente();
            if (!retTags.bError)
            {
                tagsPendentes = retTags.obj;
            }
            else
            {
                lErros.AddRange(retTags.lErros);
            }
        }

        public void Run()
        {
            string message = string.Empty;
            try
            {
                var TimeEndRun = DateTime.ParseExact(ConfigurationManager.AppSettings["TimeEndRun"], "HH:mm", CultureInfo.InvariantCulture);
                var dataFim = new DateTime(day: DateTime.Now.Day, month: DateTime.Now.Month, year: DateTime.Now.Year, hour: TimeEndRun.Hour,
                    minute: TimeEndRun.Minute, second: TimeEndRun.Second);

                bool parar = false;
                                                               
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

                        // Setando a propridedade CLIENT_NAME com o nome do usuário que vai ser sincronizado. 
                        Environment.SetEnvironmentVariable("CLIENT_NAME", usu.XNome);

                        message = $"Iniciando a sincronização.";
                        log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Info);

                        message = $"Buscando as horas lançadas no toggl.";
                        log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Debug);

                        UserToggl userToggl = new UserToggl();
                        var retUserToggl = toggl.GetUserToggl(user: usu);
                        if (!retUserToggl.bError)
                        {
                            userToggl = retUserToggl.obj;
                        }
                        else
                        {
                            lErros.AddRange(retUserToggl.lErros);
                        }

                        string xIdTagsPendente = string.Empty;
                        var retWorkspaceTags = toggl.GetWorkspaceTags(user: userToggl, xTokenToggl: usu.XTokenToggl,
                            tagsPendentes: tagsPendentes);
                        if (!retWorkspaceTags.bError)
                        {
                            xIdTagsPendente = retWorkspaceTags.obj;
                        }
                        else
                        {
                            lErros.AddRange(retWorkspaceTags.lErros);
                        }

                        List<Datum> lDatum = new List<Datum>();
                        var retGetDetailedReport = toggl.GetDetailedReport(user: userToggl, 
                            xIdTagsPendente: xIdTagsPendente, xTokenToggl: usu.XTokenToggl);
                        if (!retGetDetailedReport.bError)
                        {
                            lDatum = retGetDetailedReport.obj;
                        }
                        else
                        {
                            lErros.AddRange(retGetDetailedReport.lErros);
                        }

                        List<InfoWorklog> toggls = new List<InfoWorklog>();
                        var retToggls = toggl.ConverterParaInfoWorklog(lDt: lDatum);
                        if (!retToggls.bError)
                        {
                            toggls = retToggls.obj;
                        }
                        else
                        {
                            lErros.AddRange(retToggls.lErros);
                        }

                        message = $"Toggl - Foram encontrados {toggls.Count()} Registros de trabalho.";
                        log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Info);
                        
                        var cont = 1;
                        var contError = 0;
                        if (toggls.Count() > 0)
                        {
                            foreach (var t in toggls)
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

                                WorklogPost worklogPost = new WorklogPost();
                                var retPostJira = jira.PostJira(user: usu, infoWorklog: t);
                                if (!retPostJira.bError)
                                {
                                    worklogPost = retPostJira.obj;
                                }
                                else
                                {
                                    lErros.AddRange(retPostJira.lErros);
                                }

                                message = $"Verificando se hora de início da atividade está correta.";
                                log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Debug);

                                if (worklogPost.started != t.dtStarted)
                                {
                                    var iTimeStarted = jira.PutTimeStarted(user: usu, worklogPost: worklogPost, infoWorklog: t);
                                    if (iTimeStarted == 0) contError++;

                                    if (iTimeStarted == 0)
                                    {
                                        message = $"Tentando deletar o horário inserido anteriormente.";
                                        log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Debug);

                                        var iDeleteWorklog = jira.DeleteWorklog(user: usu, worklogPost: worklogPost, infoWorklog: t);
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
