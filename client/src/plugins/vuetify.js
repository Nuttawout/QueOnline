import 'vuetify/styles';
import '@mdi/font/css/materialdesignicons.css';
import { createVuetify } from 'vuetify';
import * as components from 'vuetify/components';
import * as directives from 'vuetify/directives';

// Independent design for QueOnline (not tied to the AIAPRO/Klein Pro token set) —
// a calm medical blue/teal pair chosen for a public, one-hand mobile screen.
const lightTheme = {
  dark: false,
  colors: {
    primary: '#102a4d',
    'primary-darken-1': '#0a1c35',
    secondary: '#00897b',
    background: '#f4f6fb',
    surface: '#ffffff',
    success: '#2e7d32',
    warning: '#ed6c02',
    error: '#c62828',
    info: '#0277bd',
  },
};

const darkTheme = {
  dark: true,
  colors: {
    primary: '#5b8def',
    secondary: '#4db6ac',
    background: '#0f1420',
    surface: '#161d2e',
    success: '#66bb6a',
    warning: '#ffa726',
    error: '#ef5350',
    info: '#4fc3f7',
  },
};

export default createVuetify({
  components,
  directives,
  theme: {
    defaultTheme: 'lightTheme',
    themes: { lightTheme, darkTheme },
  },
  defaults: {
    VBtn: { rounded: 'lg' },
    VCard: { rounded: 'lg' },
    VTextField: { variant: 'outlined', density: 'comfortable' },
  },
});
