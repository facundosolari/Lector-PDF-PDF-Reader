# 1. Capa de compilación (SDK)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["PDFLector/PDFLector.csproj", "PDFLector/"]
COPY ["Application/Application.csproj", "Application/"]
RUN dotnet restore "PDFLector/PDFLector.csproj"

COPY . . 
WORKDIR "/src/PDFLector"
RUN dotnet build "PDFLector.csproj" -c Release -o /app/build

# 2. Capa de publicación
FROM build AS publish
RUN dotnet publish "PDFLector.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 3. Imagen final de ejecución (Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Instalación de dependencias
RUN apt-get update && apt-get install -y --no-install-recommends \
    libgdiplus \
    libc6-dev \
    ghostscript \
    libgs-dev \
    tesseract-ocr \
    tesseract-ocr-spa \
    tesseract-ocr-eng \
    libtesseract-dev \
    libleptonica-dev \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

# Variables de entorno críticas
ENV DOTNET_System_Drawing_EnableUnixSupport=true
ENV LD_LIBRARY_PATH="/app:/usr/lib/x86_64-linux-gnu"
ENV ASPNETCORE_URLS=http://+:8080

# Permisos de Ghostscript
RUN find /etc -name "policy.xml" -exec sed -i 's/rights="none" pattern="PDF"/rights="read|write" pattern="PDF"/g' {} + || true

COPY --from=publish /app/publish .

# --- MAPEO ROBUSTO (Copia física y permisos) ---
# Usamos copias en lugar de links para evitar problemas de resolución de archivos en el contenedor
RUN LIBLEPT_REAL=$(find /usr/lib/x86_64-linux-gnu/ -name "liblept.so.*" | head -n 1) && \
    cp $LIBLEPT_REAL /app/libleptonica-1.82.0.so && \
    chmod 755 /app/libleptonica-1.82.0.so && \
    LIBTESS_REAL=$(find /usr/lib/x86_64-linux-gnu/ -name "libtesseract.so.*" | head -n 1) && \
    cp $LIBTESS_REAL /app/libtesseract5.so && \
    chmod 755 /app/libtesseract5.so

# Limpieza final de restos de Windows
RUN rm -rf /app/runtimes/win-x64 /app/x64 /app/x86 || true && \
    rm -f /app/leptonica-1.82.0.dll /app/tesseract50.dll || true

EXPOSE 8080
ENTRYPOINT ["dotnet", "PDFLector.dll"]