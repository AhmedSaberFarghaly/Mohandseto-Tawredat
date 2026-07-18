import fs from "node:fs/promises";
import { Workbook, SpreadsheetFile } from "@oai/artifact-tool";

const outputDir = "E:/Mohandseto Tawrdat/outputs/system_operations_control_20260716";
const previewDir = `${outputDir}/previews`;
await fs.mkdir(previewDir, { recursive: true });

const permissions = {
  "company_owner": ["catalog.view","orders.create","orders.view","orders.cancel","rfq.create","rfq.view","approvals.act","invoices.view","budgets.view","budgets.manage","company.manage","company.users.manage","reports.view"],
  "purchasing_officer": ["catalog.view","orders.create","orders.view","rfq.create","rfq.view"],
  "company_admin": ["catalog.view","orders.view","company.manage","company.users.manage"],
  "finance_manager": ["invoices.view","budgets.view","budgets.manage","approvals.act","reports.view"],
  "warehouse_officer": ["catalog.view","orders.view","inventory.view"],
  "department_manager": ["catalog.view","orders.create","orders.view","rfq.view","approvals.act","budgets.view"],
  "approver": ["orders.view","approvals.act"],
  "billing_officer": ["invoices.view"],
  "requester": ["catalog.view","orders.create","orders.view"],
  "super_admin": ["*"],
  "sales_manager": ["orders.view","orders.manage","rfq.view","rfq.manage","crm.view","crm.manage","reports.view"],
  "sales_agent": ["orders.view","rfq.view","crm.view"],
  "quotes_officer": ["rfq.view","rfq.manage"],
  "products_manager": ["catalog.view","catalog.manage"],
  "inventory_manager": ["inventory.view","inventory.manage"],
  "warehouse_manager": ["inventory.view","inventory.manage","orders.view"],
  "procurement_officer": ["suppliers.manage","inventory.view"],
  "accountant": ["invoices.view","invoices.manage","reports.view"],
  "support_agent": ["support.handle","orders.view","crm.view"],
  "graphic_designer": ["design.manage"],
  "printing_officer": ["design.manage"],
  "delivery_driver": ["shipping.driver"],
  "operations_manager": ["orders.view","orders.manage","inventory.view","shipping.manage","reports.view"],
  "system_admin": ["settings.manage","roles.manage","audit.view"],
  "auditor": ["orders.view","rfq.view","invoices.view","inventory.view","crm.view","reports.view","audit.view"],
};

const roleRows = [
  ["عميل","company_owner","صاحب الشركة","Company Owner","الإدارة العليا","مالك حساب الشركة والمسؤول النهائي عن الحوكمة والميزانيات والموافقات والتقارير","الحساب المؤسسي، الطلبات، الموافقات، المالية، التقارير","نعم","المدير المالي / مدير الإدارة"],
  ["عميل","purchasing_officer","موظف مشتريات","Purchasing Officer","المشتريات","تنفيذ الشراء اليومي وإنشاء الطلبات وطلبات عروض الأسعار","الكتالوج، الطلبات، RFQ","لا","مدير الإدارة / مسؤول الموافقات"],
  ["عميل","company_admin","مسؤول إداري","Company Admin","الإدارة","إدارة بيانات الشركة والفروع والمستخدمين والأدوار","حساب الشركة والمستخدمون","لا","صاحب الشركة"],
  ["عميل","finance_manager","مدير مالي","Finance Manager","المالية","إدارة الميزانيات والموافقات المالية ومراقبة الفواتير والتقارير","الميزانيات، الموافقات، الفواتير، التقارير","نعم","صاحب الشركة / مسؤول الحسابات"],
  ["عميل","warehouse_officer","مسؤول مخازن","Warehouse Officer","المخازن","متابعة جاهزية الاستلام ومطابقة الشحنات مع أوامر الشراء","الطلبات والمخزون","لا","مدير التشغيل / خدمة العملاء"],
  ["عميل","department_manager","مدير إدارة","Department Manager","الإدارات الطالبة","اعتماد احتياج الإدارة ومراجعته مقابل الميزانية والحدود","الطلبات، RFQ، الموافقات، الميزانيات","نعم","المدير المالي / مسؤول الموافقات"],
  ["عميل","approver","مسؤول موافقات","Approver","الحوكمة","اتخاذ قرار الموافقة أو الرفض أو طلب التعديل ضمن مستوى الاعتماد","الموافقات والطلبات","نعم","المستوى التالي / مقدم الطلب"],
  ["عميل","billing_officer","مسؤول فواتير","Billing Officer","المالية","استلام ومراجعة الفواتير وكشوف الحساب داخل الشركة","الفواتير","لا","المدير المالي"],
  ["عميل","requester","مستخدم مخول بالطلب","Requester","الأقسام","إنشاء الاحتياجات وإضافتها للسلة ومتابعة الطلب","الكتالوج والطلبات","لا","موظف المشتريات / المدير المباشر"],
  ["منصة","super_admin","مدير النظام الأعلى","Super Admin","الإدارة التقنية","السلطة النهائية لكل الوحدات والإجراءات الحساسة واستمرارية التشغيل","كل الوحدات","نعم","لا يوجد / مالك المنصة"],
  ["منصة","sales_manager","مدير مبيعات","Sales Manager","المبيعات","إدارة مسار الشركات والفرص وعروض الأسعار والعقود وأداء الفريق","CRM، RFQ، الطلبات، العقود، التقارير","نعم","التشغيل / المالية"],
  ["منصة","sales_agent","موظف مبيعات","Sales Agent","المبيعات","متابعة الشركات والأنشطة والفرص وجمع احتياج العميل","CRM، الطلبات، RFQ","لا","مدير المبيعات / مسؤول العروض"],
  ["منصة","quotes_officer","مسؤول عروض أسعار","Quotes Officer","التسعير","تحليل RFQ وبناء نسخ عروض السعر والتفاوض والتحويل إلى طلب","RFQ وعروض الأسعار","لا","مدير المبيعات / العميل"],
  ["منصة","products_manager","مسؤول منتجات","Products Manager","المنتجات والمحتوى","إدارة الكتالوج والتسعير التجاري والمحتوى والربط والاستيراد","الكتالوج والمحتوى","لا","المبيعات / المخزون"],
  ["منصة","inventory_manager","مسؤول مخزون","Inventory Manager","المخزون","ضبط الأرصدة والحركات والجرد والتحويلات وحدود إعادة الطلب","المخزون","نعم تشغيليًا","مدير التشغيل / المشتريات"],
  ["منصة","warehouse_manager","مسؤول مستودع","Warehouse Manager","المستودعات","التقاط وتجهيز الطلبات والاستلام والفحص وإدارة المواقع","المخزون والطلبات","نعم تشغيليًا","الشحن / المخزون"],
  ["منصة","procurement_officer","مسؤول مشتريات","Procurement Officer","مشتريات المنصة","إدارة الموردين وأوامر الشراء والاستلام والفواتير الموردية","الموردون والمخزون","نعم تشغيليًا","المستودع / الحسابات"],
  ["منصة","accountant","مسؤول حسابات","Accountant","الحسابات","إصدار ومطابقة الفواتير والمدفوعات والقيود والإقفالات والاستردادات","المالية والتقارير","نعم ماليًا","المدير المالي / المراجع"],
  ["منصة","support_agent","خدمة عملاء","Support Agent","خدمة العملاء","إدارة التذاكر والتواصل وSLA والتصعيد ومشكلات الطلبات","الدعم، الطلبات، CRM","نعم خدميًا","التشغيل / الحسابات"],
  ["منصة","graphic_designer","مصمم جرافيك","Graphic Designer","التصميم","مراجعة ملفات الهوية وبناء نسخ التصميم وإدارة ملاحظات العميل","التصميم والطباعة","لا","مسؤول الطباعة / العميل"],
  ["منصة","printing_officer","مسؤول طباعة","Printing Officer","الإنتاج","إدارة العينات ومراحل الإنتاج والجودة والتجهيز للشحن","الطباعة والإنتاج","نعم تشغيليًا","الشحن"],
  ["منصة","delivery_driver","مندوب توصيل","Delivery Driver","التوصيل","تنفيذ المسار ومحاولات التسليم ورفع إثبات الاستلام والموقع","الشحن والتوصيل","لا","مدير التشغيل / خدمة العملاء"],
  ["منصة","operations_manager","مدير تشغيل","Operations Manager","التشغيل","إدارة دورة تنفيذ الطلب من التأكيد حتى التسليم والاستثناءات","الطلبات، المخزون، الشحن، التقارير","نعم تشغيليًا","المبيعات / الحسابات"],
  ["منصة","system_admin","مدير نظام","System Admin","تقنية المعلومات","إدارة الإعدادات والأدوار والمراقبة والنسخ والتكاملات دون صلاحيات تجارية","النظام والتدقيق","نعم تقنيًا","مدير النظام الأعلى"],
  ["منصة","auditor","مراجع (قراءة فقط)","Auditor / Read Only","التدقيق","مراجعة الأدلة والسجلات والتقارير دون تعديل البيانات التشغيلية","التدقيق والتقارير وكل وحدات القراءة","لا","الإدارة العليا / مسؤول النظام"],
].map(r => [...r, permissions[r[1]].join(" • ")]);

const roleName = Object.fromEntries(roleRows.map(r => [r[1], r[2]]));
const roleDept = Object.fromEntries(roleRows.map(r => [r[1], r[4]]));
const permFor = (role, preferred) => preferred || (permissions[role]?.[0] ?? "—");
const st = (name, role, action, from, to, nextRole, sla, priority="متوسطة", permission="", evidence="سجل تدقيق + سجل حالة", exception="التصعيد للمسؤول التالي مع توثيق السبب") => ({name,role,action,from,to,nextRole,sla,priority,permission:permFor(role,permission),evidence,exception});

