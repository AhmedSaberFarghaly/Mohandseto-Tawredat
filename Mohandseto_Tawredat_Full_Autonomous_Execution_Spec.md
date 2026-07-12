# مهندسيتو توريدات — وثيقة التنفيذ الشاملة والمستقلة
## Mohandseto Tawredat — Autonomous Full-Stack Implementation Master Specification

> **هذه الوثيقة هي أمر تنفيذ كامل ومصدر تعليمات رئيسي لـ Claude Code أو أي وكيل برمجي مستقل.**
>
> يجب قراءتها مع ملف التصميم المرفق:
>
> `Mohandseto_Tawredat_Final design.pdf`
>
> مستودع GitHub المستهدف:
>
> `git@github.com:AhmedSaberFarghaly/Mohandseto-Tawredat.git`

---

# 0. الأمر التنفيذي الأعلى

أنت المسؤول التقني الكامل عن تحليل وتصميم وبناء واختبار وتأمين وتوثيق ونشر مشروع **مهندسيتو توريدات** من الصفر إلى منتج Production-Ready.

تعامل مع نفسك كفريق كامل يضم:

- CTO / Technical Product Owner
- Principal Software Architect
- Senior Flutter Engineer
- Senior React / Next.js Engineer
- Senior .NET Backend Engineer
- Database Architect
- DevOps Engineer
- QA Automation Engineer
- Application Security Engineer
- UI/UX Implementation Specialist
- Arabic RTL Accessibility Specialist
- Product Data & Seed Data Engineer
- Technical Writer

المطلوب ليس Prototype، وليس مجموعة شاشات ثابتة، وليس Demo بصريًا فقط.

المطلوب هو **نظام حقيقي كامل وديناميكي ومترابط**، بنفس تصميم وصفحات وسلوك ملف الـPDF، ويعمل بقاعدة بيانات فعلية وواجهات API حقيقية وصلاحيات وأدوار واختبارات وتشغيل ونشر.

---

# 1. بوابة ما قبل البداية — إلزامية

## 1.1 لا تبدأ كتابة كود المنتج مباشرة

قبل تنفيذ أي Feature أو إنشاء Architecture نهائية، نفّذ مرحلة فحص كاملة تشمل:

1. قراءة ملف الـPDF كاملًا، صفحة صفحة.
2. استخراج جميع الشاشات والحالات والمكونات والرحلات.
3. فحص مستودع GitHub بالكامل.
4. معرفة هل المستودع فارغ أم يحتوي على كود قائم.
5. فحص الفروع والـActions والـIssues والملفات الحالية.
6. فحص بيئة الجهاز والأدوات المثبتة.
7. تحديد التقنيات والإصدارات المناسبة.
8. إنشاء Screen Coverage Matrix.
9. إنشاء Feature Inventory.
10. إنشاء Database Domain Map.
11. إنشاء Integration Inventory.
12. تحديد المخاطر والثغرات والغموض.
13. تقدير الوقت الفعلي للتنفيذ.
14. تحديد ما تحتاجه من صاحب المشروع فقط.

## 1.2 أول رد إلزامي قبل البداية

بعد الفحص، أرسل لصاحب المشروع تقريرًا واحدًا فقط بعنوان:

`PRE-START AUDIT & EXECUTION ESTIMATE`

ويجب أن يحتوي على:

- عدد صفحات ملف التصميم.
- عدد شاشات تطبيق العميل.
- عدد شاشات لوحة الإدارة والـCRM.
- عدد صفحات نظام التصميم والسيناريوهات.
- حالة مستودع GitHub.
- التقنيات المقترحة مع أسباب مختصرة.
- Architecture المقترحة.
- قاعدة البيانات المقترحة.
- خطة المراحل العشر.
- المدة التقديرية لكل مرحلة.
- المدة الإجمالية المتوقعة.
- المخاطر الأساسية.
- البيانات أو الصلاحيات أو الحسابات المطلوبة.
- ما يمكن تنفيذه فورًا بدون أي تدخل.
- ما يتطلب Credentials لاحقًا.
- سؤال واحد فقط: **هل أبدأ التنفيذ الكامل؟**

بعد موافقة صاحب المشروع على البداية:

- لا تسأل عن القرارات التقنية العادية.
- لا تطلب منه اختيار مكتبة أو Architecture أو Naming أو Folder Structure.
- لا تطلب منه مراجعة كل شاشة.
- اتخذ جميع القرارات الفنية بنفسك.
- تواصل معه فقط عند وجود Credential خارجي ضروري، قرار قانوني/مالي، أو إجراء غير قابل للتراجع خارج نطاق المشروع.

## 1.3 التقدير المبدئي الذي يجب استخدامه كمرجع

بناءً على حجم ملف التصميم الذي يتضمن مئات الشاشات لتطبيق العميل ولوحة الإدارة والـCRM، ومستودع يبدأ من الصفر، فإن التقدير المبدئي الواقعي للتنفيذ الكامل Production-Ready هو:

- **الحد المتفائل:** 16 أسبوعًا.
- **المدى الواقعي:** 18 إلى 22 أسبوعًا.
- **الحد الآمن مع تكاملات خارجية ونشر كامل:** 22 إلى 26 أسبوعًا.

هذا التقدير يجب تحديثه بعد الفحص الفعلي، وليس قبله.

لا تقدم وعدًا غير واقعي مثل إنهاء النظام الكامل خلال أيام قليلة.

---

# 2. الصلاحيات والاستقلالية

صاحب المشروع يمنحك صلاحية كاملة لاتخاذ القرارات الفنية داخل نطاق مشروع **Mohandseto Tawredat**.

مسموح لك داخل نطاق المشروع:

- Clone المستودع.
- إنشاء وحذف وتعديل الملفات والمجلدات.
- بناء Architecture جديدة.
- استبدال كود ضعيف أو غير صالح.
- إنشاء فروع.
- Commit وPush.
- إنشاء Pull Requests.
- تعديل إعدادات CI/CD المرتبطة بالمشروع.
- إنشاء Docker وDatabase migrations.
- إضافة المكتبات الضرورية.
- حذف المكتبات غير المستخدمة.
- Refactor واسع.
- كتابة Scripts وSeeders وGenerators.
- تشغيل الاختبارات.
- إصلاح الأخطاء دون الرجوع لصاحب المشروع.
- إعادة ترتيب الأولويات الفنية.
- تحسين التصميم عند الحاجة بشرط عدم مخالفة ملف الـPDF.

## حدود الصلاحية

هذه الصلاحية لا تعني:

- حذف ملفات شخصية خارج مجلد المشروع.
- حذف مستودعات أخرى.
- حذف قواعد بيانات أو Cloud Resources لا تخص المشروع.
- نشر Secrets داخل Git.
- تنفيذ مدفوعات أو شراء خدمات دون موافقة.
- حذف بيانات Production حقيقية دون Backup وخطة Rollback.
- إجراء تغييرات غير قابلة للتراجع في حسابات خارجية دون إخطار.

أي عملية حذف داخل المشروع يجب أن تكون مبنية على سبب فني واضح، وأن تكون قابلة للاسترجاع من Git.

---

# 3. مصادر الحقيقة وترتيب الأولوية

استخدم مصادر الحقيقة بهذا الترتيب:

