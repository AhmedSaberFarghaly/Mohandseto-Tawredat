import { cookies } from "next/headers";
import { NextResponse, type NextRequest } from "next/server";

const apiBaseUrl = process.env.API_BASE_URL ?? "http://localhost:5199";

type Tokens = { accessToken: string; refreshToken: string };

async function proxy(request: NextRequest, context: RouteContext<"/api/admin/[...path]">) {
  const { path } = await context.params;
  const cookieStore = await cookies();
  let accessToken = cookieStore.get("admin_access")?.value;
  const refreshToken = cookieStore.get("admin_refresh")?.value;
  if (!accessToken) return NextResponse.json({ message: "غير مصرح" }, { status: 401 });

  const suffix = path.join("/");
  const url = `${apiBaseUrl}/api/admin/${suffix}${request.nextUrl.search}`;
  const body = request.method === "GET" || request.method === "HEAD"
    ? undefined
    : await request.arrayBuffer();
  const send = (token: string) => fetch(url, {
    method: request.method,
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": request.headers.get("content-type") ?? "application/json",
    },
    body,
    cache: "no-store",
  });

  let backend = await send(accessToken);
  let rotated: Tokens | null = null;
  if (backend.status === 401 && refreshToken) {
    const refresh = await fetch(`${apiBaseUrl}/api/auth/refresh`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ refreshToken }),
      cache: "no-store",
    });
    if (refresh.ok) {
      rotated = await refresh.json() as Tokens;
      accessToken = rotated.accessToken;
      backend = await send(accessToken);
    }
  }

  const responseBody = [204, 205, 304].includes(backend.status)
    ? null
    : await backend.arrayBuffer();
  const response = new NextResponse(responseBody, {
    status: backend.status,
    headers: { "Content-Type": backend.headers.get("content-type") ?? "application/json" },
  });
  if (rotated) {
    response.cookies.set("admin_access", rotated.accessToken, {
      httpOnly: true, secure: process.env.NODE_ENV === "production", sameSite: "lax", path: "/", maxAge: 15 * 60,
    });
    response.cookies.set("admin_refresh", rotated.refreshToken, {
      httpOnly: true, secure: process.env.NODE_ENV === "production", sameSite: "strict", path: "/api", maxAge: 30 * 24 * 60 * 60,
    });
  }
  if (backend.status === 401) {
    response.cookies.delete("admin_access");
    response.cookies.set("admin_refresh", "", { path: "/api", maxAge: 0 });
  }
  return response;
}

export const GET = proxy;
export const POST = proxy;
export const PUT = proxy;
export const DELETE = proxy;
