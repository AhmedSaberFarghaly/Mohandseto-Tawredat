using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Application.AdminSystemSettings;

public sealed class AdminSystemSettingsService(AppDbContext db, IDataProtectionProvider protection, IWebHostEnvironment environment)
{
    private sealed record Field(string Code, string Label, string Type, string Default, bool Required = false,
        string[]? Options = null, string? Help = null, bool Sensitive = false, decimal? Min = null, decimal? Max = null);
    private sealed record Section(string Code, string Ar, string En, string Category, string Icon, string Description, Field[] Fields);
    private readonly IDataProtector _protector = protection.CreateProtector("Mohandseto.SystemSettings.v1");

    private static readonly Section[] Definitions =
    [
        S("general","الإعدادات العامة","General Settings","general","⚙","البيانات الأساسية وهوية المنصة",
            F("platformName","اسم المنصة","text","مهندسيتو توريدات",true),F("nameEn","الاسم بالإنجليزية","text","Mohandseto Tawredat",true),F("officialEmail","البريد الرسمي","email","info@tawredat.eg",true),F("hotline","الخط الساخن","text","16789",true),F("timezone","المنطقة الزمنية","select","Africa/Cairo",true,["Africa/Cairo","UTC"]),F("workDays","أيام العمل","text","السبت - الخميس"),F("workHours","ساعات العمل","text","09:00 - 18:00"),F("logoUrl","رابط الشعار","url","https://tawredat.eg/logo.svg")),
        S("owner-company","بيانات الشركة المالكة","Owner Company Data","general","▦","البيانات القانونية للشركة المالكة",
            F("legalName","الاسم القانوني","text","شركة مهندسيتو للتوريدات ش.م.م",true),F("taxNumber","الرقم الضريبي","text","318-542-119",true),F("commercialRegistration","السجل التجاري","text","224180",true),F("capital","رأس المال","number","5000000",false,null,null,false,0),F("address","العنوان","textarea","القاهرة الجديدة - التجمع الأول - مبنى الإدارة",true)),
        S("tax","إعدادات الضرائب","Tax Settings","tax-currency","%","ضريبة القيمة المضافة والفاتورة الإلكترونية",
            F("vatPercent","ضريبة القيمة المضافة","number","14",true,null,null,false,0,100),F("appliesTo","تطبق على","select","كل المنتجات الخاضعة",true,["كل المنتجات الخاضعة","حسب المنتج"]),F("pricesIncludeTax","السعر يشمل الضريبة","boolean","true"),F("eInvoiceEnabled","الفاتورة الإلكترونية","boolean","true"),F("printingTaxPercent","ضريبة خدمة الطباعة","number","5",false,null,null,false,0,100)),
        S("currency","إعدادات العملة","Currency Settings","tax-currency","ج.م","تنسيق العملة الأساسية للمنصة",
            F("currency","العملة الأساسية","select","EGP",true,["EGP","USD","SAR"]),F("symbol","الرمز","text","ج.م",true),F("decimalPlaces","المنازل العشرية","number","2",true,null,null,false,0,4),F("thousandsSeparator","فاصل الآلاف","select",",",true,[",","."," "])),
        S("shipping","إعدادات الشحن","Shipping Settings","shipping","⌖","سياسة الشحن العامة",
            F("freeThreshold","حد الشحن المجاني","number","2000",true,null,null,false,0),F("methods","طرق الشحن","text","قياسي، سريع، استلام",true),F("expressFee","رسوم الشحن السريع","number","150",true,null,null,false,0),F("sameDay","التوصيل في نفس اليوم","boolean","true"),F("heavyLimitKg","حد الطلب الثقيل بالكيلو","number","200",true,null,null,false,1),F("heavyMethod","شحن الطلبات الثقيلة","select","سيارة مخصصة",true,["سيارة مخصصة","شركة شحن خارجي"])),
        S("delivery-cost","تكلفة التوصيل","Delivery Cost","shipping","₤","قواعد احتساب تكلفة التوصيل",
            F("basis","أساس الحساب","select","حسب المنطقة + الوزن",true,["حسب المنطقة + الوزن","سعر ثابت","حسب المسافة"]),F("baseFee","التكلفة الأساسية","number","75",true,null,null,false,0),F("feePerKg","تكلفة الكيلو","number","2",true,null,null,false,0),F("feePerKm","تكلفة الكيلومتر","number","1.5",true,null,null,false,0)),
        S("payment-methods","إعدادات طرق الدفع","Payment Methods","payment","▣","تفعيل وإدارة طرق الدفع المتاحة",
            F("credit","الحد الائتماني (آجل)","boolean","true"),F("bankTransfer","تحويل بنكي","boolean","true"),F("card","بطاقة - بوابة دفع","boolean","true"),F("cashOnDelivery","نقدي عند التسليم","boolean","true"),F("monthlyInvoice","فاتورة شهرية مجمعة","boolean","true"),F("installments","تقسيط","boolean","false")),
        S("invoice","إعدادات الفواتير","Invoice Settings","invoice","▤","قالب وسياسة الفواتير",
            F("template","قالب الفاتورة","select","رسمي بترويسة",true,["رسمي بترويسة","مبسط"]),F("eInvoice","الفاتورة الإلكترونية","boolean","true"),F("defaultDueDays","الاستحقاق الافتراضي بالأيام","number","30",true,null,null,false,0,365),F("showQr","إظهار QR","boolean","true"),F("footer","تذييل الفاتورة","textarea","شكرًا لتعاملكم مع مهندسيتو توريدات")),
        S("numbering","ترقيم الطلبات والفواتير","Numbering","invoice","#","أنماط ترقيم المستندات",
            F("orderPrefix","بادئة الطلب","text","ORD-",true),F("orderNext","رقم الطلب التالي","number","2491",true,null,null,false,1),F("invoicePrefix","بادئة الفاتورة","text","INV-",true),F("invoiceNext","رقم الفاتورة التالي","number","1189",true,null,null,false,1),F("quotePrefix","بادئة العرض","text","QT-",true),F("purchasePrefix","بادئة أمر الشراء","text","PO-",true)),
        S("inventory","إعدادات المخزون","Inventory Settings","inventory","▥","سياسات التقييم والحجز والتنبيه",
            F("valuation","طريقة التقييم","select","المتوسط المرجح",true,["المتوسط المرجح","FIFO","LIFO"]),F("reserveOnCart","حجز المخزون","select","عند تأكيد الطلب",true,["عند تأكيد الطلب","عند إضافة السلة"]),F("allowNegative","السماح بالمخزون السالب","boolean","false"),F("autoReorder","تنبيه إعادة الطلب","boolean","true"),F("expiryAlertDays","تنبيه الصلاحية بالأيام","number","30",true,null,null,false,1,365)),
        S("products","إعدادات المنتجات","Product Settings","products","◇","طريقة عرض المنتجات والخيارات العامة",
            F("defaultView","العرض الافتراضي","select","شبكي",true,["شبكي","قائمة"]),F("pageSize","عدد المنتجات في الصفحة","number","24",true,null,null,false,6,100),F("showStock","إظهار حالة المخزون","boolean","true"),F("allowCompare","تفعيل المقارنة","boolean","true"),F("showWarranty","إظهار الضمان","boolean","true")),
        S("printing","إعدادات الطباعة","Printing Settings","printing","✎","سياسة أعمال التصميم والطباعة",
            F("designerSlaDays","مهلة رد المصمم بالأيام","number","1",true,null,null,false,1,30),F("freeRevisions","جولات التعديل المجانية","number","2",true,null,null,false,0,20),F("sampleRequired","اعتماد العينة إلزامي","boolean","true"),F("maxUploadMb","أقصى حجم ملف بالميجابايت","number","25",true,null,null,false,1,200)),
        S("quotes","إعدادات عروض الأسعار","Quote Settings","quotes","◫","سياسة عروض الأسعار الافتراضية",
            F("validDays","مدة صلاحية العرض بالأيام","number","7",true,null,null,false,1,180),F("responseSlaHours","مهلة الرد المستهدفة بالساعات","number","24",true,null,null,false,1,720),F("defaultMargin","هامش الربح الافتراضي","number","20",true,null,null,false,0,100),F("autoExpire","إغلاق العرض تلقائيًا","boolean","true")),
        S("approvals","إعدادات الموافقات","Approval Settings","approvals","✓","سياسات الموافقات الداخلية الافتراضية",
            F("mode","نظام الموافقات","select","مرن لكل شركة",true,["مرن لكل شركة","موحد"]),F("slaHours","مهلة الموافقة بالساعات","number","24",true,null,null,false,1,720),F("escalationHours","التصعيد التلقائي بعد","number","24",true,null,null,false,1,720),F("threshold","إلزام الموافقة فوق مبلغ","number","5000",true,null,null,false,0),F("companyLevels","السماح بتخصيص المستويات","boolean","true")),
        S("contracts","إعدادات العقود","Contract Settings","contracts","▱","سياسة التجديد والتنبيهات",
            F("renewal","التجديد الافتراضي","select","تلقائي بموافقة",true,["تلقائي بموافقة","يدوي","تلقائي"]),F("expiryAlertDays","التنبيه قبل الانتهاء بالأيام","number","30",true,null,null,false,1,365),F("priceReviewMonths","مراجعة الأسعار كل شهور","number","3",true,null,null,false,1,24),F("renewalApproval","موافقة التجديد إلزامية","boolean","true")),
        S("notifications","إعدادات الإشعارات","Notification Settings","notifications","♢","قنوات الإشعارات على مستوى النظام",
            F("push","إشعارات التطبيق Push","boolean","true"),F("email","البريد الإلكتروني","boolean","true"),F("sms","SMS","boolean","true"),F("whatsapp","WhatsApp Business","boolean","true")),
        S("email","إعدادات البريد الإلكتروني","Email Settings","notifications","@","اتصال البريد الصادر SMTP",
            F("provider","مزود البريد","select","SendGrid",true,["SendGrid","Amazon SES","SMTP"]),F("fromEmail","البريد المرسل","email","noreply@tawredat.eg",true),F("fromName","اسم المرسل","text","مهندسيتو توريدات",true),F("connected","حالة الاتصال","boolean","true"),F("apiKey","مفتاح المزود","password","",false,null,"يترك فارغًا للاحتفاظ بالقيمة الحالية",true)),
        S("whatsapp","إعدادات WhatsApp","WhatsApp Settings","notifications","◉","اتصال WhatsApp Business API",
            F("businessPhone","رقم الأعمال","text","01000001678",true),F("provider","المزود","select","Twilio",true,["Twilio","Meta Cloud API"]),F("verified","حالة التوثيق","boolean","true"),F("approvedTemplates","القوالب المعتمدة","number","12",true,null,null,false,0),F("authToken","رمز المصادقة","password","",false,null,"يترك فارغًا للاحتفاظ بالقيمة الحالية",true)),
        S("sms","إعدادات SMS","SMS Settings","notifications","SMS","مزود الرسائل النصية",
            F("provider","المزود","select","Vodafone SMS",true,["Vodafone SMS","Twilio","Infobip"]),F("sender","اسم المرسل","text","Tawredat",true),F("active","الحالة","boolean","true"),F("balance","الرصيد","number","10000",false,null,null,false,0),F("apiKey","مفتاح المزود","password","",false,null,"يترك فارغًا للاحتفاظ بالقيمة الحالية",true)),
        S("payment-gateway","إعدادات بوابة الدفع","Payment Gateway","payment","▣","ربط بوابة الدفع الإلكتروني",
            F("provider","البوابة","select","Paymob",true,["Paymob","Fawry","Stripe"]),F("connected","الحالة","boolean","true"),F("cards","البطاقات المقبولة","text","Visa, Mastercard, Meeza",true),F("secure3d","3D Secure","boolean","true"),F("testMode","وضع الاختبار","boolean","false"),F("apiKey","مفتاح API","password","",false,null,"يترك فارغًا للاحتفاظ بالقيمة الحالية",true)),
        S("maps","إعدادات الخرائط","Maps Settings","shipping","⌖","خدمات الخرائط والتتبع",
            F("provider","المزود","select","Google Maps",true,["Google Maps","Mapbox"]),F("connected","الحالة","boolean","true"),F("liveTracking","التتبع الحي","boolean","true"),F("routeOptimization","تحسين المسارات","boolean","true"),F("apiKey","مفتاح API","password","",false,null,"يترك فارغًا للاحتفاظ بالقيمة الحالية",true)),
        S("backup","إعدادات النسخ الاحتياطي","Backup Settings","backup","▰","النسخ التلقائي والاحتفاظ والتشفير",
            F("frequency","التكرار","select","Daily",true,["Daily","Weekly","Disabled"]),F("time","وقت النسخ","time","02:00",true),F("retention","عدد النسخ المحتفظ بها","number","30",true,null,null,false,1,365),F("storage","التخزين","select","Local encrypted",true,["Local encrypted","S3","Azure Blob"]),F("encrypted","التشفير","boolean","true")),
        S("security","إعدادات الأمان","Security Settings","security","♢","سياسات أمان لوحة الإدارة",
            F("requireAdmin2fa","إلزام المصادقة الثنائية للأدمن","boolean","false"),F("lockAttempts","قفل الحساب بعد محاولات","number","5",true,null,null,false,3,20),F("lockoutMinutes","مدة القفل بالدقائق","number","15",true,null,null,false,1,1440),F("sessionMinutes","انتهاء الجلسة بعد خمول بالدقائق","number","30",true,null,null,false,5,1440),F("auditSensitive","تسجيل الإجراءات الحساسة","boolean","true"),F("blockSuspiciousIp","حظر IP المشبوه تلقائيًا","boolean","true")),
        S("password-policy","سياسة كلمات المرور","Password Policy","security","***","متطلبات كلمات مرور الحسابات",
            F("minLength","الحد الأدنى للطول","number","8",true,null,null,false,8,64),F("complexity","التعقيد","select","Letters + numbers + symbols",true,["Letters + numbers + symbols","Letters + numbers"]),F("expiryDays","انتهاء الصلاحية بالأيام","number","90",true,null,null,false,0,365),F("history","منع تكرار آخر كلمات","number","5",true,null,null,false,0,24)),
        S("two-factor","المصادقة الثنائية","2FA Settings","security","2FA","سياسة المصادقة الثنائية العامة",
            F("enforcement","الإلزام","select","All admin users",true,["All admin users","Privileged roles","Optional"]),F("methods","الطرق المسموحة","text","SMS + Authenticator",true),F("recoveryCodes","رموز الاسترداد لكل مستخدم","number","10",true,null,null,false,5,20)),
        S("language","إعدادات اللغة والترجمة","Language Settings","language","◎","لغات المنصة واللغة الأساسية",
            F("arabic","العربية RTL","boolean","true"),F("english","English LTR","boolean","true"),F("french","Français","boolean","false"),F("default","اللغة الأساسية","select","ar",true,["ar","en","fr"])),
        S("maintenance","وضع الصيانة","Maintenance Mode","general","!","إيقاف مؤقت لتطبيق العملاء",
            F("enabled","تفعيل وضع الصيانة","boolean","false"),F("message","رسالة الصيانة","textarea","نجري تحسينات - سنعود خلال دقائق",true),F("expectedReturn","العودة المتوقعة","time","02:45"),F("exceptions","الاستثناءات","text","مستخدمو اللوحة")),
        S("app-version","إصدار التطبيق المطلوب","Required App Version","general","↑","سياسة تحديث تطبيق العملاء",
            F("latest","أحدث إصدار","version","2.5.0",true),F("minimum","الحد الأدنى المطلوب","version","2.4.0",true),F("forceBelowMinimum","التحديث الإجباري لأقل من الحد الأدنى","boolean","true"),F("releaseNotes","ملاحظات الإصدار","textarea","واجهة محسنة، تتبع أسرع، وإصلاحات أمنية")),
        S("app-links","إدارة روابط التطبيق","App Links","general","↗","روابط النشر والويب",
            F("googlePlay","Google Play","url","https://play.google.com/store/apps/details?id=eg.tawredat",true),F("appStore","App Store","url","https://apps.apple.com/eg/app/tawredat",true),F("web","نسخة الويب","url","https://app.tawredat.eg",true),F("landing","صفحة الهبوط","url","https://tawredat.eg",true)),
    ];

