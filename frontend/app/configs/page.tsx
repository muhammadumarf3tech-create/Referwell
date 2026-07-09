'use client';

import { useEffect, useState } from 'react';
import { useAuth } from '@/context/AuthContext';
import { useRouter } from 'next/navigation';
import { Settings, Save, Loader2, AlertCircle, CheckCircle2, Info } from 'lucide-react';

interface Weight {
  key: string;
  value: string;
  description: string;
}

const keyLabels: Record<string, { label: string; color: string; desc: string }> = {
  weight_urgency:  { label: 'Urgency Weight',    color: 'from-red-500 to-orange-500', desc: 'Higher urgency referrals score higher' },
  weight_waittime: { label: 'Wait Time Weight',   color: 'from-blue-500 to-purple-500', desc: 'Longer-waiting referrals score higher' },
  weight_patient:  { label: 'Patient Age Weight', color: 'from-emerald-500 to-teal-500', desc: 'Older patients receive a higher score' },
};

export default function ConfigsPage() {
  const { user } = useAuth();
  const router = useRouter();
  const [weights, setWeights] = useState<Record<string, number>>({
    weight_urgency: 50, weight_waittime: 30, weight_patient: 20
  });
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [toast, setToast] = useState<{ type: 'success' | 'error'; msg: string } | null>(null);

  useEffect(() => {
    if (!user) { router.push('/login'); return; }
    if (!['Admin', 'TriageNurse'].includes(user.role)) { router.push('/dashboard'); return; }

    fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/config/weights`, {
      headers: { Authorization: `Bearer ${user.token}` }
    })
      .then(r => r.json())
      .then((data: Weight[]) => {
        const map: Record<string, number> = {};
        data.forEach(d => { map[d.key] = parseFloat(d.value); });
        setWeights(map);
      })
      .finally(() => setLoading(false));
  }, [user, router]);

  const total = Object.values(weights).reduce((a, b) => a + b, 0);
  const isValid = Math.abs(total - 100) < 0.01;

  const handleChange = (key: string, val: number) => {
    setWeights(prev => ({ ...prev, [key]: val }));
  };

  const handleSave = async () => {
    if (!user || !isValid) return;
    setSaving(true);
    try {
      const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/config/weights`, {
        method: 'POST',
        headers: { Authorization: `Bearer ${user.token}`, 'Content-Type': 'application/json' },
        body: JSON.stringify({
          weightUrgency: weights.weight_urgency,
          weightWaittime: weights.weight_waittime,
          weightPatient: weights.weight_patient,
        }),
      });
      const data = await res.json();
      if (res.ok) setToast({ type: 'success', msg: data.message });
      else setToast({ type: 'error', msg: data.message });
    } catch {
      setToast({ type: 'error', msg: 'Failed to save configuration' });
    } finally {
      setSaving(false);
      setTimeout(() => setToast(null), 4000);
    }
  };

  if (!user) return null;

  return (
    <div className="min-h-screen bg-gray-950 p-6">
      <div className="max-w-2xl mx-auto">
        {/* Header */}
        <div className="flex items-center gap-3 mb-8">
          <div className="w-10 h-10 rounded-xl bg-blue-500/10 border border-blue-500/20 flex items-center justify-center">
            <Settings className="w-5 h-5 text-blue-400" />
          </div>
          <div>
            <h1 className="text-xl font-bold text-white">Priority Formula Configuration</h1>
            <p className="text-gray-400 text-sm">Adjust weights to reorder the live queue in real-time</p>
          </div>
        </div>

        {/* Info Banner */}
        <div className="flex items-start gap-3 bg-blue-500/10 border border-blue-500/20 rounded-xl p-4 mb-6">
          <Info className="w-4 h-4 text-blue-400 shrink-0 mt-0.5" />
          <p className="text-blue-300 text-sm">
            Weights must sum to exactly 100%. Saving will immediately recalculate all active referral scores and 
            push the re-sorted queue to all connected sessions via SignalR.
          </p>
        </div>

        {/* Formula Preview */}
        <div className="bg-gray-900/60 border border-white/10 rounded-xl p-5 mb-6">
          <p className="text-xs text-gray-500 uppercase tracking-wider mb-2">Priority Formula</p>
          <code className="text-blue-300 text-sm">
            Score = ({weights.weight_urgency}% × Urgency) + ({weights.weight_waittime}% × WaitTime) + ({weights.weight_patient}% × PatientAge)
          </code>
        </div>

        {/* Sliders */}
        {loading ? (
          <div className="flex justify-center py-16"><Loader2 className="w-6 h-6 animate-spin text-blue-400" /></div>
        ) : (
          <div className="space-y-6 mb-8">
            {Object.entries(keyLabels).map(([key, meta]) => (
              <div key={key} className="bg-gray-900/60 border border-white/10 rounded-xl p-5">
                <div className="flex items-center justify-between mb-4">
                  <div>
                    <p className="text-white font-semibold text-sm">{meta.label}</p>
                    <p className="text-gray-400 text-xs mt-0.5">{meta.desc}</p>
                  </div>
                  <div className="text-right">
                    <span className={`text-2xl font-bold bg-gradient-to-r ${meta.color} bg-clip-text text-transparent`}>
                      {weights[key]}%
                    </span>
                  </div>
                </div>
                <div className="relative">
                  <input
                    type="range"
                    min={0}
                    max={100}
                    step={5}
                    value={weights[key]}
                    onChange={e => handleChange(key, parseInt(e.target.value))}
                    className="w-full h-2 rounded-full appearance-none cursor-pointer bg-gray-700 accent-blue-500"
                  />
                  <div className="flex justify-between text-xs text-gray-600 mt-1">
                    <span>0%</span><span>50%</span><span>100%</span>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}

        {/* Total indicator */}
        <div className={`flex items-center justify-between p-4 rounded-xl mb-6 border ${
          isValid
            ? 'bg-emerald-500/10 border-emerald-500/30'
            : 'bg-red-500/10 border-red-500/30'
        }`}>
          <div className="flex items-center gap-2">
            {isValid
              ? <CheckCircle2 className="w-4 h-4 text-emerald-400" />
              : <AlertCircle className="w-4 h-4 text-red-400" />}
            <span className={`text-sm font-medium ${isValid ? 'text-emerald-400' : 'text-red-400'}`}>
              {isValid ? 'Weights are valid (sum = 100%)' : `Weights sum to ${total}% — must equal 100%`}
            </span>
          </div>
          <span className={`text-xl font-bold ${isValid ? 'text-emerald-400' : 'text-red-400'}`}>{total}%</span>
        </div>

        {/* Toast */}
        {toast && (
          <div className={`flex items-center gap-2 p-4 rounded-xl mb-4 border text-sm ${
            toast.type === 'success'
              ? 'bg-emerald-900/60 border-emerald-500/40 text-emerald-300'
              : 'bg-red-900/60 border-red-500/40 text-red-300'
          }`}>
            {toast.type === 'success' ? <CheckCircle2 className="w-4 h-4" /> : <AlertCircle className="w-4 h-4" />}
            {toast.msg}
          </div>
        )}

        {/* Save Button */}
        <button
          onClick={handleSave}
          disabled={!isValid || saving}
          className="w-full py-3 px-6 bg-gradient-to-r from-blue-600 to-purple-600 hover:from-blue-500 hover:to-purple-500 text-white font-semibold rounded-xl shadow-lg shadow-blue-500/25 transition-all disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2"
        >
          {saving ? <Loader2 className="w-4 h-4 animate-spin" /> : <Save className="w-4 h-4" />}
          {saving ? 'Saving & Broadcasting...' : 'Save & Apply Weights'}
        </button>
      </div>
    </div>
  );
}
