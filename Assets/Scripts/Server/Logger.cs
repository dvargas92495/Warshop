using Aws.GameLift;
using System;
using UnityEngine;
using UnityEngine.Networking;
using log4net.Layout;
using log4net.Appender;
using log4net.Core;
using log4net.Config;
using log4net;

class Logger {

    internal ILog log;

    internal Logger(Type t)
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

    internal static void Setup(bool isServer)
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

        PatternLayout fileLayout = new PatternLayout
        {
            ConversionPattern = "%date %-5level %12logger - %message%newline"
        };
        fileLayout.ActivateOptions();
        AppenderSkeleton appender;
        if (isServer)
        {
            // setup the appender that writes to Log\EventLog.txt
            appender = new RollingFileAppender
            {
                AppendToFile = false,
                File = GameConstants.APP_LOG_DIR + "/server.log",
                Layout = fileLayout,
                MaxSizeRollBackups = 5,
                MaximumFileSize = "1GB",
                RollingStyle = RollingFileAppender.RollingMode.Size,
                StaticLogFileName = true
            };
        } else
        {
            appender = new ConsoleAppender
            {
                Layout = fileLayout
            };
        }
        appender.ActivateOptions();
        BasicConfigurator.Configure(unityAppender, appender);
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
