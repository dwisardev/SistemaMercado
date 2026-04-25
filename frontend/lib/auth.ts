import { AuthUser } from './types';

function setCookie(name: string, value: string, days: number) {
  const expires = new Date(Date.now() + days * 86400 * 1000).toUTCString();
  document.cookie = `${name}=${value}; expires=${expires}; path=/; SameSite=Lax`;
}

function deleteCookie(name: string) {
  document.cookie = `${name}=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/`;
}

export function getStoredUser(): AuthUser | null {
  if (typeof window === 'undefined') return null;
  try {
    const raw = localStorage.getItem('user');
    return raw ? JSON.parse(raw) : null;
  } catch {
    return null;
  }
}

export function storeUser(user: AuthUser) {
  localStorage.setItem('token', user.token);
  localStorage.setItem('user', JSON.stringify(user));
  // espejo en cookie para que el middleware SSR lo lea
  setCookie('token', user.token, 1);
}

export function clearUser() {
  localStorage.removeItem('token');
  localStorage.removeItem('user');
  deleteCookie('token');
}

export function isTokenExpired(user: AuthUser): boolean {
  return new Date(user.expiresAt) < new Date();
}
