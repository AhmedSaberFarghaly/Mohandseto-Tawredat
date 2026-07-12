import { cookies } from "next/headers";
import { redirect } from "next/navigation";

import { AdminShell } from "../ui/admin-shell";

export default async function DashboardLayout({ children }: LayoutProps<"/dashboard">) {
  const cookieStore = await cookies();
  if (!cookieStore.has("admin_access")) redirect("/login");
  return <AdminShell>{children}</AdminShell>;
}
