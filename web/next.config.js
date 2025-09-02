/********* Generated config (JS) *********/
/** @type {import('next').NextConfig} */
const nextConfig = {
	async rewrites() {
		if (process.env.NODE_ENV === 'development' && process.env.NEXT_PUBLIC_API_BASE) {
			return [
				{ source: '/api/:path*', destination: `${process.env.NEXT_PUBLIC_API_BASE}/api/:path*` },
			];
		}
		return [];
	},
};

module.exports = nextConfig;
