FROM mcr.microsoft.com/dotnet/aspnet:3.1 AS BASE
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:3.1 AS BUILD
WORKDIR /src
COPY ["bacen_receiver.csproj", "bacen_receiver.sln", "Program.cs", "/src/"]
RUN dotnet restore "bacen_receiver.csproj"
RUN dotnet build "bacen_receiver.csproj" -c Release -o /app/build

FROM BUILD as PUBLISH
RUN dotnet publish "bacen_receiver.csproj" -c Release -o /app/publish

FROM BASE as FINAL
WORKDIR /app
COPY --from=PUBLISH /app/publish .
ENTRYPOINT ["dotnet", "bacen_receiver.dll", "-d", "-i", "1000"] 
