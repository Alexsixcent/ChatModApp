﻿using System;
using Avalonia;
using Avalonia.Logging;
using Avalonia.ReactiveUI;
using ChatModApp.Tools;

namespace ChatModApp;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
                         .UsePlatformDetect()
#if DEBUG
                         .LogToSerilog(LogEventLevel.Debug,
                                       LogArea.Control, LogArea.Layout, LogArea.Binding, LogArea.Property,
                                       LogArea.Visual)
#endif

                         .UseReactiveUI();
    }
}