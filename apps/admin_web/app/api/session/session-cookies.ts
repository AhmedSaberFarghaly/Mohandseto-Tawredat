import { NextResponse } from "next/server";

export type AuthPayload = {
  accessToken: string;
  refreshToken: string;
  user: { fullName: string; email?: string; roles: string[] };
};

export const adminRoles = [
  "super_admin",
  "operations_manager",
  "sales_manager",
  "sales_agent",
  "quotes_officer",
  "products_manager",
  "inventory_manager",
  "warehouse_manager",
  "procurement_officer",
  "accountant",
  "support_agent",
  "graphic_designer",
  "printing_officer",
  "delivery_driver",
  "system_admin",
  "auditor",
] as const;

export function eligibleRoles(roles: string[] = []) {
  return roles.filter((role) => adminRoles.includes(role as (typeof adminRoles)[number]));
}

export function setSessionCookies(response: NextResponse, data: AuthPayload) {
  response.cookies.set("admin_access", data.accessToken, {
    httpOnly: true,
    secure: process.env.NODE_ENV === "production",
    sameSite: "lax",
    path: "/",
    maxAge: 15 * 60,
  });
  response.cookies.set("admin_refresh", data.refreshToken, {
    httpOnly: true,
    secure: process.env.NODE_ENV === "production",
    sameSite: "strict",
    path: "/api",
    maxAge: 30 * 24 * 60 * 60,
  });
}
