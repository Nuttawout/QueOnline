// Maps a CARD_TEMPLATES id (client/src/config/cardTemplates.js) to its component — kept separate
// from that config file so the config stays plain data (importable without pulling in Vue SFCs).
import TemplateClassic from './TemplateClassic.vue';
import TemplateTicket from './TemplateTicket.vue';
import TemplateHero from './TemplateHero.vue';
import TemplateDashboard from './TemplateDashboard.vue';
import TemplateTimelineFocus from './TemplateTimelineFocus.vue';
import TemplateGradient from './TemplateGradient.vue';
import TemplateDuoSplit from './TemplateDuoSplit.vue';
import TemplateGlow from './TemplateGlow.vue';
import TemplateBubble from './TemplateBubble.vue';
import TemplateDark from './TemplateDark.vue';

export const TEMPLATE_COMPONENTS = {
  classic: TemplateClassic,
  ticket: TemplateTicket,
  hero: TemplateHero,
  dashboard: TemplateDashboard,
  timelineFocus: TemplateTimelineFocus,
  gradient: TemplateGradient,
  duoSplit: TemplateDuoSplit,
  glow: TemplateGlow,
  bubble: TemplateBubble,
  dark: TemplateDark,
};
