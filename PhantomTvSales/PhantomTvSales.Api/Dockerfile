FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore PhantomTvSales/PhantomTvSales.Api/PhantomTvSales.Api.csproj
RUN dotnet publish PhantomTvSales/PhantomTvSales.Api/PhantomTvSales.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Render manda el puerto en $PORT; tu app debe escuchar en 0.0.0.0:$PORT
CMD ["sh","-c","dotnet PhantomTvSales.Api.dll --urls http://0.0.0.0:${PORT:-10000}"]
