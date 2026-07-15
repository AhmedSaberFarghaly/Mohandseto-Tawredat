# تقرير المرحلة 1 — 10% — الأساس (Foundation)

- **التاريخ:** 2026-07-12
- **Commit:** `e30e869`
- **Tag:** `v0.1.0`
- **النسبة المكتملة:** 10%

## ما تم إنجازه

### البنية
- Monorepo كامل: `apps/` (api، admin_web، client_flutter)، `packages/design_tokens`، `infrastructure/`، `docs/`، `seed/`.
- إزالة النموذج الأولي القديم (قابل للاسترجاع من `9308ab8`).

### الـBackend (ASP.NET Core / .NET 10)
- EF Core + SQLite. ملاحظة لاحقة: سلسلة migrations الحالية مرتبطة بمزود SQLite؛ الانتقال إلى SQL Server لا يتم بتغيير Connection String فقط ويحتاج migrations واختبار cutover مستقلين.
- Multi-tenancy: استخراج الـTenant من JWT claims + Global Query Filters تمنع تسرب البيانات بين الشركات.
- BaseEntity: Guid PK، حقول Audit كاملة (CreatedAt/By، UpdatedAt/By)، Soft Delete تلقائي، RowVersion.
- كيانات الهوية: Tenant، Company، CompanyBranch، CompanyDocument، User، Role، Permission، RolePermission، UserRole، OtpCode، RefreshToken، AuditLog.
- كيانات الكتالوج: Category، Brand، UnitOfMeasure، Product، ProductImage، QuantityPriceTier، ProductAttributeValue، CompanyProductPrice، Favorite، RecentlyViewed.
- Migration أولي `InitialFoundation` + تهجير تلقائي في التطوير.
- JWT auth، Serilog، Swagger، CORS، ProblemDetails، `/health` مع فحص قاعدة البيانات.
- **تم التحقق:** `dotnet build` ناجح، `/health` يرد `Healthy`.

### لوحة الإدارة (Next.js 16 + TypeScript)
- Layout عربي RTL بخط Cairo + متغيرات CSS من الـDesign Tokens.
- **تم التحقق:** `npm run build` ناجح.

### تطبيق العميل (Flutter 3.32)
- Theme كامل من الـTokens (ألوان، أزرار، حقول، بطاقات) + GoRouter + شاشة Splash (الشاشة 7) + RTL.
- **تم التحقق:** `flutter analyze` بدون أي مشاكل.

### Design Tokens
- استخراج الباليتة الدقيقة من الـPDF (تحليل Vector fills لصفحات المقدمة + عينات بكسل للشاشات):
  - أساسي `#023BAA`، شريط الإدارة `#103B5C`، تركوازي `#167A8B`، برتقالي `#F59E42`، دلالية كاملة + سلم رمادي 13 درجة.

### التتبع والجودة
- `docs/screen-coverage-matrix.csv`: 756 شاشة مسجلة بالوحدة والمنصة ورقم صفحة الـPDF + سكربت التوليد.
- CI (GitHub Actions): بناء الـAPI + بناء لوحة الإدارة + تحليل Flutter.
- وثائق: README، سجل الافتراضات، SECURITY، CHANGELOG، `.env.example`.

## Known Issues
- تحذير EF Core بخصوص Query Filters على علاقات User (سيُعالج في المرحلة 2 مع الـAuth الكامل).
- لا اختبارات آلية بعد — تبدأ من المرحلة 2 حسب الخطة.

## المرحلة التالية (v0.2.0 — 20%)
الهوية والتحقق: OTP، تسجيل الدخول، تسجيل الشركات والمستندات، شاشات Onboarding (15–39)، مكونات نظام التصميم الأساسية، Admin login + Dashboard shell.
