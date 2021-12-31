#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["ZBOTW.Translator.Web/ZBOTW.Translator.Web.csproj", "ZBOTW.Translator.Web/"]
RUN dotnet restore "ZBOTW.Translator.Web/ZBOTW.Translator.Web.csproj"
COPY . .
WORKDIR "/src/ZBOTW.Translator.Web"
RUN dotnet build "ZBOTW.Translator.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ZBOTW.Translator.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ZBOTW.Translator.Web.dll"]