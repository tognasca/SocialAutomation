# src/SocialAutomation.Worker/Dockerfile

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/SocialAutomation.Worker/SocialAutomation.Worker.csproj", "src/SocialAutomation.Worker/"]
COPY ["src/SocialAutomation.Application/SocialAutomation.Application.csproj", "src/SocialAutomation.Application/"]
COPY ["src/SocialAutomation.Domain/SocialAutomation.Domain.csproj", "src/SocialAutomation.Domain/"]
COPY ["src/SocialAutomation.Infrastructure/SocialAutomation.Infrastructure.csproj", "src/SocialAutomation.Infrastructure/"]
RUN dotnet restore "src/SocialAutomation.Worker/SocialAutomation.Worker.csproj"
COPY . .
WORKDIR "/src/src/SocialAutomation.Worker"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SocialAutomation.Worker.dll"]