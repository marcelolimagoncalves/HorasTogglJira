using System.Linq;
using System.Reflection;
using System.ServiceProcess;

namespace TogglJiraService
{
    /// <summary>
    /// Classe que realiza a interpretação dos argumentos recebidos pelo programa.
    /// </summary>
    public static class ProgramArgs
    {
        /// <summary>
        /// Nome de identificação do serviço, deve ser o mesmo que o registrado no Windows.
        /// </summary>
        private const string SERVICE_NAME = "Toggl Jira Integracao";

        /// <summary>
        /// Interpreta os argumentos fornecidos.
        /// </summary>
        /// <param name="args">Argumentos a serem interpretados.</param>
        public static void Parse(string[] args)
        {
            /* verifica se o serviço atual já existe */
            ServiceController svc = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == SERVICE_NAME);

            string parameter = string.Concat(args);

            switch (parameter.ToLower())
            {
                /* instalação do serviço */
                case "--install":

                    /* caso o serviço já exista, irá recriá-lo por questões de bom senso.
                     * pode ser que o usuário tenha instalado o serviço em um diretório diferente da instalação anterior. */
                    if (svc != null)
                    {
                        /* se estiver executando, manda parar e aguarda o estado desejado */
                        if (svc.Status == ServiceControllerStatus.Running)
                        {
                            svc.Stop();
                            svc.WaitForStatus(ServiceControllerStatus.Stopped);
                        }

                        /* desinstala o serviço */
                        System.Configuration.Install.ManagedInstallerClass.InstallHelper(new string[] { "/u", Assembly.GetExecutingAssembly().Location });
                    }

                    /* instala o serviço */
                    System.Configuration.Install.ManagedInstallerClass.InstallHelper(new string[] { Assembly.GetExecutingAssembly().Location });

                    /* inicia o serviço após a instalação */
                    svc = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == SERVICE_NAME);
                    svc.Start();

                    break;

                /* desinstalação do serviço */
                case "--uninstall":

                    /* verifica se o serviço está instalado */
                    if (svc != null)
                    {
                        /* se estiver executando, manda parar e aguarda o estado desejado */
                        if (svc.Status == ServiceControllerStatus.Running)
                        {
                            svc.Stop();
                            svc.WaitForStatus(ServiceControllerStatus.Stopped);
                        }

                        /* desinstala o serviço */
                        System.Configuration.Install.ManagedInstallerClass.InstallHelper(new string[] { "/u", Assembly.GetExecutingAssembly().Location });
                    }

                    break;
            }
        }
    }
}