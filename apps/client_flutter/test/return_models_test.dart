import 'package:flutter_test/flutter_test.dart';
import 'package:mohandseto_client/core/api/return_repository.dart';

void main() {
  test('eligible return order parses remaining quantities and dates', () {
    final order = EligibleReturnOrder.fromJson({
      'orderId': 'o1', 'number': 'ORD-1', 'deliveredAt': '2026-07-10T00:00:00Z',
      'eligibleUntil': '2026-08-09T00:00:00Z', 'deliveryAddress': 'Cairo',
      'items': [{'orderItemId': 'i1', 'sku': 'SKU', 'nameAr': 'منتج', 'orderedQuantity': 10,
        'returnedQuantity': 2, 'eligibleQuantity': 8, 'unitPrice': 100}],
    });
    expect(order.items.single.eligible, 8);
    expect(order.eligibleUntil.month, 8);
  });

  test('return detail parses pickup inspection and refund lifecycle', () {
    final detail = ReturnDetailModel.fromJson({
      'id': 'r1', 'number': 'RET-1', 'orderId': 'o1', 'orderNumber': 'ORD-1',
      'status': 'RefundApproved', 'resolution': 'Refund', 'refundMethod': 'OriginalPayment',
      'requestedTotal': 200, 'approvedTotal': 190, 'pickupAddress': 'Cairo',
      'pickupAt': '2026-07-20T10:00:00Z', 'pickupWindow': '10-12', 'pickupDriverName': 'Driver',
      'pickupDriverPhone': '0111', 'pickupLatitude': 30.1, 'pickupLongitude': 31.2,
      'rejectionReason': null, 'inspectionNotes': 'اجتاز الفحص', 'submittedAt': '2026-07-15T00:00:00Z',
      'receivedAt': '2026-07-20T12:00:00Z', 'completedAt': null,
      'items': [{'id': 'ri1', 'orderItemId': 'i1', 'sku': 'SKU', 'nameAr': 'منتج', 'quantity': 2,
        'reason': 'Damaged', 'description': 'تالف', 'unitRefund': 100, 'lineRefund': 200,
        'isEligible': true, 'eligibilityNote': null, 'inspectionPassed': true}],
      'attachments': [{'id': 'a1'}],
      'history': [{'status': 'Submitted', 'note': 'تم الإرسال', 'at': '2026-07-15T00:00:00Z'}],
      'canEdit': false, 'canCancel': false, 'canTrackPickup': false,
    });
    expect(detail.approvedTotal, 190);
    expect(detail.items.single.inspectionPassed, true);
    expect(detail.attachmentCount, 1);
    expect(detail.driverName, 'Driver');
  });
}
