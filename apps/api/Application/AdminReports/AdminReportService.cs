using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Application.AdminReports;

public interface IReportDeliverySender
{
    Task SendAsync(string reportName, IReadOnlyList<string> recipients, IReadOnlyList<string> formats, int rows, CancellationToken ct);
}
public sealed class ConsoleReportDeliverySender(ILogger<ConsoleReportDeliverySender> logger) : IReportDeliverySender
{
    public Task SendAsync(string reportName, IReadOnlyList<string> recipients, IReadOnlyList<string> formats, int rows, CancellationToken ct)
    { logger.LogInformation("Scheduled report {Report} delivered to {Recipients} in {Formats} with {Rows} rows", reportName, string.Join(',', recipients), string.Join(',', formats), rows); return Task.CompletedTask; }
}

public sealed class AdminReportService(AppDbContext db, IReportDeliverySender sender)
{
    private sealed record Definition(string Code, string Ar, string En, string Category, string Icon, string Description,
        string ValueLabel = "إجمالي القيمة", string CountLabel = "عدد السجلات", string AverageLabel = "المتوسط", string RateLabel = "معدل الإنجاز");
    private sealed record MetricRow(DateTime At, decimal Value, string Status, string Group, bool Completed,
        Dictionary<string, string> Cells, Guid? CompanyId = null, Guid? WarehouseId = null, Guid? UserId = null);

    private static readonly Definition[] Definitions =
    [
        new("sales","تقرير المبيعات","Sales Report","sales","↗","اتجاهات المبيعات ومتوسط الطلب والتوزيع","إجمالي المبيعات","عدد الطلبات","متوسط الطلب","نمو المبيعات"),
        new("orders","تقرير الطلبات","Orders Report","orders","□","حجم الطلبات وحالاتها ومتوسط التنفيذ","قيمة الطلبات","عدد الطلبات","متوسط الطلب","نسبة الاكتمال"),
        new("rfq","تقرير عروض الأسعار","RFQ Report","quotes","▣","طلبات التسعير والتحويل والحالات","قيمة العروض","عدد الطلبات","متوسط القيمة","معدل التحويل"),
        new("conversion","تقرير معدل التحويل","Conversion Rate Report","sales","⌁","تحويل طلبات التسعير إلى طلبات شراء","طلبات محولة","إجمالي RFQ","قيمة التحويل","معدل التحويل"),
        new("companies","تقرير الشركات","Companies Report","companies","▦","نشاط العملاء والائتمان والمراحل","إجمالي الائتمان","عدد الشركات","متوسط الائتمان","الشركات النشطة"),
        new("products","تقرير المنتجات","Products Report","products","◇","مبيعات المنتجات والكميات والقيمة","قيمة المنتجات","عدد المنتجات","متوسط المنتج","المنتجات النشطة"),
        new("categories","تقرير الأقسام","Categories Report","products","▦","أداء الأقسام حسب المبيعات والكميات","مبيعات الأقسام","عدد الأقسام","متوسط القسم","الأقسام النشطة"),
        new("inventory","تقرير المخزون","Inventory Report","inventory","▥","قيمة المخزون وحركته وحالته","قيمة المخزون","الأصناف المخزنة","متوسط قيمة الصنف","دقة التوفر"),
        new("out-of-stock","تقرير المنتجات النافدة","Out of Stock Report","inventory","!","الأصناف النافدة ومواقع التخزين","قيمة النقص","أصناف نافدة","متوسط النقص","نسبة المعالجة"),
        new("purchasing","تقرير المشتريات","Purchasing Report","suppliers","▤","أوامر الشراء والاستلام والتكلفة","إجمالي المشتريات","أوامر الشراء","متوسط الأمر","نسبة الاستلام"),
        new("suppliers","تقرير الموردين","Suppliers Report","suppliers","♜","أداء الموردين والقيمة والتقييم","قيمة التوريد","عدد الموردين","متوسط التوريد","الموردون النشطون"),
        new("profit","تقرير الأرباح","Profit Report","finance","★","الربحية حسب المنتجات والعملاء","صافي الربح","الطلبات الرابحة","متوسط الربح","هامش الربح"),
        new("tax","تقرير الضرائب","Tax Report","finance","%","ضريبة المبيعات والفواتير الضريبية","إجمالي الضريبة","الفواتير","متوسط الضريبة","الفواتير المعتمدة"),
        new("payments","تقرير المدفوعات","Payments Report","finance","▣","التحصيل وطرق الدفع والحالات","إجمالي المدفوعات","عمليات الدفع","متوسط الدفعة","المدفوعات المكتملة"),
        new("debts","تقرير الديون","Debts Report","finance","◷","الأرصدة المستحقة والمتأخرة","إجمالي الديون","فواتير مستحقة","متوسط الدين","نسبة التحصيل"),
        new("contracts","تقرير العقود","Contracts Report","companies","▰","قيمة العقود وحالاتها وانتهائها","القيمة السنوية","عدد العقود","متوسط العقد","العقود النشطة"),
        new("printed-products","تقرير المنتجات المطبوعة","Printed Products Report","products","▧","الإنتاج والكميات وجودة الطباعة","كمية الإنتاج","أوامر الإنتاج","متوسط الكمية","نسبة الاكتمال"),
        new("delivery","تقرير التوصيل","Delivery Report","delivery","▱","التسليم والتكلفة والمحاولات","تكلفة التوصيل","عدد الشحنات","متوسط التكلفة","التسليم الناجح"),
        new("returns","تقرير المرتجعات","Returns Report","orders","↩","المرتجعات والقيم والأسباب","قيمة المرتجعات","عدد المرتجعات","متوسط المرتجع","نسبة الإغلاق"),
        new("customer-service","تقرير خدمة العملاء","Customer Service Report","service","◉","أداء الدعم ورضا العملاء وSLA","التذاكر","عدد التذاكر","متوسط التقييم","نسبة الحل"),
        new("staff-performance","تقرير أداء الموظفين","Staff Performance Report","service","♙","أحمال العمل والإنجاز حسب الموظف","حجم العمل","عدد الموظفين","متوسط المهام","نسبة النشاط"),
        new("sales-reps","تقرير أداء مندوبي المبيعات","Sales Reps Report","sales","♞","مبيعات الموظفين وتحقيق النتائج","مبيعات المندوبين","عدد المندوبين","متوسط المبيعات","المندوبون النشطون"),
    ];

