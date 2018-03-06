using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TogglJiraConsole.LogModel;

namespace TogglJiraConsole.UtilModel
{
    public class Util
    {
        private Log log;
        public Util()
        {
            //log = new Log();
            log = Log.Instance;
        }

        public string MilisecondsToJiraFormat(int mili)
        {
            try
            {
                var weeks = 0;
                var days = 0;
                var hours = 0;
                var minutes = (mili / 1000) / 60;
                if (minutes < 1)
                {
                    return string.Empty;
                }

                if (minutes >= 10080)
                {
                    while (minutes >= 10080)
                    {
                        weeks++;
                        minutes = minutes - 10080;
                    }
                }

                if (minutes >= 1440)
                {
                    while (minutes >= 1440)
                    {
                        days++;
                        minutes = minutes - 1440;
                    }
                }

                if (minutes >= 60)
                {

                    while (minutes >= 60)
                    {
                        hours++;
                        minutes = minutes - 60;
                    }
                }

                string ret = string.Empty;
                if (weeks > 0)
                {
                    ret = string.Format("{0}w ", weeks);
                }
                if (days > 0)
                {
                    ret = ret + string.Format("{0}d ", days);
                }
                if (hours > 0)
                {
                    ret = ret + string.Format("{0}h ", hours);
                }
                if (minutes >= 1)
                {
                    ret = ret + string.Format("{0}m", minutes);
                }

                return ret;
            }
            catch (Exception ex)
            {
                string message = $"Formatar horário - Algum erro aconteceu na conversão do tempo total gasto do toggl para o jira.";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Error);
                message = $"Formatar horário - Algum erro aconteceu na conversão do tempo total gasto do toggl para o jira: {ex.GetAllMessages()}";
                log.InserirSalvarLog(message: message, arqLog: ArqLog.Principal, logLevel: LogLevel.Error);

                return string.Empty;
            }
        }
    }
}
