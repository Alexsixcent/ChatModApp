using Windows.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ReactiveUI;

namespace ChatModApp.Tools
{
    public class ReactiveTabViewItem<TViewModel> :
        TabViewItem, IViewFor<TViewModel>
        where TViewModel : class
    {
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
            "ViewModel",
            typeof(TViewModel),
            typeof(ReactiveTabViewItem<TViewModel>),
            new PropertyMetadata(null));

        public TViewModel ViewModel
        {
            get => (TViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (TViewModel)value;
        }
    }
}