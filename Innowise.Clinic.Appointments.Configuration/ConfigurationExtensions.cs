﻿using System.Text;
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
using Innowise.Clinic.Appointments.Services.TimeSlotsService.Implementations;
using Innowise.Clinic.Appointments.Services.TimeSlotsService.Interfaces;
using Innowise.Clinic.Shared.MassTransit.MessageTypes.Requests;
using Innowise.Clinic.Shared.Services.FiltrationService;
using Innowise.Clinic.Shared.Services.SqlMappingService;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using Serilog;

namespace Innowise.Clinic.Appointments.Configuration;

public static class ConfigurationExtensions
{
    public static IServiceCollection ConfigureSecurity(this IServiceCollection services)
    {
        // TODO MOVE TO SHARED PACKAGE
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            options.SaveToken = true;
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateIssuerSigningKey = false,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                    Environment.GetEnvironmentVariable("JWT__KEY") ?? throw new
                        InvalidOperationException()))
            };
        });
        return services;
    }

    public static IServiceCollection ConfigureSwagger(this IServiceCollection services)
    {
        // TODO MOVE TO SHARED PACKAGE
        services.AddSwaggerGen(opts =>
        {
            opts.AddSecurityDefinition("bearerAuth", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "JWT Authorization header using the Bearer scheme."
            });

            opts.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "bearerAuth" }
                    },
                    new string[] { }
                }
            });
        });


        services.Configure<RouteOptions>(options => { options.LowercaseUrls = true; });
        return services;
    }

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
        services.AddSingleton<FilterResolver<Appointment>>();
        services.AddSingleton<FilterResolver<AppointmentResult>>();
        services.AddSingleton<IAppointmentsService, AppointmentsService>();
        services.AddSingleton<IAppointmentResultsService, AppointmentResultsService>();
        services.AddSingleton<ITimeSlotsService, TimeSlotService>();
        return services;
    }

    public static WebApplicationBuilder ConfigureLogging(this WebApplicationBuilder builder)
    {
        // TODO MOVE TO SHARED PACKAGE
        var logger = new LoggerConfiguration()
            .WriteTo.Console()
            .MinimumLevel.Debug()
            .ReadFrom.Configuration(builder.Configuration)
            .CreateLogger();

        Log.Logger = logger;
        builder.Host.UseSerilog(logger);
        return builder;
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
}