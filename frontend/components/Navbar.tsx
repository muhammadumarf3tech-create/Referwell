'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { useAuth } from '@/context/AuthContext';
import { 
  LayoutDashboard, Settings, MessageSquare, Users, 
  LogOut, ChevronDown, Activity, Bell
} from 'lucide-react';
import { useState } from 'react';

const navItems = [
  {
    label: 'Dashboard',
    href: '/dashboard',
    icon: LayoutDashboard,
    roles: ['Admin', 'TriageNurse', 'GP'],
  },
  {
    label: 'Priority Config',
    href: '/configs',
    icon: Settings,
    roles: ['Admin', 'TriageNurse'],
  },
  {
    label: 'Mass Communications',
    href: '/mass-comm',
    icon: MessageSquare,
    roles: ['Admin', 'TriageNurse'],
  },
  {
    label: 'User Management',
    href: '/users',
    icon: Users,
    roles: ['Admin'],
  },
];

export default function Navbar() {
  const { user, logout } = useAuth();
  const pathname = usePathname();
  const [profileOpen, setProfileOpen] = useState(false);

  if (!user) return null;

  const filteredNav = navItems.filter(item => item.roles.includes(user.role));

  const roleColors: Record<string, string> = {
    Admin: 'bg-purple-500/20 text-purple-300 border border-purple-500/30',
    TriageNurse: 'bg-blue-500/20 text-blue-300 border border-blue-500/30',
    GP: 'bg-emerald-500/20 text-emerald-300 border border-emerald-500/30',
  };

  const roleBadge = roleColors[user.role] || 'bg-gray-500/20 text-gray-300';

  return (
    <nav className="fixed top-0 left-0 right-0 z-50 bg-gray-900/95 backdrop-blur-xl border-b border-white/10 shadow-2xl">
      <div className="max-w-[1600px] mx-auto px-4 sm:px-6">
        <div className="flex items-center justify-between h-16">
          {/* Logo */}
          <div className="flex items-center gap-3">
            <div className="w-8 h-8 rounded-lg bg-gradient-to-br from-blue-500 to-purple-600 flex items-center justify-center shadow-lg shadow-blue-500/30">
              <Activity className="w-4 h-4 text-white" />
            </div>
            <div>
              <span className="text-white font-bold text-lg tracking-tight">Refer</span>
              <span className="text-blue-400 font-bold text-lg tracking-tight">Well</span>
            </div>
          </div>

          {/* Navigation Links */}
          <div className="hidden md:flex items-center gap-1">
            {filteredNav.map((item) => {
              const Icon = item.icon;
              const isActive = pathname === item.href || pathname.startsWith(item.href + '/');
              return (
                <Link
                  key={item.href}
                  href={item.href}
                  className={`flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-medium transition-all duration-200 ${
                    isActive
                      ? 'bg-blue-600/20 text-blue-400 border border-blue-500/30 shadow-lg shadow-blue-500/10'
                      : 'text-gray-400 hover:text-white hover:bg-white/5'
                  }`}
                >
                  <Icon className="w-4 h-4" />
                  {item.label}
                </Link>
              );
            })}
          </div>

          {/* Right Side */}
          <div className="flex items-center gap-3">
            {/* Notification Bell */}
            <button className="relative p-2 text-gray-400 hover:text-white hover:bg-white/5 rounded-lg transition-all">
              <Bell className="w-5 h-5" />
              <span className="absolute top-1 right-1 w-2 h-2 bg-blue-500 rounded-full"></span>
            </button>

            {/* Profile Dropdown */}
            <div className="relative">
              <button
                onClick={() => setProfileOpen(!profileOpen)}
                className="flex items-center gap-3 px-3 py-2 rounded-lg hover:bg-white/5 transition-all text-sm"
              >
                <div className="w-8 h-8 rounded-full bg-gradient-to-br from-blue-500 to-purple-600 flex items-center justify-center text-white font-semibold text-xs shadow-lg">
                  {user.fullName.split(' ').map(n => n[0]).join('').slice(0, 2).toUpperCase()}
                </div>
                <div className="hidden sm:block text-left">
                  <p className="text-white text-sm font-medium leading-tight">{user.fullName}</p>
                  <span className={`inline-block text-xs px-2 py-0.5 rounded-full font-medium ${roleBadge}`}>
                    {user.role === 'TriageNurse' ? 'Triage Nurse' : user.role}
                  </span>
                </div>
                <ChevronDown className={`w-4 h-4 text-gray-400 transition-transform ${profileOpen ? 'rotate-180' : ''}`} />
              </button>

              {profileOpen && (
                <div className="absolute right-0 mt-2 w-48 bg-gray-800/95 backdrop-blur border border-white/10 rounded-xl shadow-2xl py-1 z-50">
                  <div className="px-4 py-3 border-b border-white/10">
                    <p className="text-white text-sm font-medium">{user.fullName}</p>
                    <p className="text-gray-400 text-xs truncate">{user.email}</p>
                  </div>
                  <button
                    onClick={() => { setProfileOpen(false); logout(); }}
                    className="w-full flex items-center gap-2 px-4 py-2 text-sm text-red-400 hover:bg-red-500/10 hover:text-red-300 transition-all"
                  >
                    <LogOut className="w-4 h-4" />
                    Sign Out
                  </button>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </nav>
  );
}
