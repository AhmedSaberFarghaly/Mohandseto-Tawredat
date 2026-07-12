"use client";

import { FormEvent, useState } from "react";
import { useRouter } from "next/navigation";

export function LoginForm() {
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setLoading(true);
    setError("");
    const data = new FormData(event.currentTarget);
    try {
      const response = await fetch("/api/session/login", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          email: data.get("email"),
          password: data.get("password"),
        }),
      });
      const body = await response.json();
      if (!response.ok) throw new Error(body.message || "تعذر تسجيل الدخول");
      router.replace("/dashboard");
      router.refresh();
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "تعذر تسجيل الدخول");
    } finally {
      setLoading(false);
    }
  }

  return (
    <form className="auth-form" onSubmit={submit}>
      {error && <div className="form-error" role="alert">{error}</div>}
      <label>
        البريد الإلكتروني
        <input name="email" type="email" placeholder="admin@company.com" required autoComplete="email" />
      </label>
      <label>
        كلمة المرور
        <input name="password" type="password" placeholder="••••••••" minLength={8} required autoComplete="current-password" />
      </label>
      <div className="form-options">
        <label className="check-label"><input type="checkbox" name="remember" /> تذكرني</label>
        <button type="button" className="text-button">نسيت كلمة المرور؟</button>
      </div>
      <button className="primary-button" disabled={loading}>
        {loading ? "جاري تسجيل الدخول..." : "تسجيل الدخول"}
      </button>
    </form>
  );
}
