'use client';

import { useState } from 'react';
import { useAuth } from '@/context/AuthContext';
import { useRouter } from 'next/navigation';
import { FilePlus, Send, Loader2, AlertCircle, ArrowLeft } from 'lucide-react';
import Link from 'next/link';

export default function NewReferralPage() {
  const { user } = useAuth();
  const router = useRouter();
  const [form, setForm] = useState({
    patientName: '', patientDateOfBirth: '', specialistType: '',
    reason: '', urgency: 'Routine'
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!user) return;
    setLoading(true); setError('');
    try {
      const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/referrals`, {
        method: 'POST',
        headers: { Authorization: `Bearer ${user.token}`, 'Content-Type': 'application/json' },
        body: JSON.stringify({ ...form, urgency: parseInt(form.urgency) }),
      });
      if (res.ok) { router.push('/dashboard'); }
      else { const d = await res.json(); setError(d.message || 'Failed to create referral'); }
    } catch { setError('Network error'); }
    finally { setLoading(false); }
  };

  const urgencyOptions = [
    { value: '1', label: 'Routine', color: 'text-gray-300' },
    { value: '2', label: 'Soon', color: 'text-yellow-300' },
    { value: '3', label: 'Urgent', color: 'text-orange-300' },
    { value: '4', label: 'Emergency', color: 'text-red-300' },
  ];

  return (
    <div className="min-h-screen bg-gray-950 p-6">
      <div className="max-w-xl mx-auto">
        <Link href="/dashboard" className="inline-flex items-center gap-2 text-gray-400 hover:text-white text-sm mb-6 transition-colors">
          <ArrowLeft className="w-4 h-4" /> Back to Queue
        </Link>

        <div className="flex items-center gap-3 mb-8">
          <div className="w-10 h-10 rounded-xl bg-blue-500/10 border border-blue-500/20 flex items-center justify-center">
            <FilePlus className="w-5 h-5 text-blue-400" />
          </div>
          <div>
            <h1 className="text-xl font-bold text-white">New Referral</h1>
            <p className="text-gray-400 text-sm">Submit a new specialist referral</p>
          </div>
        </div>

        <form onSubmit={handleSubmit} className="bg-gray-900/60 border border-white/10 rounded-2xl p-6 space-y-5">
          <div>
            <label className="text-xs text-gray-400 uppercase tracking-wider mb-1.5 block">Patient Full Name</label>
            <input required value={form.patientName} onChange={e => setForm(f => ({...f, patientName: e.target.value}))}
              placeholder="John Smith"
              className="w-full px-4 py-2.5 bg-gray-800 border border-white/10 rounded-xl text-white text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/50" />
          </div>

          <div>
            <label className="text-xs text-gray-400 uppercase tracking-wider mb-1.5 block">Date of Birth</label>
            <input required type="date" value={form.patientDateOfBirth} onChange={e => setForm(f => ({...f, patientDateOfBirth: e.target.value}))}
              className="w-full px-4 py-2.5 bg-gray-800 border border-white/10 rounded-xl text-white text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/50" />
          </div>

          <div>
            <label className="text-xs text-gray-400 uppercase tracking-wider mb-1.5 block">Specialist Type</label>
            <select value={form.specialistType} onChange={e => setForm(f => ({...f, specialistType: e.target.value}))} required
              className="w-full px-4 py-2.5 bg-gray-800 border border-white/10 rounded-xl text-white text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/50">
              <option value="">Select specialist type</option>
              {['Cardiology','Neurology','Orthopedics','Dermatology','Oncology','Ophthalmology','Gastroenterology','Pulmonology','Endocrinology','Rheumatology'].map(s => (
                <option key={s}>{s}</option>
              ))}
            </select>
          </div>

          <div>
            <label className="text-xs text-gray-400 uppercase tracking-wider mb-1.5 block">Urgency Level</label>
            <div className="grid grid-cols-4 gap-2">
              {urgencyOptions.map(opt => (
                <button key={opt.value} type="button"
                  onClick={() => setForm(f => ({...f, urgency: opt.value}))}
                  className={`py-2 rounded-lg border text-sm font-medium transition-all ${
                    form.urgency === opt.value
                      ? 'border-blue-500 bg-blue-500/20 text-blue-300'
                      : 'border-white/10 bg-gray-800 text-gray-400 hover:border-white/20'
                  }`}>
                  {opt.label}
                </button>
              ))}
            </div>
          </div>

          <div>
            <label className="text-xs text-gray-400 uppercase tracking-wider mb-1.5 block">Reason for Referral</label>
            <textarea required value={form.reason} onChange={e => setForm(f => ({...f, reason: e.target.value}))} rows={4}
              placeholder="Describe the clinical reason for this referral..."
              className="w-full px-4 py-2.5 bg-gray-800 border border-white/10 rounded-xl text-white text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/50 resize-none" />
          </div>

          {error && (
            <div className="flex items-center gap-2 text-red-400 text-sm bg-red-500/10 border border-red-500/20 rounded-lg px-4 py-3">
              <AlertCircle className="w-4 h-4" /> {error}
            </div>
          )}

          <button type="submit" disabled={loading}
            className="w-full py-3 px-6 bg-gradient-to-r from-blue-600 to-purple-600 hover:from-blue-500 hover:to-purple-500 text-white font-semibold rounded-xl shadow-lg shadow-blue-500/25 transition-all disabled:opacity-50 flex items-center justify-center gap-2">
            {loading ? <Loader2 className="w-4 h-4 animate-spin" /> : <Send className="w-4 h-4" />}
            {loading ? 'Submitting...' : 'Submit Referral'}
          </button>
        </form>
      </div>
    </div>
  );
}
