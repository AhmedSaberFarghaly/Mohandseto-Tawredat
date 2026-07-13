import 'package:flutter_test/flutter_test.dart';
import 'package:mohandseto_client/core/api/account_repository.dart';

void main() {
  test('account overview maps company governance metrics', () {
    final model = AccountOverviewModel.fromJson({
      'profile': {
        'id': 'u1', 'fullName': 'أحمد محمد', 'phone': '+201000000000',
        'email': 'a@example.com', 'avatarPath': null, 'language': 'ar',
        'jobTitle': 'مدير مشتريات', 'department': 'المشتريات',
        'purchaseLimit': 25000, 'defaultBranchId': 'b1',
        'roles': ['company_admin'],
      },
      'company': {
        'id': 'c1', 'legalName': 'شركة التوريدات', 'legalNameEn': 'Supplies Co',
        'commercialRegistrationNo': 'CR-1', 'taxCardNo': 'TAX-1',
        'phone': '+201000000001', 'email': 'billing@example.com',
        'governorate': 'القاهرة', 'city': 'مدينة نصر', 'addressLine': 'شارع 1',
        'industry': 'توريدات', 'employeeCountRange': 50, 'status': 'Active',
      },
      'users': 12, 'branches': 3, 'pendingInvites': 2,
      'creditLimit': 100000, 'creditUsed': 35000,
      'contractEndsAt': '2027-01-01T00:00:00Z',
    });
    expect(model.profile.purchaseLimit, 25000);
    expect(model.company.status, 'Active');
    expect(model.creditLimit - model.creditUsed, 65000);
    expect(model.contractEnds, isNotNull);
  });

  test('roles, policies and contracts map nested access settings', () {
    final role = AccountRoleModel.fromJson({
      'id': 'r1', 'code': 'purchasing', 'nameAr': 'مشتريات',
      'nameEn': 'Purchasing', 'isSystem': false, 'usersCount': 4,
      'permissions': ['orders.create', 'rfq.create'],
    });
    final policy = ApprovalPolicyAccountModel.fromJson({
      'id': 'p1', 'name': 'اعتماد الطلبات', 'minimumAmount': 1000,
      'appliesOnBudgetConflict': true, 'isActive': true,
      'levels': [
        {'id': 'l1', 'sequence': 1, 'name': 'مدير القسم', 'authorityLimit': 20000, 'slaHours': 12, 'approverUserIds': ['u1']},
      ],
    });
    final contract = CompanyContractModel.fromJson({
      'id': 'ct1', 'number': 'CTR-100', 'startsAt': '2026-01-01T00:00:00Z',
      'endsAt': '2026-12-31T00:00:00Z', 'status': 'Active',
      'paymentTermsDays': 30, 'creditLimit': 250000,
      'autoRenew': false, 'termsSummary': 'توريد سنوي',
      'documentPath': null, 'daysRemaining': 90,
    });
    expect(role.permissions, contains('rfq.create'));
    expect(policy.levels.single.approvers.single, 'u1');
    expect(contract.credit, 250000);
  });
}
