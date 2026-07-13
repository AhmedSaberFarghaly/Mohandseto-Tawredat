import 'package:flutter_test/flutter_test.dart';
import 'package:mohandseto_client/core/api/rfq_repository.dart';

void main() {
  test('RFQ detail parses extracted items quote versions and negotiation', () {
    final detail = RfqDetailModel.fromJson({
      'id': 'rfq-1',
      'number': 'RFQ-1',
      'title': 'احتياجات تشغيل',
      'description': null,
      'status': 'Negotiating',
      'requiredDate': '2026-08-01T00:00:00Z',
      'quoteDeadline': '2026-07-20T00:00:00Z',
      'deliveryGovernorate': 'القاهرة',
      'items': [
        {
          'id': 'item-1',
          'productId': 'product-1',
          'description': 'ورق',
          'quantity': 10,
          'unitName': 'عبوة',
          'specifications': null,
          'preferredBrand': null,
          'allowAlternatives': true,
          'source': 'Excel',
          'confidence': 0.9,
          'isReviewed': true,
        },
      ],
      'attachments': [],
      'quoteNumber': 'QT-1',
      'quoteStatus': 'RevisionRequested',
      'quoteVersions': [
        {
          'id': 'version-1',
          'version': 1,
          'subtotal': 1000,
          'tax': 140,
          'shipping': 0,
          'total': 1140,
          'validUntil': '2026-07-25T00:00:00Z',
          'deliveryDays': 4,
          'terms': '30 يومًا',
          'changeSummary': null,
          'sentAt': '2026-07-15T00:00:00Z',
          'isExpired': false,
          'items': [
            {
              'id': 'quote-item-1',
              'rfqItemId': 'item-1',
              'productId': 'product-1',
              'description': 'ورق',
              'quantity': 10,
              'unitName': 'عبوة',
              'unitPrice': 100,
              'lineTotal': 1000,
              'isAlternative': false,
              'alternativeReason': null,
            },
          ],
        },
      ],
      'negotiations': [
        {
          'id': 'neg-1',
          'userId': 'user-1',
          'isStaff': false,
          'type': 'CounterOffer',
          'message': 'نحتاج خصمًا',
          'proposedTotal': 1000,
          'createdAt': '2026-07-16T00:00:00Z',
        },
      ],
      'convertedOrderId': null,
    });
    expect(detail.status, 'Negotiating');
    expect(detail.items.single.source, 'Excel');
    expect(detail.versions.single.total, 1140);
    expect(detail.negotiations.single.proposed, 1000);
  });

  test('RFQ list parses latest quote total', () {
    final item = RfqListItem.fromJson({
      'id': 'rfq-1',
      'number': 'RFQ-1',
      'title': 'احتياجات',
      'status': 'Quoted',
      'itemCount': 3,
      'requiredDate': '2026-08-01T00:00:00Z',
      'quoteDeadline': '2026-07-20T00:00:00Z',
      'latestQuoteTotal': 2500,
      'createdAt': '2026-07-13T00:00:00Z',
    });
    expect(item.itemCount, 3);
    expect(item.latestTotal, 2500);
  });
}
