# Capa de compilación
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["PDFLector/PDFLector.csproj", "PDFLector/"]
COPY ["Application/Application.csproj", "Application/"]
RUN dotnet restore "PDFLector/PDFLector.csproj"
COPY . .
WORKDIR "/src/PDFLector"
RUN dotnet build "PDFLector.csproj" -c Release -o /app/build

# Publicación
FROM build AS publish
RUN dotnet publish "PDFLector.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Imagen final de ejecución
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# 1. Instalamos los motores (Ghostscript y Tesseract)
RUN apt-get update && apt-get install -y \
    libgdiplus \
    ghostscript \
    libgs-dev \
    tesseract-ocr \
    tesseract-ocr-spa \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

# 2. CONFIGURACIÓN DE ENTORNO (Crucial para Railway)
# Esta línea le da el "GPS" a tu código para encontrar Ghostscript y evitar el error de invocación.
ENV LD_LIBRARY_PATH="/usr/lib/x86_64-linux-gnu:${LD_LIBRARY_PATH}"

# 3. Enlaces simbólicos para asegurar que ImageMagick encuentre la librería .so
RUN ln -s /usr/lib/x86_64-linux-gnu/libgs.so /usr/lib/libgs.so || true

# 4. Desbloqueo de seguridad para PDFs (ImageMagick por defecto bloquea PDFs en Linux)
RUN find /etc -name "policy.xml" -exec sed -i 's/rights="none" pattern="PDF"/rights="read|write" pattern="PDF"/g' {} + || true

COPY --from=publish /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "PDFLector.dll"]