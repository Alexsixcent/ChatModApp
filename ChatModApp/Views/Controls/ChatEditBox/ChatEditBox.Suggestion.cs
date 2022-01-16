// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Microsoft.Toolkit.Uwp.Deferred;
using Microsoft.Toolkit.Uwp.UI.Controls;

namespace ChatModApp.Views.Controls.ChatEditBox;

/// <summary>
///     The ChatEditBox control extends <see cref="Windows.UI.Xaml.Controls.RichEditBox" /> control that
///     suggests and embeds custom data in a
///     rich document.
/// </summary>
public partial class ChatEditBox
{
    internal async Task CommitSuggestionAsync(object selectedItem)
    {
        var currentQuery = _currentQuery;
        var range = currentQuery?.Range.GetClone();
        var id = Guid.NewGuid();
        var prefix = currentQuery?.Prefix;
        var query = currentQuery?.QueryText;

        // range has length of 0 at the end of the commit.
        // Checking length == 0 to avoid committing twice.
        if (prefix == null || query == null || range == null || range.Length == 0) return;

        var textBefore = range.Text;
        var format = CreateTokenFormat(range);
        var eventArgs = new SuggestionChosenEventArgs
        {
            Id = id,
            Prefix = prefix,
            QueryText = query,
            SelectedItem = selectedItem,
            DisplayText = query,
            Format = format
        };

        if (SuggestionChosen != null) await TypedEventHandlerExtensions.InvokeAsync(SuggestionChosen, this, eventArgs);

        var text = eventArgs.DisplayText;
        
        IRandomAccessStream? imageStream = null;
        BitmapDecoder? imageDecoder = null;
        if (eventArgs.Image is not null)
        {
            var random = RandomAccessStreamReference.CreateFromUri(eventArgs.Image);

            imageStream = await random.OpenReadAsync();
            imageDecoder = await BitmapDecoder.CreateAsync(imageStream);
        }


        // Since this operation is async, the document may have changed at this point.
        // Double check if the range still has the expected query.
        if (string.IsNullOrEmpty(text) || textBefore != range.Text ||
            !TryExtractQueryFromRange(range, out var testPrefix, out var testQuery) || testPrefix != prefix ||
            testQuery != query)
            return;

        var displayText = prefix + text;


        void RealizeToken()
        {
            if (TryCommitSuggestionIntoDocument(range, displayText,imageStream,imageDecoder, id, eventArgs.Format ?? format, true))
            {
                var token = new ChatSuggestToken(id, displayText) { Active = true, Item = selectedItem };
                token.UpdateTextRange(range);
                _tokens.Add(range.Link, token);
            }
        }

        lock (_tokensLock)
        {
            CreateSingleEdit(RealizeToken);
        }
        
        imageStream?.Dispose();
    }

