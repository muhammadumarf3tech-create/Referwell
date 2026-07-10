'use client';

import { useState, useEffect } from 'react';
import { useAuth } from '@/context/AuthContext';
import { useRouter } from 'next/navigation';
import { 
  FilePlus, Send, Loader2, AlertCircle, ArrowLeft, 
  Search, UserPlus, CheckCircle, Upload, X, ShieldAlert, Eye 
} from 'lucide-react';
import Link from 'next/link';

const formatDateOnly = (dateStr: string | null | undefined) => {
  if (!dateStr) return '—';
  const date = new Date(dateStr);
  if (isNaN(date.getTime())) return '—';
  const dd = String(date.getDate()).padStart(2, '0');
  const MM = String(date.getMonth() + 1).padStart(2, '0');
  const yyyy = date.getFullYear();
  return `${dd}/${MM}/${yyyy}`;
};

interface Patient {
  id: string;
  name: string;
  dateOfBirth: string;
  email: string;
  phoneNumber: string;
  nhiNumber: string;
  gender: string;
}

interface User {
  id: string;
  fullName: string;
  email: string;
  roles: string[];
  isActive: boolean;
  title?: string;
}

export default function NewReferralPage() {
  const { user } = useAuth();
  const router = useRouter();

  // Patients states
  const [patients, setPatients] = useState<Patient[]>([]);
  const [search, setSearch] = useState('');
  const [selectedPatient, setSelectedPatient] = useState<Patient | null>(null);
  const [patientLoading, setPatientLoading] = useState(false);

  // New patient modal
  const [showNewPatientModal, setShowNewPatientModal] = useState(false);
  const [newPatientForm, setNewPatientForm] = useState({
    name: '', dateOfBirth: '', email: '', nhiNumber: '', gender: 'Male'
  });
  const [phoneCountryCode, setPhoneCountryCode] = useState('+64');
  const [localPhone, setLocalPhone] = useState('');
  const [patientSaving, setPatientSaving] = useState(false);
  const [patientError, setPatientError] = useState('');

  // Referral states
  const [users, setUsers] = useState<User[]>([]);
  const [nextCaseNo, setNextCaseNo] = useState('');
  const [form, setForm] = useState({
    specialistType: '', reason: '', urgency: 'Routine', assignedToUserId: ''
  });
  const [attachments, setAttachments] = useState<File[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [previewTitle, setPreviewTitle] = useState<string | null>(null);

  // Load patients and users
  useEffect(() => {
    if (!user) { router.push('/login'); return; }
    loadPatients('');
    loadUsers();
    loadNextCaseNo();
    
    // Set default assignee to current user
    setForm(f => ({ ...f, assignedToUserId: user.id }));
  }, [user, router]);

  const loadPatients = async (query: string) => {
    if (!user) return;
    setPatientLoading(true);
    try {
      const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/patients?search=${encodeURIComponent(query)}`, {
        headers: { Authorization: `Bearer ${user.token}` }
      });
      if (res.ok) setPatients(await res.json());
    } catch {
      // ignore
    } finally {
      setPatientLoading(false);
    }
  };

  const loadUsers = async () => {
    if (!user) return;
    try {
      const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/users`, {
        headers: { Authorization: `Bearer ${user.token}` }
      });
      if (res.ok) {
        const uList: User[] = await res.json();
        setUsers(uList.filter(u => u.isActive));
      }
    } catch {
      // ignore
    }
  };

  const loadNextCaseNo = async () => {
    if (!user) return;
    try {
      const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/referrals/next-case-no`, {
        headers: { Authorization: `Bearer ${user.token}` }
      });
      if (res.ok) {
        const data = await res.json();
        setNextCaseNo(data.caseNo);
      }
    } catch {
      // ignore
    }
  };

  const handleSearchChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const val = e.target.value;
    setSearch(val);
    loadPatients(val);
  };

  const handleCreatePatient = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!user) return;

    // Validate NHI format (3 letters + 4 digits)
    const nhiClean = newPatientForm.nhiNumber.trim().toUpperCase();
    if (!/^[A-Z]{3}\d{4}$/.test(nhiClean)) {
      setPatientError('NHI Number must be in format ABC1234 (3 letters followed by 4 digits).');
      return;
    }

    // Validate NZ phone format if selected
    const rawLocal = localPhone.trim();
    const cleanPhoneDigits = rawLocal.replace(/[\s\-\(\)]/g, '');
    const localPhoneNormalized = cleanPhoneDigits.startsWith('0') ? cleanPhoneDigits.slice(1) : cleanPhoneDigits;
    
    if (phoneCountryCode === '+64') {
      const nzPhoneRegex = /^[23479]\d{6,8}$/;
      if (!nzPhoneRegex.test(localPhoneNormalized)) {
        setPatientError('Please enter a valid NZ phone number (e.g. 21 123 4567 or 03 123 4567).');
        return;
      }
    }

    const fullPhoneNumber = `${phoneCountryCode} ${localPhoneNormalized}`;

    setPatientSaving(true);
    setPatientError('');
    try {
      const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/patients`, {
        method: 'POST',
        headers: {
          Authorization: `Bearer ${user.token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          ...newPatientForm,
          nhiNumber: nhiClean,
          phoneNumber: fullPhoneNumber
        })
      });
      const data = await res.json();
      if (res.ok) {
        setSelectedPatient(data);
        setShowNewPatientModal(false);
        setNewPatientForm({ name: '', dateOfBirth: '', email: '', nhiNumber: '', gender: 'Male' });
        setLocalPhone('');
      } else {
        setPatientError(data.message || 'Failed to create patient');
      }
    } catch {
      setPatientError('Network error');
    } finally {
      setPatientSaving(false);
    }
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files) {
      const list = Array.from(e.target.files);
      const nonPdf = list.filter(f => f.type !== 'application/pdf' && !f.name.toLowerCase().endsWith('.pdf'));
      if (nonPdf.length > 0) {
        setError('Only PDF files (.pdf) are allowed as attachments.');
        e.target.value = '';
        return;
      }
      setAttachments(prev => [...prev, ...list]);
    }
  };

  const removeAttachment = (idx: number) => {
    setAttachments(prev => prev.filter((_, i) => i !== idx));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!user || !selectedPatient) return;
    setLoading(true);
    setError('');

    const urgencyMap: Record<string, number> = {
      'Routine': 1,
      'Soon': 2,
      'Urgent': 3,
      'Emergency': 4
    };

    try {
      // Create Referral
      const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/referrals`, {
        method: 'POST',
        headers: {
          Authorization: `Bearer ${user.token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          patientId: selectedPatient.id,
          specialistType: form.specialistType,
          reason: form.reason,
          urgency: urgencyMap[form.urgency],
          assignedToUserId: form.assignedToUserId || user.id
        })
      });

      const refData = await res.json();
      if (!res.ok) {
        setError(refData.message || 'Failed to submit referral');
        setLoading(false);
        return;
      }

      const referralId = refData.id;

      // Upload Attachments (if any)
      if (attachments.length > 0) {
        for (const file of attachments) {
          const fileData = new FormData();
          fileData.append('file', file);

          await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/referrals/${referralId}/attachments`, {
            method: 'POST',
            headers: { Authorization: `Bearer ${user.token}` },
            body: fileData
          });
        }
      }

      router.push('/dashboard');
    } catch {
      setError('Network error');
    } finally {
      setLoading(false);
    }
  };

  const urgencyOptions = [
    { value: 'Routine', label: 'Routine' },
    { value: 'Soon', label: 'Soon' },
    { value: 'Urgent', label: 'Urgent' },
    { value: 'Emergency', label: 'Emergency' },
  ];

  if (!user) return null;

  return (
    <div className="min-h-screen bg-slate-50 p-6">
      <div className="max-w-3xl mx-auto">
        <Link href="/dashboard" className="inline-flex items-center gap-2 text-slate-500 hover:text-slate-900 text-sm mb-6 transition-colors font-semibold">
          <ArrowLeft className="w-4 h-4" /> Back to Queue
        </Link>

        {/* Header */}
        <div className="flex items-center gap-3 mb-8">
          <div className="w-10 h-10 rounded-xl bg-blue-50 border border-blue-100 flex items-center justify-center">
            <FilePlus className="w-5 h-5 text-blue-600" />
          </div>
          <div>
            <h1 className="text-xl font-bold text-slate-900">New Specialist Referral</h1>
            <p className="text-slate-500 text-sm font-medium">Register the patient, select their record, and assign assignee details</p>
          </div>
        </div>

        {/* STEP 1: Select Patient */}
        {!selectedPatient ? (
          <div className="bg-white border border-slate-200 rounded-2xl p-6 shadow-sm space-y-5">
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
              <div>
                <h2 className="text-md font-bold text-slate-900">Select Patient</h2>
                <p className="text-xs text-slate-400 font-semibold mt-0.5">Search and select the patient to proceed with the referral</p>
              </div>
              <button
                type="button"
                onClick={() => { setPatientError(''); setShowNewPatientModal(true); }}
                className="flex items-center gap-1.5 px-4 py-2 bg-blue-50 text-blue-600 border border-blue-100 rounded-xl text-xs font-bold hover:bg-blue-100 transition-all self-start"
              >
                <UserPlus className="w-4 h-4" /> Register New Patient
              </button>
            </div>

            {/* Search Grid */}
            <div className="relative">
              <Search className="absolute left-3.5 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400" />
              <input
                type="text"
                value={search}
                onChange={handleSearchChange}
                placeholder="Search patient by Name, NHI Number, or Email..."
                className="w-full pl-10 pr-4 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-slate-900 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 font-medium"
              />
            </div>

            {/* Patient Grid */}
            {patientLoading ? (
              <div className="flex justify-center py-10"><Loader2 className="w-6 h-6 animate-spin text-blue-600" /></div>
            ) : patients.length === 0 ? (
              <div className="text-center py-10 border border-dashed border-slate-200 rounded-xl text-slate-400 font-semibold text-sm">
                No patients found. Try searching or register a new one.
              </div>
            ) : (
              <div className="border border-slate-200 rounded-xl overflow-hidden shadow-sm">
                <table className="w-full text-left text-sm">
                  <thead>
                    <tr className="bg-slate-50 border-b border-slate-200 text-slate-500 font-bold text-xs uppercase">
                      <th className="px-4 py-2.5">Name</th>
                      <th className="px-4 py-2.5">DOB</th>
                      <th className="px-4 py-2.5">NHI Number</th>
                      <th className="px-4 py-2.5">Gender</th>
                      <th className="px-4 py-2.5">Phone</th>
                      <th className="px-4 py-2.5">Actions</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100 font-medium text-slate-700">
                    {patients.map(p => (
                      <tr key={p.id} className="hover:bg-blue-50/20 transition-all cursor-pointer" onClick={() => setSelectedPatient(p)}>
                        <td className="px-4 py-3 font-bold text-slate-900">{p.name}</td>
                        <td className="px-4 py-3 text-xs">{formatDateOnly(p.dateOfBirth)}</td>
                        <td className="px-4 py-3 text-xs font-bold text-blue-600">{p.nhiNumber}</td>
                        <td className="px-4 py-3 text-xs">{p.gender || 'Other'}</td>
                        <td className="px-4 py-3 text-xs truncate max-w-[120px]">{p.phoneNumber}</td>
                        <td className="px-4 py-3">
                          <button
                            type="button"
                            className="px-3 py-1 bg-blue-600 hover:bg-blue-500 text-white rounded-lg text-xs font-bold shadow-sm"
                          >
                            Select
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        ) : (
          /* STEP 2: Fill Referral Details */
          <form onSubmit={handleSubmit} className="bg-white border border-slate-200 rounded-2xl p-6 shadow-sm space-y-6">
            
            {/* Selected Patient display */}
            <div className="flex items-center justify-between p-4 bg-blue-50 border border-blue-100 rounded-xl">
              <div className="flex items-center gap-3">
                <div className="w-8 h-8 rounded-full bg-blue-600 text-white flex items-center justify-center text-xs font-bold">
                  {selectedPatient.name.charAt(0).toUpperCase()}
                </div>
                <div>
                  <h3 className="text-sm font-bold text-slate-900">{selectedPatient.name}</h3>
                  <p className="text-xs text-slate-500 font-semibold mt-0.5">
                    DOB: {formatDateOnly(selectedPatient.dateOfBirth)} | NHI: {selectedPatient.nhiNumber} | Gender: {selectedPatient.gender || 'Other'}
                  </p>
                </div>
              </div>
              <button
                type="button"
                onClick={() => setSelectedPatient(null)}
                className="text-xs font-bold text-blue-600 hover:text-blue-800 hover:underline"
              >
                Change Patient
              </button>
            </div>

            {/* Case Number display (Generated, Readonly) */}
            <div>
              <label className="text-xs text-slate-400 font-bold uppercase tracking-wider mb-1.5 block">Case Number (Auto-Generated)</label>
              <input
                type="text"
                value={nextCaseNo || 'Generating...'}
                readOnly
                disabled
                className="w-full px-4 py-2.5 bg-slate-100 border border-slate-200 rounded-xl text-slate-500 text-sm font-bold font-mono focus:outline-none select-none cursor-not-allowed"
              />
            </div>

            {/* Specialist Dropdown */}
            <div>
              <label className="text-xs text-slate-400 font-bold uppercase tracking-wider mb-1.5 block">Specialist Type</label>
              <select 
                value={form.specialistType} 
                onChange={e => setForm(f => ({...f, specialistType: e.target.value}))} 
                required
                className="w-full px-4 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-slate-900 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 font-medium"
              >
                <option value="">Select specialist type</option>
                {['Cardiology','Neurology','Orthopedics','Dermatology','Oncology','Ophthalmology','Gastroenterology','Pulmonology','Endocrinology','Rheumatology'].map(s => (
                  <option key={s}>{s}</option>
                ))}
              </select>
            </div>

            {/* Urgency Selection */}
            <div>
              <label className="text-xs text-slate-400 font-bold uppercase tracking-wider mb-1.5 block">Urgency Level</label>
              <div className="grid grid-cols-4 gap-2">
                {urgencyOptions.map(opt => (
                  <button 
                    key={opt.value} 
                    type="button"
                    onClick={() => setForm(f => ({...f, urgency: opt.value}))}
                    className={`py-2 rounded-lg border text-xs font-semibold transition-all ${
                      form.urgency === opt.value
                        ? 'border-blue-500 bg-blue-50 text-blue-600 shadow-sm'
                        : 'border-slate-200 bg-white text-slate-500 hover:border-slate-300'
                    }`}
                  >
                    {opt.label}
                  </button>
                ))}
              </div>
            </div>

            {/* Assignee Option */}
            <div>
              <label className="text-xs text-slate-400 font-bold uppercase tracking-wider mb-1.5 block">Assign Referral To</label>
              <select 
                value={form.assignedToUserId} 
                onChange={e => setForm(f => ({...f, assignedToUserId: e.target.value}))} 
                required
                className="w-full px-4 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-slate-900 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 font-medium"
              >
                {users.map(u => (
                  <option key={u.id} value={u.id}>
                    {u.title ? u.title + ' ' : ''}{u.fullName} ({u.roles.join(', ')}) {u.id === user.id ? '[You]' : ''}
                  </option>
                ))}
              </select>
            </div>

            {/* Reason */}
            <div>
              <label className="text-xs text-slate-400 font-bold uppercase tracking-wider mb-1.5 block">Clinical Reason for Referral</label>
              <textarea 
                required 
                value={form.reason} 
                onChange={e => setForm(f => ({...f, reason: e.target.value}))} 
                rows={4}
                placeholder="Describe the clinical symptoms, history and necessity for this referral..."
                maxLength={2000}
                className="w-full px-4 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-slate-900 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 resize-none font-medium"
              />
            </div>

            {/* Attachments Section */}
            <div>
              <label className="text-xs text-slate-400 font-bold uppercase tracking-wider mb-1.5 block">Upload Attachments</label>
              <div className="border-2 border-dashed border-slate-200 rounded-xl p-4 flex flex-col items-center justify-center bg-slate-50/50 hover:bg-slate-50 transition-all relative">
                <Upload className="w-8 h-8 text-slate-400 mb-2" />
                <p className="text-xs text-slate-500 font-bold">Drag files here or click to browse</p>
                <input 
                  type="file" 
                  multiple 
                  accept=".pdf,application/pdf"
                  onChange={handleFileChange}
                  className="absolute inset-0 w-full h-full opacity-0 cursor-pointer"
                />
              </div>

              {attachments.length > 0 && (
                <div className="mt-3 space-y-1.5">
                  {attachments.map((file, idx) => (
                    <div key={idx} className="flex items-center justify-between bg-white border border-slate-200 px-3 py-2 rounded-lg text-xs font-semibold text-slate-700 shadow-sm">
                      <span className="truncate max-w-[300px]">{file.name}</span>
                      <div className="flex items-center gap-1.5">
                        <button
                          type="button"
                          onClick={() => {
                            const url = URL.createObjectURL(file);
                            setPreviewUrl(url);
                            setPreviewTitle(file.name);
                          }}
                          className="text-slate-400 hover:text-blue-600 p-1 rounded hover:bg-slate-50 transition-colors"
                          title="Preview PDF"
                        >
                          <Eye className="w-4 h-4" />
                        </button>
                        <button 
                          type="button" 
                          onClick={() => removeAttachment(idx)}
                          className="text-slate-400 hover:text-red-500 p-0.5"
                        >
                          <X className="w-4 h-4" />
                        </button>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>

            {error && (
              <div className="flex items-center gap-2 text-red-600 text-sm bg-red-50 border border-red-100 rounded-lg px-4 py-3 font-semibold">
                <AlertCircle className="w-4 h-4 shrink-0" /> {error}
              </div>
            )}

            {/* Submit Button */}
            <button 
              type="submit" 
              disabled={loading}
              className="w-full py-3 px-6 bg-gradient-to-r from-blue-600 to-indigo-600 hover:from-blue-500 hover:to-indigo-500 text-white font-bold rounded-xl shadow-md shadow-blue-500/10 transition-all disabled:opacity-50 flex items-center justify-center gap-2"
            >
              {loading ? <Loader2 className="w-4 h-4 animate-spin" /> : <Send className="w-4 h-4" />}
              {loading ? 'Submitting Referral...' : 'Submit Referral'}
            </button>
          </form>
        )}
      </div>

      {/* REGISTER PATIENT MODAL */}
      {showNewPatientModal && (
        <div className="fixed inset-0 bg-slate-900/40 backdrop-blur-sm flex items-center justify-center z-50 p-4">
          <div className="bg-white border border-slate-200 rounded-2xl p-6 w-full max-w-md shadow-xl max-h-[90vh] overflow-y-auto">
            <div className="flex items-center justify-between mb-6">
              <h2 className="text-lg font-bold text-slate-900">Register New Patient</h2>
              <button 
                onClick={() => setShowNewPatientModal(false)}
                className="text-slate-400 hover:text-slate-600"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            <form onSubmit={handleCreatePatient} className="space-y-4">
              <div>
                <label className="text-xs text-slate-400 font-bold uppercase tracking-wider mb-1.5 block">Full Name</label>
                <input 
                  required 
                  value={newPatientForm.name} 
                  onChange={e => setNewPatientForm(f => ({...f, name: e.target.value}))}
                  placeholder="e.g. Alice Martin"
                  maxLength={200}
                  className="w-full px-4 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-slate-900 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 font-medium"
                />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="text-xs text-slate-400 font-bold uppercase tracking-wider mb-1.5 block">Date of Birth</label>
                  <input 
                    required 
                    type="date"
                    value={newPatientForm.dateOfBirth} 
                    onChange={e => setNewPatientForm(f => ({...f, dateOfBirth: e.target.value}))}
                    className="w-full px-4 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-slate-900 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 font-medium"
                  />
                </div>
                <div>
                  <label className="text-xs text-slate-400 font-bold uppercase tracking-wider mb-1.5 block">Gender</label>
                  <select
                    value={newPatientForm.gender}
                    onChange={e => setNewPatientForm(f => ({...f, gender: e.target.value}))}
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
                  required 
                  value={newPatientForm.nhiNumber} 
                  onChange={e => setNewPatientForm(f => ({...f, nhiNumber: e.target.value}))}
                  placeholder="e.g. ABC1234"
                  maxLength={50}
                  className="w-full px-4 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-slate-900 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 font-medium uppercase"
                />
              </div>

              <div>
                <label className="text-xs text-slate-400 font-bold uppercase tracking-wider mb-1.5 block">Email Address</label>
                <input 
                  type="email"
                  value={newPatientForm.email} 
                  onChange={e => setNewPatientForm(f => ({...f, email: e.target.value}))}
                  placeholder="e.g. john.doe@example.com"
                  maxLength={256}
                  className="w-full px-4 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-slate-900 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 font-medium"
                />
              </div>

              <div>
                <label className="text-xs text-slate-400 font-bold uppercase tracking-wider mb-1.5 block">Contact Number (NZ Format)</label>
                <div className="flex gap-2">
                  <select 
                    value={phoneCountryCode} 
                    onChange={e => setPhoneCountryCode(e.target.value)}
                    className="w-28 bg-slate-50 border border-slate-200 rounded-xl text-slate-900 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 font-bold px-2"
                  >
                    <option value="+64">🇳🇿 +64 (NZ)</option>
                    <option value="+61">🇦🇺 +61 (AU)</option>
                    <option value="+1">🇺🇸 +1 (US)</option>
                    <option value="+44">🇬🇧 +44 (UK)</option>
                  </select>
                  <input 
                    required
                    type="text"
                    value={localPhone} 
                    onChange={e => setLocalPhone(e.target.value)}
                    placeholder="e.g. 21 123 4567"
                    maxLength={50}
                    className="flex-1 px-4 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-slate-900 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 font-medium"
                  />
                </div>
              </div>

              {patientError && (
                <div className="flex items-center gap-2 text-red-600 text-sm bg-red-50 border border-red-100 rounded-lg px-4 py-3 font-semibold">
                  <ShieldAlert className="w-4 h-4 shrink-0" /> {patientError}
                </div>
              )}

              <div className="flex gap-3 mt-6 pt-2">
                <button 
                  type="button" 
                  onClick={() => setShowNewPatientModal(false)}
                  className="flex-1 py-2.5 border border-slate-200 text-slate-500 rounded-xl text-sm font-semibold hover:bg-slate-50 transition-all"
                >
                  Cancel
                </button>
                <button 
                  type="submit" 
                  disabled={patientSaving}
                  className="flex-1 py-2.5 bg-blue-600 hover:bg-blue-500 text-white rounded-xl text-sm font-bold flex items-center justify-center gap-2 transition-all disabled:opacity-50"
                >
                  {patientSaving ? <Loader2 className="w-4 h-4 animate-spin" /> : <CheckCircle className="w-4 h-4" />}
                  {patientSaving ? 'Registering...' : 'Register'}
                </button>
              </div>
            </form>
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
