import { environment } from '../config/environment'

export function DashboardPage() {
  return (
    <section>
      <div className="page-header">
        <p className="eyebrow">Platform Operations</p>
        <h2>Event Processing Dashboard</h2>
        <p>
          Submit typed financial events, replay failed messages, and validate core operational workflows across Azure environments.
        </p>
      </div>

      <div className="grid">
        <article className="card">
          <span className="card-label">API Endpoint</span>
          <strong>{environment.apiBaseUrl}</strong>
          <p>Current backend target for event ingestion and replay operations.</p>
        </article>

        <article className="card">
          <span className="card-label">Event Contracts</span>
          <strong>4 event types</strong>
          <p>Payment created, payment settled, refund issued, and fraud check requested.</p>
        </article>

        <article className="card">
          <span className="card-label">Reliability</span>
          <strong>Replay-ready</strong>
          <p>Supports dead-letter recovery and controlled requeue workflows.</p>
        </article>

        <article className="card">
          <span className="card-label">Architecture Signal</span>
          <strong>Operations-first</strong>
          <p>This portal demonstrates production operability beyond a simple demo API.</p>
        </article>
      </div>
    </section>
  )
}