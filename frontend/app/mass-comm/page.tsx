'use client';

import { useEffect, useRef, useState } from 'react';
import { useAuth } from '@/context/AuthContext';
import { useRouter } from 'next/navigation';
import { AlertCircle, Calendar, CheckCircle2, ChevronDown, Eye, Filter, Loader2, MessageSquare, Plus, Search, Send, Users, X } from 'lucide-react';
import { fetchMenuAccess, hasMenuAccess } from '@/lib/menuAccess';
import { apiFetch } from '@/lib/api';

type Campaign = { id: string; name: string; status: string; createdAt: string; createdByUser?: { fullName: string }; totalMessages: number; sentMessages: number; failedMessages: number };
type Message = { id: string; recipientName: string; recipientEmail: string; recipientType: string; referralCaseNo: string; renderedSubject: string; renderedBody: string; status: string; sentAt?: string; errorMessage?: string };
type Preview = { totalCount: number; recipients: Array<{ name: string; email: string; caseNo: string; patientName: string; status: string; subject: string; body: string }> };
type Options = { specialistTypes: string[]; assignees: Array<{ id: string; fullName: string }> };

const PAGE_SIZE = 15;
const statuses = ['Received', 'Triaged', 'Accepted', 'Declined', 'Booked', 'Completed'];
const urgencies = ['Urgent', 'SemiUrgent', 'Routine'];
const campaignStatuses = ['Sending', 'Completed'];
const mergeFields = ['{PatientName}', '{CaseNo}', '{SpecialistType}', '{Status}', '{Urgency}', '{SlaDeadline}', '{ReceivedDate}', '{ReferringGPName}'];
const emptyForm = () => ({ name: '', subjectTemplate: 'Update on your referral {CaseNo}', bodyTemplate: 'Hello {PatientName},\n\nYour {SpecialistType} referral ({CaseNo}) is now {Status}.\n\nRegards,\nReferWell', recipientType: 'Patient', filters: { urgencies: [] as string[], statuses: [] as string[], specialistTypes: [] as string[], assignedToUserIds: [] as string[], onlySlaBreached: false, receivedFrom: '', receivedTo: '', caseNo: '' } });

const formatDate = (value?: string) => {
  if (!value) return '—';
  const date = new Date(value);
  if (isNaN(date.getTime())) return '—';
  const dd = String(date.getDate()).padStart(2, '0');
  const MM = String(date.getMonth() + 1).padStart(2, '0');
  const yyyy = date.getFullYear();
  let hours = date.getHours();
  const minutes = String(date.getMinutes()).padStart(2, '0');
  const ampm = hours >= 12 ? 'pm' : 'am';
  hours = hours % 12 || 12;
  return `${dd}/${MM}/${yyyy} ${String(hours).padStart(2, '0')}:${minutes} ${ampm}`;
};

const isoToDisplay = (iso: string) => {
  if (!iso) return '';
  const [yyyy, MM, dd] = iso.split('-');
  if (!yyyy || !MM || !dd) return '';
  return `${dd}/${MM}/${yyyy}`;
};

const displayToIso = (display: string) => {
  const match = /^(\d{2})\/(\d{2})\/(\d{4})$/.exec(display.trim());
  if (!match) return null;
  const [, dd, MM, yyyy] = match;
  const date = new Date(Number(yyyy), Number(MM) - 1, Number(dd));
  if (date.getFullYear() !== Number(yyyy) || date.getMonth() !== Number(MM) - 1 || date.getDate() !== Number(dd)) return null;
  return `${yyyy}-${MM}-${dd}`;
};

const maskDdMmYyyy = (raw: string) => {
  const digits = raw.replace(/\D/g, '').slice(0, 8);
  if (digits.length <= 2) return digits;
  if (digits.length <= 4) return `${digits.slice(0, 2)}/${digits.slice(2)}`;
  return `${digits.slice(0, 2)}/${digits.slice(2, 4)}/${digits.slice(4)}`;
};

