'use client';

import { useEffect, useRef, useState } from 'react';
import { useAuth } from '@/context/AuthContext';
import { useRouter } from 'next/navigation';
import {
  AlertCircle, CheckCircle2, ChevronDown, Download, Eye, FileSpreadsheet,
  Loader2, Search, Upload, X,
} from 'lucide-react';
import { fetchMenuAccess, hasMenuAccess } from '@/lib/menuAccess';
import { apiFetch } from '@/lib/api';

type Batch = {
  id: string;
  fileName: string;
  status: string;
  totalRows: number;
  succeededRows: number;
  failedRows: number;
  createdPatients: number;
  notes?: string;
  startedAt: string;
  completedAt?: string;
  importedByUser?: { fullName: string; email?: string };
};

type ImportRow = {
  id: string;
  rowNumber: number;
  status: string;
  nhiNumber?: string;
  patientName?: string;
  specialistType?: string;
  urgency?: string;
  referralStatus?: string;
  legacyCaseNo?: string;
  caseNo?: string;
  referralId?: string;
  patientId?: string;
  patientCreated: boolean;
  errorColumn?: string;
  errorMessage?: string;
  rawData?: string;
};

const PAGE_SIZE = 15;
const ROW_PAGE_SIZE = 50;
const batchStatuses = ['Processing', 'Completed', 'Failed'];
const rowStatuses = ['Succeeded', 'Failed'];

const SAMPLE_FILES = [
  { name: 'sample-01-valid-cardiology.csv', label: 'Cardiology (20 rows)', desc: 'All valid cardiology referrals' },
  { name: 'sample-02-valid-mixed-specialty.csv', label: 'Mixed specialties (20 rows)', desc: 'Orthopedics, neurology, dermatology, etc.' },
  { name: 'sample-03-with-validation-errors.csv', label: 'With validation errors (20 rows)', desc: 'Mixed valid + invalid rows for report testing' },
  { name: 'sample-04-existing-patients.csv', label: 'Existing seed patients (20 rows)', desc: 'Seeded NHIs plus new patients' },
  { name: 'sample-05-historical-statuses.csv', label: 'Historical statuses (20 rows)', desc: 'Completed, declined, booked legacy cases' },
  { name: 'sample-10k-01-cardiology.csv', label: 'Cardiology (10,000 rows)', desc: 'All valid cardiology · ~1.9 MB' },
  { name: 'sample-10k-02-mixed-specialty.csv', label: 'Mixed specialties (10,000 rows)', desc: 'All valid mixed specialties · ~1.9 MB' },
  { name: 'sample-10k-03-with-errors.csv', label: 'With validation errors (10,000 rows)', desc: 'Valid + intentional errors · ~1.9 MB' },
  { name: 'sample-10k-04-historical.csv', label: 'Historical statuses (10,000 rows)', desc: 'Completed / declined / booked · ~1.9 MB' },
  { name: 'sample-10k-05-routine-volume.csv', label: 'Routine volume (10,000 rows)', desc: 'Routine urgency bulk load · ~1.9 MB' },
];

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

