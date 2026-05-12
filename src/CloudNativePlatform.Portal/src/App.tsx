import { Route, Routes } from 'react-router-dom'
import { AppLayout } from './layout/AppLayout'
import { DashboardPage } from './pages/DashboardPage'
import { SubmitEventPage } from './pages/SubmitEventPage'
import { ReplayEventPage } from './pages/ReplayEventPage'

export default function App() {
  return (
    <AppLayout>
      <Routes>
        <Route path="/" element={<DashboardPage />} />
        <Route path="/submit-event" element={<SubmitEventPage />} />
        <Route path="/replay-event" element={<ReplayEventPage />} />
      </Routes>
    </AppLayout>
  )
}