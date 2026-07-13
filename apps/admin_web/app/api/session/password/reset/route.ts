import { cookies } from "next/headers";
import { NextResponse } from "next/server";

const apiBaseUrl = process.env.API_BASE_URL ?? "http://localhost:5199";

export async function POST(request: Request) {
  const resetToken = (await cookies()).get("admin_password_reset")?.value;
  if (!resetToken)
    return NextResponse.json({ message: "انتهت جلسة الاستعادة، أعد المحاولة" }, { status: 401 });

  const body = await request.json();
  const backend = await fetch(`${apiBaseUrl}/api/auth/password/reset`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ ...body, resetToken }),
    cache: "no-store",
  });
  const data = await backend.json().catch(() => ({}));
  if (!backend.ok)
    return NextResponse.json({ message: data.title ?? "تعذر تعيين كلمة المرور" }, { status: backend.status });

  const result = NextResponse.json({ reset: true });
  result.cookies.delete("admin_password_reset");
  return result;
}
