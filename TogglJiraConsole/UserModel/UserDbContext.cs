using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using TogglJiraConsole.LogModel;
using TogglJiraConsole.UtilModel;
using System.IO;

namespace TogglJiraConsole.UserModel
{
    public class UserDbContext
    {
        private Log log;
        public UserDbContext()
        {
            log = new Log();
        }

        public Retorno<User> BuscarUsuarios()
        {
            string mensagemErro = string.Empty;
            var retorno = new Retorno<User>(tipo: new User(), erros: new List<LogInfo>());
            var users = new List<User>();
            try
            {
                string comandoSQL = $@"SELECT * FROM User";
                var retornoSQL = ExecutarComandoComRetorno(comandoSQL);
                foreach(var linhas in retornoSQL.obj)
                {
                    var lLinhas = linhas.ToList();
                    var user = new User();
                    foreach (var linha in lLinhas)
                    {
                        
                        
                        switch (linha.Key)
                        {
                            case "idUser":
                                user.idUser = Convert.ToInt32(linha.Value);
                                break;
                            case "xJiraLogin":
                                user.xJiraLogin = linha.Value;
                                break;
                            case "xJiraSenha":
                                user.xJiraSenha = linha.Value;
                                break;
                            case "xTogglToken":
                                user.xTogglToken = linha.Value;
                                break;
                            case "xNome":
                                user.xNome = linha.Value;
                                break;
                        }

                        
                    }
                    users.Add(user);
                }

                return retorno;
            }
            catch (Exception ex)
            {
                mensagemErro = $"Algum erro aconteceu ao buscar os usuários: {ex.GetAllMessages()}";
                retorno.lErros.Add(new LogInfo() { dtLog = DateTime.Now, logLevel = LogLevel.Error, mensagem = mensagemErro });
                return retorno;
            }
        }

        public Retorno<User> SalvarUsuario(User user)
        {
            var retorno = new Retorno<User>(tipo: user, erros: new List<LogInfo>());
            try
            {
                string query = $@"INSERT INTO User(xNome,xJiraLogin,xJiraSenha,xTogglToken) VALUES('{user.xNome}','{user.xJiraLogin}','{user.xJiraSenha}','{user.xTogglToken}')";
                var ret = ExecutarQuerySemRetorno(query);

                return retorno;
            }
            catch (Exception ex)
            {
                string message = $"Algum erro aconteceu ao salvar o usuário no banco de dados.";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Error);
                message = $"Algum erro aconteceu ao salvar o usuário no banco de dados: {ex.GetAllMessages()}";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Erro, logLevel: LogLevel.Error);

                retorno.lErros.Add(new LogInfo() { dtLog = DateTime.Now, logLevel = LogLevel.Error, mensagem = message });

