'use client';

import { useEffect, useState, useRef, useCallback } from 'react';
import { useAuth } from '@/context/AuthContext';
import { useRouter } from 'next/navigation';
import * as signalR from '@microsoft/signalr';
import {
  Search, Plus, RefreshCw, Filter, User, Clock, AlertTriangle, CheckCircle2, XCircle,
  Loader2, Lock, Unlock, TrendingUp, X, Paperclip, Download, Pencil, Save, ChevronDown, ChevronUp, Activity, Eye
} from 'lucide-react';

interface MultiSelectProps {
  label: string;
  options: { value: string; label: string }[];
  selectedValues: string[];
  onChange: (values: string[]) => void;
  icon?: React.ReactNode;
}

function MultiSelect({ label, options, selectedValues, onChange, icon }: MultiSelectProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    }
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  useEffect(() => {
    if (!isOpen) {
      setSearchTerm('');
    }
  }, [isOpen]);

  const toggleOption = (val: string) => {
    if (selectedValues.includes(val)) {
      onChange(selectedValues.filter(v => v !== val));
    } else {
      onChange([...selectedValues, val]);
    }
  };

  const filteredOptions = options.filter(opt =>
    opt.label.toLowerCase().includes(searchTerm.toLowerCase())
  );

  const displayText = selectedValues.length === 0
    ? `All ${label}s`
    : selectedValues.length === 1
      ? options.find(o => o.value === selectedValues[0])?.label || selectedValues[0]
      : `${selectedValues.length} Selected`;

  return (
    <div className="relative" ref={containerRef}>
      <button
        type="button"
        onClick={() => setIsOpen(!isOpen)}
        className="flex items-center justify-between gap-2 bg-white border border-slate-200 rounded-xl px-4 py-2 text-sm text-slate-600 focus:outline-none focus:ring-2 focus:ring-blue-500/20 font-bold shadow-sm select-none min-w-[160px]"
      >
        <div className="flex items-center gap-1.5 truncate">
          {icon}
          <span className="truncate">{displayText}</span>
        </div>
        <ChevronDown className={`w-4 h-4 transition-transform duration-200 ${isOpen ? 'rotate-180' : ''}`} />
      </button>

      {isOpen && (
        <div className="absolute left-0 mt-1.5 w-60 bg-white border border-slate-200 rounded-xl shadow-xl z-50 p-2 space-y-1.5 animate-in fade-in slide-in-from-top-1">
          {/* Dropdown Search Control */}
          <div className="relative px-1 py-0.5">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-3.5 h-3.5 text-slate-400" />
            <input
              type="text"
              value={searchTerm}
              onChange={e => setSearchTerm(e.target.value)}
              placeholder={`Search ${label.toLowerCase()}...`}
              className="w-full pl-8 pr-3 py-1.5 bg-slate-50 border border-slate-200 rounded-lg text-slate-900 text-xs focus:outline-none focus:ring-1 focus:ring-blue-500/20 focus:border-blue-500 font-semibold"
            />
          </div>
          
          <div className="max-h-60 overflow-y-auto space-y-1 pr-1">
            {filteredOptions.length === 0 ? (
              <div className="text-center py-3 text-slate-400 text-xs font-semibold">
                No matching options
              </div>
            ) : (
              filteredOptions.map(opt => {
                const isChecked = selectedValues.includes(opt.value);
                return (
                  <label
                    key={opt.value}
                    className="flex items-center gap-2 px-3 py-2 hover:bg-slate-50 rounded-lg cursor-pointer text-sm font-semibold text-slate-700 select-none"
                  >
                    <input
                      type="checkbox"
                      checked={isChecked}
                      onChange={() => toggleOption(opt.value)}
                      className="w-4 h-4 text-blue-600 border-slate-300 rounded focus:ring-blue-500 accent-blue-600"
                    />
                    <span>{opt.label}</span>
                  </label>
                );
              })
            )}
          </div>
        </div>
      )}
    </div>
  );
}

interface Attachment {
  id: string;
  fileName: string;
  filePath: string;
  contentType: string;
}

interface AuditLog {
  id: string;
  referralId: string;
  performedByUserId: string;
  performedByUser?: { fullName: string; email: string; title?: string };
  fromStatus?: string | null;
  toStatus?: string | null;
  action: string;
  notes?: string | null;
  timestamp: string;
}

interface Referral {
  id: string;
  patientId: string;
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
  createdByUser?: { fullName: string; email: string; title?: string };
  claimedByUser?: { fullName: string; email: string; title?: string } | null;
  assignedToUser?: { fullName: string; email: string; title?: string } | null;
  attachments?: Attachment[];
  claimedAt?: string | null;
  caseNo: string;
  auditLogs?: AuditLog[];
}

interface User {
  id: string;
  fullName: string;
  email: string;
  roles: string[];
  isActive: boolean;
  title?: string;
}

const urgencyColors: Record<string, string> = {
  Emergency: 'bg-red-50 text-red-600 border-red-200',
  Urgent:    'bg-orange-50 text-orange-600 border-orange-200',
  Soon:      'bg-yellow-50 text-yellow-600 border-yellow-200',
  Routine:   'bg-slate-50 text-slate-600 border-slate-200',
};

const statusColors: Record<string, string> = {
  Received:  'bg-blue-50 text-blue-600 border-blue-200',
  Triaged:   'bg-amber-50 text-amber-600 border-amber-200',
  Accepted:  'bg-emerald-50 text-emerald-600 border-emerald-200',
  Declined:  'bg-rose-50 text-rose-600 border-rose-200',
  Booked:    'bg-purple-50 text-purple-600 border-purple-200',
  Completed: 'bg-slate-50 text-slate-600 border-slate-200',
};

const statusIcons: Record<string, React.ReactNode> = {
  Received:  <Clock className="w-3 h-3" />,
  Triaged:   <Filter className="w-3 h-3" />,
  Accepted:  <CheckCircle2 className="w-3 h-3" />,
  Declined:  <XCircle className="w-3 h-3" />,
  Booked:    <Lock className="w-3 h-3" />,
  Completed: <CheckCircle2 className="w-3 h-3" />,
};

// Date Formatter: dd/MM/yyyy hh:mm t
const formatDate = (dateStr: string | null | undefined) => {
  if (!dateStr) return '—';
  const date = new Date(dateStr);
  if (isNaN(date.getTime())) return '—';
  
  const dd = String(date.getDate()).padStart(2, '0');
  const MM = String(date.getMonth() + 1).padStart(2, '0');
  const yyyy = date.getFullYear();
  
  let hours = date.getHours();
  const minutes = String(date.getMinutes()).padStart(2, '0');
  const ampm = hours >= 12 ? 'pm' : 'am';
  
  hours = hours % 12;
  hours = hours ? hours : 12;
  const hh = String(hours).padStart(2, '0');
  
  return `${dd}/${MM}/${yyyy} ${hh}:${minutes} ${ampm}`;
};

