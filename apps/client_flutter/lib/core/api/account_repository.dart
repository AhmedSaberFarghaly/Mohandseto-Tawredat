import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'api_client.dart';

typedef Json = Map<String, dynamic>;

class AccountProfileModel {
  const AccountProfileModel({
    required this.id,
    required this.name,
    required this.phone,
    this.email,
    this.avatar,
    required this.language,
    this.jobTitle,
    this.department,
    this.purchaseLimit,
    this.branchId,
    required this.roles,
  });
  factory AccountProfileModel.fromJson(Json j) => AccountProfileModel(
    id: j['id'] as String,
    name: j['fullName'] as String,
    phone: j['phone'] as String,
    email: j['email'] as String?,
    avatar: j['avatarPath'] as String?,
    language: j['language'] as String? ?? 'ar',
    jobTitle: j['jobTitle'] as String?,
    department: j['department'] as String?,
    purchaseLimit: (j['purchaseLimit'] as num?)?.toDouble(),
    branchId: j['defaultBranchId'] as String?,
    roles: List<String>.from(j['roles'] as List),
  );
  final String id, name, phone, language;
  final String? email, avatar, jobTitle, department, branchId;
  final double? purchaseLimit;
  final List<String> roles;
}

class AccountCompanyModel {
  const AccountCompanyModel({
    required this.id,
    required this.name,
    this.nameEn,
    this.registrationNo,
    this.taxNo,
    required this.phone,
    this.email,
    this.governorate,
    this.city,
    this.address,
    this.industry,
    required this.employees,
    required this.status,
  });
  factory AccountCompanyModel.fromJson(Json j) => AccountCompanyModel(
    id: j['id'] as String,
    name: j['legalName'] as String,
    nameEn: j['legalNameEn'] as String?,
    registrationNo: j['commercialRegistrationNo'] as String?,
    taxNo: j['taxCardNo'] as String?,
    phone: j['phone'] as String,
    email: j['email'] as String?,
    governorate: j['governorate'] as String?,
    city: j['city'] as String?,
    address: j['addressLine'] as String?,
    industry: j['industry'] as String?,
    employees: j['employeeCountRange'] as int? ?? 0,
    status: j['status'] as String,
  );
  final String id, name, phone, status;
  final String? nameEn,
      registrationNo,
      taxNo,
      email,
      governorate,
      city,
      address,
      industry;
  final int employees;
}

class AccountOverviewModel {
  const AccountOverviewModel(
    this.profile,
    this.company,
    this.users,
    this.branches,
    this.invites,
    this.creditLimit,
    this.creditUsed,
    this.contractEnds,
  );
  factory AccountOverviewModel.fromJson(Json j) => AccountOverviewModel(
    AccountProfileModel.fromJson(j['profile'] as Json),
    AccountCompanyModel.fromJson(j['company'] as Json),
    j['users'] as int,
    j['branches'] as int,
    j['pendingInvites'] as int,
    (j['creditLimit'] as num).toDouble(),
    (j['creditUsed'] as num).toDouble(),
    j['contractEndsAt'] == null
        ? null
        : DateTime.parse(j['contractEndsAt'] as String),
  );
  final AccountProfileModel profile;
  final AccountCompanyModel company;
  final int users, branches, invites;
  final double creditLimit, creditUsed;
  final DateTime? contractEnds;
}

class AccountBranchModel {
  const AccountBranchModel({
    required this.id,
    required this.name,
    this.governorate,
    this.city,
    this.address,
    this.phone,
    this.latitude,
    this.longitude,
    required this.main,
  });
  factory AccountBranchModel.fromJson(Json j) => AccountBranchModel(
    id: j['id'] as String,
    name: j['name'] as String,
    governorate: j['governorate'] as String?,
    city: j['city'] as String?,
    address: j['addressLine'] as String?,
    phone: j['phone'] as String?,
    latitude: (j['latitude'] as num?)?.toDouble(),
    longitude: (j['longitude'] as num?)?.toDouble(),
    main: j['isMain'] as bool,
  );
  final String id, name;
  final String? governorate, city, address, phone;
  final double? latitude, longitude;
  final bool main;
  Json toPayload() => {
    'name': name,
    'governorate': governorate,
    'city': city,
    'addressLine': address,
    'phone': phone,
    'latitude': latitude,
    'longitude': longitude,
    'isMain': main,
  };
}

