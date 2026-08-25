import { createContext, useContext, useState, type ButtonHTMLAttributes, type InputHTMLAttributes, type ReactNode, type SelectHTMLAttributes } from 'react';

export function Button({ variant = 'primary', className = '', ...props }: ButtonHTMLAttributes<HTMLButtonElement> & { variant?: 'primary' | 'secondary' | 'ghost' | 'danger' }) {
  return <button className={`lf-button lf-button-${variant} ${className}`} {...props} />;
}

export function TextInput({ label, error, ...props }: InputHTMLAttributes<HTMLInputElement> & { label?: string; error?: string }) {
  return <label className="lf-field"><span>{label}</span><input className="lf-input" {...props} />{error ? <small className="lf-error-text">{error}</small> : null}</label>;
}

export function SelectField({ label, children, ...props }: SelectHTMLAttributes<HTMLSelectElement> & { label?: string; children: ReactNode }) {
  return <label className="lf-field"><span>{label}</span><select className="lf-input" {...props}>{children}</select></label>;
}

export function Card({ title, children, className = '' }: { title?: string; children: ReactNode; className?: string }) {
  return <section className={`lf-card ${className}`}>{title ? <h2 className="lf-card-title">{title}</h2> : null}{children}</section>;
}

export function Table({ headers, rows }: { headers: string[]; rows: ReactNode[][] }) {
  return <div className="lf-table-wrap"><table className="lf-table"><thead><tr>{headers.map((header) => <th key={header}>{header}</th>)}</tr></thead><tbody>{rows.map((row, index) => <tr key={index}>{row.map((cell, cellIndex) => <td key={cellIndex}>{cell}</td>)}</tr>)}</tbody></table></div>;
}

export function Modal({ open, title, children, onClose }: { open: boolean; title: string; children: ReactNode; onClose: () => void }) {
  if (!open) return null;
  return <div className="lf-overlay" role="presentation" onClick={onClose}><div className="lf-modal" role="dialog" aria-modal="true" aria-label={title} onClick={(event) => event.stopPropagation()}><div className="lf-modal-header"><h2>{title}</h2><Button variant="ghost" aria-label="إغلاق" onClick={onClose}>×</Button></div>{children}</div></div>;
}

export function Drawer({ open, title, children, onClose }: { open: boolean; title: string; children: ReactNode; onClose: () => void }) {
  if (!open) return null;
  return <div className="lf-overlay" role="presentation" onClick={onClose}><aside className="lf-drawer" role="dialog" aria-label={title} onClick={(event) => event.stopPropagation()}><div className="lf-modal-header"><h2>{title}</h2><Button variant="ghost" onClick={onClose}>×</Button></div>{children}</aside></div>;
}

export function LoadingState({ label = 'جارٍ التحميل…' }: { label?: string }) { return <div className="lf-state" role="status"><span className="lf-spinner" />{label}</div>; }
export function EmptyState({ title = 'لا توجد بيانات', message = 'ستظهر البيانات هنا عند توفرها.' }: { title?: string; message?: string }) { return <div className="lf-state"><strong>{title}</strong><span>{message}</span></div>; }
export function ErrorState({ message = 'حدث خطأ غير متوقع.' }: { message?: string }) { return <div className="lf-state lf-state-error" role="alert"><strong>تعذر إكمال الطلب</strong><span>{message}</span></div>; }

type Toast = { id: number; message: string; tone: 'info' | 'success' | 'error' };
const ToastContext = createContext<{ push: (message: string, tone?: Toast['tone']) => void } | null>(null);

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([]);
  const push = (message: string, tone: Toast['tone'] = 'info') => {
    const id = Date.now() + Math.random();
    setToasts((current) => [...current, { id, message, tone }]);
    window.setTimeout(() => setToasts((current) => current.filter((toast) => toast.id !== id)), 4000);
  };
  return <ToastContext.Provider value={{ push }}>{children}<div className="lf-toast-viewport" aria-live="polite">{toasts.map((toast) => <div key={toast.id} className={`lf-toast lf-toast-${toast.tone}`}>{toast.message}</div>)}</div></ToastContext.Provider>;
}

export function useToast() {
  const context = useContext(ToastContext);
  if (!context) throw new Error('useToast must be used inside ToastProvider.');
  return context;
}
