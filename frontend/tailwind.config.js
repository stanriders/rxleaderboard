import {nextui} from '@nextui-org/theme'

/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    './components/**/*.{js,ts,jsx,tsx,mdx}',
    './app/**/*.{js,ts,jsx,tsx,mdx}',
    './node_modules/@nextui-org/theme/dist/**/*.{js,ts,jsx,tsx}'
  ],
  theme: {
    extend: {
      fontFamily: {
        sans: ["var(--font-sans)"],
        mono: ["var(--font-mono)"],
      },
    },
  },
  darkMode: "class",
  plugins: [
  nextui({
    themes: {
      dark: {
        colors: {
          content1: "#202023",
          primary: {
            DEFAULT: "#DBB33F",
            100: "#FDF7D9",
            200: "#FBEEB3",
            300: "#F4DE8B",
            400: "#E9CC6C",
            500: "#DBB33F",
            600: "#BC942E",
            700: "#9D761F",
            800: "#7F5B14",
            900: "#69470C",
            /*
            DEFAULT: "#FFD857",
            100: "#FFFADD",
            200: "#FFF4BB",
            300: "#FFEC9A",
            400: "#FFE481",
            500: "#FFD857",
            600: "#DBB33F",
            700: "#B7902B",
            800: "#936F1B",
            900: "#7A5710",*/
          },
        },
      },
      light: {
        colors: {
          primary: {
            DEFAULT: "#DBB33F",
            100: "#69470C",
            200: "#7F5B14",
            300: "#9D761F",
            400: "#BC942E",
            500: "#DBB33F",
            600: "#E9CC6C",
            700: "#F4DE8B",
            800: "#FBEEB3",
            900: "#FDF7D9",
            /*
            DEFAULT: "#FFD857",
            100: "#FFFADD",
            200: "#FFF4BB",
            300: "#FFEC9A",
            400: "#FFE481",
            500: "#FFD857",
            600: "#DBB33F",
            700: "#B7902B",
            800: "#936F1B",
            900: "#7A5710",*/
          },
        },
      },
    },
  }),
],
};
