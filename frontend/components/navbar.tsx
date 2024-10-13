import { Navbar as NextUINavbar, NavbarContent, NavbarMenu, NavbarMenuToggle, NavbarBrand, NavbarItem, NavbarMenuItem } from "@nextui-org/navbar";
import { link as linkStyles } from "@nextui-org/theme";
import NextLink from "next/link";
import Image from "next/image";
import clsx from "clsx";
import { siteConfig } from "@/config/site";
import { ThemeSwitch } from "@/components/theme-switch";

export const Navbar = () => {
  return (
    <NextUINavbar maxWidth="xl" position="sticky">
      <NavbarContent className="basis-1/5 sm:basis-full" justify="start">
        <NavbarMenuToggle className="md:hidden" />
        {/* logo */}
        <NavbarBrand as="li" className="gap-3 max-w-fit">
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
              <NextLink
                className={clsx(
                  linkStyles({ color: "foreground" }),
                  "data-[active=true]:text-primary data-[active=true]:font-medium",
                )}
                color="foreground"
                href={item.href}
              >
                {item.label}
              </NextLink>
            </NavbarItem>
          ))}
        </ul>
      </NavbarContent>
      <NavbarContent className="basis-1 pl-4" justify="end">
        {/*<Input
          classNames={{
            base: "md:max-w-48 max-w-[10rem] h-10",
            mainWrapper: "h-full",
            input: "text-small",
            inputWrapper: "h-full font-normal text-default-500 bg-default-400/20 dark:bg-default-500/20",
          }}
          placeholder="Search player..."
          size="sm"
          type="search"
        />*/}
        <ThemeSwitch />
      </NavbarContent>
      <NavbarMenu>
        <div className="mx-4 mt-2 flex flex-col gap-2">
          {siteConfig.navItems.map((item, _) => (
            <NavbarMenuItem key={item.href}>
              <NextLink
                className={clsx(linkStyles({ color: "foreground" }),
                          "data-[active=true]:text-primary data-[active=true]:font-medium",
                          )}
                color="foreground"
                href={item.href}
              >
                {item.label}
              </NextLink>
            </NavbarMenuItem>
          ))}
        </div>
      </NavbarMenu>
    </NextUINavbar>
  );
};
