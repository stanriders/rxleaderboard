/* eslint-disable react/no-unknown-property */
import "@/styles/globals.css";
import { Metadata, Viewport } from "next";
import Image from "next/image";
import { Link } from "@nextui-org/link";
import clsx from "clsx";

import { Providers } from "./providers";

import { siteConfig } from "@/config/site";
import { fontSans } from "@/config/fonts";
import { Navbar } from "@/components/navbar";
import { ApiBase } from "@/api/address";

export const metadata: Metadata = {
  title: {
    default: siteConfig.name,
    template: `%s - ${siteConfig.name}`,
  },
  applicationName: siteConfig.name,
  authors: [{name: "StanR", url: "https://osu.ppy.sh/users/7217455"}],
  description: siteConfig.description,
  icons: {
    icon: "/favicon.ico",
    shortcut: "/favicon.ico",
    apple: "/rv-yellowlight-192.png",
  },
  manifest: "/site.webmanifest",
  openGraph: {
    siteName: siteConfig.name,
    type: "website",
    images: ["https://rx.stanr.info/rv-yellowlight-192.png"],
  },
  twitter: {
    card: 'summary'
  }
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
  if (ApiBase == "https://rx.stanr.info/api")
    return (<html suppressHydrationWarning lang="en" prefix="og: http://ogp.me/ns#">
      <head />
      <body
        className={clsx(
          "min-h-screen bg-background font-sans antialiased",
          fontSans.variable,
        )}
      >
        <Providers themeProps={{ attribute: "class", defaultTheme: "dark" }}>
          <div className="relative flex flex-col h-screen">
            <main className="container mx-auto max-w-7xl pt-2 px-1 md:px-6 flex-grow">
            <section className="flex flex-col items-center justify-center gap-4 py-4">
              <div className="inline-block w-full text-center justify-center">
                <section className="flex flex-col items-center justify-center gap-4 py-8 md:py-10 text-xl">
                  <Image src="/rv-yellowlight.svg" alt="Relaxation vault" width={256} height={256}/>
                  Sorry, this website is not ready yet!
                </section>
              </div>
            </section>
            </main>
          </div>
        </Providers>
        <script defer src="https://umami.stanr.info/script.js" data-website-id="516606cb-17b7-4166-8315-c47fef8d73dd"></script>
      </body>
    </html>);

  return (
    <html suppressHydrationWarning lang="en" prefix="og: http://ogp.me/ns#">
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
