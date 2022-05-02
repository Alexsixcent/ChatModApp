using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Avalonia.Logging;
using Serilog;

namespace ChatModApp;

[SuppressMessage("ReSharper", "TemplateIsNotCompileTimeConstantProblem")]
public class SerilogSink : ILogSink
{
    private ILogger Logger => _logger ?? Serilog.Log.Logger;

    private readonly string[] _areas;
    private readonly LogEventLevel _minLvl;
    private readonly ILogger? _logger;


    public SerilogSink(string[] areas, LogEventLevel minLvl, ILogger? logger = null)
    {
        _logger = logger;
        _areas = areas;
        _minLvl = minLvl;
    }

    public bool IsEnabled(LogEventLevel level, string area)
    {
        return IsWriting(level, area) && Logger.IsEnabled(ToSeriLogEventLevel(level));
    }

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate)
    {
        if (IsWriting(level, area))
        {
            Logger.Write(ToSeriLogEventLevel(level), "[{Area}] " + messageTemplate, area, source);
        }
    }

    public void Log<T0>(LogEventLevel level, string area, object? source, string messageTemplate, T0 propertyValue0)
    {
        if (IsWriting(level, area))
        {
            Logger.Write(ToSeriLogEventLevel(level), "[{Area}] " + messageTemplate, area, propertyValue0);
        }
    }

    public void Log<T0, T1>(LogEventLevel level, string area, object? source, string messageTemplate, T0 propertyValue0,
                            T1 propertyValue1)
    {
        if (IsWriting(level, area))
        {
            Logger.Write(ToSeriLogEventLevel(level), "[{Area}] " + messageTemplate, area, propertyValue0,
                         propertyValue1);
        }
    }

    public void Log<T0, T1, T2>(LogEventLevel level, string area, object? source, string messageTemplate,
                                T0 propertyValue0, T1 propertyValue1, T2 propertyValue2)
    {
        if (IsWriting(level, area))
        {
            Logger.Write(ToSeriLogEventLevel(level), "[{Area}] " + messageTemplate,
                         area, propertyValue0, propertyValue1, propertyValue2);
        }
    }

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate,
                    params object?[] propertyValues)
    {
        if (IsWriting(level, area))
        {
            Logger.Write(ToSeriLogEventLevel(level), "[{Area}] " + messageTemplate,
                         propertyValues.Prepend(area).ToArray());
        }
    }

    private bool IsWriting(LogEventLevel lvl, string area) => lvl >= _minLvl && _areas.Contains(area);

    private static Serilog.Events.LogEventLevel ToSeriLogEventLevel(LogEventLevel level) => level switch
    {
        LogEventLevel.Verbose => Serilog.Events.LogEventLevel.Verbose,
        LogEventLevel.Debug => Serilog.Events.LogEventLevel.Debug,
        LogEventLevel.Information => Serilog.Events.LogEventLevel.Information,
        LogEventLevel.Warning => Serilog.Events.LogEventLevel.Warning,
        LogEventLevel.Error => Serilog.Events.LogEventLevel.Error,
        LogEventLevel.Fatal => Serilog.Events.LogEventLevel.Fatal,
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
    };
}