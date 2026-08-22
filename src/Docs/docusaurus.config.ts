import type { Config } from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';
import { remarkKroki } from 'remark-kroki';

const config: Config = {
  title: 'MESNET Mimari Dokümanlar',
  tagline: 'Mesleki Eğitim Stajları Nitelikli, Eşgüdümlü Takip Sistemi',
  favicon: 'img/favicon.ico',
  url: 'http://localhost',
  baseUrl: '/',
  // CI doküman sitesini derliyor (#186). Bu üç ayar 'warn' kalsaydı iş yeşil geçerdi ve
  // kontrol yalnız "webpack çöktü mü" sorusunu yanıtlardı — kırık bağlantı sessizce kalırdı.
  onBrokenLinks: 'throw',
  onBrokenAnchors: 'throw',
  i18n: { defaultLocale: 'tr', locales: ['tr'] },

  markdown: {
    format: 'detect',
    hooks: {
      onBrokenMarkdownLinks: 'throw',
    },
  },

  presets: [
    [
      'classic',
      {
        docs: {
          routeBasePath: '/',
          sidebarPath: './sidebars.ts',
          remarkPlugins: [
            [
              remarkKroki,
              {
                server: process.env.KROKI_SERVER || 'http://localhost:9222',
                alias: ['plantuml'],
                target: 'mdx3',
              },
            ],
          ],
        },
        blog: false,
        theme: { customCss: './src/css/custom.css' },
      } satisfies Preset.Options,
    ],
  ],

  themeConfig: {
    navbar: {
      title: 'MESNET',
      items: [
        {
          type: 'docSidebar',
          sidebarId: 'archSidebar',
          position: 'left',
          label: 'Mimari',
        },
      ],
    },
    footer: {
      style: 'dark',
      copyright: `MESNET Mimari Dokümantasyon`,
    },
    colorMode: {
      defaultMode: 'light',
      respectPrefersColorScheme: true,
    },
  } satisfies Preset.ThemeConfig,
};

export default config;
