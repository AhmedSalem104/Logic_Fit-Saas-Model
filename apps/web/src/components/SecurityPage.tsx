import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { useNavigate } from 'react-router-dom';
import { z } from 'zod';
import { ApiClientError, type AuthSessionItem, type MfaEnrollment } from '../lib/api';
import { useAuth } from '../lib/auth';
import { Button, Card, LoadingState, TextInput } from './ui';

const passwordSchema = z.object({
  currentPassword: z.string().min(1),
  newPassword: z.string().min(12),
  confirmPassword: z.string().min(1),
}).refine((values) => values.newPassword === values.confirmPassword, {
  path: ['confirmPassword'],
  message: 'تأكيد كلمة المرور غير مطابق.',
});

type PasswordValues = z.infer<typeof passwordSchema>;

function safeError(error: unknown) {
  return error instanceof ApiClientError ? error.message : 'تعذر إكمال العملية. حاول مرة أخرى.';
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat('ar-EG', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value));
}

export function SecurityPage() {
  const navigate = useNavigate();
  const {
    session,
    me,
    listSessions,
    revokeSession,
    changePassword,
    enrollMfa,
    verifyMfa,
    disableMfa,
    regenerateRecoveryCodes,
  } = useAuth();
  const [sessions, setSessions] = useState<AuthSessionItem[]>([]);
  const [sessionsLoading, setSessionsLoading] = useState(true);
  const [sessionsError, setSessionsError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [enrollment, setEnrollment] = useState<MfaEnrollment | null>(null);
  const [mfaCode, setMfaCode] = useState('');
  const [recoveryPassword, setRecoveryPassword] = useState('');
  const [disablePassword, setDisablePassword] = useState('');
  const [recoveryCodes, setRecoveryCodes] = useState<string[] | null>(null);
  const { register, handleSubmit, reset, setError, formState: { errors, isSubmitting } } = useForm<PasswordValues>();

  const reloadSessions = async () => {
    setSessionsLoading(true);
    setSessionsError(null);
    try {
      setSessions((await listSessions()).data);
    } catch (error) {
      setSessionsError(safeError(error));
    } finally {
      setSessionsLoading(false);
    }
  };

  useEffect(() => { void reloadSessions(); }, []);

  const submitPassword = async (values: PasswordValues) => {
    setActionError(null);
    setMessage(null);
    const parsed = passwordSchema.safeParse(values);
    if (!parsed.success) {
      parsed.error.issues.forEach((issue) => setError(issue.path[0] as keyof PasswordValues, { type: 'validation', message: issue.message }));
      return;
    }
    try {
      await changePassword(parsed.data.currentPassword, parsed.data.newPassword);
      reset();
      navigate('/login', { replace: true, state: { message: 'تم تغيير كلمة المرور. سجّل الدخول من جديد.' } });
    } catch (error) {
      setActionError(safeError(error));
    }
  };

  const startMfaEnrollment = async () => {
    setActionError(null);
    setMessage(null);
    try {
      setEnrollment(await enrollMfa());
    } catch (error) {
      setActionError(safeError(error));
    }
  };

  const confirmMfaEnrollment = async () => {
    if (!enrollment || !mfaCode.trim()) return;
    setActionError(null);
    try {
      await verifyMfa(enrollment.factorId, 'totp', mfaCode.trim());
      setEnrollment(null);
      setMfaCode('');
      setMessage('تم تفعيل تطبيق المصادقة بنجاح.');
    } catch (error) {
      setActionError(safeError(error));
    }
  };

  const rotateRecoveryCodes = async () => {
    setActionError(null);
    try {
      setRecoveryCodes(await regenerateRecoveryCodes(recoveryPassword));
      setRecoveryPassword('');
      setMessage('تم إنشاء رموز استرداد جديدة. احفظها في مكان آمن؛ لن تظهر مرة أخرى.');
    } catch (error) {
      setActionError(safeError(error));
    }
  };

  const turnOffMfa = async () => {
    setActionError(null);
    try {
      await disableMfa(disablePassword);
      navigate('/login', { replace: true, state: { message: 'تم تعطيل MFA. سجّل الدخول من جديد.' } });
    } catch (error) {
      setActionError(safeError(error));
    }
  };

  const revoke = async (item: AuthSessionItem) => {
    setActionError(null);
    try {
      await revokeSession(item.sessionId, 'self-service-security-page');
      if (item.isCurrent) {
        navigate('/login', { replace: true });
      } else {
        await reloadSessions();
        setMessage('تم إلغاء الجلسة.');
      }
    } catch (error) {
      setActionError(safeError(error));
    }
  };

  if (!session || !me) return <LoadingState label="جارٍ تحميل أمان الحساب…" />;

  return <div className="lf-page-stack">
    <div className="lf-page-heading">
      <div><span className="lf-eyebrow">SYS-W-002</span><h1>أمان الحساب</h1><p>إدارة كلمة المرور، المصادقة متعددة العوامل والجلسات النشطة من خلال API.</p></div>
      <Button variant="secondary" onClick={() => navigate('/app')}>العودة للمساحة</Button>
    </div>
    {actionError ? <div className="lf-form-error" role="alert">{actionError}</div> : null}
    {message ? <div className="lf-form-success" role="status">{message}</div> : null}

    <Card title="الجلسات النشطة">
      {sessionsLoading ? <LoadingState label="جارٍ تحميل الجلسات…" /> : sessionsError ? <div className="lf-form-error" role="alert">{sessionsError}</div> : sessions.length === 0 ? <div className="lf-state"><strong>لا توجد جلسات نشطة</strong><span>ستظهر الجلسات هنا بعد تسجيل الدخول.</span></div> : <div className="lf-table-wrap"><table className="lf-table"><thead><tr><th>الجلسة</th><th>الإنشاء</th><th>آخر نشاط</th><th>الانتهاء</th><th>الحالة</th><th>إجراء</th></tr></thead><tbody>{sessions.map((item) => <tr key={item.sessionId}><td><code>{item.sessionId.slice(0, 8)}…</code></td><td>{formatDate(item.createdAtUtc)}</td><td>{formatDate(item.lastSeenAtUtc)}</td><td>{formatDate(item.expiresAtUtc)}</td><td>{item.isCurrent ? 'هذه الجلسة' : item.mfaVerified ? 'موثقة' : 'تحقق MFA'}</td><td><Button variant="danger" onClick={() => void revoke(item)}>إلغاء</Button></td></tr>)}</tbody></table></div>}
    </Card>

    <div className="lf-grid-3">
      <Card title="تغيير كلمة المرور">
        <form className="lf-form-stack" onSubmit={handleSubmit(submitPassword)} noValidate>
          <TextInput label="كلمة المرور الحالية" type="password" autoComplete="current-password" {...register('currentPassword')} error={errors.currentPassword?.message} />
          <TextInput label="كلمة المرور الجديدة" type="password" autoComplete="new-password" {...register('newPassword')} error={errors.newPassword?.message} />
          <TextInput label="تأكيد كلمة المرور الجديدة" type="password" autoComplete="new-password" {...register('confirmPassword')} error={errors.confirmPassword?.message} />
          <Button type="submit" disabled={isSubmitting}>{isSubmitting ? 'جارٍ الحفظ…' : 'تغيير كلمة المرور'}</Button>
        </form>
      </Card>

      <Card title="تطبيق المصادقة MFA">
        {enrollment ? <div className="lf-form-stack"><p>أضف الحساب إلى تطبيق Authenticator ثم أدخل الرمز الحالي. رمز الإعداد يظهر أثناء هذه العملية فقط.</p><code className="lf-secret-value">{enrollment.provisioningUri}</code><TextInput label="رمز التطبيق" inputMode="numeric" value={mfaCode} onChange={(event) => setMfaCode(event.target.value)} /><Button onClick={() => void confirmMfaEnrollment()} disabled={!mfaCode.trim()}>تأكيد التفعيل</Button></div> : <div className="lf-form-stack"><p className="lf-muted">MFA يحمي تسجيل الدخول باستخدام تطبيق Authenticator.</p><Button onClick={() => void startMfaEnrollment()}>بدء التسجيل</Button><TextInput label="كلمة المرور لإدارة MFA" type="password" value={disablePassword} onChange={(event) => setDisablePassword(event.target.value)} /><Button variant="danger" onClick={() => void turnOffMfa()} disabled={!disablePassword}>تعطيل MFA</Button></div>}
      </Card>

      <Card title="رموز الاسترداد">
        <div className="lf-form-stack"><p className="lf-muted">إنشاء الرموز يلغي الرموز السابقة. لا تحفظها داخل التطبيق.</p><TextInput label="كلمة المرور الحالية" type="password" value={recoveryPassword} onChange={(event) => setRecoveryPassword(event.target.value)} /><Button variant="secondary" onClick={() => void rotateRecoveryCodes()} disabled={!recoveryPassword}>إنشاء رموز جديدة</Button>{recoveryCodes ? <div className="lf-recovery-codes" aria-label="رموز الاسترداد">{recoveryCodes.map((code) => <code key={code}>{code}</code>)}</div> : null}</div>
      </Card>
    </div>
  </div>;
}
