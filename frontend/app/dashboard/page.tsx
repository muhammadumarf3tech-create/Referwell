'use client';

import { useEffect, useState, useCallback, useRef } from 'react';
import { useAuth } from '@/context/AuthContext';
import { useRouter } from 'next/navigation';
import SlaTimer from '@/components/SlaTimer';
import * as signalR from '@microsoft/signalr';
import {
  Activity, RefreshCw, Plus, ChevronDown, ChevronUp,
  Filter, User, Clock, AlertTriangle, CheckCircle2, XCircle,
  Loader2, Lock, Unlock, TrendingUp, X
} from 'lucide-react';

interface Referral {
  id: string;
  patientName: string;
  patientDateOfBirth: string;
  specialistType: string;
  reason: string;
  urgency: string;
  status: string;
  priorityScore: number;
  receivedAt: string;
  slaDeadline: string;
  slaBreach: boolean;
  rowVersion: string;
  createdByUser?: { fullName: string; email: string };
  claimedByUser?: { fullName: string; email: string } | null;
  claimedAt?: string | null;
}

const urgencyColors: Record<string, string> = {
  Emergency: 'bg-red-500/20 text-red-300 border-red-500/40',
  Urgent:    'bg-orange-500/20 text-orange-300 border-orange-500/40',
  Soon:      'bg-yellow-500/20 text-yellow-300 border-yellow-500/40',
  Routine:   'bg-gray-500/20 text-gray-300 border-gray-500/40',
};

const statusIcons: Record<string, React.ReactNode> = {
  Received:  <Activity className="w-3.5 h-3.5" />,
  Triaged:   <Clock className="w-3.5 h-3.5" />,
  Accepted:  <CheckCircle2 className="w-3.5 h-3.5" />,
  Declined:  <XCircle className="w-3.5 h-3.5" />,
  Booked:    <CheckCircle2 className="w-3.5 h-3.5" />,
  Completed: <CheckCircle2 className="w-3.5 h-3.5" />,
};

const statusColors: Record<string, string> = {
  Received:  'bg-blue-500/20 text-blue-300 border-blue-500/40',
  Triaged:   'bg-purple-500/20 text-purple-300 border-purple-500/40',
  Accepted:  'bg-emerald-500/20 text-emerald-300 border-emerald-500/40',
  Declined:  'bg-red-500/20 text-red-300 border-red-500/40',
  Booked:    'bg-teal-500/20 text-teal-300 border-teal-500/40',
  Completed: 'bg-gray-500/20 text-gray-300 border-gray-500/40',
};

const nextStatuses: Record<string, string[]> = {
  Received:  ['Triaged'],
  Triaged:   ['Accepted', 'Declined'],
  Accepted:  ['Booked'],
  Declined:  [],
  Booked:    ['Completed'],
  Completed: [],
};