class AccountDocumentModel {
  const AccountDocumentModel(
    this.id,
    this.type,
    this.name,
    this.size,
    this.status,
    this.rejection,
    this.createdAt,
  );
  factory AccountDocumentModel.fromJson(Json j) => AccountDocumentModel(
    j['id'] as String,
    j['type'] as String,
    j['fileName'] as String,
    j['sizeBytes'] as int,
    j['reviewStatus'] as String,
    j['rejectionReason'] as String?,
    DateTime.parse(j['createdAt'] as String),
  );
  final String id, type, name, status;
  final int size;
  final String? rejection;
  final DateTime createdAt;
}

class AccountRoleModel {
  const AccountRoleModel(
    this.id,
    this.code,
    this.name,
    this.nameEn,
    this.system,
    this.users,
    this.permissions,
  );
  factory AccountRoleModel.fromJson(Json j) => AccountRoleModel(
    j['id'] as String,
    j['code'] as String,
    j['nameAr'] as String,
    j['nameEn'] as String,
    j['isSystem'] as bool,
    j['usersCount'] as int,
    List<String>.from(j['permissions'] as List),
  );
  final String id, code, name, nameEn;
  final bool system;
  final int users;
  final List<String> permissions;
}

class AccountUserModel {
  const AccountUserModel({
    required this.id,
    required this.name,
    required this.phone,
    this.email,
    required this.active,
    this.jobTitle,
    this.department,
    this.limit,
    this.branchId,
    required this.roles,
    required this.createdAt,
  });
  factory AccountUserModel.fromJson(Json j) => AccountUserModel(
    id: j['id'] as String,
    name: j['fullName'] as String,
    phone: j['phone'] as String,
    email: j['email'] as String?,
    active: j['isActive'] as bool,
    jobTitle: j['jobTitle'] as String?,
    department: j['department'] as String?,
    limit: (j['purchaseLimit'] as num?)?.toDouble(),
    branchId: j['defaultBranchId'] as String?,
    roles: (j['roles'] as List)
        .map(
          (e) => AccountRoleModel(
            (e as Json)['id'] as String,
            e['code'] as String,
            e['name'] as String,
            '',
            true,
            0,
            const [],
          ),
        )
        .toList(),
    createdAt: DateTime.parse(j['createdAt'] as String),
  );
  final String id, name, phone;
  final String? email, jobTitle, department, branchId;
  final bool active;
  final double? limit;
  final List<AccountRoleModel> roles;
  final DateTime createdAt;
}

class AccountPermissionModel {
  const AccountPermissionModel(
    this.id,
    this.code,
    this.module,
    this.description,
  );
  factory AccountPermissionModel.fromJson(Json j) => AccountPermissionModel(
    j['id'] as int,
    j['code'] as String,
    j['module'] as String,
    j['description'] as String,
  );
  final int id;
  final String code, module, description;
}

class ApprovalLevelAccountModel {
  const ApprovalLevelAccountModel(
    this.id,
    this.sequence,
    this.name,
    this.limit,
    this.sla,
    this.approvers,
  );
  factory ApprovalLevelAccountModel.fromJson(Json j) =>
      ApprovalLevelAccountModel(
        j['id'] as String?,
        j['sequence'] as int,
        j['name'] as String,
        (j['authorityLimit'] as num?)?.toDouble(),
        j['slaHours'] as int,
        List<String>.from(j['approverUserIds'] as List),
      );
  final String? id;
  final int sequence, sla;
  final String name;
  final double? limit;
  final List<String> approvers;
  Json toPayload() => {
    'id': id,
    'sequence': sequence,
    'name': name,
    'authorityLimit': limit,
    'slaHours': sla,
    'approverUserIds': approvers,
  };
}

