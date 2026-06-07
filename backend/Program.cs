using System.Text.Json.Serialization;
using ProjectManagerWeb.src.Services;
using ProjectManagerWeb.src.Utils;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    WebRootPath = "frontend"
});

builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
    options.JsonSerializerOptions.MaxDepth = 32;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSingleton<RepositorioJsonService>();
builder.Services.AddSingleton<ConfiguracaoService>();
builder.Services.AddSingleton<CloneService>();
builder.Services.AddSingleton<PastaService>();
builder.Services.AddSingleton<ComandoService>();
builder.Services.AddSingleton<PastaJsonService>();
builder.Services.AddSingleton<IISService>();
builder.Services.AddSingleton<SiteIISJsonService>();
builder.Services.AddSingleton<DeployIISService>();
builder.Services.AddSingleton<IDEJsonService>();
builder.Services.AddSingleton<MigrationService>();
builder.Services.AddSingleton<TerminalService>();
builder.Services.AddHttpClient<VersaoService>();

if (OperatingSystem.IsWindows())
    builder.Services.AddSingleton<IShellProvider, WindowsShellProvider>();
else
    builder.Services.AddSingleton<IShellProvider, LinuxShellProvider>();

var app = builder.Build();

ShellExecute.Configure(app.Services.GetRequiredService<IShellProvider>());

try
{
    var migrationService = app.Services.GetRequiredService<MigrationService>();
    await migrationService.ExecuteMigrationsAsync();
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Erro ao executar migrations durante inicialização");
}

if (!app.Environment.IsDevelopment())
{
    var porta = app.Configuration.GetValue<int>("Porta", 2025);
    app.Urls.Clear();
    app.Urls.Add($"http://*:{porta}");
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.MapControllers();

app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value;
    if (path is null) return;
    if (path.StartsWith("/api"))
    {
        await next();
        return;
    }
    var filePath = Path.Combine(app.Environment.WebRootPath, path.TrimStart('/'));
    if (File.Exists(filePath))
    {
        await context.Response.SendFileAsync(filePath);
        return;
    }
    context.Response.ContentType = "text/html";
    await context.Response.SendFileAsync(Path.Combine(app.Environment.WebRootPath, "index.html"));
});

app.Run();
