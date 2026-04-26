FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY SMG.API/SGM.API.csproj       SMG.API/
COPY SMG.Core/SGM.Core.csproj     SMG.Core/
COPY SGM.Infrastructure/SGM.Infrastructure.csproj SGM.Infrastructure/

RUN dotnet restore SMG.API/SGM.API.csproj

COPY SMG.API/       SMG.API/
COPY SMG.Core/      SMG.Core/
COPY SGM.Infrastructure/ SGM.Infrastructure/

RUN dotnet publish SMG.API/SGM.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SGM.API.dll"]