class ApprovalPolicyAccountModel {
  const ApprovalPolicyAccountModel(
    this.id,
    this.name,
    this.minimum,
    this.budgetConflict,
    this.active,
    this.levels,
  );
  factory ApprovalPolicyAccountModel.fromJson(Json j) =>
      ApprovalPolicyAccountModel(
        j['id'] as String,
        j['name'] as String,
        (j['minimumAmount'] as num).toDouble(),
        j['appliesOnBudgetConflict'] as bool,
        j['isActive'] as bool,
        (j['levels'] as List)
            .map((e) => ApprovalLevelAccountModel.fromJson(e as Json))
            .toList(),
      );
  final String id, name;
  final double minimum;
  final bool budgetConflict, active;
  final List<ApprovalLevelAccountModel> levels;
}

class AccountCostCenterModel {
  const AccountCostCenterModel(
    this.id,
    this.code,
    this.name,
    this.budget,
    this.used,
    this.reserved,
    this.threshold,
    this.active,
  );
  factory AccountCostCenterModel.fromJson(Json j) => AccountCostCenterModel(
    j['id'] as String,
    j['code'] as String,
    j['name'] as String,
    (j['budget'] as num).toDouble(),
    (j['used'] as num).toDouble(),
    (j['reserved'] as num).toDouble(),
    (j['approvalThreshold'] as num?)?.toDouble(),
    j['isActive'] as bool,
  );
  final String id, code, name;
  final double budget, used, reserved;
  final double? threshold;
  final bool active;
}

class AccountAuditModel {
  const AccountAuditModel(
    this.id,
    this.at,
    this.userId,
    this.userName,
    this.action,
    this.entity,
    this.entityId,
  );
  factory AccountAuditModel.fromJson(Json j) => AccountAuditModel(
    j['id'] as int,
    DateTime.parse(j['at'] as String),
    j['userId'] as String?,
    j['userName'] as String?,
    j['action'] as String,
    j['entityType'] as String,
    j['entityId'] as String?,
  );
  final int id;
  final DateTime at;
  final String? userId, userName, entityId;
  final String action, entity;
}

class BrandProfileModel {
  const BrandProfileModel(
    this.logo,
    this.primary,
    this.secondary,
    this.nameAr,
    this.nameEn,
  );
  factory BrandProfileModel.fromJson(Json j) => BrandProfileModel(
    j['logoPath'] as String?,
    j['primaryColor'] as String,
    j['secondaryColor'] as String,
    j['brandNameAr'] as String?,
    j['brandNameEn'] as String?,
  );
  final String? logo, nameAr, nameEn;
  final String primary, secondary;
}

class BillingProfileModel {
  const BillingProfileModel(
    this.name,
    this.email,
    this.taxNo,
    this.address,
    this.days,
    this.poRequired,
  );
  factory BillingProfileModel.fromJson(Json j) => BillingProfileModel(
    j['invoiceLegalName'] as String,
    j['billingEmail'] as String?,
    j['taxRegistrationNo'] as String?,
    j['taxAddress'] as String?,
    j['paymentTermsDays'] as int,
    j['purchaseOrderRequired'] as bool,
  );
  final String name;
  final String? email, taxNo, address;
  final int days;
  final bool poRequired;
}