// DOB Formatter: dd/MM/yyyy
const formatDateOnly = (dateStr: string | null | undefined) => {
  if (!dateStr) return '—';
  const date = new Date(dateStr);
  if (isNaN(date.getTime())) return '—';
  const dd = String(date.getDate()).padStart(2, '0');
  const MM = String(date.getMonth() + 1).padStart(2, '0');
  const yyyy = date.getFullYear();
  return `${dd}/${MM}/${yyyy}`;
};

// Transition options based on status
const statusTransitions: Record<string, string[]> = {
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
  const [users, setUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(true);
  const [toast, setToast] = useState<{ type: 'success' | 'error' | 'warning'; msg: string } | null>(null);
  const [filterStatus, setFilterStatus] = useState<string[]>([]);
  const [filterUrgency, setFilterUrgency] = useState<string[]>([]);
  const [filterAssignee, setFilterAssignee] = useState<string[]>([]);
  const [patientSearch, setPatientSearch] = useState('');
  const [caseNoSearch, setCaseNoSearch] = useState('');
  const [sortBy, setSortBy] = useState('priority');
  const [filterFromDate, setFilterFromDate] = useState('');
  const [filterToDate, setFilterToDate] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [stats, setStats] = useState({ total: 0, active: 0, urgent: 0, breached: 0 });
  const [expandedId, setExpandedId] = useState<string | null>(null);
  const hubRef = useRef<signalR.HubConnection | null>(null);

  // Edit Modal States
  const [editModalOpen, setEditModalOpen] = useState(false);
  const [editingReferral, setEditingReferral] = useState<Referral | null>(null);
  const [editForm, setEditForm] = useState({
    specialistType: '', reason: '', urgency: 'Routine', assignedToUserId: ''
  });
  const [newFiles, setNewFiles] = useState<File[]>([]);
  const [editSaving, setEditSaving] = useState(false);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [previewTitle, setPreviewTitle] = useState<string | null>(null);

  // Transition Notes Modal States
  const [transitionModal, setTransitionModal] = useState<{
    referral: Referral;
    nextStatus: string;
  } | null>(null);
  const [transitionNotes, setTransitionNotes] = useState('');
  const [transitionSaving, setTransitionSaving] = useState(false);

  const showToast = (type: 'success' | 'error' | 'warning', msg: string) => {
    setToast({ type, msg });
    setTimeout(() => setToast(null), 5000);
  };

  const previewPdf = async (url: string, fileName: string) => {
    if (!user) return;
    try {
      const res = await fetch(url, {
        headers: { Authorization: `Bearer ${user.token}` }
      });
      if (res.ok) {
        const blob = await res.blob();
        const blobUrl = URL.createObjectURL(blob);
        setPreviewUrl(blobUrl);
        setPreviewTitle(fileName);
      } else {
        showToast('error', 'Failed to load document preview');
      }
    } catch {
      showToast('error', 'Network error while loading preview');
    }
  };

  const fetchReferrals = useCallback(async (pageOverride?: number) => {
    if (!user) return;
    setLoading(true);
    try {
      const pageToFetch = pageOverride ?? currentPage;
      const params = new URLSearchParams();
      if (filterStatus.length > 0) params.set('status', filterStatus.join(','));
      if (filterUrgency.length > 0) params.set('urgency', filterUrgency.join(','));
      if (filterAssignee.length > 0) params.set('assignedTo', filterAssignee.join(','));
      if (patientSearch) params.set('patientSearch', patientSearch);
      if (caseNoSearch) params.set('caseNo', caseNoSearch);
      if (filterFromDate) params.set('fromDate', filterFromDate);
      if (filterToDate) params.set('toDate', filterToDate);
      if (sortBy) params.set('sortBy', sortBy);
      params.set('page', pageToFetch.toString());
      params.set('pageSize', '15');

      const res = await fetch(
        `${process.env.NEXT_PUBLIC_API_URL}/api/referrals?${params}`,
        { headers: { Authorization: `Bearer ${user.token}` } }
      );
      if (res.ok) {
        const data = await res.json();
        setReferrals(data.items);
        setTotalPages(data.totalPages);
        setTotalCount(data.totalCount);
        setStats({
          total: data.totalCount,
          active: data.activeCount,
          urgent: data.urgentCount,
          breached: data.breachedCount
        });
        setCurrentPage(pageToFetch);
      }
    } finally {
      setLoading(false);
    }
  }, [user, filterStatus, filterUrgency, filterAssignee, patientSearch, caseNoSearch, currentPage, sortBy, filterFromDate, filterToDate]);

  const fetchUsers = useCallback(async () => {
    if (!user) return;
    try {
      const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/users`, {
        headers: { Authorization: `Bearer ${user.token}` }
      });
      if (res.ok) {
        const data = await res.json();
        setUsers(data.filter((u: any) => u.isActive));
      }
    } catch {
      // ignore
    }
  }, [user]);

  // Debounced search/filter fetcher
  useEffect(() => {
    if (!user) return;
    const delayDebounceFn = setTimeout(() => {
      fetchReferrals(1);
    }, 300);

    return () => clearTimeout(delayDebounceFn);
  }, [filterStatus, filterUrgency, filterAssignee, patientSearch, caseNoSearch, sortBy, filterFromDate, filterToDate]);

  // Reset to first page when filters change
  useEffect(() => {
    setCurrentPage(1);
  }, [filterStatus, filterUrgency, filterAssignee, patientSearch, caseNoSearch, sortBy, filterFromDate, filterToDate]);

  useEffect(() => {
    if (!user) { router.push('/login'); return; }
    fetchUsers();
  }, [user, router, fetchUsers]);

  // Create a ref for fetchReferrals to avoid stale bindings in SignalR callbacks
  const fetchReferralsRef = useRef(fetchReferrals);
  useEffect(() => {
    fetchReferralsRef.current = fetchReferrals;
  }, [fetchReferrals]);

  // SignalR connection
  useEffect(() => {
    if (!user) return;
    const hub = new signalR.HubConnectionBuilder()
      .withUrl(`${process.env.NEXT_PUBLIC_API_URL}/hubs/queue?access_token=${user.token}`)
      .withAutomaticReconnect()
      .build();

    hub.on('ReferralCreated', () => { fetchReferralsRef.current(); showToast('success', 'New referral added to queue'); });
    hub.on('ReferralClaimed', () => { fetchReferralsRef.current(); });
    hub.on('ReferralReleased', () => { fetchReferralsRef.current(); });
    hub.on('ReferralUpdated', () => { fetchReferralsRef.current(); });
    hub.on('QueueResorted', () => { fetchReferralsRef.current(); showToast('success', 'Priority weights updated — queue resorted'); });

    hub.start()
      .then(() => hub.invoke('JoinQueue'))
      .catch(() => {});

    hubRef.current = hub;
    return () => { hub.stop(); };
  }, [user]);

  const claimReferral = async (r: Referral) => {
    if (!user) return;
    try {
      const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/referrals/${r.id}/claim`, {
        method: 'POST',
        headers: { Authorization: `Bearer ${user.token}`, 'Content-Type': 'application/json' },
        body: JSON.stringify({ rowVersion: r.rowVersion }),
      });
      const data = await res.json();
      if (res.ok) { 
        showToast('success', 'Referral claimed!'); 
        setReferrals(prev => prev.map(item => item.id === r.id ? { 
          ...item, 
          rowVersion: data.rowVersion, 
          claimedByUser: { fullName: user.fullName, email: user.email, title: user.title || '' } 
        } : item));
        fetchReferrals(); 
      }
      else {
        showToast('error', data.message || 'Could not claim referral. The queue has been updated.');
        fetchReferrals();
      }
    } catch (err) {
      showToast('error', 'Unable to claim referral: ' + (err instanceof Error ? err.message : 'Network error'));
    }
  };

  const releaseReferral = async (r: Referral) => {
    if (!user) return;
    try {
      const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/referrals/${r.id}/release`, {
        method: 'POST',
        headers: { Authorization: `Bearer ${user.token}`, 'Content-Type': 'application/json' },
        body: JSON.stringify({ rowVersion: r.rowVersion }),
      });
      const data = await res.json();
      if (res.ok) { 
        showToast('success', 'Referral released!'); 
        setReferrals(prev => prev.map(item => item.id === r.id ? { 
          ...item, 
          rowVersion: data.rowVersion, 
          claimedByUser: null 
        } : item));
        fetchReferrals(); 
      } else {
        showToast('error', data.message || 'Could not release referral.');
        fetchReferrals();
      }
    } catch (err) {
      showToast('error', 'Unable to release referral: ' + (err instanceof Error ? err.message : 'Network error'));
    }
  };

  const transitionStatus = async (r: Referral, nextStatus: string) => {
    // Open the custom notes modal instead of using prompt()
    setTransitionNotes('');
    setTransitionModal({ referral: r, nextStatus });
  };

  const confirmTransition = async () => {
    if (!user || !transitionModal) return;
    const { referral: r, nextStatus } = transitionModal;
    const statusMap: Record<string, number> = {
      Received: 1, Triaged: 2, Accepted: 3, Declined: 4, Booked: 5, Completed: 6
    };

    setTransitionSaving(true);
    try {
      const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/referrals/${r.id}/transition`, {
        method: 'POST',
        headers: { Authorization: `Bearer ${user.token}`, 'Content-Type': 'application/json' },
        body: JSON.stringify({
          newStatus: statusMap[nextStatus],
          rowVersion: r.rowVersion,
          notes: transitionNotes || null
        }),
      });
      let data: { message?: string; rowVersion?: string } = {};
      try { data = await res.json(); } catch { /* ignore */ }

      if (res.ok) {
        showToast('success', `Status transitioned to ${nextStatus}!`);
        setTransitionModal(null);
        fetchReferrals();
      } else {
        showToast('error', data.message || 'Unable to update status. Please try again.');
        fetchReferrals();
      }
    } catch {
      showToast('error', 'Unable to update status. Please check your connection and try again.');
    } finally {
      setTransitionSaving(false);
    }
  };

  const handleEditClick = async (r: Referral) => {
    setEditingReferral(r);
    setEditForm({
      specialistType: r.specialistType,
      reason: r.reason,
      urgency: r.urgency,
      assignedToUserId: r.assignedToUser ? users.find(u => u.email === r.assignedToUser?.email)?.id || '' : ''
    });
    setNewFiles([]);
    setEditModalOpen(true);

    try {
      const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/referrals/${r.id}`, {
        headers: { Authorization: `Bearer ${user?.token}` }
      });
      if (res.ok) {
        const detail = await res.json();
        setEditingReferral(prev => prev && prev.id === r.id ? {
          ...prev,
          auditLogs: detail.auditLogs,
          rowVersion: detail.rowVersion
        } : prev);
      }
    } catch (err) {
      console.error("Failed to fetch referral details", err);
    }
  };

  const handleEditSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!user || !editingReferral) return;
    setEditSaving(true);

    const urgencyMap: Record<string, number> = {
      'Routine': 1, 'Soon': 2, 'Urgent': 3, 'Emergency': 4
    };

    try {
      const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/referrals/${editingReferral.id}`, {
        method: 'PUT',
        headers: { Authorization: `Bearer ${user.token}`, 'Content-Type': 'application/json' },
        body: JSON.stringify({
          specialistType: editForm.specialistType,
          reason: editForm.reason,
          urgency: urgencyMap[editForm.urgency],
          assignedToUserId: editForm.assignedToUserId || null,
          rowVersion: editingReferral.rowVersion
        })
      });

      const data = await res.json().catch(() => ({}));

      if (res.status === 409) {
        // Concurrency conflict — another user saved first
        showToast('error', data.message || 'This referral was modified by another user. Please close and reopen to get the latest version.');
        setEditSaving(false);
        return;
      }

      if (!res.ok) {
        showToast('error', data.message || 'Failed to update referral details');
        setEditSaving(false);
        return;
      }

      // Update local rowVersion so re-saves in the same session use the latest token
      if (data.rowVersion) {
        setEditingReferral(prev => prev ? { ...prev, rowVersion: data.rowVersion } : prev);
        setReferrals(prev => prev.map(item =>
          item.id === editingReferral.id ? { ...item, rowVersion: data.rowVersion } : item
        ));
      }

      if (newFiles.length > 0) {
        for (const file of newFiles) {
          const fileData = new FormData();
          fileData.append('file', file);
          await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/referrals/${editingReferral.id}/attachments`, {
            method: 'POST',
            headers: { Authorization: `Bearer ${user.token}` },
            body: fileData
          });
        }
      }

      showToast('success', 'Referral details updated successfully');
      setEditModalOpen(false);
      fetchReferrals();
    } catch {
      showToast('error', 'Network error');
    } finally {
      setEditSaving(false);
    }
  };

  // Stats are bound from state variable updated by the backend

  const isGP = user?.roles.includes('GP') && !user?.roles.includes('Admin') && !user?.roles.includes('TriageNurse');

  if (!user) return null;

  return (
    <div className="min-h-screen bg-slate-50 p-6">
      {/* Toast */}
      {toast && (
        <div className={`fixed top-20 right-4 z-[100] flex items-center gap-3 px-5 py-3 rounded-xl shadow-lg border text-sm font-semibold transition-all animate-in slide-in-from-right ${
          toast.type === 'success' ? 'bg-emerald-50 border-emerald-200 text-emerald-700' :
          toast.type === 'warning' ? 'bg-amber-50 border-amber-200 text-amber-700' :
          'bg-red-50 border-red-200 text-red-700'
        }`}>
          {toast.type === 'success' ? <CheckCircle2 className="w-4 h-4 text-emerald-600" /> : <AlertTriangle className="w-4 h-4 text-red-600" />}
          {toast.msg}
          <button onClick={() => setToast(null)}><X className="w-4 h-4 opacity-60 hover:opacity-100" /></button>
        </div>
      )}

      <div className="max-w-[1600px] mx-auto">
        {/* Header */}
        <div className="flex items-center justify-between mb-8">
          <div>
            <h1 className="text-2xl font-bold text-slate-900">Referral Queue</h1>
            <p className="text-slate-500 text-sm mt-1 font-medium">
              {isGP ? 'Your submitted and assigned referrals' : 'All active referrals — sorted by priority score'}
            </p>
          </div>
          <div className="flex items-center gap-3">
            <button onClick={() => fetchReferrals()} className="flex items-center gap-2 px-4 py-2 bg-white hover:bg-slate-50 border border-slate-200 rounded-xl text-sm text-slate-600 font-bold transition-all shadow-sm">
              <RefreshCw className="w-4 h-4" />
              Refresh
            </button>
            {(user.roles.includes('GP') || user.roles.includes('Admin')) && (
              <button onClick={() => router.push('/referrals/new')} className="flex items-center gap-2 px-4 py-2 bg-blue-600 hover:bg-blue-500 rounded-xl text-sm text-white font-bold transition-all shadow-md shadow-blue-500/10">
                <Plus className="w-4 h-4" />
                New Referral
              </button>
            )}
          </div>
        </div>

        {/* Stats */}
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 mb-6">
          {[
            { label: 'Total Referrals', value: stats.total, color: 'text-blue-600', bg: 'bg-white border-blue-100' },
            { label: 'Active Triages', value: stats.active, color: 'text-emerald-600', bg: 'bg-white border-emerald-100' },
            { label: 'High Priority', value: stats.urgent, color: 'text-orange-600', bg: 'bg-white border-orange-100' },
            { label: 'SLA Breached', value: stats.breached, color: 'text-red-600', bg: 'bg-white border-red-100' },
          ].map(s => (
            <div key={s.label} className={`${s.bg} border rounded-xl p-4 shadow-sm`}>
              <p className="text-xs text-slate-400 font-bold uppercase tracking-wider mb-1">{s.label}</p>
              <p className={`text-2xl font-extrabold ${s.color}`}>{s.value}</p>
            </div>
          ))}
        </div>

        {/* Search and Filters Bar */}
        <div className="bg-white border border-slate-200 rounded-2xl p-5 shadow-sm space-y-4 mb-6">
          <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-4">
            {/* Patient Search */}
            <div className="relative">
              <Search className="absolute left-3.5 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400" />
              <input
                type="text"
                value={patientSearch}
                onChange={e => setPatientSearch(e.target.value)}
                placeholder="Search patient by Name or NHI..."
                className="w-full pl-10 pr-4 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-slate-900 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 font-semibold transition-all"
              />
            </div>
            {/* Case No Search */}
            <div className="relative">
              <Search className="absolute left-3.5 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400" />
              <input
                type="text"
                value={caseNoSearch}
                onChange={e => setCaseNoSearch(e.target.value)}
                placeholder="Search by Case Number..."
                className="w-full pl-10 pr-4 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-slate-900 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 font-semibold transition-all"
              />
            </div>
            {/* From Date */}
            <div className="relative flex items-center bg-slate-50 border border-slate-200 rounded-xl px-3 py-1 text-xs text-slate-600 focus-within:ring-2 focus-within:ring-blue-500/20 font-semibold shadow-sm select-none">
              <span className="text-slate-400 text-[10px] font-bold uppercase tracking-wider mr-2">From:</span>
              <input
                type="date"
                value={filterFromDate}
                onChange={e => setFilterFromDate(e.target.value)}
                className="bg-transparent border-none text-slate-700 text-xs focus:outline-none cursor-pointer font-bold w-full"
              />
            </div>
            {/* To Date */}
            <div className="relative flex items-center bg-slate-50 border border-slate-200 rounded-xl px-3 py-1 text-xs text-slate-600 focus-within:ring-2 focus-within:ring-blue-500/20 font-semibold shadow-sm select-none">
              <span className="text-slate-400 text-[10px] font-bold uppercase tracking-wider mr-2">To:</span>
              <input
                type="date"
                value={filterToDate}
                onChange={e => setFilterToDate(e.target.value)}
                className="bg-transparent border-none text-slate-700 text-xs focus:outline-none cursor-pointer font-bold w-full"
              />
            </div>
          </div>

          <div className="flex flex-wrap gap-3 items-center pt-2 border-t border-slate-100">
            <div className="flex items-center gap-1.5 text-slate-500 text-xs font-bold uppercase tracking-wider">
              <Filter className="w-3.5 h-3.5" />
              <span>Filters:</span>
            </div>

            <MultiSelect
              label="Status"
              options={[
                { value: 'Received', label: 'Received' },
                { value: 'Triaged', label: 'Triaged' },
                { value: 'Accepted', label: 'Accepted' },
                { value: 'Declined', label: 'Declined' },
                { value: 'Booked', label: 'Booked' },
                { value: 'Completed', label: 'Completed' },
              ]}
              selectedValues={filterStatus}
              onChange={setFilterStatus}
            />

            <MultiSelect
              label="Urgency"
              options={[
                { value: 'Emergency', label: 'Emergency' },
                { value: 'Urgent', label: 'Urgent' },
                { value: 'Soon', label: 'Soon' },
                { value: 'Routine', label: 'Routine' },
              ]}
              selectedValues={filterUrgency}
              onChange={setFilterUrgency}
            />

            {user.roles.includes('Admin') && (
              <MultiSelect
                label="Assignee"
                options={users.map(u => ({
                  value: u.id,
                  label: `${u.title ? u.title + ' ' : ''}${u.fullName}`
                }))}
                selectedValues={filterAssignee}
                onChange={setFilterAssignee}
                icon={<User className="w-3.5 h-3.5 text-slate-400" />}
              />
            )}

            {/* Sort Dropdown */}
            <div className="flex items-center gap-2 bg-white border border-slate-200 rounded-xl px-3 py-2 text-sm text-slate-600 focus-within:ring-2 focus-within:ring-blue-500/20 font-bold shadow-sm select-none">
              <span className="text-slate-400 text-xs font-bold uppercase tracking-wider">Sort:</span>
              <select
                value={sortBy}
                onChange={e => setSortBy(e.target.value)}
                className="bg-transparent border-none text-slate-600 text-xs focus:outline-none cursor-pointer font-bold"
              >
                <option value="priority">Priority Score</option>
                <option value="receivedDate">Received Date</option>
              </select>
            </div>

            {(filterStatus.length > 0 || filterUrgency.length > 0 || filterAssignee.length > 0 || patientSearch || caseNoSearch || sortBy !== 'priority' || filterFromDate || filterToDate) && (
              <button
                onClick={() => {
                  setFilterStatus([]);
                  setFilterUrgency([]);
                  setFilterAssignee([]);
                  setPatientSearch('');
                  setCaseNoSearch('');
                  setSortBy('priority');
                  setFilterFromDate('');
                  setFilterToDate('');
                }}
                className="text-xs text-red-600 hover:text-red-800 font-bold flex items-center gap-1 ml-auto bg-red-50 hover:bg-red-100/60 px-3 py-1.5 rounded-lg border border-red-100 transition-all"
              >
                <X className="w-3.5 h-3.5" /> Clear all filters
              </button>
            )}
          </div>
        </div>

        {/* Referral Table */}
        {loading ? (
          <div className="flex items-center justify-center py-24">
            <Loader2 className="w-8 h-8 text-blue-600 animate-spin" />
          </div>
        ) : referrals.length === 0 ? (
          <div className="text-center py-24 bg-white border border-slate-200 rounded-2xl shadow-sm text-slate-400 font-semibold">
            <Activity className="w-12 h-12 mx-auto mb-4 opacity-30 text-slate-400" />
            <p>No referrals found in the queue</p>
          </div>
        ) : (
          <>
            <div className="space-y-2.5">
              {referrals.map((r, idx) => (
                <div key={r.id}
                  className={`bg-white border rounded-xl overflow-hidden transition-all duration-200 shadow-sm ${
                    r.slaBreach ? 'border-red-300 ring-2 ring-red-100 shadow-md' : 'border-slate-200 hover:border-slate-300'
                  }`}>
                  {/* Row Header */}
                  <div
                    className="flex items-center gap-4 px-5 py-4 cursor-pointer"
                    onClick={() => setExpandedId(expandedId === r.id ? null : r.id)}
                  >
                    {/* Priority rank */}
                    <div className="w-8 h-8 rounded-lg bg-slate-100 flex items-center justify-center text-xs font-bold text-slate-600 shrink-0">
                      #{(currentPage - 1) * 15 + idx + 1}
                    </div>

                    {/* Patient info & CaseNo */}
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 flex-wrap">
                        <span className="font-bold text-slate-900 text-sm">{r.patientName}</span>
                        <span className="font-extrabold text-blue-600 text-[10px] font-mono border border-blue-100 bg-blue-50/50 px-2 py-0.5 rounded-lg shrink-0">
                          {r.caseNo}
                        </span>
                        <span className={`inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[10px] border font-bold ${urgencyColors[r.urgency]}`}>
                          {r.urgency}
                        </span>
                        <span className={`inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[10px] border font-bold ${statusColors[r.status]}`}>
                          {statusIcons[r.status]}
                          {r.status}
                        </span>
                        {r.slaBreach && (
                          <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] bg-red-100 text-red-700 border border-red-200 font-bold animate-pulse">
                            <AlertTriangle className="w-3.5 h-3.5" /> SLA BREACH
                          </span>
                        )}
                      </div>
                      <p className="text-slate-500 text-xs mt-1 font-semibold truncate">{r.specialistType} — {r.reason}</p>
                    </div>

                    {/* Priority Score */}
                    <div className="hidden md:flex items-center gap-1.5 shrink-0">
                      <TrendingUp className="w-3.5 h-3.5 text-blue-600" />
                      <span className="text-blue-600 text-xs font-bold">{r.priorityScore.toFixed(1)}</span>
                    </div>

                    {/* Assigned User display */}
                    <div className="hidden lg:flex items-center gap-1.5 text-slate-500 text-xs font-semibold shrink-0">
                      <User className="w-3.5 h-3.5 text-slate-400" />
                      <span className="truncate max-w-[120px]">
                        {r.assignedToUser ? (r.assignedToUser.title ? r.assignedToUser.title + ' ' : '') + r.assignedToUser.fullName : 'Unassigned'}
                      </span>
                    </div>

                    {/* Claimed badge */}
                    {r.claimedByUser && (
                      <div className="hidden md:flex items-center gap-1 text-[10px] font-bold text-amber-700 border border-amber-200 bg-amber-50 rounded-full px-2 py-0.5 shrink-0">
                        <Lock className="w-3.5 h-3.5 text-amber-600" />
                        <span className="truncate max-w-[80px]">Claimed: {r.claimedByUser.title ? r.claimedByUser.title + ' ' : ''}{r.claimedByUser.fullName}</span>
                      </div>
                    )}

                    {/* Expand icon */}
                    <div className="text-slate-400 shrink-0">
                      {expandedId === r.id ? <ChevronUp className="w-4 h-4" /> : <ChevronDown className="w-4 h-4" />}
                    </div>
                  </div>

                  {/* Expanded Detail Panel */}
                  {expandedId === r.id && (
                    <div className="border-t border-slate-100 bg-slate-50/50 px-5 py-4 space-y-4">
                      <div className="grid grid-cols-2 sm:grid-cols-5 gap-4 text-xs font-semibold text-slate-600">
                        <div>
                          <p className="text-slate-400 text-[10px] font-bold uppercase tracking-wider mb-1">Case Number</p>
                          <p className="text-slate-900 text-sm font-extrabold font-mono">{r.caseNo}</p>
                        </div>
                        <div>
                          <p className="text-slate-400 text-[10px] font-bold uppercase tracking-wider mb-1">Patient DOB</p>
                          <p className="text-slate-800 text-sm font-semibold">{formatDateOnly(r.patientDateOfBirth)}</p>
                        </div>
                        <div>
                          <p className="text-slate-400 text-[10px] font-bold uppercase tracking-wider mb-1">Received At</p>
                          <p className="text-slate-800 text-sm font-semibold">{formatDate(r.receivedAt)}</p>
                        </div>
                        <div>
                          <p className="text-slate-400 text-[10px] font-bold uppercase tracking-wider mb-1">SLA Deadline</p>
                          <p className={`text-sm font-bold ${r.slaBreach ? 'text-red-600' : 'text-slate-800'}`}>
                            {formatDate(r.slaDeadline)}
                          </p>
                        </div>
                        <div>
                          <p className="text-slate-400 text-[10px] font-bold uppercase tracking-wider mb-1">Submitted By</p>
                          <p className="text-slate-800 text-sm font-semibold">
                            {r.createdByUser ? (r.createdByUser.title ? r.createdByUser.title + ' ' : '') + r.createdByUser.fullName : '—'}
                          </p>
                        </div>
                      </div>

                      {/* Attachments list */}
                      {r.attachments && r.attachments.length > 0 && (
                        <div className="border-t border-slate-100 pt-3">
                          <p className="text-[10px] text-slate-400 font-bold uppercase tracking-wider mb-2 flex items-center gap-1.5"><Paperclip className="w-3.5 h-3.5 text-slate-400" /> Attachments</p>
                          <div className="flex flex-wrap gap-2">
                            {r.attachments.map(att => {
                              const url = `${process.env.NEXT_PUBLIC_API_URL}/api/referrals/attachments/${att.id}`;
                              return (
                                <div key={att.id} className="inline-flex items-center gap-1 bg-white border border-slate-200 rounded-lg shadow-sm overflow-hidden pl-3 pr-1 py-1">
                                  <span className="text-slate-700 text-xs font-semibold truncate max-w-[150px]">{att.fileName}</span>
                                  <button
                                    type="button"
                                    onClick={() => previewPdf(url, att.fileName)}
                                    className="p-1 text-slate-400 hover:text-blue-600 rounded hover:bg-slate-50 transition-colors"
                                    title="Preview PDF"
                                  >
                                    <Eye className="w-3.5 h-3.5" />
                                  </button>
                                  <a
                                    href={`${url}?download=true`}
                                    target="_blank"
                                    rel="noreferrer"
                                    className="p-1 text-slate-400 hover:text-slate-600 rounded hover:bg-slate-50 transition-colors"
                                    title="Download PDF"
                                  >
                                    <Download className="w-3.5 h-3.5" />
                                  </a>
                                </div>
                              );
                            })}
                          </div>
                        </div>
                      )}

                      {/* Action Buttons */}
                      <div className="border-t border-slate-100 pt-3 flex flex-wrap items-center justify-between gap-2">
                        <div className="flex flex-wrap gap-2">
                          {/* Claim / Release */}
                          {(user.roles.includes('TriageNurse') || user.roles.includes('Admin')) && (
                            <>
                              {!r.claimedByUser ? (
                                <button onClick={() => claimReferral(r)}
                                  className="flex items-center gap-1.5 px-4 py-2 bg-blue-50 hover:bg-blue-100 text-blue-600 border border-blue-200 rounded-xl text-xs font-bold transition-all shadow-sm">
                                  <Lock className="w-3.5 h-3.5" /> Claim Referral
                                </button>
                              ) : (
                                r.claimedByUser.email === user.email && (
                                  <button onClick={() => releaseReferral(r)}
                                    className="flex items-center gap-1.5 px-4 py-2 bg-slate-100 hover:bg-slate-200 text-slate-700 border border-slate-300 rounded-xl text-xs font-bold transition-all shadow-sm">
                                    <Unlock className="w-3.5 h-3.5" /> Release Referral
                                  </button>
                                )
                              )}
                            </>
                          )}

                          {/* Status transition dropdown / buttons */}
                          {(user.roles.includes('TriageNurse') || user.roles.includes('Admin')) && (!r.claimedByUser || r.claimedByUser.email === user.email) && (
                            <div className="flex items-center gap-1">
                              {statusTransitions[r.status]?.map(nextStat => (
                                <button
                                  key={nextStat}
                                  onClick={() => transitionStatus(r, nextStat)}
                                  className="px-3.5 py-2 bg-white hover:bg-slate-50 border border-slate-200 text-slate-700 rounded-xl text-xs font-bold transition-all shadow-sm"
                                >
                                  Mark as {nextStat}
                                </button>
                              ))}
                            </div>
                          )}
                        </div>

                        {/* Edit Details Option: Admin, the creator GP, or TriageNurse */}
                        {(user.roles.includes('Admin') || user.roles.includes('TriageNurse') || r.createdByUser?.email === user.email) && (
                          <button 
                            onClick={() => handleEditClick(r)}
                            className="flex items-center gap-1.5 px-4 py-2 bg-white hover:bg-blue-50 text-blue-600 border border-blue-200 rounded-xl text-xs font-bold transition-all shadow-sm ml-auto"
                          >
                            <Pencil className="w-3.5 h-3.5" /> Update Details
                          </button>
                        )}
                      </div>
                    </div>
                  )}
                </div>
              ))}
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
                                ? 'z-10 bg-blue-600 text-white'
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

      {/* EDIT REFERRAL DETAILS MODAL (Patient is read-only) */}
      {/* EDIT REFERRAL DETAILS & HISTORY MODAL */}
      {editModalOpen && editingReferral && (
        <div className="fixed inset-0 bg-slate-900/60 backdrop-blur-sm flex items-center justify-center z-50 p-4 animate-in fade-in">
          <div className="bg-white border border-slate-200 rounded-2xl w-full max-w-4xl shadow-2xl p-6 relative flex flex-col max-h-[90vh] overflow-hidden">
            {/* Header */}
            <div className="flex items-center justify-between pb-4 border-b border-slate-100 mb-4">
              <div>
                <h2 className="text-lg font-bold text-slate-900">Referral Details & History</h2>
                <p className="text-xs text-slate-400 font-semibold">{editingReferral.caseNo}</p>
              </div>
              <button onClick={() => setEditModalOpen(false)} className="text-slate-400 hover:text-slate-600">
                <X className="w-5 h-5" />
              </button>
            </div>
            
            {/* Two-column layout */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6 overflow-hidden flex-1">
              
              {/* Left Column: Form (scrollable) */}
              <div className="overflow-y-auto pr-2 space-y-4 max-h-[65vh]">
                <form onSubmit={handleEditSubmit} className="space-y-4">
                  
                  {/* Patient Name & DOB: Locked / Read-only */}
                  <div className="p-3.5 bg-slate-50 border border-slate-200 rounded-xl">
                    <p className="text-[10px] text-slate-400 font-bold uppercase tracking-wider mb-1">Patient Details (Cannot be changed)</p>
                    <p className="text-sm font-bold text-slate-900">{editingReferral.patientName}</p>
                    <p className="text-xs text-slate-500 font-medium mt-0.5">
                      DOB: {formatDateOnly(editingReferral.patientDateOfBirth)}
                    </p>
                  </div>

                  {/* Case Number (Locked / Read-only) */}
                  <div>
                    <label className="text-xs text-slate-400 font-bold uppercase tracking-wider mb-1.5 block">Case Number (Cannot be changed)</label>
                    <input 
                      type="text" 
                      disabled 
                      value={editingReferral.caseNo}
                      className="w-full px-4 py-2.5 bg-slate-100 border border-slate-200 rounded-xl text-slate-500 text-sm font-mono font-bold cursor-not-allowed"
                    />
                  </div>

                  {/* Specialist Type */}
                  <div>
                    <label className="text-xs text-slate-400 font-bold uppercase tracking-wider mb-1.5 block">Specialist Type</label>
                    <select 
                      value={editForm.specialistType} 
                      onChange={e => setEditForm(f => ({...f, specialistType: e.target.value}))} 
                      required
                      className="w-full px-4 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-slate-900 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 font-medium"
                    >
                      {['Cardiology','Neurology','Orthopedics','Dermatology','Oncology','Ophthalmology','Gastroenterology','Pulmonology','Endocrinology','Rheumatology'].map(s => (
                        <option key={s} value={s}>{s}</option>
                      ))}
                    </select>
                  </div>

                  {/* Urgency */}
                  <div>
                    <label className="text-xs text-slate-400 font-bold uppercase tracking-wider mb-1.5 block">Urgency Level</label>
                    <div className="grid grid-cols-4 gap-1.5">
                      {['Routine', 'Soon', 'Urgent', 'Emergency'].map(lvl => (
                        <button
                          key={lvl}
                          type="button"
                          onClick={() => setEditForm(f => ({...f, urgency: lvl}))}
                          className={`py-2 rounded-lg border text-xs font-semibold transition-all ${
                            editForm.urgency === lvl
                              ? 'border-blue-500 bg-blue-50 text-blue-600 shadow-sm'
                              : 'border-slate-200 bg-white text-slate-500 hover:border-slate-300'
                          }`}
                        >
                          {lvl}
                        </button>
                      ))}
                    </div>
                  </div>

                  {/* Assigned User */}
                  <div>
                    <label className="text-xs text-slate-400 font-bold uppercase tracking-wider mb-1.5 block">Assigned Assignee</label>
                    <select 
                      value={editForm.assignedToUserId} 
                      onChange={e => setEditForm(f => ({...f, assignedToUserId: e.target.value}))}
                      required
                      className="w-full px-4 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-slate-900 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 font-medium"
                    >
                      <option value="">Unassigned</option>
                      {users.map(u => (
                        <option key={u.id} value={u.id}>
                          {u.title ? u.title + ' ' : ''}{u.fullName} ({u.roles.join(', ')})
                        </option>
                      ))}
                    </select>
                  </div>

                  {/* Reason */}
                  <div>
                    <label className="text-xs text-slate-400 font-bold uppercase tracking-wider mb-1.5 block">Clinical Reason</label>
                    <textarea 
                      required 
                      value={editForm.reason} 
                      onChange={e => setEditForm(f => ({...f, reason: e.target.value}))} 
                      rows={4}
                      maxLength={2000}
                      className="w-full px-4 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-slate-900 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 resize-none font-medium"
                    />
                  </div>

                  {/* Existing Attachments */}
                  {editingReferral.attachments && editingReferral.attachments.length > 0 && (
                    <div>
                      <label className="text-xs text-slate-400 font-bold uppercase tracking-wider mb-1.5 block">Current Attachments</label>
                      <div className="space-y-1.5 mb-3">
                        {editingReferral.attachments.map(att => {
                          const url = `${process.env.NEXT_PUBLIC_API_URL}/api/referrals/attachments/${att.id}`;
                          return (
                            <div key={att.id} className="flex items-center justify-between bg-slate-50 border border-slate-200 px-3 py-2 rounded-lg text-xs font-semibold text-slate-700">
                              <span className="truncate max-w-[280px]">{att.fileName}</span>
                              <div className="flex items-center gap-1">
                                <button
                                  type="button"
                                  onClick={() => previewPdf(url, att.fileName)}
                                  className="text-slate-400 hover:text-blue-600 p-1 rounded hover:bg-slate-100 transition-colors"
                                  title="Preview PDF"
                                >
                                  <Eye className="w-4 h-4" />
                                </button>
                                <a
                                  href={`${url}?download=true`}
                                  target="_blank"
                                  rel="noreferrer"
                                  className="p-1 text-slate-400 hover:text-slate-600 rounded hover:bg-slate-100 transition-colors flex items-center"
                                  title="Download PDF"
                                >
                                  <Download className="w-4 h-4" />
                                </a>
                              </div>
                            </div>
                          );
                        })}
                      </div>
                    </div>
                  )}

                  {/* Add More Attachments */}
                  <div>
                    <label className="text-xs text-slate-400 font-bold uppercase tracking-wider mb-1.5 block">Add More Attachments</label>
                    <input 
                      type="file" 
                      multiple 
                      accept=".pdf,application/pdf"
                      onChange={e => {
                        if (e.target.files) {
                          const list = Array.from(e.target.files);
                          const nonPdf = list.filter(f => f.type !== 'application/pdf' && !f.name.toLowerCase().endsWith('.pdf'));
                          if (nonPdf.length > 0) {
                            showToast('error', 'Only PDF files (.pdf) are allowed as attachments.');
                            e.target.value = '';
                            return;
                          }
                          setNewFiles(list);
                        }
                      }}
                      className="w-full text-xs text-slate-500 file:mr-4 file:py-2 file:px-4 file:rounded-xl file:border-0 file:text-xs file:font-semibold file:bg-blue-50 file:text-blue-700 hover:file:bg-blue-100"
                    />

                    {newFiles.length > 0 && (
                      <div className="space-y-1.5 mt-2">
                        <p className="text-[10px] text-slate-400 font-bold uppercase tracking-wider">New Attachments to Upload</p>
                        {newFiles.map((file, idx) => (
                          <div key={idx} className="flex items-center justify-between bg-blue-50/50 border border-blue-100 px-3 py-2 rounded-lg text-xs font-semibold text-slate-700">
                            <span className="truncate max-w-[280px]">{file.name}</span>
                            <div className="flex items-center gap-1">
                              <button
                                type="button"
                                onClick={() => {
                                  const url = URL.createObjectURL(file);
                                  setPreviewUrl(url);
                                  setPreviewTitle(file.name);
                                }}
                                className="text-slate-400 hover:text-blue-600 p-1 rounded hover:bg-blue-100/50 transition-colors"
                                title="Preview PDF"
                              >
                                <Eye className="w-4 h-4" />
                              </button>
                              <button
                                type="button"
                                onClick={() => setNewFiles(prev => prev.filter((_, i) => i !== idx))}
                                className="text-slate-400 hover:text-red-500 p-1 rounded hover:bg-blue-100/50 transition-colors"
                                title="Remove"
                              >
                                <X className="w-4 h-4" />
                              </button>
                            </div>
                          </div>
                        ))}
                      </div>
                    )}
                  </div>

                  <div className="flex gap-3 pt-4">
                    <button type="button" onClick={() => setEditModalOpen(false)} className="flex-1 py-2.5 border border-slate-200 text-slate-500 rounded-xl text-sm font-semibold hover:bg-slate-50 transition-all">Cancel</button>
                    <button type="submit" disabled={editSaving}
                      className="flex-1 py-2.5 bg-blue-600 hover:bg-blue-500 text-white rounded-xl text-sm font-bold flex items-center justify-center gap-2 transition-all disabled:opacity-50">
                      {editSaving ? <Loader2 className="w-4 h-4 animate-spin" /> : <Save className="w-4 h-4" />}
                      {editSaving ? 'Saving...' : 'Save Changes'}
                    </button>
                  </div>
                </form>
              </div>

              {/* Right Column: Audit Trail */}
              <div className="overflow-y-auto pl-4 border-t md:border-t-0 md:border-l border-slate-100 flex flex-col pr-1 max-h-[65vh]">
                <h3 className="text-xs font-bold text-slate-400 uppercase tracking-wider mb-4 flex items-center gap-1.5 sticky top-0 bg-white py-1 z-10">
                  <Activity className="w-3.5 h-3.5 text-slate-400" />
                  Referral History / Audit Log
                </h3>
                
                {!editingReferral.auditLogs || editingReferral.auditLogs.length === 0 ? (
                  <div className="flex-1 flex flex-col items-center justify-center py-12 text-slate-400 font-semibold text-center text-xs">
                    <Activity className="w-8 h-8 opacity-20 mb-2" />
                    <span>No history recorded yet</span>
                  </div>
                ) : (
                  <div className="relative border-l border-slate-200 pl-5 ml-3.5 space-y-6 pt-2 pb-4">
                    {[...editingReferral.auditLogs]
                      .sort((a, b) => new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime())
                      .map(log => {
                        let actionTitle = log.action;
                        let actionColor = 'bg-slate-50 text-slate-500 border-slate-200';
                        let actionIcon: React.ReactNode = <Clock className="w-3 h-3" />;

                        if (log.action === 'Created') {
                          actionTitle = 'Referral Created';
                          actionColor = 'bg-blue-50 text-blue-600 border-blue-200';
                          actionIcon = <Plus className="w-3 h-3" />;
                        } else if (log.action === 'Updated') {
                          actionTitle = 'Details Updated';
                          actionColor = 'bg-amber-50 text-amber-600 border-amber-200';
                          actionIcon = <Pencil className="w-3 h-3" />;
                        } else if (log.action === 'Claimed') {
                          actionTitle = 'Referral Claimed';
                          actionColor = 'bg-indigo-50 text-indigo-600 border-indigo-200';
                          actionIcon = <Lock className="w-3 h-3" />;
                        } else if (log.action === 'Released') {
                          actionTitle = 'Referral Released';
                          actionColor = 'bg-slate-100 text-slate-700 border-slate-300';
                          actionIcon = <Unlock className="w-3 h-3" />;
                        } else if (log.action === 'StatusChanged') {
                          const statusMap: Record<string | number, string> = {
                            1: 'Received', 2: 'Triaged', 3: 'Accepted', 4: 'Declined', 5: 'Booked', 6: 'Completed',
                            'Received': 'Received', 'Triaged': 'Triaged', 'Accepted': 'Accepted', 'Declined': 'Declined', 'Booked': 'Booked', 'Completed': 'Completed'
                          };
                          const from = log.fromStatus ? (statusMap[log.fromStatus] || log.fromStatus.toString()) : 'None';
                          const to = log.toStatus ? (statusMap[log.toStatus] || log.toStatus.toString()) : 'Received';
                          actionTitle = `Status changed: ${from} → ${to}`;
                          actionColor = statusColors[to] || 'bg-blue-50 text-blue-600 border-blue-200';
                          actionIcon = statusIcons[to] || <Clock className="w-3 h-3" />;
                        } else if (log.action.startsWith('Uploaded')) {
                          actionColor = 'bg-purple-50 text-purple-600 border-purple-200';
                          actionIcon = <Paperclip className="w-3 h-3" />;
                        }

                        const actor = log.performedByUser 
                          ? `${log.performedByUser.title ? log.performedByUser.title + ' ' : ''}${log.performedByUser.fullName}`
                          : 'System';

                        return (
                          <div key={log.id} className="relative group">
                            {/* Marker dot */}
                            <span className="absolute -left-[27px] top-0.5 flex items-center justify-center w-5.5 h-5.5 rounded-full border bg-white shadow-sm">
                              <span className={`w-4 h-4 rounded-full flex items-center justify-center p-0.5 border ${actionColor}`}>
                                {actionIcon}
                              </span>
                            </span>

                            {/* Content */}
                            <div>
                              <div className="flex items-start justify-between gap-2">
                                <p className="text-xs font-bold text-slate-800 leading-tight">{actionTitle}</p>
                                <span className="text-[10px] text-slate-400 font-bold whitespace-nowrap">{formatDate(log.timestamp)}</span>
                              </div>
                              <p className="text-[11px] text-slate-500 font-semibold mt-0.5">by {actor}</p>
                              {log.notes && (
                                <div className="mt-1.5 p-2 bg-slate-50/80 border border-slate-100 rounded-lg text-[11px] text-slate-600 italic font-medium leading-relaxed">
                                  "{log.notes}"
                                </div>
                              )}
                            </div>
                          </div>
                        );
                      })}
                  </div>
                )}
              </div>

            </div>
          </div>
        </div>
      )}

      {/* Transition Notes Modal */}
      {transitionModal && (
        <div className="fixed inset-0 bg-slate-900/60 backdrop-blur-sm flex items-center justify-center z-[100] p-4 animate-in fade-in">
          <div className="bg-white border border-slate-200 rounded-2xl w-full max-w-md shadow-2xl p-6">
            <div className="flex items-center justify-between mb-4">
              <div>
                <h3 className="text-base font-bold text-slate-900">Confirm Status Change</h3>
                <p className="text-xs text-slate-400 font-semibold mt-0.5">
                  Transitioning to <span className="text-blue-600 font-bold">{transitionModal.nextStatus}</span>
                </p>
              </div>
              <button
                type="button"
                onClick={() => setTransitionModal(null)}
                className="text-slate-400 hover:text-slate-600 p-1 rounded-lg hover:bg-slate-100 transition-colors"
              >
                <X className="w-5 h-5" />
              </button>
            </div>
            <div className="mb-5">
              <label className="text-xs text-slate-400 font-bold uppercase tracking-wider mb-1.5 block">
                Notes / Reason <span className="text-slate-300 font-normal">(optional)</span>
              </label>
              <textarea
                value={transitionNotes}
                onChange={e => setTransitionNotes(e.target.value)}
                placeholder="Enter a brief reason or notes for this status change..."
                rows={3}
                maxLength={500}
                className="w-full px-4 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-slate-900 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 font-medium resize-none"
                autoFocus
              />
              <p className="text-[10px] text-slate-400 font-semibold mt-1 text-right">{transitionNotes.length}/500</p>
            </div>
            <div className="flex gap-3">
              <button
                type="button"
                onClick={() => setTransitionModal(null)}
                disabled={transitionSaving}
                className="flex-1 py-2.5 border border-slate-200 text-slate-500 rounded-xl text-sm font-semibold hover:bg-slate-50 transition-all disabled:opacity-50"
              >
                Cancel
              </button>
              <button
                type="button"
                onClick={confirmTransition}
                disabled={transitionSaving}
                className="flex-1 py-2.5 bg-blue-600 hover:bg-blue-500 text-white rounded-xl text-sm font-bold flex items-center justify-center gap-2 transition-all disabled:opacity-50 shadow-md shadow-blue-500/10"
              >
                {transitionSaving ? <Loader2 className="w-4 h-4 animate-spin" /> : <CheckCircle2 className="w-4 h-4" />}
                {transitionSaving ? 'Saving...' : `Mark as ${transitionModal.nextStatus}`}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* PDF Preview Modal */}
      {previewUrl && (
        <div className="fixed inset-0 bg-slate-900/60 backdrop-blur-sm flex flex-col items-center justify-center z-[100] p-4 animate-in fade-in">
          <div className="bg-white border border-slate-200 rounded-2xl w-full max-w-4xl h-[85vh] shadow-2xl flex flex-col overflow-hidden">
            <div className="flex items-center justify-between px-6 py-4 border-b border-slate-100 bg-slate-50">
              <div>
                <h3 className="text-sm font-bold text-slate-900">Document Preview</h3>
                <p className="text-[10px] text-slate-400 font-semibold">{previewTitle}</p>
              </div>
              <button 
                type="button"
                onClick={() => {
                  if (previewUrl.startsWith('blob:')) {
                    URL.revokeObjectURL(previewUrl);
                  }
                  setPreviewUrl(null);
                  setPreviewTitle(null);
                }} 
                className="text-slate-400 hover:text-slate-600 p-1 rounded-lg hover:bg-slate-100 transition-colors"
              >
                <X className="w-5 h-5" />
              </button>
            </div>
            <div className="flex-1 bg-slate-100 p-4">
              <iframe 
                src={previewUrl} 
                className="w-full h-full rounded-lg border border-slate-200 shadow-sm bg-white"
                title="PDF Preview"
              />
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
