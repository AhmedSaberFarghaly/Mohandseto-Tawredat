import 'package:dio/dio.dart';
import 'package:file_picker/file_picker.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'api_client.dart';

class CustomTemplateSummary {
  const CustomTemplateSummary({
    required this.id,
    required this.productId,
    required this.sku,
    required this.name,
    required this.imageUrl,
    required this.startingPrice,
    required this.setupFee,
    required this.minQuantity,
    required this.leadTimeDays,
  });
  factory CustomTemplateSummary.fromJson(Map<String, dynamic> json) =>
      CustomTemplateSummary(
        id: json['id'] as String,
        productId: json['productId'] as String,
        sku: json['sku'] as String,
        name: json['nameAr'] as String,
        imageUrl: json['imageUrl'] as String?,
        startingPrice: (json['startingUnitPrice'] as num).toDouble(),
        setupFee: (json['setupFee'] as num).toDouble(),
        minQuantity: json['minQuantity'] as int,
        leadTimeDays: json['leadTimeDays'] as int,
      );
  final String id, productId, sku, name;
  final String? imageUrl;
  final double startingPrice, setupFee;
  final int minQuantity, leadTimeDays;
}

class CustomChoice {
  const CustomChoice({
    required this.id,
    required this.code,
    required this.name,
    required this.description,
    required this.priceAdjustment,
    required this.hex,
  });
  factory CustomChoice.fromJson(Map<String, dynamic> json) => CustomChoice(
    id: json['id'] as String,
    code: json['code'] as String,
    name: json['nameAr'] as String,
    description: json['descriptionAr'] as String?,
    priceAdjustment: (json['priceAdjustment'] as num).toDouble(),
    hex: json['hex'] as String?,
  );
  final String id, code, name;
  final String? description, hex;
  final double priceAdjustment;
}

class CustomTemplate {
  const CustomTemplate({
    required this.id,
    required this.productId,
    required this.sku,
    required this.name,
    required this.description,
    required this.imageUrl,
    required this.startingPrice,
    required this.setupFee,
    required this.minQuantity,
    required this.leadTimeDays,
    required this.placements,
    required this.printMethods,
    required this.materials,
    required this.colors,
    required this.sizes,
  });
  factory CustomTemplate.fromJson(Map<String, dynamic> json) => CustomTemplate(
    id: json['id'] as String,
    productId: json['productId'] as String,
    sku: json['sku'] as String,
    name: json['nameAr'] as String,
    description: json['descriptionAr'] as String?,
    imageUrl: json['imageUrl'] as String?,
    startingPrice: (json['startingUnitPrice'] as num).toDouble(),
    setupFee: (json['setupFee'] as num).toDouble(),
    minQuantity: json['minQuantity'] as int,
    leadTimeDays: json['leadTimeDays'] as int,
    placements: _choices(json['placements']),
    printMethods: _choices(json['printMethods']),
    materials: _choices(json['materials']),
    colors: _choices(json['colors']),
    sizes: _choices(json['sizes']),
  );
  final String id, productId, sku, name;
  final String? description, imageUrl;
  final double startingPrice, setupFee;
  final int minQuantity, leadTimeDays;
  final List<CustomChoice> placements, printMethods, materials, colors, sizes;
  static List<CustomChoice> _choices(dynamic value) => (value as List)
      .map((x) => CustomChoice.fromJson(x as Map<String, dynamic>))
      .toList();
}

class SavedLogo {
  const SavedLogo({
    required this.id,
    required this.name,
    required this.contentType,
    required this.sizeBytes,
    required this.downloadUrl,
  });
  factory SavedLogo.fromJson(Map<String, dynamic> json) => SavedLogo(
    id: json['id'] as String,
    name: json['name'] as String,
    contentType: json['contentType'] as String,
    sizeBytes: json['sizeBytes'] as int,
    downloadUrl: json['downloadUrl'] as String,
  );
  final String id, name, contentType, downloadUrl;
  final int sizeBytes;
}

