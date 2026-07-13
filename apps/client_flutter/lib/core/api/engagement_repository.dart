import 'package:dio/dio.dart';
import 'package:file_picker/file_picker.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'api_client.dart';

typedef Json = Map<String, dynamic>;

class NotificationModel {
  const NotificationModel(
    this.id,
    this.type,
    this.title,
    this.body,
    this.entityType,
    this.entityId,
    this.read,
    this.createdAt,
  );
  factory NotificationModel.fromJson(Json j) => NotificationModel(
    j['id'] as String,
    j['type'] as String,
    j['title'] as String,
    j['body'] as String,
    j['entityType'] as String?,
    j['entityId'] as String?,
    j['isRead'] as bool,
    DateTime.parse(j['createdAt'] as String),
  );
  final String id, type, title, body;
  final String? entityType, entityId;
  final bool read;
  final DateTime createdAt;
}

class NotificationPageModel {
  const NotificationPageModel(this.unread, this.total, this.items);
  factory NotificationPageModel.fromJson(Json j) => NotificationPageModel(
    j['unreadCount'] as int,
    j['total'] as int,
    (j['items'] as List)
        .map((e) => NotificationModel.fromJson(e as Json))
        .toList(),
  );
  final int unread, total;
  final List<NotificationModel> items;
}

class NotificationPreferencesModel {
  const NotificationPreferencesModel(
    this.push,
    this.email,
    this.sms,
    this.orders,
    this.approvals,
    this.quotes,
    this.invoices,
    this.promotions,
  );
  factory NotificationPreferencesModel.fromJson(Json j) =>
      NotificationPreferencesModel(
        j['pushEnabled'] as bool,
        j['emailEnabled'] as bool,
        j['smsEnabled'] as bool,
        j['ordersEnabled'] as bool,
        j['approvalsEnabled'] as bool,
        j['quotesEnabled'] as bool,
        j['invoicesEnabled'] as bool,
        j['promotionsEnabled'] as bool,
      );
  final bool push, email, sms, orders, approvals, quotes, invoices, promotions;
  Json toJson() => {
    'pushEnabled': push,
    'emailEnabled': email,
    'smsEnabled': sms,
    'ordersEnabled': orders,
    'approvalsEnabled': approvals,
    'quotesEnabled': quotes,
    'invoicesEnabled': invoices,
    'promotionsEnabled': promotions,
  };
}

class TicketListModel {
  const TicketListModel(
    this.id,
    this.number,
    this.type,
    this.priority,
    this.status,
    this.subject,
    this.createdAt,
    this.updatedAt,
    this.unread,
  );
  factory TicketListModel.fromJson(Json j) => TicketListModel(
    j['id'] as String,
    j['number'] as String,
    j['type'] as String,
    j['priority'] as String,
    j['status'] as String,
    j['subject'] as String,
    DateTime.parse(j['createdAt'] as String),
    j['updatedAt'] == null ? null : DateTime.parse(j['updatedAt'] as String),
    j['unreadMessages'] as int,
  );
  final String id, number, type, priority, status, subject;
  final DateTime createdAt;
  final DateTime? updatedAt;
  final int unread;
}

class TicketMessageModel {
  const TicketMessageModel(
    this.id,
    this.sender,
    this.staff,
    this.body,
    this.createdAt,
  );
  factory TicketMessageModel.fromJson(Json j) => TicketMessageModel(
    j['id'] as String,
    j['senderName'] as String,
    j['isStaff'] as bool,
    j['body'] as String,
    DateTime.parse(j['createdAt'] as String),
  );
  final String id, sender, body;
  final bool staff;
  final DateTime createdAt;
}

class TicketDetailModel {
  const TicketDetailModel(
    this.id,
    this.number,
    this.type,
    this.priority,
    this.status,
    this.subject,
    this.description,
    this.orderId,
    this.createdAt,
    this.resolvedAt,
    this.rating,
    this.messages,
    this.attachments,
  );
  factory TicketDetailModel.fromJson(Json j) => TicketDetailModel(
    j['id'] as String,
    j['number'] as String,
    j['type'] as String,
    j['priority'] as String,
    j['status'] as String,
    j['subject'] as String,
    j['description'] as String,
    j['orderId'] as String?,
    DateTime.parse(j['createdAt'] as String),
    j['resolvedAt'] == null ? null : DateTime.parse(j['resolvedAt'] as String),
    j['rating'] as int?,
    (j['messages'] as List)
        .map((e) => TicketMessageModel.fromJson(e as Json))
        .toList(),
    (j['attachments'] as List).map((e) => e as Json).toList(),
  );
  final String id, number, type, priority, status, subject, description;
  final String? orderId;
  final DateTime createdAt;
  final DateTime? resolvedAt;
  final int? rating;
  final List<TicketMessageModel> messages;
  final List<Json> attachments;
}

