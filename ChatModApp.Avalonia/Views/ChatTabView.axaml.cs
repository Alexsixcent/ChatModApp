using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using Avalonia.VisualTree;
using ChatModApp.Shared.ViewModels;
using FluentAvalonia.Core.ApplicationModel;
using FluentAvalonia.UI.Controls;
using ReactiveUI;

namespace ChatModApp.Views;

public partial class ChatTabView : ReactiveUserControl<ChatTabViewModel>
{
    public ChatTabView()
    {
        this.WhenActivated(disposable =>
        {
            Observable.FromEventPattern<TabViewTabCloseRequestedEventArgs>(TabViewControl,
                                                                           nameof(TabViewControl.TabCloseRequested))
                      .Select(pattern => pattern.EventArgs.Item)
                      .InvokeCommand(ViewModel, vm => vm.CloseTabCommand)
                      .DisposeWith(disposable);

            this.BindCommand(ViewModel, vm => vm.AddTabCommand, v => v.TabViewControl,
                             nameof(TabViewControl.AddTabButtonClick))
                .DisposeWith(disposable);

            this.Bind(ViewModel, vm => vm.OpenedTabIndex, v => v.TabViewControl.SelectedIndex)
                .DisposeWith(disposable);
        });
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // Changed for SplashScreens:
        // -- If using a SplashScreen, the window will be available when this is attached
        //    and we can just call OnParentWindowOpened
        // -- If not using a SplashScreen (like before), the window won't be initialized
        //    yet and setting our custom titlebar won't work... so wait for the 
        //    WindowOpened event first
        if (e.Root is not Window b)
            return;
        if (!b.IsActive)
            b.Opened += OnParentWindowOpened;
        else
            OnParentWindowOpened(b, null);
    }

    private void OnParentWindowOpened(object? sender, EventArgs? e)
    {
        if (e is not null && sender is Window win)
            win.Opened -= OnParentWindowOpened;

        if (sender is not CoreWindow cw)
            return;
        
        var titleBar = cw.TitleBar;
        if (titleBar is null) return;
            
        titleBar.LayoutMetricsChanged += OnTitleBarLayoutMetricsChanged;

        cw.SetTitleBar(OverlayInsetHost);
        OnTitleBarLayoutMetricsChanged(titleBar);
        
        //Finds the existing default title bar in the visual tree
        var def = cw.GetVisualDescendants()
                    .OfType<Panel>()
                    .FirstOrDefault(panel => panel.Name is "DefaultTitleBar");
        
        OverlayInsetHost.MinHeight = def?.Height ?? titleBar.Height;
    }

    private void OnTitleBarLayoutMetricsChanged(CoreApplicationViewTitleBar bar, EventArgs? _ = null)
    {
        OverlayInsetHost.Margin = new(0, 0, bar.SystemOverlayRightInset, 0);
    }
}