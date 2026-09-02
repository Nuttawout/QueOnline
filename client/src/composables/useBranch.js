// No vue-router (per App.vue's own comment) — the branch slug is just the first path segment,
// e.g. /vibharam-dev or /vibharam-dev/admin. This is the ONE place that URL convention lives;
// every API call and every admin token storage key must route through here so a branch never
// leaks into another branch's requests or session storage.
//
// "super-admin" is reserved as the one path segment that is never a branch slug — it's the
// top-level branch-management page (SuperAdminView), not scoped to any single branch. See
// SuperAdminController's ReservedSlug for the matching server-side guard.
const segments = window.location.pathname.split('/').filter(Boolean);

export const isSuperAdminPath = segments[0] === 'super-admin';
export const branchSlug = isSuperAdminPath ? null : (segments[0] ?? null);
export const isAdminPath = !isSuperAdminPath && segments[1] === 'admin';

/** Builds a branch-scoped API path, e.g. apiPath('/queue/lookup') -> '/api/{branch}/queue/lookup'. */
export function apiPath(path) {
  if (!branchSlug) {
    throw new Error('apiPath() called with no branch in the URL — should never happen once App.vue routes to BranchPickerView instead.');
  }
  return `/api/${branchSlug}${path}`;
}

/** Branch-scoped sessionStorage key — keeps one browser tab's admin token per branch, so a
 * leftover token from a previously visited branch is never mistaken for this branch's session. */
export function branchStorageKey(name) {
  return `${name}_${branchSlug}`;
}
