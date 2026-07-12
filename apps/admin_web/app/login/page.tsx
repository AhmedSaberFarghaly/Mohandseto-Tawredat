import { LoginForm } from "./login-form";

export default function LoginPage() {
  return (
    <main className="auth-page">
      <div className="auth-brand">
        <span className="brand-mark">ت</span>
        <span>مهندسيتو توريدات</span>
      </div>
      <section className="auth-card" aria-labelledby="login-title">
        <div className="auth-card-logo">ت</div>
        <h1 id="login-title">تسجيل دخول الإدارة</h1>
        <p>أدخل بيانات حسابك للوصول إلى لوحة التحكم</p>
        <LoginForm />
        <small>الدخول متاح للمستخدمين المصرح لهم فقط</small>
      </section>
      <footer className="auth-footer">© 2026 مهندسيتو توريدات. جميع الحقوق محفوظة.</footer>
    </main>
  );
}