const cycleDefs = [
  {id:"C01",name:"تسجيل الشركة والتحقق والتفعيل",owner:"sales_manager",entry:"شركة جديدة تريد فتح حساب مؤسسي",exit:"شركة Active ومستخدم مالك مفعل أو رفض موثق",kpi:"زمن التفعيل، نسبة المستندات المقبولة من أول مرة",steps:[
    st("تقديم بيانات الشركة","company_owner","إدخال البيانات القانونية والإدارية وبيانات مسؤول الحساب","غير مسجلة","PendingVerification","company_admin","30 دقيقة","عالية","company.manage","نموذج التسجيل + وقت الإرسال"),
    st("رفع المستندات","company_owner","رفع السجل التجاري والبطاقة الضريبية وخطاب التفويض","PendingVerification","PendingVerification","sales_agent","يوم عمل","عالية","company.manage","نسخ الملفات وبصمتها"),
    st("مراجعة أولية","sales_agent","فحص اكتمال البيانات والمستندات وتسجيل الملاحظات","PendingVerification","UnderReview","sales_manager","4 ساعات","عالية","crm.manage"),
    st("قرار التحقق","sales_manager","اعتماد الشركة أو رفضها بسبب واضح","UnderReview","Active / Rejected","company_owner","يوم عمل","عاجلة","companies.verify","قرار المراجعة + السبب"),
    st("تهيئة الحساب المؤسسي","company_admin","ضبط الفروع والفوترة والحد الائتماني ومراكز التكلفة","Active","Active","company_owner","يوم عمل","متوسطة","company.manage"),
    st("تعليق أو إعادة تفعيل","sales_manager","تعليق الشركة عند المخاطر وإعادتها بعد المعالجة","Active / Suspended","Suspended / Active","company_owner","حسب الحالة","عالية","crm.manage")]},
  {id:"C02",name:"المستخدمون والأدوار والدعوات والوصول",owner:"company_admin",entry:"حاجة لإضافة أو تعديل مستخدم داخل الشركة",exit:"مستخدم نشط بدور ونطاق صحيح أو جلساته ملغاة",kpi:"نسبة الدعوات المقبولة، زمن إلغاء الوصول",steps:[
    st("تعريف الدور","company_owner","اختيار دور نظامي أو اعتماد دور مخصص وصلاحياته","لا يوجد","Role Ready","company_admin","4 ساعات","عالية","company.users.manage"),
    st("إرسال الدعوة","company_admin","إدخال بيانات الموظف والدور والفرع وإرسال دعوة 7 أيام","لا يوجد","Pending","requester","15 دقيقة","متوسطة","company.users.manage"),
    st("قبول الدعوة","requester","تعيين كلمة المرور وتأكيد البيانات وإنشاء الحساب","Pending","Accepted","company_admin","7 أيام","متوسطة","orders.create"),
    st("تعديل أو نقل المستخدم","company_admin","تغيير الدور أو الفرع أو حد الشراء مع تسجيل الأثر","Accepted","Active Updated","company_owner","4 ساعات","عالية","company.users.manage"),
    st("تعطيل الوصول","company_admin","تعطيل المستخدم وإلغاء جميع جلساته النشطة فورًا","Active","Disabled","company_owner","15 دقيقة","عاجلة","company.users.manage"),
    st("مراجعة دورية للصلاحيات","company_owner","مراجعة المستخدمين والأدوار وحدود الشراء والنطاقات","Active","Reviewed","auditor","شهريًا","عالية","company.users.manage") ]},
  {id:"C03",name:"الشراء المباشر من الكتالوج",owner:"purchasing_officer",entry:"احتياج متاح كمنتج فعّال وله سعر ومخزون",exit:"Checkout Submitted وطلب أنشئ للموافقة أو التأكيد",kpi:"زمن إنشاء الطلب، معدل التخلي عن السلة، دقة السعر",steps:[
    st("تحديد الاحتياج","requester","البحث والمقارنة واختيار المنتج والكمية والفرع","لا يوجد","Cart Active","purchasing_officer","يوم عمل","متوسطة","catalog.view"),
    st("مراجعة السلة","purchasing_officer","تأكيد الحد الأدنى والبدائل والأسعار والتوفر ومركز التكلفة","Cart Active","Checkout Draft","department_manager","2 ساعة","عالية","orders.create"),
    st("تحديد الشحن والدفع","purchasing_officer","اختيار العنوان وطريقة الشحن والدفع وإرفاق أمر الشراء عند اللزوم","Checkout Draft","Checkout Ready","finance_manager","2 ساعة","عالية","orders.create"),
    st("فحص الميزانية والحدود","finance_manager","مراجعة الميزانية والائتمان وشروط الدفع","Checkout Ready","Ready / Approval Required","approver","4 ساعات","عالية","budgets.view"),
    st("إرسال الطلب","purchasing_officer","تأكيد بيانات الطلب وتحويل السلة إلى طلب","Checkout Ready","Submitted","operations_manager","30 دقيقة","عاجلة","orders.create"),
    st("إشعار الأطراف","operations_manager","إرسال رقم الطلب وحالته ومسار الموافقة المتوقع","Submitted","PendingApproval / Confirmed","requester","فوري","متوسطة","orders.manage") ]},
  {id:"C04",name:"الموافقات والميزانيات ومراكز التكلفة",owner:"company_owner",entry:"طلب تجاوز حد المستخدم أو يخضع لسياسة موافقات",exit:"قرار نهائي وميزانية محجوزة أو طلب مرفوض/ملغي",kpi:"زمن الموافقة، نسبة التجاوزات، عدد التصعيدات",steps:[
    st("إنشاء سلسلة الاعتماد","company_owner","تعريف المستويات والمسؤولين والحدود وSLA","لا توجد سياسة","Policy Active","approver","مرة/عند التغيير","عالية","approvals.manage"),
    st("تقديم طلب اعتماد","requester","إرسال الطلب مع المبررات والمرفقات ومركز التكلفة","Draft","Pending","department_manager","30 دقيقة","عالية","orders.create"),
    st("اعتماد إداري","department_manager","فحص حاجة الإدارة والكمية والميزانية الأولية","Current","Approved / ChangesRequested / Rejected","finance_manager","8 ساعات","عاجلة","approvals.act"),
    st("اعتماد مالي","finance_manager","فحص الرصيد والحد الائتماني والتعارضات","Current","Approved / ChangesRequested / Rejected","approver","8 ساعات","عاجلة","approvals.act"),
    st("اعتماد نهائي","approver","اتخاذ القرار ضمن حد السلطة أو تفويض المستوى التالي","Current","Approved / Rejected / Delegated","operations_manager","8 ساعات","عاجلة","approvals.act"),
    st("تصعيد التأخير","company_owner","متابعة الطلبات المتجاوزة لـSLA وإعادة التعيين عند الحاجة","Pending Overdue","Escalated","approver","فوري بعد SLA","عاجلة","approvals.act"),
    st("حجز الميزانية","finance_manager","حجز مبلغ مركز التكلفة وتسجيل الاستخدام عند تأكيد الطلب","Approved","Budget Reserved","operations_manager","15 دقيقة","عالية","budgets.manage") ]},
  {id:"C05",name:"طلب عرض السعر والتسعير والتفاوض",owner:"quotes_officer",entry:"احتياج غير قياسي أو ملف Excel/PDF/صورة",exit:"عرض مقبول محول لطلب أو مرفوض/منتهي",kpi:"زمن أول عرض، هامش الربح، معدل التحويل",steps:[
    st("إنشاء RFQ","purchasing_officer","إدخال البنود الحرة أو رفع ملف وتحديد الموعد والمواصفات","لا يوجد","Draft","quotes_officer","ساعة","عالية","rfq.create"),
    st("استخراج ومراجعة البنود","quotes_officer","استخراج البنود وربطها بالكتالوج وتصحيح الكميات","Draft / Extracting","NeedsReview / Submitted","procurement_officer","4 ساعات","عالية","rfq.manage"),
    st("جمع أسعار الموردين","procurement_officer","إرسال طلبات سعر ومقارنة التكلفة والمدة والحد الأدنى","Submitted","UnderReview","quotes_officer","يوم عمل","عالية","suppliers.manage"),
    st("بناء نسخة العرض","quotes_officer","تسعير البيع والهامش والخصم والضريبة والشحن والبدائل","UnderReview","Quoted","sales_manager","4 ساعات","عالية","rfq.manage"),
    st("اعتماد وإرسال العرض","sales_manager","مراجعة الربحية والشروط ثم إرسال النسخة للعميل","Quoted","Sent / Negotiating","purchasing_officer","4 ساعات","عاجلة","rfq.manage"),
    st("التفاوض والمراجعات","quotes_officer","تسجيل الرسائل والعروض المقابلة وإنشاء نسخ غير قابلة للتعديل","Negotiating","RevisionRequested / Quoted","sales_manager","يوم عمل","عالية","rfq.manage"),
    st("قبول العرض","purchasing_officer","اختيار النسخة المقبولة وإثبات الموافقة","Quoted / Negotiating","Accepted","quotes_officer","حتى تاريخ الانتهاء","عاجلة","rfq.view"),
    st("التحويل إلى طلب","quotes_officer","تحويل النسخة المقبولة إلى طلب مؤسسي بنفس البنود والسعر","Accepted","Converted","operations_manager","30 دقيقة","عاجلة","rfq.manage") ]},
  {id:"C06",name:"تشغيل الطلب والتجهيز",owner:"operations_manager",entry:"طلب Confirmed ومدفوع/ائتماني ومستعد للتنفيذ",exit:"شحنة جاهزة أو طلب مكتمل/ملغي مع سجل تاريخ",kpi:"زمن التجهيز، دقة الالتقاط، نسبة الطلبات المتأخرة",steps:[
    st("مراجعة الطلب","operations_manager","فحص البنود والعنوان والمخزون والدفع وتعيين مسؤول التنفيذ","Confirmed","Processing","warehouse_manager","1 ساعة","عاجلة","orders.manage"),
    st("حجز المخزون","inventory_manager","حجز الكميات ذريًا حسب المستودع ومنع السالب","Processing","Reserved","warehouse_manager","15 دقيقة","عاجلة","inventory.manage"),
    st("الالتقاط","warehouse_manager","طباعة قائمة الالتقاط ومسح الباركود وتجميع البنود","Processing","Picking","warehouse_officer","4 ساعات","عالية","inventory.manage"),
    st("التحقق الداخلي","warehouse_officer","مطابقة SKU والكمية والحالة والإبلاغ عن النقص","Picking","Verified / Issue","warehouse_manager","2 ساعة","عالية","inventory.view"),
    st("التعبئة","warehouse_manager","تحديد الطرود والأوزان وطباعة قائمة التعبئة","Verified","Packing","operations_manager","2 ساعة","عالية","inventory.manage"),
    st("معالجة الاستثناء","operations_manager","استبدال بند أو تقسيم شحنة أو تأخير موثق بعد إخطار العميل","Issue","Resolved / Delayed","support_agent","4 ساعات","عاجلة","orders.manage"),
    st("تسليم للشحن","operations_manager","إنشاء تخصيص الشحنة بالكميات المتبقية وتحديث الطلب","Packing","Shipped / PartiallyShipped","delivery_driver","1 ساعة","عاجلة","shipping.manage") ]},
  {id:"C07",name:"إدارة المخزون والجرد والتحويل",owner:"inventory_manager",entry:"حركة مخزون أو حاجة جرد أو انخفاض عن حد الطلب",exit:"رصيد متزن وحركة غير قابلة للتعديل وأي فرق مبرر",kpi:"دقة المخزون، قيمة الفروقات، نفاد المخزون",steps:[
    st("مراقبة الرصيد","inventory_manager","متابعة المتاح والمحجوز والمنخفض والمنعدم وقيمة المخزون","رصيد حالي","Monitored","procurement_officer","يوميًا","عالية","inventory.view"),
    st("إنشاء حركة","inventory_manager","إضافة/خصم/حجز/تحرير مع سبب ومرجع","Monitored","Movement Posted","auditor","فوري","عالية","inventory.manage"),
    st("تحويل مستودعي","warehouse_manager","خصم TransferOut وإضافة TransferIn ذريًا مع استلام الوجهة","Available","Transferred","inventory_manager","نفس اليوم","عالية","inventory.manage"),
    st("فتح جلسة جرد","inventory_manager","تحديد المستودع والنطاق وتحويل الحالة إلى InProgress","لا يوجد","Draft / InProgress","warehouse_manager","حسب الخطة","متوسطة","inventory.manage"),
    st("عد فعلي","warehouse_manager","تسجيل الكميات الفعلية والأرقام المسلسلة والدفعات","InProgress","Counted","inventory_manager","يوم عمل","عالية","inventory.manage"),
    st("تسوية الفروق","inventory_manager","اعتماد Reconcile مرة واحدة وتوثيق سبب كل فرق","Counted","Reconciled","auditor","4 ساعات","عاجلة","inventory.manage"),
    st("إطلاق إعادة الطلب","inventory_manager","إشعار المشتريات بالمواد تحت حد إعادة الطلب","LowStock / OutOfStock","Reorder Requested","procurement_officer","ساعة","عاجلة","inventory.view") ]},
  {id:"C08",name:"الموردون وأوامر الشراء والاستلام",owner:"procurement_officer",entry:"نقص مخزون أو طلب مورد معتمد",exit:"بضاعة مقبولة بالمخزون وفاتورة مورد مطابقة أو مرتجع",kpi:"التسليم في الموعد، قبول الجودة، فرق المطابقة",steps:[
    st("اختيار المورد","procurement_officer","مقارنة السعر والتقييم وMOQ والمهلة والعقود","Reorder Requested","Supplier Selected","operations_manager","4 ساعات","عالية","suppliers.manage"),
    st("إنشاء أمر شراء","procurement_officer","إدخال البنود والأسعار والضريبة والمستودع والموعد","Supplier Selected","PO Draft","operations_manager","2 ساعة","عالية","suppliers.manage"),
    st("إرسال وتأكيد الأمر","procurement_officer","إصدار المستند وإرساله وتسجيل تأكيد المورد","PO Draft","Sent / Confirmed","warehouse_manager","يوم عمل","عاجلة","suppliers.manage"),
    st("استلام أولي","warehouse_manager","إنشاء إذن استلام وربطه بأمر الشراء والدفعة","Confirmed","GoodsReceipt Inspection","inventory_manager","2 ساعة","عالية","inventory.manage"),
    st("فحص وقبول","inventory_manager","قبول كامل/جزئي أو رفض تالف وتسجيل الدفعات والصلاحية","Inspection","Accepted / PartiallyAccepted / Rejected","procurement_officer","4 ساعات","عاجلة","inventory.manage"),
    st("تحديث أمر الشراء","procurement_officer","تحديث PartiallyReceived أو Received ومتابعة المتبقي","Confirmed","PartiallyReceived / Received","accountant","ساعة","عالية","suppliers.manage"),
    st("مطابقة ثلاثية","accountant","مطابقة أمر الشراء والاستلام وفاتورة المورد وتسجيل الفرق","Invoice Draft","Matched / Variance / Approved","procurement_officer","يوم عمل","عاجلة","invoices.manage"),
    st("مرتجع مورد","procurement_officer","إنشاء المرتجع وخصم المخزون ذريًا ومتابعة القبول والاسترداد","Rejected / Defective","Return Sent","accountant","يوم عمل","عالية","suppliers.manage") ]},
  {id:"C09",name:"الفوترة والتحصيل والإقفال المالي",owner:"accountant",entry:"طلب قابل للفوترة أو دفعة واردة أو موعد إقفال",exit:"فاتورة مدفوعة/مقفلة وقيود مرحّلة بلا فروق معلقة",kpi:"DSO، المتأخرات، نسبة المطابقة، زمن الإقفال",steps:[
    st("إصدار الفاتورة","accountant","إنشاء فاتورة قياسية/ضريبية ومنع التكرار","Order Billable","Issued","billing_officer","2 ساعة","عاجلة","invoices.manage"),
    st("مراجعة العميل","billing_officer","فحص الاسم الضريبي والبنود والمبلغ وشروط الدفع","Issued","Reviewed / Disputed","finance_manager","يوم عمل","عالية","invoices.view"),
    st("تسجيل التحويل","accountant","ربط إيصال البنك بالفاتورة وتسجيل محاولة الدفع","Issued / PartiallyPaid","PendingVerification","finance_manager","4 ساعات","عالية","invoices.manage"),
    st("تحقق الدفع","finance_manager","قبول/رفض المطابقة ومنع الزيادة عن الرصيد","PendingVerification","Completed / Rejected","accountant","يوم عمل","عاجلة","invoices.manage"),
    st("تحديث التحصيل","accountant","تحديث PartiallyPaid أو Paid وإصدار كشف الحساب","Completed","PartiallyPaid / Paid","company_owner","15 دقيقة","عاجلة","invoices.manage"),
    st("متابعة المتأخرات","accountant","تصنيف الأعمار وإرسال التنبيه وتصعيد Overdue","Issued Past Due","Overdue","sales_manager","يوميًا","عالية","invoices.manage"),
    st("ترحيل القيود","accountant","ترحيل المصروفات والإشعارات الدائنة/المدينة والاستردادات","Draft","Posted","auditor","يوميًا","عالية","invoices.manage"),
    st("إقفال الفترة","accountant","منع الإقفال مع قيود مسودة أو تحويلات غير مطابقة ثم غلق الفترة","Open","Closed","auditor","شهريًا","عاجلة","invoices.manage") ]},
  {id:"C10",name:"CRM ودورة الشركة والفرصة",owner:"sales_manager",entry:"شركة Prospect/Lead أو حساب قائم يحتاج متابعة",exit:"شركة Active بفرصة/عقد أو AtRisk/Dormant/Lost مع سبب",kpi:"زمن الانتقال بين المراحل، معدل التحويل، الأنشطة المتأخرة",steps:[
    st("تسجيل الشركة/الفرصة","sales_agent","إدخال القطاع والحجم والمصدر والتصنيف والاحتياج","Prospect","Lead","sales_manager","ساعة","متوسطة","crm.view"),
    st("التأهيل","sales_agent","تنفيذ مكالمة/اجتماع وتقييم الملاءمة والميزانية والموعد","Lead","Qualified / Lost","sales_manager","يومان","عالية","crm.view"),
    st("تعيين المسؤول والمهام","sales_manager","تعيين مندوب وإنشاء مهام بمواعيد وأولوية","Qualified","Proposal","sales_agent","4 ساعات","عالية","crm.manage"),
    st("إنشاء العرض التجاري","sales_manager","ربط RFQ أو أسعار خاصة وعرض المنتجات المقترحة","Proposal","Negotiation","quotes_officer","يوم عمل","عالية","crm.manage"),
    st("إغلاق الفرصة","sales_manager","تسجيل الفوز وتفعيل الحساب أو الفقد وسببه","Negotiation","Active / Lost","company_owner","يوم عمل","عاجلة","crm.manage"),
    st("مراقبة صحة العميل","sales_agent","متابعة الشراء والدعم والائتمان وتحديث AtRisk/Dormant","Active","AtRisk / Dormant / Active","sales_manager","أسبوعيًا","عالية","crm.view"),
    st("إعادة التنشيط","sales_manager","خطة متابعة وعرض مناسب لإعادة الحساب إلى Active","AtRisk / Dormant","Active / Lost","sales_agent","5 أيام","عالية","crm.manage") ]},
  {id:"C11",name:"العقود والأسعار الخاصة والتجديد",owner:"sales_manager",entry:"شركة مؤهلة تحتاج أسعارًا/طباعة/توريدًا تعاقديًا",exit:"عقد Active بأسعار منشورة أو Expired/Rejected موثق",kpi:"قيمة العقود، هامش الربح، التجديد، استهلاك العقد",steps:[
    st("إعداد مسودة العقد","sales_manager","تحديد النوع والفترة والمنتجات والتسعير والشرائح والائتمان","لا يوجد","Draft","finance_manager","يوم عمل","عالية","crm.manage"),
    st("إرفاق المستندات","sales_manager","رفع المسودة والملحقات والنسخة الموقعة ضمن حد 20MB","Draft","PendingApproval","finance_manager","يوم عمل","عالية","crm.manage"),
    st("اعتماد المبيعات","sales_manager","مراجعة القيمة والعميل والهامش وتسجيل القرار","PendingApproval","Sales Approved / Rejected","finance_manager","8 ساعات","عاجلة","crm.manage"),
    st("اعتماد المالية","finance_manager","مراجعة الائتمان وشروط الدفع والسعر وتسجيل القرار","Sales Approved","Approved / Rejected","sales_manager","8 ساعات","عاجلة","approvals.act"),
    st("تفعيل العقد","sales_manager","التحقق من النسخة الموقعة ثم نشر الأسعار ورفع الائتمان وإخطار العميل","Approved","Active","company_owner","ساعة","عاجلة","crm.manage"),
    st("تعديل سعر مجدول","sales_manager","إنشاء مراجعة سعر بموافقة العميل وتاريخ سريان","Active","PendingCustomerApproval / Scheduled","company_owner","5 أيام","عالية","crm.manage"),
    st("تجديد العقد","company_owner","تقديم طلب التجديد والتفاوض على المدة والسعر والائتمان","Expiring","Submitted","sales_manager","30 يومًا قبل الانتهاء","عالية","company.manage"),
    st("اعتماد التجديد","sales_manager","اعتماد أو رفض وإنشاء سجل تجديد دائم","Submitted / UnderReview","Approved / Rejected","company_owner","5 أيام","عاجلة","crm.manage") ]},
  {id:"C12",name:"التصميم والطباعة والإنتاج",owner:"printing_officer",entry:"طلب منتج مخصص ومواصفات وشعار وكمية",exit:"منتج Ready للشحن أو Rejected/Cancelled موثق",kpi:"قبول التصميم من أول مرة، زمن الإنتاج، نسبة نجاح الجودة",steps:[
    st("إنشاء طلب مخصص","purchasing_officer","اختيار القالب والخامة والمقاس واللون والطباعة والكمية","لا يوجد","Draft / AwaitingQuote","quotes_officer","ساعة","عالية","orders.create"),
    st("تسعير الطلب","quotes_officer","حساب رسوم التجهيز والوحدة والمهلة وإرسال العرض","AwaitingQuote","Quoted","purchasing_officer","4 ساعات","عالية","rfq.manage"),
    st("رفع ومراجعة الشعار","graphic_designer","فحص Vector والشفافية وCMYK والدقة والمؤثرات","Quoted","DesignInProgress","graphic_designer","4 ساعات","عالية","design.manage"),
    st("إنتاج نسخة التصميم","graphic_designer","رفع Mockup بإصدار غير قابل للتعديل وإرساله للعميل","DesignInProgress","AwaitingDesignApproval","purchasing_officer","يوم عمل","عالية","design.manage"),
    st("قرار التصميم","purchasing_officer","قبول أو طلب تعديل أو رفض مع ملاحظات واضحة","AwaitingDesignApproval","DesignApproved / RevisionRequested / Rejected","graphic_designer","يومان","عاجلة","orders.view"),
    st("اعتماد العينة","printing_officer","إنشاء عينة وإرسالها وتسجيل قرار العميل قبل الإنتاج","DesignApproved","AwaitingSampleApproval","purchasing_officer","يومان","عالية","design.manage"),
    st("تشغيل الإنتاج","printing_officer","تنفيذ الخامات والعينة والطباعة والتشطيب والتعبئة بالتتابع","AwaitingSampleApproval","InProduction","operations_manager","حسب المهلة","عاجلة","design.manage"),
    st("بوابة الجودة","printing_officer","تسجيل فحوص الجودة والكمية ومنع الجاهزية عند الفشل","InProduction","QualityCheck / Blocked","operations_manager","4 ساعات","عاجلة","design.manage"),
    st("جاهز للشحن","printing_officer","اعتماد التعبئة والكمية وإخطار العميل والشحن","QualityCheck","Ready","operations_manager","ساعة","عاجلة","design.manage") ]},
  {id:"C13",name:"الشحن والمسارات والتسليم",owner:"operations_manager",entry:"طرود معبأة وكميات مخصصة وعنوان صالح",exit:"Delivered بإثبات أو Failed/Rescheduled مع سبب",kpi:"OTD، نجاح أول محاولة، تكلفة الشحنة، محاولات التسليم",steps:[
    st("إنشاء الشحنة","operations_manager","تحديد البنود والوزن والمنطقة والتكلفة والموعد والإحداثيات","Packing","Ready","delivery_driver","ساعة","عاجلة","shipping.manage"),
    st("تعيين المندوب","operations_manager","اختيار مندوب نشط حسب الحمل والأداء وتأكيد المهمة","Ready","Assigned","delivery_driver","2 ساعة","عاجلة","shipping.manage"),
    st("تحسين المسار","operations_manager","تجميع المحطات وحساب ترتيب أقرب نقطة والمدة","Assigned","Route Optimized","delivery_driver","30 دقيقة","عالية","shipping.manage"),
    st("بدء الرحلة","delivery_driver","بدء المسار وتحديث الموقع ووقت الانطلاق","Assigned","InProgress / OutForDelivery","operations_manager","في الموعد","عاجلة","shipping.driver"),
    st("التواصل والمحاولة","delivery_driver","تسجيل قناة الاتصال والمحاولة والموقع والنتيجة","OutForDelivery","Delivered / Failed","support_agent","فوري","عاجلة","shipping.driver"),
    st("إثبات الاستلام","delivery_driver","رفع صورة/توقيع/OTP/مستند واسم المستلم وGPS","OutForDelivery","Delivered","operations_manager","عند التسليم","عاجلة","shipping.driver"),
    st("معالجة الفشل","operations_manager","تسجيل السبب وتحديث Delayed وجدولة موعد مستقبلي أو الإلغاء","Failed","Rescheduled / Cancelled","support_agent","ساعة","عاجلة","shipping.manage"),
    st("إغلاق الطلب","operations_manager","مزامنة التسليم الجزئي/الكامل وتحديث الطلب وإخطار العميل","Delivered","PartiallyDelivered / Delivered / Completed","company_owner","15 دقيقة","عاجلة","orders.manage") ]},
  {id:"C14",name:"المرتجعات والاستبدال والاسترداد",owner:"support_agent",entry:"طلب Delivered وبند مؤهل للإرجاع مع سبب/صور",exit:"RefundCompleted أو ReplacementDelivered والتصرف بالمخزون",kpi:"زمن القرار، زمن الاسترداد، نسبة إعادة التخزين",steps:[
    st("إنشاء المرتجع","requester","اختيار البنود والكميات والسبب والحل ورفع الصور","Delivered","Draft / Submitted","support_agent","حسب السياسة","عالية","orders.view"),
    st("المراجعة والقرار","support_agent","فحص الأهلية والصور والطلب وقبول/رفض المبلغ والحل","Submitted","UnderReview / Approved / Rejected","operations_manager","يوم عمل","عاجلة","support.handle"),
    st("جدولة الاستلام","operations_manager","تحديد العنوان والموعد والنافذة وإنشاء مهمة نقل","Approved","PickupScheduled","delivery_driver","يوم عمل","عالية","shipping.manage"),
    st("استلام المرتجع","delivery_driver","استلام القطع وتسجيل الإثبات وتحويلها للمستودع","PickupScheduled","InTransit / Received","warehouse_manager","في الموعد","عاجلة","shipping.driver"),
    st("فحص المرتجع","warehouse_manager","فحص الكمية والحالة وتحديد Restock/Dispose/Replace/CreditNote","Received","Inspecting","support_agent","4 ساعات","عاجلة","inventory.manage"),
    st("اعتماد الحل","support_agent","اعتماد الاسترداد أو بدء تجهيز الاستبدال بناءً على الفحص","Inspecting","RefundApproved / ReplacementPreparing","accountant","4 ساعات","عاجلة","support.handle"),
    st("تنفيذ الاسترداد","accountant","إنشاء معاملة الاسترداد/الإشعار الدائن ومنع التكرار","RefundApproved","RefundCompleted","requester","3 أيام عمل","عاجلة","invoices.manage"),
    st("تنفيذ الاستبدال","operations_manager","حجز وتجهيز وشحن البديل ثم إثبات التسليم","ReplacementPreparing","ReplacementShipped / ReplacementDelivered","requester","حسب التوفر","عاجلة","orders.manage"),
    st("التصرف بالمخزون","warehouse_manager","إعادة تخزين المقبول بحركة ثابتة أو إتلاف موثق","Inspecting","Disposition Posted","auditor","ساعة","عالية","inventory.manage"),
    st("إغلاق المرتجع","support_agent","التحقق من اكتمال الحل وإشعار العميل وإغلاق السجل","RefundCompleted / ReplacementDelivered","Completed","requester","ساعة","متوسطة","support.handle") ]},
  {id:"C15",name:"خدمة العملاء والتذاكر وSLA",owner:"support_agent",entry:"سؤال/شكوى/مشكلة طلب أو دفع أو حساب",exit:"Resolved ثم Closed بتأكيد العميل أو سياسة الإغلاق",kpi:"أول رد، زمن الحل، الالتزام بـSLA، تقييم العميل",steps:[
    st("فتح التذكرة","requester","اختيار النوع والأولوية وكتابة الوصف وإرفاق الملفات","لا يوجد","Open","support_agent","فوري","عالية","orders.view"),
    st("التصنيف والتعيين","support_agent","مراجعة النوع والأولوية والـSLA وتعيين المسؤول","Open","InProgress","support_agent","15 دقيقة","عاجلة","support.handle"),
    st("التحقيق والتواصل","support_agent","مراجعة الطلب وCRM وإرسال رد قالب أو مخصص وتوثيق المحادثة","InProgress","InProgress / WaitingCustomer","requester","حسب SLA","عاجلة","support.handle"),
    st("تصعيد وظيفي","support_agent","تحويل المشكلة للتشغيل/الحسابات/المبيعات مع ملخص وأدلة","InProgress Overdue","Escalated","operations_manager","قبل خرق SLA","عاجلة","support.handle"),
    st("تنفيذ الحل","operations_manager","معالجة الاستثناء التشغيلي وإرجاع نتيجة قابلة للتحقق","Escalated","Resolved","support_agent","حسب SLA","عاجلة","orders.manage"),
    st("إغلاق وتقييم","support_agent","تأكيد الحل وإغلاق التذكرة وطلب تقييم الخدمة","Resolved","Closed","requester","24 ساعة","متوسطة","support.handle"),
    st("تحليل الأسباب","support_agent","تحليل الأنواع والتكرار والتقييم واقتراح إجراء وقائي","Closed","Analyzed","operations_manager","أسبوعيًا","متوسطة","reports.view") ]},
  {id:"C16",name:"المنتجات والمحتوى والحملات",owner:"products_manager",entry:"منتج/فئة/محتوى/حملة جديدة أو تعديل مطلوب",exit:"محتوى Active/Published أو حملة Sent مع نتائج",kpi:"وقت النشر، أخطاء الاستيراد، وصول الحملة",steps:[
    st("إنشاء المنتج التجاري","products_manager","إدخال SKU والتكلفة والسعر والهامش والتعبئة والضمان وSEO","لا يوجد","Product Draft","inventory_manager","يوم عمل","عالية","catalog.manage"),
    st("مراجعة المخزون والسعر","inventory_manager","تأكيد الرصيد وحد إعادة الطلب والباركود والمستودع","Product Draft","Stock Ready","products_manager","4 ساعات","عالية","inventory.manage"),
    st("تفعيل المنتج","products_manager","ربط الفئة والصور والبدائل والأسعار الخاصة ثم Active","Stock Ready","Product Active","sales_manager","ساعة","عاجلة","catalog.manage"),
    st("استيراد جماعي","products_manager","رفع XLSX/CSV ومراجعة الصفوف المرفوضة وتطبيق التعديلات","Draft Import","Validated / Applied","auditor","يوم عمل","عالية","catalog.manage"),
    st("إدارة الصفحة الرئيسية","products_manager","ترتيب الأقسام والبنرات والجمهور والجدولة ومعاينة النتيجة","Draft","Published / Scheduled","sales_manager","4 ساعات","متوسطة","catalog.manage"),
    st("إنشاء حملة","sales_manager","تحديد القناة والجمهور والمحتوى والموعد","لا يوجد","Campaign Draft","products_manager","4 ساعات","متوسطة","campaigns.manage"),
    st("جدولة وإرسال","products_manager","فحص الجمهور والروابط ثم Scheduled/Processing/Sent","Campaign Draft","Scheduled / Sent","sales_manager","حسب الموعد","عالية","campaigns.manage"),
    st("تحليل التسليم","sales_manager","مراجعة Delivered/Failed/Bounced والنتائج واتخاذ تحسين","Sent","Analyzed","auditor","يوم عمل","متوسطة","reports.view") ]},
  {id:"C17",name:"التقارير المجدولة والتحليل",owner:"auditor",entry:"حاجة قرار أو رقابة أو تقرير دوري",exit:"ReportRun Completed ومخرجات موزعة أو Failed موثق",kpi:"نجاح التشغيل، دقة البيانات، تسليم الموعد",steps:[
    st("اختيار التقرير","auditor","اختيار تقرير جاهز أو مصدر مخصص وفترة وفلاتر","لا يوجد","Definition Draft","auditor","ساعة","متوسطة","reports.view"),
    st("بناء التعريف","auditor","تحديد الحقول والتجميع وطريقة العرض والتحقق من النتائج","Definition Draft","Preview Ready","operations_manager","2 ساعة","متوسطة","reports.view"),
    st("حفظ وجدولة","operations_manager","حفظ القالب وتحديد يومي/أسبوعي/شهري والمستلمين والصيغة","Preview Ready","Scheduled","system_admin","ساعة","متوسطة","reports.view"),
    st("تشغيل التقرير","system_admin","تشغيل الخلفية وتثبيت وقت ومعايير التنفيذ","Scheduled","Processing","auditor","حسب الجدول","عالية","settings.manage"),
    st("توزيع النتيجة","system_admin","تسليم XLSX/PDF للمستلمين وحفظ سجل النجاح","Processing","Completed","auditor","حسب الجدول","عالية","settings.manage"),
    st("معالجة الفشل","system_admin","تسجيل الخطأ الآمن وإعادة التشغيل أو تصحيح التعريف","Processing","Failed / Retried","auditor","4 ساعات","عالية","settings.manage") ]},
  {id:"C18",name:"أمن المنصة والأدوار والتدقيق",owner:"system_admin",entry:"إضافة مسؤول أو تغيير صلاحية أو حدث أمني",exit:"وصول صحيح وسجل تدقيق غير قابل للتعديل وخطر مغلق",kpi:"زمن سحب الوصول، محاولات الدخول الفاشلة، الأحداث المفتوحة",steps:[
    st("إنشاء مستخدم منصة","system_admin","إدخال الوظيفة والقسم وتعيين الأدوار والنطاقات و2FA","لا يوجد","Admin Active","super_admin","ساعة","عالية","roles.manage"),
    st("اعتماد الصلاحيات الحساسة","super_admin","مراجعة أقل صلاحية ومنع فقد آخر مدير أعلى","Admin Active","Access Approved","system_admin","4 ساعات","عاجلة","*"),
    st("مراقبة محاولات الدخول","system_admin","مراجعة النجاح والفشل وIP والجهاز والحظر والتصدير","Sessions Active","Monitored","auditor","يوميًا","عالية","audit.view"),
    st("إلغاء جلسة/تعليق","system_admin","إبطال Refresh Token أو تعليق مستخدم فورًا عند الخطر","Active","Revoked / Suspended","super_admin","15 دقيقة","عاجلة","roles.manage"),
    st("مراجعة سجل التدقيق","auditor","فحص الممثل والكيان وIP وBefore/After والاستثناءات","Audit Logged","Reviewed","super_admin","أسبوعيًا","عالية","audit.view"),
    st("إغلاق النشاط المشبوه","super_admin","تحديد Investigating ثم Resolved/Ignored مع مبرر","Open","Investigating / Resolved","auditor","حسب الشدة","عاجلة","*") ]},
  {id:"C19",name:"الإعدادات والنسخ والاستعادة والإصدار",owner:"system_admin",entry:"تغيير إعداد أو نسخة احتياطية أو إصدار تطبيق",exit:"إعداد مطبق ومدقق أو Backup Completed/Restore Completed",kpi:"نجاح النسخ، RTO/RPO، تغييرات الإعداد الفاشلة",steps:[
    st("طلب تغيير إعداد","system_admin","تحديد القسم والقيمة والسبب وتأثير التغيير","Current Config","Change Draft","super_admin","4 ساعات","عالية","settings.manage"),
    st("اعتماد التغيير الحساس","super_admin","مراجعة الصيانة والأمان والدفع والإصدارات قبل الحفظ","Change Draft","Approved","system_admin","4 ساعات","عاجلة","*"),
    st("تطبيق وتدقيق","system_admin","التحقق من النوع والمدى ثم الحفظ وتسجيل Before/After","Approved","Applied","auditor","ساعة","عالية","settings.manage"),
    st("تشغيل نسخة احتياطية","system_admin","تشغيل نسخة SQLite وحساب SHA-256 وتطبيق الاحتفاظ","Scheduled / Manual","Processing / Completed","auditor","حسب الجدول","عالية","settings.manage"),
    st("اختبار الاستعادة","system_admin","التحقق من سلامة النسخة وطلب نافذة صيانة وإعادة التشغيل","Backup Completed","Validated / AwaitingMaintenanceRestart","super_admin","ربع سنوي","عاجلة","settings.manage"),
    st("بوابة إصدار التطبيق","super_admin","تحديد latest/required version وروابط التحديث ووضع الصيانة","Current Release","Release Ready","system_admin","قبل النشر","عاجلة","*"),
    st("مراقبة ما بعد الإصدار","system_admin","مراجعة الصحة والأخطاء الحرجة والرجوع عند الحاجة","Release Ready","Stable / RolledBack","super_admin","24 ساعة","عاجلة","audit.view") ]},
  {id:"C20",name:"التكاملات والعمليات وإعادة المحاولة",owner:"system_admin",entry:"ربط مزود أو تشغيل Webhook/API أو فشل عملية",exit:"Succeeded أو Failed نهائيًا بعد 3 محاولات مع حل موثق",kpi:"نسبة النجاح، زمن الاستجابة، retries، أخطاء حرجة",steps:[
    st("تهيئة المزود","system_admin","إدخال إعدادات WhatsApp/دفع/خرائط/شحن/بريد/SMS/ERP وغيرها","غير مهيأ","Configured","super_admin","4 ساعات","عالية","settings.manage"),
    st("حفظ الأسرار","system_admin","تشفير بيانات الاعتماد وإخفاؤها بعد الحفظ ومنعها من السجلات","Configured","Secured","auditor","فوري","عاجلة","settings.manage"),
    st("اختبار الجاهزية","system_admin","تشغيل اختبار اتصال آمن وتفعيل المزود عند النجاح","Secured","Ready / Failed","super_admin","ساعة","عالية","settings.manage"),
    st("تنفيذ العملية","system_admin","تشغيل عملية يدوية/آلية وتسجيل endpoint آمن والنتيجة","Ready","Processing / Succeeded","auditor","حسب الحدث","متوسطة","settings.manage"),
    st("جدولة إعادة المحاولة","system_admin","عند الفشل زيادة Attempts وجدولة تأخير متزايد حتى 3","Failed","Retrying","super_admin","فوري","عالية","settings.manage"),
    st("حل الفشل النهائي","super_admin","تعطيل المزود مؤقتًا أو إصلاح الإعداد وإعادة التشغيل المراقب","Failed x3","Resolved / Disabled","auditor","4 ساعات","عاجلة","*") ]},
  {id:"C21",name:"الطلبات المحفوظة والمتكررة",owner:"purchasing_officer",entry:"سلة/طلب يتكرر دوريًا",exit:"جدول نشط ينشئ طلبًا أو متوقف/ملغي دون تكرار",kpi:"نجاح الإنشاء الدوري، الطلبات الفاشلة، وفر الوقت",steps:[
    st("حفظ السلة","requester","تسمية السلة وحفظ البنود والكميات للاستخدام لاحقًا","Cart Active","Cart Saved","purchasing_officer","فوري","منخفضة","orders.create"),
    st("إنشاء جدول متكرر","purchasing_officer","اختيار الطلب المصدر والتكرار وتاريخ التنفيذ والعنوان","Order Completed","Schedule Active","finance_manager","ساعة","متوسطة","orders.create"),
    st("مراجعة الميزانية المستقبلية","finance_manager","تأكيد مركز التكلفة والحد الائتماني لكل تشغيل","Schedule Active","Approved Schedule","purchasing_officer","4 ساعات","عالية","budgets.view"),
    st("إنشاء الطلب الدوري","operations_manager","نسخ البنود بالأسعار والتوفر الحالية وإنشاء طلب جديد","Approved Schedule","Run Created / Failed","purchasing_officer","في الموعد","عالية","orders.manage"),
    st("معالجة تغير السعر/التوفر","purchasing_officer","مراجعة البدائل أو السعر وطلب موافقة جديدة عند التجاوز","Run Failed","Updated / Approval Required","department_manager","يوم عمل","عالية","orders.create"),
    st("إيقاف أو إلغاء الجدول","purchasing_officer","تعليق الجدول أو إلغاؤه ومنع التشغيل القادم","Schedule Active","Paused / Cancelled","company_owner","قبل الموعد","متوسطة","orders.create") ]},
  {id:"C22",name:"حساب العميل والإشعارات والخصوصية",owner:"company_owner",entry:"مستخدم نشط يحتاج تعديل ملف/أمان/تفضيلات أو حذف",exit:"إعداد محفوظ وجلسات آمنة أو حذف مكتمل بعد المهلة",kpi:"نجاح 2FA، الأجهزة الملغاة، تفضيلات الإشعار، طلبات الحذف",steps:[
    st("تحديث الملف والتفضيلات","requester","تعديل اللغة والمظهر ووسائل الإشعار المسموحة","Active","Preferences Saved","company_admin","فوري","منخفضة","orders.view"),
    st("تغيير كلمة المرور","requester","إدخال الحالية وكلمة قوية ثم إلغاء الجلسات الأخرى","Active","Password Changed","company_admin","فوري","عالية","orders.view"),
    st("تفعيل 2FA","requester","تفعيل التحدي والتحقق برمز SMS قبل إصدار JWT","2FA Off","2FA On","company_admin","15 دقيقة","عالية","orders.view"),
    st("إدارة الأجهزة","requester","مراجعة IP والجهاز وآخر نشاط وإلغاء جلسة مشبوهة","Sessions Active","Session Revoked","company_admin","فوري","عاجلة","orders.view"),
    st("طلب حذف الحساب","requester","تأكيد الطلب وبدء مهلة 30 يومًا قابلة للإلغاء","Active","Requested","company_owner","فوري","عالية","orders.view"),
    st("إلغاء طلب الحذف","requester","إلغاء الطلب قبل انتهاء المهلة واستعادة الحالة الطبيعية","Requested","Cancelled","company_owner","خلال 30 يومًا","متوسطة","orders.view"),
    st("إكمال الحذف","system_admin","تنفيذ الحذف/إخفاء البيانات حسب السياسة بعد انتهاء المهلة","Requested","Completed","auditor","بعد 30 يومًا","عاجلة","settings.manage") ]},
];

