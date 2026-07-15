using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Domain.Entities;

namespace Mohandseto.Api.Infrastructure;

public static class SupportOperationsSeeder
{
    public static async Task SeedAsync(AppDbContext db, ILogger logger, CancellationToken ct = default)
    {
        if (!await db.SupportSlaPolicies.AnyAsync(ct))
        {
            var times = new Dictionary<SupportTicketPriority, (int Response, int Resolve)>
            {
                [SupportTicketPriority.Low] = (240, 2880), [SupportTicketPriority.Normal] = (60, 1440),
                [SupportTicketPriority.High] = (30, 480), [SupportTicketPriority.Urgent] = (15, 240)
            };
            foreach (var type in Enum.GetValues<SupportTicketType>()) foreach (var (priority, time) in times)
                db.SupportSlaPolicies.Add(new SupportSlaPolicy { Type = type, Priority = priority, FirstResponseMinutes = time.Response, ResolutionMinutes = time.Resolve });
        }
        if (!await db.SupportReplyTemplates.AnyAsync(ct))
        {
            db.SupportReplyTemplates.AddRange(
                new SupportReplyTemplate { Title = "استلام البلاغ", Body = "تم استلام بلاغك وبدأ فريق خدمة العملاء مراجعته. سنوافيك بالتحديثات في أقرب وقت." },
                new SupportReplyTemplate { Title = "طلب معلومات إضافية", Body = "نحتاج بعض المعلومات الإضافية لإكمال المراجعة. يرجى إرسال رقم الطلب وصور واضحة للمشكلة." },
                new SupportReplyTemplate { Title = "تأكيد الحل", Body = "تم تنفيذ الإجراء المطلوب. نرجو تأكيد أن المشكلة حُلّت، ويسعدنا استقبال تقييمك للخدمة." },
                new SupportReplyTemplate { Title = "متابعة التوصيل", Type = SupportTicketType.Delivery, Body = "تمت مراجعة الشحنة والتواصل مع فريق التوصيل. سنرسل لك الموعد المحدّث فور تأكيده." },
                new SupportReplyTemplate { Title = "متابعة الدفع", Type = SupportTicketType.Payment, Body = "جاري التحقق من التحويل البنكي مع فريق الحسابات، وسيتم تحديث حالة الفاتورة بعد المطابقة." });
        }
        await db.SaveChangesAsync(ct); logger.LogInformation("Seeded support SLA policies and reply templates");
    }
}
