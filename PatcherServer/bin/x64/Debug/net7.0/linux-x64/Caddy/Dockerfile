# note: never use the :latest tag in a production site
FROM caddy:2.6.4
EXPOSE 8080
EXPOSE 443
COPY Caddyfile /etc/caddy/Caddyfile


WORKDIR /data

RUN mkdir -p /data/sigfiles
RUN mkdir -p /data/deltas


COPY Caddy/LauncherFiles /data
RUN caddy run