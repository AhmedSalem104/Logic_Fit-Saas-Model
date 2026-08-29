import { useState, type FormEvent } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  ApiClientError,
  apiClient,
  type ProvisioningRequest,
  type ProvisioningStatus,
} from '../lib/api';
import { useAuth } from '../lib/auth';
import { Button, Card, EmptyState, ErrorState, LoadingState, TextInput } from './ui';

const DEFAULT_SERVER_ID = '5e5f5f7e-31d2-4f0c-9ec4-0fcf3fdbac73';
const STEP_ORDER = [
  'RequestValidation',
  'OrganizationCreation',
  'GymRegistryCreation',
  'ServerPlacement',
  'DatabaseCreation',
  'EfCoreMigrations',
  'CanonicalSeeding',
  'Verification',
  'OwnerInitialization',
  'Activation',
] as const;
const TERMINAL_STATES = new Set(['Active', 'ProvisioningFailed', 'MigrationFailed', 'SeedingFailed', 'VerificationFailed']);

function safeError(error: unknown) {
  return error instanceof ApiClientError
    ? error.message
    : 'تعذر تحميل حالة عملية التهيئة. حاول مرة أخرى.';
}

function formatDate(value: string | null) {
  if (!value) return '—';
  return new Intl.DateTimeFormat('ar-EG', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value));
}

function statusClass(status: string) {
  return status.toLowerCase() === 'active' || status.toLowerCase() === 'success'
    ? 'lf-status-success'
    : 'lf-status-neutral';
}

function StatusBadge({ value }: { value: string }) {
  return <span className={`lf-status-pill ${statusClass(value)}`}>{value}</span>;
}

function platformProvisionPermission(me: ReturnType<typeof useAuth>['me']) {
  return Boolean(me?.permissions.includes('platform.provision') && me.scopes.some((scope) =>
    scope.scopeType === 'platform' && scope.gymId === null && scope.permissions.includes('platform.provision')));
}

function ProvisioningAccessNotice() {
  return <Card title="تهيئة الأندية على المنصة">
    <div className="lf-form-error" role="alert">لا تملك صلاحية platform.provision لتنفيذ عمليات التهيئة.</div>
  </Card>;
}

function validateForm(form: ProvisioningRequest) {
  const errors: Record<string, string> = {};
  if (!form.organization.name.trim()) errors.organizationName = 'اسم المؤسسة مطلوب.';
  if (!/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(form.organization.slug)) errors.organizationSlug = 'استخدم حروفًا إنجليزية صغيرة وأرقامًا وشرطات مفردة.';
  if (!form.gym.name.trim()) errors.gymName = 'اسم النادي مطلوب.';
  if (!/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(form.gym.slug)) errors.gymSlug = 'استخدم حروفًا إنجليزية صغيرة وأرقامًا وشرطات مفردة.';
  if (!form.gym.timezoneName.trim()) errors.timezoneName = 'المنطقة الزمنية مطلوبة.';
  if (!/^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(form.serverTarget.serverId)) errors.serverId = 'معرّف الخادم يجب أن يكون UUID صالحًا.';
  if (!/^\S+@\S+\.\S+$/.test(form.owner.email)) errors.ownerEmail = 'البريد الإلكتروني غير صالح.';
  if (!form.owner.displayName.trim()) errors.displayName = 'اسم المالك مطلوب.';
  if (form.owner.initialPassword.length < 12) errors.initialPassword = 'كلمة المرور يجب أن تتوافق مع سياسة الأمان.';
  return errors;
}

