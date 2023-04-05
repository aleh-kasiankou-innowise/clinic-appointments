using System.Text;
using Dapper;
using FluentMigrator.Runner;
using Innowise.Clinic.Appointments.Persistence;
using Innowise.Clinic.Appointments.Persistence.Migrations;
using Innowise.Clinic.Appointments.Persistence.Models;
using Innowise.Clinic.Appointments.Persistence.Repositories.Implementations;
using Innowise.Clinic.Appointments.Persistence.Repositories.Interfaces;
using Innowise.Clinic.Appointments.Services.AppointmentResultsService.Implementations;
using Innowise.Clinic.Appointments.Services.AppointmentResultsService.Interfaces;
using Innowise.Clinic.Appointments.Services.AppointmentsService.Implementations;
using Innowise.Clinic.Appointments.Services.AppointmentsService.Interfaces;
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

namespace Innowise.Clinic.Appointments.Configuration;

public static class ConfigurationExtensions
{
    public static IServiceCollection ConfigureSecurity(this IServiceCollection services)
    {
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

        var defaultMapper = new HybridMapper();
        services.AddSingleton<ISqlMapper>(defaultMapper);
        foreach (var entity in EntityMappingHelper.GetAllEntities())
        {
            SqlMapper.SetTypeMap(entity, new CustomPropertyTypeMap(entity, defaultMapper.GetProperty));
        }

        services.AddSingleton(typeof(SqlRepresentation<>), typeof(SqlRepresentation<>));
        services.AddSingleton<IDoctorRepository, DoctorRepository>();
        services.AddSingleton<IAppointmentsRepository, AppointmentsRepository>();
        services.AddSingleton<IAppointmentResultsRepository, AppointmentResultsRepository>();
        return services;
    }

    public static IServiceCollection ConfigureMigrations(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddPostgres()
                .WithGlobalConnectionString(configuration.GetConnectionString("Default"))
                .ScanIn(typeof(Init).Assembly).For.Migrations())
            .AddLogging(lb => lb.AddFluentMigratorConsole())
            .BuildServiceProvider(false);

        return services;
    }

    public static IServiceCollection ConfigureServices(this IServiceCollection services)
    {
        services.AddSingleton<TreeToSqlVisitor>();
        services.AddSingleton<FilterResolver<Appointment>>();
        services.AddSingleton<FilterResolver<AppointmentResult>>();
        services.AddSingleton<IAppointmentsService, AppointmentsService>();
        services.AddSingleton<IAppointmentResultsService, AppointmentResultsService>();
        return services;
    }

    public static async Task ApplyMigrations(this WebApplication app, string dbName)
    {
        // TODO SOMEHOW REMOVE THE HARDCODED CONNECTION STRING
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        using var conn = new NpgsqlConnection("Server=appointment-db,5432;User Id=postgres; Password=secureDbPassw0rd; TrustServerCertificate=True;");
        var tableExists = (await conn.QueryAsync($"SELECT datname FROM pg_database WHERE datname = '{dbName.ToLower()}';")).Any();
        if (!tableExists)
        {
            await conn.ExecuteAsync($"CREATE DATABASE {dbName};");
        }

        var runner = services.GetRequiredService<IMigrationRunner>();
        runner.MigrateUp();
    }
}