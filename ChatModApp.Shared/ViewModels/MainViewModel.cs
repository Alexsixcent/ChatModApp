using ReactiveUI;

namespace ChatModApp.Shared.ViewModels;

public class MainViewModel : ReactiveObject, IScreen, IActivatableViewModel
{
    public RoutingState Router { get; }
    public ViewModelActivator Activator { get; }
    
    public MainViewModel()
    {
        Router = new();
        Activator = new();
    }
}