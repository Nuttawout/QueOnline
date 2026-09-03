import { onMounted, onUnmounted, ref } from 'vue';

/** A ref that ticks to `new Date()` every second — the "walking" real-time clock shown on every
 * queue card template. Each template owns its own instance/interval; that's safe here because only
 * one template ever renders live at a time (the active status card on the patient screen, or the
 * admin's single preview pane — see the 2026-09-02 admin picker redesign). */
export function useLiveClock() {
  const now = ref(new Date());
  let handle = null;

  onMounted(() => {
    handle = setInterval(() => {
      now.value = new Date();
    }, 1000);
  });

  onUnmounted(() => clearInterval(handle));

  return now;
}