    private static readonly ReportSourceDto[] Sources =
    [
        Source("orders","الطلبات",("number","رقم الطلب","text"),("company","الشركة","text"),("value","القيمة","number"),("date","التاريخ","date"),("status","الحالة","text"),("costCenter","مركز التكلفة","text")),
        Source("companies","الشركات",("company","الشركة","text"),("sector","القطاع","text"),("value","حد الائتمان","number"),("status","الحالة","text"),("date","تاريخ التسجيل","date")),
        Source("products","المنتجات",("sku","SKU","text"),("product","المنتج","text"),("category","القسم","text"),("quantity","الكمية","number"),("value","القيمة","number"),("status","الحالة","text")),
        Source("inventory","المخزون",("sku","SKU","text"),("product","المنتج","text"),("warehouse","المخزن","text"),("quantity","الكمية","number"),("value","القيمة","number"),("status","الحالة","text")),
        Source("rfq","عروض الأسعار",("number","رقم RFQ","text"),("title","العنوان","text"),("company","الشركة","text"),("date","التاريخ","date"),("status","الحالة","text"),("value","القيمة","number")),
        Source("payments","المدفوعات",("reference","المرجع","text"),("company","الشركة","text"),("method","الطريقة","text"),("value","القيمة","number"),("date","التاريخ","date"),("status","الحالة","text")),
        Source("purchasing","المشتريات",("number","رقم الأمر","text"),("supplier","المورد","text"),("warehouse","المخزن","text"),("value","القيمة","number"),("date","التاريخ","date"),("status","الحالة","text")),
        Source("customer-service","خدمة العملاء",("number","رقم التذكرة","text"),("subject","الموضوع","text"),("company","الشركة","text"),("type","النوع","text"),("date","التاريخ","date"),("status","الحالة","text")),
    ];

    public async Task<ReportsDashboardDto> DashboardAsync(CancellationToken ct = default)
    {
        var saved = await db.SavedReports.AsNoTracking().OrderByDescending(x => x.IsFavorite).ThenByDescending(x => x.CreatedAt).ToListAsync(ct);
        var runs = await db.ReportRuns.AsNoTracking().OrderByDescending(x => x.StartedAt).Take(50).ToListAsync(ct);
        var categories = Definitions.GroupBy(x => x.Category).Select(x => new ReportCategoryDto(x.Key, CategoryName(x.Key), CategoryIcon(x.Key), x.Count(),
            x.Select(d => new ReportCatalogItemDto(d.Code,d.Ar,d.En,d.Category,d.Icon,d.Description)).ToList())).ToList();
        return new(categories, Sources, saved.Select(Map).ToList(), runs.Select(Map).ToList(),
            await db.Companies.AsNoTracking().OrderBy(x=>x.LegalName).Select(x=>new ReportOptionDto(x.Id,x.LegalName)).ToListAsync(ct),
            await db.Warehouses.AsNoTracking().OrderBy(x=>x.NameAr).Select(x=>new ReportOptionDto(x.Id,x.NameAr)).ToListAsync(ct),
            await db.Users.AsNoTracking().Where(x=>x.IsPlatformStaff).OrderBy(x=>x.FullName).Select(x=>new ReportOptionDto(x.Id,x.FullName)).ToListAsync(ct));
    }

    public async Task<ReportResultDto> BuiltInAsync(string code, ReportFilterDto filter, CancellationToken ct = default)
    {
        var definition = Find(code); var range = Range(filter); var rows = Apply(await LoadAsync(code, range.From, range.To, ct), filter);
        return Result(definition, range.From, range.To, rows);
    }

    public async Task<ReportResultDto> PreviewAsync(PreviewCustomReportDto dto, CancellationToken ct = default)
    {
        var source = Sources.FirstOrDefault(x => x.Code.Equals(dto.Source, StringComparison.OrdinalIgnoreCase))
            ?? throw ApiException.BadRequest("مصدر التقرير غير صالح");
        var fields = dto.Fields.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (fields.Count == 0 || fields.Any(x => source.Fields.All(f => !f.Code.Equals(x, StringComparison.OrdinalIgnoreCase))))
            throw ApiException.BadRequest("اختر حقولًا صالحة للتقرير");
        var built = await BuiltInAsync(SourceReport(source.Code), dto.Filters, ct);
        var columns = source.Fields.Where(x => fields.Contains(x.Code, StringComparer.OrdinalIgnoreCase)).ToList();
        var selected = built.Rows.Select(row => (IReadOnlyDictionary<string,string>)columns.ToDictionary(x=>x.Code,x=>row.GetValueOrDefault(x.Code)??"—")).ToList();
        if (!string.IsNullOrWhiteSpace(dto.GroupBy) && fields.Contains(dto.GroupBy, StringComparer.OrdinalIgnoreCase))
        {
            var grouped = selected.GroupBy(x=>x.GetValueOrDefault(dto.GroupBy!)??"—").Select(x=>(IReadOnlyDictionary<string,string>)new Dictionary<string,string>
            { [dto.GroupBy!] = x.Key, ["count"] = x.Count().ToString(CultureInfo.InvariantCulture), ["value"] = x.Sum(r=>Decimal(r.GetValueOrDefault("value"))).ToString("0.##",CultureInfo.InvariantCulture) }).ToList();
            columns = [source.Fields.First(x=>x.Code.Equals(dto.GroupBy,StringComparison.OrdinalIgnoreCase)),new("count","العدد","number"),new("value","الإجمالي","number")]; selected=grouped;
        }
        return built with { Code="custom",TitleAr="معاينة التقرير المخصص",TitleEn="Custom Report Preview",Columns=columns,Rows=selected,TotalRows=selected.Count };
    }

