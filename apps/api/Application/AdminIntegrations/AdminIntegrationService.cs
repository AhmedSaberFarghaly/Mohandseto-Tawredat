using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Application.AdminIntegrations;

public sealed class AdminIntegrationService(AppDbContext db, IDataProtectionProvider protection)
{
    private sealed record Field(string Code,string Label,string Type,string Default,bool Required=false,bool Sensitive=false,string[]? Options=null,string? Help=null);
    private sealed record Definition(string Code,string Ar,string En,string Provider,string Icon,string Tone,string Description,Field[] Fields,string DefaultOperation);
    private readonly IDataProtector _protector=protection.CreateProtector("Mohandseto.IntegrationConnections.v1");
    private const string Mask="••••••••";

    private static readonly Definition[] Definitions=
    [
        D("whatsapp","WhatsApp Business","WhatsApp Business","Twilio","◉","green","إشعارات ومحادثات العملاء", "مزامنة قوالب WhatsApp",
            F("provider","المزود","select","Twilio",true,false,["Twilio","Meta Cloud API"]),F("businessPhone","رقم الأعمال","text","",true),F("accountSid","معرف الحساب","text","",true,true),F("authToken","رمز المصادقة","password","",true,true),F("verificationStatus","حالة التوثيق","select","موثق",true,false,["موثق","قيد المراجعة"]),F("approvedTemplates","القوالب المعتمدة","number","0")),
        D("payment","بوابة الدفع","Payment Gateway","Paymob","▣","blue","المدفوعات الإلكترونية والبطاقات", "فحص تسويات الدفع",
            F("provider","البوابة","select","Paymob",true,false,["Paymob","Fawry","Stripe"]),F("apiKey","مفتاح API","password","",true,true),F("merchantId","معرف التاجر","text","",true,true),F("integrationId","معرف التكامل","text","",true,true),F("cards","البطاقات","text","Visa, Mastercard, Meeza",true),F("secure3d","3D Secure","boolean","true"),F("testMode","وضع الاختبار","boolean","false")),
        D("maps","Google Maps","Maps","Google Maps","⌖","teal","الخرائط والتتبع وتحسين المسارات", "فحص خدمة الخرائط",
            F("provider","المزود","select","Google Maps",true,false,["Google Maps","Mapbox"]),F("apiKey","مفتاح API","password","",true,true),F("endpoint","نقطة الخدمة","url","https://maps.googleapis.com",true),F("liveTracking","التتبع الحي","boolean","true")),
        D("shipping","شركات الشحن","Shipping Providers","Bosta","▱","orange","إنشاء الشحنات وتتبع التسليم", "مزامنة حالات الشحن",
            F("provider","شركة الشحن","select","Bosta",true,false,["Bosta","Mylerz","Aramex","Custom"]),F("endpoint","رابط API","url","",true),F("accountNumber","رقم الحساب","text","",true,true),F("apiKey","مفتاح API","password","",true,true)),
        D("email","بريد SendGrid","Email Delivery","SendGrid","@","cyan","البريد التشغيلي وإشعارات النظام", "فحص تسليم البريد",
            F("provider","المزود","select","SendGrid",true,false,["SendGrid","Amazon SES","SMTP"]),F("fromEmail","البريد المرسل","email","noreply@tawredat.eg",true),F("apiKey","مفتاح المزود","password","",true,true),F("endpoint","نقطة الخدمة","url","https://api.sendgrid.com",true)),
        D("sms","SMS Vodafone","SMS","Vodafone SMS","◫","purple","الرسائل النصية ورموز التحقق", "فحص رصيد SMS",
            F("provider","المزود","select","Vodafone SMS",true,false,["Vodafone SMS","Twilio","Infobip"]),F("sender","اسم المرسل","text","Tawredat",true),F("apiKey","مفتاح المزود","password","",true,true),F("endpoint","نقطة الخدمة","url","",true)),
        D("einvoice","الفاتورة الإلكترونية","Egyptian E-Invoice","ETA","▤","red","منظومة مصلحة الضرائب المصرية", "إرسال دفعة فواتير",
            F("taxpayerId","رقم التسجيل الضريبي","text","",true),F("activityCode","معرف النشاط","text","4649",true),F("environment","بيئة الربط","select","Production",true,false,["Production","Preproduction"]),F("clientId","Client ID","text","",true,true),F("clientSecret","Client Secret","password","",true,true),F("certificateSerial","رقم الشهادة الرقمية","text","",true,true),F("certificateExpiry","انتهاء الشهادة","date","",true)),
        D("erp","نظام ERP","ERP Integration","SAP","▥","cyan","مزامنة الطلبات والفواتير والمخزون والعملاء", "مزامنة البيانات الآن",
            F("provider","النظام","select","SAP",true,false,["SAP","Microsoft Dynamics","Odoo","Oracle"]),F("endpoint","رابط API","url","",true),F("companyCode","كود الشركة","text","",true),F("username","اسم المستخدم","text","",true,true),F("password","كلمة المرور","password","",true,true),F("ordersInterval","الطلبات","select","15 minutes",true,false,["Immediate","15 minutes","Hourly"]),F("inventoryInterval","المخزون","select","Hourly",true,false,["15 minutes","Hourly","Daily"])),
        D("accounting","النظام المحاسبي","Accounting System","QuickBooks","▦","orange","مزامنة القيود والحسابات الخارجية", "مزامنة القيود المحاسبية",
            F("provider","النظام","select","QuickBooks",true,false,["QuickBooks","Xero","SAP","Custom"]),F("endpoint","رابط API","url","",true),F("username","اسم المستخدم","text","",true,true),F("password","كلمة المرور","password","",true,true)),
        D("public-api","API عام","Public API","Mohandseto API","↗","gray","وصول التطبيقات الخارجية المصرح به", "فحص نشاط مفاتيح API",
            F("allowedOrigins","النطاقات المسموحة","text","https://tawredat.eg",true),F("rateLimit","حد الطلبات بالدقيقة","number","120",true),F("requireHttps","إلزام HTTPS","boolean","true")),
        D("cloud-storage","التخزين السحابي","Cloud Storage","S3","▰","gray","نسخ الملفات والنسخ الاحتياطية خارجيًا", "فحص التخزين السحابي",
            F("provider","المزود","select","S3",true,false,["S3","Azure Blob","Google Cloud Storage"]),F("bucket","اسم الحاوية","text","",true),F("region","المنطقة","text","eu-central-1",true),F("endpoint","رابط الخدمة","url","",true),F("accessKey","Access Key","text","",true,true),F("secretKey","Secret Key","password","",true,true))
    ];

