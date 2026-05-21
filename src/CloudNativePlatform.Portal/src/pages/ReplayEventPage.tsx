import { useState } from 'react'
import { replayFailedEvent } from '../api/eventsApi'

export function ReplayEventPage() {
  const [eventId, setEventId] = useState('')
  const [status, setStatus] = useState<string>()
  const [error, setError] = useState<string>()
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleReplay() {
    setIsSubmitting(true)
    setStatus(undefined)
    setError(undefined)

    try {
      const result = await replayFailedEvent(eventId)
      setStatus(result)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown replay failure')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <section>
      <div className="page-header">
        <p className="eyebrow">Failure Recovery</p>
        <h2>Replay Failed Event</h2>
        <p>Recover a failed event from dead-letter storage and requeue it for processing.</p>
      </div>

      <div className="panel narrow">
        <label>
          Event ID
          <input
            value={eventId}
            onChange={(e) => setEventId(e.target.value)}
            placeholder="Enter failed event ID"
          />
        </label>

        <button onClick={handleReplay} disabled={!eventId || isSubmitting}>
          {isSubmitting ? 'Requesting replay...' : 'Replay event'}
        </button>

        {status && <div className="status success">{status}</div>}
        {error && <div className="status error">{error}</div>}
      </div>
    </section>
  )
}