    public async Task<SystemSettingsDashboardDto> DashboardAsync(CancellationToken ct = default)
    {
        var stored = await db.SystemSettings.AsNoTracking().ToListAsync(ct);
        var lookup = stored.ToDictionary(x => $"{x.Section}.{x.Key}", StringComparer.OrdinalIgnoreCase);
        var sections = Definitions.Select(section => new SettingSectionDto(section.Code, section.Ar, section.En,
            section.Category, section.Icon, section.Description,
            section.Fields.Select(x => new SettingFieldDto(x.Code, x.Label, x.Type, x.Required, x.Options ?? [], x.Help, x.Sensitive)).ToList(),
            section.Fields.ToDictionary(x => x.Code, x => Display(lookup.GetValueOrDefault($"{section.Code}.{x.Code}"), x), StringComparer.OrdinalIgnoreCase))).ToList();
        return new(sections,
            await db.DeliveryZones.AsNoTracking().OrderBy(x=>x.Governorate).ThenBy(x=>x.NameAr).Select(x=>new SettingsDeliveryZoneDto(x.Id,x.NameAr,x.Governorate,x.CitiesCsv,x.BaseFee,x.FeePerKg,x.FeePerKm,x.EstimatedDays,x.IsActive)).ToListAsync(ct),
            await db.SystemBankAccounts.AsNoTracking().OrderByDescending(x=>x.IsPrimary).ThenBy(x=>x.BankName).Select(x=>new SettingsBankAccountDto(x.Id,x.BankName,x.AccountName,x.AccountNumber,x.Iban,x.Currency,x.IsPrimary,x.IsActive)).ToListAsync(ct),
            (await db.SystemApiKeys.AsNoTracking().OrderByDescending(x=>x.CreatedAt).ToListAsync(ct)).Select(ApiKey).ToList(),
            (await db.SystemWebhooks.AsNoTracking().OrderBy(x=>x.Event).ToListAsync(ct)).Select(Webhook).ToList(),
            await db.SystemTranslations.AsNoTracking().OrderBy(x=>x.Key).Select(x=>new SettingsTranslationDto(x.Id,x.Key,x.Arabic,x.English,x.French)).ToListAsync(ct),
            await db.IntegrationOperationLogs.AsNoTracking().OrderByDescending(x=>x.StartedAt).Take(100).Select(x=>new SettingsIntegrationLogDto(x.Id,x.Integration,x.Operation,x.Reference,x.Status.ToString(),x.Attempt,x.DurationMs,x.Error,x.StartedAt,x.CompletedAt)).ToListAsync(ct),
            await db.SystemBackups.AsNoTracking().OrderByDescending(x=>x.StartedAt).Take(50).Select(x=>new SettingsBackupDto(x.Id,x.FileName,x.SizeBytes,x.Status.ToString(),x.IsAutomatic,x.StartedAt,x.CompletedAt,x.Error)).ToListAsync(ct));
    }