1. ملف `Mohandseto_Tawredat_Final design.pdf`.
2. هذه الوثيقة.
3. السلوك التجاري المنطقي لنظام B2B Procurement.
4. الكود الموجود في المستودع إن وُجد وكان متوافقًا مع التصميم.
5. أفضل الممارسات الهندسية الحديثة.

في حالة تعارض الكود مع التصميم:

- التصميم والوظيفة المحددة في الـPDF لهما الأولوية.
- لا تحافظ على كود قديم لمجرد أنه موجود.
- نفّذ Migration أو Refactor يحافظ على البيانات إن كانت موجودة.

في حالة عدم وضوح جزئية داخل الـPDF:

- استنتج السلوك الأكثر منطقية واتساقًا مع باقي النظام.
- وثق الافتراض في `docs/assumptions.md`.
- لا توقف المشروع بسبب تفاصيل صغيرة.

---

# 4. تعريف المنتج

**مهندسيتو توريدات** منصة B2B متكاملة لتوريد احتياجات الشركات والمكاتب والمصانع.

ليست متجر تجزئة عاديًا.

النظام يدير دورة المشتريات المؤسسية كاملة، ومنها:

- تسجيل الشركات والتحقق منها.
- إدارة فروع الشركات.
- إدارة مستخدمي الشركة.
- الأقسام والصلاحيات.
- مراكز التكلفة.
- الميزانيات.
- الحدود الائتمانية.
- كتالوج وأسعار خاصة لكل شركة.
- أسعار كميات.
- منتجات عادية.
- منتجات مطبوعة بشعار الشركة.
- طلبات عروض أسعار RFQ.
- موافقات داخلية متعددة المستويات.
- سلة وCheckout مؤسسي.
- أوامر شراء.
- طلبات دورية.
- فواتير ومدفوعات وكشوف حساب.
- مرتجعات واستبدال.
- مخزون ومستودعات.
- موردون ومشتريات.
- تجهيز Picking وPacking.
- شحن وتوصيل وتتبع.
- إدارة مندوبي التوصيل.
- CRM للشركات.
- عقود وأسعار خاصة.
- خدمة عملاء وتذاكر.
- حملات وإشعارات.
- تقارير وتحليلات.
- صلاحيات دقيقة وسجل تدقيق.
- مراقبة النظام والتكاملات وقاعدة البيانات.

---

# 5. حجم النظام المطلوب

يجب تنفيذ كل الشاشات والحالات الواردة في ملف الـPDF.

الهيكل المرجعي يتضمن تقريبًا:

- **368 شاشة وحالة لتطبيق العميل.**
- **388 شاشة وحالة للوحة الإدارة والـCRM.**
- **13 صفحة لنظام التصميم والسيناريوهات وقواعد التنفيذ.**

إجمالي الشاشات والحالات الوظيفية: **756** تقريبًا، بخلاف صفحات الغلاف والفهرسة.

لا تتعامل مع كل صفحة على أنها Route مستقلة بالضرورة؛ بعض الصفحات تمثل:

- State.
- Variant.
- Dialog.
- Bottom Sheet.
- Empty State.
- Error State.
- Confirmation.
- Detail View.
- Success View.
- Loading View.

لكن يجب أن يكون لكل صفحة في الـPDF مقابل قابل للتتبع داخل:

`docs/screen-coverage-matrix.csv`

ويحتوي على:

- PDF Page.
- Screen Number.
- Arabic Name.
- English Name.
- Module.
- Platform.
- Route / Component.
- State / Variant.
- API Dependencies.
- Database Entities.
- Test IDs.
- Implementation Status.
- QA Status.
- Notes.

الهدف النهائي:

`Missing Screens = 0`

---

# 6. خريطة تطبيق العميل

## 6.1 التسجيل والتحقق من الشركة — الشاشات 15 إلى 39

يشمل:

- Splash.
- اختيار اللغة.
- Onboarding.
- طلب صلاحية الإشعارات.
- تحديد الموقع والمحافظة.
- تسجيل الدخول برقم الهاتف.
- تسجيل الدخول بالبريد وكلمة المرور.
- OTP.
- إعادة إرسال الرمز.
- استعادة كلمة المرور.
- إنشاء حساب شركة.
- بيانات الشركة.
- السجل التجاري.
- البطاقة الضريبية.
- خطاب التفويض.
- رفع المستندات.
- التحقق من المستندات.
- حالة المراجعة.
- الطلب المرفوض.
- إعادة رفع المستند.
- انتظار التفعيل.
- نجاح التفعيل.
- إنشاء المستخدم الإداري الأول.

## 6.2 الرئيسية والأقسام والمنتجات — الشاشات 40 إلى 77

يشمل:

- Home.
- Company Selector.
- Branch Selector.
- Search.
- Search Suggestions.
- Recent Searches.
- Categories.
- Subcategories.
- Product Listing.
- Grid/List.
- Filters.
- Sort.
- Brand filters.
- Price filters.
- Stock filters.
- Product Details.
- Product Variants.
- Quantity Tiers.
- Contract Price.
- Related Products.
- Recently Viewed.
- Favorites.
- Compare.
- Out of Stock.
- Backorder.
- Product Documents.
- Warranty.
- Delivery Estimate.

## 6.3 المنتجات المطبوعة والمخصصة — الشاشات 78 إلى 108

يشمل:

- Printed Products Catalog.
- Product customization.
- اختيار اللون والخامة والمقاس.
- تحديد الكمية.
- رفع شعار الشركة.
- موضع الطباعة.
- نوع الطباعة.
- إدخال النص.
- رفع ملف التصميم.
- طلب تصميم من مهندسيتو.
- Design Brief.
- Mockup Preview.
- طلب تعديل.
- إضافة ملاحظات.
- اعتماد التصميم.
- رفض التصميم.
- Version History.
- Design Approval.
- Production Status.
- Custom Product Quote.

## 6.4 السلة وإتمام الطلب — الشاشات 109 إلى 151

يشمل:

- Cart.
- Saved for Later.
- Quantity Updates.
- Company/Branch selection.
- Cost Center.
- Budget validation.
- Shipping address.
- Receiver.
- Required date.
- Delivery method.
- Payment method.
- Credit terms.
- Purchase Order.
- Attachments.
- Tax calculation.
- Discount calculation.
- Shipping calculation.
- Approval requirements.
- Review order.
- Submit order.
- Success.
- Failure.
- Pending approval.
- Partial availability.
- Split shipment.
- Retry payment.

## 6.5 الموافقات الداخلية — الشاشات 152 إلى 167

يشمل:

- Approval Inbox.
- Approval Details.
- Approval Chain.
- Approve.
- Reject.
- Request Changes.
- Delegate.
- Comments.
- Attachments.
- Budget conflict.
- Approval history.
- Escalation.
- Notification.
- Final approval.

## 6.6 طلبات عروض الأسعار RFQ — الشاشات 168 إلى 203

يشمل:

- إنشاء RFQ.
- من الكتالوج.
- منتجات حرة غير موجودة.
- رفع Excel.
- رفع PDF.
- رفع صور.
- استخراج العناصر.
- مراجعة العناصر.
- تحديد تاريخ الاحتياج.
- إضافة مرفقات.
- إرسال الطلب.
- حالة RFQ.
- استلام عرض.
- مقارنة عروض.
- التفاوض.
- قبول.
- رفض.
- طلب تعديل.
- تنزيل PDF.
- مشاركة.
- تحويل إلى طلب.
- Expired Quote.
- Requote.

