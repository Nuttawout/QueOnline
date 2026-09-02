import { ref } from 'vue';
import { http } from '../api/http';
import { apiPath } from './useBranch';

function urlBase64ToUint8Array(base64String) {
  const padding = '='.repeat((4 - (base64String.length % 4)) % 4);
  const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
  const rawData = window.atob(base64);
  return Uint8Array.from([...rawData].map((c) => c.charCodeAt(0)));
}

const supported = typeof navigator !== 'undefined' && 'serviceWorker' in navigator && 'PushManager' in window;

export function usePushSubscription() {
  const subscribed = ref(false);
  const loading = ref(false);
  const error = ref('');

  /** target: { hn?, qnumber? } — identifies which queue row to watch. */
  async function subscribe(target) {
    if (!supported) {
      error.value = 'เบราว์เซอร์นี้ไม่รองรับการแจ้งเตือนแบบพุช';
      return false;
    }

    loading.value = true;
    error.value = '';

    try {
      const permission = await Notification.requestPermission();
      if (permission !== 'granted') {
        error.value = 'ไม่ได้รับอนุญาตให้แจ้งเตือน';
        return false;
      }

      const registration = await navigator.serviceWorker.register('/sw.js');
      await navigator.serviceWorker.ready;

      const { data } = await http.get(apiPath('/push/public-key'));

      let pushSubscription = await registration.pushManager.getSubscription();
      if (!pushSubscription) {
        pushSubscription = await registration.pushManager.subscribe({
          userVisibleOnly: true,
          applicationServerKey: urlBase64ToUint8Array(data.publicKey),
        });
      }

      await http.post(apiPath('/push/subscribe'), {
        subscription: pushSubscription.toJSON(),
        hn: target.hn || undefined,
        qNumber: target.qnumber || undefined,
      });

      subscribed.value = true;
      return true;
    } catch {
      error.value = 'ไม่สามารถเปิดการแจ้งเตือนแบบพุชได้ กรุณาลองใหม่';
      return false;
    } finally {
      loading.value = false;
    }
  }

  return { supported, subscribed, loading, error, subscribe };
}