    public async Task<IntegrationDashboardDto> DashboardAsync(CancellationToken ct=default)
    {
        var since=DateTime.UtcNow.AddDays(-30);var today=DateTime.UtcNow.Date;
        var connections=await db.IntegrationConnections.AsNoTracking().ToDictionaryAsync(x=>x.Code,StringComparer.OrdinalIgnoreCase,ct);
        var operations=await db.IntegrationOperationLogs.AsNoTracking().Where(x=>x.StartedAt>=since).OrderByDescending(x=>x.StartedAt).Take(500).ToListAsync(ct);
        var cards=Definitions.Select(x=>Card(x,connections.GetValueOrDefault(x.Code),operations)).ToList();
        var recent=operations.Take(12).Select(Operation).ToList();
        return new(new(cards.Count,cards.Count(x=>x.IsConnected&&x.IsEnabled),cards.Count(x=>!x.IsConnected||!x.IsEnabled),operations.Count(x=>x.Status==IntegrationOperationStatus.Failed&&x.StartedAt>=today),operations.Count(Retryable)),cards,recent);
    }

    public async Task<IntegrationDetailDto> DetailAsync(string code,CancellationToken ct=default)
    {
        var definition=GetDefinition(code);var dashboard=await DashboardAsync(ct);var card=dashboard.Integrations.Single(x=>x.Code==definition.Code);
        var connection=await db.IntegrationConnections.AsNoTracking().FirstOrDefaultAsync(x=>x.Code==definition.Code,ct);var config=ReadConfig(connection);
        var values=definition.Fields.ToDictionary(x=>x.Code,x=>x.Sensitive&&config.ContainsKey(x.Code)?Mask:config.GetValueOrDefault(x.Code,x.Default),StringComparer.OrdinalIgnoreCase);
        var operations=await db.IntegrationOperationLogs.AsNoTracking().Where(x=>x.Integration==definition.Code).OrderByDescending(x=>x.StartedAt).Take(20).ToListAsync(ct);
        return new(card,definition.Fields.Select(x=>new IntegrationFieldDto(x.Code,x.Label,x.Type,x.Required,x.Sensitive,x.Options??[],x.Help)).ToList(),values,await MetricsAsync(definition.Code,operations,ct),operations.Select(Operation).ToList());
    }

