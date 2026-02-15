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

# --- MAPEO TOTAL DE LIBRERÍAS (X64) ---
# Creamos el enlace en la carpeta de la app y en el sistema para evitar el fallo de carga
RUN ln -s /usr/lib/x86_64-linux-gnu/liblept.so /app/libleptonica-1.82.0.so || true && \
    ln -s /usr/lib/x86_64-linux-gnu/liblept.so /usr/lib/x86_64-linux-gnu/libleptonica-1.82.0.so || true && \
    ln -s /usr/lib/x86_64-linux-gnu/libtesseract.so /usr/lib/x86_64-linux-gnu/libtesseract5.so || true

# Variables de entorno críticas
ENV DOTNET_System_Drawing_EnableUnixSupport=true
# Obligamos a buscar primero en /app donde reside el .dll
ENV LD_LIBRARY_PATH="/app:/usr/lib/x86_64-linux-gnu:/usr/local/lib"
ENV ASPNETCORE_URLS=http://+:8080

# Configuración de Ghostscript y permisos de PDF
RUN ln -s /usr/lib/x86_64-linux-gnu/libgs.so /usr/lib/libgs.so || true && \
    find /etc -name "policy.xml" -exec sed -i 's/rights="none" pattern="PDF"/rights="read|write" pattern="PDF"/g' {} + || true

COPY --from=publish /app/publish .

# Aseguramos que los enlaces existan incluso si la copia sobreescribió algo
RUN ln -s /usr/lib/x86_64-linux-gnu/liblept.so /app/libleptonica-1.82.0.so || true

EXPOSE 8080
ENTRYPOINT ["dotnet", "PDFLector.dll"]