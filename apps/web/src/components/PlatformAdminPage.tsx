import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import {
  ApiClientError,
  apiClient,
  type DatabaseRegistrySummary,
  type GymSummary,
  type OrganizationSummary,
  type PlatformStatusCount,
} from '../lib/api';
import { useAuth } from '../lib/auth';
import { Button, Card, EmptyState, ErrorState, LoadingState, SelectField, Table, TextInput } from './ui';

function safeError(error: unknown) {
  return error instanceof ApiClientError
    ? error.message
    : 'تعذر تحميل بيانات المنصة. حاول مرة أخرى.';
}

function formatDate(value: string | null) {
  if (!value) return '—';
  return new Intl.DateTimeFormat('ar-EG', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value));
}

function statusClass(status: string) {
  return ['active', 'healthy', 'ready', 'connected', 'completed'].includes(status.toLowerCase())
    ? 'lf-status-success'
    : 'lf-status-neutral';
}

function StatusBadge({ value }: { value: string }) {
  return <span className={`lf-status-pill ${statusClass(value)}`}>{value}</span>;
}

function PlatformAccessNotice() {
  return <Card title="إدارة المنصة">
    <div className="lf-form-error" role="alert">لا تملك صلاحية platform.view لعرض بيانات المنصة.</div>
  </Card>;
}

export function PlatformAdminPage({ children }: { children: React.ReactNode }) {
  const { me } = useAuth();
  if (!me?.permissions.includes('platform.view')) return <div className="lf-page-stack"><PlatformAccessNotice /></div>;
  return <div className="lf-page-stack">{children}</div>;
}

function PlatformHeading({ eyebrow, title, description, onRefresh }: { eyebrow: string; title: string; description: string; onRefresh?: () => void }) {
  return <div className="lf-page-heading">
    <div><span className="lf-eyebrow">{eyebrow}</span><h1>{title}</h1><p>{description}</p></div>
    {onRefresh ? <Button variant="secondary" onClick={onRefresh}>تحديث البيانات</Button> : null}
  </div>;
}

function MetricCard({ label, value, detail }: { label: string; value: string | number; detail?: string }) {
  return <Card><span className="lf-eyebrow">{label}</span><div className="lf-metric">{value}</div>{detail ? <span className="lf-muted">{detail}</span> : null}</Card>;
}

function StatusBreakdown({ items }: { items: PlatformStatusCount[] }) {
  if (items.length === 0) return <span className="lf-muted">لا توجد حالات مسجلة</span>;
  return <div className="lf-status-breakdown">{items.map((item) => <div className="lf-status-breakdown-item" key={item.status}><StatusBadge value={item.status} /><strong>{item.count}</strong></div>)}</div>;
}

export function PlatformOverviewPage() {
  const overview = useQuery({ queryKey: ['platform', 'overview'], queryFn: apiClient.platformOverview, retry: false });
  const monitoring = useQuery({ queryKey: ['platform', 'monitoring'], queryFn: apiClient.platformMonitoring, retry: false });

  if (overview.isPending) return <LoadingState label="جارٍ تحميل ملخص المنصة…" />;
  if (overview.isError) return <ErrorState message={safeError(overview.error)} />;
  const data = overview.data.data;

  return <>
    <PlatformHeading eyebrow="PA-W-001 · PLATFORM OVERVIEW" title="نظرة عامة على المنصة" description="مؤشرات Control Plane الآمنة وحالة سجل المؤسسات والنوادي وقواعد البيانات." onRefresh={() => { void overview.refetch(); void monitoring.refetch(); }} />
    <div className="lf-grid-3">
      <MetricCard label="حالة API" value={data.platformHealth.status} detail={`${data.platformHealth.environment} · ${data.platformHealth.version}`} />
      <MetricCard label="المؤسسات" value={data.organizationCount} detail="سجل Control Plane" />
      <MetricCard label="قواعد البيانات" value={data.databaseCounts.total} detail="سجل قواعد بيانات الأندية" />
    </div>
    <div className="lf-grid-3">
      <Card title="حالة الأندية"><div className="lf-metric">{data.gymCounts.total}</div><StatusBreakdown items={data.gymCounts.byStatus} /></Card>
      <Card title="حالة قواعد البيانات"><div className="lf-metric">{data.databaseCounts.total}</div><StatusBreakdown items={data.databaseCounts.byStatus} /></Card>
      <Card title="لقطة المراقبة">{monitoring.isPending ? <LoadingState label="جارٍ القراءة…" /> : monitoring.isError ? <ErrorState message={safeError(monitoring.error)} /> : <><div className="lf-metric">{monitoring.data.data.registeredDatabases.length}</div><span className="lf-muted">سجلّات مسجلة في آخر لقطة</span></>}</Card>
    </div>
    <Card title="حدود البيانات"><div className="lf-boundary-note"><strong>Platform scope فقط</strong><span>لا تعرض هذه الشاشة أعضاء أو مدفوعات أو حضورًا أو خطط تدريب أو تغذية أو معاملات متجر أو سجلات CRM.</span></div></Card>
  </>;
}