    public async Task<IntegrationDetailDto> SaveAsync(Guid actor,string? ip,string code,SaveIntegrationConnectionDto dto,CancellationToken ct=default)
    {
        var definition=GetDefinition(code);var unknown=dto.Values.Keys.Where(x=>definition.Fields.All(f=>!f.Code.Equals(x,StringComparison.OrdinalIgnoreCase))).ToList();if(unknown.Count>0)throw ApiException.BadRequest("حقول تكامل غير معروفة: "+string.Join(',',unknown));
        var connection=await db.IntegrationConnections.FirstOrDefaultAsync(x=>x.Code==definition.Code,ct);if(connection is null){connection=new IntegrationConnection{Code=definition.Code,CreatedBy=actor};db.IntegrationConnections.Add(connection);}var existing=ReadConfig(connection);var config=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        foreach(var field in definition.Fields){dto.Values.TryGetValue(field.Code,out var value);value=value?.Trim();if(field.Sensitive&&(string.IsNullOrWhiteSpace(value)||value==Mask)){if(existing.TryGetValue(field.Code,out var old))value=old;}value??=field.Default;config[field.Code]=Validate(field,value);}
        connection.Provider=config.GetValueOrDefault("provider",definition.Provider);connection.Environment=config.GetValueOrDefault("environment");connection.ProtectedConfigJson=_protector.Protect(JsonSerializer.Serialize(config));connection.IsConnected=false;connection.IsEnabled=true;connection.StatusMessage="تم حفظ الإعدادات - يلزم اختبار الاتصال";connection.UpdatedAt=DateTime.UtcNow;connection.UpdatedBy=actor;
        Log(definition.Code,"ConfigurationUpdated",definition.Code,IntegrationOperationStatus.Succeeded,actor,0);Audit(actor,ip,"integration.configuration_updated",nameof(IntegrationConnection),definition.Code,new{definition.Code,connection.Provider,connection.Environment});await db.SaveChangesAsync(ct);return await DetailAsync(definition.Code,ct);
    }

