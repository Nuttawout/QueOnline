// Maps a CARD_TEMPLATES id (client/src/config/cardTemplates.js) to its component — kept separate
// from that config file so the config stays plain data (importable without pulling in Vue SFCs).
import TemplateBoardingPass from './TemplateBoardingPass.vue';
import TemplateTracking from './TemplateTracking.vue';
import TemplateBoard from './TemplateBoard.vue';
import TemplateHealthRings from './TemplateHealthRings.vue';
import TemplateRestaurant from './TemplateRestaurant.vue';
import TemplateOfficial from './TemplateOfficial.vue';
import TemplateEcommerce from './TemplateEcommerce.vue';
import TemplateSwiss from './TemplateSwiss.vue';
import TemplateNeumorphic from './TemplateNeumorphic.vue';
import TemplateCinema from './TemplateCinema.vue';

export const TEMPLATE_COMPONENTS = {
  boardingPass: TemplateBoardingPass,
  tracking: TemplateTracking,
  board: TemplateBoard,
  healthRings: TemplateHealthRings,
  restaurant: TemplateRestaurant,
  official: TemplateOfficial,
  ecommerce: TemplateEcommerce,
  swiss: TemplateSwiss,
  neumorphic: TemplateNeumorphic,
  cinema: TemplateCinema,
};
