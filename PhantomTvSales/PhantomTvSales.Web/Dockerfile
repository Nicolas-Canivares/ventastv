FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src
COPY . .

# Nos movemos a la carpeta del proyecto web
WORKDIR /src/PhantomTvSales/PhantomTvSales.Web

RUN dotnet restore PhantomTvSales.Web.csproj
RUN dotnet publish PhantomTvSales.Web.csproj -c Release -o /app/publish
