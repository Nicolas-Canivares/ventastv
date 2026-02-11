FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .

RUN dotnet restore ./PhantomTvSales.Web.csproj
RUN dotnet publish ./PhantomTvSales.Web.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

CMD ["sh","-c","dotnet PhantomTvSales.Web.dll --urls http://0.0.0.0:${PORT:-10000}"]
