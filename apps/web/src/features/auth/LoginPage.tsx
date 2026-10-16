import { useState } from 'react';
import { Navigate, useNavigate, useSearchParams } from 'react-router-dom';
import { authApi, authStore, useAuth } from '../../api/auth';
import { toErrorMessage } from '../../api/client';

export function LoginPage() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [pending, setPending] = useState(false);
  const [error, setError] = useState('');
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const session = useAuth();
  const destination = params.get('next')?.startsWith('/') && !params.get('next')?.startsWith('//')
    ? params.get('next')! : '/tickets';
  if (session) return <Navigate to={destination} replace />;

  const submit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!email.trim() || !password || pending) return;
    setPending(true);
    setError('');
    try {
      const result = await authApi.login(email.trim(), password);
      authStore.set(result);
      void navigate(destination, { replace: true });
    } catch (e) { setError(toErrorMessage(e, 'Sign-in failed')); }
    finally { setPending(false); }
  };

  return <main className="app__main">
    <div className="card" style={{ maxWidth: 440, margin: '4rem auto' }}>
      <h1>Agent sign in</h1>
      <form onSubmit={(e) => void submit(e)}>
        <div className="field"><label className="field__label" htmlFor="login-email">Email</label>
          <input id="login-email" type="email" autoComplete="username" value={email}
            onChange={(e) => setEmail(e.target.value)} required /></div>
        <div className="field"><label className="field__label" htmlFor="login-password">Password</label>
          <input id="login-password" type="password" autoComplete="current-password" value={password}
            onChange={(e) => setPassword(e.target.value)} required /></div>
        {error && <p role="alert">{error}</p>}
        <button type="submit" className="button button--primary" disabled={pending || !email.trim() || !password}>
          {pending ? 'Signing in…' : 'Sign in'}
        </button>
      </form>
    </div>
  </main>;
}
