'use client';

import { useEffect, useState } from 'react';
import { useAuth } from '@/context/AuthContext';
import { useRouter } from 'next/navigation';
import {
  Users, Plus, Pencil, UserX, CheckCircle2, XCircle,
  Loader2, Shield, Stethoscope, UserCog, X, Save, AlertCircle, Eye, EyeOff, Search, ChevronDown
} from 'lucide-react';
import { fetchMenuAccess, hasMenuAccess } from '@/lib/menuAccess';

interface User {
  id: string;
  fullName: string;
  email: string;
  roles: string[];
  isActive: boolean;
  createdAt: string;
  lastLoginAt: string | null;
  password?: string;
  title?: string;
  gender?: string;
  phoneNumber?: string;
}

const roleColors: Record<string, string> = {
  Admin: 'bg-purple-50 text-purple-600 border-purple-200/60',
  TriageNurse: 'bg-blue-50 text-blue-600 border-blue-200/60',
  GP: 'bg-emerald-50 text-emerald-600 border-emerald-200/60',
};
const roleIcons: Record<string, React.ReactNode> = {
  Admin: <Shield className="w-3.5 h-3.5" />,
  TriageNurse: <UserCog className="w-3.5 h-3.5" />,
  GP: <Stethoscope className="w-3.5 h-3.5" />,
};

type ModalMode = 'create' | 'edit' | null;