class CustomRequestSummary {
  const CustomRequestSummary({
    required this.id,
    required this.number,
    required this.productName,
    required this.status,
    required this.quantity,
    required this.total,
    required this.createdAt,
    required this.progress,
  });
  factory CustomRequestSummary.fromJson(Map<String, dynamic> json) =>
      CustomRequestSummary(
        id: json['id'] as String,
        number: json['number'] as String,
        productName: json['productName'] as String,
        status: json['status'] as String,
        quantity: json['quantity'] as int,
        total: (json['displayTotal'] as num).toDouble(),
        createdAt: DateTime.parse(json['createdAt'] as String),
        progress: json['progressPercent'] as int,
      );
  final String id, number, productName, status;
  final int quantity, progress;
  final double total;
  final DateTime createdAt;
}

class CustomDesignVersion {
  const CustomDesignVersion({
    required this.id,
    required this.number,
    required this.title,
    required this.changeSummary,
    required this.mockups,
  });
  factory CustomDesignVersion.fromJson(Map<String, dynamic> json) =>
      CustomDesignVersion(
        id: json['id'] as String,
        number: json['versionNumber'] as int,
        title: json['title'] as String,
        changeSummary: json['changeSummary'] as String?,
        mockups: (json['mockups'] as List)
            .map((x) => x as Map<String, dynamic>)
            .toList(),
      );
  final String id, title;
  final int number;
  final String? changeSummary;
  final List<Map<String, dynamic>> mockups;
}

class CustomProductionStage {
  const CustomProductionStage({
    required this.id,
    required this.name,
    required this.status,
    required this.order,
  });
  factory CustomProductionStage.fromJson(Map<String, dynamic> json) =>
      CustomProductionStage(
        id: json['id'] as String,
        name: json['nameAr'] as String,
        status: json['status'] as String,
        order: json['sortOrder'] as int,
      );
  final String id, name, status;
  final int order;
}

class CustomProductionSample {
  const CustomProductionSample({
    required this.id,
    required this.version,
    required this.name,
    required this.decision,
    required this.note,
    required this.decisionNote,
    required this.downloadUrl,
  });
  factory CustomProductionSample.fromJson(Map<String, dynamic> json) =>
      CustomProductionSample(
        id: json['id'] as String,
        version: json['versionNumber'] as int,
        name: json['name'] as String,
        decision: json['decision'] as String,
        note: json['note'] as String?,
        decisionNote: json['decisionNote'] as String?,
        downloadUrl: json['downloadUrl'] as String,
      );
  final String id, name, decision, downloadUrl;
  final int version;
  final String? note, decisionNote;
}

