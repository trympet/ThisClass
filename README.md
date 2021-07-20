# ThisClass
Exposes class and type information as constants in the ThisClass class using source generators powered by Roslyn, inspired by ThisAssembly.

## ThisClass
```csharp
[ThisClass]
partial class Demo
{
    public Demo()
    {
        Logger.Info($"Hello from {ThisClass.FullName}");
    }
}
```


## NLog.Extensions.ThisClass
Create class loggers without the use of reflection.
```csharp
[ClassLogger]
partial class Demo
{
    public Demo()
    {
        Logger.Info("Hello World!");
    }
}
```

You can also instantiate new class loggers with the `this.GetCurrentClassLogger()` extension method.
```csharp
var logger2 = this.GetCurrentClassLogger();
```


Looks like this behind the scenes
```csharp
public static Logger GetCurrentClassLogger<T>(this T sender)
            where T : global::ThisClassAttribute.IThisClass
            => global::NLog.LogManager.GetLogger(sender.ClassFullName);

namespace SampleApp.NLog
{
    partial class Demo
    {
        private static readonly global::NLog.Logger Logger = global::NLog.LogManager.GetLogger(ThisClass.FullName);
    }
    
    partial class Demo
    {
        public static partial class ThisClass
        {
            /// <summary>
            /// Gets the fully qualified name of the parent class, including the namespace but not the assembly.
            /// </summary>
            public const string FullName = "SampleApp.NLog.Demo";
        }
    }
    
    partial class Demo1 : global::ThisClassAttribute.IThisClass
    {
        string global::ThisClassAttribute.IThisClass.ClassFullName => ThisClass.FullName;
    }
}
```
