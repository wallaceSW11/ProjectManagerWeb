using System.Text.Json.Serialization;
using ProjectManagerWeb.src.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Adiciona política de CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Habilita CORS
app.UseCors("AllowAll");

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
