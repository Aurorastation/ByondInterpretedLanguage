# SDK for building.
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build

WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.sln .
COPY ByondLang/*.csproj ./ByondLang/
COPY ByondLang.Api/*.csproj ./ByondLang.Api/

RUN dotnet restore

# copy everything else and build app
COPY ByondLang/. ./ByondLang/
COPY ByondLang.Api/. ./ByondLang.Api/

WORKDIR /app/ByondLang
RUN dotnet publish -c Release -o out


# Build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS runtime
WORKDIR /app
COPY --from=build /app/ByondLang/out ./

EXPOSE 1945

ENTRYPOINT ["dotnet", "ByondLang.dll"]
