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

# Instalación de dependencias (Ghostscript y Tesseract con idioma español)
RUN apt-get update && apt-get install -y \
    libgdiplus \
    ghostscript \
    libgs-dev \
    tesseract-ocr \
    tesseract-ocr-spa \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

# Desbloqueo total de PDF en ImageMagick (Cubre versión 6 y 7)
RUN find /etc/ImageMagick* -name "policy.xml" -exec sed -i 's/rights="none" pattern="PDF"/rights="read|write" pattern="PDF"/g' {} +

COPY --from=publish /app/publish .

# Exponemos el puerto que usa Railway
EXPOSE 8080
ENTRYPOINT ["dotnet", "PDFLector.dll"]