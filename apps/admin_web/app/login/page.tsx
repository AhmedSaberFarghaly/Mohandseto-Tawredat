import { LoginForm } from "./login-form";

export default function LoginPage() {
  return (
    <main className="auth-page">
      <div className="auth-brand">
        <span className="brand-mark">ت</span>
        <span>مهندسيتو توريدات</span>
      </div>
      <section className="auth-card" aria-labelledby="login-title">
        <LoginForm />
      </section>
      <footer className="auth-footer">© 2026 مهندسيتو توريدات. جميع الحقوق محفوظة.</footer>
    </main>
  );
}
