FROM openpolicyagent/opa:latest

COPY config.yaml /config.yaml
COPY policies/ /policies/
COPY bundles/ /bundles/

EXPOSE 8181

CMD ["run", "--server", "--addr", ":8181", "--config-file", "/config.yaml", "/policies"]
