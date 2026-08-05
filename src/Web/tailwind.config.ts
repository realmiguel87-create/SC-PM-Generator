import type { Config } from "tailwindcss";

export default {
  darkMode: ["class"],
  content: ["./index.html", "./src/**/*.{ts,tsx}"],
  theme: {
    extend: {
      colors: {
        stirling: {
          purple: "var(--stirling-purple)",
          green: "var(--stirling-green)",
        },
        "purple-light": "var(--purple-light)",
        "green-light": "var(--green-light)",
        "purple-soft": "var(--purple-soft)",
        "green-soft": "var(--green-soft)",
        background: "var(--background)",
        "text-primary": "var(--text-primary)",
        "text-secondary": "var(--text-secondary)",
        success: "var(--success)",
        warning: "var(--warning)",
        critical: "var(--critical)",
        information: "var(--information)",
        border: "var(--border)",
        card: "var(--card)",
      },
      fontFamily: {
        sans: ["Segoe UI", "Inter", "system-ui", "sans-serif"],
      },
      borderRadius: {
        lg: "10px",
        md: "8px",
        sm: "6px",
      },
      boxShadow: {
        card: "0 1px 2px 0 rgb(0 0 0 / 0.04), 0 1px 3px 0 rgb(0 0 0 / 0.06)",
      },
    },
  },
  plugins: [],
} satisfies Config;
