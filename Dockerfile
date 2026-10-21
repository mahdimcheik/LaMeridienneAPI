FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY LaMeridienneAPI.csproj ./
RUN dotnet restore LaMeridienneAPI.csproj
COPY . ./
RUN dotnet publish LaMeridienneAPI.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app ./
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "LaMeridienneAPI.dll"]
