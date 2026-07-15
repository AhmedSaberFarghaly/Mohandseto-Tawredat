export type Campaign={id:string;number:string;name:string;channel:string;audienceType:string;sector?:string;behaviorDays?:number;title:string;body:string;actionUrl?:string;imageUrl?:string;couponCode?:string;scheduleType:string;scheduledAt?:string;sentAt?:string;status:string;cost:number;recipients:number;delivered:number;opened:number;clicked:number;conversions:number;revenue:number;openRate:number;clickRate:number;conversionRate:number;createdAt:string;tenantIds:string[]};
export type Coupon={groupId:string;code:string;nameAr:string;discountType:string;discountValue:number;minimumSubtotal:number;maximumDiscount?:number;startsAt?:string;expiresAt?:string;usageLimit?:number;usedCount:number;oncePerCompany:boolean;newCustomersOnly:boolean;excludeDiscountedProducts:boolean;canCombine:boolean;applicableCategoryIds?:string;isActive:boolean;companyCount:number;tenantIds:string[]};
export type Tenant={id:string;name:string;sector?:string;lastOrderAt?:string};
export type Category={id:string;name:string};
export type ChannelRate={channel:string;delivered:number;opened:number;openRate:number};
export type BestTime={hour:number;opened:number;openRate:number};
export type Reports={channelRates:ChannelRate[];bestTimes:BestTime[];delivered:number;opened:number;clicked:number;conversions:number;revenue:number;cost:number;roi:number;openRate:number;conversionRate:number};
export type MarketingDashboard={campaigns:Campaign[];coupons:Coupon[];tenants:Tenant[];categories:Category[];sectors:string[];reports:Reports};
