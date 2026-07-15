# Data dictionary

## قواعد مشتركة

- `BaseEntity`: Guid ID، Created/Updated/Deleted timestamps، actor IDs، soft delete وRowVersion.
- `TenantEntity`: يضيف `TenantId` ويُعزل تلقائيًا عبر Global Query Filter.
- كل الأوقات UTC، وتتحول للمنطقة الزمنية في العرض.
- الحركات المالية والمخزنية وسجلات التدقيق لا تُحذف فعليًا من مسارات المنتج.

## مجموعات البيانات

| المجموعة | أمثلة كيانات | الملكية / الحساسية |
|---|---|---|
| Identity | Tenants, Companies, Users, Roles, LoginAudits, RefreshTokens | PII؛ tenant isolation؛ tokens hashed |
| Catalog | Products, Variants, Prices, Categories, Brands | platform-managed؛ أسعار تعاقدية tenant-scoped |
| Commerce | Carts, CheckoutSessions, Orders, Invoices, Payments | tenant-scoped؛ idempotent payment attempts |
| RFQ | Rfqs, SupplierQuotes, CustomerQuotes, Negotiations | tenant-scoped مع نسخ عروض immutable |
| Operations | Warehouses, Stocks, Movements, Shipments, DeliveryProofs | مخزون ledger؛ ملفات معزولة |
| Finance | AccountingEntries, Refunds, CreditLimits, Periods | audit required؛ قيود إغلاق الفترات |
| CRM | Activities, Tasks, Contracts, PriceRevisions | platform staff، مرتبطة بالشركة |
| Platform | Settings, ApiKeys, Webhooks, Backups, Errors, Flags | أسرار encrypted/hash-only؛ super-admin mutations |

تفاصيل الأعمدة والقيود والفهارس تُراجع من ملفات `Domain/Entities/*.cs` و`AppDbContext.OnModelCreating` لتفادي انفصال الوثيقة عن schema الفعلية.
