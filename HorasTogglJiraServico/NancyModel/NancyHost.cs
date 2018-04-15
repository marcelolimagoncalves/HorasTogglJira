using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;
using Nancy.Hosting.Self;
using System.Net.Sockets;
using System.Net;
using HorasTogglJiraServico.UserModel;
using System.IO;
using HorasTogglJiraServico.ViewModel;

namespace HorasTogglJiraServico.NancyModel
{
    public class MainMod : NancyModule
    {
        private UserDbContext userDbContext;
        public MainMod(ConfigManager mgr)
        {
            userDbContext = new UserDbContext();
            var lErros = new List<string>();

            Get["/HorasTogglJira/Usuario"] = x =>
            {
                MensagensRetorno mensagens = new MensagensRetorno();
                return View[viewName: "view/cadastro.html", model: mensagens];
            };

            Post["/HorasTogglJira/Usuario"] = y =>
            {
                User user = new User();
                if (!String.IsNullOrEmpty(Request.Form["nome"]))
                {
                    user.xNome = Request.Form["nome"];
                }
                else
                {
                    lErros.Add("O campo Nome deve ser informado.");
                }
                if (!String.IsNullOrEmpty(Request.Form["login"]))
                {
                    user.xJiraLogin = Request.Form["login"];
                }
                else
                {
                    lErros.Add("O campo Jira Login deve ser informado.");
                }
                if (!String.IsNullOrEmpty(Request.Form["senha"]))
                {
                    user.xJiraSenha = Request.Form["senha"];
                }
                else
                {
                    lErros.Add("O campo Jira Senha deve ser informado.");
                }
                if (!String.IsNullOrEmpty(Request.Form["token"]))
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

                MensagensRetorno mensagens = new MensagensRetorno();
                if (lErros.Count > 0)
                {
                    
                    mensagens.bErro = true;
                    foreach (var erro in lErros)
                    {
                        mensagens.erros = mensagens.erros + "\"" + erro + "\",";
                    }
                    mensagens.erros = mensagens.erros.Substring(0, mensagens.erros.Length - 1);
                    mensagens.erros = mensagens.erros.Replace("\r\n", "");

                }
                else
                {

                    mensagens.bSucesso = true;
                    mensagens.sucessos = mensagens.sucessos + "\"Suas informações foram salvas com sucesso!\",\"Agora suas horas serão sincronizadas se estiverem lançadas no Toggl corretamente.\"";
                    
                }
                
                return View[viewName: "view/cadastro.html", model: mensagens];
            };

            

        }

    }
}
