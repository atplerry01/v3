FROM confluentinc/cp-kafka:7.6.0

ENV KAFKA_BROKER_ID=1
ENV KAFKA_ZOOKEEPER_CONNECT=zookeeper:2181
ENV KAFKA_ADVERTISED_LISTENERS=PLAINTEXT://kafka:9092
ENV KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR=1
ENV KAFKA_AUTO_CREATE_TOPICS_ENABLE=true

COPY create-topics.sh /docker-entrypoint-initdb.d/
RUN chmod +x /docker-entrypoint-initdb.d/create-topics.sh || true

EXPOSE 9092
