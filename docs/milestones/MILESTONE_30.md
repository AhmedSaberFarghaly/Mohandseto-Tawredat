# MILESTONE 30 — Catalog, Search, Comparison, and Product Content

## النسبة

30% من خارطة التنفيذ الكاملة، مع إغلاق البنية الوظيفية الأساسية للكتالوج عبر العميل والـAPI ولوحة الإدارة.

## ما تم إنجازه

- Catalog seed: 12 قسمًا رئيسيًا، 40 فرعيًا، 10 علامات، 6 وحدات، 250 منتجًا و750+ شريحة سعر.
- Flutter: الرئيسية والأقسام والقوائم والفلاتر والترتيب والبحث واقتراحاته وسجله والمفضلة والمقارنة والتفاصيل.
- Admin: Products/Categories/Brands CRUD، variants، attributes، quantity tiers، CSV import/export، images وPDF documents.
- Pricing: السعر الأساسي وشرائح الكمية وسعر عقد الشركة الفعلي على واجهات العميل.
- Media: تخزين آمن محلي قابل للاستبدال بـObject Storage، تحقق MIME/extension/size، تنزيل range-enabled وحذف من القرص.

## أهم APIs

- `GET /api/catalog/products` و`GET /api/catalog/products/{idOrSlug}`.
- `GET /api/catalog/search/suggestions` و`GET/DELETE /api/catalog/search/recent`.
- `GET/DELETE /api/catalog/compare` و`POST /api/catalog/compare/{id}/toggle`.
- Admin CRUD للمنتجات والأقسام والعلامات والمتغيرات.
- Admin content endpoints للخصائص وشرائح الأسعار والصور والمستندات.
- `GET /api/catalog/media/{kind}/{id}` للصور وPDF.
- Admin CSV export/import مع round-trip موثق.

## Entities / Migrations

- Categories، Brands، Units، Products، Images، Documents، Attributes، Variants، PriceTiers.
- Favorites، RecentlyViewed، CompareItems، RecentSearch، CompanyProductPrices.
- Migrations: `CatalogExpansion` و`CatalogSearchHistory` فوق `InitialFoundation`.

## التحقق

- 18 اختبار Backend ناجحًا على SQLite حقيقي in-memory.
- 3 اختبارات Flutter و`flutter analyze` بلا مشاكل.
- Admin ESLint وNext.js production build ناجحان.
- E2E Admin: Login، variants، brands، export/import 250 منتجًا بدون رفض.
- E2E Media: attributes/tiers، رفع PNG/PDF، تنزيل bytes، حذف DB/file.
- مصفوفة التغطية: 756 صفًا و14 عمودًا، validated.

## Known issues

- صور Seed التجارية ما زالت `asset://` لأن ملف التصميم لا يحتوي أصولًا مرخصة منفصلة؛ pipeline الرفع والعرض مكتمل.
- Google/Microsoft و2FA الحقيقي ينتظر Credentials خارجية.
- تعيين أسعار العقود من لوحة الإدارة يُنفذ مع وحدة حسابات الشركات، بينما تطبيق السعر الفعلي في الكتالوج يعمل.
- فحص Browser/Device بصري حي يعاد عند توفر سطح متصفح أو جهاز؛ build وPDF-reference QA منفذان.

## Commit / Tag

- Media/content implementation commit: `6eb25ff120802630175901b6c8bcf588214dbee0`.
- Search/reference implementation commit: `71efa9c`.
- Tag: `v0.3.0`.

## المرحلة التالية

M4: السلة وRFQ وعروض الأسعار والـcheckout، مع ربط عناوين الفروع والأسعار التجارية.
