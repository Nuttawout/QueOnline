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
// 2026-09-02 — fonts used by the 10 fixed queue-card templates (client/src/components/templates/)
// only. Each template picks a distinct real-world-referenced pairing; only ever one is active per
// hospital at a time, but every family still ships bundled (same offline-first rule as above) so
// the browser fetches whichever one it actually needs on demand.
import '@fontsource/ibm-plex-mono/500.css';
import '@fontsource/ibm-plex-mono/700.css';
import '@fontsource/plus-jakarta-sans/700.css';
import '@fontsource/plus-jakarta-sans/800.css';
import '@fontsource/prompt/400.css';
import '@fontsource/prompt/600.css';
import '@fontsource/prompt/700.css';
import '@fontsource/chakra-petch/500.css';
import '@fontsource/chakra-petch/600.css';
import '@fontsource/big-shoulders-display/900'; // this package's exports map lacks a "./*.css" subpath (unlike every other @fontsource package here) — omitting the extension lets its "./*" -> "./*.css" pattern resolve correctly instead of 404ing on a literal "900.css.css"
import '@fontsource/manrope/800.css';
import '@fontsource/mitr/400.css';
import '@fontsource/mitr/600.css';
import '@fontsource/fredoka/700.css';
import '@fontsource/archivo/700.css';
import '@fontsource/archivo/900.css';
import '@fontsource/k2d/400.css';
import '@fontsource/k2d/600.css';
import '@fontsource/quicksand/700.css';
import '@fontsource/bebas-neue/400.css';
import './styles/global.css';
import App from './App.vue';
import vuetify from './plugins/vuetify';

createApp(App).use(vuetify).mount('#app');
