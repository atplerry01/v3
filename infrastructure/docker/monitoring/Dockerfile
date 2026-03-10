FROM prom/prometheus:v2.51.0 AS prometheus

COPY prometheus.yml /etc/prometheus/prometheus.yml

FROM grafana/grafana:10.4.0 AS grafana

ENV GF_SECURITY_ADMIN_PASSWORD=whyce_dev
ENV GF_USERS_ALLOW_SIGN_UP=false
