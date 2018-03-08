using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TogglJiraConsole.LogModel
{
    
    public class Log
    {
        /// <summary>
        /// Instância para registro de logs.
        /// </summary>
        private static Logger LogArqPrincipal = LogManager.GetLogger("ArquivoUser");
        private static Logger LogArqErro = LogManager.GetLogger("ArquivoUserErros");
        private static Logger LogArqSucesso = LogManager.GetLogger("ArquivoUserSucesso");

        public Log()
        {
            lLogArqPrincipal = new List<LogInfo>();
            lLogArqErro = new List<LogInfo>();
            lLogArqSucesso = new List<LogInfo>();
        }
        private List<LogInfo> lLogArqPrincipal { get; set; }
        private List<LogInfo> lLogArqErro { get; set; }
        private List<LogInfo> lLogArqSucesso { get; set; }
        public int countErros { get; set; }

        public void InserirSalvarLog(string message, ArqLog arqLog, LogLevel logLevel)
        {
            InserirLog(message: message, arqLog: arqLog, logLevel: logLevel);
            SalvarArquivo(arqLog: arqLog);
        }

        public void InserirLog(string message, ArqLog arqLog, LogLevel logLevel)
        {
            switch (arqLog)
            {
                case ArqLog.Principal:
                    lLogArqPrincipal.Add(new LogInfo() { mensagem = message, logLevel = logLevel });
                    break;
                case ArqLog.Erro:
                    lLogArqErro.Add(new LogInfo() { mensagem = message, logLevel = logLevel });
                    if (logLevel == LogLevel.Error) countErros++;
                    break;
                case ArqLog.Sucesso:
                    lLogArqSucesso.Add(new LogInfo() { mensagem = message, logLevel = logLevel });
                    break;
            }          
        }

        /// <summary>
        /// Este método executa o NLog que salva o arquivo de log em disco.
        /// Após salvar o arquivo de log em disco a lista de log passada como parametro é limpa.
        /// </summary>
        /// <param name="arqLog">Indica o arquivo de log em que o log será salvo</param>
        public void SalvarArquivo(ArqLog arqLog)
        {

            switch (arqLog)
            {
                case ArqLog.Principal:
                    if (lLogArqPrincipal?.Count() > 0)
                    {
                        lLogArqPrincipal.ForEach(i =>
                        {
                            EscreverArqLog(logInfo: i, arqLog: arqLog);
                        });
                    }
                    lLogArqPrincipal.Clear();
                    break;
                case ArqLog.Erro:
                    if (lLogArqErro?.Count() > 0)
                    {
                        lLogArqErro.ForEach(i =>
                        {
                            EscreverArqLog(logInfo: i, arqLog: arqLog);
                        });
                    }
                    lLogArqErro.Clear();
                    break;
                case ArqLog.Sucesso:
                    if (lLogArqSucesso?.Count() > 0)
                    {
                        lLogArqSucesso.ForEach(i =>
                        {
                            EscreverArqLog(logInfo: i, arqLog: arqLog);
                        });
                    }
                    lLogArqSucesso.Clear();
                    break;
            }

            
        }

        public void EscreverArqLog(LogInfo logInfo, ArqLog arqLog)
        {

            switch (arqLog)
            {
                case ArqLog.Principal:
                    switch (logInfo.logLevel)
                    {
                        case LogLevel.Trace:
                            LogArqPrincipal.Trace(logInfo.mensagem);
                            break;
                        case LogLevel.Debug:
                            LogArqPrincipal.Debug(logInfo.mensagem);
                            break;
                        case LogLevel.Info:
                            LogArqPrincipal.Info(logInfo.mensagem);
                            break;
                        case LogLevel.Warn:
                            LogArqPrincipal.Warn(logInfo.mensagem);
                            break;
                        case LogLevel.Error:
                            LogArqPrincipal.Error(logInfo.mensagem);
                            break;
                        case LogLevel.Fatal:
                            LogArqPrincipal.Fatal(logInfo.mensagem);
                            break;
                    }
                    break;
                case ArqLog.Erro:
                    switch (logInfo.logLevel)
                    {
                        case LogLevel.Trace:
                            LogArqErro.Trace(logInfo.mensagem);
                            break;
                        case LogLevel.Debug:
                            LogArqErro.Debug(logInfo.mensagem);
                            break;
                        case LogLevel.Info:
                            LogArqErro.Info(logInfo.mensagem);
                            break;
                        case LogLevel.Warn:
                            LogArqErro.Warn(logInfo.mensagem);
                            break;
                        case LogLevel.Error:
                            LogArqErro.Error(logInfo.mensagem);
                            break;
                        case LogLevel.Fatal:
                            LogArqErro.Fatal(logInfo.mensagem);
                            break;
                    }
                    break;
                case ArqLog.Sucesso:
                    switch (logInfo.logLevel)
                    {
                        case LogLevel.Trace:
                            LogArqSucesso.Trace(logInfo.mensagem);
                            break;
                        case LogLevel.Debug:
                            LogArqSucesso.Debug(logInfo.mensagem);
                            break;
                        case LogLevel.Info:
                            LogArqSucesso.Info(logInfo.mensagem);
                            break;
                        case LogLevel.Warn:
                            LogArqSucesso.Warn(logInfo.mensagem);
                            break;
                        case LogLevel.Error:
                            LogArqSucesso.Error(logInfo.mensagem);
                            break;
                        case LogLevel.Fatal:
                            LogArqSucesso.Fatal(logInfo.mensagem);
                            break;
                    }
                    break;
            }

            
        }
    }

    public class LogInfo
    {
        public string mensagem { get; set; }
        public LogLevel logLevel { get; set; }
    }

    public enum LogLevel
    {
        Trace,
        Debug,
        Info,
        Warn,
        Error,
        Fatal
    };

    public enum ArqLog
    {
        Principal,
        Erro,
        Sucesso,
    };
}
