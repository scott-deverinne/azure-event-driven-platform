import { useState } from 'react'
import { submitFinancialEvent } from '../api/eventsApi'
import { formatTemplate } from '../templates/eventTemplates'
import type { EventTemplateKey } from '../templates/eventTemplates'

const defaultTemplate: EventTemplateKey = 'payment.created'

export function SubmitEventPage() {
  const [eventType, setEventType] = useState<EventTemplateKey>(defaultTemplate)
  const [payload, setPayload] = useState(formatTemplate(defaultTemplate))
  const [status, setStatus] = useState<string>()
  const [error, setError] = useState<string>()
  const [isSubmitting, setIsSubmitting] = useState(false)

  function handleTemplateChange(value: EventTemplateKey) {
    setEventType(value)
    setPayload(formatTemplate(value))
    setStatus(undefined)
    setError(undefined)
  }

  async function handleSubmit() {
    setIsSubmitting(true)
    setStatus(undefined)
    setError(undefined)

    try {
      JSON.parse(payload)
      const result = await submitFinancialEvent(payload)
      setStatus(result)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown submit failure')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <section>
      <div className="page-header">
        <p className="eyebrow">Event Ingestion</p>
        <h2>Submit Financial Event</h2>
        <p>Send typed contract-based financial events into the Azure Service Bus processing pipeline.</p>
      </div>

      <div className="panel">
        <label>
          Event template
          <select value={eventType} onChange={(e) => handleTemplateChange(e.target.value as EventTemplateKey)}>
            <option value="payment.created">payment.created</option>
            <option value="payment.settled">payment.settled</option>
            <option value="refund.issued">refund.issued</option>
            <option value="fraud-check.requested">fraud-check.requested</option>
          </select>
        </label>

        <label>
          Event payload
          <textarea value={payload} onChange={(e) => setPayload(e.target.value)} rows={22} />
        </label>

        <button onClick={handleSubmit} disabled={isSubmitting}>
          {isSubmitting ? 'Submitting...' : 'Submit event'}
        </button>

        {status && <div className="status success">{status}</div>}
        {error && <div className="status error">{error}</div>}
      </div>
    </section>
  )
}