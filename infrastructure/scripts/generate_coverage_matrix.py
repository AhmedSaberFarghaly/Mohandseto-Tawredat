"""Generates docs/screen-coverage-matrix.csv scaffold from the spec's screen ranges.

PDF page mapping: pdf_page = screen_no - 2 (verified: screen 8 -> p6, screen 47 -> p45,
screen 384 -> p382, screen 472 -> p470).
Names/routes are filled in per milestone as screens are implemented.
"""
import csv
import os

MODULES = [
    # (start, end, platform, module_en, module_ar)
    (15, 39, "client", "Registration & Company Verification", "التسجيل والتحقق من الشركة"),
    (40, 77, "client", "Home, Categories & Products", "الرئيسية والأقسام والمنتجات"),
    (78, 108, "client", "Printed & Custom Products", "المنتجات المطبوعة والمخصصة"),
    (109, 151, "client", "Cart & Checkout", "السلة وإتمام الطلب"),
    (152, 167, "client", "Internal Approvals", "الموافقات الداخلية"),
    (168, 203, "client", "RFQ", "طلبات عروض الأسعار"),
    (204, 235, "client", "Orders & Tracking", "الطلبات والتتبع"),
    (236, 255, "client", "Returns & Replacement", "المرتجعات والاستبدال"),
    (256, 279, "client", "Invoices & Payments", "الفواتير والمدفوعات"),
    (280, 293, "client", "Budgets & Cost Centers", "الميزانية ومراكز التكلفة"),
    (294, 320, "client", "Company Account & Users", "حساب الشركة والمستخدمون"),
    (321, 368, "client", "Notifications, Support & Settings", "الإشعارات والدعم والإعدادات"),
    (369, 381, "admin", "Login & Dashboards", "الدخول ولوحات المعلومات"),
    (382, 403, "admin", "Orders Management", "إدارة الطلبات"),
    (404, 425, "admin", "Quotes Management", "إدارة عروض الأسعار"),
    (426, 465, "admin", "Products, Categories & Content", "المنتجات والأقسام والمحتوى"),
    (466, 489, "admin", "Inventory & Warehouses", "المخزون والمستودعات"),
    (490, 508, "admin", "Suppliers & Procurement", "الموردون والمشتريات"),
    (509, 539, "admin", "Companies CRM", "إدارة الشركات CRM"),
    (540, 556, "admin", "Contracts & Special Pricing", "العقود والأسعار الخاصة"),
    (557, 580, "admin", "Printing & Design Management", "إدارة الطباعة والتصميم"),
    (581, 599, "admin", "Shipping & Delivery", "الشحن والتوصيل"),
    (600, 639, "admin", "Accounts & Customer Service", "الحسابات وخدمة العملاء"),
    (640, 699, "admin", "Campaigns, Permissions & Reports", "الحملات والصلاحيات والتقارير"),
    (700, 756, "admin", "Settings, Integrations & Monitoring", "الإعدادات والتكاملات والمراقبة"),
]

HEADERS = [
    "PDF Page", "Screen Number", "Arabic Name", "English Name", "Module",
    "Platform", "Route / Component", "State / Variant", "API Dependencies",
    "Database Entities", "Test IDs", "Implementation Status", "QA Status", "Notes",
]


def main() -> None:
    root = os.path.join(os.path.dirname(__file__), "..", "..")
    out = os.path.abspath(os.path.join(root, "docs", "screen-coverage-matrix.csv"))
    rows = []
    # design system / intro pages (screens 1-14 -> pdf pages 1-12 incl. covers)
    for n in range(1, 15):
        rows.append([max(n - 2, 1), n, "", "", "Design System & Scenarios",
                     "design-system", "", "", "", "", "", "Reference", "N/A", ""])
    for start, end, platform, mod_en, mod_ar in MODULES:
        for n in range(start, end + 1):
            rows.append([n - 2, n, "", "", f"{mod_ar} | {mod_en}",
                         platform, "", "", "", "", "", "Pending", "Pending", ""])
    with open(out, "w", newline="", encoding="utf-8-sig") as f:
        w = csv.writer(f)
        w.writerow(HEADERS)
        w.writerows(rows)
    print(f"wrote {len(rows)} rows -> {out}")


if __name__ == "__main__":
    main()
