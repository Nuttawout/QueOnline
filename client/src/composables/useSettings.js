import { reactive } from 'vue';
import { http } from '../api/http';
import { apiPath } from './useBranch';
import { DEFAULT_FIELD_LAYOUT, normalizeFieldLayout } from '../config/fieldLayout';
import { DEFAULT_CARD_TEMPLATE } from '../config/cardTemplates';

// Module-level singleton — fetched once, shared by every component (main queue view, admin
// page) so branding/theme/sound choices apply everywhere without prop-drilling. Defaults here
// match the server's AppSettingsRecord defaults, used only until the real fetch resolves (or if
// it fails — the page should still render something sensible offline).
export const settings = reactive({
  hospitalName: 'โรงพยาบาลวิภาราม',
  tagline: 'ระบบติดตามคิวออนไลน์',
  greeting: 'สวัสดีค่ะ ยินดีต้อนรับ',
  arrivedTitle: 'ถึงคิวของท่านแล้ว',
  arrivedBody: 'กรุณาเข้ารับบริการ',
  primaryColor: '#102a4d',
  accentColor: '#e8720f',
  fontFamily: 'Anuphan',
  fontSizeScale: 'medium',
  soundChoice: 'soft',
  logoUrl: null,
  customSoundUrl: null,
  fieldLayout: { ...DEFAULT_FIELD_LAYOUT },
  cardTemplate: DEFAULT_CARD_TEMPLATE,
  loaded: false,
});

let loadPromise = null;

export function loadSettings() {
  if (!loadPromise) {
    loadPromise = http
      .get(apiPath('/settings'))
      .then(({ data }) => {
        Object.assign(settings, data);
        settings.fieldLayout = normalizeFieldLayout(data.fieldLayoutJson);
      })
      .catch(() => {
        // Keep the built-in defaults above — the page still has to work if the API is unreachable.
      })
      .finally(() => {
        settings.loaded = true;
      });
  }
  return loadPromise;
}

/** Called after saving in the admin page so every open tab reflects the change immediately. */
export function applySettings(patch) {
  Object.assign(settings, patch);
  if (patch.fieldLayoutJson !== undefined) {
    settings.fieldLayout = normalizeFieldLayout(patch.fieldLayoutJson);
  }
}
