'use client';

import { useEffect, useState } from 'react';
import { useAuth } from '@/context/AuthContext';
import { useRouter } from 'next/navigation';
import { Key, Save, Loader2, CheckCircle2, AlertCircle, RefreshCw } from 'lucide-react';
import { fetchMenuAccess, hasMenuAccess, type MenuAccessDto } from '@/lib/menuAccess';

const roleNames = ['Admin', 'TriageNurse', 'GP'];
const roleEnumMap: Record<string, number> = {
  'Admin': 1,
  'TriageNurse': 2,
  'GP': 3
};

const menuItems = ['Dashboard', 'Priority Config', 'Mass Communications', 'User Management', 'Menu Access'];

export default function MenuAccessPage() {
  const { user, isLoading } = useAuth();
  const router = useRouter();
  const [accesses, setAccesses] = useState<MenuAccessDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [toast, setToast] = useState<{ type: 'success' | 'error'; msg: string } | null>(null);

  useEffect(() => {
    if (isLoading) return;
    if (!user) { router.push('/login'); return; }

    let cancelled = false;
    (async () => {
      const data = await fetchMenuAccess(user.token);
      if (cancelled) return;
      if (!hasMenuAccess('Menu Access', user.roles, data)) {
        router.push('/dashboard');
        return;
      }
      setAccesses(data);
      setLoading(false);
    })();

    return () => { cancelled = true; };
  }, [user, isLoading, router]);

  const loadMenuAccess = async () => {
    if (!user) return;
    setLoading(true);
    try {
      const data = await fetchMenuAccess(user.token);
      setAccesses(data);
    } finally {
      setLoading(false);
    }
  };

  const getAccess = (role: string, menuItem: string): boolean => {
    const roleVal = roleEnumMap[role];
    const match = accesses.find(a => 
      (a.role === roleVal || a.role === role) && a.menuItem === menuItem
    );
    return match ? match.hasAccess : false;
  };

  const handleToggle = (role: string, menuItem: string) => {
    const roleVal = roleEnumMap[role];
    setAccesses(prev => {
      const exists = prev.some(a => 
        (a.role === roleVal || a.role === role) && a.menuItem === menuItem
      );

      if (exists) {
        return prev.map(a => 
          ((a.role === roleVal || a.role === role) && a.menuItem === menuItem)
            ? { ...a, hasAccess: !a.hasAccess }
            : a
        );
      } else {
        return [...prev, { role: roleVal, menuItem, hasAccess: true }];
      }
    });
  };

  const handleSave = async () => {
    if (!user) return;
    setSaving(true);
    try {
      // Map all roles to numeric enum representation for backend
      const payload = accesses.map(a => ({
        role: typeof a.role === 'string' ? roleEnumMap[a.role] : a.role,
        menuItem: a.menuItem,
        hasAccess: a.hasAccess
      }));

      const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/menuaccess`, {
        method: 'POST',
        headers: {
          Authorization: `Bearer ${user.token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(payload)
      });

      const data = await res.json();
      if (res.ok) {
        setToast({ type: 'success', msg: data.message || 'Menu access updated!' });
        loadMenuAccess();
        // Trigger a page reload after a short delay so the navbar updates immediately
        setTimeout(() => window.location.reload(), 1500);
      } else {
        setToast({ type: 'error', msg: data.message || 'Failed to save configuration' });
      }
    } catch {
      setToast({ type: 'error', msg: 'Network error' });
    } finally {
      setSaving(false);
      setTimeout(() => setToast(null), 4000);
    }
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-slate-50 flex items-center justify-center">
        <Loader2 className="w-8 h-8 animate-spin text-blue-600" />
      </div>
    );
  }

  if (!user) return null;

  return (
    <div className="min-h-screen bg-slate-50 p-6">
      <div className="max-w-4xl mx-auto">
        {/* Toast */}
        {toast && (
          <div className={`fixed top-20 right-4 z-50 flex items-center gap-2 px-5 py-3 rounded-xl border shadow-lg text-sm font-semibold ${toast.type === 'success' ? 'bg-emerald-50 border-emerald-200 text-emerald-700' : 'bg-red-50 border-red-200 text-red-700'}`}>
            {toast.type === 'success' ? <CheckCircle2 className="w-4 h-4" /> : <AlertCircle className="w-4 h-4" />}
            {toast.msg}
          </div>
        )}

        {/* Header */}
        <div className="flex items-center justify-between mb-8">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-blue-50 border border-blue-100 flex items-center justify-center">
              <Key className="w-5 h-5 text-blue-600" />
            </div>
            <div>
              <h1 className="text-xl font-bold text-slate-900">Role Menu Access Control</h1>
              <p className="text-slate-500 text-sm font-medium">Configure menu and route visibility per user role</p>
            </div>
          </div>
          <button onClick={loadMenuAccess} className="flex items-center gap-2 px-4 py-2 bg-white hover:bg-slate-50 border border-slate-200 rounded-xl text-sm text-slate-600 font-bold transition-all shadow-sm">
            <RefreshCw className="w-4 h-4" /> Refresh
          </button>
        </div>

        {/* Access Grid Table */}
        {loading ? (
          <div className="flex justify-center py-16"><Loader2 className="w-6 h-6 animate-spin text-blue-600" /></div>
        ) : (
          <div className="bg-white border border-slate-200 rounded-2xl overflow-hidden shadow-sm mb-6">
            <table className="w-full">
              <thead>
                <tr className="border-b border-slate-200 bg-slate-50">
                  <th className="text-left px-6 py-4 text-xs font-bold text-slate-500 uppercase tracking-wider">Menu Item / Section</th>
                  {roleNames.map(role => (
                    <th key={role} className="text-center px-6 py-4 text-xs font-bold text-slate-500 uppercase tracking-wider">
                      {role === 'TriageNurse' ? 'Triage Nurse' : role}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {menuItems.map(menuItem => (
                  <tr key={menuItem} className="hover:bg-slate-50/50 transition-colors">
                    <td className="px-6 py-4 text-slate-900 text-sm font-bold">{menuItem}</td>
                    {roleNames.map(role => {
                      const hasAccess = getAccess(role, menuItem);
                      return (
                        <td key={role} className="px-6 py-4 text-center">
                          <input
                            type="checkbox"
                            checked={hasAccess}
                            onChange={() => handleToggle(role, menuItem)}
                            className="w-5 h-5 text-blue-600 border-slate-300 rounded focus:ring-blue-500 cursor-pointer accent-blue-600"
                          />
                        </td>
                      );
                    })}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {/* Save Button */}
        <button
          onClick={handleSave}
          disabled={loading || saving}
          className="w-full py-3 px-6 bg-gradient-to-r from-blue-600 to-indigo-600 hover:from-blue-500 hover:to-indigo-500 text-white font-bold rounded-xl shadow-md shadow-blue-500/10 transition-all disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2"
        >
          {saving ? <Loader2 className="w-4 h-4 animate-spin" /> : <Save className="w-4 h-4" />}
          {saving ? 'Saving changes...' : 'Save Configuration'}
        </button>
      </div>
    </div>
  );
}