export default function DashboardPage() {
  const { user } = useAuth();
  const router = useRouter();
  const [referrals, setReferrals] = useState<Referral[]>([]);
  const [loading, setLoading] = useState(true);
  const [toast, setToast] = useState<{ type: 'success' | 'error' | 'warning'; msg: string } | null>(null);
  const [filterStatus, setFilterStatus] = useState('');
  const [filterUrgency, setFilterUrgency] = useState('');
  const [expandedId, setExpandedId] = useState<string | null>(null);
  const hubRef = useRef<signalR.HubConnection | null>(null);

  const showToast = (type: 'success' | 'error' | 'warning', msg: string) => {
    setToast({ type, msg });
    setTimeout(() => setToast(null), 5000);
  };

  const fetchReferrals = useCallback(async () => {
    if (!user) return;
    setLoading(true);
    try {
      const params = new URLSearchParams();
      if (filterStatus) params.set('status', filterStatus);
      if (filterUrgency) params.set('urgency', filterUrgency);
      const res = await fetch(
        `${process.env.NEXT_PUBLIC_API_URL}/api/referrals?${params}`,
        { headers: { Authorization: `Bearer ${user.token}` } }
      );
      if (res.ok) setReferrals(await res.json());
    } finally {
      setLoading(false);
    }
  }, [user, filterStatus, filterUrgency]);

  useEffect(() => {
    if (!user) { router.push('/login'); return; }
    fetchReferrals();
  }, [user, router, fetchReferrals]);

  // SignalR connection
  useEffect(() => {
    if (!user) return;
    const hub = new signalR.HubConnectionBuilder()
      .withUrl(`${process.env.NEXT_PUBLIC_API_URL}/hubs/queue?access_token=${user.token}`)
      .withAutomaticReconnect()
      .build();

    hub.on('ReferralCreated', () => { fetchReferrals(); showToast('success', 'New referral added to queue'); });
    hub.on('ReferralClaimed', () => { fetchReferrals(); });
    hub.on('ReferralReleased', () => { fetchReferrals(); });
    hub.on('ReferralUpdated', () => { fetchReferrals(); });
    hub.on('QueueResorted', () => { fetchReferrals(); showToast('success', 'Priority weights updated — queue resorted'); });

    hub.start()
      .then(() => hub.invoke('JoinQueue'))
      .catch(() => {});

    hubRef.current = hub;
    return () => { hub.stop(); };
  }, [user, fetchReferrals]);

  const claimReferral = async (r: Referral) => {
    if (!user) return;
    const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/referrals/${r.id}/claim`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${user.token}`, 'Content-Type': 'application/json' },
      body: JSON.stringify({ rowVersion: r.rowVersion }),
    });
    const data = await res.json();
    if (res.ok) { showToast('success', 'Referral claimed!'); fetchReferrals(); }
    else showToast('error', data.message || 'Could not claim referral');
  };

  const releaseReferral = async (r: Referral) => {
    if (!user) return;
    await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/referrals/${r.id}/release`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${user.token}` },
    });
    fetchReferrals();
  };

  const transitionReferral = async (r: Referral, newStatus: string) => {
    if (!user) return;
    const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/referrals/${r.id}/transition`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${user.token}`, 'Content-Type': 'application/json' },
      body: JSON.stringify({ newStatus, rowVersion: r.rowVersion }),
    });
    const data = await res.json();
    if (res.ok) { showToast('success', `Status updated to ${newStatus}`); fetchReferrals(); }
    else showToast('error', data.message || 'Transition failed');
  };

  const stats = {
    total: referrals.length,
    urgent: referrals.filter(r => r.urgency === 'Emergency' || r.urgency === 'Urgent').length,
    breached: referrals.filter(r => r.slaBreach).length,
    active: referrals.filter(r => !['Completed', 'Declined'].includes(r.status)).length,
  };

  if (!user) return null;

  return (
    <div className="min-h-screen bg-gray-950 p-6">
      {/* Toast */}
      {toast && (
        <div className={`fixed top-20 right-4 z-50 flex items-center gap-3 px-5 py-3 rounded-xl shadow-2xl border backdrop-blur-xl text-sm font-medium transition-all animate-in slide-in-from-right ${
          toast.type === 'success' ? 'bg-emerald-900/90 border-emerald-500/40 text-emerald-300' :
          toast.type === 'warning' ? 'bg-amber-900/90 border-amber-500/40 text-amber-300' :
          'bg-red-900/90 border-red-500/40 text-red-300'
        }`}>
          {toast.type === 'success' ? <CheckCircle2 className="w-4 h-4" /> : <AlertTriangle className="w-4 h-4" />}
          {toast.msg}
          <button onClick={() => setToast(null)}><X className="w-4 h-4 opacity-60 hover:opacity-100" /></button>
        </div>
      )}

      <div className="max-w-[1600px] mx-auto">
        {/* Header */}
        <div className="flex items-center justify-between mb-8">
          <div>
            <h1 className="text-2xl font-bold text-white">Referral Queue</h1>
            <p className="text-gray-400 text-sm mt-1">
              {user.role === 'GP' ? 'Your submitted referrals' : 'All active referrals — sorted by priority score'}
            </p>
          </div>
          <div className="flex items-center gap-3">
            <button onClick={fetchReferrals} className="flex items-center gap-2 px-4 py-2 bg-gray-800 hover:bg-gray-700 border border-white/10 rounded-xl text-sm text-gray-300 transition-all">
              <RefreshCw className="w-4 h-4" />
              Refresh
            </button>
            {(user.role === 'GP' || user.role === 'Admin') && (
              <button onClick={() => router.push('/referrals/new')} className="flex items-center gap-2 px-4 py-2 bg-blue-600 hover:bg-blue-500 rounded-xl text-sm text-white font-medium transition-all shadow-lg shadow-blue-500/25">
                <Plus className="w-4 h-4" />
                New Referral
              </button>
            )}
          </div>
        </div>

        {/* Stats */}
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 mb-6">
          {[
            { label: 'Total', value: stats.total, color: 'text-blue-400', bg: 'bg-blue-500/10 border-blue-500/20' },
            { label: 'Active', value: stats.active, color: 'text-emerald-400', bg: 'bg-emerald-500/10 border-emerald-500/20' },
            { label: 'High Priority', value: stats.urgent, color: 'text-orange-400', bg: 'bg-orange-500/10 border-orange-500/20' },
            { label: 'SLA Breached', value: stats.breached, color: 'text-red-400', bg: 'bg-red-500/10 border-red-500/20' },
          ].map(s => (
            <div key={s.label} className={`${s.bg} border rounded-xl p-4`}>
              <p className="text-xs text-gray-400 mb-1">{s.label}</p>
              <p className={`text-2xl font-bold ${s.color}`}>{s.value}</p>
            </div>
          ))}
        </div>

        {/* Filters */}
        <div className="flex flex-wrap gap-3 mb-6">
          <div className="flex items-center gap-2 text-gray-400 text-sm">
            <Filter className="w-4 h-4" />
            <span>Filter:</span>
          </div>
          <select value={filterStatus} onChange={e => setFilterStatus(e.target.value)}
            className="bg-gray-800 border border-white/10 rounded-lg px-3 py-1.5 text-sm text-gray-300 focus:outline-none focus:ring-1 focus:ring-blue-500">
            <option value="">All Statuses</option>
            {['Received','Triaged','Accepted','Declined','Booked','Completed'].map(s => <option key={s}>{s}</option>)}
          </select>
          <select value={filterUrgency} onChange={e => setFilterUrgency(e.target.value)}
            className="bg-gray-800 border border-white/10 rounded-lg px-3 py-1.5 text-sm text-gray-300 focus:outline-none focus:ring-1 focus:ring-blue-500">
            <option value="">All Urgencies</option>
            {['Emergency','Urgent','Soon','Routine'].map(u => <option key={u}>{u}</option>)}
          </select>
          {(filterStatus || filterUrgency) && (
            <button onClick={() => { setFilterStatus(''); setFilterUrgency(''); }}
              className="text-xs text-red-400 hover:text-red-300 flex items-center gap-1">
              <X className="w-3 h-3" /> Clear filters
            </button>
          )}
        </div>

        {/* Referral Table */}
        {loading ? (
          <div className="flex items-center justify-center py-24">
            <Loader2 className="w-8 h-8 text-blue-400 animate-spin" />
          </div>
        ) : referrals.length === 0 ? (
          <div className="text-center py-24 text-gray-500">
            <Activity className="w-12 h-12 mx-auto mb-4 opacity-30" />
            <p>No referrals found</p>
          </div>
        ) : (
          <div className="space-y-2">
            {referrals.map((r, idx) => (
              <div key={r.id}
                className={`bg-gray-900/60 border rounded-xl overflow-hidden transition-all duration-200 ${
                  r.slaBreach ? 'border-red-500/40 shadow-lg shadow-red-500/10' : 'border-white/10 hover:border-white/20'
                }`}>
                {/* Row */}
                <div
                  className="flex items-center gap-4 px-5 py-4 cursor-pointer"
                  onClick={() => setExpandedId(expandedId === r.id ? null : r.id)}
                >
                  {/* Priority rank */}
                  <div className="w-8 h-8 rounded-lg bg-gray-800 flex items-center justify-center text-xs font-bold text-gray-400 shrink-0">
                    {idx + 1}
                  </div>

                  {/* Patient info */}
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 flex-wrap">
                      <span className="font-semibold text-white text-sm">{r.patientName}</span>
                      <span className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs border font-medium ${urgencyColors[r.urgency]}`}>
                        {r.urgency}
                      </span>
                      <span className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs border ${statusColors[r.status]}`}>
                        {statusIcons[r.status]}
                        {r.status}
                      </span>
                      {r.slaBreach && (
                        <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs bg-red-500/20 text-red-300 border border-red-500/40 font-semibold animate-pulse">
                          <AlertTriangle className="w-3 h-3" /> SLA BREACH
                        </span>
                      )}
                    </div>
                    <p className="text-gray-400 text-xs mt-0.5 truncate">{r.specialistType} — {r.reason}</p>
                  </div>

                  {/* SLA Timer */}
                  <div className="hidden sm:block shrink-0">
                    <SlaTimer deadline={r.slaDeadline} status={r.status} />
                  </div>

                  {/* Priority Score */}
                  <div className="hidden md:flex items-center gap-1.5 shrink-0">
                    <TrendingUp className="w-3.5 h-3.5 text-blue-400" />
                    <span className="text-blue-400 text-xs font-semibold">{r.priorityScore.toFixed(1)}</span>
                  </div>

                  {/* GP / Creator */}
                  <div className="hidden lg:flex items-center gap-1.5 text-gray-500 text-xs shrink-0">
                    <User className="w-3.5 h-3.5" />
                    <span>{r.createdByUser?.fullName ?? '—'}</span>
                  </div>

                  {/* Claimed badge */}
                  {r.claimedByUser && (
                    <div className="hidden md:flex items-center gap-1 text-xs text-amber-400 border border-amber-500/30 bg-amber-500/10 rounded-full px-2 py-0.5 shrink-0">
                      <Lock className="w-3 h-3" />
                      <span className="truncate max-w-[80px]">{r.claimedByUser.fullName}</span>
                    </div>
                  )}

                  {/* Expand */}
                  <div className="text-gray-600 shrink-0">
                    {expandedId === r.id ? <ChevronUp className="w-4 h-4" /> : <ChevronDown className="w-4 h-4" />}
                  </div>
                </div>

                {/* Expanded Detail Panel */}
                {expandedId === r.id && (
                  <div className="border-t border-white/5 bg-gray-900/40 px-5 py-4">
                    <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-4 text-sm">
                      <div>
                        <p className="text-gray-500 text-xs uppercase tracking-wider mb-1">Patient DOB</p>
                        <p className="text-gray-200">{new Date(r.patientDateOfBirth).toLocaleDateString()}</p>
                      </div>
                      <div>
                        <p className="text-gray-500 text-xs uppercase tracking-wider mb-1">Received</p>
                        <p className="text-gray-200">{new Date(r.receivedAt).toLocaleString()}</p>
                      </div>
                      <div>
                        <p className="text-gray-500 text-xs uppercase tracking-wider mb-1">SLA Deadline</p>
                        <p className={r.slaBreach ? 'text-red-400 font-semibold' : 'text-gray-200'}>
                          {new Date(r.slaDeadline).toLocaleString()}
                        </p>
                      </div>
                    </div>

                    {/* Action Buttons */}
                    {(user.role === 'TriageNurse' || user.role === 'Admin') && (
                      <div className="flex flex-wrap gap-2">
                        {/* Claim / Release */}
                        {!r.claimedByUser ? (
                          <button onClick={() => claimReferral(r)}
                            className="flex items-center gap-1.5 px-4 py-2 bg-blue-600/20 hover:bg-blue-600/30 text-blue-400 border border-blue-500/30 rounded-lg text-xs font-medium transition-all">
                            <Lock className="w-3.5 h-3.5" /> Claim
                          </button>
                        ) : (
                          <button onClick={() => releaseReferral(r)}
                            className="flex items-center gap-1.5 px-4 py-2 bg-gray-700/40 hover:bg-gray-700/60 text-gray-400 border border-white/10 rounded-lg text-xs font-medium transition-all">
                            <Unlock className="w-3.5 h-3.5" /> Release
                          </button>
                        )}

                        {/* State Machine Transitions */}
                        {nextStatuses[r.status]?.map(ns => (
                          <button key={ns} onClick={() => transitionReferral(r, ns)}
                            className={`flex items-center gap-1.5 px-4 py-2 border rounded-lg text-xs font-medium transition-all ${
                              ns === 'Declined'
                                ? 'bg-red-500/10 hover:bg-red-500/20 text-red-400 border-red-500/30'
                                : 'bg-emerald-500/10 hover:bg-emerald-500/20 text-emerald-400 border-emerald-500/30'
                            }`}>
                            → {ns}
                          </button>
                        ))}
                      </div>
                    )}
                  </div>
                )}
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
