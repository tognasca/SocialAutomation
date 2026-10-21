# src/SocialAutomation.Api/Dockerfile

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/SocialAutomation.Api/SocialAutomation.Api.csproj", "src/SocialAutomation.Api/"]
COPY ["src/SocialAutomation.Application/SocialAutomation.Application.csproj", "src/SocialAutomation.Application/"]
COPY ["src/SocialAutomation.Domain/SocialAutomation.Domain.csproj", "src/SocialAutomation.Domain/"]
COPY ["src/SocialAutomation.Infrastructure/SocialAutomation.Infrastructure.csproj", "src/SocialAutomation.Infrastructure/"]
RUN dotnet restore "src/SocialAutomation.Api/SocialAutomation.Api.csproj"
COPY . .
WORKDIR "/src/src/SocialAutomation.Api"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY src/SocialAutomation.Infrastructure/Database/migrations /app/migrations
ENV MIGRATIONS_PATH=/app/migrations
ENTRYPOINT ["dotnet", "SocialAutomation.Api.dll"]
