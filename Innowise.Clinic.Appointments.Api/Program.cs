using Innowise.Clinic.Appointments.Configuration;
using Innowise.Clinic.Appointments.RequestPipeline;
using Innowise.Clinic.Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureLogging();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.ConfigureSecurity();
builder.Services.ConfigureSwagger();
builder.Services.AddSingleton<ExceptionHandlingMiddleware>();
builder.Services.ConfigureRepositories(builder.Configuration);
builder.Services.ConfigureServices();
builder.Services.ConfigureCrossServiceCommunication(builder.Configuration);
builder.Services.PrepareFluentMigrator(builder.Configuration);

var app = builder.Build();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();

await app.ApplyMigrations(builder.Configuration, "appointment_db");
await app.StartNotificationSyncService();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();