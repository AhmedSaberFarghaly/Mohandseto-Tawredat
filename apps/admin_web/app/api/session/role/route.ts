import { cookies } from "next/headers";
import { NextResponse } from "next/server";
import { adminRoles } from "../session-cookies";

export async function POST(request: Request) {
  if (!(await cookies()).has("admin_access"))
    return NextResponse.json({ message: "غير مصرح" }, { status: 401 });
  const { role } = await request.json();
  if (typeof role !== "string" || !adminRoles.includes(role as (typeof adminRoles)[number]))
    return NextResponse.json({ message: "الدور المحدد غير صالح" }, { status: 400 });

  const response = NextResponse.json({ selected: true });
  response.cookies.set("admin_role", role, {
    httpOnly: true,
    secure: process.env.NODE_ENV === "production",
    sameSite: "lax",
    path: "/",
    maxAge: 30 * 24 * 60 * 60,
  });
  return response;
}
