using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Mixins;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using ChatModApp.Shared.ViewModels;
using FluentAvalonia.Styling;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Media;
using ReactiveUI;

namespace ChatModApp.Views;

public partial class MainWindow : CoreWindow, IViewFor<MainViewModel>
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

    public MainWindow()
    {
        this.WhenActivated(disposable =>
        {
            this.OneWayBind(ViewModel, vm => vm.Router, v => v.RoutedViewHost.Router)
                .DisposeWith(disposable);
        });
            
        this.GetObservable(DataContextProperty).Subscribe(OnDataContextChanged);
        this.GetObservable(ViewModelProperty).Subscribe(OnViewModelChanged);
            
        InitializeComponent();
            
        //TODO: Set new splashscreen here

#if DEBUG
        this.AttachDevTools();
#endif

        MinWidth = 100;
        MinHeight = 200;
    }
        
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

        var theme = AvaloniaLocator.Current.GetService<FluentAvaloniaTheme>() ?? throw new PlatformNotSupportedException();
        theme.RequestedThemeChanged += OnRequestedThemeChanged;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && IsWindows11 && theme.RequestedTheme != FluentAvaloniaTheme.HighContrastModeString)
        {
            TransparencyBackgroundFallback = Brushes.Transparent;
            TransparencyLevelHint = WindowTransparencyLevel.Mica;
                
            TryEnableMicaEffect(theme);
        }
            
        theme.ForceWin32WindowToTheme(this);

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
            
        TitleBar.ExtendViewIntoTitleBar = true;
    }

    private void OnRequestedThemeChanged(FluentAvaloniaTheme sender, RequestedThemeChangedEventArgs args)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) 
            return;
            
        if (IsWindows11 && args.NewTheme != FluentAvaloniaTheme.HighContrastModeString)
        {
            TryEnableMicaEffect(sender);
        }
        else if (args.NewTheme == FluentAvaloniaTheme.HighContrastModeString)
        {
            // Clear the local value here, and let the normal styles take over for HighContrast theme
            SetValue(BackgroundProperty, AvaloniaProperty.UnsetValue);
        }
    }
        
    private void TryEnableMicaEffect(FluentAvaloniaTheme thm)
    {

        // The background colors for the Mica brush are still based around SolidBackgroundFillColorBase resource
        // BUT since we can't control the actual Mica brush color, we have to use the window background to create
        // the same effect. However, we can't use SolidBackgroundFillColorBase directly since its opaque, and if
        // we set the opacity the color become lighter than we want. So we take the normal color, darken it and 
        // apply the opacity until we get the roughly the correct color
        // NOTE that the effect still doesn't look right, but it suffices. Ideally we need access to the Mica
        // CompositionBrush to properly change the color but I don't know if we can do that or not
        if (thm.RequestedTheme == FluentAvaloniaTheme.DarkModeString)
        {
            var color = this.TryFindResource("SolidBackgroundFillColorBase", out var value) ? (Color2)(Color)value : new Color2(32, 32, 32);

            color = color.LightenPercent(-0.8f);

            Background = new ImmutableSolidColorBrush(color, 0.78);
        }
        else if (thm.RequestedTheme == FluentAvaloniaTheme.LightModeString)
        {
            // Similar effect here
            var color = this.TryFindResource("SolidBackgroundFillColorBase", out var value) ? (Color2)(Color)value : new Color2(243, 243, 243);

            color = color.LightenPercent(0.5f);

            Background = new ImmutableSolidColorBrush(color, 0.9);
        }
    }
}