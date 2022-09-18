using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Fast.Components.FluentUI;
using Microsoft.Fast.Components.FluentUI.DesignTokens;
using Microsoft.JSInterop;

namespace ChatModApp.AuthCallback.Shared;

public sealed partial class MainLayout : IAsyncDisposable
{
    [Inject] private IJSRuntime JsRuntime { get; set; } = default!;

    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    [Inject] private BaseLayerLuminance BaseLayerLuminance { get; set; } = default!;

    private ElementReference _container;
    private IJSObjectReference? _module;
    private bool _menuChecked = true;
    private ErrorBoundary? _errorBoundary;
    private LocalizationDirection _dir;
    private float _baseLayerLuminance = 0.15f;
    

    protected override void OnParametersSet()
    {
        _errorBoundary?.Recover();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await BaseLayerLuminance.SetValueFor(_container, _baseLayerLuminance);
    }

    public async Task SwitchDirection()
    {
        _dir = _dir == LocalizationDirection.rtl ? LocalizationDirection.ltr : LocalizationDirection.rtl;
        await JsRuntime.InvokeVoidAsync("switchDirection", _dir.ToString());
    }

    public void SwitchTheme()
    {
        _baseLayerLuminance = _baseLayerLuminance == 0.15f ? 0.98f : 0.15f;
    }

    private void HandleChecked()
    {
        _menuChecked = !_menuChecked;
    }
    
    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        if (_module is not null)
        {
            await _module.DisposeAsync();
        }
    }
}