class CustomRequestDetail {
  const CustomRequestDetail({
    required this.id,
    required this.number,
    required this.productName,
    required this.sku,
    required this.status,
    required this.quantity,
    required this.placement,
    required this.printMethod,
    required this.material,
    required this.color,
    required this.size,
    required this.printWidthCm,
    required this.printHeightCm,
    required this.printColorCount,
    required this.estimatedTotal,
    required this.quotedTotal,
    required this.quoteExpiresAt,
    required this.designServiceRequested,
    required this.versions,
    required this.comments,
    required this.latestDecision,
    required this.productionStages,
    required this.productionSamples,
  });
  factory CustomRequestDetail.fromJson(
    Map<String, dynamic> json,
  ) => CustomRequestDetail(
    id: json['id'] as String,
    number: json['number'] as String,
    productName: json['productName'] as String,
    sku: json['sku'] as String,
    status: json['status'] as String,
    quantity: json['quantity'] as int,
    placement: json['placement'] as String,
    printMethod: json['printMethod'] as String,
    material: json['material'] as String,
    color: json['color'] as String,
    size: json['size'] as String,
    printWidthCm: (json['printWidthCm'] as num).toDouble(),
    printHeightCm: (json['printHeightCm'] as num).toDouble(),
    printColorCount: json['printColorCount'] as int,
    estimatedTotal: (json['estimatedTotal'] as num).toDouble(),
    quotedTotal: (json['quotedTotal'] as num?)?.toDouble(),
    quoteExpiresAt: json['quoteExpiresAt'] == null
        ? null
        : DateTime.parse(json['quoteExpiresAt'] as String),
    designServiceRequested: json['designServiceRequested'] as bool,
    versions: (json['versions'] as List)
        .map((x) => CustomDesignVersion.fromJson(x as Map<String, dynamic>))
        .toList(),
    comments: (json['comments'] as List)
        .map((x) => x as Map<String, dynamic>)
        .toList(),
    latestDecision: json['latestDecision'] as String?,
    productionStages: json['production'] == null
        ? []
        : ((json['production'] as Map<String, dynamic>)['stages'] as List)
              .map(
                (x) =>
                    CustomProductionStage.fromJson(x as Map<String, dynamic>),
              )
              .toList(),
    productionSamples: json['production'] == null
        ? []
        : (((json['production'] as Map<String, dynamic>)['samples'] as List?) ??
                  const [])
              .map(
                (x) =>
                    CustomProductionSample.fromJson(x as Map<String, dynamic>),
              )
              .toList(),
  );
  final String id,
      number,
      productName,
      sku,
      status,
      placement,
      printMethod,
      material,
      color,
      size;
  final int quantity;
  final double printWidthCm, printHeightCm;
  final int printColorCount;
  final double estimatedTotal;
  final double? quotedTotal;
  final DateTime? quoteExpiresAt;
  final bool designServiceRequested;
  final List<CustomDesignVersion> versions;
  final List<Map<String, dynamic>> comments;
  final String? latestDecision;
  final List<CustomProductionStage> productionStages;
  final List<CustomProductionSample> productionSamples;
}

class CreateCustomRequestInput {
  const CreateCustomRequestInput({
    required this.templateId,
    required this.quantity,
    required this.placementId,
    required this.printMethodId,
    required this.materialId,
    required this.colorId,
    required this.sizeId,
    required this.designServiceRequested,
    this.logo,
    this.existingLogoAssetId,
    this.printWidthCm = 5,
    this.printHeightCm = 5,
    this.printColorCount = 1,
    this.designFile,
    this.customText,
    this.customerNote,
    this.objective,
    this.audience,
    this.preferredColors,
    this.requiredText,
  });
  final String templateId,
      placementId,
      printMethodId,
      materialId,
      colorId,
      sizeId;
  final int quantity;
  final double printWidthCm, printHeightCm;
  final int printColorCount;
  final bool designServiceRequested;
  final PlatformFile? logo;
  final String? existingLogoAssetId;
  final PlatformFile? designFile;
  final String? customText,
      customerNote,
      objective,
      audience,
      preferredColors,
      requiredText;
}

