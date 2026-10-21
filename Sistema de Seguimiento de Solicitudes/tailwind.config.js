/** @type {import('tailwindcss').Config} */
module.exports = {
    content: [
        "./**/*.razor",
        "./**/*.html",
        "./**/*.cshtml"
    ],
    theme: {
        extend: {
            colors: {
                pro: {
                    bg: '#f1f5f9',
                    card: '#ffffff',
                    primary: '#4f46e5',
                    dark: '#0f172a'
                }
            }
        },
    },
    plugins: [],
}