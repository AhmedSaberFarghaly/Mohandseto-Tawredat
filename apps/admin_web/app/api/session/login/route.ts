import { NextResponse } from "next/server";
import { eligibleRoles, setSessionCookies, type AuthPayload } from "../session-cookies";

const apiBaseUrl = process.env.API_BASE_URL ?? "http://localhost:5199";

export async function POST(request: Request) {
  const body = await request.json();
  const response = await fetch(`${apiBaseUrl}/api/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
    cache: "no-store",
  });
  const data = await response.json().catch(() => ({}));

  if (!response.ok) {
    return NextResponse.json(
      { message: data.title ?? "البريد الإلكتروني أو كلمة المرور غير صحيحة" },
      { status: response.status },
    );
  }

  if (data.requiresTwoFactor) {
    const result = NextResponse.json({
      requiresTwoFactor: true,
      expiresAt: data.challengeExpiresAt,
      developmentCode: data.developmentCode,
    });
    result.cookies.set("admin_2fa", data.challengeToken, {
      httpOnly: true,
      secure: process.env.NODE_ENV === "production",
      sameSite: "strict",
      path: "/api/session",
      maxAge: 5 * 60,
    });
    return result;
  }

  const roles = eligibleRoles(data.user?.roles);
  if (roles.length === 0) {
    return NextResponse.json(
      { message: "هذا الحساب غير مصرح له بدخول لوحة الإدارة" },
      { status: 403 },
    );
  }

  const result = NextResponse.json({ user: data.user, roles });
  setSessionCookies(result, data as AuthPayload);
  return result;
}
