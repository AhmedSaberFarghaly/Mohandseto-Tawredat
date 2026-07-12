import type { Metadata } from "next";
import "@fontsource/cairo/400.css";
import "@fontsource/cairo/500.css";
import "@fontsource/cairo/600.css";
import "@fontsource/cairo/700.css";
import "./globals.css";

export const metadata: Metadata = {
  title: "مهندسيتو توريدات - لوحة الإدارة",
  description: "لوحة الإدارة وCRM لمنصة مهندسيتو توريدات B2B",
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html lang="ar" dir="rtl">
      <body>{children}</body>
    </html>
  );
}
