import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom';
import { authStore, useAuth } from './api/auth';

/** Application shell: the header, the navigation, and wherever the router puts the page. */
export function App() {
  const session = useAuth();
  const navigate = useNavigate();
  return (
    <div className="app">
      <header className="app__header">
        <Link className="app__brand" to="/tickets">
          Northwind Support
        </Link>

        <nav className="app__nav" aria-label="Main">
          <NavLink to="/tickets">Tickets</NavLink>
          <NavLink to="/customers">Customers</NavLink>
        </nav>
        {session && <div>
          <span>{session.user.fullName}</span>{' '}
          <button type="button" className="button button--ghost" onClick={() => {
            authStore.clear(); void navigate('/login', { replace: true });
          }}>Logout</button>
        </div>}
      </header>

      <main className="app__main">
        <Outlet />
      </main>
    </div>
  );
}
