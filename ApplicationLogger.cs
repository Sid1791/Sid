using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomReporting
{
    public static class ApplicationLog
    {
        // private static LoggingConfiguration config = new LoggingConfiguration();
        //private static FileTarget logfile = new FileTarget("logfile") { FileName = "file.txt" };
        //private static ConsoleTarget logconsole = new ConsoleTarget("logconsole");
        private static Logger log;

        static ApplicationLog()
        {
            // Rules for mapping loggers to targets            
            // config.AddRule(LogLevel.Info, LogLevel.Fatal, logconsole);
            //config.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile);
            //LogManager.Configuration = config;
            LogManager.LoadConfiguration("NLog.config");
            log = LogManager.GetCurrentClassLogger();


        }
        public static void Info(string msg)
        {
            log.Info(msg);

        }
        public static void Debug(string msg)
        {

            log.Debug(msg);

        }
        public static void Error(string msg)
        {

            log.Error(msg);

        }
        public static void Warn(string msg)
        {
            log.Warn(msg);

        }
        //public static void D365WriteLog(string applicationName, DateTime executionTime, string executionLog, string status)
        //{
        //    try
        //    {
        //        ApplicationExecutionLog log = new ApplicationExecutionLog();
        //        log.application_name = applicationName;
        //        log.execution_time = executionTime;
        //        log.execution_log = executionLog;
        //        log.status_code = status;
        //        CRMServices.Create(log);
        //    }
        //    catch (Exception)
        //    {
        //        throw;
        //    }
        //}
    }
}
