"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { DashboardData, money, number, statusNames } from "./dashboard-types";

const colors = ["#023baa", "#167a8b", "#f79009", "#6941c6", "#98a2b3", "#d92d20"];
const widgetDefaults = { kpis: true, charts: true, orders: true };

function chartPoints(values: number[], width = 700, height = 200) {
  const maximum = Math.max(...values, 1);
  return values.map((value, index) => {
    const x = values.length === 1 ? width / 2 : index * width / (values.length - 1);
    const y = height - value / maximum * (height - 25);
    return `${x.toFixed(1)},${y.toFixed(1)}`;
  }).join(" ");
}

export function DashboardClient() {
  const [days, setDays] = useState(7);
  const [data, setData] = useState<DashboardData | null>(null);
  const [error, setError] = useState("");
  const [customize, setCustomize] = useState(false);
  const [widgets, setWidgets] = useState(() => {
    if (typeof window === "undefined") return widgetDefaults;
    const saved = window.localStorage.getItem("admin-dashboard-widgets");
    if (!saved) return widgetDefaults;
    try { return { ...widgetDefaults, ...JSON.parse(saved) }; } catch { return widgetDefaults; }
  });

  useEffect(() => {
    const controller = new AbortController();
    fetch(`/api/admin/dashboard?days=${days}`, { signal: controller.signal })
      .then(async (response) => {
        const body = await response.json();
        if (!response.ok) throw new Error(body.title || body.message || "تعذر تحميل لوحة المعلومات");
        setData(body);
      })
      .catch((reason) => { if (reason.name !== "AbortError") setError(reason.message); });
    return () => controller.abort();
  }, [days]);

  const donut = useMemo(() => {
    if (!data?.ordersByStatus.length) return "var(--gray-200)";
    const total = data.ordersByStatus.reduce((sum, item) => sum + item.count, 0);
    let cursor = 0;
    const stops = data.ordersByStatus.map((item, index) => {
      const start = cursor; cursor += item.count / total * 100;
      return `${colors[index % colors.length]} ${start}% ${cursor}%`;
    });
    return `conic-gradient(${stops.join(",")})`;
  }, [data]);

  function saveWidgets(next = widgets) {
    setWidgets(next); window.localStorage.setItem("admin-dashboard-widgets", JSON.stringify(next));
  }

  function chooseDays(value: number) { setError(""); setDays(value); }

  function exportCsv() {
    if (!data) return;
    const rows = [["رقم الطلب", "الشركة", "التاريخ", "الإجمالي", "الحالة"], ...data.recentOrders.map((order) =>
      [order.number, order.company, order.createdAt, String(order.total), statusNames[order.status] ?? order.status])];
    const csv = `\uFEFF${rows.map((row) => row.map((cell) => `"${cell.replaceAll('"', '""')}"`).join(",")).join("\n")}`;
    const url = URL.createObjectURL(new Blob([csv], { type: "text/csv;charset=utf-8" }));
    const anchor = document.createElement("a"); anchor.href = url; anchor.download = "mohandseto-dashboard.csv"; anchor.click();
    URL.revokeObjectURL(url);
  }

  if (!data && !error) return <div className="admin-loading"><span className="loading-ring" />جاري تحميل مؤشرات المنصة...</div>;
  if (error) return <div className="admin-empty"><div><b>تعذر تحميل لوحة المعلومات</b><p>{error}</p><button className="secondary-button" onClick={() => chooseDays(days === 7 ? 30 : 7)}>إعادة المحاولة</button></div></div>;
  if (!data) return null;

  const metrics = [
    ["إجمالي المبيعات", money.format(data.summary.totalSales.value), data.summary.totalSales, "أزرق", "◈"],
    ["الطلبات الجديدة", number.format(data.summary.newOrders.value), data.summary.newOrders, "أخضر", "▤"],
    ["عروض بانتظار الرد", number.format(data.summary.pendingQuotes.value), data.summary.pendingQuotes, "برتقالي", "◫"],
    ["الشركات النشطة", number.format(data.summary.activeCompanies.value), data.summary.activeCompanies, "بنفسجي", "◎"],
  ] as const;
  const salesPoints = chartPoints(data.salesTrend.map((point) => point.sales));
  const orderPoints = chartPoints(data.salesTrend.map((point) => point.orders));
  const totalOrders = data.ordersByStatus.reduce((sum, item) => sum + item.count, 0);

  return <>
    <div className="page-heading dashboard-heading">
      <div><p>{new Date(data.generatedAt).toLocaleDateString("ar-EG", { dateStyle: "full" })}</p><h1>مرحبًا، مدير النظام</h1><span>إليك ملخص أداء المنصة خلال آخر {number.format(days)} أيام</span></div>
      <div className="heading-actions"><Link className="secondary-button action-link" href="/dashboard/analytics">التحليلات المتقدمة</Link><button className="secondary-button" onClick={() => setCustomize(true)}>تخصيص اللوحة</button><button className="primary-button" onClick={exportCsv}>تصدير التقرير</button></div>
    </div>
    <div className="period-tabs" aria-label="الفترة الزمنية">{[7, 30, 90].map((value) => <button key={value} className={days === value ? "active" : ""} onClick={() => chooseDays(value)}>آخر {value} يوم</button>)}</div>

    {widgets.kpis && <section className="kpi-grid">{metrics.map(([label, value, metric, color, icon]) =>
      <article className="kpi-card" key={label}><div className={`kpi-icon ${color}`}>{icon}</div><div><p>{label}</p><strong>{value}</strong><small className={metric.changePercent >= 0 ? "up" : "down"}>{metric.changePercent >= 0 ? "+" : ""}{metric.changePercent}% <span>عن الفترة السابقة</span></small></div></article>)}</section>}

    {widgets.charts && <section className="chart-grid">
      <article className="panel sales-panel"><div className="panel-heading"><div><h2>المبيعات والطلبات</h2><p>أداء الفترة المحددة من البيانات الفعلية</p></div><span className="live-chip">بيانات حية</span></div>
        <div className="chart-legend"><span><i className="teal-dot" /> المبيعات</span><span><i className="blue-dot" /> الطلبات</span></div>
        <div className="line-chart"><svg viewBox="0 0 700 220" role="img" aria-label="رسم المبيعات والطلبات"><path className="grid-line" d="M0 35H700M0 85H700M0 135H700M0 185H700"/><polyline className="sales-line" points={salesPoints}/><polyline className="order-line" points={orderPoints}/></svg></div>
        <div className="chart-days">{data.salesTrend.filter((_, index) => days === 7 || index % Math.ceil(days / 7) === 0).map((point) => <span key={point.date}>{new Date(point.date).toLocaleDateString("ar-EG", { day: "numeric", month: "short" })}</span>)}</div>
      </article>
      <article className="panel status-panel"><div className="panel-heading"><div><h2>حالة الطلبات</h2><p>توزيع الطلبات خلال الفترة</p></div></div>
        <div className="donut dynamic-donut" style={{ background: donut }}><div><strong>{number.format(totalOrders)}</strong><span>إجمالي الطلبات</span></div></div>
        <ul>{data.ordersByStatus.slice(0, 6).map((item, index) => <li key={item.status}><i style={{ background: colors[index % colors.length] }} /> {statusNames[item.status] ?? item.status}<b>{number.format(item.count)}</b><span>{totalOrders ? Math.round(item.count / totalOrders * 100) : 0}%</span></li>)}</ul>
      </article>
    </section>}

    {widgets.orders && <section className="panel orders-panel"><div className="panel-heading"><div><h2>أحدث الطلبات</h2><p>آخر الطلبات الواردة إلى المنصة</p></div><Link className="text-button" href="/dashboard/orders">عرض كل الطلبات ←</Link></div>
      {data.recentOrders.length ? <div className="table-wrap"><table><thead><tr><th>رقم الطلب</th><th>الشركة</th><th>التاريخ</th><th>الإجمالي</th><th>الحالة</th></tr></thead><tbody>{data.recentOrders.map((order) => <tr key={order.id}><td>{order.number}</td><td>{order.company}</td><td>{new Date(order.createdAt).toLocaleDateString("ar-EG")}</td><td>{money.format(order.total)}</td><td><span className={`status-chip status-${order.status.toLowerCase()}`}>{statusNames[order.status] ?? order.status}</span></td></tr>)}</tbody></table></div> : <div className="admin-empty compact">لا توجد طلبات في الفترة المحددة</div>}
    </section>}

    {customize && <div className="modal-backdrop" role="dialog" aria-modal="true" aria-labelledby="customize-title"><section className="mini-editor dashboard-customizer"><header><h2 id="customize-title">تخصيص لوحة المعلومات</h2><button onClick={() => setCustomize(false)} aria-label="إغلاق">×</button></header><div className="customizer-body"><p>اختر الأقسام التي تريد ظهورها في لوحة معلوماتك.</p>{([['kpis','بطاقات المؤشرات'],['charts','الرسوم والتحليلات'],['orders','أحدث الطلبات']] as const).map(([key,label]) => <label key={key}><input type="checkbox" checked={widgets[key]} onChange={(event) => saveWidgets({ ...widgets, [key]: event.target.checked })}/><span><b>{label}</b><small>يمكنك إعادة إظهاره في أي وقت</small></span></label>)}</div><footer><button className="secondary-button" onClick={() => saveWidgets(widgetDefaults)}>استعادة الافتراضي</button><button className="primary-button" onClick={() => setCustomize(false)}>حفظ وإغلاق</button></footer></section></div>}
  </>;
}