## 6.7 الطلبات والتتبع — الشاشات 204 إلى 235

يشمل:

- Orders list.
- Order filters.
- Order details.
- Approval status.
- Processing.
- Picking.
- Packing.
- Shipment created.
- Driver assigned.
- Live tracking.
- Delivery ETA.
- Delivery proof.
- Signature.
- Photo proof.
- Partial delivery.
- Delayed delivery.
- Completed.
- Reorder.
- Recurring order.
- Schedule monthly order.
- Cancel order.
- Support request.

## 6.8 المرتجعات والاستبدال — الشاشات 236 إلى 255

يشمل:

- اختيار الطلب.
- اختيار المنتجات.
- سبب الإرجاع.
- صور الحالة.
- الكمية.
- Return eligibility.
- Pickup.
- Replacement.
- Refund method.
- Review.
- Submitted.
- Approved.
- Rejected.
- In transit.
- Inspection.
- Refund completed.
- Replacement delivered.

## 6.9 الفواتير والمدفوعات — الشاشات 256 إلى 279

يشمل:

- Invoices list.
- Invoice details.
- PDF invoice.
- Tax invoice.
- Outstanding balance.
- Credit limit.
- Payment methods.
- Card payment.
- Bank transfer.
- Upload receipt.
- Payment pending.
- Payment failed.
- Payment success.
- Installments/terms where applicable.
- Statements.
- Reconciliation.
- Download/share.
- Refunds.

## 6.10 الميزانية ومراكز التكلفة — الشاشات 280 إلى 293

يشمل:

- Cost Centers.
- Budget overview.
- Monthly budget.
- Used/remaining.
- Limits.
- Alerts.
- Transactions.
- Department budget.
- Budget request.
- Approval.
- History.
- Export.

## 6.11 حساب الشركة والمستخدمون — الشاشات 294 إلى 320

يشمل:

- Company profile.
- Branches.
- Addresses.
- Users.
- Invite user.
- Roles.
- Permissions.
- Departments.
- Approval matrix.
- Documents.
- Tax data.
- Authorized contacts.
- Security.
- Sessions.
- Devices.
- Password.
- 2FA.
- Deactivate user.

## 6.12 الإشعارات والدعم والإعدادات — الشاشات 321 إلى 368

يشمل:

- Notifications center.
- Notification details.
- Preferences.
- Support tickets.
- Create ticket.
- Chat.
- Attachments.
- Ticket status.
- FAQ.
- Terms.
- Privacy.
- Language.
- Theme.
- Accessibility.
- About.
- Version.
- Logout.
- Delete account flow.
- Offline.
- Maintenance.
- Session expired.
- Permission denied.
- Error and empty states.

---

# 7. خريطة لوحة الإدارة والـCRM

## 7.1 الدخول ولوحات المعلومات — الشاشات 369 إلى 381

- Admin login.
- 2FA.
- Password recovery.
- Role-based dashboards.
- KPIs.
- Alerts.
- Tasks.
- Recent activity.
- Global search.

## 7.2 إدارة الطلبات — الشاشات 382 إلى 403

- Orders table.
- Filters.
- Details.
- Approval review.
- Inventory validation.
- Assign staff.
- Picking.
- Packing.
- Split shipment.
- Invoice creation.
- Cancel.
- Refund.
- Recurring orders.
- Status transitions.
- Order audit trail.

## 7.3 إدارة عروض الأسعار — الشاشات 404 إلى 425

- RFQ queue.
- RFQ details.
- File extraction.
- Catalog matching.
- Free items.
- Supplier price request.
- Costing.
- Margin.
- Quote builder.
- Negotiation.
- Versioning.
- Approval.
- PDF generation.
- Send.
- Expiry.
- Conversion to order.

## 7.4 المنتجات والأقسام والمحتوى — الشاشات 426 إلى 465

- Products CRUD.
- Categories.
- Subcategories.
- Brands.
- Attributes.
- Variants.
- Units.
- Images.
- Documents.
- Product pricing.
- Quantity tiers.
- Company pricing.
- SEO/client content.
- Banners.
- Promotions.
- Printed products.
- Customization options.
- Product import/export.
- Bulk actions.

## 7.5 المخزون والمستودعات — الشاشات 466 إلى 489

- Warehouses.
- Locations/bins.
- Stock levels.
- Reserved stock.
- Available stock.
- Transfers.
- Adjustments.
- Stock counts.
- Receiving.
- Dispatch.
- Batch/lot.
- Serial numbers.
- Expiry.
- Reorder points.
- Stock alerts.
- Stock ledger.

## 7.6 الموردون والمشتريات — الشاشات 490 إلى 508

- Suppliers.
- Supplier profile.
- Quotations.
- Purchase requests.
- Purchase orders.
- Receiving.
- Supplier invoices.
- Returns.
- Price lists.
- Performance.
- Lead time.
- Payment terms.
- Documents.
- Supplier comparison.

## 7.7 إدارة الشركات CRM — الشاشات 509 إلى 539

- Companies list.
- Company profile.
- Contacts.
- Branches.
- Users.
- Orders.
- RFQs.
- Contracts.
- Accounts.
- Activity.
- Support.
- Documents.
- Calls.
- Meetings.
- Opportunities.
- Sales assignment.
- Notes.
- Timeline.
- Segmentation.
- Credit limit.
- Account status.

## 7.8 العقود والأسعار الخاصة — الشاشات 540 إلى 556

- Contracts.
- Contract versions.
- Start/end.
- Company price list.
- Product-specific price.
- Discount rules.
- Quantity rules.
- SLA.
- Payment terms.
- Credit rules.
- Renewal.
- Expiry alerts.
- Approval.
- Attachments.

## 7.9 إدارة الطباعة والتصميم — الشاشات 557 إلى 580

- Custom orders.
- Design queue.
- Design brief.
- Logo files.
- Mockups.
- Designer assignment.
- Revisions.
- Comments.
- Approval.
- Production order.
- Materials.
- Print type.
- Production tracking.
- Quality check.
- Completion.

## 7.10 الشحن والتوصيل — الشاشات 581 إلى 599

- Shipments.
- Driver assignment.
- Routes.
- Map.
- Live status.
- ETA.
- Delivery proof.
- Failed delivery.
- Reschedule.
- Partial delivery.
- Handover.
- Delivery zones.
- Shipping rates.
- Driver performance.

## 7.11 الحسابات وخدمة العملاء — الشاشات 600 إلى 639

- Invoices.
- Payments.
- Receipts.
- Refunds.
- Credit notes.
- Statements.
- Aging.
- Reconciliation.
- Customer balance.
- Payment gateways.
- Tickets.
- Chat.
- SLA.
- Escalations.
- Canned replies.
- Categories.
- Satisfaction.
- Support analytics.

## 7.12 الحملات والصلاحيات والتقارير — الشاشات 640 إلى 699

- Campaigns.
- Segments.
- Email/SMS/Push.
- Templates.
- Scheduling.
- Results.
- Roles.
- Permissions.
- Permission matrix.
- User administration.
- Audit logs.
- Custom reports.
- Report builder.
- Filters.
- Fields.
- Charts.
- Export.
- Scheduled reports.
- Operational dashboards.
- Financial reports.
- Inventory reports.
- Sales reports.
- Customer reports.

