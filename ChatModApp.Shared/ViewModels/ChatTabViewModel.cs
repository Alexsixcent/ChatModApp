using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ChatModApp.Shared.Services;
using DryIoc;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ChatModApp.Shared.ViewModels;

public class ChatTabViewModel : ReactiveObject, IRoutableViewModel, IActivatableViewModel, IDisposable
{
    public string UrlPathSegment => "chatTabs";
    public IScreen? HostScreen { get; set; }
    public ViewModelActivator Activator { get; }

    public ReactiveCommand<Unit, Unit> AddTabCommand { get; }
    public ReactiveCommand<ChatTabItemViewModel, Unit> CloseTabCommand { get; }

    [Reactive] public int OpenedTabIndex { get; private set; }
    public ObservableCollectionExtended<ChatTabItemViewModel> ChatTabs { get; set; }

    private readonly CompositeDisposable _disposable;
    private readonly ChatTabService _tabService;
    private readonly IContainer _container;

    public ChatTabViewModel(ChatTabService tabService, IContainer container)
    {
        _tabService = tabService;
        _container = container;
        _disposable = new();
        Activator = new();
        ChatTabs = new();

        AddTabCommand = ReactiveCommand.Create(AddTab).DisposeWith(_disposable);
        CloseTabCommand = ReactiveCommand.Create<ChatTabItemViewModel>(RemoveTab).DisposeWith(_disposable);

        this.WhenActivated(disposable =>
        {
            _tabService.Tabs
                       .Cast(item => (ChatTabItemViewModel)item)
                       .ObserveOn(RxApp.MainThreadScheduler)
                       .Bind(ChatTabs)
                       .Subscribe()
                       .DisposeWith(disposable);

            foreach (var tab in _tabService.TabCache.Items)
                if (tab is ChatTabItemViewModel item)
                    item.Activator.Activate().DisposeWith(_disposable);
        });
    }

    public void Dispose() => _disposable.Dispose();

    private void AddTab()
    {
        var newTab = _container.Resolve<ChatTabItemViewModel>();
        newTab.Title = "New tab";

        _tabService.AddTab(newTab);
        OpenedTabIndex = ChatTabs.Count - 1;

        newTab.Activator.Activate().DisposeWith(_disposable);
    }

    private void RemoveTab(ChatTabItemViewModel tab)
    {
        _tabService.RemoveTab(tab.Id);
        tab.Dispose();
    }
}