# Milestone 38 — Custom and Printed Products

## النسبة

38% من خارطة التنفيذ الكاملة. أُغلقت رحلة الشاشات 78–108 وظيفيًا، بينما تبقى ملحقات Checkout المؤسسية قبل وسم `v0.4.0`.

## ما تم إنجازه

- 30 قالب منتج مطبوع مع موضع الطباعة وطريقتها والخامة واللون والمقاس وأبعاد الطباعة وعدد ألوانها.
- تقدير سعر من الخادم يشمل سعر المنتج ورسوم التجهيز وكل تعديلات الخيارات.
- رفع آمن لشعار أو تصميم بصيغ PNG/JPG/WebP/SVG/PDF/AI، مع مكتبة شعارات محفوظة ومعزولة لكل شركة.
- طلب تصميم من مهندسيتو مع Design Brief وتعليقات ونسخ Mockup واعتماد/رفض/طلب تعديل لأحدث نسخة فقط.
- عرض سعر نهائي بمدة صلاحية وقبول/رفض من العميل.
- إضافة المنتج المعتمد إلى السلة بالسعر والكمية المثبتين في العرض ثم مروره على Checkout المؤسسي.
- عدم بدء الإنتاج قبل إنشاء الطلب؛ الطلبات التي تحتاج موافقة داخلية تنتظر مرحلة الموافقات التالية.
- أمر إنتاج من ست مراحل، وعينات فعلية مؤمنة بنسخ واعتماد/رفض/تعديل قبل استكمال الإنتاج.
- فحص جودة موثق؛ لا يمكن إكمال مرحلة الجودة دون نتيجة ناجحة.
- عزل tenant على الطلبات والتعليقات والملفات والتصاميم والعينات والإنتاج.

## أهم APIs

- `GET /api/custom-products/templates` و`GET /api/custom-products/templates/{id}`.
- `GET /api/custom-products/logos`.
- `POST /api/custom-products/requests` (multipart).
- `GET /api/custom-products/requests` و`GET /api/custom-products/requests/{id}`.
- `POST /api/custom-products/requests/{id}/quote-response`.
- `POST /api/custom-products/requests/{id}/design-decision`.
- `POST /api/custom-products/requests/{id}/add-to-cart`.
- `POST /api/custom-products/requests/{id}/samples/{sampleId}/decision`.
- Admin: التسعير ونشر نسخ التصميم والعينات وتحديث مراحل الإنتاج.

## البيانات

- CustomProductTemplates، CustomizationOptions، PrintMethods، Materials، Colors، Sizes.
- CustomProductRequests، CustomRequestItems، LogoAssets، DesignBriefs، DesignVersions، DesignMockups، DesignComments، DesignApprovals.
- ProductionJobs، ProductionStages، ProductionSamples، QualityChecks.
- Migrations: `CustomProductsWorkflow` و`CustomProductSamples`.

## التحقق

- 23 اختبار Backend ناجحًا على SQLite حقيقي in-memory.
- 7 اختبارات Flutter و`flutter analyze` بلا مشاكل.
- اختبار دورة حياة: رفع ملفات → عرض سعر → نسختا تصميم → تعديل واعتماد → سلة → بدء إنتاج → عينة واعتماد → جاهزية.
- E2E HTTP على قاعدة مهاجرة جديدة: 30 قالبًا، multipart upload/download، وعزل طلب وملف شركة أخرى بنتيجة 404.
- E2E تجاري: عرض نهائي 7,500 ج.م → سلة بنفس الإجمالي → Checkout → `PendingApproval`، مع بقاء الإنتاج متوقفًا في `AwaitingOrderApproval`.
- تطبيق migrations من الصفر ناجح على قاعدة SQLite فارغة.
- مصفوفة الشاشات 78–108 محدثة إلى `Implemented / Automated`.

## المتبقي لإغلاق M4

- اختيار الموقع على الخريطة وتخزين الإحداثيات.
- مراكز التكلفة وفحص الميزانية وربطها بالطلب.
- مرفق أمر الشراء.
- بوابة الدفع بالبطاقة وWebhooks.
