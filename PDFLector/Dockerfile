# 1. Capa de compilación (SDK)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiamos archivos de proyecto para restaurar
COPY ["PDFLector/PDFLector.csproj", "PDFLector/"]
COPY ["Application/Application.csproj", "Application/"]
RUN dotnet restore "PDFLector/PDFLector.csproj"

# Copiamos todo el código y compilamos
COPY . . 
WORKDIR "/src/PDFLector"
RUN dotnet build "PDFLector.csproj" -c Release -o /app/build

# 2. Capa de publicación
FROM build AS publish
RUN dotnet publish "PDFLector.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 3. Imagen final de ejecución (Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Instalación de dependencias nativas de Linux
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

# --- MAPEO DINÁMICO DE LIBRERÍAS ---
# Buscamos la librería real instalada (ej: liblept.so.5) y creamos el alias que busca Tesseract 5.2.0
RUN ln -sf $(find /usr/lib/x86_64-linux-gnu/ -name "liblept.so.*" | head -n 1) /usr/lib/x86_64-linux-gnu/libleptonica-1.82.0.so && \
    ln -sf $(find /usr/lib/x86_64-linux-gnu/ -name "libtesseract.so.*" | head -n 1) /usr/lib/x86_64-linux-gnu/libtesseract5.so && \
    ln -sf /usr/lib/x86_64-linux-gnu/libgs.so /usr/lib/libgs.so || true

# Variables de entorno críticas
ENV DOTNET_System_Drawing_EnableUnixSupport=true
ENV LD_LIBRARY_PATH="/usr/lib/x86_64-linux-gnu:/app"
ENV ASPNETCORE_URLS=http://+:8080

# Configuración de permisos de Ghostscript para procesar PDFs
RUN find /etc -name "policy.xml" -exec sed -i 's/rights="none" pattern="PDF"/rights="read|write" pattern="PDF"/g' {} + || true

# Copiamos la aplicación publicada
COPY --from=publish /app/publish .

# --- LIMPIEZA POST-COPY ---
# Borramos cualquier DLL de Windows o carpeta runtimes/win-x64 que se haya colado.
# Esto obliga a .NET a usar los enlaces simbólicos de Linux que creamos arriba.
RUN rm -rf /app/runtimes/win-x64 /app/x64 /app/x86 || true && \
    rm -f /app/leptonica-1.82.0.dll /app/tesseract50.dll || true

# Enlace final en la raíz de la app por si el cargador nativo busca en ./
RUN ln -sf /usr/lib/x86_64-linux-gnu/libleptonica-1.82.0.so /app/libleptonica-1.82.0.so

EXPOSE 8080
ENTRYPOINT ["dotnet", "PDFLector.dll"]