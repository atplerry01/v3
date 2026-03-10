-- Whycespace PostgreSQL Schema

CREATE SCHEMA IF NOT EXISTS whyce;

-- Workflow state persistence
CREATE TABLE whyce.workflow_states (
    workflow_id   TEXT PRIMARY KEY,
    current_step  TEXT NOT NULL,
    status        TEXT NOT NULL,
    context       JSONB NOT NULL DEFAULT '{}',
    started_at    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    completed_at  TIMESTAMPTZ
);

-- Engine invocation log
CREATE TABLE whyce.engine_invocations (
    invocation_id TEXT PRIMARY KEY,
    engine_name   TEXT NOT NULL,
    workflow_id   TEXT NOT NULL,
    success       BOOLEAN NOT NULL,
    duration_ms   BIGINT NOT NULL,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Idempotency registry
CREATE TABLE whyce.idempotency (
    invocation_id TEXT PRIMARY KEY,
    processed_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Dead letter queue
CREATE TABLE whyce.dead_letters (
    id            SERIAL PRIMARY KEY,
    envelope      JSONB NOT NULL,
    reason        TEXT NOT NULL,
    failed_at     TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Event store
CREATE TABLE whyce.events (
    event_id      TEXT PRIMARY KEY,
    event_type    TEXT NOT NULL,
    aggregate_id  TEXT NOT NULL,
    timestamp     TIMESTAMPTZ NOT NULL,
    payload       JSONB NOT NULL DEFAULT '{}'
);

CREATE INDEX idx_events_aggregate ON whyce.events (aggregate_id);
CREATE INDEX idx_events_type ON whyce.events (event_type);

-- Vault balances
CREATE TABLE whyce.vault_balances (
    vault_id TEXT PRIMARY KEY,
    balance  NUMERIC(18,2) NOT NULL DEFAULT 0
);

-- SPV registrations
CREATE TABLE whyce.spv_registrations (
    spv_id            TEXT PRIMARY KEY,
    name              TEXT NOT NULL,
    cluster_id        TEXT NOT NULL,
    allocated_capital NUMERIC(18,2) NOT NULL,
    created_at        TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
