#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["m3u8restreamer/m3u8restreamer.csproj", "m3u8restreamer/"]
RUN dotnet restore "m3u8restreamer/m3u8restreamer.csproj"
COPY . .
WORKDIR "/src/m3u8restreamer"
RUN dotnet build "m3u8restreamer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "m3u8restreamer.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

RUN apt-get update && apt-get install -y \
  # Install pip so that we can use pip to install yt-dlp at runtime
  python3-pip \
  # yt-dlp needs ffmpeg, but it can't be installed with pip
  ffmpeg \
  && rm -rf /var/lib/apt/lists/*

ENTRYPOINT ["dotnet", "m3u8restreamer.dll"]