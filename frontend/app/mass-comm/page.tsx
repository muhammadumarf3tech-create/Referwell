'use client';

import { useEffect, useState } from 'react';
import { useAuth } from '@/context/AuthContext';
import { useRouter } from 'next/navigation';
import {
  MessageSquare, Send, Plus, X, Loader2,
  CheckCircle2, AlertCircle, Users, Filter, Eye
} from 'lucide-react';

const formatDateOnly = (dateStr: string | null | undefined) => {
  if (!dateStr) return '—';
  const date = new Date(dateStr);
  if (isNaN(date.getTime())) return '—';
  const dd = String(date.getDate()).padStart(2, '0');
  const MM = String(date.getMonth() + 1).padStart(2, '0');
  const yyyy = date.getFullYear();
  return `${dd}/${MM}/${yyyy}`;
};

interface Campaign {
  id: string;
  name: string;
  status: string;
  createdAt: string;
  createdByUser?: { fullName: string };
  totalMessages: number;
  sentMessages: number;
  failedMessages: number;
}

export default function MassCommPage() {
  const { user } = useAuth();
  const router = useRouter();
  const [campaigns, setCampaigns] = useState<Campaign[]>([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [form, setForm] = useState({
    name: '', subjectTemplate: '', bodyTemplate: '',
    urgencyFilter: '', statusFilter: ''
  });
  const [saving, setSaving] = useState(false);
  const [toast, setToast] = useState<{ type: 'success' | 'error'; msg: string } | null>(null);

  useEffect(() => {
    if (!user) { router.push('/login'); return; }
    
    // Check if user is TriageNurse or Admin
    const hasAccess = user.roles.includes('Admin') || user.roles.includes('TriageNurse');
    if (!hasAccess) { router.push('/dashboard'); return; }
    
    loadCampaigns();
  }, [user, router]);

  const loadCampaigns = async () => {
    if (!user) return;
    setLoading(true);
    const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/masscomm`, {
      headers: { Authorization: `Bearer ${user.token}` }
    });
    if (res.ok) setCampaigns(await res.json());
    setLoading(false);
  };

  const handleSend = async () => {
    if (!user) return;
    setSaving(true);
    try {
      const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/masscomm`, {
        method: 'POST',
        headers: { Authorization: `Bearer ${user.token}`, 'Content-Type': 'application/json' },
        body: JSON.stringify(form),
      });
      const data = await res.json();
      if (res.ok) {
        setToast({ type: 'success', msg: `Campaign created — ${data.messageCount} messages queued` });
        setShowModal(false);
        loadCampaigns();
      } else {
        setToast({ type: 'error', msg: data.message || 'Failed to send' });
      }
    } finally {
      setSaving(false);
      setTimeout(() => setToast(null), 4000);
    }
  };

  const statusColor = (status: string) =>
    status === 'Completed' ? 'text-emerald-600 bg-emerald-50 border-emerald-100' :
    status === 'Sending' ? 'text-blue-600 bg-blue-50 border-blue-100' :
    'text-slate-500 bg-slate-50 border-slate-100';

  return (
    <div className="min-h-screen bg-slate-50 p-6">
      <div className="max-w-5xl mx-auto">
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
            <div className="w-10 h-10 rounded-xl bg-teal-50 border border-teal-100 flex items-center justify-center">
              <MessageSquare className="w-5 h-5 text-teal-600" />
            </div>
            <div>
              <h1 className="text-xl font-bold text-slate-900">Mass Communications</h1>
              <p className="text-slate-500 text-sm font-medium">Send templated bulk messages to filtered referral cohorts</p>
            </div>
          </div>
          <button onClick={() => setShowModal(true)}
            className="flex items-center gap-2 px-4 py-2 bg-teal-600 hover:bg-teal-500 rounded-xl text-sm text-white font-bold transition-all shadow-md shadow-teal-500/25">
            <Plus className="w-4 h-4" /> New Campaign
          </button>
        </div>

        {/* Merge Fields Info */}
        <div className="bg-white border border-slate-200 rounded-xl p-4 mb-6 shadow-sm">
          <p className="text-xs text-slate-400 font-bold uppercase tracking-wider mb-2">Available Merge Fields</p>
          <div className="flex flex-wrap gap-2">
            {['{PatientName}', '{SpecialistType}', '{Status}', '{SlaDeadline}'].map(f => (
              <code key={f} className="bg-slate-50 text-blue-600 text-xs px-2.5 py-1 rounded-lg border border-slate-200 font-semibold">{f}</code>
            ))}
          </div>
        </div>

        {/* Campaign Table */}
        {loading ? (
          <div className="flex justify-center py-16"><Loader2 className="w-6 h-6 animate-spin text-blue-600" /></div>
        ) : campaigns.length === 0 ? (
          <div className="text-center py-24 bg-white border border-slate-200 rounded-2xl shadow-sm text-slate-400">
            <MessageSquare className="w-12 h-12 mx-auto mb-4 opacity-30" />
            <p className="font-semibold">No campaigns yet</p>
          </div>
        ) : (
          <div className="bg-white border border-slate-200/80 rounded-2xl overflow-hidden shadow-sm">
            <table className="w-full">
              <thead>
                <tr className="border-b border-slate-200 bg-slate-50">
                  {['Campaign', 'Status', 'Messages', 'Progress', 'Created By', 'Date'].map(h => (
                    <th key={h} className="text-left px-5 py-3.5 text-xs font-bold text-slate-500 uppercase tracking-wider">{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {campaigns.map(c => {
                  const progress = c.totalMessages > 0 ? Math.round((c.sentMessages / c.totalMessages) * 100) : 0;
                  return (
                    <tr key={c.id} className="hover:bg-slate-50/50 transition-colors">
                      <td className="px-5 py-4 text-slate-900 text-sm font-semibold">{c.name}</td>
                      <td className="px-5 py-4">
                        <span className={`inline-flex px-2 py-0.5 border text-xs font-semibold rounded-full ${statusColor(c.status)}`}>
                          {c.status}
                        </span>
                      </td>
                      <td className="px-5 py-4 text-slate-600 text-sm font-semibold">
                        <span className="text-emerald-600 font-bold">{c.sentMessages}</span>
                        <span className="text-slate-400"> / {c.totalMessages}</span>
                        {c.failedMessages > 0 && <span className="text-red-600 ml-1.5">({c.failedMessages} failed)</span>}
                      </td>
                      <td className="px-5 py-4">
                        <div className="w-28">
                          <div className="h-1.5 bg-slate-100 rounded-full overflow-hidden">
                            <div className="h-full bg-gradient-to-r from-teal-500 to-emerald-500 rounded-full transition-all" style={{ width: `${progress}%` }} />
                          </div>
                          <p className="text-[10px] text-slate-400 font-bold mt-1">{progress}% Complete</p>
                        </div>
                      </td>
                      <td className="px-5 py-4 text-slate-600 text-sm font-medium">{c.createdByUser?.fullName ?? '—'}</td>
                      <td className="px-5 py-4 text-slate-400 text-xs font-semibold">{formatDateOnly(c.createdAt)}</td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Create Campaign Modal */}
      {showModal && (
        <div className="fixed inset-0 bg-slate-900/40 backdrop-blur-sm flex items-center justify-center z-50 p-4">
          <div className="bg-white border border-slate-200 rounded-2xl p-6 w-full max-w-lg shadow-xl">
            <div className="flex items-center justify-between mb-6">
              <h2 className="text-lg font-bold text-slate-900">Create Campaign</h2>
              <button onClick={() => setShowModal(false)} className="text-slate-400 hover:text-slate-600"><X className="w-5 h-5" /></button>
            </div>

            <div className="space-y-4">
              <div>
                <label className="text-xs text-slate-400 font-bold uppercase tracking-wider mb-1.5 block">Campaign Name</label>
                <input value={form.name} onChange={e => setForm(f => ({...f, name: e.target.value}))}
                  placeholder="e.g. Urgent Cardiology Update"
                  className="w-full px-4 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-slate-900 text-sm focus:outline-none focus:ring-2 focus:ring-teal-500/20 focus:border-teal-500 font-medium" />
              </div>
              <div>
                <label className="text-xs text-slate-400 font-bold uppercase tracking-wider mb-1.5 block">Subject Template</label>
                <input value={form.subjectTemplate} onChange={e => setForm(f => ({...f, subjectTemplate: e.target.value}))}
                  placeholder="Update on {PatientName}'s referral"
                  className="w-full px-4 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-slate-900 text-sm focus:outline-none focus:ring-2 focus:ring-teal-500/20 focus:border-teal-500 font-medium" />
              </div>
              <div>
                <label className="text-xs text-slate-400 font-bold uppercase tracking-wider mb-1.5 block">Body Template</label>
                <textarea value={form.bodyTemplate} onChange={e => setForm(f => ({...f, bodyTemplate: e.target.value}))} rows={4}
                  placeholder="Dear GP,&#10;&#10;{PatientName}'s {SpecialistType} referral is now {Status}. SLA deadline: {SlaDeadline}."
                  className="w-full px-4 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-slate-900 text-sm focus:outline-none focus:ring-2 focus:ring-teal-500/20 focus:border-teal-500 resize-none font-medium" />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="text-xs text-slate-400 font-bold uppercase tracking-wider mb-1.5 block flex items-center gap-1.5"><Filter className="w-3 h-3" />Filter by Urgency</label>
                  <select value={form.urgencyFilter} onChange={e => setForm(f => ({...f, urgencyFilter: e.target.value}))}
                    className="w-full px-3 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-slate-900 text-sm focus:outline-none focus:ring-2 focus:ring-teal-500/20 focus:border-teal-500 font-medium">
                    <option value="">All Urgencies</option>
                    {['Emergency','Urgent','Soon','Routine'].map(u => <option key={u}>{u}</option>)}
                  </select>
                </div>
                <div>
                  <label className="text-xs text-slate-400 font-bold uppercase tracking-wider mb-1.5 block flex items-center gap-1.5"><Filter className="w-3 h-3" />Filter by Status</label>
                  <select value={form.statusFilter} onChange={e => setForm(f => ({...f, statusFilter: e.target.value}))}
                    className="w-full px-3 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-slate-900 text-sm focus:outline-none focus:ring-2 focus:ring-teal-500/20 focus:border-teal-500 font-medium">
                    <option value="">All Statuses</option>
                    {['Received','Triaged','Accepted','Booked'].map(s => <option key={s}>{s}</option>)}
                  </select>
                </div>
              </div>
            </div>

            <div className="flex gap-3 mt-6">
              <button onClick={() => setShowModal(false)} className="flex-1 py-2.5 border border-slate-200 text-slate-500 rounded-xl text-sm font-semibold hover:bg-slate-50 transition-all">Cancel</button>
              <button onClick={handleSend} disabled={saving || !form.name || !form.bodyTemplate}
                className="flex-1 py-2.5 bg-teal-600 hover:bg-teal-500 text-white rounded-xl text-sm font-bold flex items-center justify-center gap-2 transition-all disabled:opacity-50">
                {saving ? <Loader2 className="w-4 h-4 animate-spin" /> : <Send className="w-4 h-4" />}
                {saving ? 'Queuing...' : 'Send Campaign'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
