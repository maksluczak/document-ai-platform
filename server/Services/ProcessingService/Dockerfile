FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Shared/Contracts/Contracts.csproj Shared/Contracts/
COPY Services/ProcessingService/ProcessingService.csproj Services/ProcessingService/
RUN dotnet restore Services/ProcessingService/ProcessingService.csproj

COPY Shared/ Shared/
COPY Services/ProcessingService/ Services/ProcessingService/
RUN dotnet publish Services/ProcessingService/ProcessingService.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "ProcessingService.dll"]