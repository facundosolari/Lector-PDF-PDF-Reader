# 1. Capa de compilación (SDK)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Como Railway ya está en la carpeta 'PDFLector', las rutas son directas
COPY ["PDFLector/PDFLector.csproj", "PDFLector/"]
COPY ["Application/Application.csproj", "Application/"]

# Restauramos dependencias
RUN dotnet restore "PDFLector/PDFLector.csproj"

# Copiamos el resto del código (todo lo que está en 'PDFLector')
COPY . . 

# Compilamos el proyecto principal
WORKDIR "/src/PDFLector"
RUN dotnet build "PDFLector.csproj" -c Release -o /app/build

# 2. Capa de publicación
FROM build AS publish
RUN dotnet publish "PDFLector.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 3. Imagen final de ejecución (Runtime) - INDISPENSABLE PARA EL OCR
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Instalación de dependencias nativas para el motor de OCR y manejo de imágenes
RUN apt-get update && apt-get install -y --no-install-recommends \
    libgdiplus \
    libc6-dev \
    ghostscript \
    libgs-dev \
    tesseract-ocr \
    tesseract-ocr-spa \
    libtesseract-dev \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

# Variables para que .NET cargue correctamente las librerías de C++ en Linux
ENV DOTNET_System_Drawing_EnableUnixSupport=true
ENV LD_LIBRARY_PATH="/usr/lib/x86_64-linux-gnu:/usr/local/lib"
ENV ASPNETCORE_URLS=http://+:8080

# Ajustes de seguridad para que Ghostscript permita procesar PDFs
RUN ln -s /usr/lib/x86_64-linux-gnu/libgs.so /usr/lib/libgs.so || true && \
    find /etc -name "policy.xml" -exec sed -i 's/rights="none" pattern="PDF"/rights="read|write" pattern="PDF"/g' {} + || true

# Copiamos los archivos publicados desde la etapa anterior
COPY --from=publish /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "PDFLector.dll"]