    public async Task<SettingSectionDto> SaveSectionAsync(Guid actor, string? ip, string code, SaveSettingsSectionDto dto, CancellationToken ct = default)
    {
        var definition = Definition(code); var unknown = dto.Values.Keys.Where(k=>definition.Fields.All(f=>!f.Code.Equals(k,StringComparison.OrdinalIgnoreCase))).ToList();
        if(unknown.Count>0) throw ApiException.BadRequest("حقول إعدادات غير معروفة: "+string.Join(',',unknown));
        var before = new Dictionary<string,string>(); var after = new Dictionary<string,string>();
        foreach(var field in definition.Fields)
        {
            dto.Values.TryGetValue(field.Code,out var submitted); submitted=submitted?.Trim();
            var existing=await db.SystemSettings.FirstOrDefaultAsync(x=>x.Section==definition.Code&&x.Key==field.Code,ct);
            before[field.Code]=existing is null?field.Default:field.Sensitive?"[protected]":existing.Value;
            if(field.Sensitive&&(string.IsNullOrWhiteSpace(submitted)||submitted=="••••••••")){after[field.Code]=before[field.Code];continue;}
            var value=Validate(field,submitted??field.Default);
            if(existing is null){existing=new SystemSetting{Section=definition.Code,Key=field.Code,CreatedBy=actor};db.SystemSettings.Add(existing);}
            existing.Value=field.Sensitive?_protector.Protect(value):value;existing.IsProtected=field.Sensitive;existing.UpdatedAt=DateTime.UtcNow;existing.UpdatedBy=actor;
            after[field.Code]=field.Sensitive?"[protected]":value;
        }
        await ApplyRuntimeAsync(definition.Code, after, actor, ct);
        if (IntegrationSections.Contains(definition.Code))
            db.IntegrationOperationLogs.Add(new IntegrationOperationLog
            {
                Integration = definition.En,
                Operation = "ConfigurationUpdated",
                Reference = definition.Code,
                Status = IntegrationOperationStatus.Succeeded,
                DurationMs = 0,
                StartedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow,
                CreatedBy = actor
            });
        Audit(actor,ip,"system_settings.updated",nameof(SystemSetting),definition.Code,new{Before=before,After=after});
        await db.SaveChangesAsync(ct);
        return (await DashboardAsync(ct)).Sections.Single(x=>x.Code==definition.Code);
    }