const cycleRows = [];
const taskRows = [];
const raciRows = [];
for (const c of cycleDefs) {
  c.steps.forEach((x, idx) => {
    const seq = idx + 1;
    const next = c.steps[idx + 1]?.name ?? "نهاية الدورة / متابعة KPI";
    cycleRows.push([c.id,c.name,seq,x.name,x.role,roleName[x.role],roleDept[x.role],x.action,c.entry,x.from,x.to,next,x.nextRole,roleName[x.nextRole] ?? "—",x.sla,x.priority,x.permission,"إشعار داخل النظام + سجل نشاط",x.evidence,x.exception]);
    taskRows.push([`${c.id}-T${String(seq).padStart(2,"0")}`,x.role,roleName[x.role],roleDept[x.role],c.id,c.name,x.name,c.entry,x.action,x.from,x.to,x.nextRole,roleName[x.nextRole] ?? "—",x.sla,x.priority,x.permission,x.evidence,c.kpi,x.exception]);
  });
  const participants = [...new Set(c.steps.flatMap(x => [x.role,x.nextRole]).filter(Boolean))];
  for (const role of participants) {
    let raci = "I";
    if (role === c.owner) raci = "A";
    else if (c.steps.some(x => x.role === role)) raci = "R";
    else if (c.steps.some(x => x.nextRole === role)) raci = "C";
    raciRows.push([c.id,c.name,role,roleName[role] ?? role,raci,raci === "A" ? "مسؤول نهائي عن نتيجة الدورة" : raci === "R" ? "ينفذ خطوة أو أكثر" : raci === "C" ? "يُستشار أو يستلم للتدقيق/القرار" : "يتم إخطاره بالنتيجة"]);
  }
}

