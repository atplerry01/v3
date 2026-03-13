FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY src/shared/Whycespace.Shared.csproj src/shared/
COPY src/engines/Whycespace.Engines.csproj src/engines/
COPY src/runtime/Whycespace.Runtime.csproj src/runtime/
COPY src/system/Whycespace.System.csproj src/system/
COPY src/domain/Whycespace.Domain.csproj src/domain/
COPY infrastructure/host/Whycespace.FoundationHost/Whycespace.FoundationHost.csproj infrastructure/host/Whycespace.FoundationHost/
RUN dotnet restore infrastructure/host/Whycespace.FoundationHost/Whycespace.FoundationHost.csproj

COPY src/ src/
COPY infrastructure/host/Whycespace.FoundationHost/ infrastructure/host/Whycespace.FoundationHost/
RUN dotnet publish infrastructure/host/Whycespace.FoundationHost/Whycespace.FoundationHost.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Whycespace.FoundationHost.dll"]
