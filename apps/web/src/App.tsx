import { useEffect, useState } from 'react';
import { QueryClientProvider, useQuery } from '@tanstack/react-query';
import { BrowserRouter, Route, Routes } from 'react-router-dom';
import { queryClient } from './providers/query-client';
import { apiClient } from './lib/api';
import { AppShell } from './components/AppShell';
import { ErrorBoundary } from './components/ErrorBoundary';
import { Card, EmptyState, ErrorState, LoadingState, Table, ToastProvider } from './components/ui';
import './styles/index.css';

function FoundationHome() {
  const health = useQuery({ queryKey: ['foundation', 'health'], queryFn: apiClient.health });
  const readiness = useQuery({ queryKey: ['foundation', 'readiness'], queryFn: apiClient.readiness, retry: false });
  if (health.isPending) return <LoadingState />;
  if (health.isError) return <ErrorState message="تعذر الوصول إلى API المحلي. شغّل API ثم أعد المحاولة." />;
  return <div className="lf-page-stack"><div className="lf-page-heading"><div><span className="lf-eyebrow">PHASE 4</span><h1>الأساس التقني المحلي</h1><p>واجهة تحقق صغيرة للبنية المشتركة فقط. لا توجد Business Modules في هذه المرحلة.</p></div><span className="lf-status-pill lf-status-success">API يعمل</span></div><div className="lf-grid-3"><Card title="Health"><div className="lf-metric">{health.data.data.status}</div><span className="lf-muted">{health.data.data.service}</span></Card><Card title="Readiness">{readiness.isPending ? <LoadingState label="فحص قاعدة البيانات…" /> : readiness.isError ? <ErrorState message="قاعدة البيانات غير متاحة." /> : <><div className="lf-metric">{readiness.data.data.status}</div><span className="lf-muted">{readiness.data.data.database}</span></>}</Card><Card title="Environment"><div className="lf-metric">{health.data.data.environment}</div><span className="lf-muted">API {health.data.data.version}</span></Card></div><Card title="Foundation contracts"><Table headers={['الطبقة', 'الحالة', 'النطاق']} rows={[[<strong key="api">REST API</strong>, <span key="a" className="lf-status-pill lf-status-success">جاهز</span>, 'Health / Readiness / Version'], [<strong key="db">SQL Server</strong>, <span key="b" className="lf-status-pill lf-status-neutral">Foundation</span>, 'Control Plane + Gym DB'], [<strong key="clients">Clients</strong>, <span key="c" className="lf-status-pill lf-status-neutral">Foundation</span>, 'React Web + Flutter']]} /></Card><EmptyState title="لا توجد وحدات أعمال بعد" message="Members وTraining وNutrition وFinance وغيرها تبدأ في الـvertical slices التالية." /></div>;
}

function NotFound() { return <ErrorState message="الصفحة غير موجودة ضمن Foundation." />; }

export default function App() {
  const [theme, setTheme] = useState<'light' | 'dark'>(() => (localStorage.getItem('logicfit-theme') as 'light' | 'dark' | null) ?? 'light');
  useEffect(() => { document.documentElement.dir = 'rtl'; document.documentElement.lang = 'ar'; document.documentElement.dataset.theme = theme; document.documentElement.classList.toggle('dark', theme === 'dark'); localStorage.setItem('logicfit-theme', theme); }, [theme]);
  return <ErrorBoundary><QueryClientProvider client={queryClient}><ToastProvider><BrowserRouter><AppShell theme={theme} onToggleTheme={() => setTheme((current) => current === 'dark' ? 'light' : 'dark')}><Routes><Route path="/" element={<FoundationHome />} /><Route path="*" element={<NotFound />} /></Routes></AppShell></BrowserRouter></ToastProvider></QueryClientProvider></ErrorBoundary>;
}