                return retorno;
            }
        }

        public Retorno<User> ValidarDadosUsuario(User user)
        {
            string mensagemErro = string.Empty;
            var retorno = new Retorno<User>(tipo: user, erros: new List<LogInfo>());
            try
            {
                string comandoSQL = $@"SELECT * FROM User WHERE xJiraLogin = '{user.xJiraLogin}' OR xTogglToken = '{user.xTogglToken}'";
                var retornoSQL = ExecutarComandoComRetorno(comandoSQL);
                while (retornoSQL.obj.Count() > 0)
                {
                    mensagemErro = "Este Usuário já está cadastrado!!!";
                    retorno.lErros.Add(new LogInfo() { dtLog = DateTime.Now, logLevel = LogLevel.Error, mensagem = mensagemErro });
                }

                return retorno;
            }
            catch (Exception ex)
            {
                mensagemErro = $"Algum erro aconteceu ao verificar os dados do usuário: {ex.GetAllMessages()}";
                retorno.lErros.Add(new LogInfo() { dtLog = DateTime.Now, logLevel = LogLevel.Error, mensagem = mensagemErro });
                return retorno;
            }
        }

        public Retorno<int> ExecutarQuerySemRetorno(string query)
        {
            var retorno = new Retorno<int>(tipo: 1, erros: new List<LogInfo>());
            try
            {
                string queryCriarTabelas = string.Empty;
                if (!File.Exists("BD_USERS.db3"))
                {
                    SQLiteConnection.CreateFile("BD_USERS.db3");
                    queryCriarTabelas = @"CREATE TABLE IF NOT EXISTS
                                        [User](
                                        [idUser] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                                        [xNome] NVARCHAR(2048) NOT NULL,
                                        [xJiraLogin] NVARCHAR(2048) NOT NULL,
                                        [xJiraSenha] NVARCHAR(2048) NOT NULL,
                                        [xTogglToken] NVARCHAR(2048) NOT NULL)";
                }
                using (SQLiteConnection conn = new SQLiteConnection("data source=BD_USERS.db3"))
                {
                    using (SQLiteCommand cmd = new SQLiteCommand(conn))
                    {
                        conn.Open();
                        if (!String.IsNullOrEmpty(queryCriarTabelas))
                        {
                            cmd.CommandText = queryCriarTabelas;
                            cmd.ExecuteNonQuery();
                        }
                        cmd.CommandText = query;
                        cmd.ExecuteNonQuery();
                    }
                }

                return retorno;
            }
            catch (Exception ex)
            {
                string message = $"Algum erro aconteceu ao executar uma operação no banco de dados.";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Error);
                message = $"Algum erro aconteceu ao executar uma operação no banco de dados: {ex.GetAllMessages()}";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Erro, logLevel: LogLevel.Error);

                retorno.lErros.Add(new LogInfo() { dtLog = DateTime.Now, logLevel = LogLevel.Error, mensagem = message });

                return retorno;
            }
        }

        public Retorno<List<Dictionary<string, string>>> ExecutarComandoComRetorno(string comandoSQL)
        {
            string mensagemErro = string.Empty;
            List<Dictionary<string, string>> linhas = null;
            var retorno = new Retorno<List<Dictionary<string, string>>>(tipo: linhas, erros: new List<LogInfo>());
            if (string.IsNullOrEmpty(comandoSQL))
            {
                mensagemErro = "O comandoSQL não pode ser nulo ou vazio";
                retorno.lErros.Add(new LogInfo() { dtLog = DateTime.Now, logLevel = LogLevel.Error, mensagem = mensagemErro });
            }
            try
            {
                string queryCriarTabelas = string.Empty;
                if (!File.Exists("BD_USERS.db3"))
                {
                    SQLiteConnection.CreateFile("BD_USERS.db3");
                    queryCriarTabelas = @"CREATE TABLE IF NOT EXISTS
                                        [User](
                                        [idUser] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                                        [xNome] NVARCHAR(2048) NOT NULL,
                                        [xJiraLogin] NVARCHAR(2048) NOT NULL,
                                        [xJiraSenha] NVARCHAR(2048) NOT NULL,
                                        [xTogglToken] NVARCHAR(2048) NOT NULL)";
                }

                using (SQLiteConnection conn = new SQLiteConnection("data source=BD_USERS.db3"))
                {
                    using (SQLiteCommand cmd = new SQLiteCommand(conn))
                    {
                        conn.Open();
                        if (!String.IsNullOrEmpty(queryCriarTabelas))
                        {
                            cmd.CommandText = queryCriarTabelas;
                            cmd.ExecuteNonQuery();
                        }
                        cmd.CommandText = comandoSQL;
                        cmd.ExecuteNonQuery();
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            linhas = new List<Dictionary<string, string>>();
                            while (reader.Read())
                            {
                                var linha = new Dictionary<string, string>();

                                for (var i = 0; i < reader.FieldCount; i++)
                                {
                                    var nomeDaColuna = reader.GetName(i);
                                    var valorDaColuna = reader.IsDBNull(i) ? null : reader.GetString(i);
                                    linha.Add(nomeDaColuna, valorDaColuna);
                                }
                                linhas.Add(linha);
                            }
                            retorno.obj = linhas;
                        }
                    }

                    return retorno;
                }
            }
            catch(Exception ex )
            {
                mensagemErro = $"Algum erro aconteceu ao executar uma operação no banco de dados: {ex.GetAllMessages()}";
                retorno.lErros.Add(new LogInfo() { dtLog = DateTime.Now, logLevel = LogLevel.Error, mensagem = mensagemErro });
                return retorno;
            }
        }

    }
}
