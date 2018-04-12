using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;
using Nancy.Hosting.Self;
using System.Net.Sockets;
using System.Net;
using TogglJiraConsole.UserModel;
using System.IO;

namespace TogglJiraConsole.NancyModel
{
    public class MainMod : NancyModule
    {
        private UserDbContext userDbContext;
        public MainMod(ConfigManager mgr)
        {
            userDbContext = new UserDbContext();
            var lErros = new List<string>();

            Get["/Cadastro"] = x =>
            {
                return View["view/cadastro.html"];
            };

            Post["/Cadastro"] = y =>
            {
                User user = new User();
                if (Request.Form["nome"].HasValue)
                {
                    user.xNome = Request.Form["nome"];
                }
                else
                {
                    lErros.Add("O campo Nome deve ser informado.");
                }
                if (Request.Form["login"].HasValue)
                {
                    user.xJiraLogin = Request.Form["login"];
                }
                else
                {
                    lErros.Add("O campo Jira Login deve ser informado.");
                }
                if (Request.Form["senha"].HasValue)
                {
                    user.xJiraSenha = Request.Form["senha"];
                }
                else
                {
                    lErros.Add("O campo Jira Senha deve ser informado.");
                }
                if (Request.Form["token"].HasValue)
                {
                    user.xTogglToken = Request.Form["token"];
                }
                else
                {
                    lErros.Add("O campo Toggl Token deve ser informado.");
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

                string caminhoArquivo = System.AppDomain.CurrentDomain.BaseDirectory;
                caminhoArquivo += @"\View\cadastro.html";
                string responseString = File.ReadAllText(caminhoArquivo);

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
                    responseString = responseString.Replace("{sucessos}", "\"Suas informações foram salvas com sucesso!\",\"Agora suas horas serão sincronizadas se estiverem lançadas no Toggl corretamente.\"");
                }

                return View[viewName: "view/cadastro.html", model: lErros];
            };

            

        }

    }
}
