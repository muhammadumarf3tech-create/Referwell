'use client';

import { useEffect, useState } from 'react';
import { Clock, AlertTriangle, CheckCircle, PauseCircle } from 'lucide-react';

interface SlaTimerProps {
  deadline: string;
  status: string;
  compact?: boolean;
  paused?: boolean;
  pauseReason?: string | null;
}

export default function SlaTimer({ deadline, status, compact, paused, pauseReason }: SlaTimerProps) {
  const [remaining, setRemaining] = useState('');
  const [isBreached, setIsBreached] = useState(false);
  const [isWarning, setIsWarning] = useState(false);

  useEffect(() => {
    if (['Completed', 'Declined'].includes(status) || paused) return;

    const update = () => {
      const diff = new Date(deadline).getTime() - Date.now();
      const breached = diff < 0;
      setIsBreached(breached);
      setIsWarning(!breached && diff < 4 * 3600000);

      const absDiff = Math.abs(diff);
      const h = Math.floor(absDiff / 3600000);
      const m = Math.floor((absDiff % 3600000) / 60000);
      const s = Math.floor((absDiff % 60000) / 1000);

      if (diff < 0) {
        setRemaining(`${h}h ${m}m overdue`);
      } else if (h > 24) {
        const d = Math.floor(h / 24);
        setRemaining(`${d}d ${h % 24}h left`);
      } else {
        setRemaining(`${h}h ${m}m ${s}s`);
      }
    };

    update();
    const interval = setInterval(update, 1000);
    return () => clearInterval(interval);
  }, [deadline, status, paused]);

  if (['Completed', 'Declined'].includes(status)) {
    return (
      <div className="flex items-center gap-1.5 text-slate-500 text-xs font-semibold">
        <CheckCircle className="w-3.5 h-3.5" />
        <span>Closed</span>
      </div>
    );
  }

  if (paused) {
    const label = pauseReason === 'WaitingOnPatient' || !pauseReason
      ? 'SLA paused — waiting on patient'
      : `SLA paused — ${pauseReason}`;
    return (
      <div className={`flex items-center gap-1.5 text-violet-700 text-xs font-bold ${compact ? '' : ''}`}>
        <PauseCircle className="w-3.5 h-3.5 shrink-0" />
        <span>{compact ? 'Paused' : label}</span>
      </div>
    );
  }

  if (isBreached) {
    return (
      <div className={`flex items-center gap-1.5 text-red-600 text-xs font-bold animate-pulse ${compact ? '' : ''}`}>
        <AlertTriangle className="w-3.5 h-3.5 shrink-0" />
        <span>{remaining}</span>
      </div>
    );
  }

  if (isWarning) {
    return (
      <div className="flex items-center gap-1.5 text-amber-600 text-xs font-bold">
        <Clock className="w-3.5 h-3.5 shrink-0" style={{ animation: 'spin 3s linear infinite' }} />
        <span>{remaining}</span>
      </div>
    );
  }

  return (
    <div className="flex items-center gap-1.5 text-emerald-600 text-xs font-semibold">
      <Clock className="w-3.5 h-3.5 shrink-0" />
      <span>{remaining}</span>
    </div>
  );
}
