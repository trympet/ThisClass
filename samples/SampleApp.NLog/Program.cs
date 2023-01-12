using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;
using System;
using SomeNamespace;

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
            LogManager.GetCurrentClassLogger().Trace("Hello World from {}", ThisClass.FullName);
        }
    }

    namespace AnotherNamespace
    {
        using SomeOtherNamespace;
        [ClassLoggerLazy]
        partial class Demo2<T> : SomeInterface<T> where T : SomeOtherInterface
        {
            public static void SayHello()
            {
                Logger.Info("Hei på deg");
            }

            [ClassLogger]
            internal partial class NestedClass : SomeInterface<SomeOtherInterface>
            {
            }
        }
    }
}
