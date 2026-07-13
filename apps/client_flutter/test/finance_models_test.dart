import 'package:flutter_test/flutter_test.dart';
import 'package:mohandseto_client/core/api/finance_repository.dart';

void main() {
  test('invoice detail parses tax lines payments and outstanding amount', () {
    final invoice = InvoiceDetailModel.fromJson({
      'id': 'i1', 'number': 'INV-1', 'orderId': 'o1', 'orderNumber': 'ORD-1', 'status': 'PartiallyPaid',
      'type': 'Tax', 'issuedAt': '2026-07-13T00:00:00Z', 'dueAt': '2026-08-12T00:00:00Z',
      'currency': 'EGP', 'sellerTaxNumber': 'TAX-1', 'buyerTaxNumber': null, 'etaUuid': 'ETA-1',
      'subtotal': 1000, 'discount': 50, 'tax': 140, 'shipping': 20, 'total': 1110,
      'paidAmount': 500, 'outstanding': 610, 'electronicQrPayload': 'EGS|INV-1',
      'lines': [{'id': 'l1', 'sku': 'SKU', 'description': 'منتج', 'quantity': 10, 'unitPrice': 100, 'taxAmount': 140, 'lineTotal': 1000}],
      'payments': [{'id': 'p1', 'amount': 500, 'method': 'BankTransfer', 'status': 'Completed', 'reference': 'PAY-1',
        'bankReference': 'BANK-1', 'createdAt': '2026-07-14T00:00:00Z', 'verifiedAt': '2026-07-15T00:00:00Z',
        'hasReceipt': true, 'rejectionReason': null}],
    });
    expect(invoice.outstanding, 610);
    expect(invoice.lines.single.tax, 140);
    expect(invoice.payments.single.hasReceipt, true);
    expect(invoice.etaUuid, 'ETA-1');
  });

  test('finance summary parses credit utilization calendar and payments', () {
    final summary = FinanceSummaryModel.fromJson({
      'outstanding': 30000, 'overdue': 5000, 'dueSoon': 10000, 'openInvoices': 3,
      'creditLimit': 50000, 'creditUsed': 30000, 'creditAvailable': 20000, 'creditUtilization': 60,
      'upcoming': [{'id': 'i1', 'number': 'INV-1', 'orderNumber': 'ORD-1', 'status': 'Issued', 'type': 'Tax',
        'total': 10000, 'paidAmount': 0, 'outstanding': 10000, 'issuedAt': '2026-07-13T00:00:00Z',
        'dueAt': '2026-08-12T00:00:00Z', 'isOverdue': false}],
      'recentPayments': [],
    });
    expect(summary.utilization, 60);
    expect(summary.upcoming.single.outstanding, 10000);
  });
}