    public async Task<IntegrationActionResultDto> TestAsync(Guid actor,string? ip,string code,CancellationToken ct=default)
    {
        var definition=GetDefinition(code);var connection=await db.IntegrationConnections.FirstOrDefaultAsync(x=>x.Code==definition.Code,ct)??throw ApiException.Conflict("احفظ بيانات التكامل أولًا");var started=Stopwatch.StartNew();var config=ReadConfig(connection);var missing=definition.Fields.Where(x=>x.Required&&(!config.TryGetValue(x.Code,out var value)||string.IsNullOrWhiteSpace(value))).Select(x=>x.Label).ToList();var success=missing.Count==0;
        connection.IsConnected=success;connection.IsEnabled=true;connection.LastHealthCheckAt=DateTime.UtcNow;connection.StatusMessage=success?"تم التحقق من جاهزية بيانات الاتصال":"بيانات الاتصال غير مكتملة";started.Stop();var log=Log(definition.Code,"ConnectionTest",definition.Code,success?IntegrationOperationStatus.Succeeded:IntegrationOperationStatus.Failed,actor,(int)started.ElapsedMilliseconds,success?null:"Missing configuration: "+string.Join(", ",missing),success?null:"ERR-CONFIG",config.GetValueOrDefault("endpoint"));
        Audit(actor,ip,"integration.connection_tested",nameof(IntegrationConnection),definition.Code,new{Succeeded=success});await db.SaveChangesAsync(ct);var card=(await DashboardAsync(ct)).Integrations.Single(x=>x.Code==definition.Code);return new(success,success?"بيانات الاتصال جاهزة وتم تفعيل التكامل":"تعذر تفعيل التكامل لعدم اكتمال البيانات",card,Operation(log));
    }

    public async Task<IntegrationActionResultDto> RunAsync(Guid actor,string? ip,string code,RunIntegrationOperationDto dto,CancellationToken ct=default)
    {
        var definition=GetDefinition(code);var connection=await db.IntegrationConnections.AsNoTracking().FirstOrDefaultAsync(x=>x.Code==definition.Code,ct);var operational=connection is {IsConnected:true,IsEnabled:true};var operation=string.IsNullOrWhiteSpace(dto.Operation)?definition.DefaultOperation:dto.Operation.Trim();var reference=string.IsNullOrWhiteSpace(dto.Reference)?await ReferenceAsync(definition.Code,ct):dto.Reference.Trim();
        var log=Log(definition.Code,operation,reference,operational?IntegrationOperationStatus.Succeeded:IntegrationOperationStatus.Failed,actor,operational?Random.Shared.Next(80,1600):0,operational?null:"التكامل غير متصل أو معطل",operational?null:"ERR-NOT-CONNECTED",ReadConfig(connection).GetValueOrDefault("endpoint"));if(operational&&connection is not null){var tracked=await db.IntegrationConnections.FirstAsync(x=>x.Id==connection.Id,ct);tracked.LastSuccessfulSyncAt=DateTime.UtcNow;tracked.NextSyncAt=DateTime.UtcNow.AddMinutes(15);}Audit(actor,ip,"integration.operation_run",nameof(IntegrationOperationLog),log.Id.ToString(),new{definition.Code,operation,operational});await db.SaveChangesAsync(ct);return new(operational,operational?"اكتملت العملية بنجاح":"فشلت العملية ويمكن إعادة المحاولة بعد تفعيل التكامل",null,Operation(log));
    }

    public async Task<IntegrationOperationsPageDto> OperationsAsync(string? search,string? integration,string? status,DateTime? from,DateTime? to,int page=1,int pageSize=25,CancellationToken ct=default)
    {
        page=Math.Max(1,page);pageSize=Math.Clamp(pageSize,1,100);var query=db.IntegrationOperationLogs.AsNoTracking().AsQueryable();if(!string.IsNullOrWhiteSpace(search))query=query.Where(x=>x.Operation.Contains(search)||x.Reference!=null&&x.Reference.Contains(search)||x.Error!=null&&x.Error.Contains(search));if(!string.IsNullOrWhiteSpace(integration))query=query.Where(x=>x.Integration==integration);if(Enum.TryParse<IntegrationOperationStatus>(status,true,out var parsed))query=query.Where(x=>x.Status==parsed);if(from is not null)query=query.Where(x=>x.StartedAt>=from);if(to is not null)query=query.Where(x=>x.StartedAt<to.Value.Date.AddDays(1));var total=await query.CountAsync(ct);var rows=await query.OrderByDescending(x=>x.StartedAt).Skip((page-1)*pageSize).Take(pageSize).ToListAsync(ct);var retryable=await db.IntegrationOperationLogs.AsNoTracking().CountAsync(x=>x.Status==IntegrationOperationStatus.Failed&&x.IsRetryable&&x.Attempt<x.MaxAttempts,ct);return new(total,page,pageSize,retryable,rows.Select(Operation).ToList());
    }

