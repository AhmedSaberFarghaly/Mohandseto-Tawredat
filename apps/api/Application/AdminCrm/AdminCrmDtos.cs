namespace Mohandseto.Api.Application.AdminCrm;

public sealed record CrmKpisDto(int TotalCompanies, int ActiveCompanies, int Prospects, int AtRisk, decimal TotalSales, decimal Outstanding, int OpenTasks);
public sealed record CrmStaffDto(Guid Id, string Name, string? JobTitle);
public sealed record CrmProductOptionDto(Guid Id, string Sku, string Name, decimal BasePrice);
public sealed record CrmCompanyRowDto(Guid Id, Guid TenantId, string LegalName, string? LegalNameEn, string Phone, string? Email, string? CommercialRegistrationNo, string? TaxCardNo, string? Governorate, string? City, string? AddressLine, string? Industry, string? Sector, string? Activity, string Classification, string Size, string Stage, string Status, Guid? AssignedSalesRepUserId, string? AssignedSalesRep, string? LeadSource, decimal CreditLimit, decimal CreditUsed, decimal TotalPurchases, int OrderCount, int OpenTasks, DateTime CreatedAt);
public sealed record CrmDashboardDto(CrmKpisDto Kpis, IReadOnlyList<CrmCompanyRowDto> Companies, IReadOnlyList<CrmStaffDto> SalesStaff, IReadOnlyList<CrmProductOptionDto> Products);

public sealed record CrmBranchDto(Guid Id, string Name, string? Governorate, string? City, string? AddressLine, string? Phone, bool IsMain);
public sealed record CrmUserDto(Guid Id, string FullName, string Phone, string? Email, string? JobTitle, string? Department, bool IsActive, DateTime CreatedAt);
public sealed record CrmDocumentDto(Guid Id, string Type, string FileName, string StoragePath, string ReviewStatus, string? RejectionReason, DateTime CreatedAt);
public sealed record CrmActivityDto(Guid Id, string Type, string Subject, string? Details, DateTime OccurredAt, DateTime? NextFollowUpAt, string Actor, string? Contact);
public sealed record CrmTaskDto(Guid Id, string Title, string? Description, string AssignedTo, DateTime DueAt, string Status, string Priority, DateTime? CompletedAt);
public sealed record CrmStageDto(Guid Id, string From, string To, string? Reason, string ChangedBy, DateTime ChangedAt);
public sealed record CrmCommercialDocumentDto(Guid Id, string Number, string Status, decimal Total, DateTime At);
public sealed record CrmTopProductDto(Guid ProductId, string Sku, string Product, int Quantity, decimal Sales);
public sealed record CrmUpsellDto(Guid ProductId, string Sku, string Product, string Reason, decimal BasePrice);
public sealed record CrmContractDto(Guid Id, string Number, DateTime StartsAt, DateTime EndsAt, string Status, int PaymentTermsDays, decimal CreditLimit, bool AutoRenew);
public sealed record CrmSpecialPriceDto(Guid ProductId, string Sku, string Product, decimal BasePrice, decimal ContractPrice, DateTime? ValidFrom, DateTime? ValidTo);
public sealed record CrmStatementLineDto(Guid Id, string Number, string Type, DateTime At, DateTime? DueAt, decimal Debit, decimal Credit, decimal Balance, string Status);
public sealed record CrmTicketDto(Guid Id, string Number, string Subject, string Type, string Priority, string Status, DateTime CreatedAt, DateTime? ResolvedAt);
public sealed record CrmCompanyDetailDto(CrmCompanyRowDto Company, IReadOnlyList<CrmBranchDto> Branches, IReadOnlyList<CrmUserDto> Users, IReadOnlyList<CrmDocumentDto> Documents, IReadOnlyList<CrmActivityDto> Activities, IReadOnlyList<CrmTaskDto> Tasks, IReadOnlyList<CrmStageDto> StageHistory, IReadOnlyList<CrmCommercialDocumentDto> Quotes, IReadOnlyList<CrmCommercialDocumentDto> Orders, decimal AverageOrderValue, IReadOnlyList<CrmTopProductDto> TopProducts, IReadOnlyList<CrmUpsellDto> UpsellOpportunities, IReadOnlyList<CrmContractDto> Contracts, IReadOnlyList<CrmSpecialPriceDto> SpecialPrices, IReadOnlyList<CrmStatementLineDto> AccountStatement, IReadOnlyList<CrmTicketDto> SupportTickets);

public sealed record SaveCrmCompanyDto(string LegalName, string? LegalNameEn, string Phone, string? Email, string? CommercialRegistrationNo, string? TaxCardNo, string? Governorate, string? City, string? AddressLine, string? Industry, string? Sector, string? Activity, string Classification, string Size, Guid? AssignedSalesRepUserId, string? LeadSource, decimal CreditLimit, string? PrimaryContactName, string? PrimaryContactPhone, string? PrimaryContactEmail);
public sealed record ChangeCustomerStageDto(string Stage, string? Reason);
public sealed record AddCrmActivityDto(string Type, string Subject, string? Details, DateTime OccurredAt, DateTime? NextFollowUpAt, Guid? ContactUserId);
public sealed record AddCrmTaskDto(string Title, string? Description, Guid AssignedToUserId, DateTime DueAt, string Priority);
public sealed record AddCrmBranchDto(string Name, string? Governorate, string? City, string? AddressLine, string? Phone, bool IsMain);
public sealed record ReviewCrmDocumentDto(string Status, string? RejectionReason);
public sealed record SaveCrmPriceDto(Guid ProductId, decimal ContractPrice, DateTime? ValidFrom, DateTime? ValidTo);
public sealed record ReplaceCrmPricesDto(IReadOnlyList<SaveCrmPriceDto> Prices);
public sealed record UpdateCreditLimitDto(decimal CreditLimit);
public sealed record ChangeCompanyStatusDto(string Status, string? Reason);
