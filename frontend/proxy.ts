import { NextRequest, NextResponse } from 'next/server';

const PUBLIC_PATHS = ['/login'];
const PUBLIC_API_PATHS = ['/api/auth'];

export function proxy(request: NextRequest) {
  const { pathname } = request.nextUrl;

  // Allow public pages
  if (PUBLIC_PATHS.some((p) => pathname.startsWith(p))) {
    return NextResponse.next();
  }

  // Allow public API endpoints (login, refresh, etc.)
  if (PUBLIC_API_PATHS.some((p) => pathname.startsWith(p))) {
    return NextResponse.next();
  }

  // Token espejado en cookie desde localStorage para que el proxy SSR lo lea.
  // La protección por rol real ocurre en cada componente con useAuth().
  const token = request.cookies.get('token')?.value;

  if (!token) {
    const loginUrl = new URL('/login', request.url);
    loginUrl.searchParams.set('from', pathname);
    return NextResponse.redirect(loginUrl);
  }

  return NextResponse.next();
}

export const config = {
  matcher: ['/((?!_next/static|_next/image|favicon.ico|login|.*\\.(?:jpg|jpeg|png|webp|svg|gif|ico|mp4|woff2?|ttf)).*)'],
};
