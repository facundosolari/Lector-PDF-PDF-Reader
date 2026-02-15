# 1. Capa de compilación (SDK)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Ajuste de rutas según tu estructura: CarpetaContenedora/Proyecto/archivo
COPY ["PDFLector/PDFLector/PDFLector.csproj", "PDFLector/PDFLector/"]
COPY ["PDFLector/Application/Application.csproj", "PDFLector/Application/"]

RUN dotnet restore "PDFLector/PDFLector/PDFLector.csproj"

# Copiamos todo el contenido de la subcarpeta PDFLector al contenedor
COPY PDFLector/ . 

# Nos movemos a la carpeta del proyecto principal para compilar
WORKDIR "/src/PDFLector"
RUN dotnet build "PDFLector.csproj" -c Release -o /app/build

# 2. Capa de publicación
FROM build AS publish
RUN dotnet publish "PDFLector.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 3. Imagen final de ejecución (Runtime) - CON LAS LIBRERÍAS DE OCR
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Instalación de motores nativos (Indispensable para Linux)
RUN apt-get update && apt-get install -y --no-install-recommends \
    libgdiplus \
    libc6-dev \
    ghostscript \
    libgs-dev \
    tesseract-ocr \
    tesseract-ocr-spa \
    libtesseract-dev \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

# Variables de entorno para que .NET encuentre las librerías nativas
ENV DOTNET_System_Drawing_EnableUnixSupport=true
ENV LD_LIBRARY_PATH="/usr/lib/x86_64-linux-gnu:/usr/local/lib"
ENV ASPNETCORE_URLS=http://+:8080

# Permisos para que Ghostscript pueda procesar el PDF antes del OCR
RUN ln -s /usr/lib/x86_64-linux-gnu/libgs.so /usr/lib/libgs.so || true && \
    find /etc -name "policy.xml" -exec sed -i 's/rights="none" pattern="PDF"/rights="read|write" pattern="PDF"/g' {} + || true

# Traemos los archivos publicados
COPY --from=publish /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "PDFLector.dll"]