const statusTranslations = {
  Draft:"مسودة",Active:"نشط",Archived:"مؤرشف",Lead:"فرصة أولية",Saved:"محفوظ",Converted:"محوّل",Abandoned:"متروك",Ready:"جاهز",Submitted:"مُرسل",Expired:"منتهي",PendingApproval:"بانتظار الموافقة",Confirmed:"مؤكد",Processing:"قيد المعالجة",Picking:"قيد الالتقاط",Packing:"قيد التعبئة",Shipped:"تم الشحن",OutForDelivery:"خارج للتسليم",PartiallyDelivered:"تسليم جزئي",Delivered:"تم التسليم",Completed:"مكتمل",Delayed:"متأخر",Cancelled:"ملغي",Pending:"قيد الانتظار",ChangesRequested:"مطلوب تعديل",Approved:"معتمد",Rejected:"مرفوض",Waiting:"منتظر",Current:"الخطوة الحالية",Skipped:"متجاوز",Extracting:"استخراج البنود",NeedsReview:"يحتاج مراجعة",UnderReview:"تحت المراجعة",ClarificationRequested:"مطلوب استيضاح",Quoted:"تم التسعير",Negotiating:"قيد التفاوض",Accepted:"مقبول",Sent:"مرسل",RevisionRequested:"مطلوب مراجعة",InStock:"متوفر",LowStock:"مخزون منخفض",OutOfStock:"غير متوفر",Backorder:"طلب مؤجل",InProgress:"قيد التنفيذ",Reconciled:"تمت التسوية",Inspection:"قيد الفحص",PartiallyAccepted:"قبول جزئي",PartiallyReceived:"استلام جزئي",Received:"مستلم",Matched:"مطابق",Variance:"يوجد فرق",PartiallyPaid:"مدفوع جزئيًا",Paid:"مدفوع",Issued:"صادرة",Overdue:"متأخرة",Initiated:"بدأت",PendingVerification:"بانتظار التحقق",Failed:"فشل",Open:"مفتوح",Closed:"مغلق",Qualified:"مؤهل",Proposal:"عرض",Negotiation:"تفاوض",AtRisk:"معرض للخطر",Dormant:"خامل",Lost:"مفقود",Strategic:"استراتيجي",KeyAccount:"حساب رئيسي",Standard:"قياسي",Prospect:"محتمل",Expiring:"قارب الانتهاء",Suspended:"معلق",PendingCustomerApproval:"بانتظار موافقة العميل",Scheduled:"مجدول",Applied:"مطبق",AwaitingQuote:"بانتظار عرض",DesignInProgress:"التصميم جارٍ",AwaitingDesignApproval:"بانتظار اعتماد التصميم",DesignApproved:"التصميم معتمد",AwaitingCheckout:"بانتظار الدفع",AwaitingOrderApproval:"بانتظار اعتماد الطلب",AwaitingSampleApproval:"بانتظار اعتماد العينة",InProduction:"قيد الإنتاج",QualityCheck:"فحص جودة",Blocked:"متوقف",Planned:"مخطط",Optimized:"محسن",Skipped:"متجاوز",PickupScheduled:"تمت جدولة الاستلام",InTransit:"في الطريق",Inspecting:"قيد الفحص",RefundApproved:"تم اعتماد الاسترداد",RefundCompleted:"تم الاسترداد",ReplacementPreparing:"تجهيز البديل",ReplacementShipped:"شُحن البديل",ReplacementDelivered:"تم تسليم البديل",RequiresAction:"يتطلب إجراء",Succeeded:"نجح",WaitingCustomer:"بانتظار العميل",Resolved:"تم الحل",Requested:"مطلوب",Immediate:"فوري",Optimal:"وقت مثالي",Queued:"في الطابور",Bounced:"مرتد",DeliveredStatus:"وصل",Information:"معلومات",Warning:"تحذير",Error:"خطأ",Critical:"حرج",Investigating:"قيد التحقيق",Ignored:"تم التجاهل",Validated:"تم التحقق",AwaitingMaintenanceRestart:"بانتظار إعادة تشغيل الصيانة",Retrying:"إعادة محاولة",Created:"تم الإنشاء",FailedDelivery:"فشل التسليم",Disabled:"معطل",Paused:"متوقف مؤقتًا",UnderReviewStatus:"تحت المراجعة"
};