    public async Task<SettingsBankAccountDto> SaveBankAsync(Guid actor, string? ip, Guid? id, SaveSettingsBankAccountDto dto, CancellationToken ct=default)
    {
        if(string.IsNullOrWhiteSpace(dto.BankName)||string.IsNullOrWhiteSpace(dto.AccountName)||string.IsNullOrWhiteSpace(dto.AccountNumber)||string.IsNullOrWhiteSpace(dto.Currency)||dto.Currency.Length!=3)throw ApiException.BadRequest("بيانات الحساب البنكي غير مكتملة");
        var entity=id is null?new SystemBankAccount{CreatedBy=actor}:await db.SystemBankAccounts.FirstOrDefaultAsync(x=>x.Id==id,ct)??throw ApiException.NotFound("الحساب البنكي غير موجود");
        if(id is null)db.SystemBankAccounts.Add(entity);if(dto.IsPrimary)foreach(var other in await db.SystemBankAccounts.Where(x=>x.Id!=entity.Id&&x.IsPrimary).ToListAsync(ct))other.IsPrimary=false;
        entity.BankName=dto.BankName.Trim();entity.AccountName=dto.AccountName.Trim();entity.AccountNumber=dto.AccountNumber.Trim();entity.Iban=Clean(dto.Iban);entity.Currency=dto.Currency.Trim().ToUpperInvariant();entity.IsPrimary=dto.IsPrimary;entity.IsActive=dto.IsActive;entity.UpdatedAt=DateTime.UtcNow;entity.UpdatedBy=actor;
        Audit(actor,ip,id is null?"bank_account.created":"bank_account.updated",nameof(SystemBankAccount),entity.Id.ToString(),new{entity.BankName,entity.AccountNumber,entity.Currency,entity.IsPrimary,entity.IsActive});await db.SaveChangesAsync(ct);return Bank(entity);
    }
    public async Task DeleteBankAsync(Guid actor,string? ip,Guid id,CancellationToken ct=default){var x=await db.SystemBankAccounts.FirstOrDefaultAsync(y=>y.Id==id,ct)??throw ApiException.NotFound("الحساب البنكي غير موجود");if(x.IsPrimary)throw ApiException.Conflict("حدد حسابًا رئيسيًا آخر قبل الحذف");x.IsDeleted=true;x.DeletedAt=DateTime.UtcNow;Audit(actor,ip,"bank_account.deleted",nameof(SystemBankAccount),id.ToString(),null);await db.SaveChangesAsync(ct);}