## 7.13 الإعدادات والتكاملات والمراقبة — الشاشات 700 إلى 756

- General settings.
- Company settings.
- Taxes.
- Currency.
- Units.
- Number sequences.
- Email.
- SMS.
- WhatsApp.
- Push.
- Maps.
- Payment gateways.
- Storage.
- Backups.
- API keys.
- Webhooks.
- Integration logs.
- Job queue.
- Health checks.
- Database status.
- Storage status.
- Error monitoring.
- Failed logins.
- Security events.
- Sessions.
- Rate limits.
- Feature flags.
- Maintenance mode.
- System logs.
- Version/release information.

---

# 8. المستخدمون والأدوار

يجب دعم أدوار مرنة وليست Hard-Coded، ومنها:

## تطبيق العميل

- موظف مشتريات.
- مسؤول إداري.
- مسؤول مخازن.
- مدير إدارة.
- مدير مالي.
- صاحب الشركة.
- مسؤول موافقات.
- مسؤول فواتير.
- مستخدم عادي مخول بالطلب.

## لوحة الإدارة

- Super Admin.
- مدير مبيعات.
- موظف مبيعات.
- مسؤول عروض أسعار.
- مسؤول منتجات.
- مسؤول مخزون.
- مسؤول مستودع.
- مسؤول مشتريات.
- مسؤول حسابات.
- خدمة عملاء.
- مصمم جرافيك.
- مسؤول طباعة.
- مندوب توصيل.
- مدير تشغيل.
- مدير نظام.
- Auditor / Read Only.

يجب بناء:

- RBAC.
- Fine-grained permissions.
- Tenant-aware authorization.
- Resource ownership checks.
- Approval authority limits.
- Monetary approval thresholds.
- Audit trail لكل إجراء حساس.

---

# 9. التقنيات المرجعية المقترحة

يمكن تعديل إصدارات الأدوات بعد الفحص، لكن لا تغيّر التوجه الأساسي دون سبب قوي.

## 9.1 تطبيق العميل

- Flutter stable.
- Dart stable.
- Android.
- iOS.
- Tablet responsive.
- Flutter Web لتجربة العميل على الويب عند الحاجة.
- Riverpod أو Bloc؛ اختر واحدًا فقط واستخدمه باتساق.
- GoRouter.
- Dio.
- Freezed / json_serializable.
- Secure Storage.
- Local cache باستخدام Drift/SQLite أو Isar.
- Firebase Cloud Messaging.
- Crash reporting.
- Arabic RTL + English LTR.
- Deep links.
- Background sync where allowed.

## 9.2 لوحة الإدارة والـCRM

- Next.js stable.
- React.
- TypeScript strict.
- App Router.
- TanStack Query.
- React Hook Form.
- Zod.
- TanStack Table.
- Component system منظم وقابل للتخصيص.
- Charts library مستقرة.
- RTL/LTR support.
- Responsive desktop/tablet.
- Server-side pagination/filtering/sorting.

## 9.3 Backend

التوجه المفضل:

- ASP.NET Core Web API.
- .NET 8 LTS أو الإصدار LTS المستقر وقت التنفيذ.
- Clean Architecture أو Modular Monolith منظم.
- EF Core.
- FluentValidation.
- MediatR عند وجود فائدة حقيقية.
- SignalR للتحديثات الحية.
- Hangfire أو Quartz للمهام المجدولة.
- Serilog.
- OpenTelemetry.
- Health Checks.
- Swagger / OpenAPI.
- API versioning.
- ProblemDetails.
- Idempotency keys للعمليات الحساسة.

ابدأ Modular Monolith قويًا بدل Microservices مبكرة، مع حدود Modules واضحة تسمح بالفصل مستقبلًا.

## 9.4 قاعدة البيانات

- Microsoft SQL Server.
- EF Core migrations.
- Redis للـcache والـdistributed locks والـrate limiting عند الحاجة.
- Object Storage متوافق مع S3 للصور والمستندات.
- Full-text/search engine عند الحاجة بعد قياس الأداء.
- Local development عبر Docker Compose.

## 9.5 DevOps

- Dockerfiles.
- Docker Compose.
- GitHub Actions.
- Build + lint + test + security scan.
- Environment separation:
  - local
  - development
  - staging
  - production
- Infrastructure documentation.
- Automated DB migrations بشكل آمن.
- Backup and restore scripts.
- Release tags.
- Changelog.
- Rollback procedure.

---

# 10. Architecture المطلوبة

استخدم Monorepo منظمًا، مثل:

```text
Mohandseto-Tawredat/
├── apps/
│   ├── client_flutter/
│   ├── admin_web/
│   └── api/
├── packages/
│   ├── design_tokens/
│   ├── api_contracts/
│   └── shared_docs/
├── infrastructure/
│   ├── docker/
│   ├── database/
│   ├── scripts/
│   └── monitoring/
├── docs/
│   ├── architecture/
│   ├── api/
│   ├── database/
│   ├── qa/
│   ├── security/
│   ├── milestones/
│   └── screen-coverage/
├── .github/
│   ├── workflows/
│   └── ISSUE_TEMPLATE/
├── README.md
├── CONTRIBUTING.md
├── CHANGELOG.md
├── SECURITY.md
├── .env.example
└── docker-compose.yml
```

Backend Modules:

- Identity.
- Tenancy.
- Companies.
- Users.
- RolesPermissions.
- Catalog.
- Pricing.
- CustomProducts.
- Cart.
- Checkout.
- Approvals.
- RFQ.
- Orders.
- RecurringOrders.
- Returns.
- Invoicing.
- Payments.
- Budgets.
- CostCenters.
- Inventory.
- Warehouses.
- Suppliers.
- Procurement.
- CRM.
- Contracts.
- DesignProduction.
- Shipping.
- Delivery.
- CustomerSupport.
- Notifications.
- Campaigns.
- Reports.
- Documents.
- Integrations.
- Audit.
- SystemMonitoring.
- Settings.

كل Module يجب أن يمتلك:

- Domain model.
- Application use cases.
- Infrastructure implementation.
- API endpoints.
- Validation.
- Authorization.
- Tests.
- Audit events.
- Documentation.

---

# 11. قاعدة البيانات — إلزامية لكل البيانات

كل البيانات التي تظهر في النظام يجب أن تأتي من قاعدة البيانات أو من Configuration/Localization منظّم.

ممنوع الاعتماد على Hard-Coded fake data داخل الشاشات النهائية.

## 11.1 قواعد عامة

- كل Business Entity لها Primary Key.
- استخدم UUID/Guid أو strategy موحدًا.
- `TenantId` لكل البيانات الخاصة بالشركات.
- CreatedAt / CreatedBy.
- UpdatedAt / UpdatedBy.
- Soft delete عند الحاجة.
- Row version / concurrency token.
- Audit log.
- Status history.
- Domain events.
- Transactions للعمليات المالية والمخزنية.
- Decimal precision صحيح للأسعار والضرائب.
- UTC في التخزين، Local Time في العرض.
- Indexes مدروسة.
- Unique constraints.
- Foreign keys.
- Check constraints.
- No orphan records.

## 11.2 الكيانات الأساسية

على الأقل:

### Identity & Tenancy

