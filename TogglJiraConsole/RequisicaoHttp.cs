using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TogglJiraConsole.LogModel;
using TogglJiraConsole.UserModel;
using TogglJiraConsole.UtilModel;
using System.Web;

namespace TogglJiraConsole
{
    public class RequisicaoHttp
    {
        private Log log;
        private HttpListener listener;
        private UserDbContext userDbContext;
        public RequisicaoHttp()
        {
            log = new Log();
            listener = new HttpListener();
            userDbContext = new UserDbContext();

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
                        if (!string.IsNullOrEmpty(xRetResult))
                        {
                            int pFrom = xRetResult.IndexOf("<h1>") + "<h1>".Length;
                            int pTo = xRetResult.LastIndexOf("</h1>");
                            xRetResult = xRetResult.Substring(pFrom, pTo - pFrom);
                        }
                        
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
                        if (!string.IsNullOrEmpty(xRetResult))
                        {
                            int pFrom = xRetResult.IndexOf("message\":\"") + "message\":\"".Length;
                            int pTo = xRetResult.LastIndexOf("\",\"tip\":");
                            xRetResult = xRetResult.Substring(pFrom, pTo - pFrom);
                        }
                        
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

        public async Task IniciarServidorHttp(string[] prefixes)
        {
            var mensagemErro = string.Empty;
            try
            {
                if (!HttpListener.IsSupported)
                {
                    mensagemErro = $"O sistema operacional não suporta o HttpListener.";
                    //log.InserirSalvarLog(message: mensagemErro, arqLog: ArqLog.Principal, logLevel: LogLevel.Error);
                    //log.InserirSalvarLog(message: mensagemErro, arqLog: ArqLog.Erro, logLevel: LogLevel.Error);
                }

                if (prefixes == null || prefixes.Length == 0)
                {
                    mensagemErro = $"O prefixo URL não foi definido.";
                    //log.InserirSalvarLog(message: mensagemErro, arqLog: ArqLog.Principal, logLevel: LogLevel.Error);
                    //log.InserirSalvarLog(message: mensagemErro, arqLog: ArqLog.Erro, logLevel: LogLevel.Error);
                }
                          
                if (string.IsNullOrEmpty(mensagemErro))
                {
                    foreach (string s in prefixes)
                    {
                        listener.Prefixes.Add(s);
                    }

                    listener.Start();
                    //log.InserirSalvarLog(message: "O servidor http foi iniciado.", arqLog: ArqLog.Principal, logLevel: LogLevel.Debug);

                    while (true)
                    {
                        var lErros = new List<string>();
                        HttpListenerContext context = listener.GetContext();
                        HttpListenerRequest request = context.Request;
                        if (request.HttpMethod == "POST")
                        {
                            User user = new User();
                            try
                            {
                                // Here i can read all parameters in string but how to parse each one i don't know  
                                StringWriter myWriter = new StringWriter();
                                // Decode the encoded string.

                                var data = ShowRequestData(request);
                                data = WebUtility.UrlDecode(data);
                                var ldata = data.Split('&');
                                for (var i = 0; i <= ldata.Count(); i++)
                                {
                                    switch (i)
                                    {
                                        case 0:
                                            user.xNome = ldata[i].Split('=')[1].Replace('+', ' ');
                                            if (string.IsNullOrEmpty(user.xNome))
                                            {
                                                lErros.Add("O campo Nome deve ser informado.");
                                            }
                                            break;
                                        case 1:
                                            user.xJiraLogin = ldata[i].Split('=')[1].Replace('+', ' ');
                                            if (string.IsNullOrEmpty(user.xJiraLogin))
                                            {
                                                lErros.Add("O campo Jira Login deve ser informado.");
                                            }
                                            break;
                                        case 2:
                                            user.xJiraSenha = ldata[i].Split('=')[1].Replace('+', ' ');
                                            if (string.IsNullOrEmpty(user.xJiraSenha))
                                            {
                                                lErros.Add("O campo Jira Senha deve ser informado.");
                                            }
                                            break;
                                        case 3:
                                            user.xTogglToken = ldata[i].Split('=')[1].Replace('+', ' ');
                                            if (string.IsNullOrEmpty(user.xTogglToken))
                                            {
                                                lErros.Add("O campo Toggl Token deve ser informado.");
                                            }
                                            break;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                //mensagemErro = $"Algum erro aconteceu com os campos informados.";
                                //log.InserirSalvarLog(message: mensagemErro, arqLog: ArqLog.Principal, logLevel: LogLevel.Error);
                                mensagemErro = mensagemErro + $": {ex.GetAllMessages()}";
                                //log.InserirSalvarLog(message: mensagemErro, arqLog: ArqLog.Erro, logLevel: LogLevel.Error);
                                lErros.Add(mensagemErro);
                            }

                            if (lErros.Count <= 0)
                            {
                                ValidaUser validaUser = new ValidaUser();
                                var retonoUsuario = validaUser.ValidarDadosUsuario(user);
                                if (!retonoUsuario.bError)
                                {
                                    var retornoSalvar = userDbContext.SalvarUsuario(user);
                                    if (retornoSalvar.bError)
                                    {
                                        foreach (var erro in retornoSalvar.lErros)
                                        {
                                            lErros.Add(erro.mensagem);
                                        }
                                    }
                                }
                                else
                                {
                                    foreach (var erro in retonoUsuario.lErros)
                                    {
                                        lErros.Add(erro.mensagem);
                                    }
                                }
                            }

                        }
                        // Obtain a response object.
                        HttpListenerResponse response = context.Response;

                        string caminhoArquivo = Path.GetDirectoryName(System.AppDomain.CurrentDomain.BaseDirectory);
                        caminhoArquivo = Directory.GetParent(Directory.GetParent(caminhoArquivo).FullName).FullName;
                        caminhoArquivo += @"\View\cadastro.html";
                        string responseString = File.ReadAllText(caminhoArquivo);
                        if (request.HttpMethod == "POST")
                        {
                            if (lErros.Count > 0)
                            {
                                var strErros = string.Empty;
                                foreach (var erro in lErros)
                                {
                                    strErros = strErros + $"\"{erro.Replace("\r\n", "")}\",";
                                }
                                strErros = strErros.Substring(0, strErros.Length - 1);
                                responseString = responseString.Replace("{sucessos}", "");
                                responseString = responseString.Replace("{erros}", strErros);

                            }
                            else
                            {
                                responseString = responseString.Replace("{erros}", "");
                                responseString = responseString.Replace("{sucessos}", "\"Suas informações foram salvas com sucesso!\",\"Agora suas horas serão sincronizadas se estiverem lançadas no Toggl corretamente.\",\"Para lançar corretamente suas horas no Toggl procure algum colega de trabalho que certamente ele irá saber ;)\"");
                            }
                        }
                        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                        // Get a response stream and write the response to it.
                        response.ContentLength64 = buffer.Length;
                        System.IO.Stream output = response.OutputStream;
                        output.Write(buffer, 0, buffer.Length);
                        // You must close the output stream.
                        output.Close();

                    }
                }
                
            }
            catch (Exception ex)
            {
                //mensagemErro = $"Ocorreu algum erro com o servidor http.";
                //log.InserirSalvarLog(message: mensagemErro, arqLog: ArqLog.Principal, logLevel: LogLevel.Error);

                mensagemErro = $"Ocorreu algum erro com o servidor http: {ex.GetAllMessages()}";
                //log.InserirSalvarLog(message: mensagemErro, arqLog: ArqLog.Erro, logLevel: LogLevel.Error);
            }

        }

        public async void FecharServidorHttp()
        {
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
