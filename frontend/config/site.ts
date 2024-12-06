export type SiteConfig = typeof siteConfig;

export const siteConfig = {
  name: "Relaxation Vault",
  description: "Leaderboard of osu!lazer relax scores",
  navItems: [
    {
      label: "FAQ",
      href: "/faq",
    },
    {
      label: "Leaderboard",
      href: "/leaderboard",
    },
    {
      label: "Top scores",
      href: "/topscores",
    },
    {
      label: "Beatmaps",
      href: "/beatmaps",
    }
  ]
};
