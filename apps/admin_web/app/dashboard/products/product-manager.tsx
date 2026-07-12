"use client";

import { FormEvent, useCallback, useEffect, useState } from "react";
import { ProductContentEditor } from "./product-content-editor";

type Product = {
  id: string; sku: string; nameAr: string; nameEn: string; categoryName: string;
  brandName?: string; price: number; stockQty: number; stockStatus: string;
  isFeatured: boolean; isPrintable: boolean;
};
type ProductPage = { items: Product[]; page: number; total: number; totalPages: number };
type Category = { id: string; nameAr: string; children: Category[] };
type Lookup = { id: string; nameAr: string; code?: string };
type ProductForm = Record<string, string | number | boolean | null> & { id?: string };
type Variant = {
  id?: string; sku: string; nameAr: string; nameEn: string; optionsJson: string;
  priceAdjustment: number; stockQty: number; isActive: boolean;
};

const empty: ProductForm = {
  sku: "", nameAr: "", nameEn: "", slug: "", descriptionAr: "", descriptionEn: "",
  categoryId: "", brandId: null, unitId: "", basePrice: 0, compareAtPrice: null,
  taxRatePercent: 14, minOrderQty: 1, maxOrderQty: null, stockQty: 0,
  lowStockThreshold: 10, status: "Active", isPrintable: false, isFeatured: false,
  warrantyAr: "ضمان جودة وتوافق مع المواصفات", deliveryEstimateDays: 2,
};

