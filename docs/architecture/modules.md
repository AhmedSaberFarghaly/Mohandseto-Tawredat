# Application modules

الـBackend منظم حسب use-case داخل `Application/` مع كيانات الأعمال في `Domain/Entities` وEF Core في `Infrastructure/AppDbContext.cs`. Controllers رفيعة وتفوض للخدمات.

| مجموعة | وحدات رئيسية |
|---|---|
| Identity | Auth, Account, Company, AdminSystemAccess |
| Commerce | Catalog, Shopping, Orders, Finance, Budgets |
| Sourcing | RFQ, Quotes, Procurement, Contracts |
| Operations | Inventory, Printing, Shipping, Returns |
| Relationship | CRM, CustomerService, Engagement, Marketing |
| Platform | Reports, SystemSettings, Integrations, Monitoring |

كل كيان tenant-scoped يرث `TenantEntity` ويخضع Global Query Filter. العمليات الحساسة تضيف `AuditLog`، والأسرار تُشفّر بـData Protection أو تُحفظ كبصمة فقط.
