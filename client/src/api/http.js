import axios from 'axios';

export const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5224';

export const http = axios.create({ baseURL: apiBaseUrl, timeout: 10000 });
