import type { SidebarsConfig } from '@docusaurus/plugin-content-docs';

const sidebars: SidebarsConfig = {
  archSidebar: [
    'index',
    {
      type: 'category',
      label: 'Mimari',
      items: [
        'architecture/project-scope',
        'architecture/module-design',
        'architecture/business-rules',
        'architecture/3308-kanun-ozeti',
        'architecture/wolverine-patterns',
        'architecture/user-onboarding',
        'architecture/web-ui',
      ],
    },
    {
      type: 'category',
      label: 'Aktörler',
      items: ['actors/actors', 'actors/permissions'],
    },
    'scenarios',
    {
      type: 'category',
      label: 'Altyapı',
      items: [
        'infrastructure/keycloak',
        'infrastructure/gis-osrm',
      ],
    },
    {
      type: 'category',
      label: 'Modüller',
      items: [
        'modules/c4-diagrams',
        'modules/business',
        'modules/enrollment',
        'modules/contract',
        'modules/attendance',
        'modules/payment',
        'modules/coordination',
        'modules/internship',
        'modules/reporting',
        'modules/institution',
        'modules/tenant',
      ],
    },
  ],
};

export default sidebars;
