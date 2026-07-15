"use client";

import { type FormEvent, useEffect, useState } from "react";
import base from "../operations.module.css";
import styles from "./monitoring.module.css";
import type {
  Backup,
  BlockedIp,
  Dashboard,
  ErrorEvent,
  Flag,
  Point,
  Suspicious,
} from "./monitoring-types";

type View =
  | "health"
  | "services"
  | "database"
  | "storage"
  | "queues"
  | "errors"
  | "error-detail"
  | "failed"
  | "suspicious"
  | "blocked"
  | "backups"
  | "versions"
  | "flags";
type Modal = "block" | "restore" | "flag" | "version" | null;
const tabs: [View, string][] = [
  ["health", "صحة النظام"],
  ["services", "الخدمات"],
  ["database", "قاعدة البيانات"],
  ["storage", "التخزين"],
  ["queues", "الطوابير"],
  ["errors", "الأخطاء"],
  ["failed", "الدخول الفاشل"],
  ["suspicious", "أنشطة مشبوهة"],
  ["blocked", "IP محظور"],
  ["backups", "النسخ الاحتياطية"],
  ["versions", "الإصدارات"],
  ["flags", "Feature Flags"],
];
const date = new Intl.DateTimeFormat("ar-EG", {
  dateStyle: "medium",
  timeStyle: "short",
});
const num = new Intl.NumberFormat("ar-EG", { maximumFractionDigits: 1 });
async function api<T>(url: string, init?: RequestInit): Promise<T> {
  const r = await fetch(url, { ...init, cache: "no-store" });
  if (!r.ok) {
    const b = await r.json().catch(() => ({}));
    throw new Error(b.title || b.message || "تعذر تنفيذ العملية");
  }
  return r.status === 204 ? (undefined as T) : r.json();
}
const send = (method: string, body?: unknown): RequestInit => ({
  method,
  headers:
    body === undefined ? undefined : { "Content-Type": "application/json" },
  body: body === undefined ? undefined : JSON.stringify(body),
});

