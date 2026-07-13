import 'package:dio/dio.dart';
import 'package:file_picker/file_picker.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'api_client.dart';

class ApprovalListItem {
  const ApprovalListItem({
    required this.id,
    required this.number,
    required this.orderNumber,
    required this.total,
    required this.requesterName,
    required this.currentLevel,
    required this.status,
    required this.budgetConflict,
    required this.dueAt,
    required this.overdue,
  });
  factory ApprovalListItem.fromJson(Map<String, dynamic> j) => ApprovalListItem(
    id: j['id'] as String,
    number: j['number'] as String,
    orderNumber: j['orderNumber'] as String,
    total: (j['total'] as num).toDouble(),
    requesterName: j['requesterName'] as String,
    currentLevel: j['currentLevel'] as String,
    status: j['status'] as String,
    budgetConflict: j['budgetConflict'] as bool,
    dueAt: DateTime.parse(j['dueAt'] as String),
    overdue: j['isOverdue'] as bool,
  );
  final String id, number, orderNumber, requesterName, currentLevel, status;
  final double total;
  final bool budgetConflict, overdue;
  final DateTime dueAt;
}

class ApprovalStepModel {
  const ApprovalStepModel({
    required this.sequence,
    required this.name,
    required this.approverName,
    required this.status,
    required this.authorityLimit,
    required this.decidedAt,
    required this.currentUser,
  });
  factory ApprovalStepModel.fromJson(Map<String, dynamic> j) =>
      ApprovalStepModel(
        sequence: j['sequence'] as int,
        name: j['name'] as String,
        approverName: j['approverName'] as String,
        status: j['status'] as String,
        authorityLimit: (j['authorityLimit'] as num?)?.toDouble(),
        decidedAt: j['decidedAt'] == null
            ? null
            : DateTime.parse(j['decidedAt'] as String),
        currentUser: j['isCurrentUser'] as bool,
      );
  final int sequence;
  final String name, approverName, status;
  final double? authorityLimit;
  final DateTime? decidedAt;
  final bool currentUser;
}

class ApprovalActionModel {
  const ApprovalActionModel({
    required this.actor,
    required this.type,
    required this.level,
    required this.comment,
    required this.createdAt,
  });
  factory ApprovalActionModel.fromJson(Map<String, dynamic> j) =>
      ApprovalActionModel(
        actor: j['actorName'] as String,
        type: j['type'] as String,
        level: j['levelSequence'] as int,
        comment: j['comment'] as String?,
        createdAt: DateTime.parse(j['createdAt'] as String),
      );
  final String actor, type;
  final int level;
  final String? comment;
  final DateTime createdAt;
}

class ApprovalAttachmentModel {
  const ApprovalAttachmentModel({
    required this.id,
    required this.name,
    required this.size,
    required this.url,
  });
  factory ApprovalAttachmentModel.fromJson(Map<String, dynamic> j) =>
      ApprovalAttachmentModel(
        id: j['id'] as String,
        name: j['name'] as String,
        size: j['sizeBytes'] as int,
        url: j['downloadUrl'] as String,
      );
  final String id, name, url;
  final int size;
}

class ApprovalDetailModel {
  const ApprovalDetailModel({
    required this.id,
    required this.number,
    required this.orderNumber,
    required this.total,
    required this.requester,
    required this.department,
    required this.costCenter,
    required this.budgetAvailable,
    required this.budgetConflict,
    required this.status,
    required this.currentLevel,
    required this.dueAt,
    required this.steps,
    required this.actions,
    required this.attachments,
    required this.canAct,
    required this.exceedsAuthority,
  });
  factory ApprovalDetailModel.fromJson(Map<String, dynamic> j) =>
      ApprovalDetailModel(
        id: j['id'] as String,
        number: j['number'] as String,
        orderNumber: j['orderNumber'] as String,
        total: (j['total'] as num).toDouble(),
        requester: j['requesterName'] as String,
        department: j['department'] as String,
        costCenter: j['costCenter'] as String?,
        budgetAvailable: (j['budgetAvailable'] as num?)?.toDouble(),
        budgetConflict: j['budgetConflict'] as bool,
        status: j['status'] as String,
        currentLevel: j['currentLevel'] as int,
        dueAt: DateTime.parse(j['dueAt'] as String),
        steps: (j['steps'] as List)
            .map((x) => ApprovalStepModel.fromJson(x as Map<String, dynamic>))
            .toList(),
        actions: (j['actions'] as List)
            .map((x) => ApprovalActionModel.fromJson(x as Map<String, dynamic>))
            .toList(),
        attachments: (j['attachments'] as List)
            .map(
              (x) =>
                  ApprovalAttachmentModel.fromJson(x as Map<String, dynamic>),
            )
            .toList(),
        canAct: j['canAct'] as bool,
        exceedsAuthority: j['exceedsAuthority'] as bool,
      );
  final String id, number, orderNumber, requester, department, status;
  final String? costCenter;
  final double total;
  final double? budgetAvailable;
  final bool budgetConflict, canAct, exceedsAuthority;
  final int currentLevel;
  final DateTime dueAt;
  final List<ApprovalStepModel> steps;
  final List<ApprovalActionModel> actions;
  final List<ApprovalAttachmentModel> attachments;
}

