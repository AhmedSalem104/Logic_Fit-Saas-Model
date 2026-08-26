import { useEffect, useMemo, useState, type FormEvent } from 'react';
import { ApiClientError, type AccessCatalog, type AccessUser } from '../lib/api';
import { useAuth } from '../lib/auth';
import { Button, Card, LoadingState, TextInput } from './ui';

function safeError(error: unknown) {
  return error instanceof ApiClientError ? error.message : 'تعذر إكمال العملية. حاول مرة أخرى.';
}

export function AccessPage() {
  const { me, accessCatalog, accessUsers, createAccessUser, changeAccessUserStatus, assignRole, revokeRole } = useAuth();
  const [catalog, setCatalog] = useState<AccessCatalog | null>(null);
  const [users, setUsers] = useState<AccessUser[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [reason, setReason] = useState('تحديث أمني معتمد');
  const [email, setEmail] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [initialPassword, setInitialPassword] = useState('');
  const [selectedRole, setSelectedRole] = useState('');
  const [scopeType, setScopeType] = useState<'gym' | 'platform'>('gym');
  const [targetGymId, setTargetGymId] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const authorizedGymIds = useMemo(() => me?.scopes.filter((scope) => scope.scopeType === 'gym' && scope.gymId).map((scope) => scope.gymId as string) ?? [], [me]);
  const hasPlatformScope = useMemo(() => me?.scopes.some((scope) => scope.scopeType === 'platform') ?? false, [me]);
  const defaultGymId = authorizedGymIds[0] ?? '';
  const selectedGymId = targetGymId.trim() || defaultGymId;
  const scopedRoles = useMemo(() => catalog?.roles.filter((role) => role.scopeType === scopeType && role.status === 'active') ?? [], [catalog, scopeType]);

  const reload = async () => {
    if (scopeType === 'gym' && !selectedGymId) { setLoading(false); setError('أدخل Gym مصرحًا به أو اختر نطاق Platform.'); return; }
    setLoading(true);
    setError(null);
    try {
      const [nextCatalog, nextUsers] = await Promise.all([accessCatalog(), accessUsers({ gymId: scopeType === 'gym' ? selectedGymId : null, scopeType })]);
      setCatalog(nextCatalog);
      setUsers(nextUsers.data);
      setSelectedRole((current) => nextCatalog.roles.some((role) => role.roleId === current && role.scopeType === scopeType && role.status === 'active') ? current : nextCatalog.roles.find((role) => role.scopeType === scopeType && role.status === 'active')?.roleId || '');
    } catch (reasonValue) {
      setError(safeError(reasonValue));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (!defaultGymId && hasPlatformScope) setScopeType('platform');
  }, [defaultGymId, hasPlatformScope]);

  useEffect(() => { void reload(); }, [scopeType, selectedGymId]);

  const createUser = async (event: FormEvent) => {
    event.preventDefault();
    if (!selectedRole || (scopeType === 'gym' && !selectedGymId)) return;
    setSubmitting(true); setError(null); setMessage(null);
    try {
      await createAccessUser({ email: email.trim(), displayName: displayName.trim(), initialPassword, roleId: selectedRole, gymId: scopeType === 'gym' ? selectedGymId : null });
      setEmail(''); setDisplayName(''); setInitialPassword('');
      setMessage('تم إنشاء المستخدم وتعيين الدور الأولي.');
      await reload();
    } catch (reasonValue) { setError(safeError(reasonValue)); } finally { setSubmitting(false); }
  };

  const toggleStatus = async (user: AccessUser) => {
    setError(null); setMessage(null);
    try {
      await changeAccessUserStatus(user.userId, user.status === 'active' ? 'disabled' : 'active', reason.trim(), user.version);
      setMessage('تم تحديث حالة المستخدم وإلغاء الجلسات عند التعطيل.');
      await reload();
    } catch (reasonValue) { setError(safeError(reasonValue)); }
  };

  const grantSecurityRole = async (user: AccessUser) => {
    const securityRole = scopedRoles.find((role) => role.name === (scopeType === 'platform' ? 'Platform Security Admin' : 'Gym Security Admin'));
    if (!securityRole || (scopeType === 'gym' && !selectedGymId)) return;
    const existingAssignment = user.assignments.find((assignment) =>
      assignment.roleId === securityRole.roleId
      && assignment.scopeType === scopeType
      && assignment.gymId === (scopeType === 'gym' ? selectedGymId : null)
      && assignment.status !== 'active');
    setError(null); setMessage(null);
    try { await assignRole(user.userId, securityRole.roleId, scopeType === 'gym' ? selectedGymId : null, reason.trim(), existingAssignment?.version); setMessage('تم تعيين دور مسؤول الأمان.'); await reload(); } catch (reasonValue) { setError(safeError(reasonValue)); }
  };

  const revokeAssignment = async (user: AccessUser, assignment: AccessUser['assignments'][number]) => {
    setError(null); setMessage(null);
    try { await revokeRole(user.userId, assignment.assignmentId, reason.trim(), assignment.version); setMessage('تم إلغاء تعيين الدور.'); await reload(); } catch (reasonValue) { setError(safeError(reasonValue)); }
  };

  if (!me?.permissions.includes('platform.security.manage')) return <div className="lf-page-stack"><Card title="إدارة الوصول"><div className="lf-form-error" role="alert">لا تملك صلاحية platform.security.manage.</div></Card></div>;

  return <div className="lf-page-stack">
    <div className="lf-page-heading"><div><span className="lf-eyebrow">PA-W-007 · PLATFORM ACCESS</span><h1>المستخدمون والأدوار والصلاحيات</h1><p>إدارة هوية الوصول من خلال Control Plane وبنطاق Gym مصرح به فقط.</p></div></div>
    {error ? <div className="lf-form-error" role="alert">{error}</div> : null}
    {message ? <div className="lf-form-success" role="status">{message}</div> : null}
    {loading ? <LoadingState label="جارٍ تحميل كتالوج الوصول…" /> : <>
      <Card title="كتالوج الصلاحيات canonical"><div className="lf-grid-3"><div><div className="lf-metric">{catalog?.permissions.length ?? 0}</div><span className="lf-muted">صلاحية</span></div><div><div className="lf-metric">{catalog?.roles.length ?? 0}</div><span className="lf-muted">دور</span></div><div><div className="lf-metric">{catalog?.rolePermissionAssignmentCount ?? 0}</div><span className="lf-muted">تعيين صلاحية</span></div></div></Card>
      <Card title="نطاق الإدارة"><div className="lf-form-grid"><label className="lf-field"><span>نوع النطاق</span><select className="lf-input" value={scopeType} onChange={(event) => setScopeType(event.target.value as 'gym' | 'platform')}><option value="gym">Gym مصرح</option>{hasPlatformScope ? <option value="platform">Platform</option> : null}</select></label>{scopeType === 'gym' ? <TextInput label="معرّف Gym المستهدف" required value={targetGymId} placeholder={defaultGymId || 'GUID'} onChange={(event) => setTargetGymId(event.target.value)} /> : <p className="lf-muted">يُطبّق نطاق Platform على حسابات Platform فقط. لا يمنح وصولًا تشغيليًا إلى بيانات Gym.</p>}</div></Card>
      <Card title="إنشاء مستخدم"><form className="lf-form-grid" onSubmit={createUser}><TextInput label="البريد الإلكتروني" type="email" required value={email} onChange={(event) => setEmail(event.target.value)} /><TextInput label="الاسم المعروض" required value={displayName} onChange={(event) => setDisplayName(event.target.value)} /><TextInput label="كلمة المرور الأولية" type="password" required minLength={12} value={initialPassword} onChange={(event) => setInitialPassword(event.target.value)} /><label className="lf-field"><span>الدور الأولي</span><select className="lf-input" required value={selectedRole} onChange={(event) => setSelectedRole(event.target.value)}>{scopedRoles.map((role) => <option key={role.roleId} value={role.roleId}>{role.name}</option>)}</select></label><TextInput label="سبب التغيير الإداري" required value={reason} onChange={(event) => setReason(event.target.value)} /><Button type="submit" disabled={submitting || !selectedRole || (scopeType === 'gym' && !selectedGymId)}>{submitting ? 'جارٍ الإنشاء…' : 'إنشاء مستخدم'}</Button></form></Card>
      <Card title={scopeType === 'platform' ? 'مستخدمو Platform' : `مستخدمو Gym ${selectedGymId}`}><div className="lf-table-wrap"><table className="lf-table"><thead><tr><th>المستخدم</th><th>الحالة</th><th>الأدوار</th><th>إجراءات</th></tr></thead><tbody>{users.length === 0 ? <tr><td colSpan={4}><div className="lf-state"><strong>لا توجد حسابات في النطاق</strong><span>أنشئ مستخدمًا من النموذج أعلاه.</span></div></td></tr> : users.map((user) => <tr key={user.userId}><td><strong>{user.displayName}</strong><br /><span className="lf-muted">{user.email}</span></td><td>{user.status}</td><td>{user.assignments.filter((assignment) => assignment.status === 'active').map((assignment) => <div key={assignment.assignmentId} className="lf-assignment-row"><span>{assignment.roleName}</span><Button variant="ghost" onClick={() => void revokeAssignment(user, assignment)}>إلغاء الدور</Button></div>)}</td><td><div className="lf-action-row"><Button variant="secondary" onClick={() => void toggleStatus(user)}>{user.status === 'active' ? 'تعطيل' : 'تفعيل'}</Button>{!user.assignments.some((assignment) => assignment.roleName === (scopeType === 'platform' ? 'Platform Security Admin' : 'Gym Security Admin') && assignment.status === 'active') ? <Button onClick={() => void grantSecurityRole(user)}>تعيين مسؤول أمان</Button> : null}</div></td></tr>)}</tbody></table></div></Card>
    </>}
  </div>;
}
