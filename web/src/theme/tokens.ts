export const spacing = {
	xs: '0.25rem',
	sm: '0.5rem',
	md: '1rem',
	lg: '1.5rem',
	xl: '2rem',
	xxl: '3rem',
};

export const colors = {
	background: '#0b1221',
	foreground: '#ffffff',
	primary: '#10b981',
	primaryForeground: '#052e1b',
	muted: '#6b7280',
	border: '#e5e7eb',
	card: '#111827',
	cardForeground: '#e5e7eb',
};

export const radii = {
	sm: '4px',
	md: '8px',
	lg: '12px',
	full: '9999px',
};

export type ThemeTokens = {
	spacing: typeof spacing;
	colors: typeof colors;
	radii: typeof radii;
};

export const tokens: ThemeTokens = { spacing, colors, radii };
