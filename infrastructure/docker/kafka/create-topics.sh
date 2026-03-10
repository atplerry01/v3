#!/bin/bash
set -e

KAFKA_BROKER="kafka:9092"

topics=(
  "whyce.commands"
  "whyce.workflow.events"
  "whyce.engine.events"
  "whyce.cluster.events"
  "whyce.spv.events"
  "whyce.economic.events"
  "whyce.system.events"
)

for topic in "${topics[@]}"; do
  kafka-topics --create \
    --bootstrap-server "$KAFKA_BROKER" \
    --topic "$topic" \
    --partitions 16 \
    --replication-factor 1 \
    --if-not-exists
done

echo "All Whycespace Kafka topics created."
