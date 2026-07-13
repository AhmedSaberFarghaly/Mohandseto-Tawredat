import 'package:flutter_test/flutter_test.dart';
import 'package:mohandseto_client/core/api/customization_repository.dart';

void main() {
  test('custom template parses all pricing choices', () {
    final template = CustomTemplate.fromJson({
      'id': 'template-1',
      'productId': 'product-1',
      'sku': 'MT-00001',
      'nameAr': 'كوب مطبوع',
      'descriptionAr': 'تخصيص كامل',
      'imageUrl': null,
      'startingUnitPrice': 45,
      'setupFee': 250,
      'minQuantity': 25,
      'leadTimeDays': 7,
      'placements': [_choice('front', 'الأمام', 0)],
      'printMethods': [_choice('digital', 'طباعة رقمية', 8)],
      'materials': [_choice('premium', 'خامة فاخرة', 9)],
      'colors': [
        {..._choice('navy', 'كحلي', 0), 'hex': '#0E2D6D'},
      ],
      'sizes': [_choice('m', 'متوسط', 2)],
    });
    expect(template.printMethods.single.priceAdjustment, 8);
    expect(template.colors.single.hex, '#0E2D6D');
    expect(template.minQuantity, 25);
  });

  test('custom request parses quote versions and production stages', () {
    final request = CustomRequestDetail.fromJson({
      'id': 'request-1',
      'number': 'CPR-260713-1234',
      'productName': 'كوب مطبوع',
      'sku': 'MT-00001',
      'status': 'InProduction',
      'quantity': 100,
      'placement': 'الأمام',
      'printMethod': 'رقمية',
      'material': 'فاخرة',
      'color': 'كحلي',
      'size': 'متوسط',
      'printWidthCm': 8,
      'printHeightCm': 12,
      'printColorCount': 2,
      'estimatedTotal': 6500,
      'quotedTotal': 6200,
      'quoteExpiresAt': '2026-07-20T00:00:00Z',
      'designServiceRequested': true,
      'versions': [
        {
          'id': 'v1',
          'versionNumber': 1,
          'title': 'المقترح الأول',
          'changeSummary': null,
          'mockups': <Object>[],
        },
      ],
      'comments': <Object>[],
      'latestDecision': 'Approved',
      'production': {
        'stages': [
          {
            'id': 's1',
            'nameAr': 'تجهيز الخامات',
            'status': 'Completed',
            'sortOrder': 1,
          },
        ],
        'samples': [
          {
            'id': 'sample-1',
            'versionNumber': 1,
            'name': 'sample.png',
            'decision': 'Approved',
            'note': null,
            'decisionNote': null,
            'downloadUrl': '/api/custom-products/samples/sample-1',
          },
        ],
      },
    });
    expect(request.quotedTotal, 6200);
    expect(request.versions.single.number, 1);
    expect(request.productionStages.single.status, 'Completed');
    expect(request.productionSamples.single.decision, 'Approved');
    expect(request.printColorCount, 2);
  });
}

Map<String, dynamic> _choice(String code, String name, num price) => {
  'id': '$code-id',
  'code': code,
  'nameAr': name,
  'descriptionAr': null,
  'priceAdjustment': price,
  'hex': null,
};