export default function ReferralImportPage() {
  const { user, isLoading } = useAuth();
  const router = useRouter();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [batches, setBatches] = useState<Batch[]>([]);
  const [loading, setLoading] = useState(true);
  const [uploading, setUploading] = useState(false);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [toast, setToast] = useState<{ type: 'success' | 'error'; msg: string } | null>(null);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);

  const [selectedBatch, setSelectedBatch] = useState<Batch | null>(null);
  const [rows, setRows] = useState<ImportRow[]>([]);
  const [rowsLoading, setRowsLoading] = useState(false);
  const [rowStatusFilter, setRowStatusFilter] = useState('');
  const [rowSearch, setRowSearch] = useState('');
  const [rowPage, setRowPage] = useState(1);
  const [rowTotalPages, setRowTotalPages] = useState(1);
  const [rowTotalCount, setRowTotalCount] = useState(0);
  const [expandedRowId, setExpandedRowId] = useState<string | null>(null);

  const showToast = (type: 'success' | 'error', msg: string) => {
    setToast({ type, msg });
    window.setTimeout(() => setToast(null), 4500);
  };

  useEffect(() => {
    if (isLoading) return;
    if (!user) { router.push('/login'); return; }

    let cancelled = false;
    (async () => {
      const accesses = await fetchMenuAccess(user.token);
      if (cancelled) return;
      if (!hasMenuAccess('Referral Import', user.roles, accesses)) {
        router.push('/dashboard');
      }
    })();

    return () => { cancelled = true; };
  }, [user, isLoading, router]);

  useEffect(() => { setCurrentPage(1); }, [search, statusFilter, fromDate, toDate]);

  useEffect(() => {
    if (!user) return;
    const timer = window.setTimeout(() => { void loadBatches(currentPage); }, 300);
    return () => window.clearTimeout(timer);
  }, [user, search, statusFilter, fromDate, toDate, currentPage]);

  function normalizeBatch(raw: any): Batch {
    const importedBy = raw.importedByUser ?? raw.ImportedByUser;
    return {
      id: raw.id ?? raw.Id,
      fileName: raw.fileName ?? raw.FileName ?? '',
      status: raw.status ?? raw.Status ?? '',
      totalRows: raw.totalRows ?? raw.TotalRows ?? 0,
      succeededRows: raw.succeededRows ?? raw.SucceededRows ?? 0,
      failedRows: raw.failedRows ?? raw.FailedRows ?? 0,
      createdPatients: raw.createdPatients ?? raw.CreatedPatients ?? 0,
      notes: raw.notes ?? raw.Notes,
      startedAt: raw.startedAt ?? raw.StartedAt,
      completedAt: raw.completedAt ?? raw.CompletedAt,
      importedByUser: importedBy
        ? { fullName: importedBy.fullName ?? importedBy.FullName ?? '', email: importedBy.email ?? importedBy.Email }
        : undefined,
    };
  }

  function normalizeRow(raw: any): ImportRow {
    return {
      id: raw.id ?? raw.Id,
      rowNumber: raw.rowNumber ?? raw.RowNumber ?? 0,
      status: raw.status ?? raw.Status ?? '',
      nhiNumber: raw.nhiNumber ?? raw.NhiNumber,
      patientName: raw.patientName ?? raw.PatientName,
      specialistType: raw.specialistType ?? raw.SpecialistType,
      urgency: raw.urgency ?? raw.Urgency,
      referralStatus: raw.referralStatus ?? raw.ReferralStatus,
      legacyCaseNo: raw.legacyCaseNo ?? raw.LegacyCaseNo,
      caseNo: raw.caseNo ?? raw.CaseNo,
      referralId: raw.referralId ?? raw.ReferralId,
      patientId: raw.patientId ?? raw.PatientId,
      patientCreated: raw.patientCreated ?? raw.PatientCreated ?? false,
      errorColumn: raw.errorColumn ?? raw.ErrorColumn,
      errorMessage: raw.errorMessage ?? raw.ErrorMessage,
      rawData: raw.rawData ?? raw.RawData,
    };
  }

  async function loadBatches(page: number) {
    setLoading(true);
    try {
      const params = new URLSearchParams();
      params.set('page', String(page));
      params.set('pageSize', String(PAGE_SIZE));
      if (search.trim()) params.set('search', search.trim());
      if (statusFilter) params.set('status', statusFilter);
      if (fromDate) params.set('fromDate', fromDate);
      if (toDate) params.set('toDate', toDate);

      const response = await apiFetch(`/api/referralimport?${params}`, { token: user?.token });
      if (!response.ok) return;
      const data = await response.json();
      const rawItems: any[] = data.items ?? data.Items ?? [];
      setBatches(rawItems.map(normalizeBatch));
      setTotalCount(data.totalCount ?? data.TotalCount ?? rawItems.length);
      setTotalPages(data.totalPages ?? data.TotalPages ?? 1);
      const resolved = data.page ?? data.Page ?? page;
      if (resolved !== currentPage) setCurrentPage(resolved);
    } finally {
      setLoading(false);
    }
  }

  async function loadRows(batch: Batch, page: number, status: string, term: string) {
    setRowsLoading(true);
    try {
      const params = new URLSearchParams();
      params.set('page', String(page));
      params.set('pageSize', String(ROW_PAGE_SIZE));
      if (status) params.set('status', status);
      if (term.trim()) params.set('search', term.trim());
      const response = await apiFetch(`/api/referralimport/${batch.id}/rows?${params}`, { token: user?.token });
      if (!response.ok) return;
      const data = await response.json();
      const rawItems: any[] = data.items ?? data.Items ?? [];
      setRows(rawItems.map(normalizeRow));
      setRowTotalCount(data.totalCount ?? data.TotalCount ?? rawItems.length);
      setRowTotalPages(data.totalPages ?? data.TotalPages ?? 1);
      setRowPage(data.page ?? data.Page ?? page);
    } finally {
      setRowsLoading(false);
    }
  }

  async function openReport(batch: Batch) {
    setSelectedBatch(batch);
    setRowStatusFilter('');
    setRowSearch('');
    setRowPage(1);
    setExpandedRowId(null);
    await loadRows(batch, 1, '', '');
  }

  useEffect(() => {
    if (!selectedBatch) return;
    const timer = window.setTimeout(() => {
      void loadRows(selectedBatch, rowPage, rowStatusFilter, rowSearch);
    }, 250);
    return () => window.clearTimeout(timer);
  }, [rowStatusFilter, rowSearch, rowPage]);

  async function runImport() {
    if (!selectedFile) {
      showToast('error', 'Choose a CSV file to import.');
      return;
    }
    setUploading(true);
    try {
      const formData = new FormData();
      formData.append('file', selectedFile);
      const response = await apiFetch(`/api/referralimport`, {
        method: 'POST',
        token: user?.token,
        body: formData,
      });
      const data = await response.json().catch(() => ({}));
      if (!response.ok) throw new Error(data.message || data.Message || 'Import failed.');

      const batch = normalizeBatch(data);
      showToast(
        'success',
        `Imported ${batch.succeededRows}/${batch.totalRows} rows` +
          (batch.failedRows ? ` (${batch.failedRows} failed)` : '') + '.'
      );
      setSelectedFile(null);
      if (fileInputRef.current) fileInputRef.current.value = '';
      setCurrentPage(1);
      await loadBatches(1);
      await openReport(batch);
    } catch (error) {
      showToast('error', error instanceof Error ? error.message : 'Import failed.');
    } finally {
      setUploading(false);
    }
  }

  const clearFilters = () => {
    setSearch('');
    setStatusFilter('');
    setFromDate('');
    setToDate('');
  };

  const hasFilters = Boolean(search || statusFilter || fromDate || toDate);

  if (isLoading) {
    return (
      <div className="min-h-screen bg-slate-50 flex justify-center items-center">
        <Loader2 className="w-8 h-8 animate-spin text-blue-600" />
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-slate-50 p-6">
      <div className="max-w-6xl mx-auto">
        {toast && (
          <div className={`fixed top-20 right-4 z-[60] flex gap-2 p-4 rounded-xl border shadow-lg text-sm font-semibold ${
            toast.type === 'success'
              ? 'bg-emerald-50 text-emerald-700 border-emerald-200'
              : 'bg-red-50 text-red-700 border-red-200'
          }`}>
            {toast.type === 'success' ? <CheckCircle2 className="w-4 h-4" /> : <AlertCircle className="w-4 h-4" />}
            {toast.msg}
          </div>
        )}

        <div className="flex items-start justify-between gap-4 mb-8">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-blue-50 border border-blue-100 grid place-items-center">
              <Upload className="w-5 h-5 text-blue-600" />
            </div>
            <div>
              <h1 className="text-xl font-bold text-slate-900">Referral import</h1>
              <p className="text-sm text-slate-500">
                Bulk-import legacy referrals from CSV with row-level validation reporting.
              </p>
            </div>
          </div>
          <a
            href="/api/referralimport/template"
            className="inline-flex items-center gap-2 px-3 py-2 border border-slate-200 rounded-xl text-sm font-semibold text-slate-700 bg-white hover:bg-slate-50"
            onClick={async (e) => {
              e.preventDefault();
              const res = await apiFetch(`/api/referralimport/template`, { token: user?.token });
              if (!res.ok) { showToast('error', 'Unable to download template.'); return; }
              const blob = await res.blob();
              const url = URL.createObjectURL(blob);
              const a = document.createElement('a');
              a.href = url;
              a.download = 'referral-import-template.csv';
              a.click();
              URL.revokeObjectURL(url);
            }}
          >
            <Download className="w-4 h-4" /> Template
          </a>
        </div>

        {/* Upload card */}
        <div className="bg-white border border-slate-200 rounded-2xl p-5 shadow-sm mb-6 space-y-4">
          <div className="flex flex-col sm:flex-row sm:items-center gap-4">
            <div className="flex-1">
              <p className="text-xs font-bold text-slate-400 uppercase mb-2">Upload CSV</p>
              <input
                ref={fileInputRef}
                type="file"
                accept=".csv,text/csv"
                onChange={e => setSelectedFile(e.target.files?.[0] ?? null)}
                className="block w-full text-sm text-slate-600 file:mr-4 file:py-2 file:px-4 file:rounded-xl file:border-0 file:text-sm file:font-bold file:bg-blue-50 file:text-blue-700 hover:file:bg-blue-100"
              />
              {selectedFile && (
                <p className="text-xs text-slate-500 mt-2 font-semibold">
                  {selectedFile.name} · {(selectedFile.size / 1024).toFixed(1)} KB
                </p>
              )}
            </div>
            <button
              type="button"
              disabled={uploading || !selectedFile}
              onClick={() => void runImport()}
              className="inline-flex items-center justify-center gap-2 px-5 py-2.5 bg-blue-600 text-white rounded-xl text-sm font-bold disabled:opacity-50 hover:bg-blue-700"
            >
              {uploading ? <Loader2 className="w-4 h-4 animate-spin" /> : <Upload className="w-4 h-4" />}
              {uploading ? 'Importing…' : 'Import file'}
            </button>
          </div>
          <div className="border-t border-slate-100 pt-4">
            <p className="text-xs font-bold text-slate-400 uppercase mb-2">Required columns</p>
            <div className="flex flex-wrap gap-2">
              {['NhiNumber', 'PatientName', 'DateOfBirth', 'SpecialistType', 'Reason', 'Urgency'].map(col => (
                <code key={col} className="text-xs text-blue-700 bg-slate-50 border border-slate-200 rounded px-2 py-1">{col}</code>
              ))}
            </div>
            <p className="text-xs text-slate-500 mt-2">
              Optional: PatientEmail, PatientPhone, Gender, Status, ReceivedAt, AssignedToEmail, ReferringGpEmail, LegacyCaseNo.
              When <code className="text-blue-700">LegacyCaseNo</code> is provided it is kept as the referral CaseNo (not replaced with Ref-######). Imported rows are marked Migrated.
            </p>
          </div>
        </div>

        {/* Sample files */}
        <div className="bg-white border border-slate-200 rounded-2xl p-5 shadow-sm mb-6">
          <div className="flex items-center gap-2 mb-3">
            <FileSpreadsheet className="w-4 h-4 text-blue-600" />
            <p className="text-sm font-bold text-slate-800">Sample files</p>
          </div>
          <p className="text-xs text-slate-500 mb-4">
            Small (20-row) files for quick checks, plus 10,000-row files for bulk testing. LegacyCaseNo is kept as CaseNo on import.
          </p>
          <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-3">
            {SAMPLE_FILES.map(file => (
              <a
                key={file.name}
                href={`/samples/referral-import/${file.name}`}
                download
                className="block border border-slate-200 rounded-xl p-3 hover:border-blue-300 hover:bg-blue-50/40 transition-all"
              >
                <div className="flex items-start gap-2">
                  <Download className="w-4 h-4 text-blue-600 mt-0.5 shrink-0" />
                  <div>
                    <p className="text-sm font-bold text-slate-800">{file.label}</p>
                    <p className="text-xs text-slate-500 mt-0.5">{file.desc}</p>
                  </div>
                </div>
              </a>
            ))}
          </div>
        </div>

        {/* History filters */}
        <div className="bg-white border border-slate-200 rounded-2xl p-5 shadow-sm space-y-4 mb-4">
          <p className="text-sm font-bold text-slate-800">Import history</p>
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
            <div className="relative sm:col-span-2 lg:col-span-1">
              <Search className="absolute left-3.5 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400" />
              <input
                type="text"
                value={search}
                onChange={e => setSearch(e.target.value)}
                placeholder="Search file or importer…"
                className="w-full pl-10 pr-4 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-slate-900 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 font-semibold"
              />
            </div>
            <select
              value={statusFilter}
              onChange={e => setStatusFilter(e.target.value)}
              className="w-full px-4 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-slate-900 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 font-semibold"
            >
              <option value="">All statuses</option>
              {batchStatuses.map(s => <option key={s} value={s}>{s}</option>)}
            </select>
            <DateField label="From" value={fromDate} onChange={setFromDate} />
            <DateField label="To" value={toDate} onChange={setToDate} />
          </div>
          <div className="flex items-center justify-between pt-2 border-t border-slate-100">
            <p className="text-xs font-semibold text-slate-500">{totalCount} import{totalCount === 1 ? '' : 's'}</p>
            {hasFilters && (
              <button type="button" onClick={clearFilters} className="text-xs font-bold text-blue-700 hover:text-blue-600">
                Clear filters
              </button>
            )}
          </div>
        </div>

        {loading ? (
          <div className="py-20 flex justify-center"><Loader2 className="animate-spin text-blue-600" /></div>
        ) : batches.length === 0 ? (
          <div className="py-20 text-center bg-white border rounded-2xl text-slate-400">
            {hasFilters ? 'No imports match these filters.' : 'No imports yet. Upload a CSV to get started.'}
          </div>
        ) : (
          <>
            <div className="bg-white border rounded-2xl overflow-hidden">
              <table className="w-full">
                <thead className="bg-slate-50 text-xs text-slate-500 uppercase">
                  <tr>
                    <th className="p-4 text-left">File</th>
                    <th className="p-4 text-left">Result</th>
                    <th className="p-4 text-left">Imported by</th>
                    <th className="p-4 text-left">Date</th>
                    <th className="p-4" />
                  </tr>
                </thead>
                <tbody className="divide-y">
                  {batches.map(batch => (
                    <tr key={batch.id} className="text-sm">
                      <td className="p-4 font-semibold">
                        {batch.fileName}
                        <div className="text-xs text-slate-400 mt-1">{batch.status}</div>
                      </td>
                      <td className="p-4">
                        <span className="text-emerald-600 font-bold">{batch.succeededRows}</span>
                        <span className="text-slate-500"> / {batch.totalRows}</span>
                        {batch.failedRows > 0 && (
                          <span className="text-red-600 ml-1">({batch.failedRows} failed)</span>
                        )}
                        {batch.createdPatients > 0 && (
                          <div className="text-xs text-slate-400 mt-1">{batch.createdPatients} new patient{batch.createdPatients === 1 ? '' : 's'}</div>
                        )}
                      </td>
                      <td className="p-4 text-slate-600">{batch.importedByUser?.fullName ?? '—'}</td>
                      <td className="p-4 text-slate-500">{formatDate(batch.startedAt)}</td>
                      <td className="p-4">
                        <button
                          type="button"
                          onClick={() => void openReport(batch)}
                          className="inline-flex gap-1 items-center text-blue-700 font-semibold"
                        >
                          <Eye className="w-4 h-4" /> Report
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {totalPages > 1 && (
              <div className="flex items-center justify-between border-t border-slate-200 bg-white px-6 py-4 rounded-xl shadow-sm mt-4">
                <p className="text-xs font-semibold text-slate-500">
                  Showing <span className="font-extrabold text-slate-800">{(currentPage - 1) * PAGE_SIZE + 1}</span> to{' '}
                  <span className="font-extrabold text-slate-800">{Math.min(currentPage * PAGE_SIZE, totalCount)}</span>{' '}
                  of <span className="font-extrabold text-slate-800">{totalCount}</span>
                </p>
                <nav className="isolate inline-flex -space-x-px rounded-xl shadow-sm border border-slate-200">
                  <button
                    onClick={() => setCurrentPage(p => Math.max(p - 1, 1))}
                    disabled={currentPage === 1}
                    className="relative inline-flex items-center rounded-l-xl px-3 py-2 text-slate-400 hover:bg-slate-50 disabled:opacity-50"
                  >
                    <ChevronDown className="h-4 w-4 rotate-90" />
                  </button>
                  {Array.from({ length: totalPages }, (_, i) => i + 1).map(p => (
                    <button
                      key={p}
                      onClick={() => setCurrentPage(p)}
                      className={`relative inline-flex items-center px-4 py-2 text-xs font-bold ${
                        p === currentPage ? 'z-10 bg-blue-600 text-white' : 'text-slate-900 border-l border-slate-200 hover:bg-slate-50'
                      }`}
                    >
                      {p}
                    </button>
                  ))}
                  <button
                    onClick={() => setCurrentPage(p => Math.min(p + 1, totalPages))}
                    disabled={currentPage === totalPages}
                    className="relative inline-flex items-center rounded-r-xl px-3 py-2 text-slate-400 border-l border-slate-200 hover:bg-slate-50 disabled:opacity-50"
                  >
                    <ChevronDown className="h-4 w-4 -rotate-90" />
                  </button>
                </nav>
              </div>
            )}
          </>
        )}
      </div>

      {/* Detail report modal */}
      {selectedBatch && (
        <div className="fixed inset-0 z-[55] bg-slate-900/40 p-4 overflow-y-auto">
          <div className="bg-white max-w-5xl mx-auto my-8 rounded-2xl shadow-xl">
            <div className="p-6 border-b flex justify-between gap-4">
              <div>
                <h2 className="font-bold text-lg">Import report</h2>
                <p className="text-sm text-slate-500">{selectedBatch.fileName}</p>
                <div className="flex flex-wrap gap-3 mt-3 text-xs font-semibold">
                  <span className="px-2.5 py-1 rounded-lg bg-slate-100 text-slate-700">{selectedBatch.totalRows} total</span>
                  <span className="px-2.5 py-1 rounded-lg bg-emerald-50 text-emerald-700">{selectedBatch.succeededRows} succeeded</span>
                  <span className="px-2.5 py-1 rounded-lg bg-red-50 text-red-700">{selectedBatch.failedRows} failed</span>
                  <span className="px-2.5 py-1 rounded-lg bg-blue-50 text-blue-700">{selectedBatch.createdPatients} patients created</span>
                </div>
                {selectedBatch.notes && <p className="text-xs text-slate-500 mt-2">{selectedBatch.notes}</p>}
              </div>
              <button type="button" onClick={() => setSelectedBatch(null)}><X className="text-slate-400" /></button>
            </div>

            <div className="p-4 border-b grid sm:grid-cols-3 gap-3">
              <div className="relative">
                <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400" />
                <input
                  value={rowSearch}
                  onChange={e => { setRowSearch(e.target.value); setRowPage(1); }}
                  placeholder="Search NHI, patient, case, error…"
                  className="w-full pl-9 pr-3 py-2 bg-slate-50 border border-slate-200 rounded-xl text-sm font-semibold"
                />
              </div>
              <select
                value={rowStatusFilter}
                onChange={e => { setRowStatusFilter(e.target.value); setRowPage(1); }}
                className="w-full px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl text-sm font-semibold"
              >
                <option value="">All rows</option>
                {rowStatuses.map(s => <option key={s} value={s}>{s}</option>)}
              </select>
              <p className="text-xs font-semibold text-slate-500 self-center">{rowTotalCount} row{rowTotalCount === 1 ? '' : 's'}</p>
            </div>

            <div className="p-4 max-h-[60vh] overflow-auto">
              {rowsLoading ? (
                <div className="py-12 flex justify-center"><Loader2 className="animate-spin text-blue-600" /></div>
              ) : rows.length === 0 ? (
                <p className="text-center text-slate-400 py-12">No rows match these filters.</p>
              ) : (
                <div className="space-y-2">
                  {rows.map(row => (
                    <div
                      key={row.id}
                      className={`border rounded-xl p-3 text-sm ${
                        row.status === 'Succeeded' ? 'border-emerald-100 bg-emerald-50/30' : 'border-red-100 bg-red-50/30'
                      }`}
                    >
                      <div className="flex flex-wrap items-start justify-between gap-3">
                        <div>
                          <div className="flex items-center gap-2">
                            <span className="text-xs font-bold text-slate-400">Row {row.rowNumber}</span>
                            <span className={`text-xs font-bold ${row.status === 'Succeeded' ? 'text-emerald-700' : 'text-red-700'}`}>
                              {row.status}
                            </span>
                            {row.patientCreated && (
                              <span className="text-[10px] font-bold uppercase tracking-wide text-blue-700 bg-blue-50 px-1.5 py-0.5 rounded">
                                New patient
                              </span>
                            )}
                          </div>
                          <p className="font-semibold text-slate-800 mt-1">
                            {row.patientName || '—'} <span className="font-normal text-slate-500">· {row.nhiNumber || '—'}</span>
                          </p>
                          <p className="text-xs text-slate-500 mt-0.5">
                            {row.specialistType || '—'}
                            {row.urgency ? ` · ${row.urgency}` : ''}
                            {row.referralStatus ? ` · ${row.referralStatus}` : ''}
                            {row.caseNo ? ` · ${row.caseNo}` : ''}
                            {row.legacyCaseNo ? ` · legacy ${row.legacyCaseNo}` : ''}
                          </p>
                          {row.status === 'Failed' && (
                            <p className="text-xs text-red-700 mt-1 font-semibold">
                              {row.errorColumn ? `${row.errorColumn}: ` : ''}{row.errorMessage}
                            </p>
                          )}
                        </div>
                        {row.rawData && (
                          <button
                            type="button"
                            onClick={() => setExpandedRowId(expandedRowId === row.id ? null : row.id)}
                            className="text-xs font-bold text-blue-700"
                          >
                            {expandedRowId === row.id ? 'Hide raw' : 'Raw CSV'}
                          </button>
                        )}
                      </div>
                      {expandedRowId === row.id && row.rawData && (
                        <pre className="mt-2 text-[11px] bg-white border border-slate-200 rounded-lg p-2 overflow-x-auto text-slate-600 whitespace-pre-wrap">
                          {row.rawData}
                        </pre>
                      )}
                    </div>
                  ))}
                </div>
              )}
            </div>

            {rowTotalPages > 1 && (
              <div className="p-4 border-t flex items-center justify-between">
                <p className="text-xs text-slate-500 font-semibold">Page {rowPage} of {rowTotalPages}</p>
                <div className="flex gap-2">
                  <button
                    type="button"
                    disabled={rowPage <= 1}
                    onClick={() => setRowPage(p => Math.max(1, p - 1))}
                    className="px-3 py-1.5 border rounded-lg text-xs font-bold disabled:opacity-50"
                  >
                    Previous
                  </button>
                  <button
                    type="button"
                    disabled={rowPage >= rowTotalPages}
                    onClick={() => setRowPage(p => Math.min(rowTotalPages, p + 1))}
                    className="px-3 py-1.5 border rounded-lg text-xs font-bold disabled:opacity-50"
                  >
                    Next
                  </button>
                </div>
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
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
    <div className="relative flex items-center bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 text-sm text-slate-700 focus-within:ring-2 focus-within:ring-blue-500/20 focus-within:border-blue-500 font-semibold">
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
      <button type="button" onClick={openCalendar} className="ml-2 p-1 rounded-lg text-slate-400 hover:text-blue-600 hover:bg-blue-50 shrink-0" aria-label="Open calendar">
        <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><rect width="18" height="18" x="3" y="4" rx="2" ry="2"/><line x1="16" x2="16" y1="2" y2="6"/><line x1="8" x2="8" y1="2" y2="6"/><line x1="3" x2="21" y1="10" y2="10"/></svg>
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
