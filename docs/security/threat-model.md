# Threat model

## الأصول وحدود الثقة

الأصول الحرجة: حسابات الشركات، الأسعار والعقود، الطلبات والفواتير، بيانات الدفع المرجعية، ملفات الشعارات/المستندات، أسرار المزودين والنسخ الاحتياطية. حدود الثقة هي العميل/المتصفح، BFF، API، قاعدة البيانات، التخزين والمزودون الخارجيون.

| التهديد | الضوابط الحالية | تحقق الإصدار |
|---|---|---|
| عبور Tenant | JWT tenant claim + global filters + authorization | service tests وHTTP 401/roles |
| سرقة جلسة الإدارة | HttpOnly/Secure/SameSite cookies، access قصير وrefresh rotating | E2E login وcookie review |
| brute force | auth rate limit، lockout، 2FA، LoginAudit، suspicious activity | Auth tests + monitoring tests |
| تزوير/إعادة استخدام هوية خارجية | Google native sign-in؛ Microsoft PKCE؛ فحص JWKS/signature/aud/iss/exp؛ Microsoft nonce؛ تحدٍ أحادي الاستخدام؛ الربط بالمفتاح الثابت `provider+sub` | Auth replay/linking tests + provider sandbox قبل GA |
| IDOR / صلاحية زائدة | controller roles + ownership checks + scopes | permissions tests وmanual matrix |
| تسريب أسرار | env-only، Data Protection، hash-only keys، Gitleaks CI | secret scan gate |
| ملفات خبيثة | type/size validation، paths معزولة، لا تنفيذ | upload tests؛ malware scanner مطلوب عند مزود التخزين |
| Injection | EF parameterization، validation، no raw user SQL | build/tests + dependency scan |
| XSS/clickjacking | React escaping، security headers، frame deny | HTTP header E2E؛ browser axe قبل GA |
| DoS | global/auth limits، 30MB body cap، reverse proxy مطلوب | load test في staging |
| backup tampering | SHA-256، maintenance gate، audited restore request | AdminMonitoring tests |

## مخاطر باقية قبل GA

- credentials ومزود OAuth/SMS/Payment/Storage الحقيقيون لم تُسلّم بعد؛ OAuth غير المهيأ يظهر Disabled ولا يعطل طرق الدخول المحلية.
- فحص malware خارجي للملفات يحتاج اختيار مزود.
- إصدار SQLite الحالي single-instance؛ HA وSQL Server cutover يحتاجان قرار استضافة واختبار ترحيل.
- يلزم DAST وload test على عنوان staging الفعلي بعد توفيره.
