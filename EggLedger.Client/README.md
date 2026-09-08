# EggLedger Client

The modern frontend client for EggLedger, built as a Vue 3 Single Page Application (SPA) with Vite.


## Tech Stack

- **Framework**: [Vue 3](https://vuejs.org/) (Composition API with `<script setup>`)
- **Build Tool**: [Vite](https://vite.dev/)
- **State Management**: [Pinia](https://pinia.vuejs.org/)
- **Routing**: [Vue Router](https://router.vuejs.org/)
- **HTTP Client**: [Axios](https://axios-http.com/) (configured with CSRF headers, interceptors, and silent token refresh)
- **Styling**: Modern CSS design system with CSS custom properties and full Dark/Light theme support
- **Code Quality**: ESLint, Prettier


## Project Structure

```text
EggLedger.Client/
├── src/
│   ├── assets/          # Global styles, variables, theme tokens, and static media
│   ├── components/      # Modular UI components (auth, rooms, containers, ledger)
│   ├── composables/     # Shared Vue composables (theme, toast, responsive hooks)
│   ├── config/          # Client runtime configuration
│   ├── router/          # Client-side route definitions & navigation guards
│   ├── services/        # Axios HTTP clients and API service modules
│   ├── stores/          # Pinia state stores (auth, room, user)
│   ├── utils/           # Helper functions, formatters, and HTTP error normalizers
│   ├── views/           # Page-level components
│   ├── App.vue          # Root Vue component
│   └── main.js          # App entry point & plugin registration
├── public/              # Static assets served at root
├── index.html           # SPA entry HTML
├── vite.config.js       # Vite configuration
└── package.json         # Dependencies and scripts
```


## Key Features

- **Room Workspaces**: Create, join via code or invite link, manage approval queues, and view member roles.
- **Inventory & Containers**: Track shared items, log restocks and consumptions, and inspect item order history.
- **Settlement Ledger**: Real-time "who-owes-whom" balance calculation matrix and debt settlement flows.
- **Activity & Streaks**: Live household activity timeline and gamified consumption statistics.
- **Secure Authentication**: Silent token refresh with HttpOnly cookies, in-memory access token storage, and Google OAuth 2.0.
- **Adaptive UI**: Responsive layout optimized for desktop and mobile, with theme switching.


## Getting Started

### Prerequisites

- [Node.js](https://nodejs.org/) `^20.19.0` or `>=22.12.0` (required by Vite)
- [npm](https://www.npmjs.com/)

### Installation

```bash
# Install dependencies
npm install
```

### Environment Configuration

The client requires the API base URL to communicate with the backend. Create a `.env.local` file in `EggLedger.Client/`:

```env
VITE_API_BASE_URL=http://localhost:8080
```

> [!NOTE]
> When running the stack via `.NET Aspire` (`dotnet run --project EggLedger.AppHost`), `VITE_API_BASE_URL` is injected automatically.

### Development Server

Start the local Vite development server with hot-module replacement (HMR):

```bash
npm run dev
```

### Build for Production

Compile and bundle minified assets for production:

```bash
npm run build
```

The output artifacts are written to `EggLedger.Client/dist/`.

### Linting & Formatting

```bash
# Run ESLint check
npm run lint

# Format codebase with Prettier
npm run format
```


## Security Architecture

- **Token Storage**: JWT access tokens are stored strictly **in memory** (never written to `localStorage` or `sessionStorage`).
- **Refresh Token**: Stored in a secure `HttpOnly; SameSite=None` (production) cookie managed exclusively by the browser and API.
- **CSRF Protection**: All mutating and cookie-authenticated API requests automatically attach the custom `X-EggLedger-CSRF` header via Axios interceptors.
