# Google and Microsoft sign-in setup

The repository contains the complete adapters but intentionally contains no provider credentials. Without client IDs, `/api/auth/external/providers` reports the provider as disabled and phone/email authentication remains available.

## Shared security model

- Native/public clients use no embedded client secret.
- The backend accepts ID tokens only over the authenticated API transport and validates signing keys from OIDC discovery, audience, issuer and expiry before using the stable provider `sub` identifier.
- Microsoft uses Authorization Code + PKCE and a nonce bound to a one-use five-minute server challenge.
- Google uses the official native Flutter integration; the server challenge is still one-use and the signed Google ID token is validated against the web/server client ID.
- Email auto-link is off by default. A signed-in user can link a provider from **Settings → Linked accounts**; enable `AllowEmailAutoLink` only after a deliberate identity-risk review.

References: [Google backend authentication](https://developers.google.com/identity/sign-in/android/backend-auth), [Google OIDC validation](https://developers.google.com/identity/openid-connect/openid-connect), [Microsoft authorization code + PKCE](https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-auth-code-flow), and [flutter_appauth platform setup](https://pub.dev/packages/flutter_appauth).

## Google Cloud

1. Create/configure the consent screen and a **Web application** OAuth client used as the server audience. Set its ID in `GOOGLE_OAUTH_CLIENT_ID` / `ExternalAuth__Google__ClientId`.
2. Register Android package `com.mohandseto.mohandseto_client` with the production signing certificate SHA fingerprints. The native plugin requests an ID token for the web/server client ID; do not create a client secret in the app.
3. Create an iOS OAuth client for the final bundle ID, set its ID in `GOOGLE_IOS_OAUTH_CLIENT_ID`, and add its reversed client-ID URL scheme to `ios/Runner/Info.plist` in the signed build configuration.
4. Complete Google's consent/brand verification when the selected publishing mode requires it.

Google no longer supports arbitrary custom URI schemes for Android OAuth clients, which is why Android Google login uses the official native plugin rather than the Microsoft AppAuth redirect.

## Microsoft Entra

1. Register a mobile/desktop public client and set `MICROSOFT_OAUTH_CLIENT_ID`.
2. Add the custom mobile redirect `com.mohandseto.tawredat:/oauthredirect` and allow public-client Authorization Code + PKCE.
3. Use `MICROSOFT_OAUTH_TENANT=organizations` for work/school accounts, or a specific tenant GUID to restrict access. Personal Microsoft consumer tenants are rejected in `organizations` mode.
4. No Microsoft client secret belongs in Flutter or is required by this flow.

## Staging acceptance

On physical Android and iOS devices verify: provider disabled state, account picker, cancellation, first link, repeat login, mismatched account conflict, replay rejection, suspended account rejection, and 2FA continuation. Confirm login/audit events contain no raw ID tokens.