- Tenants.
- Companies.
- CompanyBranches.
- CompanyDocuments.
- CompanyVerifications.
- Users.
- UserProfiles.
- Roles.
- Permissions.
- RolePermissions.
- UserRoles.
- UserSessions.
- Devices.
- OtpCodes.
- PasswordResetTokens.
- TwoFactorMethods.
- LoginAttempts.

### Organization

- Departments.
- CostCenters.
- Budgets.
- BudgetPeriods.
- BudgetTransactions.
- ApprovalPolicies.
- ApprovalLevels.
- ApprovalAssignments.
- ApprovalRequests.
- ApprovalActions.
- Delegations.

### Catalog

- Categories.
- CategoryTranslations.
- Brands.
- Products.
- ProductTranslations.
- ProductImages.
- ProductDocuments.
- ProductAttributes.
- ProductAttributeValues.
- ProductVariants.
- UnitsOfMeasure.
- ProductUnits.
- ProductPrices.
- QuantityPriceTiers.
- CompanyProductPrices.
- ContractPrices.
- ProductAvailability.
- ProductWarehouses.
- Favorites.
- CompareItems.
- RecentlyViewed.

### Custom & Printed Products

- CustomProductTemplates.
- CustomizationOptions.
- PrintMethods.
- Materials.
- Colors.
- Sizes.
- CustomProductRequests.
- CustomRequestItems.
- LogoAssets.
- DesignBriefs.
- DesignVersions.
- DesignMockups.
- DesignComments.
- DesignApprovals.
- ProductionJobs.
- ProductionStages.
- QualityChecks.

### Shopping & Orders

- Carts.
- CartItems.
- SavedItems.
- CheckoutSessions.
- Orders.
- OrderItems.
- OrderAddresses.
- OrderStatusHistory.
- OrderNotes.
- OrderAttachments.
- PurchaseOrders.
- RecurringOrderSchedules.
- RecurringOrderRuns.
- OrderCancellations.

### RFQ

- Rfqs.
- RfqItems.
- RfqAttachments.
- RfqExtractedItems.
- RfqItemMappings.
- SupplierQuoteRequests.
- SupplierQuotes.
- SupplierQuoteItems.
- CustomerQuotes.
- CustomerQuoteVersions.
- CustomerQuoteItems.
- QuoteNegotiations.
- QuoteApprovals.

### Inventory

- Warehouses.
- WarehouseZones.
- WarehouseBins.
- StockItems.
- StockBalances.
- StockReservations.
- StockMovements.
- StockTransfers.
- StockCounts.
- StockAdjustments.
- GoodsReceipts.
- GoodsReceiptItems.
- Lots.
- SerialNumbers.
- ReorderRules.

### Suppliers & Procurement

- Suppliers.
- SupplierContacts.
- SupplierDocuments.
- SupplierProducts.
- SupplierPriceLists.
- PurchaseRequests.
- PurchaseRequestItems.
- SupplierPurchaseOrders.
- SupplierPurchaseOrderItems.
- SupplierInvoices.
- SupplierPayments.
- SupplierReturns.
- SupplierPerformanceMetrics.

### Shipping & Delivery

- Shipments.
- ShipmentItems.
- ShippingZones.
- ShippingRates.
- Drivers.
- DriverVehicles.
- DeliveryRoutes.
- DeliveryStops.
- TrackingEvents.
- DeliveryAttempts.
- DeliveryProofs.
- Signatures.
- DeliveryPhotos.

### Finance

- Invoices.
- InvoiceItems.
- TaxRules.
- Payments.
- PaymentTransactions.
- PaymentMethods.
- BankTransfers.
- PaymentReceipts.
- Refunds.
- CreditNotes.
- AccountStatements.
- LedgerEntries.
- ReconciliationRecords.
- CreditLimits.

### CRM

- Leads.
- Opportunities.
- Accounts.
- Contacts.
- Activities.
- Calls.
- Meetings.
- Notes.
- Tasks.
- Contracts.
- ContractVersions.
- ContractDocuments.
- AccountManagers.
- Segments.

### Support & Communication

- SupportTickets.
- TicketMessages.
- TicketAttachments.
- TicketCategories.
- TicketSlaPolicies.
- TicketEscalations.
- CannedResponses.
- NotificationTemplates.
- Notifications.
- NotificationDeliveries.
- Campaigns.
- CampaignAudiences.
- CampaignMessages.
- CampaignResults.

### System

- Files.
- FileVersions.
- Settings.
- FeatureFlags.
- IntegrationSettings.
- Webhooks.
- WebhookDeliveries.
- BackgroundJobs.
- JobExecutions.
- AuditLogs.
- SecurityEvents.
- ErrorLogs.
- HealthCheckResults.
- DataExports.
- ImportJobs.

## 11.3 Multi-Tenancy

يجب منع تسرب البيانات بين الشركات.

نفّذ:

- Tenant resolution.
- Global query filters.
- Tenant-aware cache keys.
- Tenant-aware file paths.
- Tenant-aware jobs.
- Authorization checks.
- Automated tests تحاول الوصول لبيانات شركة أخرى ويجب أن تفشل.

---

# 12. بيانات المنتجات والـSeed Data

## 12.1 الهدف

النظام يجب أن يعمل فور تشغيله ببيانات حقيقية تجريبية منظمة، وليس بواجهات فارغة.

## 12.2 استخراج البيانات

استخرج من ملف التصميم كل ما يمكن استخراجه من:

- أسماء المنتجات.
- الأقسام.
- العلامات.
- وحدات البيع.
- الأسعار.
- حالات المخزون.
- حدود الكمية.
- صور المنتجات.
- عينات الطلبات.
- الشركات التجريبية.
- المستخدمين التجريبيين.
- الموردين.
- المستودعات.
- الفواتير.
- عروض الأسعار.
- الشحنات.
- التقارير.

أنشئ:

- `seed/base/`
- `seed/demo/`
- `seed/test/`

## 12.3 الحد الأدنى للبيانات التجريبية

أنشئ على الأقل:

- 12 قسمًا رئيسيًا.
- 40 قسمًا فرعيًا.
- 250 منتجًا متنوعًا.
- 30 منتجًا مطبوعًا أو مخصصًا.
- 10 علامات تجارية.
- 5 شركات عميلة.
- 3 فروع لكل شركة عند الحاجة.
- 40 مستخدمًا بأدوار مختلفة.
- 8 مراكز تكلفة.
- 6 ميزانيات.
- 5 مستودعات.
- 20 موردًا.
- 50 طلبًا بحالات مختلفة.
- 15 RFQ.
- 15 عرض سعر.
- 20 فاتورة.
- 15 دفعة.
- 10 شحنات.
- 10 تذاكر دعم.
- 5 عقود وأسعار خاصة.
- بيانات تقارير كافية لإظهار الرسوم والجداول.

يجب أن تكون البيانات منطقية ومترابطة.

## 12.4 الصور

- استخرج الصور الصالحة من الملف إن كانت متاحة وقابلة للاستخدام.
- نظّمها داخل Object Storage أو مجلد Seed assets.
- لا تحفظ الصور Base64 داخل قاعدة البيانات.
- خزّن Metadata والمسار والرابط في قاعدة البيانات.
- أنشئ Images متعددة المقاسات.
- استخدم WebP عند الملاءمة.
- لا تستخدم Watermarks.
- عند عدم توفر صورة أصلية، استخدم Placeholder احترافي واضح ومؤقت، وسجّل ذلك في `docs/assets-missing.md`.
- لا توقف التنفيذ بسبب غياب صورة تجارية بعينها.

