export default function Home() {
  return (
    <main
      style={{
        minHeight: "100vh",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        flexDirection: "column",
        gap: 12,
      }}
    >
      <div
        style={{
          width: 64,
          height: 64,
          borderRadius: 16,
          background: "var(--color-primary)",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          color: "#fff",
          fontSize: 28,
          fontWeight: 700,
        }}
      >
        م
      </div>
      <h1 style={{ fontSize: 22, color: "var(--color-sidebar)" }}>
        مهندسيتو توريدات — لوحة الإدارة
      </h1>
      <p style={{ color: "var(--gray-500)", fontSize: 14 }}>
        الأساس جاهز (v0.1.0) — تسجيل الدخول ولوحات المعلومات في المرحلة القادمة
      </p>
    </main>
  );
}
