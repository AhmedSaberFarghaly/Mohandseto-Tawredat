"use client";

import { FormEvent, useCallback, useEffect, useState } from "react";

type Category = {
  id: string; parentId?: string; nameAr: string; nameEn: string; slug: string;
  iconName?: string; imageUrl?: string; productCount: number; children: Category[];
};
type Brand = { id: string; nameAr: string; nameEn: string; slug: string; logoPath?: string; isActive: boolean };

export function CategoryManager() {
  const [tab, setTab] = useState<"categories" | "brands">("categories");
  const [categories, setCategories] = useState<Category[]>([]);
  const [brands, setBrands] = useState<Brand[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [categoryEditor, setCategoryEditor] = useState<Partial<Category> | null>(null);
  const [brandEditor, setBrandEditor] = useState<Partial<Brand> | null>(null);
  const [saving, setSaving] = useState(false);

  const load = useCallback(async () => {
    const [categoryResponse, brandResponse] = await Promise.all([
      fetch("/api/admin/catalog/categories"), fetch("/api/admin/catalog/brands"),
    ]);
    if (!categoryResponse.ok || !brandResponse.ok) throw new Error("تعذر تحميل بيانات الكتالوج");
    return { categories: await categoryResponse.json() as Category[], brands: await brandResponse.json() as Brand[] };
  }, []);

  useEffect(() => {
    let active = true;
    load().then((result) => { if (active) { setCategories(result.categories); setBrands(result.brands); setLoading(false); } })
      .catch((reason) => { if (active) { setError(reason.message); setLoading(false); } });
    return () => { active = false; };
  }, [load]);

  async function reload() {
    setLoading(true);
    try { const result = await load(); setCategories(result.categories); setBrands(result.brands); }
    catch (reason) { setError(reason instanceof Error ? reason.message : "تعذر التحميل"); }
    finally { setLoading(false); }
  }

  async function submitCategory(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setSaving(true); setError("");
    const form = new FormData(event.currentTarget);
    const payload = { parentId: form.get("parentId") || null, nameAr: form.get("nameAr"), nameEn: form.get("nameEn"), slug: form.get("slug"), iconName: form.get("iconName") || null, imagePath: categoryEditor?.imageUrl || null, sortOrder: Number(form.get("sortOrder") || 0), isActive: true };
    const response = await fetch(categoryEditor?.id ? `/api/admin/catalog/categories/${categoryEditor.id}` : "/api/admin/catalog/categories", {
      method: categoryEditor?.id ? "PUT" : "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(payload),
    });
    if (!response.ok) { const body = await response.json().catch(() => ({})); setError(body.title || "تعذر حفظ القسم"); setSaving(false); return; }
    setCategoryEditor(null); setSaving(false); await reload();
  }

  async function submitBrand(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setSaving(true); setError("");
    const form = new FormData(event.currentTarget);
    const payload = { nameAr: form.get("nameAr"), nameEn: form.get("nameEn"), slug: form.get("slug"), logoPath: brandEditor?.logoPath || null, isActive: form.get("isActive") === "on" };
    const response = await fetch(brandEditor?.id ? `/api/admin/catalog/brands/${brandEditor.id}` : "/api/admin/catalog/brands", {
      method: brandEditor?.id ? "PUT" : "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(payload),
    });
    if (!response.ok) { const body = await response.json().catch(() => ({})); setError(body.title || "تعذر حفظ العلامة"); setSaving(false); return; }
    setBrandEditor(null); setSaving(false); await reload();
  }

  async function archive(kind: "categories" | "brands", id: string, name: string) {
    if (!window.confirm(`هل تريد تعطيل «${name}»؟`)) return;
    const response = await fetch(`/api/admin/catalog/${kind}/${id}`, { method: "DELETE" });
    if (!response.ok) { const body = await response.json().catch(() => ({})); setError(body.title || "تعذر تنفيذ العملية"); return; }
    await reload();
  }

  const flatCategories = categories.flatMap((parent) => [parent, ...parent.children]);
  return <>
    <div className="page-heading catalog-heading"><div><p>الكتالوج</p><h1>الأقسام والعلامات التجارية</h1><span>تنظيم هيكل الكتالوج والبيانات المرجعية</span></div><button className="primary-button" onClick={() => tab === "categories" ? setCategoryEditor({}) : setBrandEditor({ isActive: true })}>+ إضافة {tab === "categories" ? "قسم" : "علامة"}</button></div>
    {error && <div className="admin-alert" role="alert">{error}<button onClick={() => setError("")}>×</button></div>}
    <div className="catalog-tabs"><button className={tab === "categories" ? "active" : ""} onClick={() => setTab("categories")}>الأقسام ({flatCategories.length})</button><button className={tab === "brands" ? "active" : ""} onClick={() => setTab("brands")}>العلامات التجارية ({brands.length})</button></div>
    <section className="panel reference-panel">{loading ? <div className="admin-loading">جاري التحميل...</div> : tab === "categories" ? <div className="reference-grid">{categories.map((parent) => <article className="reference-card" key={parent.id}><header><span className="reference-icon">▦</span><div><b>{parent.nameAr}</b><small>{parent.nameEn} · {parent.children.length} فرعي</small></div><div className="row-actions"><button onClick={() => setCategoryEditor(parent)}>✎</button><button onClick={() => archive("categories", parent.id, parent.nameAr)}>⌫</button></div></header><ul>{parent.children.map((child) => <li key={child.id}><div><b>{child.nameAr}</b><small>{child.productCount} منتج</small></div><div className="row-actions"><button onClick={() => setCategoryEditor(child)}>✎</button><button onClick={() => archive("categories", child.id, child.nameAr)}>⌫</button></div></li>)}</ul><button className="add-child" onClick={() => setCategoryEditor({ parentId: parent.id })}>+ إضافة قسم فرعي</button></article>)}</div> : <div className="brands-grid">{brands.map((brand) => <article className={`brand-card ${brand.isActive ? "" : "inactive"}`} key={brand.id}><span>{brand.nameAr.slice(0, 1)}</span><div><b>{brand.nameAr}</b><small>{brand.nameEn}<br />/{brand.slug}</small></div><em>{brand.isActive ? "نشطة" : "معطلة"}</em><div className="row-actions"><button onClick={() => setBrandEditor(brand)}>✎</button><button onClick={() => archive("brands", brand.id, brand.nameAr)}>⌫</button></div></article>)}</div>}</section>
    {categoryEditor && <div className="modal-backdrop" onMouseDown={(event) => { if (event.target === event.currentTarget) setCategoryEditor(null); }}><section className="mini-editor" role="dialog" aria-modal="true"><header><h2>{categoryEditor.id ? "تعديل القسم" : "إضافة قسم"}</h2><button onClick={() => setCategoryEditor(null)}>×</button></header><form onSubmit={submitCategory}><label>القسم الرئيسي<select name="parentId" defaultValue={categoryEditor.parentId || ""}><option value="">قسم رئيسي</option>{categories.filter((item) => item.id !== categoryEditor.id).map((item) => <option value={item.id} key={item.id}>{item.nameAr}</option>)}</select></label><label>الاسم بالعربية<input name="nameAr" defaultValue={categoryEditor.nameAr || ""} required /></label><label>الاسم بالإنجليزية<input name="nameEn" defaultValue={categoryEditor.nameEn || ""} required dir="ltr" /></label><label>الرابط المختصر<input name="slug" defaultValue={categoryEditor.slug || ""} required dir="ltr" /></label><label>أيقونة Material<input name="iconName" defaultValue={categoryEditor.iconName || "category"} dir="ltr" /></label><label>الترتيب<input name="sortOrder" type="number" defaultValue="0" /></label><footer><button type="button" className="secondary-button" onClick={() => setCategoryEditor(null)}>إلغاء</button><button className="primary-button" disabled={saving}>{saving ? "جاري الحفظ..." : "حفظ"}</button></footer></form></section></div>}
    {brandEditor && <div className="modal-backdrop" onMouseDown={(event) => { if (event.target === event.currentTarget) setBrandEditor(null); }}><section className="mini-editor" role="dialog" aria-modal="true"><header><h2>{brandEditor.id ? "تعديل العلامة" : "إضافة علامة"}</h2><button onClick={() => setBrandEditor(null)}>×</button></header><form onSubmit={submitBrand}><label>الاسم بالعربية<input name="nameAr" defaultValue={brandEditor.nameAr || ""} required /></label><label>الاسم بالإنجليزية<input name="nameEn" defaultValue={brandEditor.nameEn || ""} required dir="ltr" /></label><label>الرابط المختصر<input name="slug" defaultValue={brandEditor.slug || ""} required dir="ltr" /></label><label className="check-label"><input name="isActive" type="checkbox" defaultChecked={brandEditor.isActive !== false} /> علامة نشطة</label><footer><button type="button" className="secondary-button" onClick={() => setBrandEditor(null)}>إلغاء</button><button className="primary-button" disabled={saving}>{saving ? "جاري الحفظ..." : "حفظ"}</button></footer></form></section></div>}
  </>;
}
