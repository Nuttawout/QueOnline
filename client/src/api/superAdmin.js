import { http } from './http';

// Not branch-scoped (no apiPath() here) — this manages the branch list itself, gated by its own
// master password, separate from any branch's admin login. See SuperAdminController.

export function superAdminLogin(password) {
  return http.post('/api/super-admin/login', { password }).then((res) => res.data);
}

export function fetchBranches(token) {
  return http.get('/api/super-admin/branches', { headers: { Authorization: `Bearer ${token}` } }).then((res) => res.data);
}

export function upsertBranch(token, slug, patch) {
  return http
    .put(`/api/super-admin/branches/${slug}`, patch, { headers: { Authorization: `Bearer ${token}` } })
    .then((res) => res.data);
}

export function deleteBranch(token, slug) {
  return http
    .delete(`/api/super-admin/branches/${slug}`, { headers: { Authorization: `Bearer ${token}` } })
    .then((res) => res.data);
}
