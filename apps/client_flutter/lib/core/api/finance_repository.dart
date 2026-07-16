import 'dart:typed_data';
import 'package:dio/dio.dart';
import 'package:file_picker/file_picker.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'api_client.dart';

class InvoiceListModel {
  const InvoiceListModel({
    required this.id,
    required this.number,
    required this.orderNumber,
    required this.status,
    required this.type,
    required this.total,
    required this.paid,
    required this.outstanding,
    required this.issuedAt,
    required this.dueAt,
    required this.overdue,
  });
  factory InvoiceListModel.fromJson(Map<String, dynamic> j) => InvoiceListModel(
    id: j['id'] as String,
    number: j['number'] as String,
    orderNumber: j['orderNumber'] as String,
    status: j['status'] as String,
    type: j['type'] as String,
    total: (j['total'] as num).toDouble(),
    paid: (j['paidAmount'] as num).toDouble(),
    outstanding: (j['outstanding'] as num).toDouble(),
    issuedAt: DateTime.parse(j['issuedAt'] as String),
    dueAt: DateTime.parse(j['dueAt'] as String),
    overdue: j['isOverdue'] as bool,
  );
  final String id, number, orderNumber, status, type;
  final double total, paid, outstanding;
  final DateTime issuedAt, dueAt;
  final bool overdue;
}

class InvoiceLineModel {
  const InvoiceLineModel(
    this.id,
    this.sku,
    this.description,
    this.quantity,
    this.unitPrice,
    this.tax,
    this.total,
  );
  factory InvoiceLineModel.fromJson(Map<String, dynamic> j) => InvoiceLineModel(
    j['id'] as String,
    j['sku'] as String,
    j['description'] as String,
    j['quantity'] as int,
    (j['unitPrice'] as num).toDouble(),
    (j['taxAmount'] as num).toDouble(),
    (j['lineTotal'] as num).toDouble(),
  );
  final String id, sku, description;
  final int quantity;
  final double unitPrice, tax, total;
}

class InvoicePaymentModel {
  const InvoicePaymentModel({
    required this.id,
    required this.amount,
    required this.method,
    required this.status,
    required this.reference,
    required this.bankReference,
    required this.createdAt,
    required this.verifiedAt,
    required this.hasReceipt,
    required this.rejectionReason,
  });
  factory InvoicePaymentModel.fromJson(Map<String, dynamic> j) =>
      InvoicePaymentModel(
        id: j['id'] as String,
        amount: (j['amount'] as num).toDouble(),
        method: j['method'] as String,
        status: j['status'] as String,
        reference: j['reference'] as String,
        bankReference: j['bankReference'] as String?,
        createdAt: DateTime.parse(j['createdAt'] as String),
        verifiedAt: j['verifiedAt'] == null
            ? null
            : DateTime.parse(j['verifiedAt'] as String),
        hasReceipt: j['hasReceipt'] as bool,
        rejectionReason: j['rejectionReason'] as String?,
      );
  final String id, method, status, reference;
  final String? bankReference, rejectionReason;
  final double amount;
  final DateTime createdAt;
  final DateTime? verifiedAt;
  final bool hasReceipt;
}

class InvoiceDetailModel {
  const InvoiceDetailModel({
    required this.id,
    required this.number,
    required this.orderNumber,
    required this.status,
    required this.type,
    required this.issuedAt,
    required this.dueAt,
    required this.currency,
    required this.sellerTaxNumber,
    required this.buyerTaxNumber,
    required this.etaUuid,
    required this.subtotal,
    required this.discount,
    required this.tax,
    required this.shipping,
    required this.total,
    required this.paid,
    required this.outstanding,
    required this.lines,
    required this.payments,
    required this.qrPayload,
  });
  factory InvoiceDetailModel.fromJson(Map<String, dynamic> j) =>
      InvoiceDetailModel(
        id: j['id'] as String,
        number: j['number'] as String,
        orderNumber: j['orderNumber'] as String,
        status: j['status'] as String,
        type: j['type'] as String,
        issuedAt: DateTime.parse(j['issuedAt'] as String),
        dueAt: DateTime.parse(j['dueAt'] as String),
        currency: j['currency'] as String,
        sellerTaxNumber: j['sellerTaxNumber'] as String,
        buyerTaxNumber: j['buyerTaxNumber'] as String?,
        etaUuid: j['etaUuid'] as String?,
        subtotal: (j['subtotal'] as num).toDouble(),
        discount: (j['discount'] as num).toDouble(),
        tax: (j['tax'] as num).toDouble(),
        shipping: (j['shipping'] as num).toDouble(),
        total: (j['total'] as num).toDouble(),
        paid: (j['paidAmount'] as num).toDouble(),
        outstanding: (j['outstanding'] as num).toDouble(),
        lines: (j['lines'] as List)
            .map((e) => InvoiceLineModel.fromJson(e as Map<String, dynamic>))
            .toList(),
        payments: (j['payments'] as List)
            .map((e) => InvoicePaymentModel.fromJson(e as Map<String, dynamic>))
            .toList(),
        qrPayload: j['electronicQrPayload'] as String,
      );
  final String id,
      number,
      orderNumber,
      status,
      type,
      currency,
      sellerTaxNumber,
      qrPayload;
  final String? buyerTaxNumber, etaUuid;
  final DateTime issuedAt, dueAt;
  final double subtotal, discount, tax, shipping, total, paid, outstanding;
  final List<InvoiceLineModel> lines;
  final List<InvoicePaymentModel> payments;
}

