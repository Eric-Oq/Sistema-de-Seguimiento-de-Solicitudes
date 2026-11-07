/** @type {import('tailwindcss').Config} */
const tailwindcssMotion = require('tailwindcss-motion');

module.exports = {
    content: [
        "./App.razor",
        "./Layout/**/*.razor",
        "./Pages/**/*.razor",
        "./Services/**/*.razor",
        "./wwwroot/index.html"
    ],
    theme: {
        extend: {
            keyframes: {
                'balance-tilt': {
                    '0%, 100%': { transform: 'rotate(-5deg)' },
                    '50%': { transform: 'rotate(5deg)' }
                },
                'balance-pan': {
                    '0%, 100%': { transform: 'rotate(5deg)' },
                    '50%': { transform: 'rotate(-5deg)' }
                },
                'login-error-shake': {
                    '0%, 100%': { transform: 'translateX(0)' },
                    '20%, 60%': { transform: 'translateX(-5px)' },
                    '40%, 80%': { transform: 'translateX(5px)' }
                },
                'login-alert-bounce': {
                    '0%': { transform: 'translateY(-6px)' },
                    '55%': { transform: 'translateY(3px)' },
                    '80%': { transform: 'translateY(-2px)' },
                    '100%': { transform: 'translateY(0)' }
                }
            },
            animation: {
                'balance-tilt': 'balance-tilt 5s ease-in-out infinite',
                'balance-pan': 'balance-pan 5s ease-in-out infinite',
                'login-error-shake': 'login-error-shake 0.6s ease-in-out',
                'login-alert-bounce': 'login-alert-bounce 0.5s ease-out'
            },
            colors: {
                brand: {
                    50: '#fbf3f5',
                    100: '#f3e3e8',
                    200: '#e6c6d1',
                    300: '#d3a0b2',
                    400: '#bd708b',
                    500: '#9f4768',
                    600: '#84324f',
                    700: '#6e203c',
                    800: '#57172f',
                    900: '#421224',
                    950: '#2d0b19'
                },
                pro: {
                    bg: '#f8f6f4',
                    card: '#ffffff',
                    primary: '#6e203c',
                    dark: '#332a2d'
                }
            }
        },
    },
    plugins: [tailwindcssMotion],
}