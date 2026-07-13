import { NextResponse } from "next/server";

const apiBaseUrl = process.env.API_BASE_URL ?? "http://localhost:5199";

export async function POST(request: Request) {
  const body = await request.json();
  const backend = await fetch(`${apiBaseUrl}/api/auth/password/request`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
    cache: "no-store",
  });
  const data = await backend.json().catch(() => ({}));
  if (!backend.ok)
    return NextResponse.json({ message: data.title ?? "تعذر إرسال رمز الاستعادة" }, { status: backend.status });

  const result = NextResponse.json({
    sent: true,
    expiresAt: data.expiresAt,
    maskedPhone: data.maskedPhone,
    developmentCode: data.developmentCode,
  });
  result.cookies.set("admin_password_reset", data.resetToken, {
    httpOnly: true,
    secure: process.env.NODE_ENV === "production",
    sameSite: "strict",
    path: "/api/session/password",
    maxAge: 10 * 60,
  });
  return result;
}
