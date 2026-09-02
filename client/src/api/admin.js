import { http } from './http';
import { apiPath } from '../composables/useBranch';

export function adminLogin(password) {
  return http.post(apiPath('/admin/login'), { password }).then((res) => res.data);
}

export function updateAdminSettings(token, patch) {
  return http
    .put(apiPath('/admin/settings'), patch, { headers: { Authorization: `Bearer ${token}` } })
    .then((res) => res.data);
}

export function uploadAdminLogo(token, file) {
  const form = new FormData();
  form.append('file', file);
  return http
    .post(apiPath('/admin/logo'), form, { headers: { Authorization: `Bearer ${token}` } })
    .then((res) => res.data);
}

export function uploadAdminSound(token, file) {
  const form = new FormData();
  form.append('file', file);
  return http
    .post(apiPath('/admin/sound'), form, { headers: { Authorization: `Bearer ${token}` } })
    .then((res) => res.data);
}
