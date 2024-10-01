export type SiteConfig = typeof siteConfig;

export const siteConfig = {
  name: "Relaxation Vault",
  description: "Leaderboard of osu!lazer relax scores",
  navItems: [
    {
      label: "Leaderboard",
      href: "/leaderboard",
    },
    {
      label: "Top scores",
      href: "/topscores",
    },
    {
      label: "Add score",
      href: "/add-score",
    },
  ]
};
