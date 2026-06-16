FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Winslow.Domain/Winslow.Domain.fsproj         src/Winslow.Domain/
COPY src/Winslow.Application/Winslow.Application.fsproj src/Winslow.Application/
COPY src/Winslow.Infrastructure/Winslow.Infrastructure.fsproj src/Winslow.Infrastructure/
COPY src/Winslow.API/Winslow.API.fsproj                src/Winslow.API/
RUN dotnet restore src/Winslow.API/Winslow.API.fsproj

COPY . .
RUN dotnet publish src/Winslow.API/Winslow.API.fsproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Winslow.API.dll"]