const statusEntities = [
  ["TenantStatus","حساب الشركة",["PendingVerification","UnderReview","Active","Rejected","Suspended"],["sales_manager"],true],
  ["CompanyInviteStatus","دعوة مستخدم",["Pending","Accepted","Expired","Cancelled"],["company_admin"],true],
  ["CartStatus","السلة",["Active","Saved","Converted","Abandoned"],["requester","purchasing_officer"],true],
  ["CheckoutStatus","الدفع/الإرسال",["Draft","Ready","Submitted","Expired"],["purchasing_officer"],true],
  ["OrderStatus","الطلب",["PendingApproval","Confirmed","Processing","Picking","Packing","Shipped","OutForDelivery","PartiallyDelivered","Delivered","Completed","Delayed","Cancelled"],["operations_manager"],true],
  ["ApprovalRequestStatus","طلب الموافقة",["Pending","ChangesRequested","Approved","Rejected","Cancelled"],["approver","finance_manager","department_manager"],true],
  ["ApprovalStepStatus","خطوة الموافقة",["Waiting","Current","Approved","Rejected","ChangesRequested","Skipped"],["approver"],false],
  ["RfqStatus","طلب عرض السعر",["Draft","Extracting","NeedsReview","Submitted","UnderReview","ClarificationRequested","Quoted","Negotiating","Accepted","Rejected","Converted","Expired","Cancelled"],["quotes_officer"],true],
  ["CustomerQuoteStatus","نسخة عرض العميل",["Draft","Sent","Accepted","Rejected","RevisionRequested","Expired"],["quotes_officer","sales_manager"],true],
  ["ProductStatus","المنتج",["Draft","Active","Archived"],["products_manager"],true],
  ["StockStatus","توفر المخزون",["InStock","LowStock","OutOfStock","Backorder"],["inventory_manager"],true],
  ["StockCountStatus","جلسة الجرد",["Draft","InProgress","Reconciled","Cancelled"],["inventory_manager","warehouse_manager"],false],
  ["GoodsReceiptStatus","إذن الاستلام",["Draft","Inspection","Accepted","PartiallyAccepted","Rejected"],["warehouse_manager","inventory_manager"],false],
  ["PurchaseOrderStatus","أمر شراء المورد",["Draft","Sent","Confirmed","PartiallyReceived","Received","Cancelled"],["procurement_officer"],false],
  ["SupplierInvoiceStatus","فاتورة المورد",["Draft","Matched","Variance","Approved","PartiallyPaid","Paid"],["accountant","procurement_officer"],false],
  ["InvoiceStatus","فاتورة العميل",["Draft","Issued","PartiallyPaid","Paid","Overdue","Cancelled"],["accountant"],true],
  ["InvoicePaymentStatus","دفعة الفاتورة",["Initiated","PendingVerification","Completed","Rejected","Failed"],["accountant","finance_manager"],true],
  ["FinancialPeriodStatus","الفترة المالية",["Open","Closed"],["accountant"],false],
  ["CustomerStage","مرحلة العميل",["Lead","Qualified","Proposal","Negotiation","Active","AtRisk","Dormant","Lost"],["sales_agent","sales_manager"],false],
  ["CrmTaskStatus","مهمة CRM",["Open","InProgress","Completed","Cancelled"],["sales_agent","sales_manager"],false],
  ["CompanyContractStatus","العقد",["Draft","PendingApproval","Active","Expiring","Expired","Suspended"],["sales_manager","finance_manager"],true],
  ["ContractApprovalStatus","اعتماد العقد",["Pending","Approved","Rejected"],["sales_manager","finance_manager"],false],
  ["ContractPriceRevisionStatus","مراجعة سعر العقد",["PendingCustomerApproval","Scheduled","Applied","Rejected"],["sales_manager","company_owner"],true],
  ["CustomRequestStatus","طلب الطباعة",["Draft","AwaitingQuote","Quoted","DesignInProgress","AwaitingDesignApproval","DesignApproved","AwaitingCheckout","AwaitingOrderApproval","AwaitingSampleApproval","InProduction","QualityCheck","Ready","Completed","Rejected","Cancelled"],["graphic_designer","printing_officer","quotes_officer"],true],
  ["DesignApprovalDecision","قرار التصميم",["Pending","Approved","RevisionRequested","Rejected"],["purchasing_officer"],true],
  ["ProductionStageStatus","مرحلة الإنتاج",["Pending","InProgress","Completed","Blocked"],["printing_officer"],false],
  ["DeliveryRouteStatus","مسار التوصيل",["Planned","Optimized","InProgress","Completed","Cancelled"],["operations_manager","delivery_driver"],false],
  ["DeliveryStopStatus","محطة التوصيل",["Pending","InProgress","Delivered","Failed","Skipped"],["delivery_driver"],true],
  ["ReturnStatus","طلب المرتجع",["Draft","Submitted","UnderReview","Approved","Rejected","PickupScheduled","InTransit","Received","Inspecting","RefundApproved","RefundCompleted","ReplacementPreparing","ReplacementShipped","ReplacementDelivered","Completed","Cancelled"],["support_agent","operations_manager","accountant"],true],
  ["RefundTransactionStatus","معاملة الاسترداد",["Pending","Processing","Completed","Failed"],["accountant"],true],
  ["SupportTicketStatus","تذكرة الدعم",["Open","InProgress","WaitingCustomer","Resolved","Closed"],["support_agent"],true],
  ["MarketingCampaignStatus","الحملة",["Draft","Scheduled","Processing","Sent","Cancelled"],["sales_manager","products_manager"],false],
  ["MarketingDeliveryStatus","تسليم الرسالة",["Queued","Delivered","Failed","Bounced"],["system_admin"],false],
  ["ReportRunStatus","تشغيل التقرير",["Processing","Completed","Failed"],["system_admin"],false],
  ["SuspiciousActivityStatus","نشاط مشبوه",["Open","Investigating","Ignored","Resolved"],["system_admin","super_admin"],false],
  ["RestoreRequestStatus","طلب الاستعادة",["Validated","AwaitingMaintenanceRestart","Completed","Failed","Cancelled"],["system_admin","super_admin"],false],
  ["SystemBackupStatus","النسخة الاحتياطية",["Processing","Completed","Failed"],["system_admin"],false],
  ["IntegrationOperationStatus","عملية التكامل",["Processing","Succeeded","Failed","Retrying"],["system_admin"],false],
  ["AccountDeletionStatus","حذف الحساب",["Requested","Cancelled","Completed"],["requester","system_admin"],true],
];

