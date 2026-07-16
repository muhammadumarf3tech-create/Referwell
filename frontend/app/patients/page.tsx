'use client';

import { useEffect, useState } from 'react';
import { useAuth } from '@/context/AuthContext';
import { useRouter } from 'next/navigation';
import {
  ContactRound, Plus, Pencil, Loader2, X, Save, AlertCircle,
  CheckCircle2, Search, ChevronDown
} from 'lucide-react';
import { fetchMenuAccess, hasMenuAccess } from '@/lib/menuAccess';
import { apiFetch } from '@/lib/api';

interface Patient {
  id: string;
  name: string;
  dateOfBirth: string;
  email: string;
  phoneNumber: string;
  nhiNumber: string;
  gender: string;
  createdAt: string;
}

type ModalMode = 'create' | 'edit' | null;

const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

/** Compact page list: 1 … 4 5 6 … 670 */
function getVisiblePages(current: number, total: number): Array<number | 'ellipsis'> {
  if (total <= 7) {
    return Array.from({ length: total }, (_, i) => i + 1);
  }

  const pages = new Set<number>();
  pages.add(1);
  pages.add(total);
  for (let p = current - 1; p <= current + 1; p++) {
    if (p >= 1 && p <= total) pages.add(p);
  }
  if (current <= 3) {
    pages.add(2);
    pages.add(3);
    pages.add(4);
  }
  if (current >= total - 2) {
    pages.add(total - 1);
    pages.add(total - 2);
    pages.add(total - 3);
  }

  const sorted = [...pages].sort((a, b) => a - b);
  const result: Array<number | 'ellipsis'> = [];
  let prev = 0;
  for (const p of sorted) {
    if (prev && p - prev > 1) result.push('ellipsis');
    result.push(p);
    prev = p;
  }
  return result;
}

function toDateInputValue(iso: string) {
  if (!iso) return '';
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return iso.slice(0, 10);
  const yyyy = d.getFullYear();
  const mm = String(d.getMonth() + 1).padStart(2, '0');
  const dd = String(d.getDate()).padStart(2, '0');
  return `${yyyy}-${mm}-${dd}`;
}

