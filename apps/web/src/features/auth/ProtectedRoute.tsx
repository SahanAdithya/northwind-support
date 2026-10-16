import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '../../api/auth';

export function ProtectedRoute() {
  const session = useAuth();
  const location = useLocation();
  return session ? <Outlet /> : <Navigate
    to={`/login?next=${encodeURIComponent(location.pathname + location.search)}`} replace />;
}