    public async Task<SavedReportDto> SaveAsync(Guid actorId, SaveCustomReportDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) throw ApiException.BadRequest("اسم التقرير مطلوب");
        await PreviewAsync(new(dto.Source,dto.Fields,dto.Filters,dto.GroupBy,dto.ChartType),ct);
        var entity = dto.Id is { } id ? await db.SavedReports.FirstOrDefaultAsync(x=>x.Id==id,ct) ?? throw ApiException.NotFound("التقرير المحفوظ غير موجود") : new SavedReport { OwnerUserId=actorId, CreatedBy=actorId };
        if (dto.Id is null) db.SavedReports.Add(entity);
        entity.Name=dto.Name.Trim(); entity.Source=dto.Source.ToLowerInvariant(); entity.FieldsJson=JsonSerializer.Serialize(dto.Fields.Distinct());
        entity.FiltersJson=JsonSerializer.Serialize(dto.Filters); entity.GroupBy=Clean(dto.GroupBy); entity.ChartType=Chart(dto.ChartType); entity.IsFavorite=dto.IsFavorite; entity.UpdatedAt=DateTime.UtcNow; entity.UpdatedBy=actorId;
        db.AuditLogs.Add(new AuditLog { UserId=actorId, Action=dto.Id is null?"report.created":"report.updated",EntityType=nameof(SavedReport),EntityId=entity.Id.ToString(),DataJson=JsonSerializer.Serialize(new { entity.Name,entity.Source,Fields=dto.Fields,entity.GroupBy,entity.ChartType }) });
        await db.SaveChangesAsync(ct); return Map(entity);
    }

    public async Task<SavedReportDto> ScheduleAsync(Guid actorId, Guid id, ScheduleReportDto dto, CancellationToken ct = default)
    {
        var entity=await db.SavedReports.FirstOrDefaultAsync(x=>x.Id==id,ct)??throw ApiException.NotFound("التقرير المحفوظ غير موجود");
        if(!Enum.TryParse<ReportScheduleFrequency>(dto.Frequency,true,out var frequency)) throw ApiException.BadRequest("تكرار الجدولة غير صالح");
        if(!TimeOnly.TryParse(dto.Time,CultureInfo.InvariantCulture,out var time)) throw ApiException.BadRequest("وقت الإرسال غير صالح");
        var formats=dto.Formats.Select(x=>x.Trim().ToUpperInvariant() switch { "EXCEL"=>"Excel", "PDF"=>"PDF", _=>"" }).Where(x=>x.Length>0).Distinct().ToList();
        var recipients=dto.Recipients.Select(x=>x.Trim().ToLowerInvariant()).Where(ValidEmail).Distinct().ToList();
        if(dto.IsActive&&(frequency==ReportScheduleFrequency.None||formats.Count==0||recipients.Count==0)) throw ApiException.BadRequest("حدد التكرار والصيغة ومستلمًا صالحًا للجدولة");
        entity.ScheduleFrequency=frequency;entity.ScheduleDay=dto.Day;entity.ScheduleTime=time;entity.FormatsCsv=string.Join(',',formats);entity.RecipientsCsv=string.Join(',',recipients);entity.IsScheduleActive=dto.IsActive;entity.NextRunAt=dto.IsActive?NextRun(frequency,dto.Day,time,DateTime.UtcNow):null;entity.UpdatedAt=DateTime.UtcNow;entity.UpdatedBy=actorId;
        db.AuditLogs.Add(new AuditLog{UserId=actorId,Action="report.schedule_updated",EntityType=nameof(SavedReport),EntityId=id.ToString(),DataJson=JsonSerializer.Serialize(new{frequency,dto.Day,time,formats,recipients,dto.IsActive,entity.NextRunAt})});
        await db.SaveChangesAsync(ct);return Map(entity);
    }

    public async Task DeleteAsync(Guid actorId, Guid id, CancellationToken ct=default)
    { var entity=await db.SavedReports.FirstOrDefaultAsync(x=>x.Id==id,ct)??throw ApiException.NotFound("التقرير المحفوظ غير موجود");entity.IsDeleted=true;entity.DeletedAt=DateTime.UtcNow;entity.UpdatedBy=actorId;db.AuditLogs.Add(new AuditLog{UserId=actorId,Action="report.deleted",EntityType=nameof(SavedReport),EntityId=id.ToString()});await db.SaveChangesAsync(ct); }

    public async Task<byte[]> ExportExcelAsync(ReportExportOptionsDto dto,CancellationToken ct=default)=>Excel(await ExportResult(dto,ct));
    public async Task<byte[]> ExportPdfAsync(ReportExportOptionsDto dto,CancellationToken ct=default)=>Pdf(await ExportResult(dto,ct),dto);
    private Task<ReportResultDto> ExportResult(ReportExportOptionsDto dto,CancellationToken ct)=>dto.Code.Equals("custom",StringComparison.OrdinalIgnoreCase)&&dto.Source is not null
        ?PreviewAsync(new(dto.Source,dto.Fields??[],dto.Filters,dto.GroupBy),ct):BuiltInAsync(dto.Code,dto.Filters,ct);

    public async Task<int> ProcessDueAsync(CancellationToken ct=default)
    {
        var due=await db.SavedReports.Where(x=>x.IsScheduleActive&&x.NextRunAt<=DateTime.UtcNow).ToListAsync(ct);var processed=0;
        foreach(var report in due)
        {
            var run=new ReportRun{SavedReportId=report.Id,ReportCode=report.Source,FormatsCsv=report.FormatsCsv,RecipientsCsv=report.RecipientsCsv};db.ReportRuns.Add(run);
            try{var fields=JsonSerializer.Deserialize<List<string>>(report.FieldsJson)??[];var filters=JsonSerializer.Deserialize<ReportFilterDto>(report.FiltersJson)??new();var result=await PreviewAsync(new(report.Source,fields,filters,report.GroupBy,report.ChartType),ct);await sender.SendAsync(report.Name,Split(report.RecipientsCsv),Split(report.FormatsCsv),result.TotalRows,ct);run.Status=ReportRunStatus.Completed;run.RowCount=result.TotalRows;run.CompletedAt=DateTime.UtcNow;processed++;}
            catch(Exception ex){run.Status=ReportRunStatus.Failed;run.Error=ex.Message.Length>500?ex.Message[..500]:ex.Message;run.CompletedAt=DateTime.UtcNow;}
            report.LastRunAt=DateTime.UtcNow;report.NextRunAt=NextRun(report.ScheduleFrequency,report.ScheduleDay,report.ScheduleTime??new TimeOnly(8,0),DateTime.UtcNow);await db.SaveChangesAsync(ct);
        }
        return processed;
    }

    private async Task<List<MetricRow>> LoadAsync(string code,DateTime from,DateTime to,CancellationToken ct)
    {
        var companies=await db.Companies.AsNoTracking().Select(x=>new{x.Id,x.TenantId,x.LegalName,x.Sector,x.CreditLimit,x.CustomerStage,x.CreatedAt}).ToListAsync(ct);
        var companyByTenant=companies.ToDictionary(x=>x.TenantId,x=>x); MetricRow M(DateTime at,decimal value,string status,string group,bool complete,Dictionary<string,string> cells,Guid? company=null,Guid? warehouse=null,Guid? user=null)=>new(at,value,status,group,complete,cells,company,warehouse,user);
        if(code is "sales" or "orders")
        {var data=await db.Orders.AsNoTracking().Where(x=>x.CreatedAt>=from&&x.CreatedAt<=to&&(code=="orders"||x.Status!=OrderStatus.Cancelled)).ToListAsync(ct);return data.Select(x=>{var c=companyByTenant.GetValueOrDefault(x.TenantId);return M(x.CreatedAt,x.Total,x.Status.ToString(),x.PaymentMethod.ToString(),x.Status is OrderStatus.Delivered or OrderStatus.Completed,new(){["number"]=x.Number,["company"]=c?.LegalName??"—",["value"]=N(x.Total),["date"]=D(x.CreatedAt),["status"]=x.Status.ToString(),["costCenter"]=x.CostCenterName??"—"},c?.Id,null,x.AssignedStaffId);}).ToList();}
        if(code is "rfq" or "conversion")
        {var data=await db.Rfqs.AsNoTracking().Include(x=>x.Items).Where(x=>x.CreatedAt>=from&&x.CreatedAt<=to).ToListAsync(ct);return data.Select(x=>{var c=companyByTenant.GetValueOrDefault(x.TenantId);var converted=x.Status is RfqStatus.Converted or RfqStatus.Accepted;return M(x.CreatedAt,code=="conversion"?(converted?1:0):x.Items.Count,x.Status.ToString(),x.DeliveryGovernorate??"غير محدد",converted,new(){["number"]=x.Number,["title"]=x.Title,["company"]=c?.LegalName??"—",["value"]=(code=="conversion"?(converted?1:0):x.Items.Count).ToString(CultureInfo.InvariantCulture),["date"]=D(x.CreatedAt),["status"]=x.Status.ToString()},c?.Id,null,x.AssignedStaffId);}).ToList();}
        if(code=="companies")return companies.Where(x=>x.CreatedAt>=from&&x.CreatedAt<=to).Select(x=>M(x.CreatedAt,x.CreditLimit,x.CustomerStage.ToString(),x.Sector??"غير محدد",x.CustomerStage==CustomerStage.Active,new(){["company"]=x.LegalName,["sector"]=x.Sector??"—",["value"]=N(x.CreditLimit),["status"]=x.CustomerStage.ToString(),["date"]=D(x.CreatedAt)},x.Id)).ToList();
        if(code is "products" or "categories" or "profit")
        {var items=await db.OrderItems.AsNoTracking().Where(x=>x.CreatedAt>=from&&x.CreatedAt<=to&&x.Order.Status!=OrderStatus.Cancelled).ToListAsync(ct);var products=await db.Products.AsNoTracking().Include(x=>x.Category).ToDictionaryAsync(x=>x.Id,ct);return items.GroupBy(x=>code=="categories"?(products.GetValueOrDefault(x.ProductId)?.CategoryId??Guid.Empty):code=="profit"?x.OrderId:x.ProductId).Select(g=>{var first=g.First();var p=products.GetValueOrDefault(first.ProductId);var value=code=="profit"?g.Sum(x=>x.LineTotal-(products.GetValueOrDefault(x.ProductId)?.CostPrice??0)*x.Quantity):g.Sum(x=>x.LineTotal);var name=code=="categories"?p?.Category.NameAr??"غير مصنف":code=="profit"?first.OrderId.ToString():p?.NameAr??first.NameAr;return M(g.Max(x=>x.CreatedAt),value,"Active",name,true,new(){["sku"]=p?.Sku??first.Sku,["product"]=name,["category"]=p?.Category.NameAr??"—",["quantity"]=g.Sum(x=>x.Quantity).ToString(),["value"]=N(value),["status"]="Active",["date"]=D(g.Max(x=>x.CreatedAt))});}).ToList();}
        if(code is "inventory" or "out-of-stock")
        {var stocks=await db.WarehouseStocks.AsNoTracking().Include(x=>x.Product).Include(x=>x.Warehouse).Where(x=>code=="inventory"||x.OnHandQty-x.ReservedQty<=0).ToListAsync(ct);return stocks.Select(x=>{var available=x.OnHandQty-x.ReservedQty;var status=available<=0?"OutOfStock":available<=x.ReorderLevel?"LowStock":"Available";return M(x.UpdatedAt??x.CreatedAt,x.OnHandQty*x.Product.CostPrice,status,x.Warehouse.NameAr,available>0,new(){["sku"]=x.Product.Sku,["product"]=x.Product.NameAr,["warehouse"]=x.Warehouse.NameAr,["quantity"]=available.ToString(),["value"]=N(x.OnHandQty*x.Product.CostPrice),["status"]=status,["date"]=D(x.UpdatedAt??x.CreatedAt)},null,x.WarehouseId);}).ToList();}
        if(code=="purchasing")
        {var suppliers=await db.Suppliers.AsNoTracking().ToDictionaryAsync(x=>x.Id,x=>x.NameAr,ct);var warehouses=await db.Warehouses.AsNoTracking().ToDictionaryAsync(x=>x.Id,x=>x.NameAr,ct);var data=await db.PurchaseOrders.AsNoTracking().Where(x=>x.CreatedAt>=from&&x.CreatedAt<=to).ToListAsync(ct);return data.Select(x=>M(x.CreatedAt,x.Total,x.Status.ToString(),suppliers.GetValueOrDefault(x.SupplierId)??"—",x.Status==PurchaseOrderStatus.Received,new(){["number"]=x.Number,["supplier"]=suppliers.GetValueOrDefault(x.SupplierId)??"—",["warehouse"]=warehouses.GetValueOrDefault(x.WarehouseId)??"—",["value"]=N(x.Total),["date"]=D(x.CreatedAt),["status"]=x.Status.ToString()},null,x.WarehouseId,x.CreatedByUserId)).ToList();}
        if(code=="suppliers")
        {var suppliers=await db.Suppliers.AsNoTracking().Where(x=>x.CreatedAt>=from&&x.CreatedAt<=to).ToListAsync(ct);var orders=await db.PurchaseOrders.AsNoTracking().Select(x=>new{x.SupplierId,x.Total}).ToListAsync(ct);var totals=orders.GroupBy(x=>x.SupplierId).ToDictionary(x=>x.Key,x=>x.Sum(y=>y.Total));return suppliers.Select(x=>M(x.CreatedAt,totals.GetValueOrDefault(x.Id),x.IsActive?"Active":"Inactive",Rating(x.Rating),x.IsActive,new(){["supplier"]=x.NameAr,["rating"]=x.Rating.ToString("0.0"),["value"]=N(totals.GetValueOrDefault(x.Id)),["status"]=x.IsActive?"Active":"Inactive",["date"]=D(x.CreatedAt)})).ToList();}
        if(code is "tax" or "debts")
        {var data=await db.Invoices.AsNoTracking().Include(x=>x.Order).Where(x=>x.IssuedAt>=from&&x.IssuedAt<=to&&x.Status!=InvoiceStatus.Cancelled).ToListAsync(ct);return data.Where(x=>code=="tax"||x.Total>x.PaidAmount).Select(x=>{var c=companyByTenant.GetValueOrDefault(x.TenantId);var value=code=="tax"?x.Tax:x.Total-x.PaidAmount;return M(x.IssuedAt,value,x.Status.ToString(),code=="tax"?x.Type.ToString():(x.DueAt<DateTime.UtcNow?"متأخر":"مستحق"),code=="tax"?x.Status!=InvoiceStatus.Draft:x.PaidAmount>=x.Total,new(){["number"]=x.Number,["company"]=c?.LegalName??"—",["value"]=N(value),["date"]=D(x.IssuedAt),["status"]=x.Status.ToString()},c?.Id);}).ToList();}
        if(code=="payments")
        {var data=await db.InvoicePayments.AsNoTracking().Include(x=>x.Invoice).Where(x=>x.CreatedAt>=from&&x.CreatedAt<=to).ToListAsync(ct);return data.Select(x=>{var c=companyByTenant.GetValueOrDefault(x.TenantId);return M(x.CreatedAt,x.Amount,x.Status.ToString(),x.Method,x.Status==InvoicePaymentStatus.Completed,new(){["reference"]=x.Reference,["company"]=c?.LegalName??"—",["method"]=x.Method,["value"]=N(x.Amount),["date"]=D(x.CreatedAt),["status"]=x.Status.ToString()},c?.Id,null,x.VerifiedBy);}).ToList();}
        if(code=="contracts")
        {var data=await db.CompanyContracts.AsNoTracking().Where(x=>x.CreatedAt>=from&&x.CreatedAt<=to).ToListAsync(ct);var names=companies.ToDictionary(x=>x.Id,x=>x.LegalName);return data.Select(x=>M(x.CreatedAt,x.AnnualValue,x.Status.ToString(),x.Type.ToString(),x.Status==CompanyContractStatus.Active,new(){["number"]=x.Number,["company"]=names.GetValueOrDefault(x.CompanyId)??"—",["value"]=N(x.AnnualValue),["date"]=D(x.CreatedAt),["status"]=x.Status.ToString()},x.CompanyId)).ToList();}
        if(code=="printed-products")
        {var data=await db.ProductionJobs.AsNoTracking().Where(x=>x.CreatedAt>=from&&x.CreatedAt<=to).ToListAsync(ct);return data.Select(x=>M(x.CreatedAt,x.ProducedQuantity,x.ActualCompletion.HasValue?"Completed":"InProgress",x.PackagingType??"بدون تغليف",x.ActualCompletion.HasValue,new(){["number"]=x.Number,["quantity"]=x.ProducedQuantity.ToString(),["value"]=x.ProducedQuantity.ToString(),["date"]=D(x.CreatedAt),["status"]=x.ActualCompletion.HasValue?"Completed":"InProgress"})).ToList();}
        if(code=="delivery")
        {var data=await db.Shipments.AsNoTracking().Where(x=>x.CreatedAt>=from&&x.CreatedAt<=to).ToListAsync(ct);return data.Select(x=>M(x.CreatedAt,x.DeliveryCost,x.Status,x.DeliveryZone??"غير محدد",x.DeliveredAt.HasValue,new(){["number"]=x.Number,["driver"]=x.DriverName??"—",["zone"]=x.DeliveryZone??"—",["value"]=N(x.DeliveryCost),["date"]=D(x.CreatedAt),["status"]=x.Status},null,null,x.DriverUserId)).ToList();}
        if(code=="returns")
        {var data=await db.ReturnRequests.AsNoTracking().Where(x=>x.CreatedAt>=from&&x.CreatedAt<=to).ToListAsync(ct);return data.Select(x=>{var c=companyByTenant.GetValueOrDefault(x.TenantId);return M(x.CreatedAt,x.ApprovedTotal??x.RequestedTotal,x.Status.ToString(),x.Resolution.ToString(),x.Status==ReturnStatus.Completed,new(){["number"]=x.Number,["company"]=c?.LegalName??"—",["value"]=N(x.ApprovedTotal??x.RequestedTotal),["date"]=D(x.CreatedAt),["status"]=x.Status.ToString()},c?.Id,null,x.UserId);}).ToList();}
        if(code=="customer-service")
        {var data=await db.SupportTickets.AsNoTracking().Where(x=>x.CreatedAt>=from&&x.CreatedAt<=to).ToListAsync(ct);return data.Select(x=>{var c=companyByTenant.GetValueOrDefault(x.TenantId);return M(x.CreatedAt,1,x.Status.ToString(),x.Type.ToString(),x.Status is SupportTicketStatus.Resolved or SupportTicketStatus.Closed,new(){["number"]=x.Number,["subject"]=x.Subject,["company"]=c?.LegalName??"—",["type"]=x.Type.ToString(),["date"]=D(x.CreatedAt),["status"]=x.Status.ToString(),["value"]="1"},c?.Id,null,x.AssignedStaffUserId);}).ToList();}
        if(code is "staff-performance" or "sales-reps")
        {var users=await db.Users.AsNoTracking().Include(x=>x.Roles).ThenInclude(x=>x.Role).Where(x=>x.IsPlatformStaff&&(code=="staff-performance"||x.Roles.Any(r=>r.Role.Code=="sales_agent"||r.Role.Code=="sales_manager"))).ToListAsync(ct);var orders=await db.Orders.AsNoTracking().Where(x=>x.CreatedAt>=from&&x.CreatedAt<=to&&x.AssignedStaffId!=null).Select(x=>new{x.AssignedStaffId,x.Total}).ToListAsync(ct);var map=orders.GroupBy(x=>x.AssignedStaffId!.Value).ToDictionary(x=>x.Key,x=>new{Value=x.Sum(y=>y.Total),Count=x.Count()});return users.Select(x=>{var metric=map.GetValueOrDefault(x.Id);var value=code=="sales-reps"?metric?.Value??0:metric?.Count??0;return M(x.CreatedAt,value,x.IsActive?"Active":"Inactive",x.Department??"غير محدد",x.IsActive,new(){["staff"]=x.FullName,["department"]=x.Department??"—",["orders"]=(metric?.Count??0).ToString(),["value"]=N(value),["date"]=D(x.CreatedAt),["status"]=x.IsActive?"Active":"Inactive"},null,null,x.Id);}).ToList();}
        throw ApiException.NotFound("نوع التقرير غير موجود");
    }

    private static List<MetricRow> Apply(List<MetricRow> rows,ReportFilterDto f)=>rows.Where(x=>(f.CompanyId==null||x.CompanyId==f.CompanyId)&&(f.WarehouseId==null||x.WarehouseId==f.WarehouseId)&&(f.UserId==null||x.UserId==f.UserId)&&(string.IsNullOrWhiteSpace(f.Status)||x.Status.Equals(f.Status,StringComparison.OrdinalIgnoreCase))&&(f.MinValue==null||x.Value>=f.MinValue)&&(f.MaxValue==null||x.Value<=f.MaxValue)&&(string.IsNullOrWhiteSpace(f.Search)||x.Cells.Values.Any(v=>v.Contains(f.Search,StringComparison.OrdinalIgnoreCase)))).ToList();
    private static ReportResultDto Result(Definition d,DateTime from,DateTime to,List<MetricRow> rows)
    {
        var total=rows.Sum(x=>x.Value);var count=rows.Count;var average=count==0?0:total/count;var rate=count==0?0:rows.Count(x=>x.Completed)*100m/count;
        var trend=rows.GroupBy(x=>new{x.At.Year,x.At.Month}).OrderBy(x=>x.Key.Year).ThenBy(x=>x.Key.Month).Select(x=>new ReportPointDto(new DateTime(x.Key.Year,x.Key.Month,1).ToString("MMM yyyy",new CultureInfo("ar-EG")),x.Sum(y=>y.Value))).ToList();
        var breakdown=rows.GroupBy(x=>x.Group).OrderByDescending(x=>x.Sum(y=>y.Value)).Take(8).Select(x=>new ReportBreakdownDto(x.Key,x.Sum(y=>y.Value),total==0?(count==0?0:decimal.Round(x.Count()*100m/count,1)):decimal.Round(x.Sum(y=>y.Value)*100m/total,1))).ToList();
        var keys=rows.SelectMany(x=>x.Cells.Keys).Distinct().Take(10).ToList();var columns=keys.Select(x=>new ReportFieldDto(x,FieldLabel(x),x is "value" or "quantity" or "orders"?"number":x=="date"?"date":"text")).ToList();
        return new(d.Code,d.Ar,d.En,from,to,[new(d.ValueLabel,total,"currency"),new(d.CountLabel,count,"number"),new(d.AverageLabel,average,"currency"),new(d.RateLabel,rate,"percent")],trend,breakdown,columns,rows.Take(500).Select(x=>(IReadOnlyDictionary<string,string>)x.Cells).ToList(),count);
    }

    private static byte[] Excel(ReportResultDto result)
    {using var output=new MemoryStream();using(var zip=new ZipArchive(output,ZipArchiveMode.Create,true)){Write(zip,"[Content_Types].xml","<?xml version=\"1.0\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/></Types>");Write(zip,"_rels/.rels","<?xml version=\"1.0\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");Write(zip,"xl/workbook.xml","<?xml version=\"1.0\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Report\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");Write(zip,"xl/_rels/workbook.xml.rels","<?xml version=\"1.0\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/></Relationships>");var rows=new List<string[]>{result.Columns.Select(x=>x.LabelAr).ToArray()};rows.AddRange(result.Rows.Select(x=>result.Columns.Select(c=>x.GetValueOrDefault(c.Code)??"").ToArray()));var xml=rows.Select((r,i)=>$"<row r=\"{i+1}\">{string.Join("",r.Select((v,c)=>$"<c r=\"{Column(c)}{i+1}\" t=\"inlineStr\"><is><t>{SecurityElement.Escape(v)}</t></is></c>"))}</row>");Write(zip,"xl/worksheets/sheet1.xml",$"<?xml version=\"1.0\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>{string.Join("",xml)}</sheetData></worksheet>");}return output.ToArray();}
    private static byte[] Pdf(ReportResultDto result,ReportExportOptionsDto options)
    {var lines=new List<string>{$"MOHANDSETO TAWRDAT - {result.TitleEn}",$"Period {result.From:yyyy-MM-dd} to {result.To:yyyy-MM-dd}",string.Join(" | ",result.Kpis.Select(x=>$"{Ascii(x.Label)} {x.Value:0.##}")),string.Join(" | ",result.Columns.Select(x=>Ascii(x.LabelAr)))};lines.AddRange(result.Rows.Take(40).Select(x=>string.Join(" | ",result.Columns.Select(c=>Ascii(x.GetValueOrDefault(c.Code)??"")))));var text=string.Join(" ",lines).Replace("\\","\\\\").Replace("(","\\(").Replace(")","\\)");if(text.Length>12000)text=text[..12000];var stream=$"BT /F1 8 Tf 28 560 Td 12 TL ({text}) Tj ET";var width=options.Orientation.Equals("Portrait",StringComparison.OrdinalIgnoreCase)?595:842;var height=options.Orientation.Equals("Portrait",StringComparison.OrdinalIgnoreCase)?842:595;var objects=new[]{"<< /Type /Catalog /Pages 2 0 R >>","<< /Type /Pages /Kids [3 0 R] /Count 1 >>",$"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {width} {height}] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>",$"<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}\nendstream","<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"};var sb=new StringBuilder("%PDF-1.4\n");var offsets=new List<int>{0};for(var i=0;i<objects.Length;i++){offsets.Add(Encoding.ASCII.GetByteCount(sb.ToString()));sb.Append($"{i+1} 0 obj\n{objects[i]}\nendobj\n");}var xref=Encoding.ASCII.GetByteCount(sb.ToString());sb.Append($"xref\n0 {objects.Length+1}\n0000000000 65535 f \n");for(var i=1;i<offsets.Count;i++)sb.Append($"{offsets[i]:D10} 00000 n \n");sb.Append($"trailer << /Size {objects.Length+1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");return Encoding.ASCII.GetBytes(sb.ToString());}

    private static SavedReportDto Map(SavedReport x)=>new(x.Id,x.Name,x.Source,JsonSerializer.Deserialize<List<string>>(x.FieldsJson)??[],JsonSerializer.Deserialize<ReportFilterDto>(x.FiltersJson)??new(),x.GroupBy,x.ChartType,x.IsFavorite,x.ScheduleFrequency.ToString(),x.ScheduleDay,x.ScheduleTime?.ToString("HH:mm"),Split(x.FormatsCsv),Split(x.RecipientsCsv),x.IsScheduleActive,x.NextRunAt,x.LastRunAt);
    private static ReportRunDto Map(ReportRun x)=>new(x.Id,x.SavedReportId,x.ReportCode,x.Status.ToString(),x.RowCount,x.FormatsCsv,x.RecipientsCsv,x.StartedAt,x.CompletedAt,x.Error);
    private static Definition Find(string code)=>Definitions.FirstOrDefault(x=>x.Code.Equals(code,StringComparison.OrdinalIgnoreCase))??throw ApiException.NotFound("نوع التقرير غير موجود");
    private static (DateTime From,DateTime To) Range(ReportFilterDto f){var to=f.To?.ToUniversalTime()??DateTime.UtcNow;var from=f.From?.ToUniversalTime()??to.AddMonths(-6);if(from>to||to-from>TimeSpan.FromDays(3660))throw ApiException.BadRequest("فترة التقرير غير صالحة");return(from,to);}
    private static ReportSourceDto Source(string code,string name,params (string Code,string Label,string Type)[] fields)=>new(code,name,fields.Select(x=>new ReportFieldDto(x.Code,x.Label,x.Type)).ToList());
    private static string SourceReport(string source)=>source switch{"orders"=>"orders","companies"=>"companies","products"=>"products","inventory"=>"inventory","rfq"=>"rfq","payments"=>"payments","purchasing"=>"purchasing","customer-service"=>"customer-service",_=>throw ApiException.BadRequest("مصدر التقرير غير صالح")};
    private static string CategoryName(string code)=>code switch{"sales"=>"المبيعات","orders"=>"الطلبات","quotes"=>"عروض الأسعار","companies"=>"الشركات","products"=>"المنتجات","inventory"=>"المخزون","suppliers"=>"الموردون","finance"=>"المالية","delivery"=>"التوصيل","service"=>"خدمة العملاء",_=>code};
    private static string CategoryIcon(string code)=>code switch{"sales"=>"↗","orders"=>"□","quotes"=>"▣","companies"=>"▦","products"=>"◇","inventory"=>"▥","suppliers"=>"♜","finance"=>"▤","delivery"=>"▱",_=>"◉"};
    private static string FieldLabel(string code)=>code switch{"number"=>"الرقم","company"=>"الشركة","value"=>"القيمة","date"=>"التاريخ","status"=>"الحالة","costCenter"=>"مركز التكلفة","title"=>"العنوان","sector"=>"القطاع","sku"=>"SKU","product"=>"المنتج","category"=>"القسم","quantity"=>"الكمية","warehouse"=>"المخزن","supplier"=>"المورد","rating"=>"التقييم","reference"=>"المرجع","method"=>"الطريقة","driver"=>"المندوب","zone"=>"المنطقة","subject"=>"الموضوع","type"=>"النوع","staff"=>"الموظف","department"=>"القسم","orders"=>"الطلبات",_=>code};
    private static string Chart(string value)=>value is "Line" or "Bar" or "Pie" or "Table"?value:"Line";
    private static string? Clean(string? value)=>string.IsNullOrWhiteSpace(value)?null:value.Trim();
    private static string D(DateTime value)=>value.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);private static string N(decimal value)=>value.ToString("0.##",CultureInfo.InvariantCulture);private static decimal Decimal(string? value)=>decimal.TryParse(value,NumberStyles.Any,CultureInfo.InvariantCulture,out var n)?n:0;
    private static string Rating(decimal value)=>value>=4.5m?"ممتاز":value>=3.5m?"جيد":"يحتاج تحسين";
    private static List<string> Split(string? value)=>string.IsNullOrWhiteSpace(value)?[]:value.Split(',',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries).ToList();
    private static bool ValidEmail(string value)=>value.Contains('@')&&value.IndexOf('@')>0&&value.LastIndexOf('.')>value.IndexOf('@')+1;
    private static DateTime NextRun(ReportScheduleFrequency frequency,int? day,TimeOnly time,DateTime now){var candidate=now.Date+time.ToTimeSpan();return frequency switch{ReportScheduleFrequency.Daily=>candidate<=now?candidate.AddDays(1):candidate,ReportScheduleFrequency.Weekly=>NextWeek(candidate,Math.Clamp(day??1,0,6),now),ReportScheduleFrequency.Monthly=>NextMonth(candidate,Math.Clamp(day??1,1,28),now),_=>now.AddYears(100)};}
    private static DateTime NextWeek(DateTime candidate,int day,DateTime now){candidate=candidate.AddDays((day-(int)candidate.DayOfWeek+7)%7);return candidate<=now?candidate.AddDays(7):candidate;}
    private static DateTime NextMonth(DateTime candidate,int day,DateTime now){candidate=new DateTime(candidate.Year,candidate.Month,day,candidate.Hour,candidate.Minute,0,DateTimeKind.Utc);return candidate<=now?candidate.AddMonths(1):candidate;}
    private static string Ascii(string value)=>new(value.Select(ch=>ch<=127?ch:'?').ToArray());
    private static void Write(ZipArchive zip,string name,string content){var e=zip.CreateEntry(name);using var w=new StreamWriter(e.Open(),new UTF8Encoding(false));w.Write(content);}
    private static string Column(int i){var v="";for(i++;i>0;i=(i-1)/26)v=(char)('A'+(i-1)%26)+v;return v;}
}

public sealed class ScheduledReportWorker(IServiceScopeFactory scopes,ILogger<ScheduledReportWorker> logger):BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {while(!stoppingToken.IsCancellationRequested){try{using var scope=scopes.CreateScope();var count=await scope.ServiceProvider.GetRequiredService<AdminReportService>().ProcessDueAsync(stoppingToken);if(count>0)logger.LogInformation("Processed {Count} scheduled reports",count);}catch(OperationCanceledException)when(stoppingToken.IsCancellationRequested){break;}catch(Exception ex){logger.LogError(ex,"Scheduled report worker failed");}await Task.Delay(TimeSpan.FromMinutes(1),stoppingToken);}}
}
