#!/bin/sh
# Rewrites wwwroot/appsettings.json and renders the nginx server block from the environment before nginx
# starts.
#
# A Blazor WebAssembly app has no server side, so its configuration is a static file the browser fetches.
# Baking the endpoint in at build time would mean one image per target server; writing the file here means
# the same image can be pointed at the public demo, a local container, or a private deployment.
set -eu

CONFIG="${STOREFRONT_CONFIG:-/usr/share/nginx/html/appsettings.json}"
NGINX_TEMPLATE="${STOREFRONT_NGINX_TEMPLATE:-/etc/nginx/templates/default.conf.template}"
NGINX_CONF="${STOREFRONT_NGINX_CONF:-/etc/nginx/conf.d/default.conf}"

STOREFRONT_LISTEN_PORT="${STOREFRONT_LISTEN_PORT:-8080}"

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

# The listen port is validated rather than passed through, because a bad value surfaces as an nginx syntax
# error or - worse - a container that starts and quietly serves nothing. Anything below 1024 is rejected up
# front: this image runs nginx as an unprivileged uid, so binding a privileged port fails no matter what.
case "${STOREFRONT_LISTEN_PORT}" in
    ''|*[!0-9]*)
        echo "entrypoint: STOREFRONT_LISTEN_PORT must be a number, got '${STOREFRONT_LISTEN_PORT}'" >&2
        exit 1
        ;;
esac
if [ "${STOREFRONT_LISTEN_PORT}" -lt 1024 ] || [ "${STOREFRONT_LISTEN_PORT}" -gt 65535 ]; then
    echo "entrypoint: STOREFRONT_LISTEN_PORT must be between 1024 and 65535 (nginx runs unprivileged here)," \
         "got '${STOREFRONT_LISTEN_PORT}'" >&2
    exit 1
fi

# Rendered from a pristine template on every start rather than edited in place, so that restarting a
# container - which reuses the existing filesystem - re-renders from the placeholder instead of trying to
# substitute a value that is already there.
if [ ! -r "${NGINX_TEMPLATE}" ]; then
    echo "entrypoint: nginx template '${NGINX_TEMPLATE}' is missing or unreadable" >&2
    exit 1
fi

# Checked up front so the failure names the cause instead of surfacing as a bare redirect error. The image
# ships both files world-writable precisely so any uid can rewrite them, so hitting this means something
# outside the image replaced them - typically a volume or bind mount over the path, or a read-only mount.
for _target in "${NGINX_CONF}" "${CONFIG}"; do
    if [ ! -w "${_target}" ]; then
        echo "entrypoint: '${_target}' is not writable by uid $(id -u)." >&2
        echo "entrypoint: this image is built to run under any uid, so the usual cause is a mount over" >&2
        echo "entrypoint: that path, or a read-only filesystem. Remove the mount, or run with --user 101." >&2
        exit 1
    fi
done
sed "s|\${STOREFRONT_LISTEN_PORT}|${STOREFRONT_LISTEN_PORT}|g" "${NGINX_TEMPLATE}" > "${NGINX_CONF}"

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
echo "entrypoint: listening on ${STOREFRONT_LISTEN_PORT}"

exec "$@"
