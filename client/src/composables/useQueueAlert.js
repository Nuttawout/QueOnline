import { ref } from 'vue';
import { settings } from './useSettings';
import { apiBaseUrl } from '../api/http';

// Alert levels, most urgent last so a skipped level (e.g. aheadCount jumps 5 -> 0 in one poll)
// still fires the single most relevant message instead of a stale "2 queues left".
const LEVELS = {
  NONE: 0,
  AHEAD_2: 1,
  AHEAD_1: 2,
  ARRIVED: 3,
};

// Admin-selectable sound presets (settings.soundChoice) — exported so the admin page can offer
// a "test sound" button per option without duplicating the note tables.
export const SOUND_PRESETS = {
  soft: {
    label: 'ระฆังนุ่ม (แนะนำ)',
    warning: [
      { freq: 783.99, start: 0, duration: 0.28 }, // G5
      { freq: 1046.5, start: 0.14, duration: 0.5 }, // C6
    ],
    arrival: [
      { freq: 1046.5, start: 0, duration: 0.22 }, // C6
      { freq: 1318.51, start: 0.12, duration: 0.22 }, // E6
      { freq: 1567.98, start: 0.24, duration: 0.45 }, // G6
    ],
  },
  bright: {
    label: 'สดใส',
    warning: [
      { freq: 1318.51, start: 0, duration: 0.18 }, // E6
      { freq: 1567.98, start: 0.1, duration: 0.35 }, // G6
    ],
    arrival: [
      { freq: 1318.51, start: 0, duration: 0.15 }, // E6
      { freq: 1567.98, start: 0.09, duration: 0.15 }, // G6
      { freq: 2093.0, start: 0.18, duration: 0.4 }, // C7
    ],
  },
  classic: {
    label: 'บี๊บคลาสสิก',
    warning: [
      { freq: 880, start: 0, duration: 0.15 },
      { freq: 880, start: 0.22, duration: 0.15 },
    ],
    arrival: [
      { freq: 880, start: 0, duration: 0.15 },
      { freq: 880, start: 0.2, duration: 0.15 },
      { freq: 880, start: 0.4, duration: 0.3 },
    ],
  },
  gentle: {
    label: 'ระฆังลม (เบาสบาย)',
    warning: [
      { freq: 1174.66, start: 0, duration: 0.4 }, // D6
      { freq: 987.77, start: 0.2, duration: 0.5 }, // B5
    ],
    arrival: [
      { freq: 1567.98, start: 0, duration: 0.3 }, // G6
      { freq: 1318.51, start: 0.15, duration: 0.3 }, // E6
      { freq: 1046.5, start: 0.3, duration: 0.5 }, // C6
    ],
  },
  marimba: {
    label: 'มาริมบา',
    warning: [
      { freq: 523.25, start: 0, duration: 0.2 }, // C5
      { freq: 659.25, start: 0.1, duration: 0.2 }, // E5
      { freq: 783.99, start: 0.2, duration: 0.35 }, // G5
    ],
    arrival: [
      { freq: 523.25, start: 0, duration: 0.15 },
      { freq: 659.25, start: 0.08, duration: 0.15 },
      { freq: 783.99, start: 0.16, duration: 0.15 },
      { freq: 1046.5, start: 0.24, duration: 0.45 },
    ],
  },
  deepbell: {
    label: 'ระฆังทุ้ม',
    warning: [
      { freq: 392.0, start: 0, duration: 0.45 }, // G4
      { freq: 523.25, start: 0.2, duration: 0.6 }, // C5
    ],
    arrival: [
      { freq: 392.0, start: 0, duration: 0.3 },
      { freq: 523.25, start: 0.15, duration: 0.3 },
      { freq: 659.25, start: 0.3, duration: 0.6 },
    ],
  },
  urgent: {
    label: 'เร่งด่วน',
    warning: [
      { freq: 1046.5, start: 0, duration: 0.12 },
      { freq: 1046.5, start: 0.16, duration: 0.12 },
      { freq: 1046.5, start: 0.32, duration: 0.12 },
    ],
    arrival: [
      { freq: 1318.51, start: 0, duration: 0.12 },
      { freq: 1318.51, start: 0.15, duration: 0.12 },
      { freq: 1318.51, start: 0.3, duration: 0.12 },
      { freq: 1318.51, start: 0.45, duration: 0.3 },
    ],
  },
  silent: {
    label: 'ปิดเสียง',
    warning: [],
    arrival: [],
  },
};

