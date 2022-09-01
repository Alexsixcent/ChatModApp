using Avalonia.ReactiveUI;
using ChatModApp.Shared.ViewModels;

namespace ChatModApp.Views;

public partial class EmotePicker : ReactiveUserControl<EmotePickerViewModel>
{
    public EmotePicker()
    {
        InitializeComponent();
    }
}