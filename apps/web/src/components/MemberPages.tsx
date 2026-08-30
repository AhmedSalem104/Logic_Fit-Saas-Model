import { useEffect, useMemo, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { z } from 'zod';
import { ApiClientError, apiClient, type MemberDetail, type MemberStatus } from '../lib/api';
import { useAuth } from '../lib/auth';
import { Button, Card, EmptyState, ErrorState, LoadingState, SelectField, TextInput } from './ui';

const memberFormSchema = z.object({
  fullName: z.string().trim().min(1).max(120),
  phone: z.string().trim().min(5).max(30),
  email: z.string().trim().email().max(254).or(z.literal('')),
  registrationDate: z.string().regex(/^\d{4}-\d{2}-\d{2}$/),
  notes: z.string().max(1000),
});

function safeError(error: unknown) {
  return error instanceof ApiClientError ? error.message : 'تعذر إكمال عملية الأعضاء. حاول مرة أخرى.';
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat('ar-EG', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value));
}

function gymForMembers(me: ReturnType<typeof useAuth>['me']) {
  return me?.scopes.find((scope) => scope.scopeType === 'gym' && scope.gymId && scope.permissions.includes('members.read'))?.gymId ?? null;
}

function MemberAccessNotice() {
  return <Card title="الأعضاء"><div className="lf-form-error" role="alert">لا توجد صلاحية members.read أو لا يوجد نطاق Gym مصرح به.</div></Card>;
}

function initialMemberForm(member?: MemberDetail) {
  return {
    fullName: member?.fullName ?? '',
    phone: member?.phone ?? '',
    email: member?.email ?? '',
    registrationDate: member?.registrationDate ?? new Date().toISOString().slice(0, 10),
    notes: member?.notes ?? '',
  };
}

function MemberForm({ member, onSaved }: { member?: MemberDetail; onSaved: (member: MemberDetail) => void }) {
  const { me } = useAuth();
  const gymId = gymForMembers(me);
  const [form, setForm] = useState(() => initialMemberForm(member));
  const [status, setStatus] = useState<'ACTIVE' | 'INACTIVE'>(member?.status === 'INACTIVE' ? 'INACTIVE' : 'ACTIVE');
  const [error, setError] = useState<string | null>(null);
  const [fieldError, setFieldError] = useState<string | null>(null);
  const queryClient = useQueryClient();
  const mutation = useMutation({
    mutationFn: async () => {
      if (!gymId) throw new Error('Gym scope is required.');
      const parsed = memberFormSchema.safeParse(form);
      if (!parsed.success) throw new Error('تحقق من الاسم والهاتف والبريد والتاريخ والملاحظات.');
      return member
        ? (await apiClient.updateMember(gymId, member.memberId, member.version, { ...parsed.data, email: parsed.data.email || null, notes: parsed.data.notes || null, status })).data
        : (await apiClient.createMember(gymId, { ...parsed.data, email: parsed.data.email || null, notes: parsed.data.notes || null })).data;
    },
    onSuccess: (saved) => {
      setError(null);
      setFieldError(null);
      queryClient.invalidateQueries({ queryKey: ['members', gymId] });
      onSaved(saved);
    },
    onError: (reason) => setError(safeError(reason)),
  });

  useEffect(() => {
    setForm(initialMemberForm(member));
    setStatus(member?.status === 'INACTIVE' ? 'INACTIVE' : 'ACTIVE');
  }, [member]);

  return <form className="lf-form-stack" onSubmit={(event) => { event.preventDefault(); setFieldError(null); mutation.mutate(); }} noValidate>
    <div className="lf-form-grid">
      <TextInput label="الاسم الكامل" required value={form.fullName} onChange={(event) => setForm((current) => ({ ...current, fullName: event.target.value }))} />
      <TextInput label="الهاتف" required value={form.phone} onChange={(event) => setForm((current) => ({ ...current, phone: event.target.value }))} />
      <TextInput label="البريد الإلكتروني" type="email" value={form.email} onChange={(event) => setForm((current) => ({ ...current, email: event.target.value }))} />
      <TextInput label="تاريخ التسجيل" type="date" required value={form.registrationDate} onChange={(event) => setForm((current) => ({ ...current, registrationDate: event.target.value }))} />
    </div>
    <label className="lf-field"><span>ملاحظات</span><textarea className="lf-input" rows={3} value={form.notes} onChange={(event) => setForm((current) => ({ ...current, notes: event.target.value }))} /></label>
    {member ? <SelectField label="الحالة" value={status} onChange={(event) => setStatus(event.target.value as 'ACTIVE' | 'INACTIVE')}><option value="ACTIVE">ACTIVE</option><option value="INACTIVE">INACTIVE</option></SelectField> : null}
    {fieldError ? <div className="lf-form-error" role="alert">{fieldError}</div> : null}
    {error ? <div className="lf-form-error" role="alert">{error}</div> : null}
    <div className="lf-action-row"><Button type="submit" disabled={mutation.isPending}>{mutation.isPending ? 'جارٍ الحفظ…' : member ? 'حفظ التعديلات' : 'إنشاء عضو'}</Button>{member ? <span className="lf-muted">Member Code: {member.memberCode}</span> : null}</div>
  </form>;
}

