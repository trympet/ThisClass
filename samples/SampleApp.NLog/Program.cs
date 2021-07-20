using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;
using System;

namespace SampleApp.NLog
{
    class Program
    {
        static void Main(string[] args)
        {
            LogSynchronouslyToConsole();
            try
            {
                new Demo1();
                new Demo2();
            }
            finally
            {
                LogManager.Flush();
                Console.ReadKey();
            }
        }

        private static void LogSynchronouslyToConsole()
        {
            LogManager.Setup().LoadConfiguration(c =>
            {
                var config = c.Configuration;
                var target = new ConsoleTarget("default");
                config.AddTarget(new AsyncTargetWrapper(target, 0, AsyncTargetWrapperOverflowAction.Block) { Name = "a" });
                config.AddTarget(target);
                config.LoggingRules.Add(new LoggingRule("*", LogLevel.Trace, LogLevel.Fatal, target));
            });
        }
    }

    [ThisClass]
    partial class Demo1
    {
        public Demo1()
        {
            // Extention method instantiation
            var x = this.GetCurrentClassLogger();
            x.Trace("Hello World from {}", ThisClass.FullName);
        }
    } 

    [ClassLogger]
    partial class Demo2
    {
        public Demo2()
        {
            try
            {
                Logger.Trace("Hello World from {}", ThisClass.FullName);
                DoImportantWork();
                return;
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        private static void DoImportantWork()
        {
            throw new Exception("Whoops!");
        }
    }
}
   