function ProvisioningForm({ onAccepted }: { onAccepted: (operationId: string) => void }) {
  const [form, setForm] = useState<ProvisioningRequest>({
    organization: { name: '', slug: '' },
    gym: { name: '', slug: '', timezoneName: 'Africa/Cairo' },
    serverTarget: { serverId: import.meta.env.VITE_PLATFORM_SERVER_ID ?? DEFAULT_SERVER_ID },
    owner: { email: '', displayName: '', initialPassword: '' },
  });
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [serverError, setServerError] = useState<string | null>(null);
  const mutation = useMutation({
    mutationFn: (input: ProvisioningRequest) => apiClient.requestProvisioning(input, crypto.randomUUID()),
    onSuccess: (response) => onAccepted(response.data.operationId),
    onError: (error) => setServerError(safeError(error)),
  });

  const update = (section: keyof ProvisioningRequest, field: string, value: string) => {
    setForm((current) => ({
      ...current,
      [section]: { ...(current[section] as Record<string, string>), [field]: value },
    }));
    setErrors((current) => {
      const next = { ...current };
      delete next[`${section}${field.charAt(0).toUpperCase()}${field.slice(1)}`];
      if (section === 'serverTarget') delete next.serverId;
      return next;
    });
    setServerError(null);
  };

  const submit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const nextErrors = validateForm(form);
    setErrors(nextErrors);
    setServerError(null);
    if (Object.keys(nextErrors).length > 0) return;
    mutation.mutate(form);
  };

  return <form className="lf-form-stack" onSubmit={submit} noValidate>
    <div className="lf-page-heading">
      <div><span className="lf-eyebrow">PA-W-004 · PROVISIONING</span><h1>تهيئة نادٍ جديد</h1><p>إنشاء سجل المؤسسة والنادي ثم تشغيل التهيئة غير المتزامنة على الخادم المسجل.</p></div>
      <Link className="lf-inline-link" to="/platform-admin">العودة إلى المنصة</Link>
    </div>
    <Card title="بيانات المؤسسة والنادي">
      <div className="lf-form-grid">
        <TextInput label="اسم المؤسسة" value={form.organization.name} onChange={(event) => update('organization', 'name', event.target.value)} error={errors.organizationName} required />
        <TextInput label="معرّف المؤسسة (slug)" value={form.organization.slug} onChange={(event) => update('organization', 'slug', event.target.value.toLowerCase())} error={errors.organizationSlug} required />
        <TextInput label="اسم النادي" value={form.gym.name} onChange={(event) => update('gym', 'name', event.target.value)} error={errors.gymName} required />
        <TextInput label="معرّف النادي (slug)" value={form.gym.slug} onChange={(event) => update('gym', 'slug', event.target.value.toLowerCase())} error={errors.gymSlug} required />
        <TextInput label="المنطقة الزمنية" value={form.gym.timezoneName} onChange={(event) => update('gym', 'timezoneName', event.target.value)} error={errors.timezoneName} required />
        <TextInput label="معرّف الخادم المسجل" value={form.serverTarget.serverId} onChange={(event) => update('serverTarget', 'serverId', event.target.value)} error={errors.serverId} required />
      </div>
    </Card>
    <Card title="تهيئة المالك الأول">
      <div className="lf-form-grid">
        <TextInput label="البريد الإلكتروني" type="email" value={form.owner.email} onChange={(event) => update('owner', 'email', event.target.value)} error={errors.ownerEmail} required autoComplete="off" />
        <TextInput label="اسم العرض" value={form.owner.displayName} onChange={(event) => update('owner', 'displayName', event.target.value)} error={errors.displayName} required />
        <TextInput label="كلمة المرور الأولية" type="password" value={form.owner.initialPassword} onChange={(event) => update('owner', 'initialPassword', event.target.value)} error={errors.initialPassword} required autoComplete="new-password" />
      </div>
      <p className="lf-muted">تُرسل كلمة المرور عبر الطلب المحمي ليتم تجزئتها في الخادم، ولا تُعرض أو تُسجل.</p>
      {serverError ? <div className="lf-form-error" role="alert">{serverError}</div> : null}
      <div className="lf-action-row">
        <Button type="button" variant="secondary" onClick={() => window.history.back()} disabled={mutation.isPending}>إلغاء</Button>
        <Button type="submit" disabled={mutation.isPending}>{mutation.isPending ? 'جارٍ إرسال الطلب…' : 'بدء التهيئة'}</Button>
      </div>
    </Card>
  </form>;
}

function StepList({ data }: { data: ProvisioningStatus }) {
  const successful = data.steps.filter((step) => step.status === 'Success').length;
  const progress = Math.round((successful / STEP_ORDER.length) * 100);
  return <Card title="تقدم العملية">
    <div className="lf-progress-track" aria-label={`اكتمل ${progress}%`}><div className="lf-progress-value" style={{ width: `${progress}%` }} /></div>
    <div className="lf-provisioning-step-list">
      {STEP_ORDER.map((stepKey) => {
        const step = data.steps.filter((item) => item.stepKey === stepKey).at(-1);
        return <div className="lf-provisioning-step" key={`${stepKey}-${step?.attemptNo ?? 0}`}><span>{stepKey}</span><StatusBadge value={step?.status ?? 'Pending'} /></div>;
      })}
    </div>
  </Card>;
}

