import { environment } from '../config/environment'

export async function submitFinancialEvent(payload: string) {
  const response = await fetch(`${environment.apiBaseUrl}/api/events`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: payload,
  })

  const text = await response.text()

  if (!response.ok) {
    throw new Error(text || `Submit failed with status ${response.status}`)
  }

  return text || 'Event submitted successfully.'
}

export async function replayFailedEvent(eventId: string) {
  const response = await fetch(`${environment.apiBaseUrl}/api/replay/${eventId}`, {
    method: 'POST',
  })

  const text = await response.text()

  if (!response.ok) {
    throw new Error(text || `Replay failed with status ${response.status}`)
  }

  return text || 'Replay requested successfully.'
}