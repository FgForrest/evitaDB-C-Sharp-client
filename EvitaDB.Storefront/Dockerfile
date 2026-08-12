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

# The root .editorconfig has to be inside the image, not just in the repository. EvitaDB.Client builds with
# GenerateDocumentationFile + TreatWarningsAsErrors, and it is the .editorconfig that silences the XML-doc
# diagnostics (CS1591, CS1574, CS0419, CS1580, CS1711). Without it those become ~8.5k errors and the publish
# below fails - which is invisible outside a container, because every other build runs from the repository
# root where .editorconfig applies. Copied after the restore so editing it does not invalidate that layer.
COPY .editorconfig ./

COPY EvitaDB.Client/ EvitaDB.Client/
COPY EvitaDB.Storefront/ EvitaDB.Storefront/

# No wasm-tools workload is needed: the app is published untrimmed and without AOT (see PublishTrimmed /
# RunAOTCompilation in the csproj), so the browser-wasm runtime arrives as an ordinary NuGet package.
RUN dotnet publish EvitaDB.Storefront/EvitaDB.Storefront.csproj -c Release -o /publish --no-restore

# ---------------------------------------------------------------- runtime
# nginx-unprivileged rather than plain nginx: it already listens on 8080, runs as uid 101 and keeps its
# pid and temp paths somewhere writable, so none of that has to be patched in by hand here.
FROM nginxinc/nginx-unprivileged:1.27-alpine AS runtime

# Kept as a template and rendered at start-up, because the listen port is configurable. It renders to
# default.conf so it replaces the config the image ships, rather than sitting alongside it and clashing.
COPY EvitaDB.Storefront/docker/default.conf.template /etc/nginx/templates/default.conf.template
COPY EvitaDB.Storefront/docker/entrypoint.sh /usr/local/bin/entrypoint.sh
COPY --from=build /publish/wwwroot /usr/share/nginx/html

# The entrypoint rewrites two files at start-up: appsettings.json and the nginx server block.
#
# Both are PRE-CREATED here and left world-writable, which is what makes the image run under an arbitrary
# uid. A shell redirect (`>`) needs write permission on the *file* when it already exists, but on the
# *directory* when it has to create one - and no ownership trick covers every uid an orchestrator might
# assign. Owning the directory as 101:0 only helps a process that is uid 101 or in gid 0; anything else
# gets EACCES, which is exactly how this failed behind a reverse proxy that pinned its own uid.
#
# The server block is additionally rendered with the default port at build time, so the image still serves
# correctly if someone bypasses the entrypoint (`docker run --entrypoint nginx`).
USER root
RUN chmod +x /usr/local/bin/entrypoint.sh \
    && chown -R 101:0 /usr/share/nginx/html /etc/nginx/conf.d \
    && chmod -R g+w /usr/share/nginx/html /etc/nginx/conf.d \
    && sed 's|${STOREFRONT_LISTEN_PORT}|8080|g' /etc/nginx/templates/default.conf.template \
         > /etc/nginx/conf.d/default.conf \
    && chmod 0666 /etc/nginx/conf.d/default.conf /usr/share/nginx/html/appsettings.json
USER 101

# Documentation only - EXPOSE cannot be dynamic. The port actually bound is STOREFRONT_LISTEN_PORT; when
# you change it, publish that port instead (`docker run -e STOREFRONT_LISTEN_PORT=9000 -p 80:9000 ...`).
EXPOSE 8080

# The evitaDB endpoint is baked into wwwroot/appsettings.json at publish time; the entrypoint rewrites it
# from the environment so one image can be pointed at any server without a rebuild.
#
# STOREFRONT_LISTEN_PORT is the port nginx binds *inside* the container. It exists for deployments behind a
# reverse proxy whose target port is fixed and not under your control - set it to whatever the proxy expects
# instead of having to change the proxy. Must be >= 1024, since nginx runs unprivileged here.
ENV EVITA_HOST=demo.evitadb.io \
    EVITA_PORT=443 \
    EVITA_TLS=true \
    EVITA_CATALOG=evita \
    STOREFRONT_LISTEN_PORT=8080

ENTRYPOINT ["/usr/local/bin/entrypoint.sh"]
CMD ["nginx", "-g", "daemon off;"]

# Shell form on purpose: it expands STOREFRONT_LISTEN_PORT at run time, so the probe follows a reconfigured
# port instead of reporting the container unhealthy the moment the port is changed.
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s \
    CMD wget --quiet --spider "http://127.0.0.1:${STOREFRONT_LISTEN_PORT}/" || exit 1
