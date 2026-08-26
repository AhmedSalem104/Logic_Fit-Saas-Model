import { useState, type FormEvent } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { ApiClientError, type AuthSession } from '../lib/api';
import { useAuth } from '../lib/auth';
import { Button, Card, SelectField, TextInput } from './ui';

const loginSchema = z.object({
  email: z.string().trim().email(),
  password: z.string().min(1),
});

type LoginValues = z.infer<typeof loginSchema>;

function safeError(error: unknown) {
  return error instanceof ApiClientError ? error.message : 'تعذر إكمال العملية. حاول مرة أخرى.';
}

export function LoginPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const { signIn, verifyMfa } = useAuth();
  const [serverError, setServerError] = useState<string | null>(null);
  const [pendingSession, setPendingSession] = useState<AuthSession | null>(null);
  const [mfaMethod, setMfaMethod] = useState<'totp' | 'recovery_code'>('totp');
  const [mfaCode, setMfaCode] = useState('');
  const [message] = useState<string | null>((location.state as { message?: string } | null)?.message ?? null);
  const { register, handleSubmit, setError, formState: { errors, isSubmitting } } = useForm<LoginValues>({ defaultValues: { email: '', password: '' } });

  const submit = async (values: LoginValues) => {
    setServerError(null);
    const parsed = loginSchema.safeParse(values);
    if (!parsed.success) {
      for (const issue of parsed.error.issues) {
        const field = issue.path[0] === 'email' ? 'email' : 'password';
        setError(field, { type: 'validation', message: field === 'email' ? 'أدخل بريدًا إلكترونيًا صحيحًا.' : 'كلمة المرور مطلوبة.' });
      }
      return;
    }

    try {
      const session = await signIn(parsed.data.email, parsed.data.password);
      if (session.requiresMfa) {
        setPendingSession(session);
        setMfaCode('');
        return;
      }
      navigate('/app', { replace: true });
    } catch (error) {
      setServerError(safeError(error));
    }
  };

  const submitMfa = async () => {
    if (!pendingSession?.challenge || !mfaCode.trim()) return;
    setServerError(null);
    try {
      await verifyMfa(pendingSession.challenge, mfaMethod, mfaCode.trim());
      navigate('/app', { replace: true });
    } catch (error) {
      setServerError(safeError(error));
    }
  };

  return <div className="lf-auth-layout">
    <div className="lf-auth-intro">
      <span className="lf-eyebrow">LOGICFIT SECURITY · SYS-W-001</span>
      <h1>تسجيل الدخول</h1>
      <p>أدخل بيانات حسابك للوصول إلى مساحة العمل المصرح بها. يتحقق الخادم من الحساب والصلاحيات والنطاق.</p>
    </div>
    <Card className="lf-auth-card">
      {pendingSession ? <div className="lf-form-stack">
        <div><h2>تحقق MFA</h2><p className="lf-muted">أدخل الرمز من تطبيق Authenticator أو استخدم رمز استرداد لمرة واحدة.</p></div>
        <SelectField label="طريقة التحقق" value={mfaMethod} onChange={(event) => setMfaMethod(event.target.value as 'totp' | 'recovery_code')}>
          <option value="totp">رمز Authenticator</option>
          <option value="recovery_code">رمز استرداد</option>
        </SelectField>
        <TextInput label="الرمز" inputMode={mfaMethod === 'totp' ? 'numeric' : 'text'} autoComplete="one-time-code" value={mfaCode} onChange={(event) => setMfaCode(event.target.value)} />
        {serverError ? <div className="lf-form-error" role="alert">{serverError}</div> : null}
        <Button type="button" onClick={() => void submitMfa()} disabled={!mfaCode.trim()}>تحقق</Button>
        <Button type="button" variant="ghost" onClick={() => setPendingSession(null)}>العودة لتسجيل الدخول</Button>
      </div> : <form className="lf-form-stack" onSubmit={handleSubmit(submit)} noValidate>
        <TextInput label="البريد الإلكتروني" type="email" autoComplete="username" {...register('email')} error={errors.email ? 'أدخل بريدًا إلكترونيًا صحيحًا.' : undefined} />
        <TextInput label="كلمة المرور" type="password" autoComplete="current-password" {...register('password')} error={errors.password ? 'كلمة المرور مطلوبة.' : undefined} />
        {message ? <div className="lf-form-success" role="status">{message}</div> : null}
        {serverError ? <div className="lf-form-error" role="alert">{serverError}</div> : null}
        <Button type="submit" disabled={isSubmitting}>{isSubmitting ? 'جارٍ التحقق…' : 'دخول آمن'}</Button>
        <Link className="lf-inline-link" to="/password-reset">نسيت كلمة المرور؟</Link>
      </form>}
    </Card>
  </div>;
}

