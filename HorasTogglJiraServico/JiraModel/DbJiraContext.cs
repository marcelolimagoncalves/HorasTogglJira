//using MySql.Data.MySqlClient;
//using System;
//using System.Collections.Generic;
//using System.Configuration;
//using System.Data;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using HorasTogglJiraServico.LogModel;
//using HorasTogglJiraServico.UtilModel;

//namespace HorasTogglJiraServico.JiraModel
//{
//    public class DbJiraContext : IDisposable
//    {
//        private Log log;
//        private MySqlConnection conexao;

//        public DbJiraContext()
//        {
//            log = new Log();
//            var conexaoString = ConfigurationManager.ConnectionStrings["JiraDb"].ConnectionString;
//            conexao = new MySqlConnection(conexaoString);
//        }

//        public int ExecutaComando(string comandoSQL, Dictionary<string, object> parametros)
//        {
//            var resultado = 0;
//            if (string.IsNullOrEmpty(comandoSQL))
//            {
//                throw new ArgumentException("O comandoSQL não pode ser nulo ou vazio");
//            }
//            try
//            {
//                AbrirConexao();
//                var cmdComando = CriarComando(comandoSQL, parametros);
//                resultado = cmdComando.ExecuteNonQuery();
//            }
//            finally
//            {
//                FecharConexao();
//            }

//            return resultado;
//        }

//        public List<Dictionary<string, string>> ExecutaComandoComRetorno(string comandoSQL, Dictionary<string, object> parametros = null)
//        {
//            List<Dictionary<string, string>> linhas = null;

//            if (string.IsNullOrEmpty(comandoSQL))
//            {
//                throw new ArgumentException("O comandoSQL não pode ser nulo ou vazio");
//            }
//            try
//            {
//                AbrirConexao();
//                var cmdComando = CriarComando(comandoSQL, parametros);
//                using (var reader = cmdComando.ExecuteReader())
//                {
//                    linhas = new List<Dictionary<string, string>>();
//                    while (reader.Read())
//                    {
//                        var linha = new Dictionary<string, string>();

//                        for (var i = 0; i < reader.FieldCount; i++)
//                        {
//                            var nomeDaColuna = reader.GetName(i);
//                            var valorDaColuna = reader.IsDBNull(i) ? null : reader.GetString(i);
//                            linha.Add(nomeDaColuna, valorDaColuna);
//                        }
//                        linhas.Add(linha);
//                    }
//                }
//            }
//            finally
//            {
//                FecharConexao();
//            }

//            return linhas;
//        }

//        private MySqlCommand CriarComando(string comandoSQL, Dictionary<string, object> parametros)
//        {
//            var cmdComando = conexao.CreateCommand();
//            cmdComando.CommandText = comandoSQL;
//            AdicionarParamatros(cmdComando, parametros);
//            return cmdComando;
//        }

//        private static void AdicionarParamatros(MySqlCommand cmdComando, Dictionary<string, object> parametros)
//        {
//            if (parametros == null)
//                return;

//            foreach (var item in parametros)
//            {
//                var parametro = cmdComando.CreateParameter();
//                parametro.ParameterName = item.Key;
//                parametro.Value = item.Value ?? DBNull.Value;
//                cmdComando.Parameters.Add(parametro);
//            }
//        }

//        private void AbrirConexao()
//        {
//            if (conexao.State == ConnectionState.Open) return;

//            conexao.Open();
//        }

//        private void FecharConexao()
//        {
//            if (conexao.State == ConnectionState.Open)
//                conexao.Close();
//        }

//        public void Dispose()
//        {
//            if (conexao == null) return;

//            conexao.Dispose();
//            conexao = null;
//        }

//        public Retorno<WorklogPost> InserirJira(InfoWorklog infoWorklog, string user)
//        {
//            var retorno = new Retorno<WorklogPost>(tipo: new WorklogPost(), erros: new List<LogInfo>());
//            string message = string.Empty;
//            try
//            {
//                string project_key = infoWorklog.key.Split('-')[0];
//                int issuenum = int.Parse(!string.IsNullOrEmpty(infoWorklog.key.Split('-')[1]) ? infoWorklog.key.Split('-')[1] : "0");
//                int project_id = 0;
//                int issue_id = 0;
//                int worlog_id = 0;

//                string strQuery = $"select project_id from project_key where project_key = '{project_key}'";

//                var rows = ExecutaComandoComRetorno(strQuery);
//                foreach (var row in rows)
//                {
//                    project_id = int.Parse(!string.IsNullOrEmpty(row["project_id"]) ? row["project_id"] : "0");
//                }

//                strQuery = $"select ID from jiraissue where project = {project_id} and issuenum = {issuenum}";

//                var rows2 = ExecutaComandoComRetorno(strQuery);
//                foreach (var row in rows2)
//                {
//                    issue_id = int.Parse(!string.IsNullOrEmpty(row["ID"]) ? row["ID"] : "0");
//                }

//                strQuery = $"select ID from worklog where issueid = {issue_id} order by ID desc LIMIT 1";

//                var rows3 = ExecutaComandoComRetorno(strQuery);
//                foreach (var row in rows3)
//                {
//                    worlog_id = int.Parse(!string.IsNullOrEmpty(row["ID"]) ? row["ID"] : "0");
//                }

//                strQuery = $"insert into worklog values({worlog_id + 1},{issue_id},'{user}',null,null,'{infoWorklog.comment}', '{DateTime.Now}', '{user}', '{DateTime.Now}', '{infoWorklog.started}', {infoWorklog.timeSpentSeconds})";

//                var rows4 = ExecutaComando(strQuery, null);

//                return retorno;
//            }
//            catch(Exception ex)
//            {
//                message = $"Jira - Ocorreu algum erro ao inserir o Registro de trabalho.";
//                log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Error);

//                message = $"Jira - Ocorreu algum erro ao inserir o Registro de trabalho: {ex.GetAllMessages()}";
//                retorno.lErros.Add(new LogInfo() { dtLog = DateTime.Now, logLevel = LogLevel.Error, mensagem = message });
//                return retorno;
//            }
            
//        }

//    }
//}
