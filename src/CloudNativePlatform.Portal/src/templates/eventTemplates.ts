export type EventTemplateKey =
  | 'payment.created'
  | 'payment.settled'
  | 'refund.issued'
  | 'fraud-check.requested'

export const eventTemplates: Record<EventTemplateKey, object> = {
  'payment.created': {
    eventId: crypto.randomUUID(),
    correlationId: crypto.randomUUID(),
    eventType: 'payment.created',
    eventVersion: 1,
    occurredAtUtc: new Date().toISOString(),
    source: 'operations-portal',
    paymentId: 'pay_1001',
    customerId: 'cus_2001',
    amount: 125.50,
    currency: 'GBP',
  },
  'payment.settled': {
    eventId: crypto.randomUUID(),
    correlationId: crypto.randomUUID(),
    eventType: 'payment.settled',
    eventVersion: 1,
    occurredAtUtc: new Date().toISOString(),
    source: 'operations-portal',
    paymentId: 'pay_1001',
    settlementId: 'set_3001',
    settledAtUtc: new Date().toISOString(),
  },
  'refund.issued': {
    eventId: crypto.randomUUID(),
    correlationId: crypto.randomUUID(),
    eventType: 'refund.issued',
    eventVersion: 1,
    occurredAtUtc: new Date().toISOString(),
    source: 'operations-portal',
    refundId: 'ref_4001',
    paymentId: 'pay_1001',
    amount: 25.00,
    currency: 'GBP',
    reason: 'Customer requested partial refund',
  },
  'fraud-check.requested': {
    eventId: crypto.randomUUID(),
    correlationId: crypto.randomUUID(),
    eventType: 'fraud-check.requested',
    eventVersion: 1,
    occurredAtUtc: new Date().toISOString(),
    source: 'operations-portal',
    paymentId: 'pay_1001',
    customerId: 'cus_2001',
    riskScore: 72,
  },
}

export function formatTemplate(key: EventTemplateKey) {
  return JSON.stringify(eventTemplates[key], null, 2)
}