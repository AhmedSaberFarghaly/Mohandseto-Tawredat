"use client";

import { FormEvent, useCallback, useEffect, useState } from "react";

type Attribute = { nameAr: string; valueAr: string; sortOrder: number };
type PriceTier = { minQty: number; unitPrice: number };
type ProductImage = { id: string; path: string; altAr?: string; isPrimary: boolean; sortOrder: number };
type ProductDocument = { id: string; nameAr: string; path: string; contentType: string };
type ProductContent = { attributes: Attribute[]; priceTiers: PriceTier[]; images: ProductImage[]; documents: ProductDocument[] };

export function ProductContentEditor({ product, onClose, onError }: {
  product: { id: string; nameAr: string; sku: string };
  onClose: () => void;
  onError: (message: string) => void;
}) {
  const [content, setContent] = useState<ProductContent | null>(null);
  const [tab, setTab] = useState<"attributes" | "tiers" | "images" | "documents">("attributes");
  const [saving, setSaving] = useState(false);

  const load = useCallback(async () => {
    const response = await fetch(`/api/admin/catalog/products/${product.id}/content`);
    if (!response.ok) throw new Error("تعذر تحميل محتوى المنتج");
    setContent(await response.json());
  }, [product.id]);

  useEffect(() => {
    const controller = new AbortController();
    fetch(`/api/admin/catalog/products/${product.id}/content`, { signal: controller.signal })
      .then(async (response) => {
        if (!response.ok) throw new Error("تعذر تحميل محتوى المنتج");
        return await response.json() as ProductContent;
      })
      .then(setContent)
      .catch((reason) => {
        if (reason instanceof DOMException && reason.name === "AbortError") return;
        onError(reason instanceof Error ? reason.message : "تعذر تحميل المحتوى");
      });
    return () => controller.abort();
  }, [product.id, onError]);

  function updateAttribute(index: number, patch: Partial<Attribute>) {
    setContent((current) => current ? { ...current, attributes: current.attributes.map((item, itemIndex) => itemIndex === index ? { ...item, ...patch } : item) } : current);
  }
  function updateTier(index: number, patch: Partial<PriceTier>) {
    setContent((current) => current ? { ...current, priceTiers: current.priceTiers.map((item, itemIndex) => itemIndex === index ? { ...item, ...patch } : item) } : current);
  }

  async function saveJson(path: string, payload: unknown, success: string) {
    setSaving(true);
    const response = await fetch(`/api/admin/catalog/products/${product.id}/${path}`, {
      method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify(payload),
    });
    if (!response.ok) {
      const body = await response.json().catch(() => ({}));
      onError(body.title || body.message || "تعذر حفظ المحتوى"); setSaving(false); return;
    }
    setSaving(false); window.alert(success); await load();
  }

  async function upload(event: FormEvent<HTMLFormElement>, kind: "images" | "documents") {
    event.preventDefault(); setSaving(true);
    const form = new FormData(event.currentTarget);
    const response = await fetch(`/api/admin/catalog/products/${product.id}/${kind}`, { method: "POST", body: form });
    if (!response.ok) {
      const body = await response.json().catch(() => ({}));
      onError(body.title || body.message || "تعذر رفع الملف"); setSaving(false); return;
    }
    event.currentTarget.reset(); setSaving(false); await load();
  }

  async function remove(kind: "images" | "documents", id: string) {
    if (!window.confirm("هل تريد حذف هذا الملف نهائيًا؟")) return;
    const response = await fetch(`/api/admin/catalog/products/${product.id}/${kind}/${id}`, { method: "DELETE" });
    if (!response.ok) { onError("تعذر حذف الملف"); return; }
    await load();
  }

  return <div className="modal-backdrop" role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget) onClose(); }}>
    <section className="product-editor content-editor" role="dialog" aria-modal="true" aria-labelledby="content-title">
      <header><div><h2 id="content-title">محتوى {product.nameAr}</h2><p>{product.sku} — الخصائص والتسعير والصور والمستندات</p></div><button onClick={onClose}>×</button></header>
      <nav className="content-tabs" aria-label="أقسام محتوى المنتج">
        <button className={tab === "attributes" ? "active" : ""} onClick={() => setTab("attributes")}>الخصائص</button>
        <button className={tab === "tiers" ? "active" : ""} onClick={() => setTab("tiers")}>شرائح الأسعار</button>
        <button className={tab === "images" ? "active" : ""} onClick={() => setTab("images")}>الصور</button>
        <button className={tab === "documents" ? "active" : ""} onClick={() => setTab("documents")}>المستندات</button>
      </nav>
      {!content ? <div className="admin-loading">جاري تحميل محتوى المنتج...</div> : <div className="content-editor-body">
        {tab === "attributes" && <>
          <div className="editable-list">{content.attributes.map((attribute, index) => <div className="editable-row" key={`${attribute.nameAr}-${index}`}>
            <label>اسم الخاصية<input value={attribute.nameAr} onChange={(event) => updateAttribute(index, { nameAr: event.target.value })} /></label>
            <label>القيمة<input value={attribute.valueAr} onChange={(event) => updateAttribute(index, { valueAr: event.target.value })} /></label>
            <button className="danger-outline" onClick={() => setContent({ ...content, attributes: content.attributes.filter((_, itemIndex) => itemIndex !== index) })}>حذف</button>
          </div>)}</div>
          <button className="secondary-button list-add" onClick={() => setContent({ ...content, attributes: [...content.attributes, { nameAr: "", valueAr: "", sortOrder: content.attributes.length }] })}>+ إضافة خاصية</button>
          <div className="content-actions"><button className="primary-button" disabled={saving || content.attributes.some((item) => !item.nameAr.trim() || !item.valueAr.trim())} onClick={() => saveJson("attributes", content.attributes, "تم حفظ خصائص المنتج")}>حفظ الخصائص</button></div>
        </>}
        {tab === "tiers" && <>
          <div className="editable-list">{content.priceTiers.map((tier, index) => <div className="editable-row tier-row" key={`${tier.minQty}-${index}`}>
            <label>من كمية<input type="number" min="1" value={tier.minQty} onChange={(event) => updateTier(index, { minQty: Number(event.target.value) })} /></label>
            <label>سعر الوحدة<input type="number" min="0.01" step="0.01" value={tier.unitPrice} onChange={(event) => updateTier(index, { unitPrice: Number(event.target.value) })} /></label>
            <button className="danger-outline" onClick={() => setContent({ ...content, priceTiers: content.priceTiers.filter((_, itemIndex) => itemIndex !== index) })}>حذف</button>
          </div>)}</div>
          <button className="secondary-button list-add" onClick={() => setContent({ ...content, priceTiers: [...content.priceTiers, { minQty: 1, unitPrice: 1 }] })}>+ إضافة شريحة</button>
          <div className="content-actions"><button className="primary-button" disabled={saving || content.priceTiers.some((item) => item.minQty < 1 || item.unitPrice <= 0)} onClick={() => saveJson("price-tiers", content.priceTiers, "تم حفظ شرائح الأسعار")}>حفظ الشرائح</button></div>
        </>}
        {tab === "images" && <>
          <div className="media-list">{content.images.map((image) => <article key={image.id}><span>▣</span><div><b>{image.altAr || "صورة المنتج"}</b><small>{image.path}{image.isPrimary ? " — رئيسية" : ""}</small></div><button className="danger-outline" onClick={() => remove("images", image.id)}>حذف</button></article>)}</div>
          <form className="upload-panel" onSubmit={(event) => upload(event, "images")}><label>ملف الصورة<input name="file" type="file" accept=".jpg,.jpeg,.png,.webp,image/*" required /></label><label>النص البديل<input name="altAr" placeholder="وصف الصورة بالعربية" /></label><label className="inline-check"><input name="isPrimary" type="checkbox" value="true" /> صورة رئيسية</label><button className="primary-button" disabled={saving}>رفع الصورة</button></form>
        </>}
        {tab === "documents" && <>
          <div className="media-list">{content.documents.map((document) => <article key={document.id}><span>PDF</span><div><b>{document.nameAr}</b><small>{document.path}</small></div><button className="danger-outline" onClick={() => remove("documents", document.id)}>حذف</button></article>)}</div>
          <form className="upload-panel" onSubmit={(event) => upload(event, "documents")}><label>ملف PDF<input name="file" type="file" accept=".pdf,application/pdf" required /></label><label>اسم المستند<input name="nameAr" placeholder="مثال: ورقة المواصفات الفنية" required /></label><button className="primary-button" disabled={saving}>رفع المستند</button></form>
        </>}
      </div>}
    </section>
  </div>;
}