export function MonitoringManager() {
  const [data, setData] = useState<Dashboard | null>(null),
    [view, setView] = useState<View>("health"),
    [selected, setSelected] = useState<ErrorEvent | null>(null),
    [modal, setModal] = useState<Modal>(null),
    [restore, setRestore] = useState<Backup | null>(null),
    [editFlag, setEditFlag] = useState<Flag | null>(null),
    [busy, setBusy] = useState(false),
    [error, setError] = useState(""),
    [notice, setNotice] = useState("");
  const load = async () => setData(await api("/api/admin/monitoring"));
  useEffect(() => {
    let live = true;
    api<Dashboard>("/api/admin/monitoring")
      .then((x) => live && setData(x))
      .catch((e) => live && setError((e as Error).message));
    const timer = setInterval(
      () =>
        api<Dashboard>("/api/admin/monitoring")
          .then((x) => live && setData(x))
          .catch(() => {}),
      30000,
    );
    return () => {
      live = false;
      clearInterval(timer);
    };
  }, []);
  async function run(work: () => Promise<unknown>, message: string) {
    setBusy(true);
    setError("");
    try {
      await work();
      await load();
      setNotice(message);
      setTimeout(() => setNotice(""), 2800);
      return true;
    } catch (e) {
      setError((e as Error).message);
      return false;
    } finally {
      setBusy(false);
    }
  }
  async function openError(row: ErrorEvent) {
    setBusy(true);
    try {
      setSelected(await api(`/api/admin/monitoring/errors/${row.id}`));
      setView("error-detail");
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  function choose(next: View) {
    setSelected(null);
    setView(next);
  }
  if (!data)
    return (
      <p className={base.empty}>
        {error || "جاري تحميل مركز مراقبة النظام..."}
      </p>
    );
  return (
    <section className={styles.page}>
      <header className={styles.hero}>
        <div>
          <span>النظام / مراقبة النظام وأمانه</span>
          <h1>{tabs.find((x) => x[0] === view)?.[1] || "تفاصيل الخطأ"}</h1>
          <p>
            قراءات تشغيلية حقيقية · آخر تحديث{" "}
            {date.format(new Date(data.health.checkedAt))}
          </p>
        </div>
        <nav>
          <button onClick={() => void load()}>↻ تحديث</button>
          <i
            className={
              data.services.every((x) => x.status === "Healthy")
                ? styles.live
                : styles.warn
            }
          >
            {data.services.every((x) => x.status === "Healthy")
              ? "كل الأنظمة سليمة"
              : "توجد تنبيهات"}
          </i>
        </nav>
      </header>
      <nav className={styles.tabs}>
        {tabs.map(([key, label]) => (
          <button
            key={key}
            className={
              (view === "error-detail" ? "errors" : view) === key
                ? styles.active
                : ""
            }
            onClick={() => choose(key)}
          >
            {label}
          </button>
        ))}
      </nav>
      {view === "health" && <HealthView data={data} />}{" "}
      {view === "services" && <ServicesView data={data} />}{" "}
      {view === "database" && <DatabaseView data={data} />}{" "}
      {view === "storage" && <StorageView data={data} />}{" "}
      {view === "queues" && <QueuesView data={data} />}
      {view === "errors" && (
        <ErrorsView
          data={data}
          open={(x) => void openError(x)}
          exportCsv={() => exportErrors(data.errors.items)}
        />
      )}{" "}
      {view === "error-detail" && selected && (
        <ErrorDetail
          row={selected}
          back={() => choose("errors")}
          resolve={async () => {
            const note = prompt("ملاحظة المعالجة (اختياري)") || null;
            if (
              await run(
                () =>
                  api(
                    `/api/admin/monitoring/errors/${selected.id}/resolve`,
                    send("POST", { note }),
                  ),
                "تم تسجيل معالجة الخطأ",
              )
            ) {
              setSelected(
                await api(`/api/admin/monitoring/errors/${selected.id}`),
              );
            }
          }}
        />
      )}
      {view === "failed" && (
        <FailedView
          data={data}
          block={(x) => {
            setRestore(null);
            setEditFlag(null);
            setModal("block");
            sessionStorage.setItem("monitoring-block", JSON.stringify(x));
          }}
        />
      )}{" "}
      {view === "suspicious" && (
        <SuspiciousView
          rows={data.suspiciousActivities}
          review={(x, action) =>
            void run(
              () =>
                api(
                  `/api/admin/monitoring/security/activities/${x.id}/${action}`,
                  send("POST", { note: null }),
                ),
              action === "ignore" ? "تم تجاهل التنبيه" : "تم فتح التحقيق",
            )
          }
        />
      )}{" "}
      {view === "blocked" && (
        <BlockedView
          rows={data.blockedIps}
          add={() => {
            sessionStorage.removeItem("monitoring-block");
            setModal("block");
          }}
          unblock={(x) =>
            void (
              confirm(`رفع الحظر عن ${x.ipAddress}؟`) &&
              run(
                () =>
                  api(
                    `/api/admin/monitoring/security/blocked-ips/${x.id}`,
                    send("DELETE"),
                  ),
                "تم رفع الحظر",
              )
            )
          }
        />
      )}
      {view === "backups" && (
        <BackupsView
          data={data}
          create={() =>
            void run(
              () => api("/api/admin/monitoring/backups", send("POST")),
              "اكتملت النسخة الاحتياطية",
            )
          }
          restore={(x) => {
            setRestore(x);
            setModal("restore");
          }}
        />
      )}{" "}
      {view === "versions" && (
        <VersionsView data={data} add={() => setModal("version")} />
      )}{" "}
      {view === "flags" && (
        <FlagsView
          data={data}
          add={() => {
            setEditFlag(null);
            setModal("flag");
          }}
          edit={(x) => {
            setEditFlag(x);
            setModal("flag");
          }}
          toggle={(x) => void saveFlag(x, { isEnabled: !x.isEnabled })}
          remove={(x) =>
            void (
              confirm(`حذف الميزة ${x.nameAr}؟`) &&
              run(
                () =>
                  api(
                    `/api/admin/monitoring/feature-flags/${x.id}`,
                    send("DELETE"),
                  ),
                "تم حذف الميزة",
              )
            )
          }
        />
      )}
      {modal && (
        <ModalForm
          type={modal}
          backup={restore}
          flag={editFlag}
          close={() => setModal(null)}
          submit={submitModal}
        />
      )}{" "}
      {busy && <div className={styles.busy}>جاري التنفيذ...</div>}
      {error && (
        <div className={base.alert} onClick={() => setError("")}>
          {error}
        </div>
      )}
      {notice && <div className={styles.notice}>✓ {notice}</div>}
    </section>
  );
  async function saveFlag(flag: Flag, patch: Partial<Flag>) {
    await run(
      () =>
        api(
          `/api/admin/monitoring/feature-flags/${flag.id}`,
          send("PUT", {
            key: flag.key,
            nameAr: flag.nameAr,
            descriptionAr: flag.descriptionAr,
            isEnabled: patch.isEnabled ?? flag.isEnabled,
            scope: flag.scope,
            rolloutPercent: flag.rolloutPercent,
            targetTenantIds: flag.targetTenantIds,
            targetUserIds: flag.targetUserIds,
            startsAt: flag.startsAt,
            endsAt: flag.endsAt,
          }),
        ),
      "تم تحديث حالة الميزة",
    );
  }
  async function submitModal(values: Record<string, unknown>) {
    let ok = false;
    if (modal === "block")
      ok = await run(
        () =>
          api(
            "/api/admin/monitoring/security/blocked-ips",
            send("POST", values),
          ),
        "تم تفعيل حظر عنوان IP",
      );
    if (modal === "restore" && restore)
      ok = await run(
        () =>
          api(
            `/api/admin/monitoring/backups/${restore.id}/restore-requests`,
            send("POST", values),
          ),
        "تم التحقق من النسخة وجدولة الاستعادة عند إعادة التشغيل الآمن",
      );
    if (modal === "version") {
      values.isStable = true;
      ok = await run(
        () => api("/api/admin/monitoring/versions", send("POST", values)),
        "تم تسجيل الإصدار",
      );
    }
    if (modal === "flag")
      ok = await run(
        () =>
          api(
            editFlag
              ? `/api/admin/monitoring/feature-flags/${editFlag.id}`
              : "/api/admin/monitoring/feature-flags",
            send(editFlag ? "PUT" : "POST", values),
          ),
        "تم حفظ الميزة",
      );
    if (ok) setModal(null);
  }
}

function HealthView({ data }: { data: Dashboard }) {
  const h = data.health;
  return (
    <>
      <section className={styles.kpis}>
        <Kpi icon="⌁" label="وقت التشغيل" value={`${h.uptimePercent}%`} />
        <Kpi
          icon="ϟ"
          label="زمن الاستجابة"
          value={`${num.format(h.averageResponseMs)} ms`}
        />
        <Kpi
          icon="♧"
          label="المستخدمون النشطون"
          value={num.format(h.activeUsers)}
        />
        <Kpi
          icon="↗"
          label="الطلبات/دقيقة"
          value={num.format(h.requestsPerMinute)}
        />
        <Kpi
          icon="△"
          label="أخطاء 24 ساعة"
          value={num.format(h.errors24Hours)}
        />
      </section>
      <div className={styles.split}>
        <Panel title="زمن الاستجابة — 24 ساعة" sub="متوسط القياسات كل 10 دقائق">
          <Chart points={h.responseTrend} />
        </Panel>
        <Panel title="حالة الخدمات" sub="فحص لحظي">
          {data.services.map((x) => (
            <div className={styles.serviceMini} key={x.code}>
              <span>{x.nameAr}</span>
              <Status value={x.status} />
            </div>
          ))}
        </Panel>
      </div>
    </>
  );
}
function ServicesView({ data }: { data: Dashboard }) {
  return (
    <Panel title="حالة الخدمات" sub={`${data.services.length} خدمات مراقبة`}>
      <div className={base.table}>
        <table>
          <thead>
            <tr>
              <th>الخدمة</th>
              <th>الحالة</th>
              <th>وقت التشغيل</th>
              <th>الاستجابة</th>
              <th>آخر فحص</th>
              <th>ملاحظة</th>
            </tr>
          </thead>
          <tbody>
            {data.services.map((x) => (
              <tr key={x.code}>
                <td>
                  <b>{x.nameAr}</b>
                  <small>{x.code}</small>
                </td>
                <td>
                  <Status value={x.status} />
                </td>
                <td>{x.uptimePercent}%</td>
                <td>{num.format(x.responseMs)} ms</td>
                <td>{date.format(new Date(x.checkedAt))}</td>
                <td>{x.message || "—"}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </Panel>
  );
}
function DatabaseView({ data }: { data: Dashboard }) {
  const d = data.database;
  return (
    <>
      <section className={styles.kpis}>
        <Kpi
          icon="▤"
          label="الاتصالات النشطة"
          value={`${d.activeConnections}/${d.maxConnections}`}
        />
        <Kpi icon="ϟ" label="زمن الاستعلام" value={`${d.queryLatencyMs} ms`} />
        <Kpi icon="▥" label="حجم القاعدة" value={bytes(d.sizeBytes)} />
        <Kpi
          icon="◷"
          label="استعلامات بطيئة"
          value={num.format(d.slowQueries24Hours)}
        />
      </section>
      <div className={styles.split}>
        <Panel title="أداء قاعدة البيانات" sub={d.provider}>
          <Chart points={d.connectionTrend} />
        </Panel>
        <Panel title="ملخص الأداء">
          <dl className={styles.facts}>
            <div>
              <dt>الحالة</dt>
              <dd>
                <Status value={d.status} />
              </dd>
            </div>
            <div>
              <dt>معدل النجاح</dt>
              <dd>{d.successRate}%</dd>
            </div>
            <div>
              <dt>آخر نسخة</dt>
              <dd>
                {d.lastBackupAt
                  ? date.format(new Date(d.lastBackupAt))
                  : "لا توجد"}
              </dd>
            </div>
            <div>
              <dt>التخزين المتاح</dt>
              <dd>{bytes(d.availableStorageBytes)}</dd>
            </div>
          </dl>
        </Panel>
      </div>
    </>
  );
}
function StorageView({ data }: { data: Dashboard }) {
  const s = data.storage;
  return (
    <>
      <section className={styles.kpis}>
        <Kpi icon="▥" label="السعة" value={bytes(s.capacityBytes)} />
        <Kpi icon="◒" label="المستخدم" value={bytes(s.usedBytes)} />
        <Kpi icon="○" label="المتاح" value={bytes(s.availableBytes)} />
        <Kpi
          icon="!"
          label="حد التحذير"
          value={`${s.warningThresholdPercent}%`}
        />
      </section>
      <div className={styles.split}>
        <Panel title="استهلاك التخزين" sub={`${s.usagePercent}% مستخدم`}>
          <div className={styles.storageMeter}>
            <i style={{ width: `${Math.min(s.usagePercent, 100)}%` }} />
          </div>
          {s.categories.map((x) => (
            <div className={styles.storageRow} key={x.code}>
              <span>{x.nameAr}</span>
              <b>{bytes(x.bytes)}</b>
            </div>
          ))}
        </Panel>
        <Panel title="سياسة التخزين">
          <dl className={styles.facts}>
            <div>
              <dt>التوسع التلقائي</dt>
              <dd>{s.autoExpandEnabled ? "مفعل" : "معطل"}</dd>
            </div>
            <div>
              <dt>التنبيه</dt>
              <dd>
                {s.usagePercent >= s.warningThresholdPercent
                  ? "تجاوز الحد"
                  : "ضمن النطاق"}
              </dd>
            </div>
          </dl>
        </Panel>
      </div>
    </>
  );
}
function QueuesView({ data }: { data: Dashboard }) {
  return (
    <Panel title="مراقبة الطوابير" sub="المهام الخلفية الفعلية">
      <div className={base.table}>
        <table>
          <thead>
            <tr>
              <th>الطابور</th>
              <th>قيد الانتظار</th>
              <th>قيد المعالجة</th>
              <th>مكتملة اليوم</th>
              <th>فشل</th>
              <th>الحالة</th>
            </tr>
          </thead>
          <tbody>
            {data.queues.map((x) => (
              <tr key={x.code}>
                <td>
                  <b>{x.nameAr}</b>
                  <small>{x.code}</small>
                </td>
                <td>{x.waiting}</td>
                <td>{x.processing}</td>
                <td>{x.completedToday}</td>
                <td>{x.failed}</td>
                <td>
                  <Status value={x.status} />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </Panel>
  );
}
function ErrorsView({
  data,
  open,
  exportCsv,
}: {
  data: Dashboard;
  open: (x: ErrorEvent) => void;
  exportCsv: () => void;
}) {
  return (
    <Panel
      title="سجل الأخطاء"
      sub={`${data.errors.total} خطأ مسجل`}
      action={
        <button className={base.secondary} onClick={exportCsv}>
          تصدير CSV
        </button>
      }
    >
      <div className={base.table}>
        <table>
          <thead>
            <tr>
              <th>الوقت</th>
              <th>الخطورة</th>
              <th>الخدمة</th>
              <th>الرسالة</th>
              <th>التكرار</th>
              <th>الحالة</th>
            </tr>
          </thead>
          <tbody>
            {data.errors.items.map((x) => (
              <tr key={x.id} onClick={() => open(x)}>
                <td>
                  {date.format(new Date(x.lastOccurredAt))}
                  <small>{x.number}</small>
                </td>
                <td>
                  <Status value={x.severity} />
                </td>
                <td>{x.service}</td>
                <td>{x.message}</td>
                <td>{x.occurrenceCount}</td>
                <td>{x.resolvedAt ? "تمت المعالجة" : "مفتوح"}</td>
              </tr>
            ))}
          </tbody>
        </table>
        {!data.errors.items.length && (
          <p className={base.empty}>
            لا توجد أخطاء مسجلة — النظام يعمل بصورة مستقرة.
          </p>
        )}
      </div>
    </Panel>
  );
}
function ErrorDetail({
  row,
  back,
  resolve,
}: {
  row: ErrorEvent;
  back: () => void;
  resolve: () => void;
}) {
  return (
    <>
      <button className={base.back} onClick={back}>
        → تفاصيل الخطأ {row.number}
      </button>
      <div className={styles.errorGrid}>
        <Panel title="التتبع" sub={row.exceptionType || row.service}>
          <pre className={styles.trace} dir="ltr">
            {row.stackTrace || row.message}
          </pre>
        </Panel>
        <Panel
          title="السياق"
          action={
            !row.resolvedAt ? (
              <button className={base.primary} onClick={resolve}>
                تحديد كمعالج
              </button>
            ) : (
              <Status value="Resolved" />
            )
          }
        >
          <dl className={styles.facts}>
            <div>
              <dt>الوقت</dt>
              <dd>{date.format(new Date(row.lastOccurredAt))}</dd>
            </div>
            <div>
              <dt>المسار</dt>
              <dd dir="ltr">{row.path || "—"}</dd>
            </div>
            <div>
              <dt>Correlation ID</dt>
              <dd dir="ltr">{row.correlationId || "—"}</dd>
            </div>
            <div>
              <dt>التكرار</dt>
              <dd>{row.occurrenceCount}</dd>
            </div>
            <div>
              <dt>المستخدم</dt>
              <dd>{row.userId || "غير محدد"}</dd>
            </div>
            <div>
              <dt>المعالجة</dt>
              <dd>{row.resolutionNote || "—"}</dd>
            </div>
          </dl>
        </Panel>
      </div>
    </>
  );
}
function FailedView({
  data,
  block,
}: {
  data: Dashboard;
  block: (x: Dashboard["failedLogins"][number]) => void;
}) {
  return (
    <Panel title="محاولات تسجيل الدخول الفاشلة" sub="آخر 24 ساعة">
      <div className={base.table}>
        <table>
          <thead>
            <tr>
              <th>آخر محاولة</th>
              <th>المستخدم</th>
              <th>IP</th>
              <th>الموقع</th>
              <th>المحاولات</th>
              <th>السبب</th>
              <th>إجراء</th>
            </tr>
          </thead>
          <tbody>
            {data.failedLogins.map((x) => (
              <tr key={`${x.identifier}-${x.ipAddress}`}>
                <td>{date.format(new Date(x.lastAttemptAt))}</td>
                <td>{x.identifier}</td>
                <td dir="ltr">{x.ipAddress}</td>
                <td>{x.location || "غير معروف"}</td>
                <td>{x.attempts}</td>
                <td>{x.failureReason || "بيانات غير صحيحة"}</td>
                <td>
                  {x.isBlocked ? (
                    <Status value="Blocked" />
                  ) : (
                    <button className={base.danger} onClick={() => block(x)}>
                      حظر IP
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        {!data.failedLogins.length && (
          <p className={base.empty}>لا توجد محاولات فاشلة خلال 24 ساعة.</p>
        )}
      </div>
    </Panel>
  );
}
function SuspiciousView({
  rows,
  review,
}: {
  rows: Suspicious[];
  review: (x: Suspicious, a: "investigate" | "ignore") => void;
}) {
  return (
    <div className={styles.activityList}>
      {rows.map((x) => (
        <article key={x.id} className={styles[x.severity.toLowerCase()]}>
          <i>△</i>
          <div>
            <h3>{x.titleAr}</h3>
            <p>{x.descriptionAr}</p>
            <small>
              {x.ipAddress} · {date.format(new Date(x.detectedAt))}
            </small>
          </div>
          <Status value={x.status} />
          {x.status === "Open" && (
            <footer>
              <button
                className={base.primary}
                onClick={() => review(x, "investigate")}
              >
                تحقيق
              </button>
              <button
                className={base.secondary}
                onClick={() => review(x, "ignore")}
              >
                تجاهل
              </button>
            </footer>
          )}
        </article>
      ))}
      {!rows.length && (
        <p className={base.empty}>لا توجد أنشطة مشبوهة مفتوحة.</p>
      )}
    </div>
  );
}
function BlockedView({
  rows,
  add,
  unblock,
}: {
  rows: BlockedIp[];
  add: () => void;
  unblock: (x: BlockedIp) => void;
}) {
  return (
    <Panel
      title="حظر عناوين IP"
      sub={`${rows.filter((x) => x.isActive).length} عناوين محظورة`}
      action={
        <button className={base.danger} onClick={add}>
          + حظر IP
        </button>
      }
    >
      <div className={base.table}>
        <table>
          <thead>
            <tr>
              <th>العنوان</th>
              <th>السبب</th>
              <th>الموقع</th>
              <th>محظور منذ</th>
              <th>المحاولات</th>
              <th>الحالة</th>
              <th>إجراء</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((x) => (
              <tr key={x.id}>
                <td dir="ltr">{x.ipAddress}</td>
                <td>{x.reason}</td>
                <td>{x.location || "غير معروف"}</td>
                <td>{date.format(new Date(x.blockedAt))}</td>
                <td>{x.failedAttempts}</td>
                <td>
                  <Status value={x.isActive ? "Blocked" : "Unblocked"} />
                </td>
                <td>
                  {x.isActive && (
                    <button
                      className={base.secondary}
                      onClick={() => unblock(x)}
                    >
                      رفع الحظر
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </Panel>
  );
}
function BackupsView({
  data,
  create,
  restore,
}: {
  data: Dashboard;
  create: () => void;
  restore: (x: Backup) => void;
}) {
  return (
    <>
      <section className={styles.kpis}>
        <Kpi
          icon="✓"
          label="آخر نسخة"
          value={
            data.backups.latestAt
              ? date.format(new Date(data.backups.latestAt))
              : "لا توجد"
          }
        />
        <Kpi
          icon="▥"
          label="الحجم الإجمالي"
          value={bytes(data.backups.totalSizeBytes)}
        />
        <Kpi
          icon="▧"
          label="النسخ المحفوظة"
          value={String(data.backups.retainedCount)}
        />
        <Kpi icon="◷" label="التكرار" value={data.backups.scheduleAr} />
      </section>
      <Panel
        title="النسخ الاحتياطية"
        action={
          <button className={base.primary} onClick={create}>
            نسخ الآن
          </button>
        }
      >
        <div className={base.table}>
          <table>
            <thead>
              <tr>
                <th>التاريخ</th>
                <th>النوع</th>
                <th>الحجم</th>
                <th>الحالة</th>
                <th>SHA-256</th>
                <th>إجراء</th>
              </tr>
            </thead>
            <tbody>
              {data.backups.items.map((x) => (
                <tr key={x.id}>
                  <td>
                    {date.format(new Date(x.startedAt))}
                    <small>{x.fileName}</small>
                  </td>
                  <td>{x.isAutomatic ? "تلقائي" : "يدوي"}</td>
                  <td>{bytes(x.sizeBytes)}</td>
                  <td>
                    <Status value={x.status} />
                  </td>
                  <td dir="ltr">
                    <small>{x.sha256?.slice(0, 16) || "—"}</small>
                  </td>
                  <td>
                    <button
                      className={base.secondary}
                      disabled={x.status !== "Completed"}
                      onClick={() => restore(x)}
                    >
                      استعادة
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </Panel>
    </>
  );
}
function VersionsView({ data, add }: { data: Dashboard; add: () => void }) {
  return (
    <Panel
      title="سجل إصدارات النظام"
      sub="تاريخ النشر البرمجي"
      action={
        <button className={base.primary} onClick={add}>
          + تسجيل إصدار
        </button>
      }
    >
      <div className={styles.versionList}>
        {data.versions.map((x) => (
          <article key={x.id}>
            <i>{x.version}</i>
            <div>
              <h3>{x.titleAr}</h3>
              <p>{x.notesAr || "بدون ملاحظات إصدار"}</p>
              <small>
                {x.environment} · {x.commitSha || "بدون commit"}
              </small>
            </div>
            <Status value={x.isStable ? "Stable" : "Preview"} />
            <time>{date.format(new Date(x.releasedAt))}</time>
          </article>
        ))}
      </div>
    </Panel>
  );
}
function FlagsView({
  data,
  add,
  edit,
  toggle,
  remove,
}: {
  data: Dashboard;
  add: () => void;
  edit: (x: Flag) => void;
  toggle: (x: Flag) => void;
  remove: (x: Flag) => void;
}) {
  return (
    <Panel
      title="Feature Flags"
      sub="إطلاق الميزات دون نشر جديد"
      action={
        <button className={base.primary} onClick={add}>
          + flag
        </button>
      }
    >
      <div className={base.table}>
        <table>
          <thead>
            <tr>
              <th>الميزة</th>
              <th>الوصف</th>
              <th>النطاق</th>
              <th>الحالة</th>
              <th>إجراء</th>
            </tr>
          </thead>
          <tbody>
            {data.featureFlags.map((x) => (
              <tr key={x.id}>
                <td>
                  <b>{x.nameAr}</b>
                  <small dir="ltr">{x.key}</small>
                </td>
                <td>{x.descriptionAr}</td>
                <td>{scope(x)}</td>
                <td>
                  <button
                    className={`${styles.switch} ${x.isEnabled ? styles.on : ""}`}
                    aria-label="تبديل حالة الميزة"
                    onClick={() => toggle(x)}
                  >
                    <i />
                  </button>
                </td>
                <td>
                  <button className={base.secondary} onClick={() => edit(x)}>
                    تعديل
                  </button>{" "}
                  <button className={base.danger} onClick={() => remove(x)}>
                    حذف
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        {!data.featureFlags.length && (
          <p className={base.empty}>
            لا توجد Feature Flags. أضف أول ميزة تدريجية.
          </p>
        )}
      </div>
    </Panel>
  );
}

function ModalForm({
  type,
  backup,
  flag,
  close,
  submit,
}: {
  type: Exclude<Modal, null>;
  backup: Backup | null;
  flag: Flag | null;
  close: () => void;
  submit: (x: Record<string, unknown>) => void;
}) {
  function done(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const f = new FormData(e.currentTarget),
      v: Record<string, unknown> = Object.fromEntries(f);
    if (type === "flag") {
      v.isEnabled = f.get("isEnabled") === "on";
      v.rolloutPercent = Number(v.rolloutPercent);
      v.targetTenantIds = String(v.targetTenantIds || "")
        .split(",")
        .filter(Boolean);
      v.targetUserIds = String(v.targetUserIds || "")
        .split(",")
        .filter(Boolean);
    }
    if (type === "block") v.failedAttempts = Number(v.failedAttempts || 0);
    submit(v);
  }
  const savedBlock =
    typeof window !== "undefined"
      ? JSON.parse(sessionStorage.getItem("monitoring-block") || "null")
      : null;
  return (
    <div className={base.modalBack}>
      <section className={base.modal}>
        <header>
          <h2>
            {type === "block"
              ? "حظر عنوان IP"
              : type === "restore"
                ? "استعادة نسخة احتياطية"
                : type === "flag"
                  ? "إعداد Feature Flag"
                  : "تسجيل إصدار"}
          </h2>
          <button onClick={close}>×</button>
        </header>
        <form className={base.form} onSubmit={done}>
          {type === "block" && (
            <>
              <label>
                عنوان IP
                <input
                  name="ipAddress"
                  dir="ltr"
                  required
                  defaultValue={savedBlock?.ipAddress}
                />
              </label>
              <label>
                السبب
                <textarea
                  name="reason"
                  required
                  minLength={8}
                  defaultValue={
                    savedBlock
                      ? `محاولات دخول فاشلة (${savedBlock.attempts})`
                      : ""
                  }
                />
              </label>
              <div className={base.cols}>
                <label>
                  الموقع
                  <input name="location" defaultValue={savedBlock?.location} />
                </label>
                <label>
                  عدد المحاولات
                  <input
                    name="failedAttempts"
                    type="number"
                    min="0"
                    defaultValue={savedBlock?.attempts || 0}
                  />
                </label>
              </div>
            </>
          )}
          {type === "restore" && (
            <>
              <p className={styles.restoreWarning}>
                الاستعادة إجراء حساس. سيتم التحقق من الملف وجدولتها لإعادة تشغيل
                صيانة آمنة، ولن تُستبدل قاعدة البيانات أثناء عمل الخدمة.
              </p>
              <label>
                النسخة
                <input disabled value={backup?.fileName || ""} />
              </label>
              <label>
                البيئة
                <select name="environment">
                  <option value="production">الإنتاج</option>
                  <option value="staging">Staging</option>
                </select>
              </label>
              <label>
                سبب الاستعادة
                <textarea name="reason" required minLength={10} />
              </label>
              <label>
                اكتب RESTORE للتأكيد
                <input
                  name="confirmation"
                  required
                  pattern="RESTORE"
                  dir="ltr"
                />
              </label>
            </>
          )}
          {type === "flag" && (
            <>
              <div className={base.cols}>
                <label>
                  المفتاح
                  <input
                    name="key"
                    dir="ltr"
                    required
                    defaultValue={flag?.key}
                  />
                </label>
                <label>
                  اسم الميزة
                  <input name="nameAr" required defaultValue={flag?.nameAr} />
                </label>
              </div>
              <label>
                الوصف
                <textarea
                  name="descriptionAr"
                  required
                  defaultValue={flag?.descriptionAr}
                />
              </label>
              <div className={base.cols}>
                <label>
                  النطاق
                  <select name="scope" defaultValue={flag?.scope || "AllUsers"}>
                    <option value="AllUsers">كل المستخدمين</option>
                    <option value="Percentage">نسبة تجريبية</option>
                    <option value="Tenant">شركات محددة</option>
                    <option value="User">مستخدمون محددون</option>
                  </select>
                </label>
                <label>
                  نسبة الإطلاق
                  <input
                    name="rolloutPercent"
                    type="number"
                    min="0"
                    max="100"
                    defaultValue={flag?.rolloutPercent ?? 100}
                  />
                </label>
              </div>
              <label>
                Tenant IDs مفصولة بفاصلة
                <input
                  name="targetTenantIds"
                  dir="ltr"
                  defaultValue={flag?.targetTenantIds.join(",")}
                />
              </label>
              <label>
                User IDs مفصولة بفاصلة
                <input
                  name="targetUserIds"
                  dir="ltr"
                  defaultValue={flag?.targetUserIds.join(",")}
                />
              </label>
              <label className={styles.checkbox}>
                <input
                  name="isEnabled"
                  type="checkbox"
                  defaultChecked={flag?.isEnabled}
                />{" "}
                مفعلة
              </label>
            </>
          )}
          {type === "version" && (
            <>
              <div className={base.cols}>
                <label>
                  الإصدار
                  <input
                    name="version"
                    dir="ltr"
                    required
                    placeholder="1.0.0"
                  />
                </label>
                <label>
                  البيئة
                  <input
                    name="environment"
                    required
                    defaultValue="production"
                  />
                </label>
              </div>
              <label>
                العنوان
                <input name="titleAr" required />
              </label>
              <label>
                ملاحظات الإصدار
                <textarea name="notesAr" />
              </label>
              <label>
                Commit SHA
                <input name="commitSha" dir="ltr" />
              </label>
              <input type="hidden" name="isStable" value="true" />
            </>
          )}
          <footer>
            <button type="button" className={base.secondary} onClick={close}>
              إلغاء
            </button>
            <button className={type === "restore" ? base.danger : base.primary}>
              تأكيد وحفظ
            </button>
          </footer>
        </form>
      </section>
    </div>
  );
}
function Panel({
  title,
  sub,
  action,
  children,
}: {
  title: string;
  sub?: string;
  action?: React.ReactNode;
  children: React.ReactNode;
}) {
  return (
    <section className={base.panel}>
      <header className={base.panelHead}>
        <div>
          <h2>{title}</h2>
          {sub && <span>{sub}</span>}
        </div>
        {action}
      </header>
      {children}
    </section>
  );
}
function Kpi({
  icon,
  label,
  value,
}: {
  icon: string;
  label: string;
  value: string;
}) {
  return (
    <article className={styles.kpi}>
      <i>{icon}</i>
      <span>{label}</span>
      <b>{value}</b>
    </article>
  );
}
function Status({ value }: { value: string }) {
  const bad = ["Down", "Critical", "Error", "Blocked", "Failed"].includes(
      value,
    ),
    warn = [
      "Degraded",
      "Delayed",
      "Warning",
      "High",
      "Medium",
      "Open",
      "Investigating",
      "Preview",
    ].includes(value);
  return (
    <u
      className={`${styles.status} ${bad ? styles.bad : warn ? styles.caution : styles.good}`}
    >
      {translate(value)}
    </u>
  );
}
function Chart({ points }: { points: Point[] }) {
  const values = points.length ? points.map((x) => x.value) : [0],
    max = Math.max(...values, 1),
    path = values
      .map(
        (v, i) =>
          `${i ? "L" : "M"} ${values.length === 1 ? 50 : (i * 100) / (values.length - 1)} ${90 - (v / max) * 70}`,
      )
      .join(" ");
  return (
    <div className={styles.chart}>
      <svg
        viewBox="0 0 100 100"
        preserveAspectRatio="none"
        aria-label="رسم زمني"
      >
        <path d={`${path} L 100 100 L 0 100 Z`} className={styles.area} />
        <path d={path} className={styles.line} />
      </svg>
      <span>
        {points.length
          ? `${num.format(values.at(-1) || 0)} ms الآن`
          : "تبدأ القياسات مع حركة الطلبات"}
      </span>
    </div>
  );
}
function bytes(value: number) {
  if (value <= 0) return "0 B";
  const units = ["B", "KB", "MB", "GB", "TB"],
    i = Math.min(Math.floor(Math.log(value) / Math.log(1024)), 4);
  return `${num.format(value / 1024 ** i)} ${units[i]}`;
}
function scope(x: Flag) {
  return x.scope === "AllUsers"
    ? "كل المستخدمين"
    : x.scope === "Percentage"
      ? `${x.rolloutPercent}% تجريبي`
      : x.scope === "Tenant"
        ? `${x.targetTenantIds.length} شركات`
        : `${x.targetUserIds.length} مستخدمين`;
}
function translate(x: string) {
  return (
    (
      {
        Healthy: "يعمل",
        Degraded: "متأثر",
        Down: "متوقف",
        Delayed: "متأخر",
        Information: "معلومة",
        Warning: "تحذير",
        Error: "خطأ",
        Critical: "حرج",
        Open: "مفتوح",
        Investigating: "قيد التحقيق",
        Ignored: "تم التجاهل",
        Resolved: "تمت المعالجة",
        Blocked: "محظور",
        Unblocked: "مرفوع",
        Completed: "ناجح",
        Failed: "فشل",
        Stable: "مستقر",
        Preview: "تجريبي",
      } as Record<string, string>
    )[x] || x
  );
}
function exportErrors(rows: ErrorEvent[]) {
  const csv =
      "\uFEFF" +
      [
        ["الوقت", "الرقم", "الخطورة", "الخدمة", "الرسالة", "التكرار"],
        ...rows.map((x) => [
          x.lastOccurredAt,
          x.number,
          x.severity,
          x.service,
          x.message,
          String(x.occurrenceCount),
        ]),
      ]
        .map((r) => r.map((v) => `"${v.replaceAll('"', '""')}"`).join(","))
        .join("\n"),
    url = URL.createObjectURL(
      new Blob([csv], { type: "text/csv;charset=utf-8" }),
    ),
    a = document.createElement("a");
  a.href = url;
  a.download = "system-errors.csv";
  a.click();
  URL.revokeObjectURL(url);
}