class FinanceSummaryModel {
  const FinanceSummaryModel({
    required this.outstanding,
    required this.overdue,
    required this.dueSoon,
    required this.openInvoices,
    required this.creditLimit,
    required this.creditUsed,
    required this.creditAvailable,
    required this.utilization,
    required this.upcoming,
    required this.payments,
  });
  factory FinanceSummaryModel.fromJson(Map<String, dynamic> j) =>
      FinanceSummaryModel(
        outstanding: (j['outstanding'] as num).toDouble(),
        overdue: (j['overdue'] as num).toDouble(),
        dueSoon: (j['dueSoon'] as num).toDouble(),
        openInvoices: j['openInvoices'] as int,
        creditLimit: (j['creditLimit'] as num).toDouble(),
        creditUsed: (j['creditUsed'] as num).toDouble(),
        creditAvailable: (j['creditAvailable'] as num).toDouble(),
        utilization: (j['creditUtilization'] as num).toDouble(),
        upcoming: (j['upcoming'] as List)
            .map((e) => InvoiceListModel.fromJson(e as Map<String, dynamic>))
            .toList(),
        payments: (j['recentPayments'] as List)
            .map((e) => InvoicePaymentModel.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
  final double outstanding,
      overdue,
      dueSoon,
      creditLimit,
      creditUsed,
      creditAvailable,
      utilization;
  final int openInvoices;
  final List<InvoiceListModel> upcoming;
  final List<InvoicePaymentModel> payments;
}

class PaymentStartedModel {
  const PaymentStartedModel(
    this.id,
    this.reference,
    this.amount,
    this.status,
    this.bankName,
    this.accountName,
    this.iban,
  );
  factory PaymentStartedModel.fromJson(Map<String, dynamic> j) =>
      PaymentStartedModel(
        j['paymentId'] as String,
        j['reference'] as String,
        (j['amount'] as num).toDouble(),
        j['status'] as String,
        j['bankName'] as String,
        j['accountName'] as String,
        j['iban'] as String,
      );
  final String id, reference, status, bankName, accountName, iban;
  final double amount;
}

class FinanceRepository {
  FinanceRepository(this._api);
  final ApiClient _api;
  Future<List<InvoiceListModel>> invoices([String? status]) async =>
      ((await _api.dio.get(
                '/api/finance/invoices',
                queryParameters: {if (status != null) 'status': status},
              )).data
              as List)
          .map((e) => InvoiceListModel.fromJson(e as Map<String, dynamic>))
          .toList();
  Future<InvoiceDetailModel> invoice(String id) async =>
      InvoiceDetailModel.fromJson(
        (await _api.dio.get('/api/finance/invoices/$id')).data
            as Map<String, dynamic>,
      );
  Future<FinanceSummaryModel> summary() async => FinanceSummaryModel.fromJson(
    (await _api.dio.get('/api/finance/summary')).data as Map<String, dynamic>,
  );
  Future<PaymentStartedModel> startPayment(
    String id,
    double amount,
    String? bankRef,
  ) async => PaymentStartedModel.fromJson(
    (await _api.dio.post(
          '/api/finance/invoices/$id/payments',
          data: {'amount': amount, 'bankReference': bankRef},
        )).data
        as Map<String, dynamic>,
  );
  Future<void> uploadReceipt(String paymentId, PlatformFile file) async {
    final upload = file.bytes != null
        ? MultipartFile.fromBytes(file.bytes!, filename: file.name)
        : await MultipartFile.fromFile(file.path!, filename: file.name);
    await _api.dio.post(
      '/api/finance/payments/$paymentId/receipt',
      data: FormData.fromMap({'file': upload}),
    );
  }

  Future<void> requestCredit(double limit, String reason) async =>
      _api.dio.post(
        '/api/finance/credit-limit-requests',
        data: {'requestedLimit': limit, 'reason': reason},
      );
  Future<Uint8List> pdf(String id) async => Uint8List.fromList(
    (await _api.dio.get<List<int>>(
      '/api/finance/invoices/$id/pdf',
      options: Options(responseType: ResponseType.bytes),
    )).data!,
  );
  Future<Uint8List> export({
    String format = 'xlsx',
    String? status,
    DateTime? from,
    DateTime? to,
  }) async => Uint8List.fromList(
    (await _api.dio.get<List<int>>(
      '/api/finance/invoices/export',
      queryParameters: {
        'format': format,
        if (status != null) 'status': status,
        if (from != null) 'from': from.toIso8601String(),
        if (to != null) 'to': to.toIso8601String(),
      },
      options: Options(responseType: ResponseType.bytes),
    )).data!,
  );
}

final financeRepositoryProvider = Provider(
  (ref) => FinanceRepository(ref.watch(apiClientProvider)),
);
final invoiceListProvider =
    FutureProvider.family<List<InvoiceListModel>, String?>(
      (ref, status) => ref.watch(financeRepositoryProvider).invoices(status),
    );
final invoiceDetailProvider = FutureProvider.family<InvoiceDetailModel, String>(
  (ref, id) => ref.watch(financeRepositoryProvider).invoice(id),
);
final financeSummaryProvider = FutureProvider(
  (ref) => ref.watch(financeRepositoryProvider).summary(),
);