class CompanyContractModel {
  const CompanyContractModel(
    this.id,
    this.number,
    this.start,
    this.end,
    this.status,
    this.days,
    this.credit,
    this.autoRenew,
    this.summary,
    this.document,
    this.remaining,
  );
  factory CompanyContractModel.fromJson(Json j) => CompanyContractModel(
    j['id'] as String,
    j['number'] as String,
    DateTime.parse(j['startsAt'] as String),
    DateTime.parse(j['endsAt'] as String),
    j['status'] as String,
    j['paymentTermsDays'] as int,
    (j['creditLimit'] as num).toDouble(),
    j['autoRenew'] as bool,
    j['termsSummary'] as String?,
    j['documentPath'] as String?,
    j['daysRemaining'] as int,
  );
  final String id, number, status;
  final DateTime start, end;
  final int days, remaining;
  final double credit;
  final bool autoRenew;
  final String? summary, document;
}

class AccountRepository {
  AccountRepository(this._api);
  final ApiClient _api;
  Future<AccountOverviewModel> overview() async =>
      AccountOverviewModel.fromJson(
        (await _api.dio.get('/api/account')).data as Json,
      );
  Future<AccountProfileModel> updateProfile(Json data) async =>
      AccountProfileModel.fromJson(
        (await _api.dio.put('/api/account/profile', data: data)).data as Json,
      );
  Future<AccountCompanyModel> company() async => AccountCompanyModel.fromJson(
    (await _api.dio.get('/api/account/company')).data as Json,
  );
  Future<AccountCompanyModel> updateCompany(Json data) async =>
      AccountCompanyModel.fromJson(
        (await _api.dio.put('/api/account/admin/company', data: data)).data
            as Json,
      );
  Future<List<AccountBranchModel>> branches() async =>
      ((await _api.dio.get('/api/account/branches')).data as List)
          .map((e) => AccountBranchModel.fromJson(e as Json))
          .toList();
  Future<void> saveBranch(
    AccountBranchModel branch, {
    bool create = false,
  }) async => create
      ? _api.dio.post('/api/account/admin/branches', data: branch.toPayload())
      : _api.dio.put(
          '/api/account/admin/branches/${branch.id}',
          data: branch.toPayload(),
        );
  Future<void> deleteBranch(String id) async =>
      _api.dio.delete('/api/account/admin/branches/$id');
  Future<List<AccountDocumentModel>> documents() async =>
      ((await _api.dio.get('/api/account/documents')).data as List)
          .map((e) => AccountDocumentModel.fromJson(e as Json))
          .toList();
  Future<List<AccountUserModel>> users() async =>
      ((await _api.dio.get('/api/account/admin/users')).data as List)
          .map((e) => AccountUserModel.fromJson(e as Json))
          .toList();
  Future<void> createUser(Json data) async =>
      _api.dio.post('/api/account/admin/users', data: data);
  Future<void> updateUser(String id, Json data) async =>
      _api.dio.put('/api/account/admin/users/$id', data: data);
  Future<void> setActive(String id, bool active) async => _api.dio.post(
    '/api/account/admin/users/$id/${active ? 'activate' : 'deactivate'}',
  );
  Future<Json> invite(Json data) async =>
      (await _api.dio.post('/api/account/admin/invites', data: data)).data
          as Json;
  Future<List<AccountRoleModel>> roles() async =>
      ((await _api.dio.get('/api/account/admin/roles')).data as List)
          .map((e) => AccountRoleModel.fromJson(e as Json))
          .toList();
  Future<List<AccountPermissionModel>> permissions() async =>
      ((await _api.dio.get('/api/account/admin/permissions')).data as List)
          .map((e) => AccountPermissionModel.fromJson(e as Json))
          .toList();
  Future<void> createRole(String ar, String en, List<int> ids) async =>
      _api.dio.post(
        '/api/account/admin/roles',
        data: {'nameAr': ar, 'nameEn': en, 'permissionIds': ids},
      );
  Future<void> updateRole(String id, List<int> ids) async => _api.dio.put(
    '/api/account/admin/roles/$id/permissions',
    data: {'permissionIds': ids},
  );
  Future<List<ApprovalPolicyAccountModel>> policies() async =>
      ((await _api.dio.get('/api/account/admin/approval-policies')).data
              as List)
          .map((e) => ApprovalPolicyAccountModel.fromJson(e as Json))
          .toList();
  Future<void> updatePolicy(
    ApprovalPolicyAccountModel p,
    List<ApprovalLevelAccountModel> levels,
  ) async => _api.dio.put(
    '/api/account/admin/approval-policies/${p.id}',
    data: {
      'name': p.name,
      'minimumAmount': p.minimum,
      'appliesOnBudgetConflict': p.budgetConflict,
      'isActive': p.active,
      'levels': levels.map((e) => e.toPayload()).toList(),
    },
  );
  Future<List<AccountCostCenterModel>> centers() async =>
      ((await _api.dio.get('/api/account/admin/cost-centers')).data as List)
          .map((e) => AccountCostCenterModel.fromJson(e as Json))
          .toList();
  Future<void> saveCenter(Json data, {String? id}) async => id == null
      ? _api.dio.post('/api/account/admin/cost-centers', data: data)
      : _api.dio.put('/api/account/admin/cost-centers/$id', data: data);
  Future<List<AccountAuditModel>> audit([String? userId]) async =>
      ((await _api.dio.get(
                '/api/account/admin/audit',
                queryParameters: {if (userId != null) 'userId': userId},
              )).data
              as List)
          .map((e) => AccountAuditModel.fromJson(e as Json))
          .toList();
  Future<BrandProfileModel> brand() async => BrandProfileModel.fromJson(
    (await _api.dio.get('/api/account/brand')).data as Json,
  );
  Future<void> updateBrand(Json data) async =>
      _api.dio.put('/api/account/admin/brand', data: data);
  Future<BillingProfileModel> billing() async => BillingProfileModel.fromJson(
    (await _api.dio.get('/api/account/billing')).data as Json,
  );
  Future<void> updateBilling(Json data) async =>
      _api.dio.put('/api/account/admin/billing', data: data);
  Future<List<CompanyContractModel>> contracts() async =>
      ((await _api.dio.get('/api/account/contracts')).data as List)
          .map((e) => CompanyContractModel.fromJson(e as Json))
          .toList();
  Future<void> renew(String id, int months, String note) async => _api.dio.post(
    '/api/account/contracts/$id/renewal-requests',
    data: {'requestedMonths': months, 'note': note},
  );
}

