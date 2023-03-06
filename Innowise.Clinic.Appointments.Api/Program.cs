using Innowise.Clinic.Appointments.Configuration;
using Innowise.Clinic.Appointments.Persistence;
using Innowise.Clinic.Appointments.RequestPipeline;
using Innowise.Clinic.Appointments.Services.AppointmentResultsService.Implementations;
using Innowise.Clinic.Appointments.Services.AppointmentResultsService.Interfaces;
using Innowise.Clinic.Appointments.Services.AppointmentsService.Implementations;
using Innowise.Clinic.Appointments.Services.AppointmentsService.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<AppointmentsDbContext>(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString(
    "Default")));
builder.Services.ConfigureSecurity();
builder.Services.ConfigureSwagger();
builder.Services.AddScoped<IAppointmentsService, AppointmentsService>();
builder.Services.AddScoped<IAppointmentResultsService, AppointmentResultsService>();
builder.Services.AddSingleton<ExceptionHandlingMiddleware>();
builder.Services.ConfigureCrossServiceCommunication(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var context = services.GetRequiredService<AppointmentsDbContext>();
    if ((await context.Database.GetPendingMigrationsAsync()).Any()) await context.Database.MigrateAsync();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();