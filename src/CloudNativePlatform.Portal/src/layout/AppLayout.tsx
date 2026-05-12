import { NavLink } from 'react-router-dom'
import { environment } from '../config/environment'

type AppLayoutProps = {
  children: React.ReactNode
}

export function AppLayout({ children }: AppLayoutProps) {
  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div>
          <p className="eyebrow">CloudNative Platform</p>
          <h1>Operations Portal</h1>
        </div>

        <nav>
          <NavLink to="/" end>Dashboard</NavLink>
          <NavLink to="/submit-event">Submit Event</NavLink>
          <NavLink to="/replay-event">Replay Failed Event</NavLink>
        </nav>

        <div className="environment-card">
          <span>Environment</span>
          <strong>{environment.name}</strong>
        </div>
      </aside>

      <main className="content">
        {children}
      </main>
    </div>
  )
}