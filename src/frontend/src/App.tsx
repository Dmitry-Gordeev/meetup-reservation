import { Routes, Route } from 'react-router-dom'
import CatalogPage from './pages/CatalogPage'
import EventPage from './pages/EventPage'
import HomePage from './pages/HomePage'
import LoginPage from './pages/LoginPage'
import OrganizerCabinetPage from './pages/OrganizerCabinetPage'
import OrganizerPage from './pages/OrganizerPage'
import RegisterPage from './pages/RegisterPage'

function App() {
  return (
    <Routes>
      <Route path="/" element={<HomePage />} />
      <Route path="/events" element={<CatalogPage />} />
      <Route path="/events/:id" element={<EventPage />} />
      <Route path="/organizers/:id" element={<OrganizerPage />} />
      <Route path="/organizer/create" element={<OrganizerCabinetPage />} />
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
    </Routes>
  )
}

export default App
