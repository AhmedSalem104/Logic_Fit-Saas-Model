import { useState, type ReactNode } from 'react';
import { NavLink } from 'react-router-dom';
import { Button, Drawer } from './ui';

const navigation = [
  { label: 'الأساس التقني', to: '/' },
  { label: 'تسجيل الدخول', to: '/login' },
];

export function AppShell({ children, theme, onToggleTheme }: { children: ReactNode; theme: 'light' | 'dark'; onToggleTheme: () => void }) {
  const [menuOpen, setMenuOpen] = useState(false);
  const menu = <nav className="lf-nav" aria-label="التنقل الرئيسي">{navigation.map((item) => <NavLink key={item.to} to={item.to} onClick={() => setMenuOpen(false)} className={({ isActive }) => `lf-nav-link ${isActive ? 'is-active' : ''}`}>{item.label}</NavLink>)}</nav>;

  return <div className="lf-app-shell">
    <aside className="lf-sidebar"><div className="lf-brand"><span className="lf-brand-mark">L</span><span>LogicFit</span></div>{menu}</aside>
    <Drawer open={menuOpen} title="القائمة" onClose={() => setMenuOpen(false)}>{menu}</Drawer>
    <div className="lf-main">
      <header className="lf-header"><Button variant="ghost" className="lf-mobile-menu" aria-label="فتح القائمة" onClick={() => setMenuOpen(true)}>☰</Button><div><span className="lf-eyebrow">LOCAL FOUNDATION</span><strong>منصة LogicFit</strong></div><div className="lf-header-actions"><span className="lf-theme-label">{theme === 'dark' ? 'داكن' : 'فاتح'}</span><Button variant="secondary" onClick={onToggleTheme} aria-label="تبديل المظهر">◐</Button></div></header>
      <main className="lf-content">{children}</main>
    </div>
  </div>;
}
