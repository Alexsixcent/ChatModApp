using Avalonia;
using Avalonia.Logging;

namespace ChatModApp.Tools;

public static class LoggingExtensions
{
    public static AppBuilder LogToSerilog(
        this AppBuilder builder,
        LogEventLevel level = LogEventLevel.Warning,
        params string[] areas)
    {
        Logger.Sink = new SerilogSink(areas, level);
        return builder;
    }
}