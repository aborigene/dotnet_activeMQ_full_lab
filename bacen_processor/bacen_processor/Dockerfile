FROM mcr.microsoft.com/dotnet/runtime:4.7 AS BASE
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:4.7 AS BUILD
WORKDIR /src
COPY ["bacen_processor.csproj", "bacen_processor.sln", "Program.cs", "/src/"]
RUN dotnet restore bacen_processor.csproj
RUN dotnet build "bacen_processor.csproj" -c Release -o /app/build

FROM BUILD as PUBLISH
RUN dotnet publish "bacen_processor.csproj" -c Release -o /app/publish

FROM BASE as FINAL
WORKDIR /app
COPY --from=PUBLISH /app/publish .
ENTRYPOINT ["dotnet", "bacen_processor.dll"] 
