import { Link, useNavigate } from 'react-router-dom';
import { useAuthStore } from '@/stores/authStore';
import { SubscriptionTier } from '@/types';
import TierBadge from '@/components/subscription/TierBadge';
import { LogOut, User, ChevronDown } from 'lucide-react';
import { useState, useRef, useEffect } from 'react';

export default function Navbar() {
  const { isAuthenticated, user, logout } = useAuthStore();
  const navigate = useNavigate();
  const [menuOpen, setMenuOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) {
        setMenuOpen(false);
      }
    };
    document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, []);

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <header className="h-14 bg-gray-900 border-b border-gray-800 flex items-center px-4 gap-4 z-50 relative">
      <Link to="/" className="flex items-center gap-2 font-bold text-lg text-blue-400 shrink-0">
        <span className="text-blue-500">⚡</span>
        LaBot Kripto
      </Link>

      <nav className="hidden md:flex items-center gap-1 ml-4">
        <Link to="/" className="px-3 py-1.5 text-sm text-gray-400 hover:text-gray-100 hover:bg-gray-800 rounded-lg transition-colors">
          Dashboard
        </Link>
        <Link to="/market" className="px-3 py-1.5 text-sm text-gray-400 hover:text-gray-100 hover:bg-gray-800 rounded-lg transition-colors">
          Market
        </Link>
        <Link to="/signals" className="px-3 py-1.5 text-sm text-gray-400 hover:text-gray-100 hover:bg-gray-800 rounded-lg transition-colors">
          Signals
        </Link>
        <Link to="/pricing" className="px-3 py-1.5 text-sm text-gray-400 hover:text-gray-100 hover:bg-gray-800 rounded-lg transition-colors">
          Pricing
        </Link>
      </nav>

      <div className="ml-auto flex items-center gap-3">
        {isAuthenticated && user ? (
          <div className="relative" ref={menuRef}>
            <button
              onClick={() => setMenuOpen((o) => !o)}
              className="flex items-center gap-2 px-3 py-1.5 bg-gray-800 hover:bg-gray-700 rounded-lg transition-colors text-sm"
            >
              <User size={16} className="text-gray-400" />
              <span className="hidden sm:inline text-gray-200">{user.userName}</span>
              <TierBadge tier={user.subscriptionTier} small />
              <ChevronDown size={14} className="text-gray-400" />
            </button>
            {menuOpen && (
              <div className="absolute right-0 mt-1 w-48 bg-gray-800 border border-gray-700 rounded-lg shadow-lg py-1 z-50">
                <Link
                  to="/profile"
                  className="flex items-center gap-2 px-4 py-2 text-sm text-gray-300 hover:text-gray-100 hover:bg-gray-700"
                  onClick={() => setMenuOpen(false)}
                >
                  <User size={14} /> Profile
                </Link>
                {user.role === 'Admin' && (
                  <Link
                    to="/admin"
                    className="flex items-center gap-2 px-4 py-2 text-sm text-yellow-400 hover:bg-gray-700"
                    onClick={() => setMenuOpen(false)}
                  >
                    ⚙ Admin
                  </Link>
                )}
                <hr className="border-gray-700 my-1" />
                <button
                  onClick={handleLogout}
                  className="flex items-center gap-2 px-4 py-2 text-sm text-red-400 hover:bg-gray-700 w-full text-left"
                >
                  <LogOut size={14} /> Logout
                </button>
              </div>
            )}
          </div>
        ) : (
          <div className="flex items-center gap-2">
            <Link to="/login" className="btn-secondary text-sm py-1.5 px-3">
              Login
            </Link>
            <Link to="/register" className="btn-primary text-sm py-1.5 px-3">
              Register
            </Link>
          </div>
        )}
      </div>
    </header>
  );
}

export { SubscriptionTier };
