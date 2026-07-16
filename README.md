# مهندسيتو توريدات — Mohandseto Tawredat

منصة B2B متكاملة لتوريد احتياجات الشركات: تطبيق عميل Flutter، لوحة إدارة وCRM عربية RTL، وواجهة API مركزية. يغطي التنفيذ الكتالوج والأسعار الخاصة وRFQ والموافقات والمنتجات المطبوعة والطلبات والمخزون والمشتريات والشحن والحسابات والدعم والتقارير والتكاملات والمراقبة.

مصدر الحقيقة هو ملف التصميم `Mohandseto_Tawredat_Final design.pdf` (759 صفحة/756 شاشة وحالة) ووثيقة `Mohandseto_Tawredat_Full_Autonomous_Execution_Spec.md`.

## المكونات

```text
apps/api/             ASP.NET Core (.NET 10) + EF Core
apps/admin_web/       Next.js 16 + TypeScript — Admin/CRM RTL
apps/client_flutter/  Flutter — Android / iOS / Web
packages/             Design tokens وAPI contracts
infrastructure/       Docker، reverse proxy، وسكربتات التشغيل
docs/                 Architecture، Database، Security، QA، Deployment
```

## أسرع تشغيل محلي

باستخدام Docker Desktop وCompose v2:

```bash
docker compose up --build
```

- لوحة الإدارة: `http://localhost:3000`
- الـAPI: `http://localhost:5199`
- الجاهزية: `http://localhost:5199/health/ready`

أو شغّل المكونات مباشرة:

```bash
dotnet run --project apps/api/Mohandseto.Api.csproj
# http://localhost:5247 — Swagger: /swagger

cd apps/admin_web
npm ci
npm run dev
```

ومن `apps/client_flutter`: نفّذ `flutter pub get` ثم `flutter run` على الجهاز المطلوب. التفاصيل في [دليل التشغيل المحلي](docs/deployment/local.md).

## التحقق قبل التسليم

```bash
dotnet test apps/api.Tests/Mohandseto.Api.Tests.csproj -c Release

cd apps/admin_web
npm run lint
npm run build

cd ../client_flutter
flutter analyze
flutter test
```

CI يضيف فحص الثغرات والأسرار، بناء صور الحاويات، والتحقق من Compose. سيناريوهات القبول في [E2E](docs/qa/e2e-scenarios.md) وقائمة إطلاق v1.0.0 في [release checklist](docs/release/v1.0.0-checklist.md).

## حالة الإطلاق

- تم تنفيذ **756 من 756** شاشة/حالة برمجيًا ومتابعتها في [screen coverage matrix](docs/screen-coverage-matrix.csv). يشمل الإغلاق النهائي Google Sign-In الأصلي، Microsoft OIDC/PKCE، ربط الحسابات، وشاشة تصدير الفواتير المطابقة للتصميم بصيغ PDF وExcel وCSV. تفعيل المزودين في Staging/Production يحتاج بيانات OAuth الفعلية واختبار الأجهزة وفق [دليل OAuth](docs/deployment/oauth.md).
- النسخة الحالية Release Candidate وليست GA منشورة بعد؛ النشر العام يحتاج استضافة ودومينات وTLS وأسرار الإنتاج وبيانات مزودي الدفع/SMS والبريد واعتماد تجاري نهائي.
- سلسلة migrations الحالية مخصصة لـSQLite. مسار v1 المدعوم هو API واحد مع قرص دائم ونسخ احتياطية. الانتقال إلى SQL Server يتطلب migrations واختبار نقل مستقلين؛ تغيير Connection String وحده غير كافٍ.
- تعليمات Production والـrollback موثقة في [دليل النشر](docs/deployment/production.md).

## الأمان

لا تضع أسرارًا حقيقية في Git. استخدم secret store ومتغيرات البيئة، وراجع [SECURITY.md](SECURITY.md) و[threat model](docs/security/threat-model.md). الإعدادات غير الآمنة تجعل الـAPI يفشل مبكرًا في بيئة Production.

## الترخيص

مشروع خاص — جميع الحقوق محفوظة لمهندسيتو توريدات.
