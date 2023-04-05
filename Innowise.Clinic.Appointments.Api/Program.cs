using Innowise.Clinic.Appointments.Configuration;
using Innowise.Clinic.Appointments.RequestPipeline;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.ConfigureSecurity();
builder.Services.ConfigureSwagger();
builder.Services.AddSingleton<ExceptionHandlingMiddleware>();
builder.Services.ConfigureRepositories(builder.Configuration);
builder.Services.ConfigureMigrations(builder.Configuration);
builder.Services.ConfigureServices();
builder.Services.ConfigureCrossServiceCommunication(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();

await app.ApplyMigrations("appointment_db");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();