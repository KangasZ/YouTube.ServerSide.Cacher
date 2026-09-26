FROM nvidia/cuda:13.1.1-devel-ubuntu22.04 AS builder

ARG FFMPEG_VERSION="8.0"
ENV FFMPEG_VERSION="${FFMPEG_VERSION}"
ENV DEBIAN_FRONTEND=noninteractive

# Install build dependencies in a single layer
RUN apt-get update --fix-missing && \
    apt-get -y install --no-install-recommends \
    autoconf \
    automake \
    build-essential \
    ca-certificates \
    cleancss \
    cmake \
    debhelper-compat \
    doxygen \
    flite1-dev \
    frei0r-plugins-dev \
    git \
    ladspa-sdk \
    libaom-dev \
    libaribb24-dev \
    libass-dev \
    libbluray-dev \
    libbs2b-dev \
    libbz2-dev \
    libcaca-dev \
    libcdio-paranoia-dev \
    libchromaprint-dev \
    libcodec2-dev \
    libdc1394-dev \
    libdrm-dev \
    libfdk-aac-dev \
    libffmpeg-nvenc-dev \
    libfontconfig1-dev \
    libfreetype6-dev \
    libfribidi-dev \
    libgl1-mesa-dev \
    libgme-dev \
    libgnutls28-dev \
    libgsm1-dev \
    libiec61883-dev \
    libavc1394-dev \
    libjack-jackd2-dev \
    liblensfun-dev \
    liblilv-dev \
    liblzma-dev \
    libmp3lame-dev \
    libmysofa-dev \
    libopenal-dev \
    libomxil-bellagio-dev \
    libopencore-amrnb-dev \
    libopencore-amrwb-dev \
    libopenjp2-7-dev \
    libopenmpt-dev \
    libopus-dev \
    libpulse-dev \
    librubberband-dev \
    librsvg2-dev \
    libsctp-dev \
    libsdl2-dev \
    libshine-dev \
    libsnappy-dev \
    libsoxr-dev \
    libspeex-dev \
    libssh-gcrypt-dev \
    libtesseract-dev \
    libtheora-dev \
    libtwolame-dev \
    libva-dev \
    libvdpau-dev \
    libvidstab-dev \
    libvo-amrwbenc-dev \
    libvorbis-dev \
    libvpx-dev \
    libwavpack-dev \
    libwebp-dev \
    libx264-dev \
    libx265-dev \
    libxcb-shape0-dev \
    libxcb-shm0-dev \
    libxcb-xfixes0-dev \
    libxml2-dev \
    libxv-dev \
    libxvidcore-dev \
    libxvmc-dev \
    libzmq3-dev \
    libzvbi-dev \
    nasm \
    node-less \
    ocl-icd-opencl-dev \
    pkg-config \
    tar \
    texinfo \
    wget \
    yasm \
    zlib1g-dev && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /tmp

# Download and extract FFmpeg source (try .tar.xz first, fallback to .tar.bz2)
RUN if wget -q --spider https://ffmpeg.org/releases/ffmpeg-${FFMPEG_VERSION}.tar.xz 2>/dev/null; then \
    wget -q https://ffmpeg.org/releases/ffmpeg-${FFMPEG_VERSION}.tar.xz && \
    tar -xJf ffmpeg-${FFMPEG_VERSION}.tar.xz && \
    rm ffmpeg-${FFMPEG_VERSION}.tar.xz; \
    else \
    wget -q https://ffmpeg.org/releases/ffmpeg-${FFMPEG_VERSION}.tar.bz2 && \
    tar -xjf ffmpeg-${FFMPEG_VERSION}.tar.bz2 && \
    rm ffmpeg-${FFMPEG_VERSION}.tar.bz2; \
    fi

# Install nv-codec-headers (required for cuvid)  n13.0.19.0 for 8.1+
ARG NV_CODEC_HEADERS_VERSION="n12.2.72.0"

RUN git clone --depth 1 --branch ${NV_CODEC_HEADERS_VERSION} \
    https://git.videolan.org/git/ffmpeg/nv-codec-headers.git /tmp/nv-codec-headers && \
    cd /tmp/nv-codec-headers && \
    make && \
    make PREFIX=/usr/local install && \
    rm -rf /tmp/nv-codec-headers


WORKDIR /tmp/ffmpeg-${FFMPEG_VERSION}

# Configure and build FFmpeg
RUN ./configure \
    --prefix=/usr/local/ffmpeg-nvidia \
    --extra-cflags="-I/usr/local/cuda/include" \
    --extra-ldflags="-L/usr/local/cuda/lib64" \
    --toolchain=hardened \
    --enable-gpl \
    --disable-stripping \
    --disable-filter=resample \
    --enable-cuvid \
    --enable-gnutls \
    --enable-ladspa \
    --enable-libaom \
    --enable-libass \
    --enable-libbluray \
    --enable-libbs2b \
    --enable-libcaca \
    --enable-libcdio \
    --enable-libcodec2 \
    --enable-libfdk-aac \
    --enable-libflite \
    --enable-libfontconfig \
    --enable-libfreetype \
    --enable-libfribidi \
    --enable-libgme \
    --enable-libgsm \
    --enable-libjack \
    --enable-libmp3lame \
    --enable-libmysofa \
    --enable-libopenjpeg \
    --enable-libopenmpt \
    --enable-libopus \
    --enable-libpulse \
    --enable-librsvg \
    --enable-librubberband \
    --enable-libshine \
    --enable-libsnappy \
    --enable-libsoxr \
    --enable-libspeex \
    --enable-libssh \
    --enable-libtheora \
    --enable-libtwolame \
    --enable-libvorbis \
    --enable-libvidstab \
    --enable-libvpx \
    --enable-libwebp \
    --enable-libx264 \
    --enable-libx265 \
    --enable-libxml2 \
    --enable-libxvid \
    --enable-libzmq \
    --enable-libzvbi \
    --enable-lv2 \
    --enable-nvenc \
    --enable-nonfree \
    --enable-omx \
    --enable-openal \
    --enable-opencl \
    --enable-opengl \
    --enable-sdl2 && \
    make -j$(nproc) && \
    make install