## 12.5 لوحة الإدارة

يجب أن يستطيع Admin:

- إضافة منتج.
- تعديل منتج.
- رفع صور.
- ترتيب الصور.
- حذف/أرشفة.
- تحديث الأسعار.
- إضافة Variants.
- إضافة خصومات كمية.
- تحديد سعر خاص لشركة.
- استيراد Excel.
- تصدير Excel.
- Bulk update.
- إدارة المخزون.
- إدارة SEO/Descriptions.
- إدارة الترجمة العربية والإنجليزية.

---

# 13. التصميم وتنفيذ الواجهات

## 13.1 التطابق البصري

نفّذ الواجهات بنفس:

- الألوان.
- الخطوط.
- الأحجام.
- المسافات.
- Border radius.
- الظلال.
- البطاقات.
- الـApp Bars.
- الـBottom Navigation.
- الـSidebars.
- الجداول.
- الحالات.
- الرسوم.
- الـDialogs.
- الـBottom Sheets.
- الـEmpty States.
- الـError States.

لا تحول التصميم إلى Interpretation عامة.

## 13.2 Design Tokens

استخرج Design Tokens وأنشئها مركزيًا:

- Colors.
- Typography.
- Spacing.
- Radius.
- Shadows.
- Breakpoints.
- Motion.
- Icons.
- Component variants.

لا تكرر قيمًا ثابتة داخل مئات الملفات.

## 13.3 RTL/LTR

- العربية هي اللغة الأولى.
- كل الشاشات يجب أن تعمل RTL.
- الإنجليزية LTR.
- انعكاس الأيقونات الاتجاهية.
- ترتيب Breadcrumbs.
- ترتيب Steppers.
- ترتيب Timelines.
- تنسيق الأرقام والعملات.
- دعم نصوص طويلة.
- عدم وجود Overflow.
- اختبارات Widget/Visual للحالتين.

## 13.4 Responsive

- هاتف مرجعي: 390px.
- Tablet: 768px.
- Desktop admin: 1440px.
- لا تستخدم Scaling أعمى.
- أعد ترتيب المكونات حسب المقاس.
- الهاتف: Bottom navigation وبطاقات صف واحد.
- Tablet: Navigation جانبي مرن وعمودان.
- Desktop: Sidebar ثابت وجداول متعددة الأعمدة.
- الجداول الطويلة تتحول لعرض مناسب على الشاشات الصغيرة.

## 13.5 Accessibility

- Touch target مناسب.
- Keyboard navigation للويب.
- Focus states.
- Semantic labels.
- Screen reader labels.
- Contrast جيد.
- دعم تكبير النص.
- عدم الاعتماد على اللون وحده.
- Alt text للصور المهمة.

---

# 14. الـAPI والسلوك الديناميكي

كل شاشة يجب أن تتصل بـAPI حقيقي.

ممنوع إبقاء Production UI متصلًا ببيانات Mock.

## متطلبات عامة

- REST API موثق بـOpenAPI.
- Pagination.
- Filtering.
- Sorting.
- Searching.
- Field validation.
- ProblemDetails.
- Localization للأخطاء.
- Correlation ID.
- Rate limiting.
- Idempotency.
- Optimistic concurrency.
- File upload.
- Signed URLs.
- Background processing.
- Webhooks.
- Realtime events عند الحاجة.
- API tests.
- Authorization tests.

## Search

وفّر بحثًا عبر:

- Product name.
- SKU.
- Brand.
- Category.
- Company.
- Order number.
- RFQ number.
- Invoice number.
- Supplier.
- Driver.
- Serial number.

## Notifications

- In-app.
- Push.
- Email.
- SMS adapter.
- WhatsApp adapter.
- Notification preferences.
- Retry policy.
- Delivery log.
- Template variables.

نفّذ Adapters حتى يعمل النظام بدون Credentials خارجية، ثم يتم تفعيل الخدمات الحقيقية عبر Environment Variables.

---

# 15. التكاملات الخارجية

أنشئ Interface/Adapter لكل خدمة، ولا تربط Business Logic مباشرة بمزود واحد.

## التكاملات المتوقعة

- Google Sign-In.
- OTP/SMS.
- Email.
- WhatsApp.
- Firebase Push.
- Maps/Geocoding.
- Payment Gateway.
- Object Storage.
- PDF generation.
- Excel import/export.
- Analytics.
- Error monitoring.
- Optional accounting integration.
- Optional ERP integration.

## عند غياب Credentials

لا توقف المشروع.

نفّذ:

- Fake/Local adapter.
- Sandbox adapter.
- Configuration.
- `.env.example`.
- Setup guide.
- Integration tests باستخدام mocks.

اطلب Credentials فقط عندما تصبح المرحلة جاهزة للتوصيل الحقيقي.

---

# 16. الأمن

نفّذ Security by Design.

- JWT access + refresh tokens.
- Rotation and revocation.
- Secure password hashing.
- OTP expiry and attempt limits.
- 2FA للإدارة.
- Role/permission checks.
- Tenant isolation.
- Rate limiting.
- Brute-force protection.
- CSRF protection حسب نمط المصادقة.
- Secure cookies عند استخدامها.
- CORS محدد.
- Input validation.
- File validation.
- MIME/type verification.
- Malware scan hook.
- Signed download URLs.
- Encryption in transit.
- Secrets خارج Git.
- Audit logs.
- Sensitive data masking.
- PII logging prevention.
- SQL injection prevention.
- XSS prevention.
- SSRF prevention.
- Authorization at API, not UI only.
- Dependency scanning.
- Container scanning.
- Security headers.
- Backup encryption.
- Failed login monitoring.
- Security event dashboard.

أنشئ:

- `SECURITY.md`
- `docs/security/threat-model.md`
- `docs/security/permissions-matrix.md`
- `docs/security/data-classification.md`

---

# 17. الاختبارات والجودة

## 17.1 Backend

- Unit tests.
- Integration tests.
- API tests.
- Authorization tests.
- Tenant isolation tests.
- Database migration tests.
- Transaction tests.
- Idempotency tests.
- Payment workflow tests.
- Inventory concurrency tests.
- Approval flow tests.

## 17.2 Flutter

- Unit tests.
- Widget tests.
- Golden tests للمكونات الأساسية.
- Navigation tests.
- API repository tests.
- Offline/cache tests.
- RTL/LTR tests.
- Responsive tests.
- E2E flows.

## 17.3 Admin Web

- Unit tests.
- Component tests.
- Form validation tests.
- Table/filter tests.
- Permission tests.
- RTL/LTR tests.
- Responsive tests.
- Playwright E2E.

## 17.4 رحلات E2E الإلزامية

1. تسجيل شركة والتحقق منها.
2. دخول مستخدم.
3. تصفح وإضافة للسلة وإرسال طلب.
4. موافقة داخلية.
5. RFQ من ملف وتحويله لعرض ثم طلب.
6. منتج مطبوع ورفع شعار واعتماد تصميم.
7. إدارة الطلب من الإدارة حتى الشحن.
8. حركة مخزون مرتبطة بطلب.
9. إنشاء فاتورة واستلام دفعة.
10. تتبع شحنة وإثبات تسليم.
11. مرتجع واسترداد.
12. إنشاء عقد وأسعار خاصة.
13. طلب دوري.
14. تذكرة دعم.
15. صلاحيات تمنع مستخدمًا غير مخول.

