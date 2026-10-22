import { Navigate, Outlet } from 'react-router-dom'
import { useSession } from '../../hooks/useSession'
export function Protected() {
  const token = useSession()
  return token ? <Outlet /> : <Navigate to="/" replace />
}
