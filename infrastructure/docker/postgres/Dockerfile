FROM postgres:16-alpine

ENV POSTGRES_DB=whycespace
ENV POSTGRES_USER=whyce
ENV POSTGRES_PASSWORD=whyce_dev

COPY init.sql /docker-entrypoint-initdb.d/

EXPOSE 5432
