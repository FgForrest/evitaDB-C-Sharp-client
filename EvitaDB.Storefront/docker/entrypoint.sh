#!/bin/sh
# Rewrites wwwroot/appsettings.json from the environment before nginx starts.
#
# A Blazor WebAssembly app has no server side, so its configuration is a static file the browser fetches.
# Baking the endpoint in at build time would mean one image per target server; writing the file here means
# the same image can be pointed at the public demo, a local container, or a private deployment.
set -eu

CONFIG="${STOREFRONT_CONFIG:-/usr/share/nginx/html/appsettings.json}"

EVITA_HOST="${EVITA_HOST:-demo.evitadb.io}"
EVITA_PORT="${EVITA_PORT:-443}"
EVITA_TLS="${EVITA_TLS:-true}"
EVITA_CATALOG="${EVITA_CATALOG:-evita}"

# guard against a typo turning into malformed JSON that the app cannot parse
case "${EVITA_PORT}" in
    ''|*[!0-9]*)
        echo "entrypoint: EVITA_PORT must be a number, got '${EVITA_PORT}'" >&2
        exit 1
        ;;
esac
case "${EVITA_TLS}" in
    true|false) ;;
    *)
        echo "entrypoint: EVITA_TLS must be 'true' or 'false', got '${EVITA_TLS}'" >&2
        exit 1
        ;;
esac

cat > "${CONFIG}" <<JSON
{
  "Evita": {
    "Host": "${EVITA_HOST}",
    "Port": ${EVITA_PORT},
    "TlsEnabled": ${EVITA_TLS},
    "Catalog": "${EVITA_CATALOG}"
  }
}
JSON

echo "entrypoint: storefront -> ${EVITA_HOST}:${EVITA_PORT} (tls=${EVITA_TLS}, catalog=${EVITA_CATALOG})"

exec "$@"
