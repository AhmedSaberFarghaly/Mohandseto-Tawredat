import 'package:flutter_test/flutter_test.dart';
import 'package:mohandseto_client/core/api/engagement_repository.dart';

void main() {
  test('notifications and preferences parse delivery channels', () {
    final page = NotificationPageModel.fromJson({
      'unreadCount': 2,
      'total': 3,
      'items': [
        {'id': 'n1', 'type': 'invoice.due', 'title': 'فاتورة مستحقة', 'body': 'موعد السداد غدًا', 'entityType': 'Invoice', 'entityId': 'i1', 'isRead': false, 'createdAt': '2026-07-13T10:00:00Z'},
      ],
    });
    final prefs = NotificationPreferencesModel.fromJson({
      'pushEnabled': true, 'emailEnabled': true, 'smsEnabled': false,
      'ordersEnabled': true, 'approvalsEnabled': true, 'quotesEnabled': true,
      'invoicesEnabled': true, 'promotionsEnabled': false,
    });
    expect(page.unread, 2);
    expect(page.items.single.entityId, 'i1');
    expect(prefs.toJson()['promotionsEnabled'], false);
  });

  test('support ticket and security settings parse lifecycle data', () {
    final ticket = TicketDetailModel.fromJson({
      'id': 't1', 'number': 'SUP-100', 'type': 'Technical',
      'priority': 'High', 'status': 'Resolved', 'subject': 'مشكلة دفع',
      'description': 'تعذر الدفع', 'orderId': null,
      'createdAt': '2026-07-12T10:00:00Z', 'resolvedAt': '2026-07-13T10:00:00Z',
      'rating': 5,
      'messages': [{'id': 'm1', 'senderUserId': 'u1', 'senderName': 'فريق الدعم', 'isStaff': true, 'body': 'تم الحل', 'createdAt': '2026-07-13T09:00:00Z', 'readAt': null}],
      'attachments': [],
    });
    final settings = UserSettingsModel.fromJson({
      'language': 'ar', 'theme': 'dark', 'twoFactorEnabled': true,
      'twoFactorChannel': 'sms',
      'sessions': [{'id': 's1', 'device': 'Chrome / Windows', 'createdAt': '2026-07-13T08:00:00Z', 'expiresAt': '2026-08-13T08:00:00Z', 'isCurrent': false}],
      'deletionScheduledFor': null,
    });
    expect(ticket.messages.single.staff, true);
    expect(ticket.rating, 5);
    expect(settings.twoFactor, true);
    expect(settings.sessions.single.device, contains('Windows'));
  });
}
