FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Shared/Contracts/Contracts.csproj Shared/Contracts/
COPY Services/UploadService/UploadService.csproj Services/UploadService/
RUN dotnet restore Services/UploadService/UploadService.csproj

COPY Shared/ Shared/
COPY Services/UploadService/ Services/UploadService/
RUN dotnet publish Services/UploadService/UploadService.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "UploadService.dll"]