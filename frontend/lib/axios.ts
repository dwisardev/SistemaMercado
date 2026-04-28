import axios from 'axios';
import { storeUser, clearUser, getStoredRefreshToken } from './auth';

const api = axios.create({
  baseURL: "/",
  headers: { 'Content-Type': 'application/json' },
});

api.interceptors.request.use((config) => {
  if (typeof window !== 'undefined') {
    const token = localStorage.getItem('token');
    if (token) config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

let isRefreshing = false;
let waitQueue: Array<(token: string) => void> = [];

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const original = error.config;

    if (error.response?.status === 401 && !original._retry && typeof window !== 'undefined') {
      const refreshToken = getStoredRefreshToken();

      if (refreshToken) {
        if (isRefreshing) {
          return new Promise((resolve) => {
            waitQueue.push((token) => {
              original.headers.Authorization = `Bearer ${token}`;
              resolve(api(original));
            });
          });
        }

        original._retry = true;
        isRefreshing = true;

        try {
          const res = await axios.post<{
            token: string; refreshToken: string; usuarioId: string;
            nombreCompleto: string; email: string; rol: string;
            expiresAt: string; refreshTokenExpiresAt: string;
          }>('/api/auth/refresh', { refreshToken });

          const newUser = res.data as Parameters<typeof storeUser>[0];
          storeUser(newUser);
          api.defaults.headers.common.Authorization = `Bearer ${newUser.token}`;
          waitQueue.forEach((cb) => cb(newUser.token));
          waitQueue = [];
          original.headers.Authorization = `Bearer ${newUser.token}`;
          return api(original);
        } catch {
          clearUser();
          window.location.href = '/login';
        } finally {
          isRefreshing = false;
        }
      } else {
        clearUser();
        window.location.href = '/login';
      }
    }

    return Promise.reject(error);
  }
);

export default api;
