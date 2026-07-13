import 'package:flutter_test/flutter_test.dart';
import 'package:mohandseto_client/core/api/order_repository.dart';

void main() {
  test('order detail parses tracking proof issue rating and business data', () {
    final order = OrderDetailModel.fromJson({
      'id': 'o1',
      'number': 'ORD-1',
      'status': 'OutForDelivery',
      'subtotal': 1000,
      'savings': 50,
      'couponDiscount': 0,
      'taxIncluded': 140,
      'shipping': 25,
      'total': 1115,
      'branchName': 'المقر',
      'deliveryAddress': 'القاهرة',
      'receiverName': 'أحمد',
      'receiverPhone': '0100',
      'requiredDate': '2026-07-20T00:00:00Z',
      'timeSlot': null,
      'shippingMethod': 'Express',
      'paymentMethod': 'BankTransfer',
      'purchaseOrderNumber': 'PO-1',
      'internalReference': null,
      'costCenterCode': 'OPS',
      'costCenterName': 'التشغيل',
      'projectCode': null,
      'projectName': null,
      'requestingDepartment': 'المشتريات',
      'orderNote': null,
      'allowSplitDelivery': true,
      'requiresApproval': false,
      'canCancel': false,
      'canTrack': true,
      'items': [
        {
          'id': 'i1',
          'productId': 'p1',
          'variantId': null,
          'sku': 'SKU-1',
          'nameAr': 'منتج',
          'variantName': null,
          'quantity': 10,
          'unitPrice': 100,
          'lineTotal': 1000,
          'customerNote': null,
          'rating': 4,
        },
      ],
      'history': [
        {
          'status': 'Shipped',
          'note': 'غادر المخزن',
          'at': '2026-07-19T08:00:00Z',
        },
      ],
      'shipments': [
        {
          'id': 's1',
          'number': 'SHP-1',
          'carrierName': 'Mohandseto',
          'trackingNumber': 'TRK-1',
          'status': 'OutForDelivery',
          'driverName': 'السائق',
          'driverPhone': '0111',
          'driverLatitude': 30.1,
          'driverLongitude': 31.2,
          'estimatedArrival': '2026-07-19T12:00:00Z',
          'deliveredAt': null,
          'events': [
            {
              'id': 'e1',
              'status': 'Shipped',
              'descriptionAr': 'تم الشحن',
              'location': 'المخزن',
              'latitude': 30.0,
              'longitude': 31.0,
              'at': '2026-07-19T08:00:00Z',
            },
          ],
        },
      ],
      'proofs': [],
      'issues': [
        {
          'id': 'x1',
          'orderItemId': 'i1',
          'type': 'DamagedItem',
          'affectedQuantity': 1,
          'description': 'تلف',
          'status': 'Open',
          'createdAt': '2026-07-19T13:00:00Z',
          'hasPhoto': true,
        },
      ],
      'rating': {'deliveryRating': 5, 'serviceRating': 4, 'comment': 'جيد'},
      'schedules': [
        {'id': 'r1'},
      ],
    });
    expect(order.shipments.single.events.single.location, 'المخزن');
    expect(order.items.single.rating, 4);
    expect(order.issues.single.hasPhoto, true);
    expect(order.deliveryRating, 5);
    expect(order.scheduleCount, 1);
  });

  test('order list parses tracking capabilities', () {
    final order = OrderListItem.fromJson({
      'id': 'o1',
      'number': 'ORD-1',
      'status': 'Shipped',
      'total': 1200,
      'itemCount': 3,
      'requiredDate': '2026-07-20T00:00:00Z',
      'createdAt': '2026-07-13T00:00:00Z',
      'canCancel': false,
      'canTrack': true,
    });
    expect(order.canTrack, true);
    expect(order.itemCount, 3);
  });
}
