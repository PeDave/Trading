import { NavLink } from 'react-router-dom';
import { useAuthStore } from '@/stores/authStore';
import {
  LayoutDashboard,
  BarChart2,
  TrendingUp,
  Image,
  BookOpen,
  ShieldCheck,
  User,
  DollarSign,
} from 'lucide-react';
import { clsx } from 'clsx';

interface NavItem {
  to: string;
  label: string;
  icon: React.ReactNode;
  adminOnly?: boolean;
}

const navItems: NavItem[] = [
  { to: '/', label: 'Dashboard', icon: <LayoutDashboard size={18} /> },
  { to: '/market', label: 'Market Overview', icon: <BarChart2 size={18} /> },
  { to: '/signals', label: 'Trading Signals', icon: <TrendingUp size={18} /> },
  { to: '/charts', label: 'Chart Analysis', icon: <Image size={18} /> },
  { to: '/playbooks', label: 'Playbooks', icon: <BookOpen size={18} /> },
  { to: '/admin', label: 'Admin Dashboard', icon: <ShieldCheck size={18} />, adminOnly: true },
  { to: '/profile', label: 'Profile', icon: <User size={18} /> },
  { to: '/pricing', label: 'Pricing', icon: <DollarSign size={18} /> },
];

export default function Sidebar() {
  const isAdmin = useAuthStore((s) => s.isAdmin);

  return (
    <aside className="w-56 bg-gray-900 border-r border-gray-800 flex flex-col py-4 shrink-0">
      <nav className="flex flex-col gap-0.5 px-2">
        {navItems
          .filter((item) => !item.adminOnly || isAdmin)
          .map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.to === '/'}
              className={({ isActive }) =>
                clsx(
                  'flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm transition-colors',
                  isActive
                    ? 'bg-blue-600/20 text-blue-400 font-medium'
                    : 'text-gray-400 hover:text-gray-100 hover:bg-gray-800'
                )
              }
            >
              {item.icon}
              {item.label}
            </NavLink>
          ))}
      </nav>
    </aside>
  );
}