export default function PatientsPage() {
  const { user: currentUser, isLoading } = useAuth();
  const router = useRouter();
  const [patients, setPatients] = useState<Patient[]>([]);
  const [loading, setLoading] = useState(true);
  const [modal, setModal] = useState<ModalMode>(null);
  const [editTarget, setEditTarget] = useState<Patient | null>(null);
  const [form, setForm] = useState({
    name: '', dateOfBirth: '', email: '', nhiNumber: '', gender: 'Male'
  });
  const [phoneCountryCode, setPhoneCountryCode] = useState('+64');
  const [localPhone, setLocalPhone] = useState('');
  const [saving, setSaving] = useState(false);
  const [toast, setToast] = useState<{ type: 'success' | 'error'; msg: string } | null>(null);

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
      if (!hasMenuAccess('Patients', currentUser.roles, accesses)) {
        router.push('/dashboard');
      }
    })();

    return () => { cancelled = true; };
  }, [currentUser, isLoading, router]);

  const loadPatients = async (pageOverride?: number) => {
    if (!currentUser) return;
    setLoading(true);
    try {
      const pageToFetch = pageOverride ?? currentPage;
      const params = new URLSearchParams();
      params.set('page', pageToFetch.toString());
      params.set('pageSize', '15');
      if (searchQuery) params.set('search', searchQuery);

      const res = await apiFetch(`/api/patients?${params}`, {
        token: currentUser.token
      });
      if (res.ok) {
        const data = await res.json();
        setPatients(data.items ?? []);
        setTotalPages(data.totalPages ?? 1);
        setTotalCount(data.totalCount ?? 0);
        setCurrentPage(pageToFetch);
      }
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    setCurrentPage(1);
  }, [searchQuery]);

  useEffect(() => {
    if (!currentUser) return;
    const delayDebounceFn = setTimeout(() => {
      loadPatients(currentPage);
    }, 300);

    return () => clearTimeout(delayDebounceFn);
  }, [currentUser, searchQuery, currentPage]);

  const showToast = (type: 'success' | 'error', msg: string) => {
    setToast({ type, msg });
    setTimeout(() => setToast(null), 4000);
  };

  const parsePhone = (phone: string) => {
    let code = '+64';
    let local = phone || '';
    for (const c of ['+64', '+61', '+1', '+44']) {
      if (local.startsWith(c)) {
        code = c;
        local = local.slice(c.length).trim();
        break;
      }
    }
    return { code, local };
  };

  const openCreate = () => {
    setForm({ name: '', dateOfBirth: '', email: '', nhiNumber: '', gender: 'Male' });
    setPhoneCountryCode('+64');
    setLocalPhone('');
    setEditTarget(null);
    setModal('create');
  };

  const openEdit = (p: Patient) => {
    const { code, local } = parsePhone(p.phoneNumber);
    setForm({
      name: p.name,
      dateOfBirth: toDateInputValue(p.dateOfBirth),
      email: p.email,
      nhiNumber: p.nhiNumber,
      gender: p.gender || 'Male'
    });
    setPhoneCountryCode(code);
    setLocalPhone(local);
    setEditTarget(p);
    setModal('edit');
  };

  const handleSubmit = async () => {
    if (!currentUser) return;

    const cleanName = form.name.trim();
    const cleanEmail = form.email.trim();
    const nhiClean = form.nhiNumber.trim().toUpperCase();

    if (!cleanName) {
      showToast('error', 'Patient name is required.');
      return;
    }
    if (!form.dateOfBirth) {
      showToast('error', 'Date of birth is required.');
      return;
    }
    if (!cleanEmail) {
      showToast('error', 'Email address is required.');
      return;
    }
    if (!EMAIL_REGEX.test(cleanEmail)) {
      showToast('error', 'Please enter a valid email address format.');
      return;
    }
    if (!/^[A-Z]{3}\d{4}$/.test(nhiClean)) {
      showToast('error', 'NHI Number must be in format ABC1234 (3 letters followed by 4 digits).');
      return;
    }

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
      const url = editTarget ? `/api/patients/${editTarget.id}` : `/api/patients`;
      const method = editTarget ? 'PUT' : 'POST';
      const body = {
        name: cleanName,
        dateOfBirth: form.dateOfBirth,
        email: cleanEmail,
        nhiNumber: nhiClean,
        gender: form.gender,
        phoneNumber: fullPhoneNumber
      };

      const res = await apiFetch(url, {
        method,
        token: currentUser.token,
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
      });
      let data: { message?: string } = {};
      try {
        data = await res.json();
      } catch {
        data = {};
      }

      if (res.ok) {
        showToast('success', editTarget ? 'Patient updated!' : 'Patient registered!');
        setModal(null);
        loadPatients(currentPage);
      } else if (res.status >= 500) {
        showToast('error', 'Unable to save record. Please try again.');
      } else {
        showToast('error', data.message || 'Unable to save record. Please check your inputs and try again.');
      }
    } catch {
      showToast('error', 'Unable to save record. Please check your connection and try again.');
    } finally {
      setSaving(false);
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
        {toast && (
          <div className={`fixed top-20 right-4 z-[100] flex items-center gap-2 px-5 py-3 rounded-xl border shadow-lg text-sm font-semibold ${toast.type === 'success' ? 'bg-emerald-50 border-emerald-200 text-emerald-700' : 'bg-red-50 border-red-200 text-red-700'}`}>
            {toast.type === 'success' ? <CheckCircle2 className="w-4 h-4" /> : <AlertCircle className="w-4 h-4" />}
            {toast.msg}
          </div>
        )}

        <div className="flex items-center justify-between mb-6">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-teal-50 border border-teal-100 flex items-center justify-center">
              <ContactRound className="w-5 h-5 text-teal-600" />
            </div>
            <div>
              <h1 className="text-xl font-bold text-slate-900">Patients</h1>
              <p className="text-slate-500 text-sm font-medium">View and update the patient register</p>
            </div>
          </div>
          <button onClick={openCreate}
            className="flex items-center gap-2 px-4 py-2 bg-blue-600 hover:bg-blue-500 rounded-xl text-sm text-white font-bold transition-all shadow-md shadow-blue-500/10">
            <Plus className="w-4 h-4" /> Register Patient
          </button>
        </div>

        <div className="mb-6">
          <div className="relative max-w-md">
            <Search className="absolute left-3.5 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400" />
            <input
              type="text"
              value={searchQuery}
              onChange={e => setSearchQuery(e.target.value)}
              placeholder="Search by name, NHI, email or phone..."
              className="w-full pl-10 pr-4 py-2.5 bg-white border border-slate-200 rounded-xl text-slate-900 text-sm focus:outline-none focus:ring-2 focus:ring-teal-500/20 focus:border-teal-500 font-semibold transition-all shadow-sm"
            />
          </div>
        </div>

        {loading ? (
          <div className="flex justify-center py-16"><Loader2 className="w-6 h-6 animate-spin text-blue-600" /></div>
        ) : patients.length === 0 ? (
          <div className="bg-white border border-slate-200 rounded-2xl p-12 text-center text-slate-500 font-medium shadow-sm">
            No patients found
          </div>
        ) : (
          <>
            <div className="bg-white border border-slate-200 rounded-2xl overflow-hidden shadow-sm">
              <table className="w-full">
                <thead>
                  <tr className="border-b border-slate-200 bg-slate-50">
                    {['Patient', 'NHI', 'DOB', 'Gender', 'Phone', 'Actions'].map(h => (
                      <th key={h} className="text-left px-5 py-3.5 text-xs font-bold text-slate-500 uppercase tracking-wider">{h}</th>
                    ))}
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {patients.map(p => (
                    <tr key={p.id} className="hover:bg-slate-50/50 transition-colors">
                      <td className="px-5 py-4">
                        <div className="flex items-center gap-3">
                          <div className="w-9 h-9 rounded-full bg-gradient-to-br from-teal-500 to-cyan-600 flex items-center justify-center text-white text-xs font-bold">
                            {p.name.split(' ').map(n => n[0]).join('').slice(0, 2).toUpperCase()}
                          </div>
                          <div>
                            <p className="text-slate-900 text-sm font-semibold">{p.name}</p>
                            <p className="text-slate-400 text-xs mt-0.5">{p.email}</p>
                          </div>
                        </div>
                      </td>
                      <td className="px-5 py-4 text-xs font-bold text-slate-700 uppercase">{p.nhiNumber}</td>
                      <td className="px-5 py-4 text-xs font-semibold text-slate-500">
                        {toDateInputValue(p.dateOfBirth)}
                      </td>
                      <td className="px-5 py-4 text-xs font-semibold text-slate-500">{p.gender || '—'}</td>
                      <td className="px-5 py-4 text-xs font-bold text-slate-700">{p.phoneNumber || '—'}</td>
                      <td className="px-5 py-4">
                        <button
                          onClick={() => openEdit(p)}
                          title="Edit patient"
                          className="p-1.5 text-slate-400 hover:text-blue-600 hover:bg-blue-50 rounded-lg transition-all"
                        >
                          <Pencil className="w-4 h-4" />
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {totalPages > 1 && (
              <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between border-t border-slate-200 bg-white px-6 py-4 rounded-xl shadow-sm mt-4">
                <p className="text-xs font-semibold text-slate-500">
                  Showing <span className="font-extrabold text-slate-800">{(currentPage - 1) * 15 + 1}</span> to{' '}
                  <span className="font-extrabold text-slate-800">
                    {Math.min(currentPage * 15, totalCount)}
                  </span>{' '}
                  of <span className="font-extrabold text-slate-800">{totalCount.toLocaleString()}</span> results
                  <span className="text-slate-400 font-medium"> · Page {currentPage} of {totalPages.toLocaleString()}</span>
                </p>
                <nav className="inline-flex items-center gap-1 rounded-xl shadow-sm border border-slate-200 bg-white p-1" aria-label="Pagination">
                  <button
                    onClick={() => setCurrentPage(prev => Math.max(prev - 1, 1))}
                    disabled={currentPage === 1}
                    className="inline-flex items-center gap-1 rounded-lg px-3 py-1.5 text-xs font-bold text-slate-600 hover:bg-slate-50 disabled:opacity-40 disabled:hover:bg-transparent"
                  >
                    <ChevronDown className="h-3.5 w-3.5 rotate-90" />
                    Prev
                  </button>

                  {getVisiblePages(currentPage, totalPages).map((item, idx) =>
                    item === 'ellipsis' ? (
                      <span key={`e-${idx}`} className="px-2 text-xs font-bold text-slate-400 select-none">…</span>
                    ) : (
                      <button
                        key={item}
                        onClick={() => setCurrentPage(item)}
                        className={`min-w-[2rem] rounded-lg px-2.5 py-1.5 text-xs font-bold transition-all ${
                          item === currentPage
                            ? 'bg-teal-600 text-white shadow-sm'
                            : 'text-slate-700 hover:bg-slate-50'
                        }`}
                      >
                        {item}
                      </button>
                    )
                  )}

                  <button
                    onClick={() => setCurrentPage(prev => Math.min(prev + 1, totalPages))}
                    disabled={currentPage === totalPages}
                    className="inline-flex items-center gap-1 rounded-lg px-3 py-1.5 text-xs font-bold text-slate-600 hover:bg-slate-50 disabled:opacity-40 disabled:hover:bg-transparent"
                  >
                    Next
                    <ChevronDown className="h-3.5 w-3.5 -rotate-90" />
                  </button>
                </nav>
              </div>
            )}
          </>
        )}
      </div>

      {modal && (
        <div className="fixed inset-0 bg-slate-900/40 backdrop-blur-sm flex items-center justify-center z-50 p-4">
          <div className="bg-white border border-slate-200 rounded-2xl p-6 w-full max-w-md shadow-xl max-h-[90vh] overflow-y-auto">
            <div className="flex items-center justify-between mb-6">
              <h2 className="text-lg font-bold text-slate-900">{modal === 'create' ? 'Register Patient' : 'Update Patient'}</h2>
              <button onClick={() => setModal(null)} className="text-slate-400 hover:text-slate-600"><X className="w-5 h-5" /></button>
            </div>

            <div className="space-y-4">
              <div>
                <label className="text-xs text-slate-400 font-bold uppercase tracking-wider mb-1.5 block">Full Name</label>
                <input
                  value={form.name}
                  onChange={e => setForm(f => ({ ...f, name: e.target.value }))}
                  placeholder="Jane Doe"
                  maxLength={200}
                  className="w-full px-4 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-slate-900 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 font-medium"
                />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="text-xs text-slate-400 font-bold uppercase tracking-wider mb-1.5 block">Date of Birth</label>
                  <input
                    type="date"
                    value={form.dateOfBirth}
                    onChange={e => setForm(f => ({ ...f, dateOfBirth: e.target.value }))}
                    className="w-full px-4 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-slate-900 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 font-medium"
                  />
                </div>
                <div>
                  <label className="text-xs text-slate-400 font-bold uppercase tracking-wider mb-1.5 block">Gender</label>
                  <select
                    value={form.gender}
                    onChange={e => setForm(f => ({ ...f, gender: e.target.value }))}
                    className="w-full px-4 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-slate-900 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 font-medium"
                  >
                    <option>Male</option>
                    <option>Female</option>
                    <option>Other</option>
                  </select>
                </div>
              </div>

              <div>
                <label className="text-xs text-slate-400 font-bold uppercase tracking-wider mb-1.5 block">NHI Number</label>
                <input
                  value={form.nhiNumber}
                  onChange={e => setForm(f => ({ ...f, nhiNumber: e.target.value }))}
                  placeholder="e.g. ABC1234"
                  maxLength={50}
                  className="w-full px-4 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-slate-900 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 font-medium uppercase"
                />
              </div>

              <div>
                <label className="text-xs text-slate-400 font-bold uppercase tracking-wider mb-1.5 block">Email Address</label>
                <input
                  type="email"
                  required
                  value={form.email}
                  onChange={e => setForm(f => ({ ...f, email: e.target.value }))}
                  placeholder="e.g. jane.doe@example.com"
                  maxLength={256}
                  pattern="[^\s@]+@[^\s@]+\.[^\s@]+"
                  title="Enter a valid email address"
                  className="w-full px-4 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-slate-900 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 font-medium"
                />
              </div>

              <div>
                <label className="text-xs text-slate-400 font-bold uppercase tracking-wider mb-1.5 block">Contact Number</label>
                <div className="flex gap-2">
                  <select
                    value={phoneCountryCode}
                    onChange={e => setPhoneCountryCode(e.target.value)}
                    className="w-28 bg-slate-50 border border-slate-200 rounded-xl text-slate-900 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 font-bold px-2"
                  >
                    <option value="+64">+64 (NZ)</option>
                    <option value="+61">+61 (AU)</option>
                    <option value="+1">+1 (US)</option>
                    <option value="+44">+44 (UK)</option>
                  </select>
                  <input
                    type="text"
                    value={localPhone}
                    onChange={e => setLocalPhone(e.target.value)}
                    placeholder="e.g. 21 123 4567"
                    maxLength={50}
                    className="flex-1 px-4 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-slate-900 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 font-medium"
                  />
                </div>
              </div>

              <div className="flex gap-3 mt-6 pt-2">
                <button
                  type="button"
                  onClick={() => setModal(null)}
                  className="flex-1 py-2.5 border border-slate-200 text-slate-500 rounded-xl text-sm font-semibold hover:bg-slate-50 transition-all"
                >
                  Cancel
                </button>
                <button
                  type="button"
                  onClick={handleSubmit}
                  disabled={saving}
                  className="flex-1 flex items-center justify-center gap-2 py-2.5 bg-blue-600 hover:bg-blue-500 disabled:opacity-60 text-white rounded-xl text-sm font-bold transition-all"
                >
                  {saving ? <Loader2 className="w-4 h-4 animate-spin" /> : <Save className="w-4 h-4" />}
                  {editTarget ? 'Save Changes' : 'Register'}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
