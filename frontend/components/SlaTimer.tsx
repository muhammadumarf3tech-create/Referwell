'use client';

import { useEffect, useState } from 'react';
import { Clock, AlertTriangle, CheckCircle } from 'lucide-react';

interface SlaTimerProps {
  deadline: string;
  status: string;
}

export default function SlaTimer({ deadline, status }: SlaTimerProps) {
  const [remaining, setRemaining] = useState('');
  const [isBreached, setIsBreached] = useState(false);
  const [isWarning, setIsWarning] = useState(false);

  useEffect(() => {
    if (['Completed', 'Declined'].includes(status)) return;

    const update = () => {
      const diff = new Date(deadline).getTime() - Date.now();
      setIsBreached(diff < 0);

      const absDiff = Math.abs(diff);
      const h = Math.floor(absDiff / 3600000);
      const m = Math.floor((absDiff % 3600000) / 60000);
      const s = Math.floor((absDiff % 60000) / 1000);

      setIsWarning(!isBreached && diff < 4 * 3600000); // warn if < 4 hours

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
  }, [deadline, status, isBreached]);

  if (['Completed', 'Declined'].includes(status)) {
    return (
      <div className="flex items-center gap-1.5 text-gray-500 text-xs">
        <CheckCircle className="w-3.5 h-3.5" />
        <span>Closed</span>
      </div>
    );
  }

  if (isBreached) {
    return (
      <div className="flex items-center gap-1.5 text-red-400 text-xs font-semibold animate-pulse">
        <AlertTriangle className="w-3.5 h-3.5" />
        <span>{remaining}</span>
      </div>
    );
  }

  if (isWarning) {
    return (
      <div className="flex items-center gap-1.5 text-amber-400 text-xs font-medium">
        <Clock className="w-3.5 h-3.5 animate-spin" style={{ animationDuration: '3s' }} />
        <span>{remaining}</span>
      </div>
    );
  }

  return (
    <div className="flex items-center gap-1.5 text-emerald-400 text-xs">
      <Clock className="w-3.5 h-3.5" />
      <span>{remaining}</span>
    </div>
  );
}
