FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
RUN apt-get update && apt-get install -y --no-install-recommends \
    ffmpeg yt-dlp curl 7zip \
    && rm -rf /var/lib/apt/lists/*
RUN curl -fsSL https://deno.land/install.sh | sh
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["YT.Cacher/YT.Cacher.csproj", "YT.Cacher/"]
RUN dotnet restore "YT.Cacher/YT.Cacher.csproj"
COPY . .
WORKDIR "/src/YT.Cacher"
RUN dotnet build "./YT.Cacher.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./YT.Cacher.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "YT.Cacher.dll"]