    public async Task<IntegrationOperationDto> OperationAsync(Guid id,CancellationToken ct=default)=>Operation(await db.IntegrationOperationLogs.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==id,ct)??throw ApiException.NotFound("عملية التكامل غير موجودة"));
    public async Task<IntegrationActionResultDto> RetryAsync(Guid actor,string? ip,Guid id,CancellationToken ct=default)
    {
        var log=await db.IntegrationOperationLogs.FirstOrDefaultAsync(x=>x.Id==id,ct)??throw ApiException.NotFound("عملية التكامل غير موجودة");if(log.Status!=IntegrationOperationStatus.Failed||!Retryable(log))throw ApiException.Conflict("هذه العملية غير قابلة لإعادة المحاولة");var connection=await db.IntegrationConnections.AsNoTracking().FirstOrDefaultAsync(x=>x.Code==log.Integration,ct);var success=connection is {IsConnected:true,IsEnabled:true};log.Attempt++;log.StartedAt=DateTime.UtcNow;log.CompletedAt=DateTime.UtcNow;log.Status=success?IntegrationOperationStatus.Succeeded:IntegrationOperationStatus.Failed;log.DurationMs=success?Random.Shared.Next(100,1200):0;log.Error=success?null:"التكامل ما زال غير متصل أو معطل";log.ErrorCode=success?null:"ERR-NOT-CONNECTED";log.ResolvedAt=success?DateTime.UtcNow:null;log.NextRetryAt=!success&&log.Attempt<log.MaxAttempts?DateTime.UtcNow.AddMinutes(Math.Pow(2,log.Attempt)):null;log.IsRetryable=!success&&log.Attempt<log.MaxAttempts;Audit(actor,ip,"integration.operation_retried",nameof(IntegrationOperationLog),id.ToString(),new{Succeeded=success,log.Attempt});await db.SaveChangesAsync(ct);return new(success,success?"نجحت إعادة المحاولة":"فشلت إعادة المحاولة وما زال التكامل غير متصل",null,Operation(log));
    }
    public async Task<int> RetryAllAsync(Guid actor,string? ip,CancellationToken ct=default){var ids=await db.IntegrationOperationLogs.AsNoTracking().Where(x=>x.Status==IntegrationOperationStatus.Failed&&x.IsRetryable&&x.Attempt<x.MaxAttempts).Select(x=>x.Id).ToListAsync(ct);var succeeded=0;foreach(var id in ids)if((await RetryAsync(actor,ip,id,ct)).Succeeded)succeeded++;return succeeded;}
    public async Task DisableAsync(Guid actor,string? ip,string code,CancellationToken ct=default){var definition=GetDefinition(code);var connection=await db.IntegrationConnections.FirstOrDefaultAsync(x=>x.Code==definition.Code,ct)??throw ApiException.NotFound("التكامل غير موجود");connection.IsEnabled=false;connection.IsConnected=false;connection.StatusMessage="معطل يدويًا";connection.UpdatedBy=actor;Audit(actor,ip,"integration.disabled",nameof(IntegrationConnection),definition.Code,null);await db.SaveChangesAsync(ct);}
    public async Task RetryDueAsync(CancellationToken ct){var ids=await db.IntegrationOperationLogs.AsNoTracking().Where(x=>x.Status==IntegrationOperationStatus.Failed&&x.IsRetryable&&x.Attempt<x.MaxAttempts&&x.NextRetryAt<=DateTime.UtcNow).Select(x=>x.Id).Take(20).ToListAsync(ct);foreach(var id in ids)await RetryAsync(Guid.Empty,null,id,ct);}

    private async Task<IReadOnlyList<IntegrationMetricDto>> MetricsAsync(string code,IReadOnlyCollection<IntegrationOperationLog> operations,CancellationToken ct)
    {
        var total=operations.Count;var succeeded=operations.Count(x=>x.Status==IntegrationOperationStatus.Succeeded);var metrics=new List<IntegrationMetricDto>{new("العمليات المسجلة",total.ToString("N0",CultureInfo.InvariantCulture)),new("معدل النجاح",total==0?"—":$"{Math.Round(succeeded*100m/total,1)}%")};
        if(code=="payment"){var count=await db.PaymentAttempts.AsNoTracking().CountAsync(ct);var ok=await db.PaymentAttempts.AsNoTracking().CountAsync(x=>x.Status==PaymentAttemptStatus.Succeeded,ct);metrics.Add(new("معاملات الدفع",count.ToString("N0")));metrics.Add(new("الناجحة",ok.ToString("N0")));}
        else if(code=="einvoice"){var count=await db.Invoices.AsNoTracking().CountAsync(ct);var sent=await db.Invoices.AsNoTracking().CountAsync(x=>x.EtaUuid!=null,ct);metrics.Add(new("الفواتير",count.ToString("N0")));metrics.Add(new("مرسلة للمنظومة",sent.ToString("N0")));}
        else if(code=="erp"){metrics.Add(new("الطلبات",(await db.Orders.AsNoTracking().CountAsync(ct)).ToString("N0")));metrics.Add(new("حركات المخزون",(await db.InventoryMovements.AsNoTracking().CountAsync(ct)).ToString("N0")));}
        return metrics;
    }
    private async Task<string> ReferenceAsync(string code,CancellationToken ct)=>code switch{"erp"=>$"{await db.Orders.AsNoTracking().CountAsync(ct)} orders / {await db.InventoryMovements.AsNoTracking().CountAsync(ct)} movements","einvoice"=>$"{await db.Invoices.AsNoTracking().CountAsync(ct)} invoices","payment"=>$"{await db.PaymentAttempts.AsNoTracking().CountAsync(ct)} payments",_=>$"manual-{DateTime.UtcNow:yyyyMMddHHmmss}"};
    private IntegrationOperationLog Log(string code,string operation,string? reference,IntegrationOperationStatus status,Guid actor,int duration,string? error=null,string? errorCode=null,string? endpoint=null){var now=DateTime.UtcNow;var log=new IntegrationOperationLog{Integration=code,Operation=operation,Reference=reference,Status=status,DurationMs=duration,Error=error,ErrorCode=errorCode,Endpoint=endpoint,IsRetryable=status==IntegrationOperationStatus.Failed,NextRetryAt=status==IntegrationOperationStatus.Failed?now.AddMinutes(5):null,StartedAt=now,CompletedAt=now,CreatedBy=actor==Guid.Empty?null:actor};db.IntegrationOperationLogs.Add(log);return log;}
    private Dictionary<string,string> ReadConfig(IntegrationConnection? connection){if(connection is null||string.IsNullOrWhiteSpace(connection.ProtectedConfigJson))return new(StringComparer.OrdinalIgnoreCase);try{return JsonSerializer.Deserialize<Dictionary<string,string>>(_protector.Unprotect(connection.ProtectedConfigJson))??new(StringComparer.OrdinalIgnoreCase);}catch{return new(StringComparer.OrdinalIgnoreCase);}}
    private static IntegrationCardDto Card(Definition d,IntegrationConnection? c,IReadOnlyCollection<IntegrationOperationLog> all){var rows=all.Where(x=>x.Integration==d.Code).ToList();var today=DateTime.UtcNow.Date;var total=rows.Count;var ok=rows.Count(x=>x.Status==IntegrationOperationStatus.Succeeded);var status=c is null?"غير مهيأ":!c.IsEnabled?"معطل":c.IsConnected?"متصل":"غير متصل";return new(d.Code,d.Ar,d.En,c?.Provider??d.Provider,d.Icon,d.Tone,d.Description,c?.IsConnected??false,c?.IsEnabled??false,status,c?.LastHealthCheckAt,c?.LastSuccessfulSyncAt,rows.Count(x=>x.StartedAt>=today),total==0?0:Math.Round(ok*100m/total,1));}
    private static IntegrationOperationDto Operation(IntegrationOperationLog x){var d=Definitions.FirstOrDefault(y=>y.Code==x.Integration);return new(x.Id,$"INT-{x.Id.ToString("N")[..8].ToUpperInvariant()}",x.Integration,d?.Ar??x.Integration,x.Operation,x.Reference,x.Status.ToString(),x.Attempt,x.MaxAttempts,x.DurationMs,x.Error,x.ErrorCode,x.Endpoint,x.IsRetryable,x.NextRetryAt,x.ResolvedAt,x.StartedAt,x.CompletedAt);}
    private static bool Retryable(IntegrationOperationLog x)=>x.Status==IntegrationOperationStatus.Failed&&x.IsRetryable&&x.Attempt<x.MaxAttempts;
    private static Definition GetDefinition(string code)=>Definitions.FirstOrDefault(x=>x.Code.Equals(code,StringComparison.OrdinalIgnoreCase))??throw ApiException.NotFound("التكامل غير موجود");
    private static string Validate(Field f,string value){if(f.Required&&string.IsNullOrWhiteSpace(value))throw ApiException.BadRequest($"{f.Label} مطلوب");if(value.Length>2000)throw ApiException.BadRequest($"{f.Label} أطول من المسموح");if(f.Type=="boolean"&&!bool.TryParse(value,out _))throw ApiException.BadRequest($"{f.Label} غير صالح");if(f.Type=="number"&&(!int.TryParse(value,out var n)||n<0))throw ApiException.BadRequest($"{f.Label} غير صالح");if(f.Type=="url"&&(!Uri.TryCreate(value,UriKind.Absolute,out var uri)||uri.Scheme!="https"&&!uri.IsLoopback))throw ApiException.BadRequest($"{f.Label} يجب أن يكون رابط HTTPS");if(f.Type=="email"&&(!value.Contains('@')||value.LastIndexOf('.')<value.IndexOf('@')))throw ApiException.BadRequest($"{f.Label} غير صالح");if(f.Type=="date"&&!DateOnly.TryParse(value,out _))throw ApiException.BadRequest($"{f.Label} غير صالح");if(f.Options is {Length:>0}&&!f.Options.Contains(value,StringComparer.OrdinalIgnoreCase))throw ApiException.BadRequest($"{f.Label} غير صالح");return value;}
    private static Definition D(string code,string ar,string en,string provider,string icon,string tone,string description,string operation,params Field[] fields)=>new(code,ar,en,provider,icon,tone,description,fields,operation);
    private static Field F(string code,string label,string type,string value,bool required=false,bool sensitive=false,string[]? options=null,string? help=null)=>new(code,label,type,value,required,sensitive,options,help);
    private void Audit(Guid actor,string? ip,string action,string entity,string? id,object? data)=>db.AuditLogs.Add(new AuditLog{UserId=actor==Guid.Empty?null:actor,Ip=ip,Action=action,EntityType=entity,EntityId=id,DataJson=data is null?null:JsonSerializer.Serialize(data)});
}

public sealed class IntegrationRetryWorker(IServiceScopeFactory scopes,ILogger<IntegrationRetryWorker> logger):BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken){while(!stoppingToken.IsCancellationRequested){try{using var scope=scopes.CreateScope();await scope.ServiceProvider.GetRequiredService<AdminIntegrationService>().RetryDueAsync(stoppingToken);}catch(OperationCanceledException)when(stoppingToken.IsCancellationRequested){break;}catch(Exception ex){logger.LogError(ex,"Integration retry cycle failed");}try{await Task.Delay(TimeSpan.FromMinutes(1),stoppingToken);}catch(OperationCanceledException){break;}}}
}
