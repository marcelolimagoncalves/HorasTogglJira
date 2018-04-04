using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using TogglJiraConsole.JiraModel;
using TogglJiraConsole.TogglModel;
using TogglJiraConsole.UtilModel;
using TogglJiraConsole.XmlModel;

namespace TogglJiraConsole
{
    public class Service
    {
        private static bool running = false;
        private static bool setinterval = true;

        private System.Timers.Timer _timer;
        private RequisicaoHttp requisicaoHttp;

        public Service()
        {
            requisicaoHttp = new RequisicaoHttp();
            _timer = new System.Timers.Timer(1000);
            _timer.Elapsed += timer_Elapsed;
        }

        static DateTime TimeStarterRun = DateTime.ParseExact(ConfigurationManager.AppSettings["TimeStarterRun"], "HH:mm", CultureInfo.InvariantCulture);
        static DateTime TimeEndRun = DateTime.ParseExact(ConfigurationManager.AppSettings["TimeEndRun"], "HH:mm", CultureInfo.InvariantCulture);
        private void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (setinterval)
            {
                _timer.Interval = 660000;
                setinterval = false;
            }

            var dataInicio = new DateTime(day: DateTime.Now.Day, month: DateTime.Now.Month, year: DateTime.Now.Year, hour: TimeStarterRun.Hour,
                minute: TimeStarterRun.Minute, second: TimeStarterRun.Second);
#if DEBUG
            RunService r = new RunService();
            r.Run();
#else
            if (Convert.ToDateTime(DateTime.Now.ToString("dd/MM/yyyy HH:mm")) == Convert.ToDateTime(dataInicio.ToString("dd/MM/yyyy HH:mm")))
            {
                if (!running)
                {
                    RunService r = new RunService();
                    r.Run();
                }
            }
#endif
        }


        public async Task Start()
        {

            string[] prefixes = new string[1];
            prefixes[0] = "http://localhost:1302/cadastro/";
            Task.Run(() => requisicaoHttp.IniciarServidorHttp(prefixes));
           
           
            _timer.Start();
        }

        public void Stop()
        {
            requisicaoHttp.FecharServidorHttp();
            _timer.Stop();
        }
    }
}