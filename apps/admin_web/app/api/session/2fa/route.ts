import { cookies } from "next/headers";
import { NextResponse } from "next/server";
import { eligibleRoles, setSessionCookies, type AuthPayload } from "../session-cookies";

const apiBaseUrl = process.env.API_BASE_URL ?? "http://localhost:5199";

export async function POST(request: Request) {
  const challengeToken = (await cookies()).get("admin_2fa")?.value;
  if (!challengeToken)
    return NextResponse.json({ message: "انتهت جلسة التحقق، سجل الدخول من جديد" }, { status: 401 });

  const { code } = await request.json();
  const backend = await fetch(`${apiBaseUrl}/api/auth/2fa/verify`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ challengeToken, code }),
    cache: "no-store",
  });
  const data = await backend.json().catch(() => ({}));
  if (!backend.ok)
    return NextResponse.json({ message: data.title ?? "رمز التحقق غير صحيح" }, { status: backend.status });

  const roles = eligibleRoles(data.user?.roles);
  if (roles.length === 0)
    return NextResponse.json({ message: "هذا الحساب غير مصرح له بدخول لوحة الإدارة" }, { status: 403 });

  const result = NextResponse.json({ user: data.user, roles });
  setSessionCookies(result, data as AuthPayload);
  result.cookies.delete("admin_2fa");
  return result;
}
