FROM mysql:8.0.23

RUN mkdir -p /messaging-db/data
COPY messaging-db.sql /docker-entrypoint-initdb.d

ENV MYSQL_ROOT_PASSWORD=123456789
ENV MYSQL_USER=messaging-db-user
ENV MYSQL_PASSWORD=123456789auth