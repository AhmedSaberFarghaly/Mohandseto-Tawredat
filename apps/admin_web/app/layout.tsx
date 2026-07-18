import type { Metadata } from "next";
import "@fontsource/ibm-plex-sans-arabic/400.css";
import "@fontsource/ibm-plex-sans-arabic/500.css";
import "@fontsource/ibm-plex-sans-arabic/600.css";
import "@fontsource/ibm-plex-sans-arabic/700.css";
import "./globals.css";
import NetworkProgress from "./ui/network-progress";

export const metadata: Metadata = {
  title: "مهندسيتو توريدات - لوحة الإدارة",
  description: "لوحة الإدارة وCRM لمنصة مهندسيتو توريدات B2B",
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html lang="ar" dir="rtl">
      <body>
        <NetworkProgress />
        {children}
      </body>
    </html>
  );
}