class FaqModel {
  const FaqModel(this.slug, this.category, this.question, this.answer);
  factory FaqModel.fromJson(Json j) => FaqModel(
    j['slug'] as String,
    j['category'] as String,
    j['question'] as String,
    j['answer'] as String,
  );
  final String slug, category, question, answer;
}

class ContentPageModel {
  const ContentPageModel(
    this.slug,
    this.title,
    this.body,
    this.phone,
    this.whatsApp,
    this.email,
    this.address,
  );
  factory ContentPageModel.fromJson(Json j) => ContentPageModel(
    j['slug'] as String,
    j['title'] as String,
    j['body'] as String,
    j['phone'] as String?,
    j['whatsApp'] as String?,
    j['email'] as String?,
    j['address'] as String?,
  );
  final String slug, title, body;
  final String? phone, whatsApp, email, address;
}

class UserSessionModel {
  const UserSessionModel(this.id, this.device, this.createdAt, this.expiresAt);
  factory UserSessionModel.fromJson(Json j) => UserSessionModel(
    j['id'] as String,
    j['device'] as String,
    DateTime.parse(j['createdAt'] as String),
    DateTime.parse(j['expiresAt'] as String),
  );
  final String id, device;
  final DateTime createdAt, expiresAt;
}

class UserSettingsModel {
  const UserSettingsModel(
    this.language,
    this.theme,
    this.twoFactor,
    this.channel,
    this.sessions,
    this.deletionAt,
  );
  factory UserSettingsModel.fromJson(Json j) => UserSettingsModel(
    j['language'] as String,
    j['theme'] as String,
    j['twoFactorEnabled'] as bool,
    j['twoFactorChannel'] as String?,
    (j['sessions'] as List)
        .map((e) => UserSessionModel.fromJson(e as Json))
        .toList(),
    j['deletionScheduledFor'] == null
        ? null
        : DateTime.parse(j['deletionScheduledFor'] as String),
  );
  final String language, theme;
  final bool twoFactor;
  final String? channel;
  final List<UserSessionModel> sessions;
  final DateTime? deletionAt;
}

class MobileAppConfigModel {
  const MobileAppConfigModel(
    this.minimum,
    this.latest,
    this.maintenance,
    this.message,
    this.updateUrl,
  );
  factory MobileAppConfigModel.fromJson(Json j) => MobileAppConfigModel(
    j['minimumVersion'] as String,
    j['latestVersion'] as String,
    j['maintenanceEnabled'] as bool,
    j['message'] as String?,
    j['updateUrl'] as String?,
  );
  final String minimum, latest;
  final bool maintenance;
  final String? message, updateUrl;
}

class EngagementRepository {
  EngagementRepository(this._api);
  final ApiClient _api;

  MultipartFile _supportFile(PlatformFile file) {
    final extension = file.extension?.toLowerCase();
    final contentType = switch (extension) {
      'pdf' => DioMediaType('application', 'pdf'),
      'png' => DioMediaType('image', 'png'),
      'jpg' || 'jpeg' => DioMediaType('image', 'jpeg'),
      _ => DioMediaType('application', 'octet-stream'),
    };
    return MultipartFile.fromBytes(
      file.bytes!,
      filename: file.name,
      contentType: contentType,
    );
  }

  Future<NotificationPageModel> notifications([bool unread = false]) async =>
      NotificationPageModel.fromJson(
        (await _api.dio.get(
              '/api/notifications',
              queryParameters: {'unreadOnly': unread},
            )).data
            as Json,
      );
  Future<void> readNotification(String? id) async => _api.dio.post(
    id == null ? '/api/notifications/read-all' : '/api/notifications/$id/read',
  );
  Future<NotificationPreferencesModel> notificationPreferences() async =>
      NotificationPreferencesModel.fromJson(
        (await _api.dio.get('/api/notifications/preferences')).data as Json,
      );
  Future<void> saveNotificationPreferences(
    NotificationPreferencesModel p,
  ) async => _api.dio.put('/api/notifications/preferences', data: p.toJson());
  Future<List<TicketListModel>> tickets() async =>
      ((await _api.dio.get('/api/support/tickets')).data as List)
          .map((e) => TicketListModel.fromJson(e as Json))
          .toList();
  Future<TicketDetailModel> ticket(String id) async =>
      TicketDetailModel.fromJson(
        (await _api.dio.get('/api/support/tickets/$id')).data as Json,
      );
  Future<TicketDetailModel> createTicket(
    Json data,
    List<PlatformFile> files,
  ) async {
    final form = FormData.fromMap({
      ...data,
      'files': files.where((f) => f.bytes != null).map(_supportFile).toList(),
    });
    return TicketDetailModel.fromJson(
      (await _api.dio.post('/api/support/tickets', data: form)).data as Json,
    );
  }

