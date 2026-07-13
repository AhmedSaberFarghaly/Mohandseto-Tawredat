export type Metric = { value: number; changePercent: number };
export type StatusSlice = { status: string; count: number };
export type DashboardData = {
  generatedAt: string;
  days: number;
  summary: {
    totalSales: Metric;
    newOrders: Metric;
    pendingQuotes: Metric;
    activeCompanies: Metric;
  };
  salesTrend: { date: string; sales: number; orders: number }[];
  ordersByStatus: StatusSlice[];
  quotesByStatus: StatusSlice[];
  topCompanies: { tenantId: string; company: string; sales: number; orders: number }[];
  recentOrders: { id: string; number: string; company: string; createdAt: string; total: number; status: string }[];
};

export const statusNames: Record<string, string> = {
  PendingApproval: "بانتظار الموافقة", Confirmed: "تم التأكيد", Processing: "قيد التجهيز",
  Picking: "جاري الجمع", Packing: "جاري التعبئة", Shipped: "تم الشحن",
  OutForDelivery: "خرج للتسليم", PartiallyDelivered: "تسليم جزئي", Delivered: "تم التسليم",
  Completed: "مكتمل", Delayed: "متأخر", Cancelled: "ملغي", Draft: "مسودة",
  Submitted: "مقدم", UnderReview: "قيد المراجعة", ClarificationRequested: "بانتظار توضيح",
  Quoted: "تم التسعير", Negotiating: "تفاوض", Accepted: "مقبول", Rejected: "مرفوض",
};

export const money = new Intl.NumberFormat("ar-EG", { style: "currency", currency: "EGP", maximumFractionDigits: 0 });
export const number = new Intl.NumberFormat("ar-EG");
