# Imagen del microservicio de Clientes.
# El contexto de build es ./server y no ./server/clientes, porque el proyecto
# compartido shared/Devsu.Contracts tiene que entrar en el contexto.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Los archivos de proyecto se copian primero y por separado: mientras no cambien
# las dependencias, Docker reutiliza la capa del restore y el build baja de
# minutos a segundos.
COPY Directory.Build.props Directory.Packages.props ./
COPY shared/Devsu.Contracts/Devsu.Contracts.csproj shared/Devsu.Contracts/
COPY clientes/src/Devsu.Clientes.Domain/Devsu.Clientes.Domain.csproj clientes/src/Devsu.Clientes.Domain/
COPY clientes/src/Devsu.Clientes.Application/Devsu.Clientes.Application.csproj clientes/src/Devsu.Clientes.Application/
COPY clientes/src/Devsu.Clientes.Infrastructure/Devsu.Clientes.Infrastructure.csproj clientes/src/Devsu.Clientes.Infrastructure/
COPY clientes/src/Devsu.Clientes.Api/Devsu.Clientes.Api.csproj clientes/src/Devsu.Clientes.Api/

RUN dotnet restore clientes/src/Devsu.Clientes.Api/Devsu.Clientes.Api.csproj

COPY shared/ shared/
COPY clientes/src/ clientes/src/

RUN dotnet publish clientes/src/Devsu.Clientes.Api/Devsu.Clientes.Api.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# curl lo usa el healthcheck del compose; la imagen base no lo trae.
RUN apt-get update \
    && apt-get install --no-install-recommends -y curl \
    && rm -rf /var/lib/apt/lists/*

# Usuario sin privilegios: si alguien escapa del proceso, no es root en el contenedor.
RUN useradd --uid 1001 --create-home --shell /usr/sbin/nologin devsu
USER devsu

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_gcServer=1

EXPOSE 8080

ENTRYPOINT ["dotnet", "Devsu.Clientes.Api.dll"]