function Pagination({ page, pageSize, total, hasNext, onChange }: { page: number; pageSize: number; total: number; hasNext: boolean; onChange: (page: number) => void }) {
  return <div className="lf-pagination"><span className="lf-muted">{total} سجل · الصفحة {page}</span><div className="lf-action-row"><Button variant="secondary" disabled={page <= 1} onClick={() => onChange(page - 1)}>السابق</Button><Button variant="secondary" disabled={!hasNext} onClick={() => onChange(page + 1)}>التالي</Button></div></div>;
}

function OrganizationRow({ organization }: { organization: OrganizationSummary }) {
  return <tr><td><strong>{organization.name}</strong><br /><span className="lf-muted">{organization.slug}</span></td><td><StatusBadge value={organization.status} /></td><td>{formatDate(organization.createdAtUtc)}</td><td>{formatDate(organization.updatedAtUtc)}</td></tr>;
}

function GymRow({ gym }: { gym: GymSummary }) {
  return <tr><td><Link className="lf-inline-link lf-table-link" to={`/platform-admin/gyms/${gym.gymId}`}>{gym.name}</Link><br /><span className="lf-muted">{gym.slug}</span></td><td><StatusBadge value={gym.status} /></td><td>{gym.timezoneName}</td><td>{formatDate(gym.updatedAtUtc)}</td></tr>;
}

export function PlatformOrganizationsPage() {
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState('');
  const [page, setPage] = useState(1);
  const organizations = useQuery({ queryKey: ['platform', 'organizations', search, status, page], queryFn: () => apiClient.platformOrganizations({ search, status, page, pageSize: 25, sort: 'name:asc' }), retry: false });
  const gyms = useQuery({ queryKey: ['platform', 'gyms', search, status, page], queryFn: () => apiClient.platformGyms({ search, status, page, pageSize: 25, sort: 'name:asc' }), retry: false });

  return <>
    <Card><Link className="lf-inline-link" to="/platform-admin/provisioning">تهيئة نادٍ جديد من خلال workflow المنصة</Link></Card>
    <PlatformHeading eyebrow="PA-W-002 · REGISTRY" title="المؤسسات والأندية" description="قراءة سجل المؤسسات والأندية من Control Plane مع بحث وتصفية من الخادم." />
    <Card title="فلاتر السجل"><div className="lf-form-grid"><TextInput label="بحث بالاسم أو المعرّف النصي" value={search} onChange={(event) => { setSearch(event.target.value); setPage(1); }} placeholder="ابحث…" /><SelectField label="الحالة" value={status} onChange={(event) => { setStatus(event.target.value); setPage(1); }}><option value="">كل الحالات</option><option value="active">active</option><option value="inactive">inactive</option><option value="provisioning">provisioning</option></SelectField></div></Card>
    {organizations.isPending ? <LoadingState label="جارٍ تحميل المؤسسات…" /> : organizations.isError ? <ErrorState message={safeError(organizations.error)} /> : <Card title="المؤسسات"><div className="lf-table-wrap"><table className="lf-table"><thead><tr><th>المؤسسة</th><th>الحالة</th><th>الإنشاء</th><th>آخر تحديث</th></tr></thead><tbody>{organizations.data.data.length === 0 ? <tr><td colSpan={4}><EmptyState title="لا توجد مؤسسات" message="لم يعثر السجل على مؤسسات بهذه الفلاتر." /></td></tr> : organizations.data.data.map((item) => <OrganizationRow key={item.organizationId} organization={item} />)}</tbody></table></div><Pagination {...organizations.data.meta} onChange={setPage} /></Card>}
    {gyms.isPending ? <LoadingState label="جارٍ تحميل الأندية…" /> : gyms.isError ? <ErrorState message={safeError(gyms.error)} /> : <Card title="الأندية"><div className="lf-table-wrap"><table className="lf-table"><thead><tr><th>النادي</th><th>الحالة</th><th>المنطقة الزمنية</th><th>آخر تحديث</th></tr></thead><tbody>{gyms.data.data.length === 0 ? <tr><td colSpan={4}><EmptyState title="لا توجد أندية" message="لم يعثر السجل على أندية بهذه الفلاتر." /></td></tr> : gyms.data.data.map((item) => <GymRow key={item.gymId} gym={item} />)}</tbody></table></div><Pagination {...gyms.data.meta} onChange={setPage} /></Card>}
  </>;
}

function DatabaseTable({ databases }: { databases: DatabaseRegistrySummary[] }) {
  return <div className="lf-table-wrap"><table className="lf-table"><thead><tr><th>قاعدة البيانات</th><th>النادي</th><th>البيئة</th><th>الحالة</th><th>Schema / Seed</th><th>آخر فحص</th></tr></thead><tbody>{databases.map((database) => <tr key={database.gymDatabaseId}><td><strong>{database.databaseName}</strong></td><td><Link className="lf-inline-link lf-table-link" to={`/platform-admin/gyms/${database.gymId}`}>{database.gymId.slice(0, 8)}…</Link></td><td>{database.environment}</td><td><StatusBadge value={database.status} /></td><td><span className="lf-muted">{database.schemaVersion ?? '—'} / {database.seedVersion ?? '—'}</span></td><td>{formatDate(database.lastHealthAtUtc)}</td></tr>)}</tbody></table></div>;
}

