import { Router } from '@solidjs/router'
import { AppRoot } from './providers/AppRoot'
import { AppRoutes } from './routes'

export default function App() {
  return (
    <Router root={AppRoot}>
      <AppRoutes />
    </Router>
  )
}
