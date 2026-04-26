import type { NextConfig } from "next";

// En local: http://localhost:5000
// En Vercel: variable de entorno API_BACKEND_URL con la URL de Railway
const apiBackendUrl = process.env.API_BACKEND_URL ?? "http://localhost:5000";

const nextConfig: NextConfig = {
  turbopack: {},
  async rewrites() {
    return [
      {
        source: "/api/:path*",
        destination: `${apiBackendUrl}/api/:path*`,
      },
    ];
  },
};

export default nextConfig;
