# Capa de compilación (SDK estable)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiamos los archivos de proyecto
COPY ["PDFLector/PDFLector.csproj", "PDFLector/"]
COPY ["Application/Application.csproj", "Application/"]

RUN dotnet restore "PDFLector/PDFLector.csproj"

# Copiamos el resto y compilamos
COPY . .
WORKDIR "/src/PDFLector"
RUN dotnet build "PDFLector.csproj" -c Release -o /app/build

# Publicar la app
FROM build AS publish
RUN dotnet publish "PDFLector.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Imagen final de ejecución (Runtime estable)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# 1. Instalación de dependencias (Motores nativos)
RUN apt-get update && apt-get install -y \
    libgdiplus \
    ghostscript \
    libgs-dev \
    tesseract-ocr \
    tesseract-ocr-spa \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

# 2. VARIABLE CLAVE: Esto soluciona el error de "Invocación" en Railway
# Le dice a .NET donde están las librerías compartidas (.so)
ENV LD_LIBRARY_PATH="/usr/lib/x86_64-linux-gnu:${LD_LIBRARY_PATH}"

# 3. Forzamos los enlaces simbólicos para Magick.NET
RUN ln -s /usr/lib/x86_64-linux-gnu/libgs.so /usr/lib/libgs.so || true && \
    ln -s /usr/bin/gs /usr/bin/ghostscript || true

# 4. Desbloqueo de seguridad de ImageMagick para procesar PDFs
RUN find /etc -name "policy.xml" -exec sed -i 's/rights="none" pattern="PDF"/rights="read|write" pattern="PDF"/g' {} + || true

COPY --from=publish /app/publish .

# Exponemos el puerto que usa Railway
EXPOSE 8080
ENTRYPOINT ["dotnet", "PDFLector.dll"]