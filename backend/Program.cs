using System.Text.Json.Serialization;
using ProjectManagerWeb.src.Services;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    // Define a pasta do frontend como WebRoot
    WebRootPath = "frontend"
});

// Adiciona serviços
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

var app = builder.Build();

// Executar migrations antes de iniciar a aplicação
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

// Configura porta 2025 em execução standalone (não IIS)
if (!app.Environment.IsDevelopment())
{
    app.Urls.Clear();
    app.Urls.Add("http://*:2025");
}

// Pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();

// Serve arquivos estáticos (JS, CSS, assets)
app.UseStaticFiles();

// Rotas da API
app.MapControllers();

// Fallback para rotas do Vue
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value;

    if (path is null) return;

    // Se for API, passa para o próximo middleware
    if (path.StartsWith("/api"))
    {
        await next();
        return;
    }

    // Se existir arquivo físico correspondente, serve ele
    var filePath = Path.Combine(app.Environment.WebRootPath, path.TrimStart('/'));
    if (File.Exists(filePath))
    {
        await context.Response.SendFileAsync(filePath);
        return;
    }

    // Qualquer outra rota cai no index.html (Vue Router)
    context.Response.ContentType = "text/html";
    await context.Response.SendFileAsync(Path.Combine(app.Environment.WebRootPath, "index.html"));
});

app.Run();
