# Feature inventory

| المجال | Client | Admin / CRM | Backend state |
|---|---|---|---|
| الهوية والشركات | OTP، كلمة مرور، تسجيل شركة، 2FA، جلسات؛ OAuth ينتظر credentials | تحقق، مستخدمون، أدوار ونطاقات | JWT + refresh rotation + tenant filters + audit؛ الجلسة الحالية Tenant واحد |
| الكتالوج | بحث، فلاتر، مقارنة، مفضلة، أسعار شركات | CRUD، متغيرات، عبوات، تكلفة، SEO، import | Products, pricing tiers, contract prices |
| السلة والشراء | سلال محفوظة، كوبون، Checkout، دفع | تشغيل الطلب والاسترداد والأرشيف | idempotency, approvals, invoices, inventory |
| RFQ | إنشاء ومقارنة وتفاوض وقبول | extraction، موردون، هوامش وإصدارات | versioned quotes and conversion |
| الطباعة | brief، شعار، تعليقات واعتماد | تصميم، عينات، إنتاج وجودة | immutable versions and production stages |
| المخزون والموردون | تتبع حالة الطلب | مخازن، جرد، batches، PO، 3-way match | immutable movements and reservations |
| CRM والعقود | حساب الشركة وفروعها | stages، tasks، credit، contracts، pricing | tenant commercial lifecycle |
| الشحن | تتبع وإثبات واستلام | shipments، routes، couriers، zones | delivery events and proof isolation |
| المالية والدعم | فواتير وميزانيات وتذاكر | accounting، returns، SLA، campaigns | ledger, refunds, support audit |
| النظام | maintenance/version gates | reports، settings، integrations، monitoring | encrypted secrets, backups, flags, IP blocks |

التفاصيل الدقيقة لكل شاشة ومسار وAPI وكيانات واختبارات موجودة في مصفوفة التغطية.
