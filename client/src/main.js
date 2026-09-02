import { createApp } from 'vue';
// Font choices offered in the admin panel (client/src/config/fonts.js) — all self-hosted via
// @fontsource, no external font CDN. Only 400/500 weights are used anywhere in this app's own
// styling per the design-token spec (2026-08-31) — 700 is imported for legacy/admin-provided
// text that isn't ours to restyle (e.g. hospital-entered greeting text rendered bold by Vuetify
// utility classes elsewhere), not for new UI.
import '@fontsource/anuphan/400.css';
import '@fontsource/anuphan/500.css';
import '@fontsource/ibm-plex-sans-thai/400.css';
import '@fontsource/ibm-plex-sans-thai/500.css';
import '@fontsource/ibm-plex-sans-thai/700.css';
import '@fontsource/sarabun/400.css';
import '@fontsource/sarabun/500.css';
import '@fontsource/sarabun/700.css';
import '@fontsource/kanit/400.css';
import '@fontsource/kanit/500.css';
import '@fontsource/kanit/700.css';
import '@fontsource/noto-sans-thai/400.css';
import '@fontsource/noto-sans-thai/500.css';
import '@fontsource/noto-sans-thai/700.css';
import './styles/global.css';
import App from './App.vue';
import vuetify from './plugins/vuetify';

createApp(App).use(vuetify).mount('#app');