export function ProductManager() {
  const [data, setData] = useState<ProductPage | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [query, setQuery] = useState("");
  const [page, setPage] = useState(1);
  const [editor, setEditor] = useState<ProductForm | null>(null);
  const [categories, setCategories] = useState<Category[]>([]);
  const [brands, setBrands] = useState<Lookup[]>([]);
  const [units, setUnits] = useState<Lookup[]>([]);
  const [saving, setSaving] = useState(false);
  const [variantProduct, setVariantProduct] = useState<Product | null>(null);
  const [variants, setVariants] = useState<Variant[]>([]);
  const [exporting, setExporting] = useState(false);
  const [importing, setImporting] = useState(false);
  const [contentProduct, setContentProduct] = useState<Product | null>(null);

  const requestProducts = useCallback(async (signal?: AbortSignal) => {
    const response = await fetch(`/api/admin/catalog/products?page=${page}&pageSize=20&q=${encodeURIComponent(query)}`, { signal });
    if (!response.ok) throw new Error("تعذر تحميل المنتجات");
    return await response.json() as ProductPage;
  }, [page, query]);

  useEffect(() => {
    const controller = new AbortController();
    requestProducts(controller.signal)
      .then((result) => { setData(result); setLoading(false); })
      .catch((reason) => {
        if (reason instanceof DOMException && reason.name === "AbortError") return;
        setError(reason instanceof Error ? reason.message : "تعذر تحميل المنتجات");
        setLoading(false);
      });
    return () => controller.abort();
  }, [requestProducts]);

  async function reload() {
    setLoading(true); setError("");
    try { setData(await requestProducts()); }
    catch (reason) { setError(reason instanceof Error ? reason.message : "تعذر تحميل المنتجات"); }
    finally { setLoading(false); }
  }

  async function openEditor(id?: string) {
    setError("");
    const [categoryResponse, lookupResponse, productResponse] = await Promise.all([
      fetch("/api/admin/catalog/categories"),
      fetch("/api/admin/catalog/lookups"),
      id ? fetch(`/api/admin/catalog/products/${id}`) : Promise.resolve(null),
    ]);
    if (!categoryResponse.ok || !lookupResponse.ok || (productResponse && !productResponse.ok)) {
      setError("تعذر تحميل بيانات نموذج المنتج"); return;
    }
    const lookup = await lookupResponse.json();
    setCategories(await categoryResponse.json()); setBrands(lookup.brands); setUnits(lookup.units);
    setEditor(productResponse ? await productResponse.json() : { ...empty });
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setSaving(true); setError("");
    const form = new FormData(event.currentTarget);
    const payload = {
      sku: form.get("sku"), nameAr: form.get("nameAr"), nameEn: form.get("nameEn"), slug: form.get("slug"),
      descriptionAr: form.get("descriptionAr"), descriptionEn: form.get("descriptionEn"),
      categoryId: form.get("categoryId"), brandId: form.get("brandId") || null, unitId: form.get("unitId"),
      basePrice: Number(form.get("basePrice")), compareAtPrice: form.get("compareAtPrice") ? Number(form.get("compareAtPrice")) : null,
      taxRatePercent: Number(form.get("taxRatePercent")), minOrderQty: Number(form.get("minOrderQty")),
      maxOrderQty: form.get("maxOrderQty") ? Number(form.get("maxOrderQty")) : null,
      stockQty: Number(form.get("stockQty")), lowStockThreshold: Number(form.get("lowStockThreshold")),
      status: form.get("status"), isPrintable: form.get("isPrintable") === "on", isFeatured: form.get("isFeatured") === "on",
      warrantyAr: form.get("warrantyAr"), deliveryEstimateDays: Number(form.get("deliveryEstimateDays")),
    };
    const response = await fetch(editor?.id ? `/api/admin/catalog/products/${editor.id}` : "/api/admin/catalog/products", {
      method: editor?.id ? "PUT" : "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(payload),
    });
    if (!response.ok) {
      const body = await response.json().catch(() => ({}));
      setError(body.title || body.message || "تعذر حفظ المنتج"); setSaving(false); return;
    }
    setEditor(null); setSaving(false); await reload();
  }

  async function archive(product: Product) {
    if (!window.confirm(`هل تريد أرشفة المنتج «${product.nameAr}»؟`)) return;
    const response = await fetch(`/api/admin/catalog/products/${product.id}`, { method: "DELETE" });
    if (!response.ok) setError("تعذر أرشفة المنتج"); else await reload();
  }

  async function openVariants(product: Product) {
    setError("");
    const response = await fetch(`/api/admin/catalog/products/${product.id}/variants`);
    if (!response.ok) { setError("تعذر تحميل متغيرات المنتج"); return; }
    setVariants(await response.json());
    setVariantProduct(product);
  }

  function updateVariant(index: number, patch: Partial<Variant>) {
    setVariants((current) => current.map((variant, itemIndex) => itemIndex === index ? { ...variant, ...patch } : variant));
  }

  async function saveVariants() {
    if (!variantProduct) return;
    setSaving(true); setError("");
    const response = await fetch(`/api/admin/catalog/products/${variantProduct.id}/variants`, {
      method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify(variants),
    });
    if (!response.ok) {
      const body = await response.json().catch(() => ({}));
      setError(body.title || body.message || "تعذر حفظ المتغيرات"); setSaving(false); return;
    }
    setSaving(false); setVariantProduct(null);
  }

  async function exportCatalog() {
    setExporting(true); setError("");
    try {
      const response = await fetch(`/api/admin/catalog/products/export?q=${encodeURIComponent(query)}`);
      if (!response.ok) throw new Error("تعذر تصدير الكتالوج");
      const blob = await response.blob();
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement("a");
      anchor.href = url; anchor.download = `mohandseto-products-${new Date().toISOString().slice(0, 10)}.csv`;
      document.body.appendChild(anchor); anchor.click(); anchor.remove(); URL.revokeObjectURL(url);
    } catch (reason) { setError(reason instanceof Error ? reason.message : "تعذر تصدير الكتالوج"); }
    finally { setExporting(false); }
  }

  async function importCatalog(file?: File) {
    if (!file) return;
    setImporting(true); setError("");
    const form = new FormData(); form.append("file", file);
    try {
      const response = await fetch("/api/admin/catalog/products/import", { method: "POST", body: form });
      const body = await response.json().catch(() => ({}));
      if (!response.ok) throw new Error(body.title || body.message || "تعذر استيراد الكتالوج");
      window.alert(`اكتمل الاستيراد: ${body.created} جديد، ${body.updated} محدث، ${body.rejected} مرفوض`);
      await reload();
    } catch (reason) { setError(reason instanceof Error ? reason.message : "تعذر استيراد الكتالوج"); }
    finally { setImporting(false); }
  }

  const flatCategories = categories.flatMap((parent) => [parent, ...parent.children]);
  return (
    <>
      <div className="page-heading catalog-heading">
        <div><p>الكتالوج</p><h1>إدارة المنتجات</h1><span>إضافة وتعديل وتسعير منتجات المنصة</span></div>
        <button className="primary-button" onClick={() => openEditor()}>+ إضافة منتج جديد</button>
      </div>
      {error && <div className="admin-alert" role="alert">{error}<button onClick={() => setError("")}>×</button></div>}
      <section className="panel catalog-toolbar">
        <div className="search-box"><span>⌕</span><input value={query} onChange={(event) => { setLoading(true); setQuery(event.target.value); setPage(1); }} placeholder="ابحث بالاسم أو كود المنتج..." /></div>
        <button className="secondary-button" onClick={() => { setQuery(""); setPage(1); }}>مسح التصفية</button>
        <label className="secondary-button file-button">{importing ? "جاري الاستيراد..." : "استيراد CSV"}<input type="file" accept=".csv,text/csv" disabled={importing} onChange={(event) => { void importCatalog(event.target.files?.[0]); event.target.value = ""; }} /></label>
        <button className="secondary-button" disabled={exporting} onClick={exportCatalog}>{exporting ? "جاري التصدير..." : "تصدير Excel (CSV)"}</button>
        <span className="catalog-count">{data?.total ?? 0} منتج</span>
      </section>
      <section className="panel products-admin-panel">
        {loading ? <div className="admin-loading">جاري تحميل المنتجات...</div> : data?.items.length === 0 ? <div className="admin-empty">لا توجد منتجات مطابقة</div> : (
          <div className="table-wrap"><table className="products-table"><thead><tr><th>المنتج</th><th>الكود</th><th>القسم</th><th>السعر</th><th>المخزون</th><th>النوع</th><th>إجراءات</th></tr></thead><tbody>
            {data?.items.map((product) => <tr key={product.id}>
              <td><div className="product-cell"><span className="product-thumb">▣</span><div><b>{product.nameAr}</b><small>{product.brandName || product.nameEn}</small></div></div></td>
              <td className="sku-cell">{product.sku}</td><td>{product.categoryName}</td>
              <td><b>{product.price.toLocaleString("ar-EG")} ج.م</b></td>
              <td><span className={`stock-chip ${product.stockStatus === "OutOfStock" ? "out" : product.stockStatus === "LowStock" ? "low" : ""}`}>{product.stockQty} متاح</span></td>
              <td>{product.isPrintable ? <span className="type-chip">مطبوع</span> : "قياسي"}</td>
              <td><div className="row-actions"><button title="تعديل" onClick={() => openEditor(product.id)}>✎</button><button title="المحتوى والملفات" onClick={() => setContentProduct(product)}>▣</button><button title="المتغيرات" onClick={() => openVariants(product)}>◆</button><button title="أرشفة" onClick={() => archive(product)}>⌫</button></div></td>
            </tr>)}
          </tbody></table></div>
        )}
        {data && data.totalPages > 1 && <div className="admin-pagination"><button disabled={page === 1} onClick={() => { setLoading(true); setPage(page - 1); }}>السابق</button><span>صفحة {page} من {data.totalPages}</span><button disabled={page === data.totalPages} onClick={() => { setLoading(true); setPage(page + 1); }}>التالي</button></div>}
      </section>
      {editor && <div className="modal-backdrop" role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget) setEditor(null); }}>
        <section className="product-editor" role="dialog" aria-modal="true" aria-labelledby="editor-title">
          <header><div><h2 id="editor-title">{editor.id ? "تعديل المنتج" : "إضافة منتج جديد"}</h2><p>أدخل بيانات المنتج الأساسية والتسعير والمخزون</p></div><button onClick={() => setEditor(null)}>×</button></header>
          <form key={editor.id ?? "new"} onSubmit={submit}>
            <div className="form-grid">
              <label>اسم المنتج بالعربية<input name="nameAr" defaultValue={String(editor.nameAr ?? "")} required /></label>
              <label>اسم المنتج بالإنجليزية<input name="nameEn" defaultValue={String(editor.nameEn ?? "")} required dir="ltr" /></label>
              <label>كود المنتج SKU<input name="sku" defaultValue={String(editor.sku ?? "")} required dir="ltr" /></label>
              <label>الرابط المختصر<input name="slug" defaultValue={String(editor.slug ?? "")} required dir="ltr" /></label>
              <label>القسم<select name="categoryId" defaultValue={String(editor.categoryId ?? "")} required><option value="">اختر القسم</option>{flatCategories.map((category) => <option key={category.id} value={category.id}>{category.nameAr}</option>)}</select></label>
              <label>العلامة التجارية<select name="brandId" defaultValue={String(editor.brandId ?? "")}><option value="">بدون علامة</option>{brands.map((brand) => <option key={brand.id} value={brand.id}>{brand.nameAr}</option>)}</select></label>
              <label>وحدة البيع<select name="unitId" defaultValue={String(editor.unitId ?? "")} required><option value="">اختر الوحدة</option>{units.map((unit) => <option key={unit.id} value={unit.id}>{unit.nameAr}</option>)}</select></label>
              <label>الحالة<select name="status" defaultValue={String(editor.status ?? "Active")}><option value="Active">نشط</option><option value="Draft">مسودة</option><option value="Archived">مؤرشف</option></select></label>
              <label>سعر البيع<input name="basePrice" type="number" min="0.01" step="0.01" defaultValue={Number(editor.basePrice ?? 0)} required /></label>
              <label>السعر قبل الخصم<input name="compareAtPrice" type="number" min="0" step="0.01" defaultValue={editor.compareAtPrice == null ? "" : Number(editor.compareAtPrice)} /></label>
              <label>الكمية بالمخزون<input name="stockQty" type="number" min="0" defaultValue={Number(editor.stockQty ?? 0)} required /></label>
              <label>حد تنبيه المخزون<input name="lowStockThreshold" type="number" min="0" defaultValue={Number(editor.lowStockThreshold ?? 10)} /></label>
              <label>أقل كمية للطلب<input name="minOrderQty" type="number" min="1" defaultValue={Number(editor.minOrderQty ?? 1)} /></label>
              <label>أقصى كمية للطلب<input name="maxOrderQty" type="number" min="1" defaultValue={editor.maxOrderQty == null ? "" : Number(editor.maxOrderQty)} /></label>
              <label>الضريبة %<input name="taxRatePercent" type="number" min="0" max="100" step="0.01" defaultValue={Number(editor.taxRatePercent ?? 14)} /></label>
              <label>مدة التوصيل بالأيام<input name="deliveryEstimateDays" type="number" min="1" defaultValue={Number(editor.deliveryEstimateDays ?? 2)} /></label>
              <label className="full-field">الوصف بالعربية<textarea name="descriptionAr" defaultValue={String(editor.descriptionAr ?? "")} rows={3} /></label>
              <label className="full-field">الضمان<input name="warrantyAr" defaultValue={String(editor.warrantyAr ?? "")} /></label>
              <input type="hidden" name="descriptionEn" value={String(editor.descriptionEn ?? "")} />
              <div className="full-field check-row"><label><input name="isFeatured" type="checkbox" defaultChecked={Boolean(editor.isFeatured)} /> منتج مميز</label><label><input name="isPrintable" type="checkbox" defaultChecked={Boolean(editor.isPrintable)} /> قابل للطباعة والتخصيص</label></div>
            </div>
            <footer><button type="button" className="secondary-button" onClick={() => setEditor(null)}>إلغاء</button><button className="primary-button" disabled={saving}>{saving ? "جاري الحفظ..." : "حفظ المنتج"}</button></footer>
          </form>
        </section>
      </div>}
      {variantProduct && <div className="modal-backdrop" role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget) setVariantProduct(null); }}>
        <section className="product-editor variant-editor" role="dialog" aria-modal="true" aria-labelledby="variants-title">
          <header><div><h2 id="variants-title">متغيرات {variantProduct.nameAr}</h2><p>الأكواد والخصائص وفروق الأسعار والمخزون لكل اختيار</p></div><button onClick={() => setVariantProduct(null)}>×</button></header>
          <div className="variant-editor-body">
            <div className="variant-list">
              {variants.map((variant, index) => <article className="variant-row" key={variant.id ?? `new-${index}`}>
                <label>SKU<input dir="ltr" value={variant.sku} onChange={(event) => updateVariant(index, { sku: event.target.value })} required /></label>
                <label>الاسم بالعربية<input value={variant.nameAr} onChange={(event) => updateVariant(index, { nameAr: event.target.value })} required /></label>
                <label>الاسم بالإنجليزية<input dir="ltr" value={variant.nameEn} onChange={(event) => updateVariant(index, { nameEn: event.target.value })} /></label>
                <label>الخيارات JSON<input dir="ltr" value={variant.optionsJson ?? ""} onChange={(event) => updateVariant(index, { optionsJson: event.target.value })} placeholder={'{"color":"أزرق"}'} /></label>
                <label>فرق السعر<input type="number" step="0.01" value={variant.priceAdjustment} onChange={(event) => updateVariant(index, { priceAdjustment: Number(event.target.value) })} /></label>
                <label>المخزون<input type="number" min="0" value={variant.stockQty} onChange={(event) => updateVariant(index, { stockQty: Number(event.target.value) })} /></label>
                <label className="variant-active"><input type="checkbox" checked={variant.isActive} onChange={(event) => updateVariant(index, { isActive: event.target.checked })} /> نشط</label>
                <button className="variant-remove" type="button" onClick={() => setVariants((current) => current.filter((_, itemIndex) => itemIndex !== index))}>حذف</button>
              </article>)}
              {variants.length === 0 && <div className="admin-empty compact">لا توجد متغيرات لهذا المنتج</div>}
            </div>
            <button className="secondary-button add-variant" type="button" onClick={() => setVariants((current) => [...current, { sku: `${variantProduct.sku}-`, nameAr: "", nameEn: "", optionsJson: "{}", priceAdjustment: 0, stockQty: 0, isActive: true }])}>+ إضافة متغير</button>
          </div>
          <footer className="variant-footer"><button type="button" className="secondary-button" onClick={() => setVariantProduct(null)}>إلغاء</button><button className="primary-button" disabled={saving || variants.some((variant) => !variant.sku.trim() || !variant.nameAr.trim())} onClick={saveVariants}>{saving ? "جاري الحفظ..." : "حفظ المتغيرات"}</button></footer>
        </section>
      </div>}
      {contentProduct && <ProductContentEditor product={contentProduct} onClose={() => setContentProduct(null)} onError={setError} />}
    </>
  );
}