    private async Task RequestSuggestionsAsync(ITextRange range = null)
    {
        string prefix;
        string query;
        var currentQuery = _currentQuery;
        var queryFound = range == null
                             ? TryExtractQueryFromSelection(out prefix, out query, out range)
                             : TryExtractQueryFromRange(range, out prefix, out query);

        if (queryFound && prefix == currentQuery?.Prefix && query == currentQuery?.QueryText &&
            range.EndPosition == currentQuery?.Range.EndPosition && _suggestionPopup.IsOpen)
            return;

        var previousTokenSource = currentQuery?.CancellationTokenSource;
        if (!(previousTokenSource?.IsCancellationRequested ?? true)) previousTokenSource.Cancel();

        if (queryFound)
        {
            using var tokenSource = new CancellationTokenSource();
            _currentQuery = new()
            {
                Prefix = prefix, QueryText = query, Range = range, CancellationTokenSource = tokenSource
            };

            var cancellationToken = tokenSource.Token;
            var eventArgs = new SuggestionRequestedEventArgs { QueryText = query, Prefix = prefix };
            if (SuggestionRequested != null)
                try
                {
                    await TypedEventHandlerExtensions.InvokeAsync(SuggestionRequested, this, eventArgs,
                                                                  cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

            if (!eventArgs.Cancel)
            {
                _suggestionChoice = 0;
                ShowSuggestionsPopup(_suggestionsList?.Items?.Count > 0);
            }

            tokenSource.Cancel();
        }
        else
        {
            ShowSuggestionsPopup(false);
        }
    }

    private void UpdateSuggestionsListSelectedItem(int choice)
    {
        var itemsList = _suggestionsList.Items;
        if (itemsList == null) return;

        _suggestionsList.SelectedItem = choice == 0 ? null : itemsList[choice - 1];
        _suggestionsList.ScrollIntoView(_suggestionsList.SelectedItem);
    }

    private void ShowSuggestionsPopup(bool show)
    {
        if (_suggestionPopup == null) return;

        _suggestionPopup.IsOpen = show;
        if (!show)
        {
            _suggestionChoice = 0;
            _suggestionPopup.VerticalOffset = 0;
            _suggestionPopup.HorizontalOffset = 0;
            _suggestionsList.SelectedItem = null;
            _suggestionsList.ScrollIntoView(_suggestionsList.Items?.FirstOrDefault());
            UpdateCornerRadii();
        }
    }

    private void UpdatePopupWidth()
    {
        if (_suggestionsContainer == null) return;

        if (PopupPlacement == SuggestionPopupPlacementMode.Attached)
        {
            _suggestionsContainer.MaxWidth = double.PositiveInfinity;
            _suggestionsContainer.Width = _richEditBox.ActualWidth;
        }
        else
        {
            _suggestionsContainer.MaxWidth = _richEditBox.ActualWidth;
            _suggestionsContainer.Width = double.NaN;
        }
    }

    /// <summary>
    ///     Calculate whether to open the suggestion list up or down depends on how much screen space is available
    /// </summary>
    private void UpdatePopupOffset()
    {
        if (_suggestionsContainer == null || _suggestionPopup == null || _richEditBox == null) return;

        _richEditBox.TextDocument.Selection.GetRect(PointOptions.None, out var selectionRect, out _);
        var padding = _richEditBox.Padding;
        selectionRect.X -= HorizontalOffset;
        selectionRect.Y -= VerticalOffset;

        // Update horizontal offset
        if (PopupPlacement == SuggestionPopupPlacementMode.Attached)
        {
            _suggestionPopup.HorizontalOffset = 0;
        }
        else
        {
            var editBoxWidth = _richEditBox.ActualWidth - padding.Left - padding.Right;
            if (_suggestionPopup.HorizontalOffset == 0 && editBoxWidth > 0)
            {
                var normalizedX = selectionRect.X / editBoxWidth;
                _suggestionPopup.HorizontalOffset
                    = (_richEditBox.ActualWidth - _suggestionsContainer.ActualWidth) * normalizedX;
            }
        }

        // Update vertical offset
        var downOffset = _richEditBox.ActualHeight;
        var upOffset = -_suggestionsContainer.ActualHeight;
        if (PopupPlacement == SuggestionPopupPlacementMode.Floating)
        {
            downOffset = selectionRect.Bottom + padding.Top + padding.Bottom;
            upOffset += selectionRect.Top;
        }

        if (_suggestionPopup.VerticalOffset == 0)
        {
            if (IsElementOnScreen(_suggestionsContainer, offsetY: downOffset) &&
                (IsElementInsideWindow(_suggestionsContainer, offsetY: downOffset) ||
                 !IsElementInsideWindow(_suggestionsContainer, offsetY: upOffset) ||
                 !IsElementOnScreen(_suggestionsContainer, offsetY: upOffset)))
            {
                _suggestionPopup.VerticalOffset = downOffset;
                _popupOpenDown = true;
            }
            else
            {
                _suggestionPopup.VerticalOffset = upOffset;
                _popupOpenDown = false;
            }

            UpdateCornerRadii();
        }
        else
        {
            _suggestionPopup.VerticalOffset = _popupOpenDown ? downOffset : upOffset;
        }
    }

    /// <summary>
    ///     Set corner radii so that inner corners, where suggestion list and text box connect, are square.
    ///     This only applies when <see cref="RichSuggestBox.PopupPlacement" /> is set to
    ///     <see cref="SuggestionPopupPlacementMode.Attached" />
    ///     .
    /// </summary>
    /// https://docs.microsoft.com/en-us/windows/apps/design/style/rounded-corner#when-not-to-round
    private void UpdateCornerRadii()
    {
        if (_richEditBox == null || _suggestionsContainer == null ||
            !ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 7))
            return;

        _richEditBox.CornerRadius = CornerRadius;
        _suggestionsContainer.CornerRadius = PopupCornerRadius;

        if (_suggestionPopup.IsOpen && PopupPlacement == SuggestionPopupPlacementMode.Attached)
        {
            if (_popupOpenDown)
            {
                var cornerRadius = new CornerRadius(CornerRadius.TopLeft, CornerRadius.TopRight, 0, 0);
                _richEditBox.CornerRadius = cornerRadius;
                var popupCornerRadius =
                    new CornerRadius(0, 0, PopupCornerRadius.BottomRight, PopupCornerRadius.BottomLeft);
                _suggestionsContainer.CornerRadius = popupCornerRadius;
            }
            else
            {
                var cornerRadius = new CornerRadius(0, 0, CornerRadius.BottomRight, CornerRadius.BottomLeft);
                _richEditBox.CornerRadius = cornerRadius;
                var popupCornerRadius = new CornerRadius(PopupCornerRadius.TopLeft, PopupCornerRadius.TopRight, 0, 0);
                _suggestionsContainer.CornerRadius = popupCornerRadius;
            }
        }
    }
}