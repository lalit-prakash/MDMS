import type { Metadata } from "next";
import { Roboto_Flex, Roboto_Mono, Noto_Sans } from "next/font/google";
import Script from "next/script";
import "./globals.css";
import { Providers } from "./providers";
import { AppShell } from "./AppShell";

const robotoFlex = Roboto_Flex({
  variable: "--font-roboto-flex",
  subsets: ["latin"],
});

const robotoMono = Roboto_Mono({
  variable: "--font-roboto-mono",
  subsets: ["latin"],
});

const notoSans = Noto_Sans({
  variable: "--font-noto-sans",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: "MDMS",
  description: "Meter Data Management System",
};

// Inline, pre-hydration: reads the persisted/system theme choice and stamps [data-theme] on
// <html> before first paint, so there's no light->dark flash for returning dark-mode users.
// ThemeModeProvider (client-side) reconciles React state to match on mount and owns all
// subsequent changes; this script only ever runs once, before anything else.
const THEME_INIT_SCRIPT = `(function(){try{var s=localStorage.getItem('mdms-theme-mode');var m=s==='light'||s==='dark'?s:(window.matchMedia('(prefers-color-scheme: dark)').matches?'dark':'light');document.documentElement.setAttribute('data-theme',m);}catch(e){document.documentElement.setAttribute('data-theme','light');}})();`;

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html
      lang="en"
      data-theme="light"
      className={`${robotoFlex.variable} ${robotoMono.variable} ${notoSans.variable}`}
    >
      <body>
        {/* next/script (not a raw JSX <script>) is the App-Router-supported way to run this
            synchronously before paint — a bare <script> tag triggers a React dev warning
            ("scripts inside React components are never executed") and isn't guaranteed to run
            pre-hydration the way this anti-flash script requires. */}
        <Script id="theme-init" strategy="beforeInteractive">
          {THEME_INIT_SCRIPT}
        </Script>
        <Providers>
          <AppShell>{children}</AppShell>
        </Providers>
      </body>
    </html>
  );
}