    public async Task<SettingsDeliveryZoneDto> SaveZoneAsync(Guid actor,string? ip,Guid? id,SaveSettingsDeliveryZoneDto dto,CancellationToken ct=default)
    {
        if(string.IsNullOrWhiteSpace(dto.Name)||string.IsNullOrWhiteSpace(dto.Governorate)||dto.BaseFee<0||dto.FeePerKg<0||dto.FeePerKm<0||dto.EstimatedDays is <1 or >60)throw ApiException.BadRequest("بيانات منطقة التوصيل غير صالحة");
        var x=id is null?new DeliveryZone{CreatedBy=actor}:await db.DeliveryZones.FirstOrDefaultAsync(y=>y.Id==id,ct)??throw ApiException.NotFound("منطقة التوصيل غير موجودة");if(id is null)db.DeliveryZones.Add(x);
        x.NameAr=dto.Name.Trim();x.Governorate=dto.Governorate.Trim();x.CitiesCsv=Clean(dto.Cities);x.BaseFee=dto.BaseFee;x.FeePerKg=dto.FeePerKg;x.FeePerKm=dto.FeePerKm;x.EstimatedDays=dto.EstimatedDays;x.IsActive=dto.IsActive;x.UpdatedAt=DateTime.UtcNow;x.UpdatedBy=actor;
        Audit(actor,ip,id is null?"delivery_zone.created":"delivery_zone.updated",nameof(DeliveryZone),x.Id.ToString(),dto);await db.SaveChangesAsync(ct);return Zone(x);
    }
    public async Task DeleteZoneAsync(Guid actor,string? ip,Guid id,CancellationToken ct=default){var x=await db.DeliveryZones.FirstOrDefaultAsync(y=>y.Id==id,ct)??throw ApiException.NotFound("منطقة التوصيل غير موجودة");x.IsDeleted=true;x.DeletedAt=DateTime.UtcNow;Audit(actor,ip,"delivery_zone.deleted",nameof(DeliveryZone),id.ToString(),null);await db.SaveChangesAsync(ct);}

