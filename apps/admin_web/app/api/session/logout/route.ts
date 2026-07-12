import { cookies } from "next/headers";
import { NextResponse } from "next/server";

const apiBaseUrl = process.env.API_BASE_URL ?? "http://localhost:5199";

export async function POST() {
  const cookieStore = await cookies();
  const refreshToken = cookieStore.get("admin_refresh")?.value;
  if (refreshToken) {
    await fetch(`${apiBaseUrl}/api/auth/logout`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ refreshToken }),
      cache: "no-store",
    }).catch(() => undefined);
  }
  const result = NextResponse.json({ loggedOut: true });
  result.cookies.delete("admin_access");
  result.cookies.delete("admin_refresh");
  return result;
}
