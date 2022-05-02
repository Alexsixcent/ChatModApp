using Avalonia.Controls;
using Avalonia.Logging;

namespace ChatModApp.Tools;

public static class LoggingExtensions
{
    public static T LogToSerilog<T>(
        this T builder,
        LogEventLevel level = LogEventLevel.Warning,
        params string[] areas)
        where T : AppBuilderBase<T>, new()
    {
        Logger.Sink = new SerilogSink(areas, level);
        return builder;
    }
}