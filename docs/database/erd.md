# Database ERD — logical view

```mermaid
erDiagram
    TENANT ||--|| COMPANY : owns
    TENANT ||--o{ USER : contains
    USER }o--o{ ROLE : assigned
    ROLE }o--o{ PERMISSION : grants
    TENANT ||--o{ ORDER : places
    ORDER ||--|{ ORDER_ITEM : contains
    ORDER ||--o| INVOICE : billed
    ORDER ||--o{ SHIPMENT : fulfilled
    PRODUCT ||--o{ ORDER_ITEM : selected
    PRODUCT ||--o{ WAREHOUSE_STOCK : stocked
    WAREHOUSE ||--o{ WAREHOUSE_STOCK : holds
    RFQ ||--|{ RFQ_ITEM : requests
    RFQ ||--o{ CUSTOMER_QUOTE : produces
    COMPANY ||--o{ COMPANY_CONTRACT : signs
    COMPANY_CONTRACT ||--o{ COMPANY_CONTRACT_PRODUCT : prices
    SYSTEM_BACKUP ||--o{ SYSTEM_RESTORE_REQUEST : validates
```

هذا رسم منطقي لأهم العلاقات فقط. المصدر التنفيذي الكامل هو EF model snapshot في `apps/api/Migrations/AppDbContextModelSnapshot.cs`، وتطبق migrations من الصفر ضمن اختبار `MigrationIntegrityTests`.
