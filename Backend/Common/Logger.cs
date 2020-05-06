using log4net.Layout;
using log4net.Appender;
using log4net.Core;
using log4net.Config;
using log4net;

namespace WarshopCommon {
    public class Logger
    {
        internal ILog log;

        internal Logger(string t)
        {
            log = LogManager.GetLogger("Warshop", t);
        }

        internal void Info(object message)
        {
            log.Info(message);
        }

        internal void Error(object message)
        {
            log.Error(message);
        }

        internal void Fatal(object message)
        {
            log.Fatal(message);
        }

        internal void Setup()
        {
            PatternLayout fileLayout = new PatternLayout
            {
                ConversionPattern = "%date %-5level %12logger - %message%newline"
            };
            fileLayout.ActivateOptions();
            RollingFileAppender appender = new RollingFileAppender
            {
                AppendToFile = true,
                File = GameConstants.APP_LOG_DIR + "/server.log",
                Layout = fileLayout,
                MaxSizeRollBackups = 5,
                MaximumFileSize = "1GB",
                RollingStyle = RollingFileAppender.RollingMode.Size,
                StaticLogFileName = true,
                Threshold = Level.Info
            };
            appender.ActivateOptions();
            BasicConfigurator.Configure(log.Logger.Repository);
        }

        internal void ConfigureNewGame(string gameSessionId)
        {
            PatternLayout layout = new PatternLayout
            {
                ConversionPattern = "%date %-5level %12logger - %message%newline"
            };
            layout.ActivateOptions();
            FileAppender appender = new FileAppender
            {
                AppendToFile = true,
                File = GameConstants.APP_LOG_DIR + "/"+ gameSessionId +".log",
                Layout = layout,
                Threshold = Level.Info
            };
            appender.ActivateOptions();
            BasicConfigurator.Configure(log.Logger.Repository);
        }

        internal static void RemoveGame()
        {
            IAppender[] apps = LogManager.GetRepository("Warshop").GetAppenders();
            foreach(IAppender app in apps)
            {
                if (app is FileAppender) app.Close();
            }
        }

    }

}
