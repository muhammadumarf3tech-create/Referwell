'use client';

import { useEffect, useState } from 'react';
import { useAuth } from '@/context/AuthContext';
import { useRouter } from 'next/navigation';
import {
  Users, Plus, Pencil, UserX, CheckCircle2, XCircle,
  Loader2, Shield, Stethoscope, UserCog, X, Save, AlertCircle
} from 'lucide-react';

interface User {
  id: string;
  fullName: string;
  email: string;
  role: string;
  isActive: boolean;
  createdAt: string;
  lastLoginAt: string | null;
}

const roleColors: Record<string, string> = {
  Admin: 'bg-purple-500/20 text-purple-300 border-purple-500/30',
  TriageNurse: 'bg-blue-500/20 text-blue-300 border-blue-500/30',
  GP: 'bg-emerald-500/20 text-emerald-300 border-emerald-500/30',
};
const roleIcons: Record<string, React.ReactNode> = {
  Admin: <Shield className="w-3.5 h-3.5" />,
  TriageNurse: <UserCog className="w-3.5 h-3.5" />,
  GP: <Stethoscope className="w-3.5 h-3.5" />,
};

type ModalMode = 'create' | 'edit' | null;

export default function UsersPage() {
  const { user: currentUser } = useAuth();
  const router = useRouter();
  const [users, setUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(true);
  const [modal, setModal] = useState<ModalMode>(null);
  const [editTarget, setEditTarget] = useState<User | null>(null);
  const [form, setForm] = useState({ fullName: '', email: '', password: '', role: 'GP', isActive: true });
  const [saving, setSaving] = useState(false);
  const [toast, setToast] = useState<{ type: 'success'|'error'; msg: string } | null>(null);

  useEffect(() => {
    if (!currentUser) { router.push('/login'); return; }
    if (currentUser.role !== 'Admin') { router.push('/dashboard'); return; }
    loadUsers();
  }, [currentUser, router]);

  const loadUsers = async () => {
    if (!currentUser) return;
    setLoading(true);
    const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/users`, {
      headers: { Authorization: `Bearer ${currentUser.token}` }
    });
    if (res.ok) setUsers(await res.json());
    setLoading(false);
  };

  const showToast = (type: 'success'|'error', msg: string) => {
    setToast({ type, msg });
    setTimeout(() => setToast(null), 4000);
  };

  const openCreate = () => {
    setForm({ fullName: '', email: '', password: '', role: 'GP', isActive: true });
    setEditTarget(null);
    setModal('create');
  };

  const openEdit = (u: User) => {
    setForm({ fullName: u.fullName, email: u.email, password: '', role: u.role, isActive: u.isActive });
    setEditTarget(u);
    setModal('edit');
  };

  const handleSubmit = async () => {
    if (!currentUser) return;
    setSaving(true);
    try {
      const url = editTarget
        ? `${process.env.NEXT_PUBLIC_API_URL}/api/users/${editTarget.id}`
        : `${process.env.NEXT_PUBLIC_API_URL}/api/users`;
      const method = editTarget ? 'PUT' : 'POST';
      const body = editTarget
        ? { fullName: form.fullName, role: form.role, isActive: form.isActive, newPassword: form.password || undefined }
        : { fullName: form.fullName, email: form.email, password: form.password, role: form.role };

      const res = await fetch(url, {
        method,
        headers: { Authorization: `Bearer ${currentUser.token}`, 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
      });
      const data = await res.json();
      if (res.ok) { showToast('success', editTarget ? 'User updated!' : 'User created!'); setModal(null); loadUsers(); }
      else showToast('error', data.message || 'Failed');
    } finally { setSaving(false); }
  };

  const deactivate = async (u: User) => {
    if (!currentUser || !confirm(`Deactivate ${u.fullName}?`)) return;
    const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/users/${u.id}`, {
      method: 'DELETE',
      headers: { Authorization: `Bearer ${currentUser.token}` }
    });
    if (res.ok) { showToast('success', 'User deactivated'); loadUsers(); }
  };

  return (
    <div className="min-h-screen bg-gray-950 p-6">
      <div className="max-w-5xl mx-auto">
        {/* Toast */}
        {toast && (
          <div className={`fixed top-20 right-4 z-50 flex items-center gap-2 px-5 py-3 rounded-xl border shadow-2xl text-sm ${toast.type === 'success' ? 'bg-emerald-900/90 border-emerald-500/40 text-emerald-300' : 'bg-red-900/90 border-red-500/40 text-red-300'}`}>
            {toast.type === 'success' ? <CheckCircle2 className="w-4 h-4" /> : <AlertCircle className="w-4 h-4" />}
            {toast.msg}
          </div>
        )}

        {/* Header */}
        <div className="flex items-center justify-between mb-8">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-purple-500/10 border border-purple-500/20 flex items-center justify-center">
              <Users className="w-5 h-5 text-purple-400" />
            </div>
            <div>
              <h1 className="text-xl font-bold text-white">User Management</h1>
              <p className="text-gray-400 text-sm">Manage system users and their roles</p>
            </div>
          </div>
          <button onClick={openCreate}
            className="flex items-center gap-2 px-4 py-2 bg-blue-600 hover:bg-blue-500 rounded-xl text-sm text-white font-medium transition-all shadow-lg shadow-blue-500/25">
            <Plus className="w-4 h-4" /> Add User
          </button>
        </div>

        {/* Stats row */}
        <div className="grid grid-cols-3 gap-4 mb-6">
          {['Admin','TriageNurse','GP'].map(role => (
            <div key={role} className={`border rounded-xl p-4 ${role === 'Admin' ? 'bg-purple-500/10 border-purple-500/20' : role === 'TriageNurse' ? 'bg-blue-500/10 border-blue-500/20' : 'bg-emerald-500/10 border-emerald-500/20'}`}>
              <p className="text-xs text-gray-400">{role === 'TriageNurse' ? 'Triage Nurses' : role + 's'}</p>
              <p className={`text-2xl font-bold mt-1 ${role === 'Admin' ? 'text-purple-400' : role === 'TriageNurse' ? 'text-blue-400' : 'text-emerald-400'}`}>
                {users.filter(u => u.role === role && u.isActive).length}
              </p>
            </div>
          ))}
        </div>

        {/* Table */}
        {loading ? (
          <div className="flex justify-center py-16"><Loader2 className="w-6 h-6 animate-spin text-blue-400" /></div>
        ) : (
          <div className="bg-gray-900/60 border border-white/10 rounded-2xl overflow-hidden">
            <table className="w-full">
              <thead>
                <tr className="border-b border-white/10 bg-gray-900/40">
                  {['User','Role','Status','Last Login','Actions'].map(h => (
                    <th key={h} className="text-left px-5 py-3 text-xs font-semibold text-gray-400 uppercase tracking-wider">{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody className="divide-y divide-white/5">
                {users.map(u => (
                  <tr key={u.id} className="hover:bg-white/2 transition-colors">
                    <td className="px-5 py-4">
                      <div className="flex items-center gap-3">
                        <div className="w-9 h-9 rounded-full bg-gradient-to-br from-blue-500 to-purple-600 flex items-center justify-center text-white text-xs font-bold">
                          {u.fullName.split(' ').map(n => n[0]).join('').slice(0,2).toUpperCase()}
                        </div>
                        <div>
                          <p className="text-white text-sm font-medium">{u.fullName}</p>
                          <p className="text-gray-500 text-xs">{u.email}</p>
                        </div>
                      </div>
                    </td>
                    <td className="px-5 py-4">
                      <span className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs border font-medium ${roleColors[u.role] || 'bg-gray-500/20 text-gray-300'}`}>
                        {roleIcons[u.role]}
                        {u.role === 'TriageNurse' ? 'Triage Nurse' : u.role}
                      </span>
                    </td>
                    <td className="px-5 py-4">
                      {u.isActive
                        ? <span className="inline-flex items-center gap-1 text-xs text-emerald-400"><CheckCircle2 className="w-3.5 h-3.5" />Active</span>
                        : <span className="inline-flex items-center gap-1 text-xs text-red-400"><XCircle className="w-3.5 h-3.5" />Inactive</span>}
                    </td>
                    <td className="px-5 py-4 text-xs text-gray-500">
                      {u.lastLoginAt ? new Date(u.lastLoginAt).toLocaleString() : 'Never'}
                    </td>
                    <td className="px-5 py-4">
                      <div className="flex items-center gap-2">
                        <button onClick={() => openEdit(u)} className="p-1.5 text-gray-400 hover:text-white hover:bg-white/5 rounded-lg transition-all">
                          <Pencil className="w-4 h-4" />
                        </button>
                        {u.isActive && u.id !== currentUser?.id && (
                          <button onClick={() => deactivate(u)} className="p-1.5 text-gray-400 hover:text-red-400 hover:bg-red-500/10 rounded-lg transition-all">
                            <UserX className="w-4 h-4" />
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Modal */}
      {modal && (
        <div className="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4">
          <div className="bg-gray-900 border border-white/10 rounded-2xl p-6 w-full max-w-md shadow-2xl">
            <div className="flex items-center justify-between mb-6">
              <h2 className="text-lg font-bold text-white">{modal === 'create' ? 'Create User' : 'Edit User'}</h2>
              <button onClick={() => setModal(null)} className="text-gray-400 hover:text-white"><X className="w-5 h-5" /></button>
            </div>

            <div className="space-y-4">
              <div>
                <label className="text-xs text-gray-400 uppercase tracking-wider mb-1.5 block">Full Name</label>
                <input value={form.fullName} onChange={e => setForm(f => ({...f, fullName: e.target.value}))}
                  className="w-full px-4 py-2.5 bg-gray-800 border border-white/10 rounded-xl text-white text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/50" />
              </div>
              {modal === 'create' && (
                <div>
                  <label className="text-xs text-gray-400 uppercase tracking-wider mb-1.5 block">Email</label>
                  <input type="email" value={form.email} onChange={e => setForm(f => ({...f, email: e.target.value}))}
                    className="w-full px-4 py-2.5 bg-gray-800 border border-white/10 rounded-xl text-white text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/50" />
                </div>
              )}
              <div>
                <label className="text-xs text-gray-400 uppercase tracking-wider mb-1.5 block">
                  {modal === 'edit' ? 'New Password (leave blank to keep)' : 'Password'}
                </label>
                <input type="password" value={form.password} onChange={e => setForm(f => ({...f, password: e.target.value}))}
                  className="w-full px-4 py-2.5 bg-gray-800 border border-white/10 rounded-xl text-white text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/50" />
              </div>
              <div>
                <label className="text-xs text-gray-400 uppercase tracking-wider mb-1.5 block">Role</label>
                <select value={form.role} onChange={e => setForm(f => ({...f, role: e.target.value}))}
                  className="w-full px-4 py-2.5 bg-gray-800 border border-white/10 rounded-xl text-white text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/50">
                  <option value="Admin">Admin</option>
                  <option value="TriageNurse">Triage Nurse</option>
                  <option value="GP">GP</option>
                </select>
              </div>
              {modal === 'edit' && (
                <div className="flex items-center gap-3">
                  <input type="checkbox" id="active" checked={form.isActive} onChange={e => setForm(f => ({...f, isActive: e.target.checked}))} className="accent-blue-500" />
                  <label htmlFor="active" className="text-sm text-gray-300">Active</label>
                </div>
              )}
            </div>

            <div className="flex gap-3 mt-6">
              <button onClick={() => setModal(null)} className="flex-1 py-2.5 border border-white/10 text-gray-400 rounded-xl text-sm hover:bg-white/5 transition-all">Cancel</button>
              <button onClick={handleSubmit} disabled={saving}
                className="flex-1 py-2.5 bg-blue-600 hover:bg-blue-500 text-white rounded-xl text-sm font-medium flex items-center justify-center gap-2 transition-all disabled:opacity-50">
                {saving ? <Loader2 className="w-4 h-4 animate-spin" /> : <Save className="w-4 h-4" />}
                {saving ? 'Saving...' : 'Save'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
