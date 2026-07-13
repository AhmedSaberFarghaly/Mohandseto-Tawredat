export type OrderRow = { id:string; number:string; company:string; customer:string; status:string; total:number; itemCount:number; createdAt:string; requiredDate:string; purchaseOrderNumber?:string; assignedStaff?:string; isLate:boolean; isArchived:boolean };
export type OrderPage = { items:OrderRow[]; total:number; page:number; pageSize:number; summary:{ total:number; newToday:number; processing:number; outForDelivery:number; late:number; archived:number } };
export type OrderProduct = { itemId:string; productId:string; sku:string; name:string; quantity:number; unitPrice:number; lineTotal:number; stockQty:number; stockStatus:string };
export type OrderDetail = {
  order:{ id:string; number:string; status:string; subtotal:number; savings:number; couponDiscount:number; taxIncluded:number; shipping:number; total:number; branchName:string; deliveryAddress:string; receiverName:string; receiverPhone:string; requiredDate:string; shippingMethod:string; paymentMethod:string; purchaseOrderNumber?:string; internalReference?:string; costCenterName?:string; projectName?:string; requestingDepartment?:string; orderNote?:string; allowSplitDelivery:boolean; requiresApproval:boolean; items:{id:string;sku:string;nameAr:string;quantity:number;unitPrice:number;lineTotal:number}[]; history:{status:string;note?:string;at:string}[] };
  company:{ tenantId:string; legalName:string; taxNumber?:string; industry?:string; governorate?:string; phone?:string; email?:string; creditLimit:number; creditUsed:number };
  customer:{ id:string;fullName:string;phone:string;email?:string;jobTitle?:string;department?:string };
  assignedStaff?:string; archivedAt?:string; products:OrderProduct[];
  notes:{id:string;staffName:string;body:string;at:string}[];
  communications:{id:string;staffName:string;channel:string;direction:string;subject:string;body?:string;at:string}[];
  refunds:{id:string;amount:number;method:string;reason:string;reference:string;status:string;at:string}[];
  invoice?:{id:string;number:string;status:string;total:number;paidAmount:number;issuedAt:string;dueAt:string};
  shipments:{id:string;number:string;status:string;carrier:string;createdAt:string;items:{orderItemId:string;product:string;quantity:number}[]}[];
};
export type RecurringOrder = { id:string;orderNumber:string;company:string;frequency:string;interval:number;nextRunAt:string;endsAt?:string;isActive:boolean;estimatedValue:number };
export const money = new Intl.NumberFormat("ar-EG",{style:"currency",currency:"EGP",maximumFractionDigits:0});
export const statusName:Record<string,string>={PendingApproval:"بانتظار الموافقة",Confirmed:"مؤكد",Processing:"قيد التجهيز",Picking:"جاري الجمع",Packing:"جاري التغليف",Shipped:"تم الشحن",OutForDelivery:"خرج للتوصيل",PartiallyDelivered:"تسليم جزئي",Delivered:"تم التسليم",Completed:"مكتمل",Delayed:"متأخر",Cancelled:"ملغي"};
