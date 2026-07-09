'use client';

import { useState } from 'react';
import { useAuth } from '@/context/AuthContext';
import { Activity, Eye, EyeOff, Lock, Mail, AlertCircle, Loader2 } from 'lucide-react';

const quickLogins = [
  { label: 'Admin',       email: 'admin@referwell.com', password: 'Admin@123',  color: 'text-purple-400 border-purple-500/30 hover:bg-purple-500/10' },
  { label: 'Triage Nurse', email: 'nurse@referwell.com', password: 'Nurse@123', color: 'text-blue-400 border-blue-500/30 hover:bg-blue-500/10' },
  { label: 'GP (Dr. Wilson)', email: 'gp1@referwell.com', password: 'Gp1@1234', color: 'text-emerald-400 border-emerald-500/30 hover:bg-emerald-500/10' },
  { label: 'GP (Dr. Hart)', email: 'gp2@referwell.com', password: 'Gp2@1234', color: 'text-teal-400 border-teal-500/30 hover:bg-teal-500/10' },
];

export default function LoginPage() {
  const { login } = useAuth();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [showPass, setShowPass] = useState(false);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      await login(email, password);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Login failed');
    } finally {
      setLoading(false);
    }
  };

  const quickLogin = async (e: string, p: string) => {
    setEmail(e); setPassword(p);
    setError(''); setLoading(true);
    try { await login(e, p); }
    catch (err: unknown) { setError(err instanceof Error ? err.message : 'Login failed'); }
    finally { setLoading(false); }
  };

  return (
    <div className="min-h-screen bg-gray-950 flex items-center justify-center p-4 relative overflow-hidden">
      {/* Ambient background */}
      <div className="absolute inset-0 overflow-hidden pointer-events-none">
        <div className="absolute -top-40 -left-40 w-96 h-96 bg-blue-600/10 rounded-full blur-3xl" />
        <div className="absolute -bottom-40 -right-40 w-96 h-96 bg-purple-600/10 rounded-full blur-3xl" />
        <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-[600px] h-[600px] bg-blue-500/5 rounded-full blur-3xl" />
      </div>

      <div className="w-full max-w-md relative z-10">
        {/* Logo */}
        <div className="text-center mb-8">
          <div className="inline-flex items-center gap-3 mb-4">
            <div className="w-12 h-12 rounded-2xl bg-gradient-to-br from-blue-500 to-purple-600 flex items-center justify-center shadow-2xl shadow-blue-500/30">
              <Activity className="w-6 h-6 text-white" />
            </div>
            <div>
              <span className="text-white font-bold text-3xl tracking-tight">Refer</span>
              <span className="text-blue-400 font-bold text-3xl tracking-tight">Well</span>
            </div>
          </div>
          <p className="text-gray-400 text-sm">Referral Triage & SLA Queue Management</p>
        </div>

        {/* Card */}
        <div className="bg-gray-900/80 backdrop-blur-xl border border-white/10 rounded-2xl p-8 shadow-2xl">
          <h1 className="text-xl font-bold text-white mb-6">Sign in to your account</h1>

          <form onSubmit={handleSubmit} className="space-y-5">
            <div>
              <label className="block text-sm font-medium text-gray-300 mb-2">Email address</label>
              <div className="relative">
                <Mail className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-500" />
                <input
                  type="email"
                  value={email}
                  onChange={e => setEmail(e.target.value)}
                  required
                  placeholder="you@referwell.com"
                  className="w-full pl-10 pr-4 py-3 bg-gray-800/60 border border-white/10 rounded-xl text-white placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-blue-500/50 focus:border-blue-500/50 transition-all text-sm"
                />
              </div>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-300 mb-2">Password</label>
              <div className="relative">
                <Lock className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-500" />
                <input
                  type={showPass ? 'text' : 'password'}
                  value={password}
                  onChange={e => setPassword(e.target.value)}
                  required
                  placeholder="••••••••"
                  className="w-full pl-10 pr-12 py-3 bg-gray-800/60 border border-white/10 rounded-xl text-white placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-blue-500/50 focus:border-blue-500/50 transition-all text-sm"
                />
                <button type="button" onClick={() => setShowPass(!showPass)} className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-500 hover:text-gray-300">
                  {showPass ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                </button>
              </div>
            </div>

            {error && (
              <div className="flex items-center gap-2 text-red-400 text-sm bg-red-500/10 border border-red-500/20 rounded-lg px-4 py-3">
                <AlertCircle className="w-4 h-4 shrink-0" />
                {error}
              </div>
            )}

            <button
              type="submit"
              disabled={loading}
              className="w-full py-3 px-4 bg-gradient-to-r from-blue-600 to-purple-600 hover:from-blue-500 hover:to-purple-500 text-white font-semibold rounded-xl shadow-lg shadow-blue-500/25 transition-all duration-200 disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2"
            >
              {loading ? <Loader2 className="w-4 h-4 animate-spin" /> : null}
              {loading ? 'Signing in...' : 'Sign In'}
            </button>
          </form>

          {/* Quick Login Buttons */}
          <div className="mt-6 pt-6 border-t border-white/10">
            <p className="text-xs text-gray-500 mb-3 text-center font-medium uppercase tracking-wider">Quick login for testing</p>
            <div className="grid grid-cols-2 gap-2">
              {quickLogins.map((ql) => (
                <button
                  key={ql.label}
                  onClick={() => quickLogin(ql.email, ql.password)}
                  disabled={loading}
                  className={`px-3 py-2 border rounded-lg text-xs font-medium transition-all disabled:opacity-50 ${ql.color}`}
                >
                  {ql.label}
                </button>
              ))}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
