import { cookies } from "next/headers";
import { NextResponse } from "next/server";

const apiBaseUrl = process.env.API_BASE_URL ?? "http://localhost:5199";

export async function POST() {
  const cookieStore = await cookies();
  const refreshToken = cookieStore.get("admin_refresh")?.value;
  if (!refreshToken) {
    return NextResponse.json({ message: "انتهت الجلسة" }, { status: 401 });
  }

  const response = await fetch(`${apiBaseUrl}/api/auth/refresh`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ refreshToken }),
    cache: "no-store",
  });
  if (!response.ok) {
    const result = NextResponse.json({ message: "انتهت الجلسة" }, { status: 401 });
    result.cookies.delete("admin_access");
    result.cookies.delete("admin_refresh");
    return result;
  }

  const data = await response.json();
  const result = NextResponse.json({ refreshed: true });
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
