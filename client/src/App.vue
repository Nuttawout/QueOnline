<script setup>
import { watchEffect } from 'vue';
import { useTheme } from 'vuetify';
import BranchPickerView from './views/BranchPickerView.vue';
import QueueStatusView from './views/QueueStatusView.vue';
import AdminView from './views/AdminView.vue';
import SuperAdminView from './views/SuperAdminView.vue';
import { branchSlug, isAdminPath, isSuperAdminPath } from './composables/useBranch';
import { settings, loadSettings } from './composables/useSettings';
import { fontSizeScaleValue } from './config/fonts';

// No vue-router — four top-level pages, told apart by path segments: ['super-admin'] -> branch
// management, [] -> branch picker, [slug] -> queue status, [slug, 'admin'] -> admin. See
// useBranch.js for the one place this URL convention lives.
const theme = useTheme();

if (branchSlug) {
  loadSettings();
}

watchEffect(() => {
  if (!settings.loaded) return;
  theme.themes.value.lightTheme.colors.primary = settings.primaryColor;
  document.documentElement.style.setProperty('--accent-color', settings.accentColor);
  document.documentElement.style.setProperty('--font-thai', `'${settings.fontFamily}', system-ui, sans-serif`);
  document.documentElement.style.setProperty('--text-scale', String(fontSizeScaleValue(settings.fontSizeScale)));
});
</script>

<template>
  <v-app>
    <v-main>
      <SuperAdminView v-if="isSuperAdminPath" />
      <BranchPickerView v-else-if="!branchSlug" />
      <AdminView v-else-if="isAdminPath" />
      <QueueStatusView v-else />
    </v-main>
  </v-app>
</template>
