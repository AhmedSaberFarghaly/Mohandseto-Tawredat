# Permissions matrix

المصدر التنفيذي للأدوار والصلاحيات هو `Infrastructure/DbSeeder.cs`; هذه الوثيقة تلخص حدود الوصول ولا تستبدل الاختبارات.

| الدور | النطاق الأساسي |
|---|---|
| super_admin | كل الوحدات والإجراءات الحساسة والاستعادة وFeature Flags |
| system_admin | الإعدادات والأدوار والتدقيق والمراقبة؛ بعض destructive actions محجوبة |
| auditor | قراءة الطلبات/RFQ/المالية/المخزون/CRM/التقارير والتدقيق |
| operations_manager | الطلبات والمخزون والشحن والتقارير |
| sales_manager / sales_agent | الطلبات وRFQ وCRM حسب مستوى الإدارة |
| quotes_officer | RFQ وعروض الأسعار |
| products_manager | الكتالوج وإدارة المنتجات |
| inventory / warehouse manager | المخزون والمستودعات |
| procurement_officer | الموردون والمشتريات مع قراءة المخزون |
| accountant | الفواتير والحسابات والتقارير |
| support_agent | الدعم مع قراءة الطلب وCRM |
| designer / printing officer | التصميم والطباعة |
| delivery_driver | مهام وإثباتات التوصيل المعيّنة له |
| company_owner | إدارة الشركة والمستخدمين والشراء والموافقات والمالية |
| purchasing / requester | إنشاء وقراءة الطلبات/RFQ وفق الدور |
| approver / department_manager | قرارات الموافقة والنطاق الإداري |
| finance_manager / billing_officer | الميزانيات والموافقات أو الفواتير |
| warehouse_officer | قراءة المخزون والطلبات |

يمكن تقييد موظف المنصة بفروع أو مخازن محددة عبر `UserAccessScope`. أي endpoint إداري جديد يجب أن يحدد Role/Permission صراحة ويضيف AuditLog إن كان mutation حساسًا.