function ProvisioningStatusPage({ runId }: { runId: string }) {
  const queryClient = useQueryClient();
  const [reason, setReason] = useState('');
  const status = useQuery({
    queryKey: ['platform', 'provisioning', runId],
    queryFn: () => apiClient.provisioningStatus(runId),
    retry: false,
    refetchInterval: (query) => {
      const current = query.state.data?.data.status;
      return current && TERMINAL_STATES.has(current) ? false : 2000;
    },
  });
  const retry = useMutation({
    mutationFn: (retryReason: string) => apiClient.retryProvisioning(runId, retryReason, crypto.randomUUID()),
    onSuccess: () => {
      setReason('');
      void queryClient.invalidateQueries({ queryKey: ['platform', 'provisioning', runId] });
    },
  });

  if (status.isPending) return <LoadingState label="جارٍ تحميل حالة التهيئة…" />;
  if (status.isError) return <ErrorState message={safeError(status.error)} />;
  const data = status.data.data;
  const canRetry = data.retryable && !retry.isPending;
  const failureMessage = data.failure ? `${data.failure.errorCode} · ${data.failure.failureCategory}` : null;

  return <>
    <div className="lf-page-heading">
      <div><span className="lf-eyebrow">PA-W-004 · PROVISIONING STATUS</span><h1>حالة تهيئة النادي</h1><p>تُحدّث هذه الصفحة حالة العملية من خلال API المنصة فقط.</p></div>
      <div className="lf-action-row"><StatusBadge value={data.status} /><Link className="lf-inline-link" to="/platform-admin">العودة إلى المنصة</Link></div>
    </div>
    <div className="lf-grid-3">
      <Card title="المعرّف"><code>{data.operationId}</code><span className="lf-muted">طلب التهيئة</span></Card>
      <Card title="الخطوة الحالية"><strong>{data.currentStep ?? 'مكتملة'}</strong><span className="lf-muted">المحاولة {data.attemptNo}</span></Card>
      <Card title="التواريخ"><span className="lf-muted">بدأت: {formatDate(data.startedAtUtc)}</span><span className="lf-muted">اكتملت: {formatDate(data.completedAtUtc)}</span></Card>
    </div>
    {failureMessage ? <div className="lf-form-error" role="alert">فشلت العملية: {failureMessage}{data.failure?.retryable ? ' يمكن إعادة المحاولة.' : ''}</div> : null}
    {data.status === 'Active' ? <div className="lf-form-success" role="status">تم تفعيل النادي بنجاح. تم تهيئة قاعدة البيانات والبيانات المرجعية والمالك الأول.</div> : null}
    <StepList data={data} />
    <Card title="بيانات آمنة">
      <div className="lf-table-wrap"><table className="lf-table"><tbody>
        <tr><th>Organization ID</th><td><code>{data.organizationId}</code></td></tr>
        <tr><th>Gym ID</th><td><code>{data.gymId}</code></td></tr>
        <tr><th>الخادم</th><td>{data.server ? `${data.server.environment} · ${data.server.status}` : 'لم يُحدد بعد'}</td></tr>
        <tr><th>قاعدة البيانات</th><td>{data.database ? `${data.database.databaseName} · ${data.database.status}` : 'لم تُنشأ بعد'}</td></tr>
        <tr><th>Owner</th><td>{data.ownerInitialized ? 'تمت التهيئة' : 'قيد التهيئة'}</td></tr>
      </tbody></table></div>
    </Card>
    {canRetry ? <Card title="إعادة المحاولة">
      <TextInput label="سبب آمن لإعادة المحاولة" value={reason} onChange={(event) => setReason(event.target.value)} maxLength={500} placeholder="مثال: تعذر الاتصال المؤقت بخادم SQL Server" />
      {retry.isError ? <div className="lf-form-error" role="alert">{safeError(retry.error)}</div> : null}
      <Button variant="secondary" disabled={!reason.trim() || retry.isPending} onClick={() => retry.mutate(reason.trim())}>{retry.isPending ? 'جارٍ إرسال إعادة المحاولة…' : 'إعادة المحاولة'}</Button>
    </Card> : null}
  </>;
}

export function PlatformProvisioningPage() {
  const { runId } = useParams();
  const navigate = useNavigate();
  const { me } = useAuth();
  if (!platformProvisionPermission(me)) return <div className="lf-page-stack"><ProvisioningAccessNotice /></div>;
  if (!runId) return <ProvisioningForm onAccepted={(operationId) => navigate(`/platform-admin/provisioning/${operationId}`)} />;
  return <ProvisioningStatusPage runId={runId} />;
}