export default function MassCommPage() {
  const { user, isLoading } = useAuth();
  const router = useRouter();
  const [campaigns, setCampaigns] = useState<Campaign[]>([]);
  const [options, setOptions] = useState<Options>({ specialistTypes: [], assignees: [] });
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [step, setStep] = useState<'compose' | 'preview'>('compose');
  const [form, setForm] = useState(emptyForm());
  const [preview, setPreview] = useState<Preview | null>(null);
  const [saving, setSaving] = useState(false);
  const [selectedCampaign, setSelectedCampaign] = useState<Campaign | null>(null);
  const [messages, setMessages] = useState<Message[]>([]);
  const [toast, setToast] = useState<{ type: 'success' | 'error'; msg: string } | null>(null);

  const showToast = (type: 'success' | 'error', msg: string) => {
    setToast({ type, msg });
    window.setTimeout(() => setToast(null), 4000);
  };
  const [campaignSearch, setCampaignSearch] = useState('');
  const [campaignStatusFilter, setCampaignStatusFilter] = useState('');
  const [campaignFromDate, setCampaignFromDate] = useState('');
  const [campaignToDate, setCampaignToDate] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  useEffect(() => {
    if (isLoading) return;
    if (!user) { router.push('/login'); return; }

    let cancelled = false;
    (async () => {
      const accesses = await fetchMenuAccess(user.token);
      if (cancelled) return;
      if (!hasMenuAccess('Mass Communications', user.roles, accesses)) {
        router.push('/dashboard');
        return;
      }
      void loadOptions();
    })();

    return () => { cancelled = true; };
  }, [user, isLoading, router]);

  useEffect(() => {
    setCurrentPage(1);
  }, [campaignSearch, campaignStatusFilter, campaignFromDate, campaignToDate]);

  useEffect(() => {
    if (!user) return;
    const timer = window.setTimeout(() => { void loadCampaigns(currentPage); }, 300);
    return () => window.clearTimeout(timer);
  }, [user, campaignSearch, campaignStatusFilter, campaignFromDate, campaignToDate, currentPage]);

  async function loadOptions() {
    const optionResponse = await apiFetch(`/api/masscomm/filter-options`, { token: user?.token });
    if (!optionResponse.ok) return;
    const data = await optionResponse.json();
    const rawAssignees: any[] = data.assignees ?? data.Assignees ?? [];
    setOptions({
      specialistTypes: data.specialistTypes ?? data.SpecialistTypes ?? [],
      assignees: rawAssignees
        .map(a => ({
          id: String(a.id ?? a.Id ?? ''),
          fullName: String(a.fullName ?? a.FullName ?? ''),
        }))
        .filter(a => a.id),
    });
  }

  function buildCampaignQuery(page: number) {
    const params = new URLSearchParams();
    params.set('page', String(page));
    params.set('pageSize', String(PAGE_SIZE));
    if (campaignSearch.trim()) params.set('search', campaignSearch.trim());
    if (campaignStatusFilter) params.set('status', campaignStatusFilter);
    if (campaignFromDate) params.set('fromDate', campaignFromDate);
    if (campaignToDate) params.set('toDate', campaignToDate);
    return params;
  }

  function normalizeCampaign(raw: any): Campaign {
    const createdBy = raw.createdByUser ?? raw.CreatedByUser;
    return {
      id: raw.id ?? raw.Id,
      name: raw.name ?? raw.Name ?? '',
      status: raw.status ?? raw.Status ?? '',
      createdAt: raw.createdAt ?? raw.CreatedAt,
      createdByUser: createdBy
        ? { fullName: createdBy.fullName ?? createdBy.FullName ?? '' }
        : undefined,
      totalMessages: raw.totalMessages ?? raw.TotalMessages ?? 0,
      sentMessages: raw.sentMessages ?? raw.SentMessages ?? 0,
      failedMessages: raw.failedMessages ?? raw.FailedMessages ?? 0,
    };
  }

  function matchesCampaignFilters(campaign: Campaign) {
    if (campaignStatusFilter && campaign.status !== campaignStatusFilter) return false;
    const createdKey = campaign.createdAt ? String(campaign.createdAt).slice(0, 10) : '';
    if (campaignFromDate && createdKey && createdKey < campaignFromDate) return false;
    if (campaignToDate && createdKey && createdKey > campaignToDate) return false;
    const searchText = campaignSearch.trim().toLowerCase();
    if (!searchText) return true;
    const createdBy = campaign.createdByUser?.fullName?.toLowerCase() ?? '';
    return campaign.name.toLowerCase().includes(searchText)
      || createdBy.includes(searchText)
      || campaign.status.toLowerCase().includes(searchText);
  }

  async function loadCampaigns(page: number, silent = false) {
    if (!silent) setLoading(true);
    try {
      const response = await apiFetch(`/api/masscomm?${buildCampaignQuery(page)}`, { token: user?.token });
      if (!response.ok) return;
      const data = await response.json();

      // Legacy unpaginated array response — filter + paginate on the client.
      if (Array.isArray(data)) {
        const filtered = data.map(normalizeCampaign).filter(matchesCampaignFilters);
        const count = filtered.length;
        const pages = Math.max(1, Math.ceil(count / PAGE_SIZE));
        const resolvedPage = Math.min(Math.max(page, 1), pages);
        const items = filtered.slice((resolvedPage - 1) * PAGE_SIZE, resolvedPage * PAGE_SIZE);
        setCampaigns(items);
        setTotalCount(count);
        setTotalPages(pages);
        if (resolvedPage !== currentPage) setCurrentPage(resolvedPage);
        setSelectedCampaign(current => {
          if (!current) return current;
          return items.find(campaign => campaign.id === current.id) ?? current;
        });
        return;
      }

      const rawItems: any[] = data.items ?? data.Items ?? [];
      const items = rawItems.map(normalizeCampaign);
      const count = data.totalCount ?? data.TotalCount ?? items.length;
      const pages = data.totalPages ?? data.TotalPages ?? Math.max(1, Math.ceil(count / PAGE_SIZE));
      const resolvedPage = data.page ?? data.Page ?? page;

      setCampaigns(items);
      setTotalCount(count);
      setTotalPages(pages);
      if (resolvedPage !== currentPage) setCurrentPage(resolvedPage);
      setSelectedCampaign(current => {
        if (!current) return current;
        return items.find(campaign => campaign.id === current.id) ?? current;
      });
    } finally {
      if (!silent) setLoading(false);
    }
  }

  const hasSendingCampaign = campaigns.some(campaign => campaign.status === 'Sending');
  useEffect(() => {
    if (!user || !hasSendingCampaign) return;
    const interval = window.setInterval(() => { void loadCampaigns(currentPage, true); }, 1000);
    return () => window.clearInterval(interval);
  }, [hasSendingCampaign, user, currentPage, campaignSearch, campaignStatusFilter, campaignFromDate, campaignToDate]);

  useEffect(() => {
    if (!user || !selectedCampaign || selectedCampaign.status !== 'Sending') return;
    const loadMessages = async () => {
      const response = await apiFetch(`/api/masscomm/${selectedCampaign.id}/messages`, { token: user?.token });
      if (response.ok) setMessages(await response.json());
    };
    const interval = window.setInterval(() => { void loadMessages(); }, 1000);
    return () => window.clearInterval(interval);
  }, [selectedCampaign?.id, selectedCampaign?.status, user]);

  const toggle = (key: 'urgencies' | 'statuses' | 'specialistTypes' | 'assignedToUserIds', value: string) => setForm(current => ({ ...current, filters: { ...current.filters, [key]: current.filters[key].includes(value) ? current.filters[key].filter(x => x !== value) : [...current.filters[key], value] } }));
  const insertField = (field: string) => setForm(current => ({ ...current, bodyTemplate: `${current.bodyTemplate}${current.bodyTemplate.endsWith(' ') || !current.bodyTemplate ? '' : ' '}${field}` }));
  const openComposer = () => { setForm(emptyForm()); setPreview(null); setStep('compose'); setShowModal(true); };
  const requestPayload = () => ({
    ...form,
    filters: {
      ...form.filters,
      assignedToUserIds: form.filters.assignedToUserIds.filter(id => !!id && id !== 'undefined' && id !== 'null'),
      receivedFrom: form.filters.receivedFrom || null,
      receivedTo: form.filters.receivedTo || null,
      caseNo: form.filters.caseNo.trim() || null,
    }
  });
  const requestPreview = async () => {
    setSaving(true);
    try {
      const response = await apiFetch(`/api/masscomm/preview`, { method: 'POST', token: user?.token, headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(requestPayload()) });
      const data = await response.json();
      if (!response.ok) throw new Error(data.message || 'Unable to prepare preview.');
      setPreview(data); setStep('preview');
    } catch (error) { showToast('error', error instanceof Error ? error.message : 'Unable to prepare preview.'); }
    finally { setSaving(false); }
  };
  const sendCampaign = async () => {
    setSaving(true);
    try {
      const response = await apiFetch(`/api/masscomm`, { method: 'POST', token: user?.token, headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(requestPayload()) });
      const data = await response.json();
      if (!response.ok) throw new Error(data.message || 'Unable to send campaign.');
      showToast('success', `${data.messageCount} messages queued for delivery.`);
      setShowModal(false);
      setCurrentPage(1);
      await loadCampaigns(1);
    } catch (error) { showToast('error', error instanceof Error ? error.message : 'Unable to send campaign.'); }
    finally { setSaving(false); }
  };
  const viewMessages = async (campaign: Campaign) => {
    setSelectedCampaign(campaign);
    setMessages([]);
    const response = await apiFetch(`/api/masscomm/${campaign.id}/messages`, { token: user?.token });
    if (response.ok) setMessages(await response.json());
  };
  const clearCampaignFilters = () => {
    setCampaignSearch('');
    setCampaignStatusFilter('');
    setCampaignFromDate('');
    setCampaignToDate('');
  };
  const hasCampaignFilters = Boolean(campaignSearch || campaignStatusFilter || campaignFromDate || campaignToDate);
  const displayedCampaigns = campaigns.filter(matchesCampaignFilters);
  const displayCount = hasCampaignFilters && displayedCampaigns.length !== campaigns.length
    ? displayedCampaigns.length
    : totalCount;

  if (isLoading) return <div className="min-h-screen bg-slate-50 flex justify-center items-center"><Loader2 className="w-8 h-8 animate-spin text-teal-600" /></div>;

  return <div className="min-h-screen bg-slate-50 p-6"><div className="max-w-6xl mx-auto">
    {toast && <div className={`fixed top-20 right-4 z-[60] flex gap-2 p-4 rounded-xl border shadow-lg text-sm font-semibold ${toast.type === 'success' ? 'bg-emerald-50 text-emerald-700 border-emerald-200' : 'bg-red-50 text-red-700 border-red-200'}`}>{toast.type === 'success' ? <CheckCircle2 className="w-4 h-4" /> : <AlertCircle className="w-4 h-4" />}{toast.msg}</div>}
    <div className="flex items-center justify-between mb-8"><div className="flex items-center gap-3"><div className="w-10 h-10 rounded-xl bg-teal-50 border border-teal-100 grid place-items-center"><MessageSquare className="w-5 h-5 text-teal-600" /></div><div><h1 className="text-xl font-bold text-slate-900">Referral status communications</h1><p className="text-sm text-slate-500">Notify patients or referring GPs, then retain a complete delivery record.</p></div></div><button onClick={openComposer} className="flex items-center gap-2 px-4 py-2 bg-teal-600 text-white rounded-xl text-sm font-bold"><Plus className="w-4 h-4" />New campaign</button></div>
    <div className="bg-white border border-slate-200 rounded-xl p-4 mb-6"><p className="text-xs font-bold text-slate-400 uppercase mb-2">Merge fields</p><div className="flex flex-wrap gap-2">{mergeFields.map(field => <code key={field} className="text-xs text-blue-600 bg-slate-50 border rounded px-2 py-1">{field}</code>)}</div></div>

    <div className="bg-white border border-slate-200 rounded-2xl p-5 shadow-sm space-y-4 mb-4">
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <div className="relative sm:col-span-2 lg:col-span-1">
          <Search className="absolute left-3.5 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400" />
          <input
            type="text"
            value={campaignSearch}
            onChange={e => setCampaignSearch(e.target.value)}
            placeholder="Search campaign or creator..."
            className="w-full pl-10 pr-4 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-slate-900 text-sm focus:outline-none focus:ring-2 focus:ring-teal-500/20 focus:border-teal-500 font-semibold transition-all"
          />
        </div>
        <select
          value={campaignStatusFilter}
          onChange={e => setCampaignStatusFilter(e.target.value)}
          className="w-full px-4 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-slate-900 text-sm focus:outline-none focus:ring-2 focus:ring-teal-500/20 focus:border-teal-500 font-semibold"
        >
          <option value="">All statuses</option>
          {campaignStatuses.map(status => <option key={status} value={status}>{status}</option>)}
        </select>
        <DateField label="From" value={campaignFromDate} onChange={setCampaignFromDate} />
        <DateField label="To" value={campaignToDate} onChange={setCampaignToDate} />
      </div>
      <div className="flex items-center justify-between pt-2 border-t border-slate-100">
        <p className="text-xs font-semibold text-slate-500">{displayCount} campaign{displayCount === 1 ? '' : 's'}</p>
        {hasCampaignFilters && <button type="button" onClick={clearCampaignFilters} className="text-xs font-bold text-teal-700 hover:text-teal-600">Clear filters</button>}
      </div>
    </div>

    {loading ? <div className="py-20 flex justify-center"><Loader2 className="animate-spin text-teal-600" /></div> : displayedCampaigns.length === 0 ? <div className="py-20 text-center bg-white border rounded-2xl text-slate-400">{hasCampaignFilters ? 'No campaigns match these filters.' : 'No campaigns have been sent yet.'}</div> : <>
      <div className="bg-white border rounded-2xl overflow-hidden"><table className="w-full"><thead className="bg-slate-50 text-xs text-slate-500 uppercase"><tr><th className="p-4 text-left">Campaign</th><th className="p-4 text-left">Delivery</th><th className="p-4 text-left">Created by</th><th className="p-4 text-left">Date</th><th className="p-4" /></tr></thead><tbody className="divide-y">{displayedCampaigns.map(c => <tr key={c.id} className="text-sm"><td className="p-4 font-semibold">{c.name}<div className="text-xs text-slate-400 mt-1">{c.status}</div></td><td className="p-4"><span className="text-emerald-600 font-bold">{c.sentMessages}</span> / {c.totalMessages}{c.failedMessages > 0 && <span className="text-red-600 ml-1">({c.failedMessages} failed)</span>}</td><td className="p-4 text-slate-600">{c.createdByUser?.fullName ?? '—'}</td><td className="p-4 text-slate-500">{formatDate(c.createdAt)}</td><td className="p-4"><button onClick={() => void viewMessages(c)} className="inline-flex gap-1 items-center text-teal-700 font-semibold"><Eye className="w-4 h-4" />View</button></td></tr>)}</tbody></table></div>
      {totalPages > 1 && (
        <div className="flex items-center justify-between border-t border-slate-200 bg-white px-6 py-4 rounded-xl shadow-sm mt-4">
          <div className="flex flex-1 justify-between sm:hidden">
            <button onClick={() => setCurrentPage(prev => Math.max(prev - 1, 1))} disabled={currentPage === 1} className="relative inline-flex items-center rounded-xl border border-slate-200 bg-white px-4 py-2 text-xs font-bold text-slate-700 hover:bg-slate-50 transition-all disabled:opacity-50">Previous</button>
            <button onClick={() => setCurrentPage(prev => Math.min(prev + 1, totalPages))} disabled={currentPage === totalPages} className="relative ml-3 inline-flex items-center rounded-xl border border-slate-200 bg-white px-4 py-2 text-xs font-bold text-slate-700 hover:bg-slate-50 transition-all disabled:opacity-50">Next</button>
          </div>
          <div className="hidden sm:flex sm:flex-1 sm:items-center sm:justify-between">
            <p className="text-xs font-semibold text-slate-500">
              Showing <span className="font-extrabold text-slate-800">{displayedCampaigns.length === 0 ? 0 : (currentPage - 1) * PAGE_SIZE + 1}</span> to{' '}
              <span className="font-extrabold text-slate-800">{Math.min(currentPage * PAGE_SIZE, displayCount)}</span>{' '}
              of <span className="font-extrabold text-slate-800">{displayCount}</span> results
            </p>
            <nav className="isolate inline-flex -space-x-px rounded-xl shadow-sm border border-slate-200" aria-label="Pagination">
              <button onClick={() => setCurrentPage(prev => Math.max(prev - 1, 1))} disabled={currentPage === 1} className="relative inline-flex items-center rounded-l-xl px-3 py-2 text-slate-400 hover:bg-slate-50 disabled:opacity-50"><ChevronDown className="h-4 w-4 rotate-90" /></button>
              {Array.from({ length: totalPages }, (_, i) => i + 1).map(p => (
                <button key={p} onClick={() => setCurrentPage(p)} className={`relative inline-flex items-center px-4 py-2 text-xs font-bold focus:z-20 transition-all ${p === currentPage ? 'z-10 bg-teal-600 text-white' : 'text-slate-900 border-l border-slate-200 hover:bg-slate-50'}`}>{p}</button>
              ))}
              <button onClick={() => setCurrentPage(prev => Math.min(prev + 1, totalPages))} disabled={currentPage === totalPages} className="relative inline-flex items-center rounded-r-xl px-3 py-2 text-slate-400 border-l border-slate-200 hover:bg-slate-50 disabled:opacity-50"><ChevronDown className="h-4 w-4 -rotate-90" /></button>
            </nav>
          </div>
        </div>
      )}
    </>}

  </div>
  {showModal && <div className="fixed inset-0 z-50 bg-slate-900/40 backdrop-blur-sm overflow-y-auto p-4"><div className="bg-white max-w-3xl mx-auto my-8 rounded-2xl shadow-xl"><div className="flex justify-between p-6 border-b"><div><h2 className="font-bold text-lg">{step === 'compose' ? 'Create referral status campaign' : 'Preview and confirm'}</h2><p className="text-sm text-slate-500">{step === 'compose' ? 'Select the referral group and personalise the notification.' : 'Review the final recipient list and rendered messages before sending.'}</p></div><button onClick={() => setShowModal(false)}><X className="text-slate-400" /></button></div>
    {step === 'compose' ? <div className="p-6 space-y-5"><div className="grid grid-cols-2 gap-4"><label className="text-sm font-semibold">Campaign name<input value={form.name} onChange={e => setForm({ ...form, name: e.target.value })} className="input mt-1" placeholder="e.g. Accepted referral update" /></label><label className="text-sm font-semibold">Notify<select value={form.recipientType} onChange={e => setForm({ ...form, recipientType: e.target.value })} className="input mt-1"><option value="Patient">Patient</option><option value="ReferringGP">Referring GP (submitter)</option></select></label></div><label className="text-sm font-semibold block">Subject<input value={form.subjectTemplate} onChange={e => setForm({ ...form, subjectTemplate: e.target.value })} className="input mt-1" /></label><label className="text-sm font-semibold block">Message<textarea rows={6} value={form.bodyTemplate} onChange={e => setForm({ ...form, bodyTemplate: e.target.value })} className="input mt-1 resize-y" /></label><div className="flex flex-wrap gap-2">{mergeFields.map(field => <button key={field} onClick={() => insertField(field)} className="text-xs px-2 py-1 border rounded text-blue-600 hover:bg-blue-50">+ {field}</button>)}</div>
      <div className="border rounded-xl p-4"><div className="flex gap-2 items-center font-bold text-sm mb-3"><Filter className="w-4 h-4 text-teal-600" />Referral filters <span className="font-normal text-slate-400">(leave unchecked for all)</span></div><div className="grid sm:grid-cols-2 gap-4"><FilterGroup title="Status" values={statuses} selected={form.filters.statuses} onToggle={v => toggle('statuses', v)} /><FilterGroup title="Urgency" values={urgencies} selected={form.filters.urgencies} onToggle={v => toggle('urgencies', v)} /><FilterGroup title="Specialty" values={options.specialistTypes} selected={form.filters.specialistTypes} onToggle={v => toggle('specialistTypes', v)} /><FilterGroup title="Assigned hospital staff" values={options.assignees.map(a => a.id)} selected={form.filters.assignedToUserIds} onToggle={v => toggle('assignedToUserIds', v)} labelFor={id => options.assignees.find(a => a.id === id)?.fullName ?? id} /></div>
        <div className="grid sm:grid-cols-3 gap-3 mt-4">
          <div>
            <p className="text-xs font-bold text-slate-500 mb-1">RECEIVED FROM</p>
            <DateField value={form.filters.receivedFrom} onChange={value => setForm({ ...form, filters: { ...form.filters, receivedFrom: value } })} />
          </div>
          <div>
            <p className="text-xs font-bold text-slate-500 mb-1">RECEIVED TO</p>
            <DateField value={form.filters.receivedTo} onChange={value => setForm({ ...form, filters: { ...form.filters, receivedTo: value } })} />
          </div>
          <label className="text-xs font-bold text-slate-500">CASE NUMBER<input value={form.filters.caseNo} onChange={e => setForm({ ...form, filters: { ...form.filters, caseNo: e.target.value } })} placeholder="Ref-000001" className="input mt-1" /></label>
        </div>
        <label className="flex gap-2 mt-3 text-sm"><input type="checkbox" checked={form.filters.onlySlaBreached} onChange={e => setForm({ ...form, filters: { ...form.filters, onlySlaBreached: e.target.checked } })} />Only active SLA-breached referrals <span className="text-slate-400">(excludes paused / waiting on patient)</span></label></div></div> : <div className="p-6"><div className="flex items-center gap-2 text-teal-700 bg-teal-50 p-3 rounded-xl mb-4"><Users className="w-5 h-5" /><b>{preview?.totalCount ?? 0}</b> valid recipients match these filters.</div>{preview?.totalCount === 0 ? <p className="text-red-600">No valid recipient emails were found. Go back and adjust the audience.</p> : <div className="max-h-[50vh] overflow-auto border rounded-xl divide-y">{preview?.recipients.map((item, index) => <div key={`${item.caseNo}-${index}`} className="p-4 text-sm"><div className="font-bold">{item.name} <span className="font-normal text-slate-500">&lt;{item.email}&gt;</span></div><div className="text-xs text-slate-500 mb-2">{item.caseNo} · {item.status}</div><div className="font-semibold">{item.subject}</div><p className="whitespace-pre-line text-slate-600 mt-1">{item.body}</p></div>)}</div>} {preview && preview.totalCount > preview.recipients.length && <p className="text-xs text-slate-500 mt-2">Showing the first {preview.recipients.length} recipients.</p>}</div>}
    <div className="flex justify-end gap-3 p-6 border-t"><button onClick={() => step === 'preview' ? setStep('compose') : setShowModal(false)} className="px-4 py-2 border rounded-xl text-sm font-semibold">{step === 'preview' ? 'Back to edit' : 'Cancel'}</button>{step === 'compose' ? <button disabled={saving || !form.name || !form.bodyTemplate} onClick={() => void requestPreview()} className="px-4 py-2 rounded-xl bg-teal-600 text-white text-sm font-bold disabled:opacity-50">{saving ? <Loader2 className="w-4 h-4 animate-spin" /> : 'Preview recipients'}</button> : <button disabled={saving || !preview?.totalCount} onClick={() => void sendCampaign()} className="inline-flex gap-2 items-center px-4 py-2 rounded-xl bg-teal-600 text-white text-sm font-bold disabled:opacity-50"><Send className="w-4 h-4" />{saving ? 'Queuing...' : 'Confirm & send'}</button>}</div></div></div>}
  {selectedCampaign && <div className="fixed inset-0 z-[55] bg-slate-900/40 p-4 overflow-y-auto"><div className="bg-white max-w-3xl mx-auto my-8 rounded-2xl shadow-xl"><div className="p-6 border-b flex justify-between"><div><h2 className="font-bold text-lg">Sent-message record</h2><p className="text-sm text-slate-500">{selectedCampaign.name}</p></div><button onClick={() => setSelectedCampaign(null)}><X className="text-slate-400" /></button></div><div className="p-6 max-h-[70vh] overflow-auto space-y-3">{messages.length === 0 ? <p className="text-slate-500">Loading messages…</p> : messages.map(message => <div className="border rounded-xl p-4 text-sm" key={message.id}><div className="flex justify-between gap-4"><div><b>{message.recipientName}</b> <span className="text-slate-500">&lt;{message.recipientEmail}&gt;</span><p className="text-xs text-slate-500">{message.recipientType === 'Patient' ? 'Patient' : 'Referring GP'} · {message.referralCaseNo}</p></div><div className={message.status === 'Sent' ? 'text-emerald-600 font-semibold' : message.status === 'Failed' ? 'text-red-600 font-semibold' : 'text-slate-500 font-semibold'}>{message.status}</div></div><p className="font-semibold mt-3">{message.renderedSubject}</p><p className="whitespace-pre-line text-slate-600 mt-1">{message.renderedBody}</p><p className="text-xs text-slate-400 mt-2">{message.sentAt ? `Sent ${formatDate(message.sentAt)}` : message.errorMessage ?? 'Queued for delivery'}</p></div>)}</div></div></div>}
  <style jsx>{`.input { width: 100%; padding: .6rem .75rem; border: 1px solid #cbd5e1; border-radius: .75rem; background: #f8fafc; font-size: .875rem; font-weight: 500; } .input:focus { outline: 2px solid rgb(20 184 166 / .2); border-color: #14b8a6; }`}</style>
  </div>;
}

