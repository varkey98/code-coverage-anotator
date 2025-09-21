FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src
COPY src/Code.Coverage.Anotator ./Code.Coverage.Anotator
RUN dotnet publish Code.Coverage.Anotator/Code.Coverage.Anotator.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/runtime:10.0

WORKDIR /app
COPY --from=build /out .

ENTRYPOINT ["dotnet", "/app/Code.Coverage.Anotator.dll"]