const terminalSet = new Set(["Accepted","Archived","Paid","Closed","Completed","Cancelled","Rejected","Expired","Lost","Applied","Resolved","Ignored","Succeeded","Abandoned","Reconciled","RefundCompleted","ReplacementDelivered"]);
const statusRows = [];
for (const [enumName,entity,states,setters,visible] of statusEntities) {
  states.forEach((state,i) => {
    const next = states.slice(i+1, Math.min(states.length,i+4)).join(" / ") || "حالة نهائية";
    statusRows.push([enumName,entity,state,statusTranslations[state] ?? state,setters.join(" / "),next,terminalSet.has(state) || i === states.length-1 ? "نعم" : "لا",visible ? "نعم" : "لا","يُسجل وقت التغيير والمستخدم والسبب في سجل الحالة/التدقيق"]);
  });
}

const handoffRows = cycleRows.map(r => [r[0],r[1],r[3],r[4],r[5],r[12],r[13],r[10],r[14],r[17],r[18],r[19]]);
const sources = [
  ["Infrastructure/DbSeeder.cs","الأدوار والصلاحيات النظامية (9 عميل + 16 منصة)","مصدر تنفيذي"],
  ["docs/security/permissions-matrix.md","حدود الوصول العامة ونطاقات الفروع والمستودعات","مرجع أمني"],
  ["Mohandseto_Tawredat_Full_Autonomous_Execution_Spec.md","نطاق النظام والسيناريو ودورة المشتريات المؤسسية","مواصفات"],
  ["MILESTONE_65_COMPANY_ACCOUNT.md","حساب الشركة والمستخدمون والدعوات والموافقات والميزانيات","تنفيذ موثق"],
  ["MILESTONE_70_ENGAGEMENT_SETTINGS.md","الإشعارات والدعم والأمان وإعدادات المستخدم","تنفيذ موثق"],
  ["MILESTONE_75_ADMIN_ORDERS.md","تشغيل الطلبات والتجهيز والاستثناءات","تنفيذ موثق"],
  ["MILESTONE_78_ADMIN_QUOTES.md","RFQ والتسعير والموردون والتفاوض والتحويل","تنفيذ موثق"],
  ["MILESTONE_80_PRODUCT_COMMERCIAL.md + MILESTONE_82_ADMIN_CONTENT.md","المنتجات والتسعير والمحتوى والحملات","تنفيذ موثق"],
  ["MILESTONE_85_INVENTORY.md","المخزون والجرد والتحويل والاستلام","تنفيذ موثق"],
  ["MILESTONE_88_PROCUREMENT.md","الموردون وأوامر الشراء والمطابقة والمرتجعات","تنفيذ موثق"],
  ["MILESTONE_92_COMPANY_CRM.md","CRM والمراحل والأنشطة وصحة العميل","تنفيذ موثق"],
  ["MILESTONE_94_CONTRACT_LIFECYCLE.md","العقود والاعتمادات والأسعار والتجديد","تنفيذ موثق"],
  ["MILESTONE_97_PRINTING_DESIGN.md","التصميم والعينات والإنتاج والجودة","تنفيذ موثق"],
  ["MILESTONE_98_SHIPPING_DELIVERY.md","الشحن والمسارات وإثبات التسليم","تنفيذ موثق"],
  ["MILESTONE_99_ACCOUNTS_CUSTOMER_SERVICE.md","الحسابات والمرتجعات والدعم وSLA","تنفيذ موثق"],
  ["MILESTONE_100_ADMIN_SYSTEM_ACCESS.md","مستخدمي المنصة والصلاحيات والجلسات والتدقيق","تنفيذ موثق"],
  ["MILESTONE_102_REPORTING_ENGINE.md","التقارير الجاهزة والمخصصة والمجدولة","تنفيذ موثق"],
  ["MILESTONE_103_SYSTEM_SETTINGS.md","الإعدادات والنسخ والأمان والإصدارات","تنفيذ موثق"],
  ["MILESTONE_104_INTEGRATIONS_HUB.md","التكاملات والتشفير والسجلات وإعادة المحاولة","تنفيذ موثق"],
  ["Domain/Entities/*.cs","قاموس الحالات والتحولات التنفيذية","مصدر تنفيذي"],
];

const riskMeta = [
  ["C01","قبول شركة بمستندات ناقصة أو غير صحيحة","ضعف التحقق أو ضغط زمن التفعيل","مخاطر قانونية وائتمانية وبيانات عميل غير موثوقة","قائمة مستندات إلزامية ومراجعة ثنائية قبل Active","تقرير الشركات المعلقة والمستندات Pending/Rejected","إيقاف التفعيل وطلب مستند بديل وإعادة المراجعة","sales_manager",3,5,"عدد طلبات التفعيل المعادة أو المرفوضة","أسبوعي"],
  ["C02","منح مستخدم صلاحيات أو نطاق وصول أوسع من المطلوب","اختيار دور خاطئ أو عدم مراجعة النقل/التعطيل","تسريب بيانات أو تنفيذ عملية غير مصرح بها","مبدأ أقل صلاحية وموافقة المالك وإلغاء الجلسات عند التعطيل","مراجعة الأدوار والجلسات والنطاقات النشطة","سحب الدور وإبطال الجلسات وفتح مراجعة تدقيق","company_owner",3,5,"مستخدمون بلا فرع أو بأدوار متعارضة","شهري"],
  ["C03","إنشاء طلب بسعر أو كمية أو توفر غير صحيح","بيانات كتالوج قديمة أو عدم فحص الحد الأدنى والمخزون","إعادة عمل وتأخير وفقد هامش أو رضا العميل","إعادة تسعير وفحص المخزون والحدود قبل Submitted","طلبات متأخرة أو بنود غير متاحة بعد الإرسال","تقديم بديل أو طلب موافقة سعر جديدة","purchasing_officer",3,4,"نسبة تعديلات الطلب بعد الإرسال","أسبوعي"],
  ["C04","تعطل الموافقات أو تجاوز الميزانية","مستوى غير معين أو SLA غير واقعي أو تعارض ميزانية","تأخير الشراء والتزام مالي غير معتمد","سياسة مستويات وحدود وSLA وحجز ميزانية آلي","قائمة الموافقات المتأخرة وتعارضات الميزانية","تصعيد أو تفويض موثق وإعادة الحجز بعد القرار","company_owner",4,5,"عدد الموافقات المتجاوزة لـSLA","يومي"],
  ["C05","عرض سعر غير مربح أو مبني على تكلفة منتهية","عدم تحديث سعر المورد أو خطأ استخراج البنود","خسارة هامش أو رفض العميل أو عدم القدرة على التوريد","نسخ عروض غير قابلة للتعديل ومقارنة موردين واعتماد هامش","تقرير العروض منخفضة الهامش والمنتهية","إصدار Revision مع توثيق السعر والسبب","quotes_officer",3,5,"هامش العرض ومعدل Revision","يومي"],
  ["C06","التقاط أو تعبئة منتج/كمية خاطئة","فشل مسح الباركود أو عدم مطابقة قائمة الالتقاط","مرتجع وتكلفة شحن وتأخير العميل","حجز ذري وقائمة التقاط وتحقق مزدوج قبل Packing","فروقات الالتقاط ومشكلات الطلبات بعد الشحن","إيقاف الشحنة وتصحيح الطرد وتسجيل الحادث","operations_manager",3,5,"نسبة أخطاء الالتقاط لكل 100 طلب","يومي"],
  ["C07","رصيد سالب أو فروق جرد غير مبررة","حركة خارج النظام أو تسوية متكررة أو تحويل غير مكتمل","نفاد مفاجئ وقيمة مخزون غير دقيقة","حركات غير قابلة للتعديل وتحويل ذري وتسوية مرة واحدة","تقرير السالب والفروقات والجلسات غير المكتملة","تجميد SKU والجرد الفوري والتحقيق في الحركات","inventory_manager",3,5,"قيمة فروق الجرد ودقة المخزون","يومي"],
  ["C08","تأخر المورد أو قبول استلام غير مطابق","اختيار مورد ضعيف أو فحص غير كافٍ","توقف تنفيذ الطلب وفروق مالية وجودة منخفضة","تقييم مورد وPO مؤكد وفحص استلام وربط دفعات","التسليم المتأخر ونسب الرفض وVariance","استلام جزئي أو مورد بديل أو مرتجع مورد","procurement_officer",4,4,"OTD المورد ونسبة القبول","أسبوعي"],
  ["C09","فاتورة مكررة أو دفعة غير مطابقة أو إقفال غير سليم","إدخال يدوي أو تحويل غير مربوط أو قيود مسودة","أرصدة عميل خاطئة ومخاطر مالية وضريبية","منع التكرار والزيادة ومطابقة التحويل ومنع الإقفال مع المعلقات","تقرير التحويلات غير المطابقة والقيود Draft","عكس القيد أو رفض الدفعة وإعادة فتح المعالجة قبل الإقفال","accountant",3,5,"قيمة غير المطابق ومدة التحصيل DSO","يومي"],
  ["C10","فرصة بلا متابعة أو مرحلة CRM غير واقعية","مهام متأخرة أو غياب سبب الانتقال","فقد مبيعات وتوقعات غير دقيقة","مهمة وتاريخ استحقاق لكل انتقال وسجل مرحلة بسبب","تقرير الأنشطة المتأخرة والحسابات Dormant/AtRisk","إعادة تعيين المسؤول وخطة تنشيط أو إغلاق Lost بسبب","sales_manager",4,4,"نسبة الفرص بلا نشاط 7 أيام","أسبوعي"],
  ["C11","تفعيل عقد بلا نسخة موقعة أو بسعر يضر الهامش","تجاوز بوابة الاعتماد أو تعديل سعر بلا موافقة العميل","التزام قانوني وخسارة مالية ونزاع عميل","نسخة موقعة إلزامية واعتماد مبيعات ومالية وتاريخ سريان","العقود PendingApproval والمراجعات غير المطبقة","تعليق العقد وسحب السعر المنشور وإعادة الاعتماد","sales_manager",2,5,"عقود بلا مرفق موقع أو هامش أقل من الحد","أسبوعي"],
  ["C12","بدء إنتاج قبل اعتماد التصميم/العينة أو فشل الجودة","ضغط موعد أو تجاهل بوابة العميل والجودة","هالك إنتاج وإعادة طباعة وتأخير كبير","اعتماد تصميم وعينة إلزامي ومراحل متسلسلة وبوابة جودة","المراحل Blocked وفحوص الجودة الراسبة","إيقاف الإنتاج وعزل الكمية وإعادة العينة/الطباعة","printing_officer",3,5,"First-pass yield ووقت إعادة العمل","يومي"],
  ["C13","فشل التسليم أو غياب إثبات صحيح","عنوان/مسار غير دقيق أو مندوب محمل أو إثبات ناقص","نزاع تسليم وتكلفة محاولة إضافية وتأخير","تحسين مسار وتعيين حسب الحمل وOTP/GPS/توقيع","المحاولات الفاشلة والشحنات دون Proof","إعادة جدولة مستقبلية أو تصعيد لخدمة العملاء","operations_manager",4,4,"نجاح أول محاولة وOTD","يومي"],
  ["C14","مرتجع غير مؤهل أو استرداد مزدوج أو تصرف مخزون خاطئ","مراجعة أدلة ضعيفة أو فصل غير واضح بين القرار والتنفيذ","خسارة مالية ورصيد مخزون غير صحيح","قرار أهلية وفحص مستودع ومنع تكرار الاسترداد وحركة مخزون ثابتة","المرتجعات المفتوحة ومعاملات Refund Failed/مكررة","تجميد المعاملة والتحقيق ثم Credit Note/Replacement صحيح","support_agent",3,5,"زمن المرتجع ونسبة Restock","يومي"],
  ["C15","خرق SLA أو إغلاق تذكرة دون حل مثبت","تعيين متأخر أو تصعيد غير واضح أو انتظار عميل غير مسجل","انخفاض رضا العميل وتصاعد الشكاوى","SLA حسب النوع والأولوية وتعيين وتصعيد قبل الخرق","لوحة التذاكر المتأخرة وFirst Response","تصعيد فوري للمسؤول الوظيفي وإعادة فتح التذكرة","support_agent",4,4,"First response وResolution SLA","يومي"],
  ["C16","نشر سعر/محتوى خاطئ أو إرسال حملة لجمهور غير مناسب","مراجعة ناقصة أو إعداد Audience/جدولة خاطئ","ضرر سمعة وشكاوى وسعر بيع غير صحيح","Preview واعتماد وجدولة واستهداف شركة/قطاع واضح","الصفوف المرفوضة وFailed/Bounced وشكاوى الحملة","إيقاف الحملة وأرشفة المحتوى وتصحيح السعر","products_manager",3,4,"معدل فشل التسليم وأخطاء النشر","لكل نشر"],
  ["C17","تقرير بفلتر خاطئ أو وصول مستلم غير مصرح له","تعريف مخصص غير مراجع أو قائمة مستلمين قديمة","قرار خاطئ أو تسريب بيانات","Preview وصلاحية reports.view وتثبيت الفلاتر والمستلمين","ReportRun Failed وتقارير بلا نتائج أو حجم غير طبيعي","إيقاف الجدول وتصحيح التعريف وإعادة التشغيل","auditor",2,5,"فشل التشغيل وتغير عدد الصفوف غير المتوقع","لكل تشغيل"],
  ["C18","تصعيد صلاحية أو تعليق آخر Super Admin","ضعف الفصل بين المنشئ والمعتمد أو خطأ إدارة المستخدمين","سيطرة غير مصرح بها أو فقد إدارة النظام","مراجعة أقل صلاحية وحارس آخر مدير أعلى و2FA","محاولات الدخول الفاشلة والنشاط المشبوه وتغييرات الأدوار","إبطال الجلسات وتعليق الحساب وتحقيق تدقيق","system_admin",2,5,"تغييرات الصلاحيات الحساسة والنشاط Critical","يومي"],
  ["C19","نسخة احتياطية تالفة أو استعادة غير مختبرة أو إصدار غير مستقر","فشل التخزين أو غياب اختبار دوري أو تغيير إعداد حساس","فقد بيانات أو توقف الخدمة","SHA-256 واحتفاظ واختبار Restore ونافذة صيانة وبوابة إصدار","فشل النسخ وأخطاء ما بعد الإصدار وHealth","تفعيل الصيانة والرجوع للإصدار/النسخة الأخيرة السليمة","system_admin",2,5,"نجاح النسخ وRPO/RTO والأخطاء الحرجة","يومي/ربع سنوي"],
  ["C20","تسريب سر تكامل أو عاصفة إعادة محاولات","تسجيل Credential أو Retry بلا حد أو إعداد خاطئ","اختراق مزود أو ازدواج عمليات أو توقف تكامل","تشفير وإخفاء الأسرار وحد 3 محاولات وتأخير متزايد","Operation Failed/Retrying ومعدل الخطأ لكل مزود","تعطيل المزود وتدوير السر وإصلاح الإعداد ثم تشغيل مراقب","system_admin",3,5,"Failure rate وعدد retries","يومي"],
  ["C21","إنشاء طلب دوري مكرر أو بسعر/توفر تغير دون موافقة","جدول مزدوج أو نسخ سعر قديم أو تشغيل متزامن","شراء زائد وتجاوز ميزانية وتأخير","مرجع تشغيل فريد وفحص السعر والمخزون والميزانية في كل Run","Runs Failed أو أكثر من طلب لنفس الموعد","إيقاف الجدول وإلغاء المكرر وطلب موافقة جديدة","purchasing_officer",3,4,"الطلبات الدورية الفاشلة/المكررة","لكل تشغيل"],
  ["C22","استيلاء على الحساب أو حذف بيانات قبل انتهاء المهلة","جهاز مشبوه أو 2FA غير مفعل أو منطق حذف غير منضبط","فقد وصول وبيانات ونزاع خصوصية","2FA وإدارة جلسات ومهلة حذف 30 يومًا قابلة للإلغاء","جلسات من IP جديد وطلبات حذف مفتوحة","إبطال الجلسات وإلغاء الحذف والتحقيق قبل الإكمال","company_owner",2,5,"جلسات ملغاة وطلبات الحذف خلال المهلة","أسبوعي"],
];

