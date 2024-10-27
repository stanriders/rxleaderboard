/** @type {import('next-sitemap').IConfig} */
module.exports = {
    siteUrl: process.env.SITE_URL || 'https://rx.stanr.info',
    generateRobotsTxt: true,
    robotsTxtOptions: {
      policies: [
        {
          userAgent: 'GPTBot',
          disallow: '/',
        },
      ],
    }
}