export function PlatformGymDetailPage() {
  const { gymId = '' } = useParams();
  const gym = useQuery({ queryKey: ['platform', 'gym', gymId], queryFn: () => apiClient.platformGym(gymId), enabled: Boolean(gymId), retry: false });
  if (gym.isPending) return <LoadingState label="جارٍ تحميل بيانات النادي…" />;
  if (gym.isError) return <ErrorState message={safeError(gym.error)} />;
  const data = gym.data.data;
  return <>
    <PlatformHeading eyebrow="PA-W-003 · GYM REGISTRY" title={data.name} description="بيانات تعريف النادي وسجل قواعد البيانات الآمن فقط." onRefresh={() => { void gym.refetch(); }} />
    <div className="lf-grid-3"><MetricCard label="الحالة" value={data.status} detail={data.slug} /><MetricCard label="المنطقة الزمنية" value={data.timezoneName} /><MetricCard label="قواعد البيانات" value={data.databases.length} detail={`آخر تحديث ${formatDate(data.updatedAtUtc)}`} /></div>
    <Card title="هوية النادي"><Table headers={['الحقل', 'القيمة']} rows={[["Gym ID", <code key="id">{data.gymId}</code>], ['Organization ID', <code key="org">{data.organizationId}</code>], ['Created', formatDate(data.createdAtUtc)], ['Updated', formatDate(data.updatedAtUtc)]]} /></Card>
    <Card title="قواعد البيانات المسجلة">{data.databases.length === 0 ? <EmptyState title="لا توجد قواعد بيانات مسجلة" message="لا توجد placement metadata لهذا النادي." /> : <DatabaseTable databases={data.databases} />}</Card>
    <Link className="lf-inline-link" to="/platform-admin/organizations">العودة إلى المؤسسات والأندية</Link>
  </>;
}

export function PlatformDatabasesPage() {
  const [environment, setEnvironment] = useState('');
  const [status, setStatus] = useState('');
  const [page, setPage] = useState(1);
  const databases = useQuery({ queryKey: ['platform', 'databases', environment, status, page], queryFn: () => apiClient.platformDatabases({ environment, status, page, pageSize: 25, sort: 'databaseName:asc' }), retry: false });
  return <>
    <PlatformHeading eyebrow="PA-W-005 · DATABASE REGISTRY" title="سجل قواعد البيانات" description="قراءة placement metadata وحالة قواعد بيانات الأندية دون كشف أسرار الاتصال." />
    <Card title="فلاتر السجل"><div className="lf-form-grid"><SelectField label="البيئة" value={environment} onChange={(event) => { setEnvironment(event.target.value); setPage(1); }}><option value="">كل البيئات</option><option value="local">local</option><option value="development">development</option><option value="production">production</option></SelectField><SelectField label="الحالة" value={status} onChange={(event) => { setStatus(event.target.value); setPage(1); }}><option value="">كل الحالات</option><option value="pending">pending</option><option value="active">active</option><option value="inactive">inactive</option></SelectField></div></Card>
    {databases.isPending ? <LoadingState label="جارٍ تحميل سجل قواعد البيانات…" /> : databases.isError ? <ErrorState message={safeError(databases.error)} /> : <Card title="قواعد البيانات">{databases.data.data.length === 0 ? <EmptyState title="لا توجد قواعد بيانات" message="لم يعثر السجل على نتائج بهذه الفلاتر." /> : <DatabaseTable databases={databases.data.data} />}<Pagination {...databases.data.meta} onChange={setPage} /></Card>}
  </>;
}

export function PlatformOperationsPage() {
  const monitoring = useQuery({ queryKey: ['platform', 'monitoring', 'operations'], queryFn: apiClient.platformMonitoring, retry: false });
  if (monitoring.isPending) return <LoadingState label="جارٍ تحميل لقطة العمليات…" />;
  if (monitoring.isError) return <ErrorState message={safeError(monitoring.error)} />;
  const data = monitoring.data.data;
  return <>
    <PlatformHeading eyebrow="PA-W-009 · OPERATIONS SNAPSHOT" title="لقطة العمليات" description="لقطة طلبية من Health API وسجل قواعد البيانات؛ ليست بث مراقبة لحظيًا ولا تستبدل Platform Operations." onRefresh={() => { void monitoring.refetch(); }} />
    <div className="lf-grid-3"><MetricCard label="API" value={data.platformHealth.status} detail={`${data.platformHealth.service} · ${data.platformHealth.version}`} /><MetricCard label="البيئة" value={data.platformHealth.environment} /><MetricCard label="السجلات" value={data.registeredDatabases.length} detail={`Observed ${formatDate(data.observedAtUtc)}`} /></div>
    <Card title="حالة قواعد البيانات المسجلة">{data.registeredDatabases.length === 0 ? <EmptyState title="لا توجد سجلات" message="لا توجد قواعد بيانات مسجلة في Control Plane." /> : <DatabaseTable databases={data.registeredDatabases} />}</Card>
  </>;
}
