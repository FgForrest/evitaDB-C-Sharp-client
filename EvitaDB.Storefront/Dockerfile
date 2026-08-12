# Blazor WebAssembly publishes to plain static files, so the runtime stage is a web server, not a .NET
# runtime - none of this app executes on the server, it all runs in the visitor's browser.
#
# IMPORTANT: build from the repository root, not from this directory. The project references
# ../EvitaDB.Client, which has to be inside the build context:
#
#   docker build -f EvitaDB.Storefront/Dockerfile -t evita-storefront .
#   docker run --rm -p 8080:8080 evita-storefront
#
# or simply `docker compose up --build` from the repository root.

# ---------------------------------------------------------------- build
# --platform=$BUILDPLATFORM keeps this stage on the builder's native architecture even for a multi-arch
# build. That is safe *because* a Blazor WebAssembly publish emits architecture-independent static files:
# there is no native host binary in the output, so one run serves every target platform. Without it, a
# linux/arm64 build would run the whole SDK stage under emulation for no benefit.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# restore against just the project files first, so the slow restore layer survives source edits
COPY EvitaDB.Client/EvitaDB.Client.csproj EvitaDB.Client/
COPY EvitaDB.Storefront/EvitaDB.Storefront.csproj EvitaDB.Storefront/
RUN dotnet restore EvitaDB.Storefront/EvitaDB.Storefront.csproj

COPY EvitaDB.Client/ EvitaDB.Client/
COPY EvitaDB.Storefront/ EvitaDB.Storefront/

# No wasm-tools workload is needed: the app is published untrimmed and without AOT (see PublishTrimmed /
# RunAOTCompilation in the csproj), so the browser-wasm runtime arrives as an ordinary NuGet package.
RUN dotnet publish EvitaDB.Storefront/EvitaDB.Storefront.csproj -c Release -o /publish --no-restore

# ---------------------------------------------------------------- runtime
# nginx-unprivileged rather than plain nginx: it already listens on 8080, runs as uid 101 and keeps its
# pid and temp paths somewhere writable, so none of that has to be patched in by hand here.
FROM nginxinc/nginx-unprivileged:1.27-alpine AS runtime

# named default.conf so it replaces the config the image ships, rather than sitting alongside it
COPY EvitaDB.Storefront/docker/default.conf /etc/nginx/conf.d/default.conf
COPY EvitaDB.Storefront/docker/entrypoint.sh /usr/local/bin/entrypoint.sh
COPY --from=build /publish/wwwroot /usr/share/nginx/html

# the entrypoint rewrites appsettings.json at start-up, so the web root must be writable by the runtime uid
USER root
RUN chmod +x /usr/local/bin/entrypoint.sh && chown -R 101:101 /usr/share/nginx/html
USER 101

EXPOSE 8080

# The evitaDB endpoint is baked into wwwroot/appsettings.json at publish time; the entrypoint rewrites it
# from the environment so one image can be pointed at any server without a rebuild.
ENV EVITA_HOST=demo.evitadb.io \
    EVITA_PORT=443 \
    EVITA_TLS=true \
    EVITA_CATALOG=evita

ENTRYPOINT ["/usr/local/bin/entrypoint.sh"]
CMD ["nginx", "-g", "daemon off;"]

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s \
    CMD wget --quiet --spider http://127.0.0.1:8080/ || exit 1
