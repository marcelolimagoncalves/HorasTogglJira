using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
using TogglJiraConsole.UtilModel;

namespace TogglJiraConsole
{
    public class RequisicaoHttp
    {
        private Log log;
        public RequisicaoHttp()
        {
            log = new Log();
        }

        private string UrlBaseToggl = ConfigurationManager.AppSettings["UrlBaseToggl"];
        private string UrlBaseJira = ConfigurationManager.AppSettings["UrlBaseJira"];

        public Retorno<T> ExecReqJira<T>(T tipo, string url, string token, MetodoHttp metodoHttp, object param)
        {
            var retAux = Activator.CreateInstance<T>();
            var retorno = new Retorno<T>(tipo: retAux, erros: new List<LogInfo>());
            var message = string.Empty;
            try
            {
                url = $"{UrlBaseJira}" + url;
                var ret = ExecutarRequisicao(url: url, token: token, metodoHttp: metodoHttp,
                    param: param);

                if (!ret.bError)
                {
                    if (ret.obj.IsSuccessStatusCode)
                    {
                        var retResult = ret.obj.Content.ReadAsAsync<T>().Result;
                        retorno.obj = retResult;
                    }
                    else
                    {
                        var xRetResult = ret.obj.Content.ReadAsStringAsync().Result;
                        int pFrom = xRetResult.IndexOf("<h1>") + "<h1>".Length;
                        int pTo = xRetResult.LastIndexOf("</h1>");
                        xRetResult = xRetResult.Substring(pFrom, pTo - pFrom);

                        message = $"Jira - Ocorreu algum erro ao executar a requisição http.";
                        log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Error);

                        message = $@"Jira - Ocorreu algum erro ao executar a requisição http. StatusCode: 
                                    {(int)ret.obj.StatusCode}. Message: {xRetResult}";
                        retorno.lErros.Add(new LogInfo() { dtLog = DateTime.Now, logLevel = LogLevel.Error, mensagem = message });
                    }
                }
                else
                {
                    retorno.lErros.AddRange(ret.lErros);
                }

                return retorno;
            }
            catch (Exception ex)
            {
                message = $"Jira - Ocorreu algum erro ao executar a requisição http.";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Error);

                message = $"Jira - Ocorreu algum erro ao executar a requisição http: {ex.GetAllMessages()}";
                retorno.lErros.Add(new LogInfo() { dtLog = DateTime.Now, logLevel = LogLevel.Error, mensagem = message });
                return retorno;
            }

        }

        public Retorno<T> ExecReqToggl<T>(T tipo, string url, string token, MetodoHttp metodoHttp, object param)
        {
            var retAux = Activator.CreateInstance<T>();
            var retorno = new Retorno<T>(tipo: retAux, erros: new List<LogInfo>());
            var message = string.Empty;
            try
            {
                url = $"{UrlBaseToggl}" + url;
                token = $"{token}:api_token";
                var ret = ExecutarRequisicao(url: url, token: token, metodoHttp: metodoHttp,
                    param: param);

                if (!ret.bError)
                {
                    if (ret.obj.IsSuccessStatusCode)
                    {
                        var retResult = ret.obj.Content.ReadAsAsync<T>().Result;
                        retorno.obj = retResult;
                    }
                    else
                    {
                        var xRetResult = ret.obj.Content.ReadAsStringAsync().Result;
                        int pFrom = xRetResult.IndexOf("message\":\"") + "message\":\"".Length;
                        int pTo = xRetResult.LastIndexOf("\",\"tip\":");
                        xRetResult = xRetResult.Substring(pFrom, pTo - pFrom);

                        message = $"Toggl - Ocorreu algum erro ao executar a requisição http.";
                        log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Error);

                        message = $@"Toggl - Ocorreu algum erro ao executar a requisição http. StatusCode: 
                                    {(int)ret.obj.StatusCode}. Message: {xRetResult}";
                        retorno.lErros.Add(new LogInfo() { dtLog = DateTime.Now, logLevel = LogLevel.Error, mensagem = message });
                    }
                }
                else
                {
                    retorno.lErros.AddRange(ret.lErros);
                }

                return retorno;
            }
            catch (Exception ex)
            {
                message = $"Toggl - Ocorreu algum erro ao executar a requisição http.";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Error);

                message = $"Toggl - Ocorreu algum erro ao executar a requisição http: {ex.GetAllMessages()}";
                retorno.lErros.Add(new LogInfo() { dtLog = DateTime.Now, logLevel = LogLevel.Error, mensagem = message });
                return retorno;
            }

        }

        public Retorno<HttpResponseMessage> ExecutarRequisicao(string url, string token, MetodoHttp metodoHttp, object param)
        {
            var retAux = Activator.CreateInstance<HttpResponseMessage>();
            var retorno = new Retorno<HttpResponseMessage>(tipo: retAux, erros: new List<LogInfo>());
            var message = string.Empty;
            var result = new HttpResponseMessage();
            try
            {
                using (var client = new HttpClient())
                {
                    var tokenAux = token;
                    var tokenBytes = System.Text.Encoding.UTF8.GetBytes(tokenAux);
                    token = Convert.ToBase64String(tokenBytes);
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
                    switch (metodoHttp)
                    {
                        case MetodoHttp.GetAsync:
                            result = client.GetAsync(url).Result;
                            break;
                        case MetodoHttp.PostAsJsonAsync:
                            result = client.PostAsJsonAsync(url, param).Result;
                            break;
                        case MetodoHttp.PutAsJsonAsync:
                            result = client.PutAsJsonAsync(url, param).Result;
                            break;
                        case MetodoHttp.DeleteAsync:
                            result = client.DeleteAsync(url).Result;
                            break;
                    }
                    
                    Thread.Sleep(1000);

                    retorno.obj = result;
                    return retorno;
                }
            }
            catch (Exception ex)
            {
                message = $"Ocorreu algum erro ao executar a requisição http.";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Error);

                message = $"Ocorreu algum erro ao executar a requisição http: {ex.GetAllMessages()}";
                retorno.lErros.Add(new LogInfo() { dtLog = DateTime.Now, logLevel = LogLevel.Error, mensagem = message });
                return retorno;
            }
        }
    }

    public enum MetodoHttp
    {
        GetAsync,
        PostAsJsonAsync,
        PutAsJsonAsync,
        DeleteAsync
    }
}
