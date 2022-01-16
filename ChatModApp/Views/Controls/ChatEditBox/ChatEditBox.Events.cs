using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ChatModApp.Views.Controls.ChatEditBox;

/// <summary>
///     The RichSuggestBox control extends <see cref="RichEditBox" /> control that suggests and embeds custom data in a
///     rich document.
/// </summary>
public partial class ChatEditBox
{
    /// <summary>
    ///     Event raised when the control needs to show suggestions.
    /// </summary>
    public event TypedEventHandler<ChatEditBox, SuggestionRequestedEventArgs> SuggestionRequested;

    /// <summary>
    ///     Event raised when user click on a suggestion.
    ///     This event lets you customize the token appearance in the document.
    /// </summary>
    public event TypedEventHandler<ChatEditBox, SuggestionChosenEventArgs> SuggestionChosen;

    /// <summary>
    ///     Event raised when a token is fully highlighted.
    /// </summary>
    public event TypedEventHandler<ChatEditBox, ChatEditTokenSelectedEventArgs> TokenSelected;

    /// <summary>
    ///     Event raised when a pointer is hovering over a token.
    /// </summary>
    public event TypedEventHandler<ChatEditBox, ChatEditTokenPointerOverEventArgs> TokenPointerOver;

    /// <summary>
    ///     Event raised when text is changed, either by user or by internal formatting.
    /// </summary>
    public event TypedEventHandler<ChatEditBox, RoutedEventArgs> TextChanged;

    /// <summary>
    ///     Event raised when the text selection has changed.
    /// </summary>
    public event TypedEventHandler<ChatEditBox, RoutedEventArgs> SelectionChanged;

    /// <summary>
    ///     Event raised when text is pasted into the control.
    /// </summary>
    public event TypedEventHandler<ChatEditBox, TextControlPasteEventArgs> Paste;
}