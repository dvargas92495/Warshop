using Aws.GameLift;
using UnityEngine;
using UnityEngine.Networking;
using log4net.Layout;
using log4net.Appender;
using log4net.Core;
using log4net.Config;
using log4net;

class Logger {

    internal ILog log;
    private static bool configured;

    internal Logger(string t)
    {
        log = LogManager.GetLogger(t);
    }

    internal void Info(NetworkMessage netMsg, string msg)
    {
        int cid = -1;
        if (netMsg.conn != null)
        {
            cid = netMsg.conn.connectionId;
        }
        Info(msg + ": " + cid);
    }

    internal void Info(object message)
    {
        log.Info(message);
    }

    internal void Error(GenericOutcome outcome)
    {
        Error(outcome.Error.ErrorName + " - " + outcome.Error.ErrorMessage);
    }

    internal void Error(object message)
    {
        log.Error(message);
    }

    internal void Fatal(ZException e)
    {
        log.Fatal(e.GetType() + " - " + e.Message + ":\n\t" + e.StackTrace);
    }

    internal void Fatal(object message)
    {
        log.Fatal(message);
    }

    internal static void Setup(bool isServer)
    {
        if (configured) return;
        if (Application.isEditor)
        {
            PatternLayout editorLayout = new PatternLayout
            {
                ConversionPattern = "%logger - %message%newline"
            };
            editorLayout.ActivateOptions();
            UnityAppender unityAppender = new UnityAppender
            {
                Layout = editorLayout
            };
            unityAppender.ActivateOptions();
            BasicConfigurator.Configure(unityAppender);
        }
        else
        {
            PatternLayout fileLayout = new PatternLayout
            {
                ConversionPattern = "%date %-5level %12logger - %message%newline"
            };
            fileLayout.ActivateOptions();
            RollingFileAppender appender = new RollingFileAppender
            {
                AppendToFile = true,
                File = GameConstants.APP_LOG_DIR + (isServer ? "/server.log" : "/client.log"),
                Layout = fileLayout,
                MaxSizeRollBackups = 5,
                MaximumFileSize = "1GB",
                RollingStyle = RollingFileAppender.RollingMode.Size,
                StaticLogFileName = true,
                Threshold = Level.Info
            };
            appender.ActivateOptions();
            BasicConfigurator.Configure(appender);
        }
        configured = true;
    }

    internal static void ConfigureNewGame(string gameSessionId)
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
        BasicConfigurator.Configure(appender);
    }

    internal static void RemoveGame()
    {
        IAppender[] apps = LogManager.GetRepository().GetAppenders();
        foreach(IAppender app in apps)
        {
            if (app is FileAppender) app.Close();
        }
    }

    private class UnityAppender : AppenderSkeleton
    {
        protected override void Append(LoggingEvent loggingEvent)
        {
            string message = RenderLoggingEvent(loggingEvent);

            if (Level.Compare(loggingEvent.Level, Level.Error) >= 0) Debug.LogError(message);
            else if (Level.Compare(loggingEvent.Level, Level.Warn) >= 0) Debug.LogWarning(message);
            else Debug.Log(message);
        }
    }

}
