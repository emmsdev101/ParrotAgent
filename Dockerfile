FROM mcr.microsoft.com/dotnet/sdk:10.0 as build
WORKDIR /src

COPY ParrotAgent/*.csproj ./ParrotAgent/
RUN dotnet restore ./ParrotAgent/*.csproj

COPY . ./
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 as final
WORKDIR /src/ParrotAgent

COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "ParrotAgent.dll"]