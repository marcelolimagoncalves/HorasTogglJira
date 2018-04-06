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
using TogglJiraConsole.UserModel;
using TogglJiraConsole.UtilModel;
using TogglJiraConsole.XmlModel;
using TogglJiraConsole.UserModel;
using System.IO;

namespace TogglJiraConsole
{
    public class RunService
    {
        private Log log;
        private ArquivoXml xml;
        private List<User> usuarios;
        private TagsPendente tagsPendentes;
        private Toggl toggl;
        private Jira jira;
        private List<LogInfo> lErros;
        private UserDbContext userDbContext;
        public RunService()
        {
            lErros = new List<LogInfo>();
            log = new Log();
            xml = new ArquivoXml();
            toggl = new Toggl();
            jira = new Jira();
            userDbContext = new UserDbContext();
            //var retUsu = xml.LerArqUsuarios();
            var retUsu = userDbContext.BuscarUsuarios();
            if (!retUsu.bError)
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

                if (usuarios.Count() > 0)
                {
                    foreach (var usu in usuarios)
                    {
                        message = $"Iniciando a sincronização do usuário {usu.xNome}.";
                        log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Debug);

                        if (parar == true)
                        {
                            break;
                        }

                        Environment.SetEnvironmentVariable("CLIENT_FOLDER", System.AppDomain.CurrentDomain.BaseDirectory);

                        // Setando a propridedade CLIENT_NAME com o nome do usuário que vai ser sincronizado. 
                        Environment.SetEnvironmentVariable("CLIENT_NAME", usu.xNome);

                        message = $"Iniciando a sincronização.";
                        log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Info);

                        message = $"Buscando as horas lançadas no toggl.";
                        log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Debug);

                        //string caminhoArquivo1 = System.AppDomain.CurrentDomain.BaseDirectory;
                        //caminhoArquivo1 += @"\Logs\NewText03.txt";
                        //FileStream fs1 = new FileStream(caminhoArquivo1,
                        //    FileMode.Append);
                        //StreamWriter sw1 = new StreamWriter(fs1);
                        //sw1.WriteLine("Testando 03");
                        //sw1.Flush();
                        //sw1.Close();
                        //fs1.Close();

                        UserToggl userToggl = new UserToggl();
                        var retUserToggl = toggl.GetUserToggl(XTokenToggl: usu.xTogglToken);
                        if (!retUserToggl.bError)
                        {
                            userToggl = retUserToggl.obj;
                        }
                        else
                        {
                            lErros.AddRange(retUserToggl.lErros);
                        }

                        string xIdTagsPendente = string.Empty;
                        var retWorkspaceTags = toggl.GetWorkspaceTags(user: userToggl, xTokenToggl: usu.xTogglToken,
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
                            xIdTagsPendente: xIdTagsPendente, xTokenToggl: usu.xTogglToken);
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

                                    message = $"Verificando se hora de início da atividade está correta.";
                                    log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Debug);

                                    if (worklogPost.started != t.dtStarted)
                                    {
                                        message = $"Jira - Horario de início do Registro de trabalho está incorreto. Horário inserido: {t.dtStarted} - Horário retornado: {worklogPost.started}";
                                        log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Debug);
                                        message = $"Jira - Atualizando o horário de início do Registro de trabalho.";
                                        log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Debug);

                                        var retPutTimeStarted = jira.PutTimeStarted(user: usu, worklogPost: worklogPost, infoWorklog: t);
                                        if (!retPutTimeStarted.bError)
                                        {
                                            worklogPost = retPutTimeStarted.obj;
                                        }
                                        else
                                        {
                                            lErros.AddRange(retPutTimeStarted.lErros);

                                            message = $"Jira - Tentando deletar o horário inserido.";
                                            log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Debug);

                                            var retDeleteWorklog = jira.DeleteWorklog(user: usu, worklogPost: worklogPost, infoWorklog: t);
                                            if (!retDeleteWorklog.bError)
                                            {
                                                worklogPost = retDeleteWorklog.obj;
                                            }
                                            else
                                            {
                                                lErros.AddRange(retDeleteWorklog.lErros);
                                            }
                                        }
                                    }

                                    message = $"Tentando retirar as tags pendentes do toggl referente.";
                                    log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Debug);

                                    TogglPost togglPost = new TogglPost();
                                    var retPutTogglTags = toggl.PutTogglTags(user: usu, infoWorklog: t, tagsPendentes: tagsPendentes);
                                    if (!retPutTogglTags.bError)
                                    {
                                        message = $"({cont}) Jira - Inserindo Registro de trabalho: {t.key} - {t.comment} | {t.timeSpent} | {t.started} | {t.dtStarted} ";
                                        log.InserirSalvarLog(message: message, arqLog: ArqLog.Sucesso, logLevel: LogLevel.Info);
                                        togglPost = retPutTogglTags.obj;
                                    }
                                    else
                                    {
                                        lErros.AddRange(retPutTogglTags.lErros);
                                    }
                                }
                                else
                                {
                                    lErros.AddRange(retPostJira.lErros);
                                }

                                cont++;
                            }

                        }

                        message = $"Fim da sincronização.";
                        log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Info);

                        if (lErros.Count > 0)
                        {
                            
                            string caminhoArquivo = System.AppDomain.CurrentDomain.BaseDirectory;
                            caminhoArquivo += $@"Logs\{Environment.GetEnvironmentVariable("CLIENT_NAME")}\{lErros.FirstOrDefault().dtLog.ToString("yyyy-MM")}\{lErros.FirstOrDefault().dtLog.ToString("yyyy-MM-dd")}-Erros.log";
                            FileStream fs = new FileStream(caminhoArquivo,
                                FileMode.Append);
                            foreach (var erro in lErros)
                            {
                                StreamWriter sw = new StreamWriter(fs);
                                var strErro = $"{erro.dtLog.ToString("HH:mm:ss")} | ERROR | {erro.mensagem}";
                                sw.WriteLine(strErro);
                                sw.Flush();
                                sw.Close();

                            }
                            fs.Close();
                        }
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
