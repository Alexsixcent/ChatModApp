using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Utils;
using Avalonia.Interactivity;
using Avalonia.Styling;

namespace ChatModApp.Controls;

public class AutoSuggestBox : AutoCompleteBox, IStyleable
{
    public event EventHandler? Committed;
    
    Type IStyleable.StyleKey => typeof(AutoCompleteBox);
    
    private ISelectionAdapter? _adapter;

    
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (_adapter is not null) 
            _adapter.Commit -= OnSelectionAdapterOnCommit;

        SelectionAdapter.Commit += OnSelectionAdapterOnCommit;
        
        _adapter = SelectionAdapter;
    }

    private void OnSelectionAdapterOnCommit(object? sender, RoutedEventArgs args)
    {
        Committed?.Invoke(sender, args);
    }
}