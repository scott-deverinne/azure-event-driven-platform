# Event Contracts

This platform uses strongly typed financial event contracts to support reliable event-driven processing, schema evolution, replay safety, and operational observability.

## Base Metadata

Every event inherits from `FinancialEvent`.

| Field | Description |
|---|---|
| `eventId` | Unique identifier for the event. Used for idempotency. |
| `correlationId` | End-to-end trace identifier across API, Service Bus, Function, Blob Storage, and replay. |
| `eventType` | Logical event name, for example `payment.created`. |
| `eventVersion` | Contract version, for example `1.0`. |
| `occurredAtUtc` | UTC timestamp when the business event occurred. |
| `source` | Producing system or component. |

## Current Event Types

| Event Type | Contract |
|---|---|
| `payment.created` | `PaymentCreatedEvent` |
| `payment.settled` | `PaymentSettledEvent` |
| `refund.issued` | `RefundIssuedEvent` |
| `fraud-check.requested` | `FraudCheckRequestedEvent` |

## Versioning Rules

- Additive fields are allowed in minor versions.
- Removing or renaming fields requires a new major version.
- Consumers must reject unsupported event types.
- Consumers should dead-letter unsupported versions rather than silently ignoring them.
