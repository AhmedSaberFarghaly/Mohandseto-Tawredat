"use client";

import { useEffect, useState } from "react";
import { DashboardData, money, number, statusNames } from "../dashboard-types";

export function AnalyticsDashboard() {
  const [data, setData] = useState<DashboardData | null>(null);
  const [days, setDays] = useState(30);
  useEffect(() => {
    const controller = new AbortController();
    fetch(`/api/admin/dashboard?days=${days}`, { signal: controller.signal }).then((response) => response.ok ? response.json() : Promise.reject()).then(setData).catch(() => undefined);
    return () => controller.abort();
  }, [days]);
  if (!data) return <div className="admin-loading"><span className="loading-ring" />جاري تجهيز التحليلات...</div>;
  const maxCompany = Math.max(...data.topCompanies.map((company) => company.sales), 1);
  const quoteTotal = data.quotesByStatus.reduce((sum, item) => sum + item.count, 0);
  return <>
    <div className="page-heading"><div><p>تقارير لحظية</p><h1>لوحة التحليلات المتقدمة</h1><span>قراءة تفصيلية لأداء المبيعات والطلبات وعروض الأسعار</span></div><select className="admin-select" value={days} onChange={(event) => setDays(Number(event.target.value))}><option value={7}>آخر 7 أيام</option><option value={30}>آخر 30 يومًا</option><option value={90}>آخر 90 يومًا</option></select></div>
    <section className="analytics-kpis"><article><span>متوسط قيمة الطلب</span><b>{money.format(data.summary.newOrders.value ? data.summary.totalSales.value / data.summary.newOrders.value : 0)}</b></article><article><span>عروض الأسعار النشطة</span><b>{number.format(data.summary.pendingQuotes.value)}</b></article><article><span>معدل التحول التقريبي</span><b>{quoteTotal ? Math.round(data.summary.newOrders.value / quoteTotal * 100) : 0}%</b></article></section>
    <section className="analytics-grid">
      <article className="panel"><div className="panel-heading"><div><h2>أفضل الشركات حسب المبيعات</h2><p>القيمة وعدد الطلبات في الفترة المحددة</p></div></div><div className="ranking-list">{data.topCompanies.length ? data.topCompanies.map((company, index) => <div key={company.tenantId}><span>{index + 1}</span><div><b>{company.company}</b><i><em style={{ width: `${company.sales / maxCompany * 100}%` }} /></i></div><strong>{money.format(company.sales)}<small>{number.format(company.orders)} طلب</small></strong></div>) : <p className="admin-empty compact">لا توجد بيانات مبيعات</p>}</div></article>
      <article className="panel"><div className="panel-heading"><div><h2>مراحل عروض الأسعار</h2><p>توزيع الحالات الحالية</p></div></div><div className="status-bars">{data.quotesByStatus.map((item) => <div key={item.status}><span>{statusNames[item.status] ?? item.status}</span><i><em style={{ width: `${quoteTotal ? item.count / quoteTotal * 100 : 0}%` }}/></i><b>{item.count}</b></div>)}</div></article>
    </section>
  </>;
}