final accountRepositoryProvider = Provider(
  (ref) => AccountRepository(ref.watch(apiClientProvider)),
);
final accountOverviewProvider = FutureProvider(
  (ref) => ref.watch(accountRepositoryProvider).overview(),
);
final accountBranchesProvider = FutureProvider(
  (ref) => ref.watch(accountRepositoryProvider).branches(),
);
final accountDocumentsProvider = FutureProvider(
  (ref) => ref.watch(accountRepositoryProvider).documents(),
);
final accountUsersProvider = FutureProvider(
  (ref) => ref.watch(accountRepositoryProvider).users(),
);
final accountRolesProvider = FutureProvider(
  (ref) => ref.watch(accountRepositoryProvider).roles(),
);
final accountPermissionsProvider = FutureProvider(
  (ref) => ref.watch(accountRepositoryProvider).permissions(),
);
final accountPoliciesProvider = FutureProvider(
  (ref) => ref.watch(accountRepositoryProvider).policies(),
);
final accountCentersProvider = FutureProvider(
  (ref) => ref.watch(accountRepositoryProvider).centers(),
);
final accountAuditProvider = FutureProvider(
  (ref) => ref.watch(accountRepositoryProvider).audit(),
);
final accountBrandProvider = FutureProvider(
  (ref) => ref.watch(accountRepositoryProvider).brand(),
);
final accountBillingProvider = FutureProvider(
  (ref) => ref.watch(accountRepositoryProvider).billing(),
);
final accountContractsProvider = FutureProvider(
  (ref) => ref.watch(accountRepositoryProvider).contracts(),
);
