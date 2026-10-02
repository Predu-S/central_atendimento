using System.Text.Json.Serialization;
using Api;
using Application;
using Domain;
using FluentValidation;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer(); builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails(); builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddCors(o => o.AddPolicy("Frontend", p => p.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddValidatorsFromAssemblyContaining<TicketValidator>();
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System); builder.Services.AddSingleton<SlaCalculator>();
builder.Services.AddScoped<IAtendimentoRepository, AtendimentoRepository>();
builder.Services.AddScoped<SlaService>(); builder.Services.AddScoped<TicketService>(); builder.Services.AddScoped<AnalyticsService>();
var provider = builder.Configuration["Database:Provider"] ?? "SqlServer";
if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
{
    if (!builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Testing")) throw new InvalidOperationException("SQLite é permitido apenas em desenvolvimento e testes.");
    builder.Services.AddDbContext<SqliteAtendimentoDbContext>(o => o.UseSqlite(builder.Configuration.GetConnectionString("Sqlite")));
    builder.Services.AddScoped<AtendimentoDbContext>(sp => sp.GetRequiredService<SqliteAtendimentoDbContext>());
}
else if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddDbContext<AtendimentoDbContext>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")));
else throw new InvalidOperationException("Database:Provider deve ser Sqlite ou SqlServer.");
var app = builder.Build();
app.UseExceptionHandler(); app.UseCors("Frontend");
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.Use(async (context, next) => {
    var inicio = System.Diagnostics.Stopwatch.GetTimestamp();
    await next(context);
    app.Logger.LogInformation("HTTP {Metodo} {Caminho} respondeu {Status} em {Duracao} ms", context.Request.Method, context.Request.Path, context.Response.StatusCode, System.Diagnostics.Stopwatch.GetElapsedTime(inicio).TotalMilliseconds);
});
app.MapControllers();
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AtendimentoDbContext>();
    await db.Database.MigrateAsync();
    var horario = builder.Configuration.GetSection("Sla:HorarioComercial").Get<HorarioComercial>() ?? new HorarioComercial();
    var configuracao = new ConfiguracaoSlaDto(Enum.GetValues<Prioridade>().Select(p => new RegraSlaDto(p, 60, 480)).ToList(), horario);
    await scope.ServiceProvider.GetRequiredService<IValidator<ConfiguracaoSlaDto>>().ValidateAndThrowAsync(configuracao);
    await SeedData.Inicializar(db, horario, builder.Configuration.GetValue("Database:Seed", true));
    app.Logger.LogInformation("Banco {Provider} migrado e inicializado", provider);
}
app.Run();
public partial class Program;