  Future<void> message(
    String id,
    String body,
    List<PlatformFile> files,
  ) async => _api.dio.post(
    '/api/support/tickets/$id/messages',
    data: FormData.fromMap({
      'body': body,
      'files': files.where((f) => f.bytes != null).map(_supportFile).toList(),
    }),
  );
  Future<void> closeTicket(String id) async =>
      _api.dio.post('/api/support/tickets/$id/close');
  Future<void> rateTicket(String id, int rating, String comment) async =>
      _api.dio.post(
        '/api/support/tickets/$id/rating',
        data: {'rating': rating, 'comment': comment},
      );
  Future<void> callback(String phone, String topic, DateTime at) async =>
      _api.dio.post(
        '/api/support/callback-requests',
        data: {
          'phone': phone,
          'topic': topic,
          'preferredAt': at.toUtc().toIso8601String(),
        },
      );
  Future<List<FaqModel>> faq() async =>
      ((await _api.dio.get('/api/content/faq')).data as List)
          .map((e) => FaqModel.fromJson(e as Json))
          .toList();
  Future<ContentPageModel> content(String slug) async =>
      ContentPageModel.fromJson(
        (await _api.dio.get('/api/content/pages/$slug')).data as Json,
      );
  Future<UserSettingsModel> settings() async => UserSettingsModel.fromJson(
    (await _api.dio.get('/api/settings')).data as Json,
  );
  Future<UserSettingsModel> appearance(String language, String theme) async =>
      UserSettingsModel.fromJson(
        (await _api.dio.put(
              '/api/settings/appearance',
              data: {'language': language, 'theme': theme},
            )).data
            as Json,
      );
  Future<void> changePassword(String current, String next) async =>
      _api.dio.post(
        '/api/settings/password',
        data: {'currentPassword': current, 'newPassword': next},
      );
  Future<String?> requestTwoFactor() async =>
      ((await _api.dio.post(
                '/api/settings/two-factor/request',
                data: {'channel': 'sms'},
              )).data
              as Json)['devCode']
          as String?;
  Future<void> enableTwoFactor(String code) async => _api.dio.post(
    '/api/settings/two-factor/enable',
    data: {'code': code, 'channel': 'sms'},
  );
  Future<void> disableTwoFactor(String password) async => _api.dio.post(
    '/api/settings/two-factor/disable',
    data: {'password': password},
  );
  Future<void> revokeSession(String? id) async => _api.dio.delete(
    id == null ? '/api/settings/sessions' : '/api/settings/sessions/$id',
  );
  Future<DateTime> deleteAccount(String password, String reason) async =>
      DateTime.parse(
        ((await _api.dio.post(
                  '/api/settings/account-deletion',
                  data: {'password': password, 'reason': reason},
                )).data
                as Json)['scheduledFor']
            as String,
      );
  Future<void> cancelDeletion() async =>
      _api.dio.delete('/api/settings/account-deletion');
  Future<MobileAppConfigModel> appConfig() async =>
      MobileAppConfigModel.fromJson(
        (await _api.dio.get('/api/app/config')).data as Json,
      );
}

final engagementRepositoryProvider = Provider(
  (ref) => EngagementRepository(ref.watch(apiClientProvider)),
);
final notificationsProvider =
    FutureProvider.family<NotificationPageModel, bool>(
      (ref, unread) =>
          ref.watch(engagementRepositoryProvider).notifications(unread),
    );
final notificationPreferencesProvider = FutureProvider(
  (ref) => ref.watch(engagementRepositoryProvider).notificationPreferences(),
);
final supportTicketsProvider = FutureProvider(
  (ref) => ref.watch(engagementRepositoryProvider).tickets(),
);
final supportTicketProvider = FutureProvider.family<TicketDetailModel, String>(
  (ref, id) => ref.watch(engagementRepositoryProvider).ticket(id),
);
final faqProvider = FutureProvider(
  (ref) => ref.watch(engagementRepositoryProvider).faq(),
);
final contentPageProvider = FutureProvider.family<ContentPageModel, String>(
  (ref, slug) => ref.watch(engagementRepositoryProvider).content(slug),
);
final userSettingsProvider = FutureProvider(
  (ref) => ref.watch(engagementRepositoryProvider).settings(),
);
