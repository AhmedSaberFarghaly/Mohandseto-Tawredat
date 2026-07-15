# OpenAPI and API conventions

- في Development: شغّل API وافتح `/swagger` أو `/swagger/v1/swagger.json`.
- في Production: Swagger UI غير منشورة. احفظ artifact المواصفة من بيئة CI/Release إن كان مطلوبًا للشركاء.
- المصادقة: `Authorization: Bearer <JWT>`؛ لوحة الإدارة تستخدم BFF وHttpOnly cookies.
- الأخطاء: RFC-style Problem Details مع `title`, `status`, `code`, `traceId` دون stack trace.
- pagination تستخدم `page` و`pageSize`، والفلاتر server-side حيث توجد أحجام كبيرة.
- timestamps بصيغة ISO-8601 UTC، والعملة قيم decimal مع كود العملة.

## مسارات الصحة

| Path | الغرض |
|---|---|
| `/health/live` | العملية تعمل؛ لا يعتمد على قاعدة البيانات |
| `/health/ready` | قاعدة البيانات متاحة ويمكن توجيه traffic |
| `/health` | الفحص المجمع للتوافق الخلفي |

العقد التنفيذي مصدره Controllers وDTOs. يكشف CI أي كسر compile، وتغطي `ApiE2eTests` الدخول والرفض المجهول والكتالوج والمراقبة.
