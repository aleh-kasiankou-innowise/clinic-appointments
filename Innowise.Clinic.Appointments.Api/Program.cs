using Innowise.Clinic.Appointments.Configuration;
using Innowise.Clinic.Appointments.RequestPipeline;
using Innowise.Clinic.Shared.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.ConfigureSecurity();
builder.Services.ConfigureSwagger();
builder.Services.AddSingleton<ExceptionHandlingMiddleware>();
builder.Services.ConfigureRepositories(builder.Configuration);
builder.Services.ConfigureServices();
builder.Services.ConfigureCrossServiceCommunication(builder.Configuration);
builder.Services.PrepareFluentMigrator(builder.Configuration);
builder.ConfigureSerilog();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging();
app.UseSwagger();
app.UseSwaggerUI();

await app.ApplyMigrations(builder.Configuration, "appointment_db");
await app.StartNotificationSyncService();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
Log.Information("Starting the Appointments service");
app.Run();
Log.Information("Stopping the Appointments service");
await Log.CloseAndFlushAsync();