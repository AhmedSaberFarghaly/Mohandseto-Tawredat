import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'api_client.dart';

class BudgetPointModel {
  const BudgetPointModel(this.label, this.amount);
  factory BudgetPointModel.fromJson(Map<String, dynamic> j) =>
      BudgetPointModel(j['label'] as String, (j['amount'] as num).toDouble());
  final String label;
  final double amount;
}

class BudgetAlertModel {
  const BudgetAlertModel(
    this.severity,
    this.title,
    this.body,
    this.centerId,
    this.at,
  );
  factory BudgetAlertModel.fromJson(Map<String, dynamic> j) => BudgetAlertModel(
    j['severity'] as String,
    j['title'] as String,
    j['body'] as String,
    j['costCenterId'] as String?,
    DateTime.parse(j['at'] as String),
  );
  final String severity, title, body;
  final String? centerId;
  final DateTime at;
}

class BudgetCenterModel {
  const BudgetCenterModel({
    required this.id,
    required this.code,
    required this.name,
    required this.budget,
    required this.used,
    required this.reserved,
    required this.available,
    required this.utilization,
    required this.health,
    required this.start,
    required this.end,
  });
  factory BudgetCenterModel.fromJson(Map<String, dynamic> j) =>
      BudgetCenterModel(
        id: j['id'] as String,
        code: j['code'] as String,
        name: j['name'] as String,
        budget: (j['budget'] as num).toDouble(),
        used: (j['used'] as num).toDouble(),
        reserved: (j['reserved'] as num).toDouble(),
        available: (j['available'] as num).toDouble(),
        utilization: (j['utilization'] as num).toDouble(),
        health: j['health'] as String,
        start: DateTime.parse(j['periodStart'] as String),
        end: DateTime.parse(j['periodEnd'] as String),
      );
  final String id, code, name, health;
  final double budget, used, reserved, available, utilization;
  final DateTime start, end;
}

class BudgetSummaryModel {
  const BudgetSummaryModel({
    required this.year,
    required this.month,
    required this.total,
    required this.used,
    required this.reserved,
    required this.available,
    required this.utilization,
    required this.forecast,
    required this.months,
    required this.categories,
    required this.centers,
    required this.alerts,
  });
  factory BudgetSummaryModel.fromJson(Map<String, dynamic> j) =>
      BudgetSummaryModel(
        year: j['year'] as int,
        month: j['month'] as int?,
        total: (j['totalBudget'] as num).toDouble(),
        used: (j['used'] as num).toDouble(),
        reserved: (j['reserved'] as num).toDouble(),
        available: (j['available'] as num).toDouble(),
        utilization: (j['utilization'] as num).toDouble(),
        forecast: (j['forecastEnd'] as num).toDouble(),
        months: (j['monthlyTrend'] as List)
            .map((e) => BudgetPointModel.fromJson(e as Map<String, dynamic>))
            .toList(),
        categories: (j['categoryBreakdown'] as List)
            .map((e) => BudgetPointModel.fromJson(e as Map<String, dynamic>))
            .toList(),
        centers: (j['centers'] as List)
            .map((e) => BudgetCenterModel.fromJson(e as Map<String, dynamic>))
            .toList(),
        alerts: (j['alerts'] as List)
            .map((e) => BudgetAlertModel.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
  final int year;
  final int? month;
  final double total, used, reserved, available, utilization, forecast;
  final List<BudgetPointModel> months, categories;
  final List<BudgetCenterModel> centers;
  final List<BudgetAlertModel> alerts;
}

class BudgetOrderModel {
  const BudgetOrderModel(
    this.id,
    this.number,
    this.department,
    this.project,
    this.total,
    this.status,
    this.createdAt,
  );
  factory BudgetOrderModel.fromJson(Map<String, dynamic> j) => BudgetOrderModel(
    j['orderId'] as String,
    j['number'] as String,
    j['department'] as String,
    j['project'] as String?,
    (j['total'] as num).toDouble(),
    j['status'] as String,
    DateTime.parse(j['createdAt'] as String),
  );
  final String id, number, department, status;
  final String? project;
  final double total;
  final DateTime createdAt;
}

class BudgetCenterDetailModel {
  const BudgetCenterDetailModel(
    this.center,
    this.orders,
    this.departments,
    this.months,
    this.average,
    this.forecast,
  );
  factory BudgetCenterDetailModel.fromJson(Map<String, dynamic> j) =>
      BudgetCenterDetailModel(
        BudgetCenterModel.fromJson(j['center'] as Map<String, dynamic>),
        (j['orders'] as List)
            .map((e) => BudgetOrderModel.fromJson(e as Map<String, dynamic>))
            .toList(),
        (j['departmentBreakdown'] as List)
            .map((e) => BudgetPointModel.fromJson(e as Map<String, dynamic>))
            .toList(),
        (j['monthlyTrend'] as List)
            .map((e) => BudgetPointModel.fromJson(e as Map<String, dynamic>))
            .toList(),
        (j['averageMonthlySpend'] as num).toDouble(),
        (j['forecastEnd'] as num).toDouble(),
      );
  final BudgetCenterModel center;
  final List<BudgetOrderModel> orders;
  final List<BudgetPointModel> departments, months;
  final double average, forecast;
}

class BudgetRepository {
  BudgetRepository(this._api);
  final ApiClient _api;
  Future<BudgetSummaryModel> summary(int year, [int? month]) async =>
      BudgetSummaryModel.fromJson(
        (await _api.dio.get(
              '/api/budgets/summary',
              queryParameters: {
                'year': year,
                if (month != null) 'month': month,
              },
            )).data
            as Map<String, dynamic>,
      );
  Future<BudgetCenterDetailModel> center(String id) async =>
      BudgetCenterDetailModel.fromJson(
        (await _api.dio.get('/api/budgets/centers/$id')).data
            as Map<String, dynamic>,
      );
  Future<void> adjustment(
    String centerId,
    double requested,
    String reason,
  ) async => _api.dio.post(
    '/api/budgets/adjustment-requests',
    data: {
      'costCenterId': centerId,
      'requestedBudget': requested,
      'reason': reason,
    },
  );
}

final budgetRepositoryProvider = Provider(
  (ref) => BudgetRepository(ref.watch(apiClientProvider)),
);
final budgetSummaryProvider = FutureProvider.family<BudgetSummaryModel, int>(
  (ref, year) => ref.watch(budgetRepositoryProvider).summary(year),
);
final budgetCenterProvider =
    FutureProvider.family<BudgetCenterDetailModel, String>(
      (ref, id) => ref.watch(budgetRepositoryProvider).center(id),
    );
