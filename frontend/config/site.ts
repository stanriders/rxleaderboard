export type SiteConfig = typeof siteConfig;

export const siteConfig = {
  name: "Relaxation Vault",
  description: "Leaderboard of osu!lazer relax players",
  navItems: [
    {
      label: "Leaderboard",
      href: "/leaderboard",
    },
    {
      label: "Top scores",
      href: "/topscores",
    },
  ],
  navMenuItems: [
    {
      label: "Leaderboard",
      href: "/leaderboard",
    },
    {
      label: "Top scores",
      href: "/topscores",
    },
  ],
};
