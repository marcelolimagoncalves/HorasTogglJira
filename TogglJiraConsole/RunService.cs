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
        private Toggl toggl;
        private Jira jira;
        public RunService()
        {
            log = new Log();
            xml = new ArquivoXml();
            usuarios = xml.LerArqUsuarios();
            toggl = new Toggl();
            jira = new Jira();

        }

        public void Run()
        {
            string message = string.Empty;
            //running = true;
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
                        
                        var toggls = toggl.GetToggl(user: usu);

                        message = $"Toggl - Foram encontrados {toggls.Count()} Registros de trabalho.";
                        log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Info);
                        
                        var cont = 1;
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

                                //LogInfoJira = $"({cont}) Jira - Inserindo Registro de trabalho: {t.key} - {t.comment} | {t.timeSpent} | {t.started} | {t.dtStarted} ";
                                var iJira = jira.PostJira(user: usu, infoWorklog: t);
                                Thread.Sleep(1000);

                                //if (lError.Count() > 0)
                                //{
                                //    LogArqErros.Info(LogInfoJira);
                                //    foreach (var i in lError)
                                //    {
                                //        LogArqErros.Error(i);
                                //    }

                                //    lError.Clear();
                                //}
                                //else
                                //{

                                //}

                                //LogArqSucesso.Info(LogInfoJira);
                                //LogArqSucesso.Info($"Jira - Registro de trabalho foi inserido com sucesso.");
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