function playSynthesizedChime(notes) {
  if (notes.length === 0) return;
  try {
    const AudioCtx = window.AudioContext || window.webkitAudioContext;
    const ctx = new AudioCtx();
    const now = ctx.currentTime;

    for (const { freq, start, duration } of notes) {
      const osc = ctx.createOscillator();
      const gain = ctx.createGain();
      osc.type = 'sine';
      osc.frequency.value = freq;
      osc.connect(gain);
      gain.connect(ctx.destination);

      const t0 = now + start;
      gain.gain.setValueAtTime(0, t0);
      gain.gain.linearRampToValueAtTime(0.22, t0 + 0.02);
      gain.gain.exponentialRampToValueAtTime(0.001, t0 + duration);
      osc.start(t0);
      osc.stop(t0 + duration + 0.05);
    }

    setTimeout(() => ctx.close(), 1200);
  } catch {
    // Web Audio unavailable — the in-page toast and OS notification still carry the alert.
  }
}

/**
 * Plays either a synthesized preset or the admin-uploaded custom sound file (settings.soundChoice
 * === 'custom'). Exported for the admin page's "test sound" preview button.
 */
export function playChime(celebratory, soundChoice = settings.soundChoice) {
  if (soundChoice === 'custom') {
    if (!settings.customSoundUrl) return;
    try {
      new Audio(apiBaseUrl + settings.customSoundUrl).play().catch(() => {});
    } catch {
      // Playback blocked (e.g. no user gesture yet) — the toast/OS notification still carry the alert.
    }
    return;
  }

  const preset = SOUND_PRESETS[soundChoice] ?? SOUND_PRESETS.soft;
  playSynthesizedChime(celebratory ? preset.arrival : preset.warning);
}

export function useQueueAlert() {
  const permission = ref(typeof Notification !== 'undefined' ? Notification.permission : 'unsupported');
  const firedLevel = ref(LEVELS.NONE);
  // Set every time an alert fires — the view watches this to show an in-page toast, since sound
  // and the OS notification are easy to miss (no permission granted, phone on silent, etc.) and
  // give no on-screen explanation of what just happened on their own.
  const lastAlert = ref(null);

  async function requestPermission() {
    if (typeof Notification === 'undefined') return;
    permission.value = await Notification.requestPermission();
  }

  function fire(title, body, celebratory = false) {
    playChime(celebratory);
    if (navigator.vibrate) navigator.vibrate([200, 100, 200]);
    if (permission.value === 'granted') {
      try {
        new Notification(title, { body, tag: 'que-online-alert' });
      } catch {
        // Some mobile browsers (notably iOS Safari) don't support the Notification constructor
        // even when permission is granted — the beep/vibrate/in-page toast still fire.
      }
    }
    // Always show something on-screen too — permission may be denied, the tab may not have focus
    // for the OS popup to register, or the phone may be on silent, but the toast never depends on any of that.
    lastAlert.value = { title, body, id: Date.now() };
  }

  /**
   * Call on every poll with the latest lookup result. Fires at most once per level per search
   * (reset() starts a fresh search over). `arrived` should reflect the queue's own "called" step,
   * not aheadCount === 0, since 0-ahead-but-not-yet-called and already-called are different states.
   */
  function check({ aheadCount, arrived, label }) {
    let level = LEVELS.NONE;
    if (arrived) level = LEVELS.ARRIVED;
    else if (typeof aheadCount === 'number' && aheadCount <= 1) level = LEVELS.AHEAD_1;
    else if (typeof aheadCount === 'number' && aheadCount <= 2) level = LEVELS.AHEAD_2;

    if (level <= firedLevel.value || level === LEVELS.NONE) return;
    firedLevel.value = level;

    if (level === LEVELS.ARRIVED) {
      const body = label ? `${settings.arrivedBody} คิวหมายเลข ${label}` : settings.arrivedBody;
      fire(settings.arrivedTitle, body, true);
    } else if (level === LEVELS.AHEAD_1) {
      // aheadCount can already be 0 here (next in line, not yet called) — word it as clearly
      // distinct from "arrived" (ARRIVED fires separately, only once actually called) so the two
      // states are never confused for each other on a quick glance.
      const title = aheadCount <= 0 ? 'คิวถัดไปคือคิวของท่าน' : 'อีก 1 คิวจะถึงคิวของท่าน';
      const body = aheadCount <= 0 ? 'กรุณาเตรียมตัว รอเรียกอีกไม่นาน' : label ? `คิวหมายเลข ${label} เตรียมตัวได้เลย` : undefined;
      fire(title, body);
    } else if (level === LEVELS.AHEAD_2) {
      fire('อีก 2 คิวจะถึงคิวของท่าน', label ? `คิวหมายเลข ${label} เตรียมตัวได้เลย` : undefined);
    }
  }

  function reset() {
    firedLevel.value = LEVELS.NONE;
  }

  return { permission, requestPermission, check, reset, lastAlert };
}
