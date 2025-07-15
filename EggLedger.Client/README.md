# EggLedger.Client

This template should help get you started developing with Vue 3 in Vite.

## Authentication Routes

The application now features a unified authentication system with the following routes:

- `/eggledger/accounts/login` - Login page
- `/eggledger/accounts/signup` - Signup page

Both routes use the same `AccountsView.vue` component with toggle functionality between login and signup forms.

### Components Structure

- `src/views/AccountsView.vue` - Main authentication view with tab switching
- `src/components/auth/LoginForm.vue` - Login form component
- `src/components/auth/SignupForm.vue` - Signup form component

The components communicate via events to switch between login and signup modes, providing a seamless user experience.

## Recommended IDE Setup

[VSCode](https://code.visualstudio.com/) + [Volar](https://marketplace.visualstudio.com/items?itemName=Vue.volar) (and disable Vetur).

## Customize configuration

See [Vite Configuration Reference](https://vite.dev/config/).

## Project Setup

```sh
npm install
```

### Compile and Hot-Reload for Development

```sh
npm run dev
```

### Compile and Minify for Production

```sh
npm run build
```

### Lint with [ESLint](https://eslint.org/)

```sh
npm run lint
```
