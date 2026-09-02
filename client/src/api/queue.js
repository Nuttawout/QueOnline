import { http } from './http';
import { apiPath } from '../composables/useBranch';

export function lookupQueue({ hn, qnumber }, signal) {
  return http.get(apiPath('/queue/lookup'), { params: { hn, qnumber }, signal }).then((res) => res.data);
}
