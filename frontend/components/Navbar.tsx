'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { useAuth } from '@/context/AuthContext';
import { 
  LayoutDashboard, Settings, MessageSquare, Users, 
  LogOut, ChevronDown, Activity, Bell, ShieldCheck, Key
} from 'lucide-react';
import { useState, useEffect } from 'react';

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
  {
    label: 'Menu Access',
    href: '/menu-access',
    icon: Key,
    roles: ['Admin'],
  },
];

interface MenuAccessDto {
  role: number | string;
  menuItem: string;
  hasAccess: boolean;
}

const roleNameMap: Record<number | string, string> = {
  1: 'Admin',
  2: 'TriageNurse',
  3: 'GP',
  'Admin': 'Admin',
  'TriageNurse': 'TriageNurse',
  'GP': 'GP'
};

export default function Navbar() {
  const { user, logout } = useAuth();
  const pathname = usePathname();
  const [profileOpen, setProfileOpen] = useState(false);
  const [menuAccesses, setMenuAccesses] = useState<MenuAccessDto[]>([]);

  useEffect(() => {
    if (!user) return;
    fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/menuaccess`, {
      headers: { Authorization: `Bearer ${user.token}` }
    })
      .then(res => res.json())
      .then((data: MenuAccessDto[]) => setMenuAccesses(data))
      .catch(() => {});
  }, [user]);

  if (!user) return null;

  // Filter navigation items dynamically based on the DB configuration
  const filteredNav = navItems.filter(item => {
    const rolesList = user.roles || [];
    if (menuAccesses.length > 0) {
      return rolesList.some(uRole => {
        return menuAccesses.some(ma => {
          const mappedRole = roleNameMap[ma.role];
          return mappedRole === uRole && ma.menuItem === item.label && ma.hasAccess;
        });
      });
    }
    // Fallback: check hardcoded roles
    return item.roles.some(r => rolesList.includes(r));
  });

  const getRoleLabel = (roles: string[]) => {
    let rolesList = roles || [];
    if (rolesList.length === 0 && user && (user as any).role) {
      rolesList = [(user as any).role];
    }
    return rolesList
      .map(r => r === 'TriageNurse' ? 'Triage Nurse' : r)
      .join(', ');
  };

  return (
    <nav className="fixed top-0 left-0 right-0 z-50 bg-white/95 backdrop-blur-xl border-b border-slate-200/80 shadow-sm">
      <div className="max-w-[1600px] mx-auto px-4 sm:px-6">
        <div className="flex items-center justify-between h-16">
          {/* Logo */}
          <div className="flex items-center gap-3">
            <div className="w-8 h-8 rounded-lg bg-gradient-to-br from-blue-600 to-indigo-600 flex items-center justify-center shadow-md shadow-blue-500/20">
              <Activity className="w-4 h-4 text-white" />
            </div>
            <div>
              <span className="text-slate-900 font-bold text-lg tracking-tight">Refer</span>
              <span className="text-blue-600 font-bold text-lg tracking-tight">Well</span>
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
                      ? 'bg-blue-50 text-blue-600 border border-blue-100 shadow-sm'
                      : 'text-slate-600 hover:text-slate-900 hover:bg-slate-50'
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
            <button className="relative p-2 text-slate-500 hover:text-slate-800 hover:bg-slate-50 rounded-lg transition-all">
              <Bell className="w-5 h-5" />
              <span className="absolute top-1.5 right-1.5 w-2.5 h-2.5 bg-blue-600 border-2 border-white rounded-full"></span>
            </button>

            {/* Profile Dropdown */}
            <div className="relative">
              <button
                onClick={() => setProfileOpen(!profileOpen)}
                className="flex items-center gap-3 px-3 py-2 rounded-lg hover:bg-slate-50 border border-transparent hover:border-slate-100 transition-all text-sm text-slate-700"
              >
                <div className="w-8 h-8 rounded-full bg-gradient-to-br from-blue-500 to-indigo-600 flex items-center justify-center text-white font-semibold text-xs shadow-sm">
                  {user.fullName.split(' ').map(n => n[0]).join('').slice(0, 2).toUpperCase()}
                </div>
                <div className="hidden sm:block text-left">
                  <p className="text-slate-900 text-sm font-semibold leading-tight">{user.title ? user.title + ' ' : ''}{user.fullName}</p>
                  <span className="inline-block text-[10px] font-semibold text-blue-600 bg-blue-50 px-2 py-0.5 rounded-full mt-0.5">
                    {getRoleLabel(user.roles)}
                  </span>
                </div>
                <ChevronDown className={`w-4 h-4 text-slate-500 transition-transform ${profileOpen ? 'rotate-180' : ''}`} />
              </button>

              {profileOpen && (
                <div className="absolute right-0 mt-2 w-52 bg-white border border-slate-200 rounded-xl shadow-lg py-1 z-50 animate-in slide-in-from-right">
                  <div className="px-4 py-3 border-b border-slate-100">
                    <p className="text-slate-900 text-sm font-semibold">{user.title ? user.title + ' ' : ''}{user.fullName}</p>
                    <p className="text-slate-500 text-xs truncate mt-0.5">{user.email}</p>
                  </div>
                  <button
                    onClick={() => { setProfileOpen(false); logout(); }}
                    className="w-full flex items-center gap-2 px-4 py-2.5 text-sm text-red-600 hover:bg-red-50 transition-all font-medium"
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
