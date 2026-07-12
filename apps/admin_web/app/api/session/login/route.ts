import { NextResponse } from "next/server";

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

  const roles = (data.user?.roles ?? []) as string[];
  const allowed = roles.some((role) => role.startsWith("platform_") || role === "super_admin");
  if (!allowed) {
    return NextResponse.json(
      { message: "هذا الحساب غير مصرح له بدخول لوحة الإدارة" },
      { status: 403 },
    );
  }

  const result = NextResponse.json({ user: data.user });
  result.cookies.set("admin_access", data.accessToken, {
    httpOnly: true,
    secure: process.env.NODE_ENV === "production",
    sameSite: "lax",
    path: "/",
    maxAge: 15 * 60,
  });
  result.cookies.set("admin_refresh", data.refreshToken, {
    httpOnly: true,
    secure: process.env.NODE_ENV === "production",
    sameSite: "strict",
    path: "/api/session",
    maxAge: 30 * 24 * 60 * 60,
  });
  return result;
}
