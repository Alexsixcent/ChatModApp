using Microsoft.Fast.Components.FluentUI;

namespace ChatModApp.AuthCallback;

public static class StartupHelpers
{
    public static WebApplicationBuilder CreateBuilder(WebApplicationOptions? options = null)
    {
        var builder = WebApplication.CreateBuilder(options ?? new());

        // Add services to the container.
        builder.Services.AddRazorPages();
        builder.Services.AddServerSideBlazor();
        builder.Services.AddFluentUIComponents();
        builder.Services.AddHttpClient();

        builder.Services.AddSingleton<AuthTriggeredService>();

        return builder;
    }

    public static WebApplication CreateApplication(WebApplicationBuilder builder)
    {
        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseStaticFiles();

        app.UseRouting();

        app.MapBlazorHub();
        app.MapFallbackToPage("/_Host");

        return app;
    }
}