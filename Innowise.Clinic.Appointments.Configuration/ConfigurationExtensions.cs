using Dapper;
using FluentMigrator.Runner;
using Innowise.Clinic.Appointments.Persistence.Migrations;
using Innowise.Clinic.Appointments.Persistence.Models;
using Innowise.Clinic.Appointments.Persistence.ObjectRelationalMapping;
using Innowise.Clinic.Appointments.Persistence.Repositories.Implementations;
using Innowise.Clinic.Appointments.Persistence.Repositories.Interfaces;
using Innowise.Clinic.Appointments.Services.AppointmentResultsService.Implementations;
using Innowise.Clinic.Appointments.Services.AppointmentResultsService.Interfaces;
using Innowise.Clinic.Appointments.Services.AppointmentsService.Implementations;
using Innowise.Clinic.Appointments.Services.AppointmentsService.Interfaces;
using Innowise.Clinic.Appointments.Services.MassTransitService.Consumers;
using Innowise.Clinic.Appointments.Services.NotificationsService;
using Innowise.Clinic.Appointments.Services.TimeSlotsService.Implementations;
using Innowise.Clinic.Appointments.Services.TimeSlotsService.Interfaces;
using Innowise.Clinic.Shared.MassTransit.MessageTypes.Requests;
using Innowise.Clinic.Shared.Services.FiltrationService;
using Innowise.Clinic.Shared.Services.SqlMappingService;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Serilog;
using Serilog.Sinks.Elasticsearch;

namespace Innowise.Clinic.Appointments.Configuration;

public static class ConfigurationExtensions
{
    public static IServiceCollection ConfigureCrossServiceCommunication(this IServiceCollection services,
        IConfiguration configuration)
    {
        var rabbitMqConfig = configuration.GetSection("RabbitConfigurations");
        services.AddMassTransit(x =>
        {
            x.AddConsumer<DoctorChangesAppointmentsConsumer>();
            x.AddRequestClient<ProfileExistsAndHasRoleRequest>();
            x.AddRequestClient<ServiceExistsAndBelongsToSpecializationRequest>();
            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitMqConfig["HostName"], h =>
                {
                    h.Username(rabbitMqConfig["UserName"]);
                    h.Password(rabbitMqConfig["Password"]);
                });
                cfg.ConfigureEndpoints(context);
            });
        });
        return services;
    }

    public static IServiceCollection ConfigureRepositories(this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default") ??
                               throw new ApplicationException(
                                   "The connection string for the database connection was not found.");
        services.AddNpgsqlDataSource(connectionString);

        services.AddSingleton<TreeToSqlVisitor>();
        var defaultMapper = new HybridMapper();
        services.AddSingleton<ISqlMapper>(defaultMapper);

        foreach (var entity in EntityMappingHelper.GetAllEntities())
        {
            SqlMapper.SetTypeMap(entity, new CustomPropertyTypeMap(entity, defaultMapper.GetProperty));
            var sqlRepresentationType = typeof(SqlRepresentation<>).MakeGenericType(entity);
            services.AddSingleton(sqlRepresentationType);
        }

        services.AddSingleton<IDoctorRepository, DoctorRepository>();
        services.AddSingleton<IAppointmentsRepository, AppointmentsRepository>();
        services.AddSingleton<IAppointmentResultsRepository, AppointmentResultsRepository>();
        services.AddSingleton<ITimeSlotRepository, TimeSlotRepository>();
        return services;
    }

    public static IServiceCollection PrepareFluentMigrator(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddPostgres()
                .WithGlobalConnectionString(configuration.GetConnectionString("Default"))
                .ScanIn(typeof(Init).Assembly).For.Migrations())
            .AddLogging(lb => lb.AddFluentMigratorConsole());

        return services;
    }

    public static IServiceCollection ConfigureServices(this IServiceCollection services)
    {
        services.AddSingleton<BackgroundNotificationsService>();
        services.AddSingleton<FilterResolver<Appointment>>();
        services.AddSingleton<FilterResolver<AppointmentResult>>();
        services.AddSingleton<IAppointmentsService, AppointmentsService>();
        services.AddSingleton<IAppointmentResultsService, AppointmentResultsService>();
        services.AddSingleton<ITimeSlotsService, TimeSlotService>();
        return services;
        
    }

    public static async Task ApplyMigrations(this WebApplication app, IConfiguration configuration, string dbName)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var connectionString = configuration.GetConnectionString("Migrator");
        using var conn =
            new NpgsqlConnection(connectionString);
        
        var databaseExists =
            (await conn.QueryAsync($"SELECT datname FROM pg_database WHERE datname = '{dbName.ToLower()}';")).Any();
        if (!databaseExists)
        {
            await conn.ExecuteAsync($"CREATE DATABASE {dbName};");
        }

        var runner = services.GetRequiredService<IMigrationRunner>();
        runner.MigrateUp();
    }
    
    public static async Task StartNotificationSyncService(this WebApplication app)
    {
        var service = app.Services.GetRequiredService<BackgroundNotificationsService>();
        var token = new CancellationToken();
        await Task.Run(() => service.StartAsync(token), token);
    }
    
    public static WebApplicationBuilder ConfigureSerilog(this WebApplicationBuilder builder)
    {
        var logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(builder.Configuration["ElasticSearchHost"]))
            {
                AutoRegisterTemplate = true,
                OverwriteTemplate = true,
                IndexFormat = $"clinic.appointments-{0:yy.MM}",
                BatchAction = ElasticOpType.Index,
                DetectElasticsearchVersion = true,
            })
            .WriteTo.Console()
            .CreateLogger();

        Log.Logger = logger;
        builder.Host.UseSerilog(logger);
        return builder;
    }
}