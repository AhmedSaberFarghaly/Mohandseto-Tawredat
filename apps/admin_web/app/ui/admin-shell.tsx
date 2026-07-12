"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useRouter } from "next/navigation";
import { useState } from "react";

const groups = [
  ["الرئيسية", [["لوحة التحكم", "/dashboard", "⌂"]]],
  ["التشغيل", [["الطلبات", "/dashboard/orders", "▤"], ["عروض الأسعار", "/dashboard/quotes", "◫"], ["الطباعة والتصميم", "/dashboard/printing", "✎"]]],
  ["الكتالوج", [["المنتجات", "/dashboard/products", "◇"], ["الأقسام", "/dashboard/categories", "▦"], ["المحتوى والعروض", "/dashboard/content", "☆"]]],
  ["الإدارة", [["المخزون", "/dashboard/inventory", "▥"], ["الموردون", "/dashboard/suppliers", "♙"], ["الشركات وCRM", "/dashboard/companies", "◉"], ["العقود", "/dashboard/contracts", "▱"]]],
  ["التحليلات والنظام", [["التقارير", "/dashboard/reports", "⌁"], ["المستخدمون والصلاحيات", "/dashboard/users", "♧"], ["الإعدادات", "/dashboard/settings", "⚙"]]],
] as const;

export function AdminShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const [open, setOpen] = useState(false);
  async function logout() {
    await fetch("/api/session/logout", { method: "POST" });
    router.replace("/login");
    router.refresh();
  }
  return (
    <div className="admin-layout">
      <aside className={`sidebar ${open ? "sidebar-open" : ""}`}>
        <div className="sidebar-brand"><span>ت</span><div><b>مهندسيتو</b><small>لوحة الإدارة</small></div></div>
        <nav>
          {groups.map(([title, items]) => (
            <div className="nav-group" key={title}>
              <p>{title}</p>
              {items.map(([label, href, icon]) => (
                <Link key={href} href={href} onClick={() => setOpen(false)} className={pathname === href ? "active" : ""}>
                  <i>{icon}</i><span>{label}</span>{label === "الطلبات" && <em>12</em>}
                </Link>
              ))}
            </div>
          ))}
        </nav>
        <div className="sidebar-user"><span>م</span><div><b>مدير النظام</b><small>admin@mohandseto.com</small></div><button aria-label="تسجيل الخروج" title="تسجيل الخروج" onClick={logout}>↪</button></div>
      </aside>
      {open && <button className="sidebar-overlay" aria-label="إغلاق القائمة" onClick={() => setOpen(false)} />}
      <section className="admin-main">
        <header className="topbar">
          <button className="menu-button" onClick={() => setOpen(true)} aria-label="فتح القائمة">☰</button>
          <div className="search-box"><span>⌕</span><input aria-label="البحث" placeholder="ابحث عن طلب، شركة، منتج..." /></div>
          <div className="topbar-actions"><button aria-label="الإشعارات">♢<em>3</em></button><button aria-label="المساعدة">؟</button></div>
        </header>
        <main className="dashboard-content">{children}</main>
      </section>
    </div>
  );
}
