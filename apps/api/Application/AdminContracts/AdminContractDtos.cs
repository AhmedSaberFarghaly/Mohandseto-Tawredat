namespace Mohandseto.Api.Application.AdminContracts;

public sealed record ContractKpisDto(int ActiveContracts, decimal TotalAnnualValue, int ExpiringSoon, decimal AverageCustomerSavingPercent);
public sealed record ContractCompanyOptionDto(Guid Id, Guid TenantId, string Name, string Status, decimal AnnualPurchases, int PreviousContracts, decimal CreditLimit);
public sealed record ContractProductOptionDto(Guid Id, string Sku, string Name, decimal MarketPrice, decimal CostPrice);
public sealed record ContractRowDto(Guid Id, string Number, Guid CompanyId, string Company, string Type, decimal AnnualValue, decimal ConsumptionPercent, DateTime StartsAt, DateTime EndsAt, string Status, int ProductCount, decimal AverageSavingPercent, string? SalesRep);
public sealed record ContractDashboardDto(ContractKpisDto Kpis, IReadOnlyList<ContractRowDto> Contracts, IReadOnlyList<ContractCompanyOptionDto> Companies, IReadOnlyList<ContractProductOptionDto> Products);

public sealed record ContractProductDto(Guid ProductId, string Sku, string Product, decimal MarketPrice, decimal CostPrice, decimal ContractPrice, int EstimatedAnnualQuantity, decimal CustomerSaving, decimal CustomerSavingPercent, decimal MarginPercent);
public sealed record ContractTierDto(Guid Id, int MinQuantity, int? MaxQuantity, decimal AdditionalDiscountPercent);
public sealed record ContractAttachmentDto(Guid Id, string Type, string FileName, string StoragePath, string ContentType, long SizeBytes, DateTime CreatedAt);
public sealed record ContractApprovalDto(Guid Id, int Sequence, string RoleCode, string LabelAr, string? AssignedUser, string Status, string? DecidedBy, DateTime? DecidedAt, string? DecisionNote);
public sealed record ContractRevisionItemDto(Guid ProductId, string Product, decimal OldPrice, decimal NewPrice, string? Reason);
public sealed record ContractRevisionDto(Guid Id, DateTime EffectiveAt, DateTime? CustomerApprovedAt, string Reason, string Status, IReadOnlyList<ContractRevisionItemDto> Items);
public sealed record ContractHealthDto(decimal AnnualCustomerSaving, decimal AverageMarginPercent, string Rating, decimal ConsumptionPercent, int Orders, decimal TotalPurchases);
public sealed record ContractDetailDto(ContractRowDto Contract, string PricingMode, decimal MarketDiscountPercent, int PaymentTermsDays, decimal CreditLimit, bool AutoRenew, bool RenewalRequiresApproval, int ExpiryAlertDays, decimal EarlyPaymentDiscountPercent, int EarlyPaymentDays, decimal LatePaymentPenaltyPercent, string PaymentMethod, string DeliveryPriority, int DeliveryHours, decimal DeliveryLatePenaltyPercent, bool FreeShipping, int CreditReviewMonths, string? TermsSummary, DateTime? ActivatedAt, DateTime? CustomerNotifiedAt, IReadOnlyList<ContractProductDto> Products, IReadOnlyList<ContractTierDto> QuantityTiers, IReadOnlyList<ContractAttachmentDto> Attachments, IReadOnlyList<ContractApprovalDto> Approvals, IReadOnlyList<ContractRevisionDto> PriceRevisions, ContractHealthDto Health);

public sealed record SaveContractProductDto(Guid ProductId, decimal ContractPrice, int EstimatedAnnualQuantity);
public sealed record SaveContractTierDto(int MinQuantity, int? MaxQuantity, decimal AdditionalDiscountPercent);
public sealed record SaveContractDto(Guid CompanyId, string Type, DateTime StartsAt, DateTime EndsAt, bool AutoRenew, bool RenewalRequiresApproval, int ExpiryAlertDays, string PricingMode, decimal MarketDiscountPercent, decimal AnnualValue, int PaymentTermsDays, decimal CreditLimit, decimal EarlyPaymentDiscountPercent, int EarlyPaymentDays, decimal LatePaymentPenaltyPercent, string PaymentMethod, string DeliveryPriority, int DeliveryHours, decimal DeliveryLatePenaltyPercent, bool FreeShipping, int CreditReviewMonths, string? TermsSummary, IReadOnlyList<SaveContractProductDto> Products, IReadOnlyList<SaveContractTierDto> QuantityTiers);
public sealed record AddContractAttachmentDto(string Type, string FileName, string StoragePath, string ContentType, long SizeBytes);
public sealed record DecideContractApprovalDto(string Decision, string? Note);
public sealed record ActivateContractDto(bool ApplyPricesImmediately, bool NotifyCustomer);
public sealed record RenewContractDto(int Months, decimal PriceAdjustmentPercent, decimal? CreditLimit, string? Note);
public sealed record SaveContractRevisionItemDto(Guid ProductId, decimal NewPrice, string? Reason);
public sealed record CreateContractPriceRevisionDto(DateTime EffectiveAt, bool CustomerApproved, string Reason, IReadOnlyList<SaveContractRevisionItemDto> Items);