function DateField({ value, onChange, label }: { value: string; onChange: (value: string) => void; label?: string }) {
  const dateRef = useRef<HTMLInputElement>(null);
  const [text, setText] = useState(isoToDisplay(value));

  useEffect(() => {
    setText(isoToDisplay(value));
  }, [value]);

  const openCalendar = () => {
    const input = dateRef.current;
    if (!input) return;
    if (typeof input.showPicker === 'function') input.showPicker();
    else input.click();
  };

  return (
    <div className={`relative flex items-center bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-sm text-slate-700 focus-within:ring-2 focus-within:ring-teal-500/20 focus-within:border-teal-500 font-semibold ${label ? '' : 'w-full'}`}>
      {label && <span className="text-slate-400 text-[10px] font-bold uppercase tracking-wider mr-2 shrink-0">{label}</span>}
      <input
        type="text"
        inputMode="numeric"
        placeholder="dd/MM/yyyy"
        value={text}
        onChange={e => {
          const next = maskDdMmYyyy(e.target.value);
          setText(next);
          if (!next) { onChange(''); return; }
          const iso = displayToIso(next);
          if (iso) onChange(iso);
        }}
        onBlur={() => {
          if (!text) { onChange(''); return; }
          const iso = displayToIso(text);
          if (iso) onChange(iso);
          else setText(isoToDisplay(value));
        }}
        className="bg-transparent border-none text-slate-700 text-sm focus:outline-none font-semibold w-full min-w-0"
      />
      <button type="button" onClick={openCalendar} className="ml-2 p-1 rounded-lg text-slate-400 hover:text-teal-600 hover:bg-teal-50 shrink-0" aria-label="Open calendar">
        <Calendar className="w-4 h-4" />
      </button>
      <input
        ref={dateRef}
        type="date"
        value={value}
        onChange={e => onChange(e.target.value)}
        className="absolute opacity-0 pointer-events-none w-0 h-0"
        tabIndex={-1}
      />
    </div>
  );
}

