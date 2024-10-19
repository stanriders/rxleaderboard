"use client";
import { Navbar as NextUINavbar, NavbarContent, NavbarMenu, NavbarMenuToggle, NavbarBrand, NavbarItem, NavbarMenuItem } from "@nextui-org/navbar";
import { link as linkStyles } from "@nextui-org/theme";
import { Link } from "@nextui-org/link";
import NextLink from "next/link";
import Image from "next/image";
import clsx from "clsx";
import { siteConfig } from "@/config/site";
import { ThemeSwitch } from "@/components/theme-switch";
import { usePathname } from 'next/navigation';

export const Navbar = () => {
  const pathname = usePathname();

  return (
    <NextUINavbar maxWidth="xl" position="sticky">
      <NavbarContent className="basis-1/5 sm:basis-full" justify="start">
        <NavbarMenuToggle className="md:hidden" />
        {/* logo */}
        <NavbarBrand as="li" className="gap-3 max-w-fit md:mx-2">
          <NextLink className="flex justify-center items-center gap-3 ml-3" href="/">
            <Image className="dark:hidden block flex-none" src="/rv-yellowdark.svg" width={32} height={32} alt="Relaxation Vault"/>
            <Image className="hidden dark:block flex-none" src="/rv-yellowlight.svg" width={32} height={32} alt="Relaxation Vault"/>
            <p className="font-bold text-primary-400 text-inherit">Relaxation Vault</p>
          </NextLink>
        </NavbarBrand>
        {/* normal */}
        <ul className="hidden md:flex gap-4 justify-start ml-2">
          {siteConfig.navItems.map((item) => (
            <NavbarItem key={item.href}>
              <Link as={NextLink}
                className={clsx(
                  linkStyles({ color: "foreground" }),
                  pathname === item.href ? "text-primary" : ""
                )}
                color="foreground"
                href={item.href}
              >
                {item.label}
              </Link>
            </NavbarItem>
          ))}
        </ul>
      </NavbarContent>
      <NavbarContent className="basis-1 pl-4" justify="end">
        <ThemeSwitch />
      </NavbarContent>
      <NavbarMenu>
        <div className="mx-4 mt-2 flex flex-col gap-2">
          {siteConfig.navItems.map((item, _) => (
            <NavbarMenuItem key={item.href}>
              <Link as={NextLink}
                className={clsx(
                  linkStyles({ color: "foreground" }),
                  pathname === item.href ? "text-primary" : ""
                )}
                color="foreground"
                href={item.href}
              >
                {item.label}
              </Link>
            </NavbarMenuItem>
          ))}
        </div>
      </NavbarMenu>
    </NextUINavbar>
  );
};
