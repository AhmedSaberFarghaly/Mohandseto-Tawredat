# مهندسيتو توريدات — Mohandseto Tawredat

منصة B2B متكاملة لتوريد احتياجات الشركات والمكاتب والمصانع: كتالوج وأسعار خاصة، طلبات عروض أسعار RFQ، موافقات داخلية، منتجات مطبوعة بشعار الشركة، مخزون وشحن وفواتير وCRM.

> **مصدر الحقيقة:** ملف التصميم `Mohandseto_Tawredat_Final design.pdf` (759 صفحة — 756 شاشة وحالة) + وثيقة المواصفات `Mohandseto_Tawredat_Full_Autonomous_Execution_Spec.md`.

## بنية المشروع (Monorepo)

```text
apps/
  api/             ASP.NET Core (.NET 10) + EF Core + SQLite (dev) — الـBackend
  admin_web/       Next.js + TypeScript — لوحة الإدارة والـCRM (RTL)
  client_flutter/  Flutter — تطبيق العميل (Android / iOS / Web)
packages/
  design_tokens/   مصدر الحقيقة للألوان والخطوط والمقاسات (tokens.json)
  api_contracts/   عقود الـAPI المشتركة
infrastructure/    Docker، سكربتات، قاعدة البيانات، المراقبة
docs/              الوثائق: المعمارية، قاعدة البيانات، QA، الأمن، المراحل
seed/              بيانات الإدخال: base / demo / test
```

## التشغيل محليًا

### الـAPI

```bash
cd apps/api
dotnet run
# http://localhost:5000 — Swagger على /swagger — الصحة على /health
```

قاعدة البيانات SQLite تُنشأ وتُهاجَر تلقائيًا في بيئة التطوير. للإنتاج: SQL Server عبر تغيير الـConnection String.

### لوحة الإدارة

```bash
cd apps/admin_web
npm install
npm run dev
# http://localhost:3000
```

### تطبيق العميل

```bash
cd apps/client_flutter
flutter pub get
flutter run -d chrome   # أو جهاز Android
```

## خارطة الطريق (10 مراحل × 10%)

| Tag | المرحلة |
|---|---|
| v0.1.0 | الأساس: Monorepo، API skeleton، DB، Design tokens، CI |
| v0.2.0 | الهوية وتسجيل الشركات والتحقق + نظام التصميم |
| v0.3.0 | الكتالوج والمنتجات والبحث + بيانات المنتجات |
| v0.4.0 | المنتجات المطبوعة + السلة + Checkout |
| v0.5.0 | الموافقات + RFQ |
| v0.6.0 | الطلبات والتتبع والمرتجعات والفواتير (عميل) |
| v0.7.0 | إدارة الطلبات والعروض والكتالوج (إدارة) |
| v0.8.0 | المخزون والموردون وCRM والعقود والطباعة |
| v0.9.0 | الشحن والحسابات والدعم والتقارير والمراقبة |
| v1.0.0 | QA شامل + الأمن + الأداء + النشر |

تتبُّع الشاشات: [docs/screen-coverage-matrix.csv](docs/screen-coverage-matrix.csv) — الهدف النهائي: **Missing Screens = 0**.

## الترخيص

مشروع خاص — جميع الحقوق محفوظة لمهندسيتو توريدات.