class ApprovalUserModel {
  const ApprovalUserModel({
    required this.id,
    required this.name,
    required this.phone,
  });
  factory ApprovalUserModel.fromJson(Map<String, dynamic> j) =>
      ApprovalUserModel(
        id: j['id'] as String,
        name: j['name'] as String,
        phone: j['phone'] as String,
      );
  final String id, name, phone;
}

class ApprovalRepository {
  ApprovalRepository(this._api);
  final ApiClient _api;
  Future<List<ApprovalListItem>> inbox([String? status]) async =>
      ((await _api.dio.get(
                '/api/approvals',
                queryParameters: {if (status != null) 'status': status},
              )).data
              as List)
          .map((x) => ApprovalListItem.fromJson(x as Map<String, dynamic>))
          .toList();
  Future<ApprovalDetailModel> detail(String id) async =>
      ApprovalDetailModel.fromJson(
        (await _api.dio.get('/api/approvals/$id')).data as Map<String, dynamic>,
      );
  Future<ApprovalDetailModel> decision(
    String id,
    String action,
    String? comment,
  ) async => ApprovalDetailModel.fromJson(
    (await _api.dio.post(
          '/api/approvals/$id/$action',
          data: {'comment': comment},
        )).data
        as Map<String, dynamic>,
  );
  Future<ApprovalDetailModel> comment(String id, String comment) async =>
      ApprovalDetailModel.fromJson(
        (await _api.dio.post(
              '/api/approvals/$id/comments',
              data: {'comment': comment},
            )).data
            as Map<String, dynamic>,
      );
  Future<List<ApprovalUserModel>> users() async =>
      ((await _api.dio.get('/api/approvals/users')).data as List)
          .map((x) => ApprovalUserModel.fromJson(x as Map<String, dynamic>))
          .toList();
  Future<ApprovalDetailModel> delegate(
    String id,
    String userId,
    String? comment,
  ) async => ApprovalDetailModel.fromJson(
    (await _api.dio.post(
          '/api/approvals/$id/delegate',
          data: {'userId': userId, 'comment': comment},
        )).data
        as Map<String, dynamic>,
  );
  Future<ApprovalAttachmentModel> upload(String id, PlatformFile file) async {
    final upload = file.bytes != null
        ? MultipartFile.fromBytes(file.bytes!, filename: file.name)
        : await MultipartFile.fromFile(file.path!, filename: file.name);
    return ApprovalAttachmentModel.fromJson(
      (await _api.dio.post(
            '/api/approvals/$id/attachments',
            data: FormData.fromMap({'file': upload}),
          )).data
          as Map<String, dynamic>,
    );
  }
}

final approvalRepositoryProvider = Provider(
  (ref) => ApprovalRepository(ref.watch(apiClientProvider)),
);
final approvalInboxProvider =
    FutureProvider.family<List<ApprovalListItem>, String?>(
      (ref, status) => ref.watch(approvalRepositoryProvider).inbox(status),
    );
final approvalDetailProvider =
    FutureProvider.family<ApprovalDetailModel, String>(
      (ref, id) => ref.watch(approvalRepositoryProvider).detail(id),
    );