export default function UsersPage() {
  const { user: currentUser, isLoading } = useAuth();
  const router = useRouter();
  const [users, setUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(true);
  const [modal, setModal] = useState<ModalMode>(null);
  const [editTarget, setEditTarget] = useState<User | null>(null);
  const [form, setForm] = useState({ fullName: '', email: '', password: '', roles: ['GP'], isActive: true, title: 'Mr.', gender: 'Male' });
  const [phoneCountryCode, setPhoneCountryCode] = useState('+64');
  const [localPhone, setLocalPhone] = useState('');
  const [saving, setSaving] = useState(false);
  const [toast, setToast] = useState<{ type: 'success'|'error'; msg: string } | null>(null);
  const [visiblePasswords, setVisiblePasswords] = useState<Record<string, boolean>>({});
  const [showModalPass, setShowModalPass] = useState(false);
  const [confirmDeactivateId, setConfirmDeactivateId] = useState<string | null>(null);

  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [searchQuery, setSearchQuery] = useState('');

  useEffect(() => {
    if (isLoading) return;
    if (!currentUser) { router.push('/login'); return; }

    let cancelled = false;
    (async () => {
      const accesses = await fetchMenuAccess(currentUser.token);
      if (cancelled) return;
      if (!hasMenuAccess('User Management', currentUser.roles, accesses)) {
        router.push('/dashboard');
      }
    })();

    return () => { cancelled = true; };
  }, [currentUser, isLoading, router]);

  const loadUsers = async (pageOverride?: number) => {
    if (!currentUser) return;
    setLoading(true);
    try {
      const pageToFetch = pageOverride ?? currentPage;
      const params = new URLSearchParams();
      params.set('page', pageToFetch.toString());
      params.set('pageSize', '15');
      if (searchQuery) params.set('search', searchQuery);

      const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/users?${params}`, {
        headers: { Authorization: `Bearer ${currentUser.token}` }
      });
      if (res.ok) {
        const data = await res.json();
        setUsers(data.items);
        setTotalPages(data.totalPages);
        setTotalCount(data.totalCount);
        setCurrentPage(pageToFetch);
      }
    } finally {
      setLoading(false);
    }
  };

  // Reset page to 1 when searchQuery changes
  useEffect(() => {
    setCurrentPage(1);
  }, [searchQuery]);

  // Fetch when page or searchQuery changes
  useEffect(() => {
    if (!currentUser) return;
    const delayDebounceFn = setTimeout(() => {
      loadUsers(currentPage);
    }, 300);

    return () => clearTimeout(delayDebounceFn);
  }, [currentUser, searchQuery, currentPage]);

  const showToast = (type: 'success'|'error', msg: string) => {
    setToast({ type, msg });
    setTimeout(() => setToast(null), 4000);
  };

  const openCreate = () => {
    setForm({ fullName: '', email: '', password: '', roles: ['GP'], isActive: true, title: 'Mr.', gender: 'Male' });
    setPhoneCountryCode('+64');
    setLocalPhone('');
    setEditTarget(null);
    setShowModalPass(false);
    setModal('create');
  };

  const openEdit = (u: User) => {
    let code = '+64';
    let local = u.phoneNumber || '';
    for (const c of ['+64', '+61', '+1', '+44']) {
      if (local.startsWith(c)) {
        code = c;
        local = local.slice(c.length).trim();
        break;
      }
    }

    setForm({ 
      fullName: u.fullName, 
      email: u.email, 
      password: '', 
      roles: u.roles, 
      isActive: u.isActive, 
      title: u.title || 'Mr.', 
      gender: u.gender || 'Male' 
    });
    setPhoneCountryCode(code);
    setLocalPhone(local);
    setEditTarget(u);
    setShowModalPass(false);
    setModal('edit');
  };

  const toggleRoleSelection = (role: string) => {
    setForm(prev => {
      const roles = prev.roles.includes(role)
        ? prev.roles.filter(r => r !== role)
        : [...prev.roles, role];
      return { ...prev, roles: roles.length === 0 ? ['GP'] : roles }; 
    });
  };

  const togglePasswordVisibility = (userId: string) => {
    setVisiblePasswords(prev => ({ ...prev, [userId]: !prev[userId] }));
  };

  const handleSubmit = async () => {
    if (!currentUser) return;

    const cleanFullName = form.fullName.trim();
    const cleanEmail = form.email.trim();

    if (!cleanFullName) {
      showToast('error', 'Full Name is required.');
      return;
    }

    if (!cleanEmail) {
      showToast('error', 'Email Address is required.');
      return;
    }

    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(cleanEmail)) {
      showToast('error', 'Please enter a valid email address format.');
      return;
    }

    if (!editTarget && !form.password) {
      showToast('error', 'Password is required.');
      return;
    }

    // Validate NZ phone format if NZ selected
    const cleanPhoneDigits = localPhone.trim().replace(/[\s\-\(\)]/g, '');
    const localPhoneNormalized = cleanPhoneDigits.startsWith('0') ? cleanPhoneDigits.slice(1) : cleanPhoneDigits;
    
    if (phoneCountryCode === '+64' && localPhoneNormalized) {
      const nzPhoneRegex = /^[23479]\d{6,8}$/;
      if (!nzPhoneRegex.test(localPhoneNormalized)) {
        showToast('error', 'Please enter a valid NZ phone number (e.g. 21 123 4567).');
        return;
      }
    }

    const fullPhoneNumber = localPhoneNormalized ? `${phoneCountryCode} ${localPhoneNormalized}` : '';

    setSaving(true);
    try {
      const url = editTarget
        ? `${process.env.NEXT_PUBLIC_API_URL}/api/users/${editTarget.id}`
        : `${process.env.NEXT_PUBLIC_API_URL}/api/users`;
      const method = editTarget ? 'PUT' : 'POST';
      
      const body = editTarget
        ? { 
            fullName: cleanFullName,
            email: cleanEmail,
            roles: form.roles.map(r => r === 'Admin' ? 1 : r === 'TriageNurse' ? 2 : 3), 
            isActive: form.isActive, 
            newPassword: form.password || undefined,
            title: form.title,
            gender: form.gender,
            phoneNumber: fullPhoneNumber
          }
        : { 
            fullName: cleanFullName, 
            email: cleanEmail, 
            password: form.password, 
            roles: form.roles.map(r => r === 'Admin' ? 1 : r === 'TriageNurse' ? 2 : 3),
            title: form.title,
            gender: form.gender,
            phoneNumber: fullPhoneNumber
          };

      const res = await fetch(url, {
        method,
        headers: { Authorization: `Bearer ${currentUser.token}`, 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
      });
      let data: { message?: string } = {};
      try {
        data = await res.json();
      } catch {
        data = {};
      }
      if (res.ok) { 
        showToast('success', editTarget ? 'User updated!' : 'User created!'); 
        setModal(null); 
        loadUsers(currentPage); 
      } else if (res.status >= 500) {
        showToast('error', 'Unable to save record. Please try again.');
      } else {
        showToast('error', data.message || 'Unable to save record. Please check your inputs and try again.');
      }
    } catch {
      showToast('error', 'Unable to save record. Please check your connection and try again.');
    } finally { setSaving(false); }
  };

  const deactivate = async (u: User) => {
    if (!currentUser) return;
    const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/users/${u.id}`, {
      method: 'DELETE',
      headers: { Authorization: `Bearer ${currentUser.token}` }
    });
    if (res.ok) { 
      showToast('success', 'User deactivated'); 
      setConfirmDeactivateId(null);
      loadUsers(currentPage); 
    } else {
      showToast('error', 'Failed to deactivate user. Please try again.');
      setConfirmDeactivateId(null);
    }
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-slate-50 flex items-center justify-center">
        <Loader2 className="w-8 h-8 animate-spin text-blue-600" />
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-slate-50 p-6">
      <div className="max-w-5xl mx-auto">
        {/* Toast */}
        {toast && (
          <div className={`fixed top-20 right-4 z-[100] flex items-center gap-2 px-5 py-3 rounded-xl border shadow-lg text-sm font-semibold ${toast.type === 'success' ? 'bg-emerald-50 border-emerald-200 text-emerald-700' : 'bg-red-50 border-red-200 text-red-700'}`}>
            {toast.type === 'success' ? <CheckCircle2 className="w-4 h-4" /> : <AlertCircle className="w-4 h-4" />}
            {toast.msg}
          </div>
        )}

        {/* Header */}
        <div className="flex items-center justify-between mb-6">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-purple-50 border border-purple-100 flex items-center justify-center">
              <Users className="w-5 h-5 text-purple-600" />
            </div>
            <div>
              <h1 className="text-xl font-bold text-slate-900">User Management</h1>
              <p className="text-slate-500 text-sm font-medium">Manage system users and their roles</p>
            </div>
          </div>
          <button onClick={openCreate}
            className="flex items-center gap-2 px-4 py-2 bg-blue-600 hover:bg-blue-500 rounded-xl text-sm text-white font-bold transition-all shadow-md shadow-blue-500/10">
            <Plus className="w-4 h-4" /> Add User
          </button>
        </div>

        {/* Search Input */}
        <div className="mb-6">
          <div className="relative max-w-md">
            <Search className="absolute left-3.5 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400" />
            <input
              type="text"
              value={searchQuery}
              onChange={e => setSearchQuery(e.target.value)}
              placeholder="Search users by Full Name or Email..."
              className="w-full pl-10 pr-4 py-2.5 bg-white border border-slate-200 rounded-xl text-slate-900 text-sm focus:outline-none focus:ring-2 focus:ring-purple-500/20 focus:border-purple-500 font-semibold transition-all shadow-sm"
            />
          </div>
        </div>

        {/* Table & Pagination */}
        {loading ? (
          <div className="flex justify-center py-16"><Loader2 className="w-6 h-6 animate-spin text-blue-600" /></div>
        ) : (
          <>
            <div className="bg-white border border-slate-200 rounded-2xl overflow-hidden shadow-sm">
              <table className="w-full">
                <thead>
                  <tr className="border-b border-slate-200 bg-slate-50">
                    {['User','Roles','Gender','Phone','Status','Actions'].map(h => (
                      <th key={h} className="text-left px-5 py-3.5 text-xs font-bold text-slate-500 uppercase tracking-wider">{h}</th>
                    ))}
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {users.map(u => (
                    <tr key={u.id} className="hover:bg-slate-50/50 transition-colors">
                      <td className="px-5 py-4">
                        <div className="flex items-center gap-3">
                          <div className="w-9 h-9 rounded-full bg-gradient-to-br from-blue-500 to-indigo-600 flex items-center justify-center text-white text-xs font-bold">
                            {u.fullName.split(' ').map(n => n[0]).join('').slice(0,2).toUpperCase()}
                          </div>
                          <div>
                            <p className="text-slate-900 text-sm font-semibold">
                              {u.title ? u.title + ' ' : ''}{u.fullName}
                            </p>
                            <p className="text-slate-400 text-xs mt-0.5">{u.email}</p>
                          </div>
                        </div>
                      </td>
                      <td className="px-5 py-4">
                        <div className="flex flex-wrap gap-1">
                          {u.roles.map(r => (
                            <span key={r} className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] border font-semibold ${roleColors[r] || 'bg-slate-50 text-slate-500 border-slate-200'}`}>
                              {roleIcons[r]}
                              {r === 'TriageNurse' ? 'Triage Nurse' : r}
                            </span>
                          ))}
                        </div>
                      </td>
                      <td className="px-5 py-4 text-xs font-semibold text-slate-500">
                        {u.gender || 'Other'}
                      </td>
                      <td className="px-5 py-4 text-xs font-bold text-slate-700">
                        {u.phoneNumber || '—'}
                      </td>
                      <td className="px-5 py-4">
                        {u.isActive
                          ? <span className="inline-flex items-center gap-1 text-xs font-semibold text-emerald-600 bg-emerald-50 px-2 py-0.5 border border-emerald-100 rounded-full"><CheckCircle2 className="w-3 h-3" />Active</span>
                          : <span className="inline-flex items-center gap-1 text-xs font-semibold text-red-600 bg-red-50 px-2 py-0.5 border border-red-100 rounded-full"><XCircle className="w-3 h-3" />Inactive</span>}
                      </td>
                      <td className="px-5 py-4">
                        <div className="flex items-center gap-1">
                          {/* Edit button */}
                          <button 
                            onClick={() => openEdit(u)} 
                            title="Edit user"
                            className="p-1.5 text-slate-400 hover:text-blue-600 hover:bg-blue-50 rounded-lg transition-all"
                          >
                            <Pencil className="w-4 h-4" />
                          </button>

                          {/* Deactivate button — inline 2-step confirmation */}
                          {u.isActive && u.id !== currentUser?.id && (
                            confirmDeactivateId === u.id ? (
                              <div className="flex items-center gap-1 bg-red-50 border border-red-200 rounded-lg px-2 py-1">
                                <span className="text-xs font-bold text-red-600 whitespace-nowrap">Deactivate?</span>
                                <button
                                  onClick={() => deactivate(u)}
                                  title="Confirm deactivation"
                                  className="text-xs font-bold text-white bg-red-500 hover:bg-red-600 rounded px-2 py-0.5 transition-all"
                                >
                                  Yes
                                </button>
                                <button
                                  onClick={() => setConfirmDeactivateId(null)}
                                  title="Cancel"
                                  className="text-xs font-bold text-slate-500 hover:text-slate-700 rounded px-1 py-0.5 transition-all"
                                >
                                  No
                                </button>
                              </div>
                            ) : (
                              <button 
                                onClick={() => setConfirmDeactivateId(u.id)} 
                                title="Deactivate user"
                                className="p-1.5 text-slate-400 hover:text-red-600 hover:bg-red-50 rounded-lg transition-all"
                              >
                                <UserX className="w-4 h-4" />
                              </button>
                            )
                          )}
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {/* Pagination Controls */}
            {totalPages > 1 && (
              <div className="flex items-center justify-between border-t border-slate-200 bg-white px-6 py-4 rounded-xl shadow-sm mt-4">
                <div className="flex flex-1 justify-between sm:hidden">
                  <button
                    onClick={() => setCurrentPage(prev => Math.max(prev - 1, 1))}
                    disabled={currentPage === 1}
                    className="relative inline-flex items-center rounded-xl border border-slate-200 bg-white px-4 py-2 text-xs font-bold text-slate-700 hover:bg-slate-50 transition-all disabled:opacity-50"
                  >
                    Previous
                  </button>
                  <button
                    onClick={() => setCurrentPage(prev => Math.min(prev + 1, totalPages))}
                    disabled={currentPage === totalPages}
                    className="relative ml-3 inline-flex items-center rounded-xl border border-slate-200 bg-white px-4 py-2 text-xs font-bold text-slate-700 hover:bg-slate-50 transition-all disabled:opacity-50"
                  >
                    Next
                  </button>
                </div>
                <div className="hidden sm:flex sm:flex-1 sm:items-center sm:justify-between">
                  <div>
                    <p className="text-xs font-semibold text-slate-500">
                      Showing <span className="font-extrabold text-slate-800">{(currentPage - 1) * 15 + 1}</span> to{' '}
                      <span className="font-extrabold text-slate-800">
                        {Math.min(currentPage * 15, totalCount)}
                      </span>{' '}
                      of <span className="font-extrabold text-slate-800">{totalCount}</span> results
                    </p>
                  </div>
                  <div>
                    <nav className="isolate inline-flex -space-x-px rounded-xl shadow-sm border border-slate-200" aria-label="Pagination">
                      <button
                        onClick={() => setCurrentPage(prev => Math.max(prev - 1, 1))}
                        disabled={currentPage === 1}
                        className="relative inline-flex items-center rounded-l-xl px-3 py-2 text-slate-400 hover:bg-slate-50 disabled:opacity-50"
                      >
                        <span className="sr-only">Previous</span>
                        <ChevronDown className="h-4 w-4 rotate-90" />
                      </button>
                      
                      {Array.from({ length: totalPages }, (_, i) => i + 1).map(p => {
                        const isCurrent = p === currentPage;
                        return (
                          <button
                            key={p}
                            onClick={() => setCurrentPage(p)}
                            className={`relative inline-flex items-center px-4 py-2 text-xs font-bold focus:z-20 transition-all ${
                              isCurrent
                                ? 'z-10 bg-purple-600 text-white'
                                : 'text-slate-900 border-l border-slate-200 hover:bg-slate-50'
                            }`}
                          >
                            {p}
                          </button>
                        );
                      })}

                      <button
                        onClick={() => setCurrentPage(prev => Math.min(prev + 1, totalPages))}
                        disabled={currentPage === totalPages}
                        className="relative inline-flex items-center rounded-r-xl px-3 py-2 text-slate-400 border-l border-slate-200 hover:bg-slate-50 disabled:opacity-50"
                      >
                        <span className="sr-only">Next</span>
                        <ChevronDown className="h-4 w-4 -rotate-90" />
                      </button>
                    </nav>
                  </div>
                </div>
              </div>
            )}
          </>
        )}
      </div>

      {/* Modal */}
      {modal && (
        <div className="fixed inset-0 bg-slate-900/40 backdrop-blur-sm flex items-center justify-center z-50 p-4">
          <div className="bg-white border border-slate-200 rounded-2xl p-6 w-full max-w-md shadow-xl max-h-[90vh] overflow-y-auto">
            <div className="flex items-center justify-between mb-6">
              <h2 className="text-lg font-bold text-slate-900">{modal === 'create' ? 'Create User' : 'Edit User'}</h2>
              <button onClick={() => setModal(null)} className="text-slate-400 hover:text-slate-600"><X className="w-5 h-5" /></button>
            </div>

            <div className="space-y-4">
              <div className="grid grid-cols-3 gap-2">
                <div>
                  <label className="text-xs text-slate-400 font-bold uppercase tracking-wider mb-1.5 block">Title</label>
                  <select value={form.title} onChange={e => setForm(f => ({...f, title: e.target.value}))}
                    className="w-full px-3 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-slate-900 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 font-medium">
                    {['Mr.', 'Mrs.', 'Dr.', 'Ms.'].map(t => <option key={t}>{t}</option>)}
                  </select>
                </div>
                <div className="col-span-2">
                  <label className="text-xs text-slate-400 font-bold uppercase tracking-wider mb-1.5 block">Full Name</label>
                  <input value={form.fullName} onChange={e => setForm(f => ({...f, fullName: e.target.value}))}
                    placeholder="John Doe"
                    maxLength={200}
                    className="w-full px-4 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-slate-900 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 font-medium" />
                </div>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="text-xs text-slate-400 font-bold uppercase tracking-wider mb-1.5 block">Email Address</label>
                  <input type="email" value={form.email} onChange={e => setForm(f => ({...f, email: e.target.value}))}
                    placeholder="email@example.com"
                    maxLength={256}
                    className="w-full px-4 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-slate-900 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 font-medium" />
                </div>
                <div>
                  <label className="text-xs text-slate-400 font-bold uppercase tracking-wider mb-1.5 block">Gender</label>
                  <select value={form.gender} onChange={e => setForm(f => ({...f, gender: e.target.value}))}
                    className="w-full px-4 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-slate-900 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 font-medium">
                    <option>Male</option>
                    <option>Female</option>
                    <option>Other</option>
                  </select>
                </div>
              </div>

              <div>
                <label className="text-xs text-slate-400 font-bold uppercase tracking-wider mb-1.5 block">Contact Number (NZ Format)</label>
                <div className="flex gap-2">
                  <select value={phoneCountryCode} onChange={e => setPhoneCountryCode(e.target.value)}
                    className="w-28 bg-slate-50 border border-slate-200 rounded-xl text-slate-900 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 font-bold px-2">
                    <option value="+64">🇳🇿 +64 (NZ)</option>
                    <option value="+61">🇦🇺 +61 (AU)</option>
                    <option value="+1">🇺🇸 +1 (US)</option>
                    <option value="+44">🇬🇧 +44 (UK)</option>
                  </select>
                  <input type="text" value={localPhone} 
                    onChange={e => {
                      const val = e.target.value;
                      const cleanVal = val.replace(/[^0-9\s\-]/g, '');
                      setLocalPhone(cleanVal);
                    }}
                    placeholder="e.g. 21 123 4567"
                    maxLength={50}
                    className="flex-1 px-4 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-slate-900 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 font-medium" />
                </div>
              </div>

              <div>
                <label className="text-xs text-slate-400 font-bold uppercase tracking-wider mb-1.5 block">
                  {modal === 'edit' ? 'New Password (leave blank to keep)' : 'Password'}
                </label>
                <div className="relative">
                  <input type={showModalPass ? 'text' : 'password'} value={form.password} onChange={e => setForm(f => ({...f, password: e.target.value}))}
                    placeholder={modal === 'edit' ? 'Keep current password' : '••••••••'}
                    maxLength={100}
                    className="w-full pl-4 pr-12 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-slate-900 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 font-medium" />
                  <button type="button" onClick={() => setShowModalPass(!showModalPass)} className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600">
                    {showModalPass ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                  </button>
                </div>
              </div>

              <div>
                <label className="text-xs text-slate-400 font-bold uppercase tracking-wider mb-2.5 block">Assigned Roles (Select all that apply)</label>
                <div className="space-y-2 border border-slate-200 rounded-xl p-3 bg-slate-50">
                  {['Admin', 'TriageNurse', 'GP'].map(role => {
                    const isChecked = form.roles.includes(role);
                    return (
                      <label key={role} className="flex items-center gap-3 cursor-pointer py-1">
                        <input
                          type="checkbox"
                          checked={isChecked}
                          onChange={() => toggleRoleSelection(role)}
                          className="w-4 h-4 text-blue-600 border-slate-300 rounded focus:ring-blue-500 accent-blue-600"
                        />
                        <span className="text-sm font-semibold text-slate-700">
                          {role === 'TriageNurse' ? 'Triage Nurse' : role}
                        </span>
                      </label>
                    );
                  })}
                </div>
              </div>
              {modal === 'edit' && (
                <div className="flex items-center gap-3 py-1">
                  <input type="checkbox" id="active" checked={form.isActive} onChange={e => setForm(f => ({...f, isActive: e.target.checked}))} className="w-4 h-4 text-blue-600 border-slate-300 rounded focus:ring-blue-500 accent-blue-600" />
                  <label htmlFor="active" className="text-sm font-semibold text-slate-700">Active</label>
                </div>
              )}
            </div>

            <div className="flex gap-3 mt-6">
              <button onClick={() => setModal(null)} className="flex-1 py-2.5 border border-slate-200 text-slate-500 rounded-xl text-sm font-semibold hover:bg-slate-50 transition-all">Cancel</button>
              <button onClick={handleSubmit} disabled={saving}
                className="flex-1 py-2.5 bg-blue-600 hover:bg-blue-500 text-white rounded-xl text-sm font-bold flex items-center justify-center gap-2 transition-all disabled:opacity-50">
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