function FilterGroup({ title, values, selected, onToggle, labelFor }: { title: string; values: string[]; selected: string[]; onToggle: (value: string) => void; labelFor?: (value: string) => string }) {
  const [isOpen, setIsOpen] = useState(false);
  const [query, setQuery] = useState('');
  const containerRef = useRef<HTMLDivElement>(null);
  const displayValue = (value: string) => {
    if (labelFor) return labelFor(value);
    return value === 'SemiUrgent' ? 'Semi-Urgent' : value;
  };
  const filteredValues = values.filter(value => displayValue(value).toLowerCase().includes(query.toLowerCase()));

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false);
        setQuery('');
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const toggleDropdown = () => {
    setIsOpen(open => !open);
    if (isOpen) setQuery('');
  };

  return <div className="relative" ref={containerRef}><p className="text-xs font-bold text-slate-500 uppercase mb-2">{title}</p><button type="button" onClick={toggleDropdown} className="flex w-full items-center justify-between gap-2 bg-white border border-slate-200 rounded-xl px-4 py-2 text-sm text-slate-600 focus:outline-none focus:ring-2 focus:ring-teal-500/20 font-bold shadow-sm select-none"><span className="truncate">{selected.length === 0 ? `All ${title.toLowerCase()}` : selected.length === 1 ? displayValue(selected[0]) : `${selected.length} Selected`}</span><ChevronDown className={`w-4 h-4 transition-transform duration-200 ${isOpen ? 'rotate-180' : ''}`} /></button>{isOpen && <div className="absolute z-20 w-full mt-1.5 bg-white border border-slate-200 rounded-xl shadow-xl p-2 space-y-1.5"><div className="relative px-1 py-0.5"><Search className="absolute left-3 top-1/2 -translate-y-1/2 w-3.5 h-3.5 text-slate-400" /><input autoFocus value={query} onChange={event => setQuery(event.target.value)} placeholder={`Search ${title.toLowerCase()}...`} className="w-full pl-8 pr-3 py-1.5 bg-slate-50 border border-slate-200 rounded-lg text-slate-900 text-xs focus:outline-none focus:ring-1 focus:ring-teal-500/20 focus:border-teal-500 font-semibold" /></div><div className="flex items-center justify-between px-2 pb-1 text-xs font-bold"><button type="button" onClick={() => values.filter(value => !selected.includes(value)).forEach(onToggle)} className="text-teal-700 hover:text-teal-600">Select all</button><button type="button" onClick={() => [...selected].forEach(onToggle)} className="text-slate-500 hover:text-slate-700">Deselect all</button></div><div className="max-h-60 overflow-y-auto space-y-1 pr-1">{filteredValues.length ? filteredValues.map(value => <label key={value} className="flex items-center gap-2 px-3 py-2 hover:bg-slate-50 rounded-lg cursor-pointer text-sm font-semibold text-slate-700 select-none"><input type="checkbox" checked={selected.includes(value)} onChange={() => onToggle(value)} className="w-4 h-4 text-teal-600 border-slate-300 rounded focus:ring-teal-500 accent-teal-600" /><span>{displayValue(value)}</span></label>) : <p className="text-center py-3 text-slate-400 text-xs font-semibold">No matching options</p>}</div></div>}</div>;
}
