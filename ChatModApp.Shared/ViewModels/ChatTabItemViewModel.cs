using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using System.Runtime.Serialization;
using ChatModApp.Shared.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;

namespace ChatModApp.Shared.ViewModels;

[DataContract]
public class ChatTabItemViewModel : ReactiveObject, IChatTabItem, IScreen, IActivatableViewModel, IDisposable,
                                    IEquatable<ChatTabItemViewModel>
{
    [DataMember] public Guid Id { get; set; }

    [Reactive] [DataMember] public string Title { get; set; }

    [Reactive] [DataMember] public ITwitchChannel? Channel { get; set; }

    public RoutingState Router { get; }

    public ViewModelActivator Activator { get; }


    private readonly CompositeDisposable _disposables;


    public ChatTabItemViewModel(ChatTabPromptViewModel? prompt = null)
    {
        _disposables = new();
        Id = Guid.NewGuid();
        Title = "ChatTab";
        Router = new();
        Activator = new();

        this.WhenActivated(() =>
        {
            NavigateImpl(prompt);

            return Enumerable.Empty<IDisposable>();
        });
    }

    public void Navigate() => NavigateImpl();

    public void Dispose() => _disposables.Dispose();


    public bool Equals(ChatTabItemViewModel? other)
    {
        if (ReferenceEquals(null, other)) return false;
        return ReferenceEquals(this, other) || Id.Equals(other.Id);
    }

    [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    private void NavigateImpl(ChatTabPromptViewModel? prompt = null)
    {
        if (Channel is not null)
        {
            var cvm = Locator.Current.GetService<ChatViewModel>() ?? throw new InvalidOperationException();
            cvm.HostScreen = this;
            cvm.Channel = Channel;
            Router.NavigateAndReset
                  .Execute(cvm)
                  .Subscribe()
                  .DisposeWith(_disposables);
            return;
        }

        var pvm = prompt ?? Locator.Current.GetService<ChatTabPromptViewModel>() ??
                  throw new InvalidOperationException();
        pvm.HostScreen = this;
        pvm.ParentTabId = Id;

        Router.NavigateAndReset
              .Execute(pvm)
              .Subscribe()
              .DisposeWith(_disposables);

        pvm.DisposeWith(_disposables);
    }
}