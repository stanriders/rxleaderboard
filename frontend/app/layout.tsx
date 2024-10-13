import "@/styles/globals.css";
import { Metadata, Viewport } from "next";
import { Link } from "@nextui-org/link";
import clsx from "clsx";

import { Providers } from "./providers";

import { siteConfig } from "@/config/site";
import { fontSans } from "@/config/fonts";
import { Navbar } from "@/components/navbar";

export const metadata: Metadata = {
  title: {
    default: siteConfig.name,
    template: `%s - ${siteConfig.name}`,
  },
  description: siteConfig.description,
  icons: {
    icon: "/favicon.ico",
  },
  manifest: "/site.webmanifest",
  openGraph: {
    images: ["/rv-yellowlight.svg"]
  },
};

// all fetch request should be cached for 1 minute unless specified otherwise
export const revalidate = 60;

export const viewport: Viewport = {
  themeColor: [
    { media: "(prefers-color-scheme: light)", color: "white" },
    { media: "(prefers-color-scheme: dark)", color: "black" },
  ],
};

export default function RootLayout({ children, }: { children: React.ReactNode; }) {
  return (
    <html suppressHydrationWarning lang="en">
      <head />
      <body
        className={clsx(
          "min-h-screen bg-background font-sans antialiased",
          fontSans.variable,
        )}
      >
        <Providers themeProps={{ attribute: "class", defaultTheme: "dark" }}>
          <div className="relative flex flex-col h-screen">
            <Navbar />
            <main className="container mx-auto max-w-7xl pt-2 px-1 md:px-6 flex-grow">
            <section className="flex flex-col items-center justify-center gap-4 py-4">
              <div className="inline-block w-full text-center justify-center">
                {children}
              </div>
            </section>
            </main>
            <footer className="w-full flex items-center justify-center py-3">

            <p className="text-default-300 text-sm">
              <Link
                isExternal
                size="sm"
                className="gap-1 text-current"
                href="https://osu.ppy.sh/users/7217455"
                title="osu! profile"
              >Made by StanR</Link> | <Link
                isExternal
                size="sm"
                className="gap-1 text-current"
                href="https://github.com/stanriders/rxleaderboard"
                title="GitHub"
              >Source code</Link> | <Link
              isExternal
              size="sm"
              className="gap-1 text-current"
              href="https://ko-fi.com/stanr"
              title="Ko-fi"
            >Donate ❤</Link>
            </p>
            </footer>
          </div>
        </Providers>
        <script defer src="https://umami.stanr.info/script.js" data-website-id="516606cb-17b7-4166-8315-c47fef8d73dd"></script>
      </body>
    </html>
  );
}