const resetRequestSchema = z.object({ email: z.string().trim().email() });
const resetCompleteSchema = z.object({ token: z.string().min(1), newPassword: z.string().min(12), confirmPassword: z.string().min(1) }).refine((values) => values.newPassword === values.confirmPassword, { path: ['confirmPassword'], message: 'تأكيد كلمة المرور غير مطابق.' });

export function PasswordResetPage() {
  const { requestPasswordReset, completePasswordReset } = useAuth();
  const navigate = useNavigate();
  const [requested, setRequested] = useState(false);
  const [error, setErrorMessage] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [email, setEmail] = useState('');
  const [token, setToken] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');

  const submitRequest = async (event: FormEvent) => {
    event.preventDefault();
    setErrorMessage(null);
    if (!resetRequestSchema.safeParse({ email }).success) { setErrorMessage('أدخل بريدًا إلكترونيًا صحيحًا.'); return; }
    try { await requestPasswordReset(email.trim()); setRequested(true); setSuccess('إذا كان الحساب موجودًا، ستصل تعليمات الاسترداد عبر قناة التسليم المحلية المعتمدة.'); } catch (reason) { setErrorMessage(safeError(reason)); }
  };

  const submitComplete = async (event: FormEvent) => {
    event.preventDefault();
    setErrorMessage(null);
    const parsed = resetCompleteSchema.safeParse({ token, newPassword, confirmPassword });
    if (!parsed.success) { setErrorMessage(parsed.error.issues[0]?.message ?? 'تحقق من البيانات.'); return; }
    try { await completePasswordReset(token.trim(), newPassword); setSuccess('تم تغيير كلمة المرور. يمكنك تسجيل الدخول الآن.'); setTimeout(() => navigate('/login', { replace: true }), 500); } catch (reason) { setErrorMessage(safeError(reason)); }
  };

  return <div className="lf-auth-layout"><div className="lf-auth-intro"><span className="lf-eyebrow">SYS-W-001 · RECOVERY</span><h1>استرداد الحساب</h1><p>يستخدم الطلب استجابة عامة لحماية خصوصية الحسابات.</p></div><Card className="lf-auth-card"><div className="lf-form-stack"><form className="lf-form-stack" onSubmit={submitRequest}><TextInput label="البريد الإلكتروني" type="email" value={email} onChange={(event) => setEmail(event.target.value)} />{requested ? <span className="lf-muted">يمكنك إكمال الاسترداد عند توفر رمز صالح.</span> : null}<Button type="submit">طلب تعليمات الاسترداد</Button></form><form className="lf-form-stack" onSubmit={submitComplete}><TextInput label="رمز الاسترداد" value={token} onChange={(event) => setToken(event.target.value)} /><TextInput label="كلمة المرور الجديدة" type="password" value={newPassword} onChange={(event) => setNewPassword(event.target.value)} /><TextInput label="تأكيد كلمة المرور" type="password" value={confirmPassword} onChange={(event) => setConfirmPassword(event.target.value)} /><Button type="submit" variant="secondary">إكمال تغيير كلمة المرور</Button></form>{error ? <div className="lf-form-error" role="alert">{error}</div> : null}{success ? <div className="lf-form-success" role="status">{success}</div> : null}<Link className="lf-inline-link" to="/login">العودة لتسجيل الدخول</Link></div></Card></div>;
}

export function AuthenticatedHome() {
  const navigate = useNavigate();
  const { me, signOut } = useAuth();
  const [error, setError] = useState<string | null>(null);

  const logout = async () => {
    try { await signOut(); navigate('/login', { replace: true }); } catch { setError('تعذر إنهاء الجلسة المحلية.'); }
  };

  return <div className="lf-page-stack"><div className="lf-page-heading"><div><span className="lf-eyebrow">AUTHENTICATED SHELL · SYS-W-002</span><h1>مرحبًا {me?.user.displayName}</h1><p>هذه المساحة تعرض هوية الحساب ونطاق الصلاحيات الذي أعاده API فقط.</p></div><div className="lf-header-actions"><Button variant="secondary" onClick={() => navigate('/app/security')}>أمان الحساب</Button><Button variant="secondary" onClick={logout}>تسجيل الخروج</Button></div></div>{error ? <div className="lf-form-error" role="alert">{error}</div> : null}<Card title="نطاق الحساب"><div className="lf-auth-scope-grid">{me?.scopes.map((scope) => <div className="lf-scope-item" key={`${scope.scopeType}-${scope.gymId ?? 'platform'}`}><strong>{scope.scopeType === 'platform' ? 'Platform' : `Gym ${scope.gymId}`}</strong><span>{scope.permissions.length} صلاحية</span></div>)}</div></Card>{me?.permissions.includes('platform.security.manage') ? <Card title="إدارة الوصول"><p className="lf-muted">إدارة المستخدمين والأدوار ضمن النطاق المصرح به.</p><Button onClick={() => navigate('/platform/access')}>فتح إدارة الوصول</Button></Card> : null}</div>;
}