export function MembersPage() {
  const { me } = useAuth();
  const navigate = useNavigate();
  const gymId = gymForMembers(me);
  const canCreate = me?.permissions.includes('members.create') ?? false;
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState<'ACTIVE' | 'INACTIVE' | 'ARCHIVED' | ''>('');
  const [page, setPage] = useState(1);
  const members = useQuery({
    queryKey: ['members', gymId, search, status, page],
    queryFn: () => apiClient.listMembers(gymId!, { search, page, pageSize: 25, statuses: status ? [status] : ['ACTIVE', 'INACTIVE'] }),
    enabled: Boolean(gymId),
    retry: false,
  });

  if (!gymId) return <div className="lf-page-stack"><MemberAccessNotice /></div>;
  return <div className="lf-page-stack">
    <div className="lf-page-heading"><div><span className="lf-eyebrow">MEM-W-001 · MEMBERS</span><h1>الأعضاء</h1><p>ملف الأعضاء الأساسي ضمن نطاق Gym المصرح به فقط. لا توجد بيانات عضوية أو حضور أو مدفوعات هنا.</p></div><span className="lf-status-pill lf-status-neutral">Gym {gymId.slice(0, 8)}…</span></div>
    {canCreate ? <Card title="إضافة عضو"><MemberForm onSaved={(member) => navigate(`/app/members/${member.memberId}`)} /></Card> : null}
    <Card title="البحث والتصفية"><div className="lf-form-grid"><TextInput label="بحث آمن" placeholder="Member Code، الاسم، الهاتف أو البريد" value={search} onChange={(event) => { setSearch(event.target.value); setPage(1); }} /><SelectField label="الحالة" value={status} onChange={(event) => { setStatus(event.target.value as typeof status); setPage(1); }}><option value="">ACTIVE + INACTIVE</option><option value="ACTIVE">ACTIVE</option><option value="INACTIVE">INACTIVE</option><option value="ARCHIVED">ARCHIVED</option></SelectField></div></Card>
    {members.isPending ? <LoadingState label="جارٍ تحميل الأعضاء…" /> : members.isError ? <ErrorState message={safeError(members.error)} /> : <Card title="قائمة الأعضاء"><div className="lf-table-wrap"><table className="lf-table"><thead><tr><th>الاسم</th><th>Member Code</th><th>الهاتف</th><th>الحالة</th><th>آخر تحديث</th><th /></tr></thead><tbody>{members.data.data.length === 0 ? <tr><td colSpan={6}><EmptyState title="لا توجد أعضاء" message="غيّر البحث أو أنشئ عضوًا جديدًا من النموذج المصرح به." /></td></tr> : members.data.data.map((member) => <tr key={member.memberId}><td><strong>{member.fullName}</strong><br /><span className="lf-muted">{member.email ?? '—'}</span></td><td><code>{member.memberCode}</code></td><td>{member.phone}</td><td><span className={`lf-status-pill ${member.status === 'ACTIVE' ? 'lf-status-success' : 'lf-status-neutral'}`}>{member.status}</span></td><td>{formatDate(member.updatedAtUtc)}</td><td><Button variant="secondary" onClick={() => navigate(`/app/members/${member.memberId}`)}>فتح الملف</Button></td></tr>)}</tbody></table></div><div className="lf-pagination"><span className="lf-muted">{members.data.meta.total} سجل · الصفحة {page}</span><div className="lf-action-row"><Button variant="secondary" disabled={page <= 1} onClick={() => setPage((current) => current - 1)}>السابق</Button><Button variant="secondary" disabled={!members.data.meta.hasNext} onClick={() => setPage((current) => current + 1)}>التالي</Button></div></div></Card>}
  </div>;
}

