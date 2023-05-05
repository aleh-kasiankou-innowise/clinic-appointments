FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["./Innowise.Clinic.Appointments.Api/NuGet.Config", "./"]
COPY ["Innowise.Clinic.Appointments.Api/Innowise.Clinic.Appointments.Api.csproj", "Innowise.Clinic.Appointments.Api/"]
COPY ["Innowise.Clinic.Appointments.Services/Innowise.Clinic.Appointments.Services.csproj", "Innowise.Clinic.Appointments.Services/"]
COPY ["Innowise.Clinic.Appointments.Dto/Innowise.Clinic.Appointments.Dto.csproj", "Innowise.Clinic.Appointments.Dto/"]
COPY ["Innowise.Clinic.Appointments.Shared/Innowise.Clinic.Appointments.Shared.csproj", "Innowise.Clinic.Appointments.Shared/"]
COPY ["Innowise.Clinic.Appointments.Persistence/Innowise.Clinic.Appointments.Persistence.csproj", "Innowise.Clinic.Appointments.Persistence/"]
RUN dotnet restore "Innowise.Clinic.Appointments.Api/Innowise.Clinic.Appointments.Api.csproj"
COPY . .
WORKDIR "/src/Innowise.Clinic.Appointments.Api"
RUN dotnet build "Innowise.Clinic.Appointments.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Innowise.Clinic.Appointments.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Innowise.Clinic.Appointments.Api.dll"]
