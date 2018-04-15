using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using HorasTogglJiraServico.LogModel;
using HorasTogglJiraServico.UtilModel;

namespace HorasTogglJiraServico.XmlModel
{
    public class ArquivoXml
    {
        private Log log;
        public ArquivoXml()
        {
            log = new Log();
        }
       
        public Retorno<TagsPendente> LerArqTagsPendente()
        {
            var message = $"Buscando os tags no arquivo TagsPendente.xml.";
            log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Debug);

            var ret = LerArquivo(tipo: new TagsPendente(), nomeArq: @"\TagsPendente.xml");
            ret.obj.Tag = ret.obj.Tag.ConvertAll(d => d.ToUpper());
            return ret;
        }

        //public Retorno<Users> LerArqUsuarios()
        //{
        //    var message = $"Buscando os usuários no arquivo Users.xml.";
        //    log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Debug);

        //    var ret = LerArquivo(tipo: new Users(), nomeArq: @"\Users.xml");
        //    return ret;
        //}
        public Retorno<T> LerArquivo<T>(T tipo, string nomeArq)
        {
            var ret = Activator.CreateInstance<T>();
            var retorno = new Retorno<T>(tipo: ret, erros: new List<LogInfo>());
            try
            {

                //string caminhoArquivo = Path.GetDirectoryName(System.AppDomain.CurrentDomain.BaseDirectory);
                //caminhoArquivo = Directory.GetParent(Directory.GetParent(caminhoArquivo).FullName).FullName;
                string caminhoArquivo = System.AppDomain.CurrentDomain.BaseDirectory;
                caminhoArquivo += nomeArq;

                XmlSerializer ser = new XmlSerializer(typeof(T));
                TextReader textReader = (TextReader)new StreamReader(caminhoArquivo);
                XmlTextReader reader = new XmlTextReader(textReader);
                reader.Read();

                ret = (T)ser.Deserialize(reader);
                retorno.obj = ret;

                return retorno;
            }
            catch (Exception ex)
            {
                string message = $"Algum erro aconteceu na leitura do arquivo xml.";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Error);
                message = $"Algum erro aconteceu na leitura do arquivo xml: {ex.GetAllMessages()}";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Erro, logLevel: LogLevel.Error);

                retorno.lErros.Add(new LogInfo() { dtLog = DateTime.Now, logLevel = LogLevel.Error, mensagem = message});

                return retorno;
            }

        }

    }
}