    public async Task<SettingsApiKeyDto> CreateApiKeyAsync(Guid actor,string? ip,CreateSettingsApiKeyDto dto,CancellationToken ct=default)
    {
        var scopes=(dto.Scopes??[]).Where(x=>!string.IsNullOrWhiteSpace(x)).Select(x=>x.Trim().ToLowerInvariant()).Where(x=>AllowedScopes.Contains(x)).Distinct().ToList();if(string.IsNullOrWhiteSpace(dto.Name)||scopes.Count==0||dto.ExpiresAt<=DateTime.UtcNow)throw ApiException.BadRequest("اسم المفتاح وصلاحياته وتاريخ انتهائه غير صالحة");
        var raw="tk_live_"+Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();var x=new SystemApiKey{Name=dto.Name.Trim(),Prefix="tk_live",LastFour=raw[^4..],KeyHash=Hash(raw),ScopesCsv=string.Join(',',scopes),ExpiresAt=dto.ExpiresAt?.ToUniversalTime(),CreatedBy=actor};db.SystemApiKeys.Add(x);Audit(actor,ip,"api_key.created",nameof(SystemApiKey),x.Id.ToString(),new{x.Name,Scopes=scopes,x.ExpiresAt});await db.SaveChangesAsync(ct);return ApiKey(x)with{Secret=raw};
    }
    public async Task RevokeApiKeyAsync(Guid actor,string? ip,Guid id,CancellationToken ct=default){var x=await db.SystemApiKeys.FirstOrDefaultAsync(y=>y.Id==id,ct)??throw ApiException.NotFound("مفتاح API غير موجود");x.RevokedAt=DateTime.UtcNow;x.UpdatedBy=actor;Audit(actor,ip,"api_key.revoked",nameof(SystemApiKey),id.ToString(),new{x.Name});await db.SaveChangesAsync(ct);}

    public async Task<SettingsWebhookDto> SaveWebhookAsync(Guid actor,string? ip,Guid? id,SaveSettingsWebhookDto dto,CancellationToken ct=default)
    {
        if(!AllowedEvents.Contains(dto.Event)||!Uri.TryCreate(dto.Url,UriKind.Absolute,out var uri)||(uri.Scheme!=Uri.UriSchemeHttps&&!uri.IsLoopback))throw ApiException.BadRequest("حدث أو رابط Webhook غير صالح");
        var raw=id is null?"whsec_"+Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant():null;var x=id is null?new SystemWebhook{CreatedBy=actor,SecretHash=Hash(raw!)}:await db.SystemWebhooks.FirstOrDefaultAsync(y=>y.Id==id,ct)??throw ApiException.NotFound("Webhook غير موجود");if(id is null)db.SystemWebhooks.Add(x);x.Event=dto.Event;x.Url=uri.ToString();x.IsActive=dto.IsActive;x.UpdatedAt=DateTime.UtcNow;x.UpdatedBy=actor;Audit(actor,ip,id is null?"webhook.created":"webhook.updated",nameof(SystemWebhook),x.Id.ToString(),new{x.Event,x.Url,x.IsActive});await db.SaveChangesAsync(ct);return Webhook(x)with{Secret=raw};
    }
    public async Task DeleteWebhookAsync(Guid actor,string? ip,Guid id,CancellationToken ct=default){var x=await db.SystemWebhooks.FirstOrDefaultAsync(y=>y.Id==id,ct)??throw ApiException.NotFound("Webhook غير موجود");x.IsDeleted=true;x.DeletedAt=DateTime.UtcNow;Audit(actor,ip,"webhook.deleted",nameof(SystemWebhook),id.ToString(),null);await db.SaveChangesAsync(ct);}

    public async Task<SettingsTranslationDto> SaveTranslationAsync(Guid actor,string? ip,SaveSettingsTranslationDto dto,CancellationToken ct=default)
    {
        if(string.IsNullOrWhiteSpace(dto.Key)||!dto.Key.All(x=>char.IsLetterOrDigit(x)||x is '.' or '_' or '-')||string.IsNullOrWhiteSpace(dto.Arabic)||string.IsNullOrWhiteSpace(dto.English))throw ApiException.BadRequest("مفتاح ونصوص الترجمة غير صالحة");
        var normalizedKey=dto.Key.Trim();if(await db.SystemTranslations.AnyAsync(y=>y.Key==normalizedKey&&y.Id!=dto.Id,ct))throw ApiException.Conflict("مفتاح الترجمة مستخدم بالفعل");var x=dto.Id is null?new SystemTranslation{CreatedBy=actor}:await db.SystemTranslations.FirstOrDefaultAsync(y=>y.Id==dto.Id,ct)??throw ApiException.NotFound("الترجمة غير موجودة");if(dto.Id is null)db.SystemTranslations.Add(x);x.Key=normalizedKey;x.Arabic=dto.Arabic.Trim();x.English=dto.English.Trim();x.French=Clean(dto.French);x.UpdatedAt=DateTime.UtcNow;x.UpdatedBy=actor;Audit(actor,ip,dto.Id is null?"translation.created":"translation.updated",nameof(SystemTranslation),x.Id.ToString(),new{x.Key});await db.SaveChangesAsync(ct);return new(x.Id,x.Key,x.Arabic,x.English,x.French);
    }