class CustomizationRepository {
  CustomizationRepository(this._api);
  final ApiClient _api;
  Future<List<CustomTemplateSummary>> templates() async =>
      ((await _api.dio.get('/api/custom-products/templates')).data as List)
          .map((x) => CustomTemplateSummary.fromJson(x as Map<String, dynamic>))
          .toList();
  Future<CustomTemplate> template(String id) async => CustomTemplate.fromJson(
    (await _api.dio.get('/api/custom-products/templates/$id')).data
        as Map<String, dynamic>,
  );
  Future<List<SavedLogo>> savedLogos() async =>
      ((await _api.dio.get('/api/custom-products/logos')).data as List)
          .map((x) => SavedLogo.fromJson(x as Map<String, dynamic>))
          .toList();
  Future<List<CustomRequestSummary>> requests() async =>
      ((await _api.dio.get('/api/custom-products/requests')).data as List)
          .map((x) => CustomRequestSummary.fromJson(x as Map<String, dynamic>))
          .toList();
  Future<CustomRequestDetail> request(String id) async =>
      CustomRequestDetail.fromJson(
        (await _api.dio.get('/api/custom-products/requests/$id')).data
            as Map<String, dynamic>,
      );
  Future<CustomRequestDetail> create(CreateCustomRequestInput input) async {
    final map = <String, dynamic>{
      'templateId': input.templateId,
      'quantity': input.quantity,
      'placementId': input.placementId,
      'printMethodId': input.printMethodId,
      'materialId': input.materialId,
      'colorId': input.colorId,
      'sizeId': input.sizeId,
      'designServiceRequested': input.designServiceRequested,
      'printWidthCm': input.printWidthCm,
      'printHeightCm': input.printHeightCm,
      'printColorCount': input.printColorCount,
      if (input.logo != null) 'logo': await _file(input.logo!),
      if (input.existingLogoAssetId != null)
        'existingLogoAssetId': input.existingLogoAssetId,
      if (input.designFile != null)
        'designFile': await _file(input.designFile!),
      if (input.customText?.isNotEmpty == true) 'customText': input.customText,
      if (input.customerNote?.isNotEmpty == true)
        'customerNote': input.customerNote,
      if (input.objective?.isNotEmpty == true) 'objective': input.objective,
      if (input.audience?.isNotEmpty == true) 'audience': input.audience,
      if (input.preferredColors?.isNotEmpty == true)
        'preferredColors': input.preferredColors,
      if (input.requiredText?.isNotEmpty == true)
        'requiredText': input.requiredText,
    };
    return CustomRequestDetail.fromJson(
      (await _api.dio.post(
            '/api/custom-products/requests',
            data: FormData.fromMap(map),
          )).data
          as Map<String, dynamic>,
    );
  }

  Future<CustomRequestDetail> respondQuote(String id, bool accept) async =>
      CustomRequestDetail.fromJson(
        (await _api.dio.post(
              '/api/custom-products/requests/$id/quote-response',
              data: {'accept': accept},
            )).data
            as Map<String, dynamic>,
      );
  Future<CustomRequestDetail> decideDesign(
    String id,
    String versionId,
    String decision,
    String? note,
  ) async => CustomRequestDetail.fromJson(
    (await _api.dio.post(
          '/api/custom-products/requests/$id/design-decision',
          data: {'versionId': versionId, 'decision': decision, 'note': note},
        )).data
        as Map<String, dynamic>,
  );
  Future<CustomRequestDetail> comment(String id, String body) async =>
      CustomRequestDetail.fromJson(
        (await _api.dio.post(
              '/api/custom-products/requests/$id/comments',
              data: {'body': body},
            )).data
            as Map<String, dynamic>,
      );
  Future<CustomRequestDetail> decideSample(
    String requestId,
    String sampleId,
    String decision,
    String? note,
  ) async => CustomRequestDetail.fromJson(
    (await _api.dio.post(
          '/api/custom-products/requests/$requestId/samples/$sampleId/decision',
          data: {'decision': decision, 'note': note},
        )).data
        as Map<String, dynamic>,
  );
  Future<void> addToCart(String requestId) =>
      _api.dio.post('/api/custom-products/requests/$requestId/add-to-cart');
  static Future<MultipartFile> _file(PlatformFile file) async =>
      file.bytes != null
      ? MultipartFile.fromBytes(file.bytes!, filename: file.name)
      : MultipartFile.fromFile(file.path!, filename: file.name);
}

final customizationRepositoryProvider = Provider(
  (ref) => CustomizationRepository(ref.watch(apiClientProvider)),
);
final customTemplatesProvider = FutureProvider(
  (ref) => ref.watch(customizationRepositoryProvider).templates(),
);
final customTemplateProvider = FutureProvider.family(
  (ref, String id) => ref.watch(customizationRepositoryProvider).template(id),
);
final customRequestsProvider = FutureProvider(
  (ref) => ref.watch(customizationRepositoryProvider).requests(),
);
final savedLogosProvider = FutureProvider(
  (ref) => ref.watch(customizationRepositoryProvider).savedLogos(),
);
final customRequestProvider = FutureProvider.family(
  (ref, String id) => ref.watch(customizationRepositoryProvider).request(id),
);
