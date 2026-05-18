FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Directory.Build.props", "."]
COPY ["src/BuildingBlocks/", "src/BuildingBlocks/"]
COPY ["src/Services/", "src/Services/"]
COPY ["src/Gateway/", "src/Gateway/"]

# 构建服务
ARG SERVICE_PATH
RUN dotnet restore "$SERVICE_PATH"
RUN dotnet build "$SERVICE_PATH" -c Release -o /app/build

FROM build AS publish
ARG SERVICE_PATH
RUN dotnet publish "$SERVICE_PATH" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet"]
CMD ["Mes.Service.Api.dll"]