const riskRows = riskMeta.map((r,i) => [`R-${String(i+1).padStart(3,"0")}`,r[0],cycleDefs.find(c=>c.id===r[0])?.name ?? "—",r[1],r[2],r[3],r[4],r[5],r[6],r[7],roleName[r[7]] ?? r[7],r[8],r[9],null,null,r[10],r[11],"مفتوح"]);
const sopRows = cycleDefs.map((c) => {
  const risk = riskMeta.find(r=>r[0]===c.id);
  return [c.id,c.name,c.owner,roleName[c.owner],`إتمام ${c.name} بصورة قابلة للتدقيق من نقطة البدء إلى النتيجة النهائية`,c.entry,`صلاحية ${c.owner} + بيانات الكيان مكتملة + عدم وجود حظر أمني/مالي`,"بيانات الطلب أو الكيان، المستندات الداعمة، المستخدم المنفذ، التاريخ والمرجع",c.steps.length,risk?.[4] ?? "مراجعة ثنائية وسجل تدقيق",risk?.[6] ?? "إيقاف الخطوة والتصعيد الموثق",c.exit,"سجل الحالة، سجل التدقيق، المرفقات، إشعارات التسليم",c.kpi,risk?.[11] ?? "شهري",roleName[c.owner]];
});

const wb = Workbook.create();
const summary = wb.worksheets.add("الملخص التنفيذي");
const guide = wb.worksheets.add("طريقة الاستخدام");
const opsDashboard = wb.worksheets.add("لوحة التشغيل");
const trackerSheet = wb.worksheets.add("متابعة التنفيذ");
const sopSheet = wb.worksheets.add("SOP الدورات");
const risksSheet = wb.worksheets.add("المخاطر والضوابط");
const handoffFormSheet = wb.worksheets.add("نموذج التسليم");
const rolesSheet = wb.worksheets.add("الأدوار والصلاحيات");
const tasksSheet = wb.worksheets.add("مهام كل وظيفة");
const cycleGuide = wb.worksheets.add("دليل الدورات");
const cyclesSheet = wb.worksheets.add("خطوات الدورات");
const raciSheet = wb.worksheets.add("RACI");
const statusesSheet = wb.worksheets.add("قاموس الحالات");
const handoffsSheet = wb.worksheets.add("التسليمات والتنبيهات");
const sourcesSheet = wb.worksheets.add("المصادر");

const C = {navy:"#123B5D",teal:"#16A6A1",light:"#E8F3F4",mint:"#DDF4EF",gold:"#E8A23B",red:"#C9484A",ink:"#17324D",gray:"#66788A",pale:"#F5F8FA",white:"#FFFFFF",line:"#CFDCE5",green:"#2D8A6E"};
const fontBody = "IBM Plex Sans Arabic";
const headerStyle = {fill:C.navy,font:{bold:true,color:C.white,name:fontBody,size:12},horizontalAlignment:"center",verticalAlignment:"center",wrapText:true,borders:{bottom:{style:"medium",color:C.teal}}};
const bodyStyle = {font:{name:fontBody,size:11,color:C.ink},verticalAlignment:"top",horizontalAlignment:"right",wrapText:true,borders:{insideHorizontal:{style:"thin",color:C.line}}};

function title(sheet, lastCol, text, subtitle) {
  sheet.showGridLines = false;
  const t = sheet.getRange(`A1:${lastCol}1`); t.merge(); t.values=[[text]]; t.format={fill:C.navy,font:{name:fontBody,size:22,bold:true,color:C.white},horizontalAlignment:"center",verticalAlignment:"center"}; t.format.rowHeight=36;
  const s = sheet.getRange(`A2:${lastCol}2`); s.merge(); s.values=[[subtitle]]; s.format={fill:C.light,font:{name:fontBody,size:11,color:C.gray,italic:true},horizontalAlignment:"center",verticalAlignment:"center",wrapText:true}; s.format.rowHeight=30;
}
function tableSheet(sheet, lastCol, titleText, subtitle, headers, rows, tableName, widths) {
  title(sheet,lastCol,titleText,subtitle);
  const matrix=[headers,...rows];
  const range=sheet.getRangeByIndexes(3,0,matrix.length,headers.length); range.values=matrix;
  sheet.getRangeByIndexes(3,0,1,headers.length).format=headerStyle; sheet.getRangeByIndexes(3,0,1,headers.length).format.rowHeight=32;
  if(rows.length) sheet.getRangeByIndexes(4,0,rows.length,headers.length).format=bodyStyle;
  const table=sheet.tables.add(`A4:${lastCol}${rows.length+4}`,true,tableName); table.style="TableStyleMedium2"; table.showBandedRows=true; table.showFilterButton=true;
  sheet.freezePanes.freezeRows(4); sheet.freezePanes.freezeColumns(Math.min(3,headers.length));
  widths.forEach((w,i)=>sheet.getRangeByIndexes(0,i,rows.length+4,1).format.columnWidth=w);
  if(rows.length) sheet.getRangeByIndexes(4,0,rows.length,headers.length).format.rowHeight=42;
}

// Summary
title(summary,"J","الملخص التنفيذي — وظائف النظام ودورات العمل","نسخة تشغيلية مبنية على أدوار وصلاحيات وحالات مشروع Mohandseto Tawredat — تاريخ الإعداد 16/07/2026");
summary.getRange("A4:J4").merge(); summary.getRange("A4").values=[["مؤشرات النطاق"]]; summary.getRange("A4:J4").format={fill:C.teal,font:{name:fontBody,size:14,bold:true,color:C.white},horizontalAlignment:"center"};
const cards=[["عدد الوظائف",`=COUNTA('الأدوار والصلاحيات'!$B$5:$B$${roleRows.length+4})`,"عدد المهام",`=COUNTA('مهام كل وظيفة'!$A$5:$A$${taskRows.length+4})`,"عدد الدورات",`=COUNTA('دليل الدورات'!$A$5:$A$${cycleDefs.length+4})`,"خطوات الدورات",`=COUNTA('خطوات الدورات'!$A$5:$A$${cycleRows.length+4})`,"حالات موثقة",`=COUNTA('قاموس الحالات'!$A$5:$A$${statusRows.length+4})`]];
summary.getRange("A6:J6").values=[[cards[0][0],"",cards[0][2],"",cards[0][4],"",cards[0][6],"",cards[0][8],""]];
summary.getRange("A7:J7").values=[["",null,"",null,"",null,"",null,"",null]];
summary.getRange("B7").formulas=[[cards[0][1]]]; summary.getRange("D7").formulas=[[cards[0][3]]]; summary.getRange("F7").formulas=[[cards[0][5]]]; summary.getRange("H7").formulas=[[cards[0][7]]]; summary.getRange("J7").formulas=[[cards[0][9]]];
for (const c of ["A6:B7","C6:D7","E6:F7","G6:H7","I6:J7"]) { summary.getRange(c).format={fill:C.pale,font:{name:fontBody,size:12,color:C.ink,bold:true},horizontalAlignment:"center",verticalAlignment:"center",borders:{preset:"outside",style:"medium",color:C.teal}}; }
summary.getRange("B7,D7,F7,H7,J7").format={font:{name:fontBody,size:20,bold:true,color:C.teal},horizontalAlignment:"center"};
summary.getRange("A10:E10").values=[["الإدارة","عدد المهام","مهام عاجلة","دورات تقودها","ملاحظة تشغيلية"]]; summary.getRange("A10:E10").format=headerStyle;
const departments=[...new Set(roleRows.map(r=>r[4]))];
summary.getRangeByIndexes(10,0,departments.length,1).values=departments.map(x=>[x]);
summary.getRange("B11").formulas=[[`=COUNTIF('مهام كل وظيفة'!$D$5:$D$${taskRows.length+4},A11)`]]; summary.getRange(`B11:B${departments.length+10}`).fillDown();
summary.getRange("C11").formulas=[[`=COUNTIFS('مهام كل وظيفة'!$D$5:$D$${taskRows.length+4},A11,'مهام كل وظيفة'!$O$5:$O$${taskRows.length+4},"عاجلة")`]]; summary.getRange(`C11:C${departments.length+10}`).fillDown();
summary.getRange("D11").formulas=[[`=COUNTIF('دليل الدورات'!$E$5:$E$${cycleDefs.length+4},A11)`]]; summary.getRange(`D11:D${departments.length+10}`).fillDown();
summary.getRangeByIndexes(10,4,departments.length,1).values=departments.map(d=>[d.includes("تقنية")||d.includes("التدقيق")?"رقابة واستمرارية":"تشغيل وتسليم بين الإدارات"]);
summary.getRangeByIndexes(10,0,departments.length,5).format=bodyStyle;
summary.getRange("G10:J10").merge(); summary.getRange("G10").values=[["كيف تقرأ السايكل؟"]]; summary.getRange("G10:J10").format=headerStyle;
summary.getRange("G11:J17").merge(); summary.getRange("G11").values=[["1) ابدأ من «لوحة التشغيل» لمعرفة الوضع الحالي.\n2) سجّل التنفيذ الفعلي في «متابعة التنفيذ» باختيار Task ID.\n3) استخدم «SOP الدورات» لمعرفة شروط البدء والضوابط والمخرجات.\n4) راجع «المخاطر والضوابط» قبل تنفيذ خطوة حساسة.\n5) استخدم «نموذج التسليم» عند نقل المسؤولية بين وظيفتين.\n6) ارجع لدليل وخطوات الدورات وRACI لفهم المسؤوليات.\n7) لا تُغلق خطوة بلا دليل تنفيذ وسجل تدقيق عند الإجراءات الحساسة."]]; summary.getRange("G11:J17").format={fill:C.light,font:{name:fontBody,size:12,color:C.ink},horizontalAlignment:"right",verticalAlignment:"top",wrapText:true,borders:{preset:"outside",style:"medium",color:C.teal}};
summary.getRange("A1:J40").format.font={name:fontBody}; [18,14,14,14,24,24,24,24,24,24].forEach((w,i)=>summary.getRangeByIndexes(0,i,40,1).format.columnWidth=w); summary.freezePanes.freezeRows(3); summary.showGridLines=false;

