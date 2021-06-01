FROM mcr.microsoft.com/dotnet/aspnet:3.1 AS BASE
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:3.1 AS BUILD
WORKDIR /src
COPY ["bacen_processor.csproj", "bacen_processor.sln", "Program.cs", "/src/"]

#RUN /bin/bash -c 'PWD=`pwd`; echo $PWD' 
#RUN /bin/bash -c 'LS=`ls`; echo $LS'
RUN dotnet restore bacen_processor.csproj

RUN dotnet build "/src/bacen_processor.csproj" -c Release -o /app/build

FROM BUILD as PUBLISH
RUN dotnet publish "bacen_processor.csproj" -c Release -o /app/publish

FROM BASE as FINAL
WORKDIR /app
COPY --from=PUBLISH /app/publish .
ADD https://raw.githubusercontent.com/vishnubob/wait-for-it/master/wait-for-it.sh /app/
RUN chmod 755 /app/wait-for-it.sh
ENTRYPOINT ["wait-for-it.sh", "127.0.0.1:61616", "--", "dotnet", "bacen_processor.dll"] 
