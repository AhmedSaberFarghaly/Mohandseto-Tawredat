# System context

```mermaid
flowchart LR
    Company[موظفو الشركات] --> Client[Flutter Client]
    Staff[فريق المنصة] --> Admin[Next.js Admin / CRM]
    Client --> API[ASP.NET Core API]
    Admin --> BFF[Next.js HttpOnly-cookie BFF]
    BFF --> API
    API --> DB[(SQLite release store)]
    API --> Files[(Persistent files / backups)]
    API --> Providers[SMS / Email / WhatsApp / Payment / Maps]
```

- الـAPI هي حدود الثقة ومصدر الصلاحيات وعزل الشركات؛ الواجهة لا تُعد طبقة حماية.
- لوحة الإدارة لا تضع access/refresh tokens في JavaScript؛ تحفظها BFF في HttpOnly cookies.
- تكاملات الإنتاج تعمل عبر adapters وتظل credentials خارج Git.
- إصدار Docker الحالي single-instance مع SQLite وvolume دائم. الانتقال إلى SQL Server يحتاج migration set خاصًا بالمزود واختبار cutover.
