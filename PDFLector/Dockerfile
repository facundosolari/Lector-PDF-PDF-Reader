# Imagen base con el SDK de .NET para compilar
FROM mcr.microsoft.com/dotnet/sdk:8.0-preview AS build
WORKDIR /src

# Copiamos los archivos de proyecto usando la ruta relativa correcta
# Docker va a buscar la carpeta PDFLector y el archivo adentro
COPY ["PDFLector/PDFLector.csproj", "PDFLector/"]
COPY ["Application/Application.csproj", "Application/"]

RUN dotnet restore "PDFLector/PDFLector.csproj"

# Copiar todo el resto del código
COPY . .
WORKDIR "/src/PDFLector"
RUN dotnet build "PDFLector.csproj" -c Release -o /app/build

# Publicar la app
FROM build AS publish
RUN dotnet publish "PDFLector.csproj" -c Release -o /app/publish

# Imagen final de ejecución
FROM mcr.microsoft.com/dotnet/aspnet:8.0-preview AS final
WORKDIR /app

# Instalación de dependencias (Ghostscript y Tesseract)
RUN apt-get update && apt-get install -y \
    ghostscript \
    tesseract-ocr \
    libtesseract-dev \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "PDFLector.dll"]