export function MemberDetailPage() {
  const { me } = useAuth();
  const { memberId = '' } = useParams();
  const navigate = useNavigate();
  const gymId = gymForMembers(me);
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState(false);
  const [archiveError, setArchiveError] = useState<string | null>(null);
  const member = useQuery({ queryKey: ['member', gymId, memberId], queryFn: () => apiClient.getMember(gymId!, memberId), enabled: Boolean(gymId && memberId), retry: false });
  const timeline = useQuery({ queryKey: ['member-timeline', gymId, memberId], queryFn: () => apiClient.memberTimeline(gymId!, memberId), enabled: Boolean(gymId && memberId), retry: false });
  const current = member.data?.data;
  const archive = useMutation({
    mutationFn: () => apiClient.archiveMember(gymId!, memberId, current?.version ?? ''),
    onSuccess: async () => { setArchiveError(null); await queryClient.invalidateQueries({ queryKey: ['member', gymId, memberId] }); await queryClient.invalidateQueries({ queryKey: ['member-timeline', gymId, memberId] }); },
    onError: (reason) => setArchiveError(safeError(reason)),
  });

  const canUpdate = me?.permissions.includes('members.update') ?? false;
  const canArchive = me?.permissions.includes('members.delete') ?? false;
  const timelineItems = useMemo(() => timeline.data?.data ?? [], [timeline.data]);
  if (!gymId) return <div className="lf-page-stack"><MemberAccessNotice /></div>;
  if (member.isPending) return <LoadingState label="جارٍ تحميل ملف العضو…" />;
  if (member.isError || !current) return <ErrorState message={safeError(member.error)} />;
  if (editing && canUpdate && current.status !== 'ARCHIVED') return <div className="lf-page-stack"><div className="lf-page-heading"><div><span className="lf-eyebrow">MEM-W-003 · EDIT</span><h1>تعديل عضو</h1></div><Button variant="secondary" onClick={() => setEditing(false)}>إلغاء</Button></div><Card title="الملف الأساسي"><MemberForm member={current} onSaved={() => { setEditing(false); void queryClient.invalidateQueries({ queryKey: ['member', gymId, memberId] }); }} /></Card></div>;

  return <div className="lf-page-stack">
    <div className="lf-page-heading"><div><span className="lf-eyebrow">MEM-W-002 · MEMBER DETAIL</span><h1>{current.fullName}</h1><p>ملف الهوية والحالة وسجل أحداث Member-domain فقط.</p></div><div className="lf-action-row"><Button variant="secondary" onClick={() => navigate('/app/members')}>العودة للقائمة</Button>{canUpdate && current.status !== 'ARCHIVED' ? <Button onClick={() => setEditing(true)}>تعديل</Button> : null}{canArchive && current.status !== 'ARCHIVED' ? <Button variant="danger" onClick={() => { if (window.confirm('هل تريد أرشفة هذا العضو؟')) archive.mutate(); }} disabled={archive.isPending}>أرشفة</Button> : null}</div></div>
    {archiveError ? <div className="lf-form-error" role="alert">{archiveError}</div> : null}
    <Card title="الملف الأساسي"><div className="lf-grid-3"><div><span className="lf-eyebrow">Member Code</span><div className="lf-metric">{current.memberCode}</div></div><div><span className="lf-eyebrow">الحالة</span><div className="lf-metric">{current.status}</div></div><div><span className="lf-eyebrow">الهاتف</span><div className="lf-metric">{current.phone}</div></div></div><div className="lf-form-grid" style={{ marginTop: '18px' }}><div><span className="lf-muted">البريد</span><p>{current.email ?? '—'}</p></div><div><span className="lf-muted">تاريخ التسجيل</span><p>{current.registrationDate}</p></div><div><span className="lf-muted">أنشئ في</span><p>{formatDate(current.createdAtUtc)}</p></div><div><span className="lf-muted">آخر تحديث</span><p>{formatDate(current.updatedAtUtc)}</p></div></div>{current.notes ? <div className="lf-boundary-note"><strong>ملاحظات</strong><span>{current.notes}</span></div> : null}</Card>
    <Card title="Timeline · Member-domain"><p className="lf-muted">الأحداث الأساسية فقط: إنشاء، تحديث، تغيير حالة، وأرشفة. لا توجد أحداث عضوية أو حضور أو دفع في هذه المرحلة.</p>{timeline.isPending ? <LoadingState label="جارٍ تحميل السجل…" /> : timeline.isError ? <ErrorState message={safeError(timeline.error)} /> : timelineItems.length === 0 ? <EmptyState title="لا توجد أحداث" /> : <div className="lf-provisioning-step-list">{timelineItems.map((item) => <div className="lf-provisioning-step" key={item.eventId}><div><strong>{item.eventType}</strong><br /><span className="lf-muted">{formatDate(item.occurredAt)}</span></div><code>{Object.entries(item.metadata).map(([key, value]) => `${key}: ${value ?? '—'}`).join(' · ')}</code></div>)}</div>}</Card>
  </div>;
}
