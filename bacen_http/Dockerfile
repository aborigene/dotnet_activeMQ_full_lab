FROM mcr.microsoft.com/dotnet/runtime:4.7 AS BASE
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:4.7 AS BUILD
WORKDIR /src
COPY ["bacen_http.csproj", "bacen_http.sln", "Program.cs", "Startup.cs", "wwwroot/", "Pages/", "Properties/", "/src/"]
RUN dotnet restore bacen_processor.csproj
RUN dotnet build "bacen_http.csproj" -c Release -o /app/build

FROM BUILD as PUBLISH
RUN dotnet publish "bacen_http.csproj" -c Release -o /app/publish

FROM BASE as FINAL
WORKDIR /app
COPY --from=PUBLISH /app/publish .
ENTRYPOINT ["dotnet", "bacen_http.dll"] 
