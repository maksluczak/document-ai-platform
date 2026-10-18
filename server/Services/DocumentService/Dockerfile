FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Shared/Contracts/Contracts.csproj Shared/Contracts/
COPY Services/DocumentService/DocumentService.csproj Services/DocumentService/
RUN dotnet restore Services/DocumentService/DocumentService.csproj

COPY Shared/ Shared/
COPY Services/DocumentService/ Services/DocumentService/
RUN dotnet publish Services/DocumentService/DocumentService.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "DocumentService.dll"]