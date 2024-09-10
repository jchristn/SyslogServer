FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app
COPY . /app/
EXPOSE 514/udp
ENTRYPOINT ["dotnet", "SyslogServer.dll"]
