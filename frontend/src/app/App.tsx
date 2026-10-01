import { Router } from '@solidjs/router'
import { AppRoot } from './providers/AppRoot'
import { AppRoutes } from './routes'

export default function App() {
  return (
    <Router root={AppRoot} base={import.meta.env.BASE_URL.replace(/\/$/, '')}>
      <AppRoutes />
    </Router>
  )
}