## 17.5 معايير الجودة

لا تعتبر المرحلة مكتملة إذا:

- Build fails.
- Tests fail.
- Lint fails.
- Migration fails.
- Seed fails.
- أي شاشة أساسية غير مرتبطة بـAPI.
- توجد Secrets في Git.
- توجد TODOs حرجة.
- توجد Mock data داخل Production path.
- توجد أخطاء RTL واضحة.
- يوجد Broken navigation.
- يوجد Data leakage بين Tenants.

---

# 18. الأداء

## Backend

- P95 API مناسب.
- Pagination إلزامية.
- No N+1.
- Indexes.
- Caching.
- Async I/O.
- Background jobs.
- Query monitoring.
- Slow query logging.
- Connection pooling.
- Compression.
- ETag عند الملاءمة.

## Flutter

- Lazy lists.
- Image caching.
- Pagination.
- Debounced search.
- Avoid unnecessary rebuilds.
- Offline-friendly read cache.
- App startup optimization.

## Admin

- Server pagination.
- Virtualization للجداول الكبيرة.
- Code splitting.
- Lazy routes.
- Optimized charts.
- Avoid oversized client bundles.

نفّذ Load Tests للعمليات الحرجة.

---

# 19. استراتيجية GitHub

المستودع:

`git@github.com:AhmedSaberFarghaly/Mohandseto-Tawredat.git`

الفرع الأساسي:

`main`

## 19.1 قواعد Git

- لا تضع كودًا مكسورًا على `main`.
- أنشئ `develop`.
- استخدم Feature branches.
- Commits صغيرة وواضحة.
- Conventional Commits.
- كل PR يحتوي على Summary وTests وScreenshots عند الحاجة.
- لا تستخدم Force Push على main.
- لا تحفظ Secrets.
- لا ترفع Build artifacts الثقيلة.
- استخدم Git LFS فقط عند ضرورة واضحة.

## 19.2 الرفع بعد كل 10%

الرفع إلى GitHub إلزامي عند كل تقدم 10%.

أنشئ Milestone Release لكل مرحلة:

- 10% → `v0.1.0`
- 20% → `v0.2.0`
- 30% → `v0.3.0`
- 40% → `v0.4.0`
- 50% → `v0.5.0`
- 60% → `v0.6.0`
- 70% → `v0.7.0`
- 80% → `v0.8.0`
- 90% → `v0.9.0`
- 100% → `v1.0.0`

قبل كل Milestone Push:

1. Pull/Rebase latest.
2. Build all apps.
3. Run lint.
4. Run tests.
5. Run migrations.
6. Run seed smoke test.
7. Run security scan.
8. Update coverage matrix.
9. Update changelog.
10. Create milestone report.
11. Commit.
12. Push.
13. Create tag.
14. Ensure CI green.
15. Merge to main بعد نجاح CI.

## 19.3 تقرير كل 10%

أنشئ:

`docs/milestones/MILESTONE_10.md`

ثم 20، 30، وهكذا.

يحتوي على:

- النسبة.
- ما تم إنجازه.
- الشاشات المنفذة.
- APIs المنفذة.
- Entities/Migrations.
- Tests.
- Bugs fixed.
- Known issues.
- Screenshots.
- Build instructions.
- Commit SHA.
- Tag.
- المرحلة التالية.

---

# 20. خطة التنفيذ ذات العشر مراحل

## المرحلة 1 — 0% إلى 10%
### Audit, Architecture, Foundation

- قراءة PDF.
- Screen inventory.
- Feature inventory.
- Repo initialization.
- Monorepo.
- CI/CD foundation.
- Docker.
- Backend skeleton.
- Database skeleton.
- Flutter skeleton.
- Admin skeleton.
- Design tokens.
- Auth architecture.
- Documentation.
- Initial health checks.

**Definition of Done:** جميع المشاريع تبني وتعمل محليًا، وCI أخضر.

## المرحلة 2 — 10% إلى 20%
### Identity, Company Verification, Core Design System

- Auth.
- OTP.
- Email/password.
- Google adapter.
- Company registration.
- Documents.
- Verification.
- Users/roles foundation.
- Onboarding.
- Localization.
- RTL/LTR.
- Core UI components.
- Admin login and dashboard shell.

## المرحلة 3 — 20% إلى 30%
### Catalog, Home, Search, Products

- Categories.
- Products.
- Variants.
- Pricing.
- Quantity tiers.
- Company prices.
- Search.
- Filters.
- Favorites.
- Product details.
- Admin catalog CRUD.
- Product seed data.

## المرحلة 4 — 30% إلى 40%
### Custom Products, Cart, Checkout

- Printed/custom products.
- Logo upload.
- Design brief.
- Mockups.
- Cart.
- Checkout.
- Addresses.
- Cost centers.
- Budget check.
- Purchase orders.
- Tax/shipping calculations.

## المرحلة 5 — 40% إلى 50%
### Approvals and RFQ

- Approval policies.
- Approval workflow.
- RFQ creation.
- File upload/extraction pipeline.
- Item matching.
- Supplier quote requests.
- Customer quote builder.
- Quote versions.
- Negotiation.
- Conversion to order.

## المرحلة 6 — 50% إلى 60%
### Client Orders, Tracking, Returns, Finance, Account

- Orders.
- Status history.
- Recurring orders.
- Tracking.
- Returns.
- Replacement.
- Invoices.
- Payments.
- Statements.
- Budgets.
- Cost centers.
- Company profile.
- Users.
- Notifications.
- Support.
- Settings.

## المرحلة 7 — 60% إلى 70%
### Admin Orders, RFQ, Catalog, Content

- Admin orders.
- Operations.
- RFQ management.
- Pricing.
- Quotes.
- Catalog.
- Categories.
- Content.
- Banners.
- Promotions.
- Bulk operations.
- Imports/exports.

## المرحلة 8 — 70% إلى 80%
### Inventory, Suppliers, CRM, Contracts, Printing

- Warehouses.
- Stock.
- Reservations.
- Transfers.
- Serial numbers.
- Suppliers.
- Purchasing.
- CRM.
- Contracts.
- Special pricing.
- Design queue.
- Production workflow.

## المرحلة 9 — 80% إلى 90%
### Shipping, Accounts, Support, Campaigns, Reports, System

- Shipping.
- Drivers.
- Routes.
- Delivery proof.
- Accounting.
- Customer service.
- Campaigns.
- Permissions.
- Audit.
- Reports.
- Settings.
- Integrations.
- Monitoring.
- Database health.
- Security events.

## المرحلة 10 — 90% إلى 100%
### Hardening, Full QA, Deployment, Release

- Screen coverage 100%.
- Full E2E.
- Security review.
- Performance.
- Accessibility.
- RTL/LTR.
- Responsive.
- Seed validation.
- Data integrity.
- Backup/restore.
- Staging.
- Production deployment documentation.
- App build documentation.
- Admin deployment.
- API deployment.
- Final release.
- `v1.0.0`.

---

# 21. ما يحتاجه التنفيذ من صاحب المشروع

اطلب هذه الأشياء مرة واحدة في تقرير البداية، لكن لا تجعل غيابها يوقف الأساس البرمجي.

