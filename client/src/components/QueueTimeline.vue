<script setup>
defineProps({
  steps: { type: Array, required: true },
});

function formatTime(iso) {
  if (!iso) return null;
  return new Date(iso).toLocaleTimeString('th-TH', { hour: '2-digit', minute: '2-digit' });
}
</script>

<template>
  <v-timeline density="compact" side="end" truncate-line="both">
    <v-timeline-item
      v-for="step in steps"
      :key="step.key"
      :dot-color="step.done ? 'primary' : 'grey-lighten-1'"
      size="small"
    >
      <div class="d-flex justify-space-between align-center ga-2 flex-wrap">
        <div>
          <div :class="['text-body-1', step.done ? 'font-weight-bold' : 'text-medium-emphasis']">
            {{ step.label }}
          </div>
          <div v-if="step.location" class="text-caption text-medium-emphasis">
            {{ step.location }}
          </div>
        </div>
        <div class="text-caption text-medium-emphasis">
          {{ formatTime(step.at) ?? 'ยังไม่ถึง' }}
        </div>
      </div>
    </v-timeline-item>
  </v-timeline>
</template>
