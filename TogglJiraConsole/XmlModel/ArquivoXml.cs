using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using TogglJiraConsole.LogModel;
using TogglJiraConsole.UtilModel;

namespace TogglJiraConsole.XmlModel
{
    public class ArquivoXml
    {
        private Log log;
        public ArquivoXml()
        {
            //log = new Log();
            log = Log.Instance;
        }
        public TagsPendente LerArqTagsPendente()
        {
            try
            {
                string caminhoArquivo = Path.GetDirectoryName(System.AppDomain.CurrentDomain.BaseDirectory);
                caminhoArquivo = Directory.GetParent(Directory.GetParent(caminhoArquivo).FullName).FullName;
                caminhoArquivo += @"\TagsPendente.xml";

                XmlSerializer ser = new XmlSerializer(typeof(TagsPendente));
                TextReader textReader = (TextReader)new StreamReader(caminhoArquivo);
                XmlTextReader reader = new XmlTextReader(textReader);
                reader.Read();

                TagsPendente tags = (TagsPendente)ser.Deserialize(reader);

                tags.Tag = tags.Tag.ConvertAll(d => d.ToUpper());

                return tags;
            }
            catch (Exception ex)
            {
                string message = $"Tags - Algum erro aconteceu na leitura das tags pendentes.";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Error);
                message = $"Tags - Algum erro aconteceu na leitura das tags pendentes: {ex.GetAllMessages()}";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Erro, logLevel: LogLevel.Error);
                
                return new TagsPendente();
            }

        }

        public Users LerArqUsuarios()
        {
            try
            {
                string caminhoArquivo = Path.GetDirectoryName(System.AppDomain.CurrentDomain.BaseDirectory);
                caminhoArquivo = Directory.GetParent(Directory.GetParent(caminhoArquivo).FullName).FullName;
                caminhoArquivo += @"\Users.xml";

                XmlSerializer ser = new XmlSerializer(typeof(Users));
                TextReader textReader = (TextReader)new StreamReader(caminhoArquivo);
                XmlTextReader reader = new XmlTextReader(textReader);
                reader.Read();

                Users usu = (Users)ser.Deserialize(reader);

                return usu;
            }
            catch (Exception ex)
            {
                string message = $"Usuarios - Algum erro aconteceu na leitura dos usuarios.";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Error);
                message = $"Usuarios - Algum erro aconteceu na leitura dos usuarios: {ex.GetAllMessages()}";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Erro, logLevel: LogLevel.Error);

                return new Users();
            }

        }

    }
}
