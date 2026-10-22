# Etapa 1: Compilación con el SDK de .NET 10
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar el archivo de proyecto y restaurar dependencias
COPY ["MonitorProductividad.Server.csproj", "./"]
RUN dotnet restore "MonitorProductividad.Server.csproj"

# Copiar el resto del código del backend y publicar
COPY . .
RUN dotnet publish "MonitorProductividad.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etapa 2: Imagen ligera de ejecución (ASP.NET Core Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Optimización de memoria para contenedor de 512 MB (Workstation GC)
ENV DOTNET_gcServer=0
# Escuchar en el puerto 8080 (estándar de Koyeb y contenedores .NET modernos)
ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MonitorProductividad.Server.dll"]