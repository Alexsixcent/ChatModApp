// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using Windows.UI.Text;

namespace ChatModApp.Views.Controls.ChatEditBox;

/// <summary>
///     Describes a suggestion token in the document.
/// </summary>
public class ChatSuggestToken : INotifyPropertyChanged
{
    /// <summary>
    ///     Gets the token ID.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    ///     Gets the start position of the text range.
    /// </summary>
    public int RangeStart { get; private set; }

    /// <summary>
    ///     Gets the end position of the text range.
    /// </summary>
    public int RangeEnd { get; private set; }

    /// <summary>
    ///     Gets the start position of the token in number of characters.
    /// </summary>
    public int Position => _range?.GetIndex(TextRangeUnit.Character) - 1 ?? 0;

    /// <summary>
    ///     Gets or sets the suggested item associated with this token.
    /// </summary>
    public object Item { get; set; }

    /// <summary>
    ///     Gets the text displayed in the document.
    /// </summary>
    public string DisplayText { get; }

    private ITextRange _range;

    internal bool Active { get; set; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ChatSuggestToken" /> class.
    /// </summary>
    /// <param name="id">Token ID</param>
    /// <param name="displayText">Text in the document</param>
    public ChatSuggestToken(Guid id, string displayText)
    {
        Id = id;
        DisplayText = displayText;
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler PropertyChanged;

    /// <inheritdoc />
    public override string ToString() =>
        $"HYPERLINK \"{Id}\"\u200B{DisplayText}\u200B";

    internal void UpdateTextRange(ITextRange range)
    {
        var rangeStartChanged = RangeStart != range.StartPosition;
        var rangeEndChanged = RangeEnd != range.EndPosition;
        var positionChanged = _range == null || rangeStartChanged;
        _range = range.GetClone();
        RangeStart = _range.StartPosition;
        RangeEnd = _range.EndPosition;

        if (rangeStartChanged) PropertyChanged?.Invoke(this, new(nameof(RangeStart)));

        if (rangeEndChanged) PropertyChanged?.Invoke(this, new(nameof(RangeEnd)));

        if (positionChanged) PropertyChanged?.Invoke(this, new(nameof(Position)));
    }
}