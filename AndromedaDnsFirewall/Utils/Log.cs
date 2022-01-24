using AndromedaDnsFirewall.main;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndromedaDnsFirewall.Utils
{
    public class LogWrapper
    {
        Logger logger = null;
        string IdLog;

        public static LogWrapper Empty()
        {
            return new LogWrapper();
        }
        private LogWrapper()
        {
        }
        public LogWrapper(string id)
        {
            string folder = null;
            var split = id.Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);

            if (split.Length == 2)
            {
                id = split[1];
                folder = split[0];
            }
            else if (split.Length > 2)
            {
                throw new NotImplementedException();
            }

            var path = $"{ProgramUtils.BinFolder}/logs/";
            if (folder != null)
            {
                path = $"{path}{folder}/";
                System.IO.Directory.CreateDirectory(path);
            }

            IdLog = id;
            try
            {
                var NLogTarget = LogManager.Configuration.FindTargetByName(id);

                if (NLogTarget == null) //Don't Re Add the Target
                {
                    NLogTarget = new FileTarget()
                    {
                        Name = id,
                        //FileName = $"{path}${{shortdate}}_{id}.log",
                        FileName = $"{path}{id}.log",
                        Encoding = Encoding.UTF8,
                        Layout = @"${date:format=dd.MM.yy HH\:mm\:ss.fff} ${processid:padding=6} ${threadid:padding=2} ${message}",
                        ArchiveAboveSize = 7864320,
                        MaxArchiveFiles = 5,
                        LineEnding = LineEndingMode.LF
                    };

                    AsyncTargetWrapper asyncWrapper = new();
                    asyncWrapper.QueueLimit = 5000;
                    asyncWrapper.OverflowAction = AsyncTargetWrapperOverflowAction.Block;
                    asyncWrapper.WrappedTarget = NLogTarget;

                    LogManager.Configuration.AddTarget(id, asyncWrapper);

                    var NLogRule = new LoggingRule(id, LogLevel.Info, LogLevel.Error, asyncWrapper);
                    NLogRule.EnableLoggingForLevel(LogLevel.Info);
                    NLogRule.EnableLoggingForLevel(LogLevel.Warn);
                    NLogRule.EnableLoggingForLevel(LogLevel.Error);
                    LogManager.Configuration.LoggingRules.Add(NLogRule);

                    LogManager.ReconfigExistingLoggers();
                }

                logger = LogManager.GetLogger(id);
            }
            catch { }
        }

        public void PrintLine(string str)
        {
            if (logger == null)
                return;
            logger.Info(str);
        }
    }

    internal static class Log
    {
        static Log()
        {
            LogManager.Configuration = new LoggingConfiguration();
        }

        static ConcurrentDictionary<string, LogWrapper> dictLog = new();
        static LogWrapper GetWrap(string file)
        {
            if (!dictLog.TryGetValue(file, out LogWrapper lw))
            {
                lock (dictLog)
                {
                    if (!dictLog.TryGetValue(file, out lw))
                    {
                        lw = new LogWrapper(file);
                        dictLog[file] = lw;
                    }
                }
            }
            return lw;
        }

        static void LogBase(string file, string info)
        {
            if (!Config.Inst.DebugLog)
                return;
            GetWrap(file).PrintLine(info);
        }

        public static void Info(string info)
        {
            LogBase("main", info);
        }

        public static void Err(Exception ex)
        {
            LogBase("main", $"ERROR: {ex.Message}");
            LogBase("err", $"--------------------------\n{ex}");
        }
    }
}
