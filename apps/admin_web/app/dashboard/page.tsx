const kpis = [
  ["إجمالي المبيعات", "1,284,650 ج.م", "+12.5%", "أزرق"],
  ["الطلبات الجديدة", "248", "+8.2%", "أخضر"],
  ["عروض بانتظار الرد", "36", "-3.1%", "برتقالي"],
  ["الشركات النشطة", "1,842", "+6.4%", "بنفسجي"],
];

const orders = [
  ["#ORD-28491", "الشركة المصرية للمقاولات", "12 يوليو 2026", "24,850 ج.م", "قيد التجهيز"],
  ["#ORD-28490", "مجموعة النيل التجارية", "12 يوليو 2026", "18,420 ج.م", "تم التأكيد"],
  ["#ORD-28489", "الأفق للصناعات", "11 يوليو 2026", "42,100 ج.م", "بانتظار الموافقة"],
  ["#ORD-28488", "رواد التعليم", "11 يوليو 2026", "9,780 ج.م", "تم الشحن"],
];

export default function DashboardPage() {
  return (
    <>
      <div className="page-heading">
        <div><p>الأحد، 12 يوليو 2026</p><h1>مرحبًا، مدير النظام</h1><span>إليك ملخص أداء المنصة اليوم</span></div>
        <div className="heading-actions"><button className="secondary-button">تصدير التقرير</button><button className="primary-button">+ إضافة طلب</button></div>
      </div>
      <section className="kpi-grid">
        {kpis.map(([label, value, change, color]) => (
          <article className="kpi-card" key={label}>
            <div className={`kpi-icon ${color}`}>◈</div>
            <div><p>{label}</p><strong>{value}</strong><small className={change.startsWith("+") ? "up" : "down"}>{change} <span>عن الشهر الماضي</span></small></div>
          </article>
        ))}
      </section>
      <section className="chart-grid">
        <article className="panel sales-panel">
          <div className="panel-heading"><div><h2>المبيعات والطلبات</h2><p>أداء آخر 7 أيام</p></div><select aria-label="الفترة"><option>آخر 7 أيام</option><option>آخر 30 يومًا</option></select></div>
          <div className="chart-legend"><span><i className="blue-dot" /> المبيعات</span><span><i className="teal-dot" /> الطلبات</span></div>
          <div className="line-chart" aria-label="رسم المبيعات"><svg viewBox="0 0 700 220" role="img"><defs><linearGradient id="area" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stopColor="#167a8b" stopOpacity=".28"/><stop offset="1" stopColor="#167a8b" stopOpacity="0"/></linearGradient></defs><path className="grid-line" d="M0 35H700M0 85H700M0 135H700M0 185H700"/><path className="area" d="M0 170 C70 140,100 145,150 120 S250 100,300 110 S390 60,450 80 S560 45,700 35 V220 H0Z"/><path className="sales-line" d="M0 170 C70 140,100 145,150 120 S250 100,300 110 S390 60,450 80 S560 45,700 35"/><path className="order-line" d="M0 185 C80 170,130 178,190 150 S300 155,360 125 S470 120,530 95 S630 110,700 75"/></svg></div>
          <div className="chart-days"><span>الأحد</span><span>الاثنين</span><span>الثلاثاء</span><span>الأربعاء</span><span>الخميس</span><span>الجمعة</span><span>السبت</span></div>
        </article>
        <article className="panel status-panel">
          <div className="panel-heading"><div><h2>حالة الطلبات</h2><p>توزيع الطلبات الحالية</p></div><button>⋯</button></div>
          <div className="donut"><div><strong>248</strong><span>إجمالي الطلبات</span></div></div>
          <ul><li><i className="blue-dot" /> جديد <b>68</b><span>27%</span></li><li><i className="teal-dot" /> قيد التجهيز <b>84</b><span>34%</span></li><li><i className="orange-dot" /> تم الشحن <b>57</b><span>23%</span></li><li><i className="gray-dot" /> مكتمل <b>39</b><span>16%</span></li></ul>
        </article>
      </section>
      <section className="panel orders-panel">
        <div className="panel-heading"><div><h2>أحدث الطلبات</h2><p>آخر الطلبات الواردة للمنصة</p></div><button className="text-button">عرض كل الطلبات ←</button></div>
        <div className="table-wrap"><table><thead><tr><th>رقم الطلب</th><th>الشركة</th><th>التاريخ</th><th>الإجمالي</th><th>الحالة</th><th /></tr></thead><tbody>{orders.map((order) => <tr key={order[0]}>{order.map((cell, index) => <td key={cell}>{index === 4 ? <span className="status-chip">{cell}</span> : cell}</td>)}<td><button>⋮</button></td></tr>)}</tbody></table></div>
      </section>
    </>
  );
}
