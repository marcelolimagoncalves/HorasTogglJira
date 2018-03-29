using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
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

        public async void IniciarServidorHttp(string[] prefixes)
        {
            if (!HttpListener.IsSupported)
            {
                //Console.WriteLine("Windows XP SP2 or Server 2003 is required to use the HttpListener class.");
                //return;
            }
            // URI prefixes are required,
            // for example "http://contoso.com:8080/index/".
            if (prefixes == null || prefixes.Length == 0)
                throw new ArgumentException("prefixes");

            // Create a listener.
            HttpListener listener = new HttpListener();
            // Add the prefixes.
            foreach (string s in prefixes)
            {
                listener.Prefixes.Add(s);
            }

            Console.WriteLine("Listening...");
            listener.Start();
            while (true)
            {
                // Note: The GetContext method blocks while waiting for a request. 
                HttpListenerContext context = listener.GetContext();
                HttpListenerRequest request = context.Request;
                if (request.HttpMethod == "POST")
                {
                    // Here i can read all parameters in string but how to parse each one i don't know  
                    var data = ShowRequestData(request);
                }
                // Obtain a response object.
                HttpListenerResponse response = context.Response;
                // Construct a response.
                string responseString = "<!DOCTYPE html>" +
                                            "<html>" +
                                            "<head>" +
                                            "	<meta charset=\"utf-8\"/>" +
                                            "	<meta content=\"width=device-width, initial-scale=1, maximum-scale=1\" name=\"viewport\">" +
                                            "	<title>Título da Página (Estrutura básica de uma página com HTML 5)</title>" +
                                            "<script src=\"https://code.jquery.com/jquery-1.10.2.js\"></script>" +
                                            //"<script>" +
                                            //"$(document).on('click', \"#btnEnviarDados\", function () {  " +
                                            //"var url=\"simpleserver?login=\"+$('[name=\"login\"]').val()+\"&senha=\"+$('[name=\"senha\"]').val();" +
                                            // "$.post(url," +
                                            //"        {" +
                                            //"          login: $('[name=\"login\"]').val()," +
                                            //"          senha: $('[name=\"senha\"]').val()" +
                                            //"        }," +
                                            //"        function(data,status){" +
                                            //"            " +
                                            //"        });"+
                                            //                "});" +
                                            //"</script>" +
                                            "</head>" +
                                            "<body>" +
                                            "<h2>Informações do Jira</h2>" +
                                            "<form action=\"/simpleserver\" method=\"post\">" +
                                            "  Login:<br>" +
                                            "  <input type=\"text\" name=\"login\" value=\"\">" +
                                            "  <br>" +
                                            "  Senha:<br>" +
                                            "  <input type=\"text\" name=\"senha\" value=\"\">" +
                                            "  <br><br>" +
                                            "  <input id=\"btnEnviarDados\" type=\"submit\" value=\"Salvar\">" +
                                            "</form> " +
                                            "</body>" +
                                         "</html>";
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                // Get a response stream and write the response to it.
                response.ContentLength64 = buffer.Length;
                System.IO.Stream output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                // You must close the output stream.
                output.Close();

            }

            listener.Stop();

        }

        public static string ShowRequestData(HttpListenerRequest request)
        {
            System.IO.Stream body = request.InputStream;
            System.Text.Encoding encoding = request.ContentEncoding;
            System.IO.StreamReader reader = new System.IO.StreamReader(body, encoding);
            return reader.ReadToEnd();
            body.Close();
            reader.Close();
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
