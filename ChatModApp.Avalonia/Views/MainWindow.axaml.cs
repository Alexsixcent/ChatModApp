using System;
using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using ChatModApp.Shared.ViewModels;
using FluentAvalonia.Styling;
using FluentAvalonia.UI.Media;
using FluentAvalonia.UI.Windowing;
using ReactiveUI;
using Splat;

namespace ChatModApp.Views;

public partial class MainWindow : AppWindow, IViewFor<MainViewModel>
{
    public static readonly StyledProperty<MainViewModel?> ViewModelProperty =
        AvaloniaProperty.Register<MainWindow, MainViewModel?>(nameof(ViewModel));

    public MainViewModel? ViewModel
    {
        get => GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    object? IViewFor.ViewModel
    {
        get => ViewModel;
        set => ViewModel = (MainViewModel?)value;
    }
    
    public MainWindow(IApplicationSplashScreen splashScreen, MainViewModel mainViewModel)
    {
        TitleBar.TitleBarHitTestType = TitleBarHitTestType.Complex;
        this.WhenActivated(disposable =>
        {
            this.OneWayBind(ViewModel, vm => vm.Router, v => v.RoutedViewHost.Router)
                .DisposeWith(disposable);
        });

        this.GetObservable(DataContextProperty).Subscribe(OnDataContextChanged);
        this.GetObservable(ViewModelProperty).Subscribe(OnViewModelChanged);

        InitializeComponent();

        DataContext = mainViewModel;
        SplashScreen = splashScreen;

#if DEBUG
        this.AttachDevTools();
#endif

        MinWidth = 100;
        MinHeight = 200;
        
        Application.Current!.ActualThemeVariantChanged += OnActualThemeVariantChanged;
    }

    private void OnActualThemeVariantChanged(object? sender, EventArgs e)
    {
        if (OperatingSystem.IsWindows())
        {
            if (IsWindows11 && ActualThemeVariant != FluentAvaloniaTheme.HighContrastTheme)
            {
                TryEnableMicaEffect();
            }
            else if (ActualThemeVariant != FluentAvaloniaTheme.HighContrastTheme)
            {
                // Clear the local value here, and let the normal styles take over for HighContrast theme
                SetValue(BackgroundProperty, AvaloniaProperty.UnsetValue);
            }
        }
    }

    public MainWindow() : this(Locator.Current.GetService<IApplicationSplashScreen>()!,
                               Locator.Current.GetService<MainViewModel>()!)
    { }

    private void OnDataContextChanged(object? value)
    {
        if (value is MainViewModel viewModel)
            ViewModel = viewModel;
        else
            ViewModel = null;
    }

    private void OnViewModelChanged(object? value)
    {
        if (value is null)
            ClearValue(DataContextProperty);
        else if (DataContext != value)
            DataContext = value;
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        var theme = ActualThemeVariant;

        if (OperatingSystem.IsWindows() && IsWindows11 && theme != FluentAvaloniaTheme.HighContrastTheme)
        {
            TransparencyBackgroundFallback = Brushes.Transparent;
            TransparencyLevelHint = WindowTransparencyLevel.Mica;

            TryEnableMicaEffect();
        }

        var screen = Screens.ScreenFromVisual(this);
        // ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        // There is a missing Nullable attribute in the Avalonia API
        if (screen is null)
            return;
        // ReSharper restore ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

        Width = screen.WorkingArea.Width switch
        {
            > 1280 => 1280,
            > 1000 => 1000,
            > 700 => 700,
            > 500 => 500,
            _ => 450
        };

        Height = screen.WorkingArea.Height switch
        {
            > 720 => 720,
            > 600 => 600,
            > 500 => 500,
            _ => 400
        };
        
        TitleBar.ExtendsContentIntoTitleBar = true;
    }

    private void TryEnableMicaEffect()
    {
        // The background colors for the Mica brush are still based around SolidBackgroundFillColorBase resource
        // BUT since we can't control the actual Mica brush color, we have to use the window background to create
        // the same effect. However, we can't use SolidBackgroundFillColorBase directly since its opaque, and if
        // we set the opacity the color become lighter than we want. So we take the normal color, darken it and 
        // apply the opacity until we get the roughly the correct color
        // NOTE that the effect still doesn't look right, but it suffices. Ideally we need access to the Mica
        // CompositionBrush to properly change the color but I don't know if we can do that or not
        if (ActualThemeVariant == ThemeVariant.Dark)
        {
            var color = this.TryFindResource("SolidBackgroundFillColorBase",
                                             ThemeVariant.Dark, out var value) ? (Color2)(Color)value : new(32, 32, 32);

            color = color.LightenPercent(-0.8f);

            Background = new ImmutableSolidColorBrush(color, 0.78);
        }
        else if (ActualThemeVariant == ThemeVariant.Light)
        {
            // Similar effect here
            var color = this.TryFindResource("SolidBackgroundFillColorBase",
                                             ThemeVariant.Light, out var value) ? (Color2)(Color)value : new(243, 243, 243);

            color = color.LightenPercent(0.5f);

            Background = new ImmutableSolidColorBrush(color, 0.9);
        }
    }
}