FROM nvidia/cuda:13.1.1-base-ubuntu22.04 as base

ENV DEBIAN_FRONTEND=noninteractive
ENV PATH="/usr/local/ffmpeg-nvidia/bin:${PATH}"
ENV NVIDIA_VISIBLE_DEVICES=all
ENV NVIDIA_DRIVER_CAPABILITIES=compute,video,utility

# Create non-root user
RUN groupadd -r ffmpeg && \
    useradd -r -g ffmpeg -m -d /home/ffmpeg -s /bin/bash ffmpeg && \
    mkdir -p /home/ffmpeg/workspace && \
    chown -R root:root /home/ffmpeg

RUN apt-get update && apt-get -y install software-properties-common unzip
RUN add-apt-repository ppa:dotnet/backports

# Install only runtime dependencies (use package names without version numbers for apt to resolve)
RUN apt-get update --fix-missing && \
    apt-get -y install --no-install-recommends \
    aspnetcore-runtime-10.0 \
    ca-certificates \
    libaom3 \
    libaribb24-0 \
    libass9 \
    libbluray2 \
    libbs2b0 \
    libbz2-1.0 \
    libcaca0 \
    libcdio19 \
    libcdio-paranoia2 \
    libchromaprint1 \
    libcodec2-1.0 \
    libdc1394-25 \
    libdrm2 \
    libfdk-aac2 \
    libfontconfig1 \
    libfreetype6 \
    libfribidi0 \
    libflite1 \
    libgl1 \
    libgme0 \
    libgnutls30 \
    libgsm1 \
    libiec61883-0 \
    libavc1394-0 \
    libjack-jackd2-0 \
    liblensfun1 \
    liblilv-0-0 \
    liblzma5 \
    libmp3lame0 \
    libmysofa1 \
    libopenal1 \
    libomxil-bellagio0 \
    libopencore-amrnb0 \
    libopencore-amrwb0 \
    libopenjp2-7 \
    libopenmpt0 \
    libopus0 \
    libpulse0 \
    librubberband2 \
    librsvg2-2 \
    libsctp1 \
    libsdl2-2.0-0 \
    libshine3 \
    libsnappy1v5 \
    libsoxr0 \
    libspeex1 \
    libssh-gcrypt-4 \
    libtesseract4 \
    libtheora0 \
    libtwolame0 \
    libva2 \
    libvdpau1 \
    libvidstab1.1 \
    libvo-amrwbenc0 \
    libvorbis0a \
    libvpx7 \
    libwavpack1 \
    libwebp7 \
    libx264-163 \
    libx265-199 \
    libxcb-shape0 \
    libxcb-shm0 \
    libxcb-xfixes0 \
    libxml2 \
    libxv1 \
    libxvidcore4 \
    libxvmc1 \
    libzmq5 \
    libzvbi0 \
    ocl-icd-libopencl1 && \
    rm -rf /var/lib/apt/lists/* /var/cache/apt/archives/*

RUN update-ca-certificates

# Copy FFmpeg binaries and libraries from builder
COPY --from=builder /usr/local/ffmpeg-nvidia /usr/local/ffmpeg-nvidia

# Copy required CUDA NPP libraries from builder (needed for --enable-libnpp)
#COPY --from=builder /usr/local/cuda/lib64/libnpp*.so* /usr/local/cuda/lib64/

# Update library cache
RUN ldconfig

# Verify FFmpeg installation (as root before switching users)
RUN /usr/local/ffmpeg-nvidia/bin/ffmpeg -version && \
    /usr/local/ffmpeg-nvidia/bin/ffmpeg -codecs 2>/dev/null | grep -q cuvid && \
    /usr/local/ffmpeg-nvidia/bin/ffmpeg -codecs 2>/dev/null | grep -q nvenc

# Add needed yt-dlp stuff

RUN apt-get update && apt-get install -y --no-install-recommends \
    jq curl 7zip \
    && rm -rf /var/lib/apt/lists/*
ENV DENO_INSTALL=/usr/local
RUN curl -fsSL https://deno.land/install.sh -o /tmp/deno-install.sh \
    && sh /tmp/deno-install.sh -y \
    && rm /tmp/deno-install.sh \
    && deno --version
RUN set -eux; \
    release_json="$(curl -fsSL https://api.github.com/repos/yt-dlp/yt-dlp-nightly-builds/releases/latest)"; \
    version="$(printf '%s' "$release_json" | jq -r '.tag_name')"; \
    url="$(printf '%s' "$release_json" | jq -r '[.assets[] | select(.name == "yt-dlp_linux")][0].browser_download_url')"; \
    echo "Installing yt-dlp nightly $version from $url"; \
    curl -fsSL "$url" -o /usr/local/bin/yt-dlp; \
    chmod +x /usr/local/bin/yt-dlp; \
    yt-dlp --version
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
USER root

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["YouTube.ServerSide.Cacher/YouTube.ServerSide.Cacher.csproj", "YouTube.ServerSide.Cacher/"]
RUN dotnet restore "YouTube.ServerSide.Cacher/YouTube.ServerSide.Cacher.csproj"
COPY . .
WORKDIR "/src/YouTube.ServerSide.Cacher"
RUN dotnet build "./YouTube.ServerSide.Cacher.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./YouTube.ServerSide.Cacher.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_ENVIRONMENT=Production
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "YouTube.ServerSide.Cacher.dll"]
