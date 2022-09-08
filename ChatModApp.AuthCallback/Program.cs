using ChatModApp.AuthCallback;

var builder = StartupHelpers.CreateBuilder(new() {Args = args});

await StartupHelpers.CreateApplication(builder).RunAsync();