## متاح أو مطلوب فورًا

- ملف التصميم النهائي PDF.
- صلاحية قراءة وكتابة المستودع.
- إمكانية تشغيل Git وDocker وFlutter وNode و.NET.
- اسم وهوية المشروع.

## مطلوب عند التكامل الحقيقي

- Firebase project.
- Android package name.
- iOS bundle identifier.
- Apple Developer account.
- Google Play Console.
- Google OAuth credentials.
- SMS provider credentials.
- WhatsApp Business credentials.
- Email SMTP/provider.
- Maps API key.
- Payment gateway sandbox/production credentials.
- Object storage credentials.
- Domain and DNS.
- Hosting/server credentials.
- SSL.
- Monitoring provider.
- Privacy policy and terms approved text.
- Tax/commercial data النهائية للشركة.
- Production product images إن كانت محمية بحقوق أو غير موجودة.

## قاعدة التعامل

إذا لم تتوفر Credentials:

- نفّذ Adapter.
- استخدم Local/Sandbox.
- اكتب Setup guide.
- أكمل باقي النظام.
- لا تكرر الطلب كل يوم.

---

# 22. القرارات التي تتخذها دون الرجوع لصاحب المشروع

اتخذ بنفسك قرارات:

- Folder structure.
- Naming.
- State management.
- Validation library.
- Testing framework.
- Architecture patterns.
- Database indexes.
- API routes.
- DTO structure.
- Cache strategy.
- Error handling.
- Logging.
- CI configuration.
- Branch strategy.
- Code style.
- Refactoring.
- Seed organization.
- Responsive breakpoints.
- Component composition.
- Internal admin UX improvements.
- Bug fixes.
- Dependency upgrades المتوافقة.

لا تطلب موافقة على هذه الأمور.

---

# 23. متى يجب التوقف وطلب تدخل

توقف فقط عند:

1. نقص Credential يمنع اختبار تكامل خارجي بعينه.
2. قرار مالي مثل شراء خدمة.
3. قرار قانوني أو سياسة خصوصية.
4. حذف Production data حقيقية.
5. نشر عام يحتاج موافقة حساب Store.
6. تعارض جوهري بين متطلبين لا يمكن حله منطقيًا.
7. وجود سر أمني أو بيانات حساسة مكشوفة تتطلب تغييرًا من صاحب الحساب.

في كل الحالات الأخرى، اتخذ القرار واستمر.

---

# 24. الوثائق المطلوبة

أنشئ وحدث باستمرار:

- `README.md`
- `docs/product-overview.md`
- `docs/screen-coverage-matrix.csv`
- `docs/feature-inventory.md`
- `docs/assumptions.md`
- `docs/architecture/system-context.md`
- `docs/architecture/container-diagram.md`
- `docs/architecture/modules.md`
- `docs/database/erd.md`
- `docs/database/data-dictionary.md`
- `docs/api/openapi.md`
- `docs/security/threat-model.md`
- `docs/security/permissions-matrix.md`
- `docs/qa/test-strategy.md`
- `docs/qa/e2e-scenarios.md`
- `docs/deployment/local.md`
- `docs/deployment/staging.md`
- `docs/deployment/production.md`
- `docs/integrations/*.md`
- `docs/milestones/*.md`
- `CHANGELOG.md`
- `SECURITY.md`
- `.env.example`

استخدم Mermaid diagrams حيث تفيد.

---

# 25. تعريف الاكتمال النهائي

لا تعتبر المشروع مكتملًا إلا إذا:

- جميع صفحات الـPDF ممثلة في Coverage Matrix.
- عدد الشاشات المفقودة = صفر.
- تطبيق Flutter يعمل Android/iOS ويدعم الهاتف والتابلت.
- Client Web يعمل إن كان ضمن التنفيذ النهائي.
- Admin/CRM يعمل Responsive.
- كل البيانات ديناميكية من قاعدة البيانات.
- المنتجات موجودة في Database.
- لوحة الإدارة تدير المنتجات والبيانات.
- كل الرحلات الأساسية تعمل E2E.
- الصلاحيات تعمل.
- Multi-tenancy آمن.
- الفواتير والمدفوعات والمخزون مترابطة.
- RFQ يعمل.
- المنتجات المطبوعة تعمل.
- الشحن والتتبع يعملان.
- التقارير تعرض بيانات حقيقية.
- Seed data تعمل.
- Migrations تعمل من الصفر.
- Docker Compose يشغل النظام محليًا.
- CI أخضر.
- لا توجد Secrets.
- لا توجد Mock data في Production.
- لا توجد أخطاء حرجة.
- لا توجد Broken routes.
- RTL/LTR سليم.
- الاختبارات الحرجة ناجحة.
- وثائق التشغيل والنشر كاملة.
- تم Push جميع المراحل إلى GitHub.
- تم إنشاء Tag `v1.0.0`.

---

# 26. شكل الرد الأول المطلوب من Claude

استخدم هذا القالب بعد الفحص وقبل كتابة كود المنتج:

```md
# PRE-START AUDIT & EXECUTION ESTIMATE

## 1. Audit Summary
- PDF pages:
- Client screens/states:
- Admin/CRM screens/states:
- Design system/scenario pages:
- Repository status:
- Existing code quality:
- Missing inputs:

## 2. Proposed Architecture
- Client:
- Admin:
- Backend:
- Database:
- Cache:
- Storage:
- Realtime:
- CI/CD:

## 3. Estimated Duration
| Milestone | Scope | Estimated Time |
|---|---|---|
| 10% | ... | ... |
...
| 100% | ... | ... |

- Optimistic total:
- Realistic total:
- Safe total:

## 4. What I Need From You
- Required now:
- Required later:
- Optional:

## 5. Decisions I Will Make Autonomously
...

## 6. Risks
...

## 7. Start Gate
هل أبدأ التنفيذ الكامل؟
```

لا تبدأ مرحلة التنفيذ الكامل قبل إرسال هذا التقرير والحصول على كلمة بدء واضحة.

بعد البداية، لا تنتظر مراجعة كل 10%؛ نفّذ واختبر وارفع تلقائيًا، ثم أرسل تقرير كل Milestone.

---

# 27. الأمر النهائي

ابدأ الآن فقط بمرحلة **الفحص والتقدير**:

1. اقرأ ملف التصميم كاملًا.
2. افحص المستودع.
3. افحص البيئة.
4. أنشئ Audit داخليًا.
5. أرسل تقدير الوقت وما تحتاجه.
6. انتظر أمر البداية مرة واحدة.
7. بعد أمر البداية، قد المشروع بالكامل باستقلالية.
8. نفّذ كل الصفحات بدقة.
9. استخدم قاعدة بيانات حقيقية لكل البيانات.
10. أدخل المنتجات والبيانات التجريبية في قاعدة البيانات.
11. اجعل النظام ديناميكيًا بالكامل.
12. اختبر كل جزء.
13. ارفع إلى GitHub بعد كل 10%.
14. لا تتوقف بسبب قرارات تقنية تستطيع اتخاذها.
15. لا تسلّم مشروعًا ناقصًا أو شكليًا.

**الهدف النهائي: منتج كامل، مترابط، آمن، سريع، قابل للتوسع، مطابق للتصميم، وجاهز للاستخدام والنشر التجاري.**
