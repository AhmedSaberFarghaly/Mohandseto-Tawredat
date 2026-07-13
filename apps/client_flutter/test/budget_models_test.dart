import 'package:flutter_test/flutter_test.dart';
import 'package:mohandseto_client/core/api/budget_repository.dart';

void main() {
  test('budget summary parses centers trend categories and alerts', () {
    final summary = BudgetSummaryModel.fromJson({
      'year': 2026, 'month': null, 'totalBudget': 100000, 'used': 70000, 'reserved': 10000,
      'available': 20000, 'utilization': 80, 'forecastEnd': 110000,
      'monthlyTrend': [{'label': 'Jan', 'amount': 10000}],
      'categoryBreakdown': [{'label': 'المشتريات', 'amount': 50000}],
      'centers': [{'id': 'c1', 'code': 'OPS', 'name': 'التشغيل', 'budget': 100000,
        'used': 70000, 'reserved': 10000, 'available': 20000, 'utilization': 80,
        'health': 'Warning', 'periodStart': '2026-01-01T00:00:00Z', 'periodEnd': '2026-12-31T00:00:00Z'}],
      'alerts': [{'severity': 'Warning', 'title': 'تنبيه', 'body': 'تم استخدام 80%', 'costCenterId': 'c1', 'at': '2026-07-13T00:00:00Z'}],
    });
    expect(summary.centers.single.health, 'Warning');
    expect(summary.categories.single.amount, 50000);
    expect(summary.alerts.single.centerId, 'c1');
  });

  test('budget center detail parses order allocations and forecast', () {
    final detail = BudgetCenterDetailModel.fromJson({
      'center': {'id': 'c1', 'code': 'OPS', 'name': 'التشغيل', 'budget': 100000,
        'used': 70000, 'reserved': 0, 'available': 30000, 'utilization': 70, 'health': 'Healthy',
        'periodStart': '2026-01-01T00:00:00Z', 'periodEnd': '2026-12-31T00:00:00Z'},
      'orders': [{'orderId': 'o1', 'number': 'ORD-1', 'department': 'المشتريات', 'project': null,
        'total': 5000, 'status': 'Confirmed', 'createdAt': '2026-07-13T00:00:00Z'}],
      'departmentBreakdown': [{'label': 'المشتريات', 'amount': 5000}],
      'monthlyTrend': [{'label': 'Jul', 'amount': 5000}], 'averageMonthlySpend': 10000, 'forecastEnd': 120000,
    });
    expect(detail.orders.single.total, 5000);
    expect(detail.forecast, 120000);
  });
}
