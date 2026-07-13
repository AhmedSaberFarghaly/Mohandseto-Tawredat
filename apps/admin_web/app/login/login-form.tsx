"use client";

import { FormEvent, useState } from "react";
import { useRouter } from "next/navigation";

type Step = "login" | "twoFactor" | "forgot" | "reset" | "success" | "role";

const roleNames: Record<string, string> = {
  super_admin: "مدير النظام الأعلى",
  platform_admin: "مدير المنصة",
  operations_manager: "مدير العمليات",
  sales_manager: "مدير المبيعات",
  sales_agent: "موظف المبيعات",
  quotes_officer: "مسؤول عروض الأسعار",
  products_manager: "مدير المنتجات",
  inventory_manager: "مسؤول المخزون",
  warehouse_manager: "مسؤول المستودع",
  procurement_officer: "مسؤول المشتريات",
  accountant: "مسؤول الحسابات",
  support_agent: "خدمة العملاء",
  graphic_designer: "مصمم جرافيك",
  printing_officer: "مسؤول الطباعة",
  delivery_driver: "مندوب التوصيل",
  system_admin: "مسؤول النظام",
  auditor: "مراجع - قراءة فقط",
};

export function LoginForm() {
  const router = useRouter();
  const [step, setStep] = useState<Step>("login");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [email, setEmail] = useState("");
  const [roles, setRoles] = useState<string[]>([]);
  const [developmentCode, setDevelopmentCode] = useState<string | null>(null);
  const [maskedPhone, setMaskedPhone] = useState<string | null>(null);

  function fail(reason: unknown, fallback: string) {
    setError(reason instanceof Error ? reason.message : fallback);
  }

  function completeAuthentication(nextRoles: string[]) {
    setRoles(nextRoles);
    setStep("success");
    window.setTimeout(() => setStep("role"), 850);
  }

  async function submitLogin(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setLoading(true); setError("");
    const data = new FormData(event.currentTarget);
    const submittedEmail = String(data.get("email") ?? "");
    setEmail(submittedEmail);
    try {
      const response = await fetch("/api/session/login", {
        method: "POST", headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email: submittedEmail, password: data.get("password") }),
      });
      const body = await response.json();
      if (!response.ok) throw new Error(body.message || "تعذر تسجيل الدخول");
      if (body.requiresTwoFactor) {
        setDevelopmentCode(body.developmentCode ?? null); setStep("twoFactor"); return;
      }
      completeAuthentication(body.roles ?? []);
    } catch (reason) { fail(reason, "تعذر تسجيل الدخول"); }
    finally { setLoading(false); }
  }

  async function submitTwoFactor(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setLoading(true); setError("");
    const data = new FormData(event.currentTarget);
    try {
      const response = await fetch("/api/session/2fa", {
        method: "POST", headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ code: data.get("code") }),
      });
      const body = await response.json();
      if (!response.ok) throw new Error(body.message || "رمز التحقق غير صحيح");
      completeAuthentication(body.roles ?? []);
    } catch (reason) { fail(reason, "تعذر التحقق"); }
    finally { setLoading(false); }
  }

  async function requestReset(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setLoading(true); setError("");
    const data = new FormData(event.currentTarget);
    const submittedEmail = String(data.get("email") ?? "");
    setEmail(submittedEmail);
    try {
      const response = await fetch("/api/session/password/request", {
        method: "POST", headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email: submittedEmail }),
      });
      const body = await response.json();
      if (!response.ok) throw new Error(body.message || "تعذر إرسال الرمز");
      setMaskedPhone(body.maskedPhone ?? null);
      setDevelopmentCode(body.developmentCode ?? null);
      setStep("reset");
    } catch (reason) { fail(reason, "تعذر إرسال الرمز"); }
    finally { setLoading(false); }
  }

  async function submitReset(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setLoading(true); setError("");
    const data = new FormData(event.currentTarget);
    if (data.get("password") !== data.get("confirmPassword")) {
      setError("كلمتا المرور غير متطابقتين"); setLoading(false); return;
    }
    try {
      const response = await fetch("/api/session/password/reset", {
        method: "POST", headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ code: data.get("code"), newPassword: data.get("password") }),
      });
      const body = await response.json();
      if (!response.ok) throw new Error(body.message || "تعذر تعيين كلمة المرور");
      setDevelopmentCode(null); setStep("login");
    } catch (reason) { fail(reason, "تعذر تعيين كلمة المرور"); }
    finally { setLoading(false); }
  }

  async function selectRole(role: string) {
    setLoading(true); setError("");
    try {
      const response = await fetch("/api/session/role", {
        method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ role }),
      });
      if (!response.ok) throw new Error("تعذر اختيار الدور");
      router.replace("/dashboard"); router.refresh();
    } catch (reason) { fail(reason, "تعذر اختيار الدور"); setLoading(false); }
  }

  const heading = {
    login: ["لوحة إدارة مهندسيتو", "سجّل الدخول بحساب العمل الخاص بك"],
    twoFactor: ["المصادقة الثنائية", "أدخل الرمز المرسل إلى هاتفك المسجل"],
    forgot: ["استعادة كلمة المرور", "سنرسل رمز تحقق إلى الهاتف المرتبط بالحساب"],
    reset: ["تعيين كلمة مرور جديدة", maskedPhone ? `تم إرسال الرمز إلى ${maskedPhone}` : "أدخل رمز التحقق وكلمة المرور الجديدة"],
    success: ["تم تسجيل الدخول بنجاح", "جاري تجهيز مساحة العمل الآمنة"],
    role: ["اختر نطاق العمل", "حدد الدور الذي تريد الدخول به إلى لوحة الإدارة"],
  }[step];

  return (
    <>
      <div className={`auth-state-icon ${step === "success" ? "success" : ""}`}>
        {step === "success" ? "✓" : step === "twoFactor" ? "⌁" : step === "forgot" || step === "reset" ? "↻" : "ت"}
      </div>
      <h1 id="login-title">{heading[0]}</h1><p>{heading[1]}</p>
      {error && <div className="form-error auth-global-error" role="alert">{error}</div>}

      {step === "login" && <form className="auth-form" onSubmit={submitLogin}>
        <label>البريد الإلكتروني<input name="email" type="email" defaultValue={email} placeholder="admin@company.com" required autoComplete="email" /></label>
        <label>كلمة المرور<input name="password" type="password" placeholder="••••••••" minLength={8} required autoComplete="current-password" /></label>
        <div className="form-options">
          <label className="check-label"><input type="checkbox" name="remember" /> تذكرني</label>
          <button type="button" className="text-button" onClick={() => { setError(""); setStep("forgot"); }}>نسيت كلمة المرور؟</button>
        </div>
        <button className="primary-button" disabled={loading}>{loading ? "جاري تسجيل الدخول..." : "تسجيل الدخول"}</button>
      </form>}

      {step === "twoFactor" && <form className="auth-form" onSubmit={submitTwoFactor}>
        <label>رمز التحقق<input className="otp-input" name="code" inputMode="numeric" pattern="[0-9]{6}" maxLength={6} placeholder="000000" autoFocus required autoComplete="one-time-code" /></label>
        {developmentCode && <div className="development-code">رمز بيئة التطوير: <b>{developmentCode}</b></div>}
        <button className="primary-button" disabled={loading}>{loading ? "جاري التحقق..." : "تأكيد الدخول"}</button>
        <button type="button" className="auth-back" onClick={() => setStep("login")}>العودة لتسجيل الدخول</button>
      </form>}

      {step === "forgot" && <form className="auth-form" onSubmit={requestReset}>
        <label>البريد الإلكتروني<input name="email" type="email" defaultValue={email} placeholder="admin@company.com" required autoComplete="email" /></label>
        <button className="primary-button" disabled={loading}>{loading ? "جاري الإرسال..." : "إرسال رمز الاستعادة"}</button>
        <button type="button" className="auth-back" onClick={() => setStep("login")}>العودة لتسجيل الدخول</button>
      </form>}

      {step === "reset" && <form className="auth-form" onSubmit={submitReset}>
        <label>رمز التحقق<input className="otp-input" name="code" inputMode="numeric" pattern="[0-9]{6}" maxLength={6} placeholder="000000" required /></label>
        <label>كلمة المرور الجديدة<input name="password" type="password" minLength={8} required autoComplete="new-password" /></label>
        <label>تأكيد كلمة المرور<input name="confirmPassword" type="password" minLength={8} required autoComplete="new-password" /></label>
        {developmentCode && <div className="development-code">رمز بيئة التطوير: <b>{developmentCode}</b></div>}
        <button className="primary-button" disabled={loading}>{loading ? "جاري الحفظ..." : "حفظ كلمة المرور"}</button>
      </form>}

      {step === "success" && <div className="login-success"><span>✓</span><b>تم التحقق من هويتك</b><small>اتصال آمن ومشفّر</small></div>}

      {step === "role" && <div className="role-options">
        {(roles.length ? roles : ["super_admin"]).map((role, index) =>
          <button key={role} disabled={loading} onClick={() => selectRole(role)} className={index === 0 ? "selected" : ""}>
            <span>{index === 0 ? "★" : "◇"}</span><div><b>{roleNames[role] ?? role}</b><small>{role === "super_admin" ? "صلاحية كاملة على كل وحدات المنصة" : "صلاحيات وفق سياسات الدور"}</small></div><i>←</i>
          </button>)}
      </div>}

      <small className="auth-security-note">♢ الدخول متاح للمستخدمين المصرح لهم فقط</small>
    </>
  );
}
