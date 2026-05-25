FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app
COPY GestorAI.slnx .
COPY src/GestorAI.API/GestorAI.API.csproj src/GestorAI.API/
RUN dotnet restore
COPY . .
RUN dotnet publish src/GestorAI.API -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out .
ENTRYPOINT ["dotnet", "GestorAI.API.dll"]
