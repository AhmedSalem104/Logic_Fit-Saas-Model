import type { Config } from 'tailwindcss';

export default {
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        ink: 'var(--color-ink)',
        muted: 'var(--color-muted)',
        surface: 'var(--color-surface)',
        panel: 'var(--color-panel)',
        line: 'var(--color-line)',
        accent: 'var(--color-accent)',
      },
    },
  },
  plugins: [],
} satisfies Config;