    public async Task<SettingsBackupDto> CreateBackupAsync(Guid actor,string? ip,bool automatic=false,CancellationToken ct=default)
    {
        var backup=new SystemBackup{FileName=$"mohandseto-{DateTime.UtcNow:yyyyMMdd-HHmmss}.db",Status=SystemBackupStatus.Processing,IsAutomatic=automatic,CreatedBy=actor==Guid.Empty?null:actor};db.SystemBackups.Add(backup);await db.SaveChangesAsync(ct);
        try
        {
            var dir=Path.Combine(environment.ContentRootPath,"storage","backups");Directory.CreateDirectory(dir);var path=Path.Combine(dir,backup.FileName);var source=db.Database.GetDbConnection() as SqliteConnection??throw new InvalidOperationException("النسخ المحلي متاح لقاعدة SQLite فقط");var opened=source.State!=ConnectionState.Open;if(opened)await source.OpenAsync(ct);await using(var target=new SqliteConnection($"Data Source={path};Pooling=False")){await target.OpenAsync(ct);source.BackupDatabase(target);}if(opened)await source.CloseAsync();backup.StoragePath=path;backup.SizeBytes=new FileInfo(path).Length;await using(var file=File.OpenRead(path))backup.Sha256=Convert.ToHexString(await SHA256.HashDataAsync(file,ct)).ToLowerInvariant();backup.Status=SystemBackupStatus.Completed;backup.CompletedAt=DateTime.UtcNow;await EnforceBackupRetentionAsync(dir,ct);
        }
        catch(Exception ex){backup.Status=SystemBackupStatus.Failed;backup.Error=ex.Message.Length>500?ex.Message[..500]:ex.Message;backup.CompletedAt=DateTime.UtcNow;}
        Audit(actor==Guid.Empty?null:actor,ip,"system_backup.created",nameof(SystemBackup),backup.Id.ToString(),new{backup.FileName,backup.Status,backup.SizeBytes,automatic});await db.SaveChangesAsync(ct);return Backup(backup);
    }

    public async Task<string> ValueAsync(string section,string key,string fallback,CancellationToken ct=default)
    {var item=await db.SystemSettings.AsNoTracking().FirstOrDefaultAsync(x=>x.Section==section&&x.Key==key,ct);if(item is null)return fallback;return item.IsProtected?_protector.Unprotect(item.Value):item.Value;}
    public async Task<int> IntAsync(string section,string key,int fallback,CancellationToken ct=default)=>int.TryParse(await ValueAsync(section,key,fallback.ToString(CultureInfo.InvariantCulture),ct),out var n)?n:fallback;
    public async Task<bool> BoolAsync(string section,string key,bool fallback,CancellationToken ct=default)=>bool.TryParse(await ValueAsync(section,key,fallback.ToString(),ct),out var b)?b:fallback;

