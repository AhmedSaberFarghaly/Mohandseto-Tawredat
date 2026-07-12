# MILESTONE 20 — Identity, Verification, and Admin Foundation

## النسبة

20% من خارطة التنفيذ الأساسية. الكتالوج بدأ بعد هذه البوابة ووصل المشروع حاليًا إلى نحو 26%.

## ما تم إنجازه

- Backend Auth كامل: OTP، بريد وكلمة مرور، Refresh rotation، Logout، تسجيل شركة ومستندات وحالات مراجعة.
- Flutter Auth flow من الدخول حتى إنشاء الشركة ورفع المستندات والمراجعة.
- Admin Login عبر BFF وHttpOnly cookies مع Role gate وDashboard shell متجاوب.
- Multi-tenancy وSoft delete وAudit fields وفلاتر العلاقات التابعة.

## الشاشات المنفذة

- Flutter: الشاشات والحالات 15–39، مع تسجيل الحالات الخارجية التي تحتاج Credentials كـPartial.
- Admin: شاشات الدخول والـDashboard الأساسية 369–381، مع توثيق الحالات الجزئية في Coverage Matrix.

## APIs

- `POST /api/auth/otp/request`
- `POST /api/auth/otp/verify`
- `POST /api/auth/login`
- `POST /api/auth/register-company`
- `POST /api/auth/refresh`
- `POST /api/auth/logout`
- `GET /api/auth/me`
- `GET /api/company/verification-status`
- `POST /api/company/documents`
- Admin session routes: login، refresh، logout.

## Entities / Migrations

- Tenant، Company، CompanyBranch، CompanyDocument، User، Role، Permission، OTP، RefreshToken، AuditLog.
- Migration: `InitialFoundation`.

## Tests

- 9 اختبارات Backend Auth على SQLite حقيقي in-memory.
- Flutter navigation test من Splash إلى Login.
- Admin lint وproduction build.
- Login E2E من Admin إلى API باستخدام HttpOnly session.

## Bugs fixed

- مطابقة Query filters على User dependents.
- تدوير Refresh Token وكشف إعادة استخدام Token مسروق.
- تصحيح حالة Tenant الفعلية `Active` في Flutter.
- منع تتبع SQLite WAL/SHM داخل Git.
- إزالة اعتماد build على تحميل Google Fonts عبر تجميع Cairo محليًا.

## Known issues

- Google وMicrosoft والـ2FA الحقيقي يحتاجون Credentials خارجية؛ الواجهات مسجلة ولا توقف بقية التنفيذ.
- فحص بصري حي للمتصفح لم يكن متاحًا في جلسة التنفيذ؛ PDF render وbuild checks نُفذا، ويُعاد الفحص الحي عند توفر سطح المتصفح.
- تنبيه npm متوسط داخل نسخة PostCSS التابعة لـNext.js؛ لا يوجد تحديث آمن غير كاسر معلن داخل النسخة المثبتة وقت الفحص.

## Screenshots

تمت مراجعة صفحات PDF المرجعية بصريًا. لقطات التطبيق النهائية تُضاف بعد تشغيل Browser/Device QA في بوابة الإصدار.

## Build instructions

```bash
dotnet test apps/api.Tests/Mohandseto.Api.Tests.csproj
cd apps/admin_web && npm run lint && npm run build
cd ../client_flutter && flutter analyze && flutter test
```

## Commit / Tag

- Implementation commit SHA: `7bcca05`
- Tag: `v0.2.0`

## المرحلة التالية

الكتالوج والرئيسية والبحث والمنتجات وAdmin Products CRUD والـseed، وهي قيد التنفيذ بالفعل.