// Guide
title(guide,"H","طريقة الاستخدام والسايكل","هذا الملف هو SOP/RACI مبسط؛ كل صف قابل للتصفية ويحتوي على مالك، منفذ، حالة، SLA، دليل وتسليم.");
guide.getRange("A4:H4").values=[["الترتيب","الشيت","الغرض","متى تستخدمه","المفتاح الأساسي","الإجراء المطلوب","المخرج","ملاحظة"]]; guide.getRange("A4:H4").format=headerStyle;
const guideRows=[
  [1,"الملخص التنفيذي","فهم نطاق التشغيل بسرعة","بداية أي مراجعة","الإدارة","حدد الوحدة الأعلى ضغطًا","أولوية مراجعة","الأرقام بصيغ Excel وليست قيمًا ثابتة"],
  [2,"لوحة التشغيل","متابعة الأداء الجاري والمتأخر والمتعثر","اجتماع التشغيل اليومي","حالة التنفيذ","راجع المؤشرات والاستثناءات","قرار يومي","تتحدث تلقائيًا من شيت متابعة التنفيذ"],
  [3,"متابعة التنفيذ","تسجيل نسخة فعلية من المهام","عند بدء أي مهمة تشغيلية","Task ID + Run ID","اختر Task ID وحدد المسؤول والتواريخ والحالة","سجل متابعة","الأعمدة الوصفية تُملأ تلقائيًا من تعريف المهام"],
  [4,"SOP الدورات","معيار تشغيل مختصر لكل دورة","قبل بدء دورة أو تدريب موظف","Cycle ID","راجع الشروط والضوابط والمخرج المطلوب","تنفيذ منضبط","لا يُستبدل به تفاصيل خطوات الدورات"],
  [5,"المخاطر والضوابط","تحديد الخطر والتحكم وخطة الاستجابة","مراجعة رقابية أو قبل إجراء حساس","Risk ID + Cycle ID","قيّم الاحتمال والأثر وتابع الضابط","خطر مراقب","درجة الخطر = الاحتمال × الأثر"],
  [6,"نموذج التسليم","توثيق نقل المسؤولية بين شخصين/وظيفتين","عند نهاية خطوة وبداية أخرى","Handoff ID + Task ID","أرفق الناتج ومعيار القبول والموعد","تسليم مقبول","الرفض يحتاج سببًا وتصعيدًا"],
  [7,"الأدوار والصلاحيات","معرفة الدور وحدود وصوله","تعيين موظف أو مراجعة صلاحية","Role Code","طابق الوظيفة مع الصلاحيات","وصول مضبوط","الأدوار المخصصة داخل الشركات تُراجع منفصلًا"],
  [8,"مهام كل وظيفة","قائمة مهام قابلة للتصفية لكل دور","توصيف وظيفي أو متابعة يومية","Role Code + Task ID","صفِّ حسب الوظيفة/الأولوية","قائمة عمل","كل مهمة مرتبطة بدورة وتسليم"],
  [9,"دليل الدورات","خريطة جميع دورات النظام","اختيار العملية المراد فهمها","Cycle ID","حدد نقطة البداية والنهاية والـKPI","تعريف دورة","مالك الدورة هو Accountable"],
  [10,"خطوات الدورات","التسلسل التفصيلي للحالة والتسليم","تنفيذ العملية خطوة بخطوة","Cycle ID + Sequence","رتب تصاعديًا ونفذ الدليل المطلوب","حالة جديدة","لا تتجاوز حالة دون صلاحية"],
  [11,"RACI","فصل المسؤولية عن التنفيذ","حل تضارب المسؤوليات","Cycle ID + Role Code","حدد A واحدًا وR واحدًا أو أكثر","ملكية واضحة","A=مسؤول نهائي، R=منفذ، C=مستشار، I=مخطر"],
  [12,"قاموس الحالات","فهم Status ومعرفة من يغيره","قبل أي تحديث حالة أو Automation","Entity + Status","تحقق من الانتقال التالي","انتقال صحيح","الانتقالات التفصيلية يحكمها الكود واختبارات التدفق"],
  [13,"التسليمات والتنبيهات","إدارة Handoff بين الفرق","نهاية كل خطوة","Cycle + Step","سلّم الحالة والدليل داخل SLA","استلام واضح","التصعيد موثق عند التأخير"],
  [14,"المصادر","الرجوع للمستند أو الكود","التدقيق أو تحديث الشيت","اسم الملف","حدّث الصف عند تغيير المصدر","تتبع موثوق","تاريخ النسخة 16/07/2026"],
];
guide.getRangeByIndexes(4,0,guideRows.length,8).values=guideRows; guide.getRangeByIndexes(4,0,guideRows.length,8).format=bodyStyle; guide.getRangeByIndexes(4,0,guideRows.length,8).format.rowHeight=52; [10,22,28,25,20,30,22,30].forEach((w,i)=>guide.getRangeByIndexes(0,i,24,1).format.columnWidth=w); guide.freezePanes.freezeRows(4); guide.showGridLines=false;
guide.getRange("A21:H21").merge(); guide.getRange("A21").values=[["قاعدة تشغيل مختصرة: Trigger → Input → Action → Status Change → Evidence → Handoff → SLA/KPI"]]; guide.getRange("A21:H21").format={fill:C.gold,font:{name:fontBody,size:14,bold:true,color:C.white},horizontalAlignment:"center",verticalAlignment:"center"}; guide.getRange("A21:H21").format.rowHeight=32;

tableSheet(rolesSheet,"J","الأدوار والصلاحيات","الأدوار النظامية الفعلية كما هي مزروعة في DbSeeder؛ صلاحيات الدور المخصص لا تُفترض بل تُراجع من قاعدة البيانات.",["نوع المستخدم","Role Code","الوظيفة","English","الإدارة","الغرض الوظيفي","الوحدات الأساسية","صلاحية قرار؟","يسلّم إلى / يتعاون مع","Permission Codes"],roleRows,"RolesPermissionsTable",[13,22,20,20,20,38,30,16,27,28]);
tableSheet(tasksSheet,"S","مهام كل وظيفة","صفِّ عمود «الوظيفة» أو Role Code لتحصل على Job Description تشغيلي كامل مرتبط بالسايكل والتسليم والـSLA.",["Task ID","Role Code","الوظيفة","الإدارة","Cycle ID","اسم الدورة","المهمة","متى تبدأ","طريقة التنفيذ داخل النظام","الحالة قبل","الحالة بعد","يسلّم إلى Code","يسلّم إلى","SLA","الأولوية","Permission","دليل التنفيذ","KPI الدورة","الاستثناء/التصعيد"],taskRows,"RoleTasksTable",[14,21,20,19,12,28,25,30,42,22,24,21,20,16,13,22,28,25,34]);
tasksSheet.getRange(`O5:O${taskRows.length+4}`).dataValidation={rule:{type:"list",values:["عاجلة","عالية","متوسطة","منخفضة"]}};
tasksSheet.getRange(`O5:O${taskRows.length+4}`).conditionalFormats.add("containsText",{text:"عاجلة",format:{fill:"#FCE3E3",font:{bold:true,color:C.red}}});
tasksSheet.getRange(`O5:O${taskRows.length+4}`).conditionalFormats.add("containsText",{text:"عالية",format:{fill:"#FFF0D6",font:{bold:true,color:"#9A5A00"}}});

const cycleGuideRows=cycleDefs.map(c=>[c.id,c.name,c.owner,roleName[c.owner],roleDept[c.owner],c.entry,c.exit,c.steps.length,[...new Set(c.steps.map(x=>roleName[x.role]))].join("، "),c.kpi]);
tableSheet(cycleGuide,"J","دليل دورات النظام","22 دورة شاملة من تسجيل الشركة حتى التشغيل والدعم والتقارير والأمن والتكاملات.",["Cycle ID","اسم الدورة","Owner Code","مالك الدورة","إدارة المالك","نقطة البداية","نقطة النهاية","عدد الخطوات","الوظائف المنفذة","KPI"],cycleGuideRows,"CycleDirectoryTable",[12,32,21,20,19,38,38,14,48,32]);
tableSheet(cyclesSheet,"T","خطوات دورات العمل","رتّب حسب Cycle ID ثم الترتيب. كل صف يوضح Trigger والحالة قبل/بعد والتسليم والدليل والتصعيد.",["Cycle ID","اسم الدورة","#","الخطوة","Role Code","الوظيفة","الإدارة","الإجراء داخل النظام","Trigger الدورة","الحالة قبل","الحالة بعد","الخطوة التالية","Next Role Code","المستلم","SLA","الأولوية","Permission","التنبيه","دليل التنفيذ","الاستثناء/التصعيد"],cycleRows,"CycleStepsTable",[12,30,8,25,21,20,18,42,32,22,25,27,21,20,15,13,22,27,28,34]);
cyclesSheet.getRange(`P5:P${cycleRows.length+4}`).conditionalFormats.add("containsText",{text:"عاجلة",format:{fill:"#FCE3E3",font:{bold:true,color:C.red}}});
tableSheet(raciSheet,"F","مصفوفة RACI الطولية","يمكن تحويلها بسهولة إلى Pivot: الصفوف=الدورات، الأعمدة=الوظائف، القيم=RACI.",["Cycle ID","اسم الدورة","Role Code","الوظيفة","RACI","معنى المشاركة"],raciRows,"RaciTable",[12,34,22,22,10,40]);
raciSheet.getRange(`E5:E${raciRows.length+4}`).dataValidation={rule:{type:"list",values:["A","R","C","I"]}};
raciSheet.getRange(`E5:E${raciRows.length+4}`).conditionalFormats.add("containsText",{text:"A",format:{fill:C.navy,font:{bold:true,color:C.white}}});
raciSheet.getRange(`E5:E${raciRows.length+4}`).conditionalFormats.add("containsText",{text:"R",format:{fill:C.mint,font:{bold:true,color:C.green}}});
raciSheet.getRange(`E5:E${raciRows.length+4}`).conditionalFormats.add("containsText",{text:"C",format:{fill:"#FFF0D6",font:{bold:true,color:"#9A5A00"}}});
tableSheet(statusesSheet,"I","قاموس حالات النظام","مرجع سريع للحالات الأساسية. الانتقال النهائي يُنفذ فقط عبر خدمات النظام والصلاحيات المحددة.",["Enum","الكيان","Status Code","المعنى العربي","من يغير الحالة","الحالات التالية المحتملة","نهائية؟","تظهر للعميل؟","أثر التدقيق"],statusRows,"StatusDictionaryTable",[27,22,28,24,30,38,12,16,38]);
statusesSheet.getRange(`G5:G${statusRows.length+4}`).conditionalFormats.add("containsText",{text:"نعم",format:{fill:C.mint,font:{bold:true,color:C.green}}});
tableSheet(handoffsSheet,"L","التسليمات والتنبيهات بين الوظائف","كل Handoff يحمل حالة جديدة، دليل تنفيذ، مستلم، SLA، وتنبيه؛ لا تعتمد على التواصل الشفهي وحده.",["Cycle ID","اسم الدورة","الخطوة","From Code","من الوظيفة","To Code","إلى الوظيفة","الحالة المسلّمة","SLA","التنبيه","حزمة التسليم/الدليل","التصعيد"],handoffRows,"HandoffsTable",[12,30,26,21,20,21,20,25,15,28,30,36]);
tableSheet(sourcesSheet,"C","مصادر الشيت","المحتوى مشتق من الكود والمواصفات وملفات التسليم الموجودة داخل المشروع، وليس من افتراضات خارجية.",["المصدر","ما تم استخراجه","النوع"],sources,"SourcesTable",[54,68,20]);

for (const sh of [rolesSheet,tasksSheet,cycleGuide,cyclesSheet,raciSheet,statusesSheet,handoffsSheet,sourcesSheet]) {
  sh.getUsedRange().format.font={name:fontBody};
}

// Formula/error inspection before export
const errorScan = await wb.inspect({kind:"match",searchTerm:"#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A",options:{useRegex:true,maxResults:100},summary:"final formula error scan"});
await fs.writeFile(`${outputDir}/inspection-errors.ndjson`,errorScan.ndjson ?? String(errorScan),"utf8");
const keyInspect = await wb.inspect({kind:"region",sheetId:"الملخص التنفيذي",range:"A1:J25",maxChars:12000});
await fs.writeFile(`${outputDir}/inspection-summary.ndjson`,keyInspect.ndjson ?? String(keyInspect),"utf8");

for (const sh of [summary,guide,rolesSheet,tasksSheet,cycleGuide,cyclesSheet,raciSheet,statusesSheet,handoffsSheet,sourcesSheet]) {
  const preview = await wb.render({sheetName:sh.name,autoCrop:"all",scale:0.55,format:"png"});
  await fs.writeFile(`${previewDir}/${sh.name}.png`,new Uint8Array(await preview.arrayBuffer()));
}

const xlsx = await SpreadsheetFile.exportXlsx(wb);
await xlsx.save(`${outputDir}/Mohandseto_Roles_Tasks_System_Cycles.xlsx`);
console.log(JSON.stringify({output:`${outputDir}/Mohandseto_Roles_Tasks_System_Cycles.xlsx`,roles:roleRows.length,tasks:taskRows.length,cycles:cycleDefs.length,steps:cycleRows.length,statuses:statusRows.length,raci:raciRows.length},null,2));