    private async Task ApplyRuntimeAsync(string section,IReadOnlyDictionary<string,string> values,Guid actor,CancellationToken ct)
    {
        if(section is not("maintenance" or "app-version" or "app-links"))return;var config=await db.MobileAppConfigs.FirstOrDefaultAsync(x=>x.Platform=="all",ct);if(config is null){config=new MobileAppConfig{Platform="all",CreatedBy=actor};db.MobileAppConfigs.Add(config);}
        if(section=="maintenance"){config.MaintenanceEnabled=Bool(values,"enabled");config.MessageAr=values.GetValueOrDefault("message");}
        if(section=="app-version"){config.LatestVersion=values.GetValueOrDefault("latest")??config.LatestVersion;config.MinimumVersion=values.GetValueOrDefault("minimum")??config.MinimumVersion;config.MessageAr=values.GetValueOrDefault("releaseNotes")??config.MessageAr;}
        if(section=="app-links")config.UpdateUrl=values.GetValueOrDefault("googlePlay")??config.UpdateUrl;
        config.UpdatedAt=DateTime.UtcNow;config.UpdatedBy=actor;
    }
    private async Task EnforceBackupRetentionAsync(string directory,CancellationToken ct)
    {
        var retention=await IntAsync("backup","retention",30,ct);
        var expired=await db.SystemBackups.Where(x=>x.Status==SystemBackupStatus.Completed)
            .OrderByDescending(x=>x.StartedAt).Skip(Math.Max(retention-1,0)).ToListAsync(ct);
        var root=Path.GetFullPath(directory)+Path.DirectorySeparatorChar;
        foreach(var item in expired)
        {
            var path=Path.GetFullPath(item.StoragePath);
            if(path.StartsWith(root,StringComparison.OrdinalIgnoreCase)&&File.Exists(path))File.Delete(path);
            item.IsDeleted=true;item.DeletedAt=DateTime.UtcNow;
        }
    }
    private string Display(SystemSetting? value,Field field)=>value is null?field.Default:value.IsProtected?"••••••••":value.Value;
    private static string Validate(Field f,string value)
    {
        if(f.Required&&string.IsNullOrWhiteSpace(value))throw ApiException.BadRequest($"{f.Label} مطلوب");if(value.Length>4000)throw ApiException.BadRequest($"{f.Label} أطول من المسموح");
        if(f.Type=="boolean"&&!bool.TryParse(value,out _))throw ApiException.BadRequest($"{f.Label} يجب أن يكون نعم أو لا");
        if(f.Type=="number"){if(!decimal.TryParse(value,NumberStyles.Any,CultureInfo.InvariantCulture,out var number)||number<f.Min||number>f.Max)throw ApiException.BadRequest($"{f.Label} خارج النطاق المسموح");value=number.ToString(CultureInfo.InvariantCulture);}
        if(f.Type=="email"&&!ValidEmail(value))throw ApiException.BadRequest($"{f.Label} غير صالح");
        if(f.Type=="url"&&(!Uri.TryCreate(value,UriKind.Absolute,out var uri)||uri.Scheme is not("http" or "https")))throw ApiException.BadRequest($"{f.Label} غير صالح");
        if(f.Type=="time"&&!TimeOnly.TryParse(value,CultureInfo.InvariantCulture,out _))throw ApiException.BadRequest($"{f.Label} غير صالح");
        if(f.Type=="version"&&!Version.TryParse(value.TrimStart('v','V'),out _))throw ApiException.BadRequest($"{f.Label} غير صالح");
        if(f.Options is {Length:>0}&&!f.Options.Contains(value,StringComparer.OrdinalIgnoreCase))throw ApiException.BadRequest($"{f.Label} غير صالح");return value;
    }
    private static Section Definition(string code)=>Definitions.FirstOrDefault(x=>x.Code.Equals(code,StringComparison.OrdinalIgnoreCase))??throw ApiException.NotFound("قسم الإعدادات غير موجود");
    private static Section S(string code,string ar,string en,string category,string icon,string description,params Field[] fields)=>new(code,ar,en,category,icon,description,fields);
    private static Field F(string code,string label,string type,string value,bool required=false,string[]? options=null,string? help=null,bool sensitive=false,decimal? min=null,decimal? max=null)=>new(code,label,type,value,required,options,help,sensitive,min,max);
    private static readonly HashSet<string> AllowedScopes=["orders.read","orders.write","catalog.read","inventory.read","reports.read","webhooks.manage"];
    private static readonly HashSet<string> AllowedEvents=["order.created","order.updated","invoice.issued","payment.completed","shipment.updated","product.updated"];
    private static readonly HashSet<string> IntegrationSections=["email","whatsapp","sms","payment-gateway","maps"];
    private static bool ValidEmail(string value)=>value.Contains('@')&&value.LastIndexOf('.')>value.IndexOf('@')+1;
    private static string Hash(string value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private static string? Clean(string? value)=>string.IsNullOrWhiteSpace(value)?null:value.Trim();
    private static bool Bool(IReadOnlyDictionary<string,string> values,string key)=>bool.TryParse(values.GetValueOrDefault(key),out var b)&&b;
    private static List<string> Split(string value)=>value.Split(',',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries).ToList();
    private static SettingsApiKeyDto ApiKey(SystemApiKey x)=>new(x.Id,x.Name,$"{x.Prefix}_••••{x.LastFour}",Split(x.ScopesCsv),x.IsActive,x.ExpiresAt,x.LastUsedAt,x.CreatedAt);
    private static SettingsWebhookDto Webhook(SystemWebhook x)=>new(x.Id,x.Event,x.Url,x.IsActive,x.LastTriggeredAt,x.FailureCount,x.CreatedAt);
    private static SettingsBankAccountDto Bank(SystemBankAccount x)=>new(x.Id,x.BankName,x.AccountName,x.AccountNumber,x.Iban,x.Currency,x.IsPrimary,x.IsActive);
    private static SettingsDeliveryZoneDto Zone(DeliveryZone x)=>new(x.Id,x.NameAr,x.Governorate,x.CitiesCsv,x.BaseFee,x.FeePerKg,x.FeePerKm,x.EstimatedDays,x.IsActive);
    private static SettingsBackupDto Backup(SystemBackup x)=>new(x.Id,x.FileName,x.SizeBytes,x.Status.ToString(),x.IsAutomatic,x.StartedAt,x.CompletedAt,x.Error);
    private void Audit(Guid? actor,string? ip,string action,string entity,string? id,object? data)=>db.AuditLogs.Add(new AuditLog{UserId=actor,Ip=ip,Action=action,EntityType=entity,EntityId=id,DataJson=data is null?null:JsonSerializer.Serialize(data)});
}

public sealed class ScheduledSystemBackupWorker(IServiceScopeFactory scopes,ILogger<ScheduledSystemBackupWorker> logger):BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while(!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope=scopes.CreateScope();var service=scope.ServiceProvider.GetRequiredService<AdminSystemSettingsService>();var frequency=await service.ValueAsync("backup","frequency","Disabled",stoppingToken);var timeText=await service.ValueAsync("backup","time","02:00",stoppingToken);
                if(!frequency.Equals("Disabled",StringComparison.OrdinalIgnoreCase)&&TimeOnly.TryParse(timeText,out var time))
                {var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();var now=DateTime.UtcNow;var due=now.TimeOfDay>=time.ToTimeSpan()&&!await db.SystemBackups.AnyAsync(x=>x.IsAutomatic&&x.StartedAt>=now.Date,stoppingToken);if(due)await service.CreateBackupAsync(Guid.Empty,null,true,stoppingToken);}
            }
            catch(OperationCanceledException)when(stoppingToken.IsCancellationRequested){break;}catch(Exception ex){logger.LogError(ex,"Scheduled system backup failed");}
            await Task.Delay(TimeSpan.FromMinutes(30),stoppingToken);
        }
    }
}
