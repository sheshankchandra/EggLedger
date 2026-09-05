<template>
  <div class="landing-view">
    <header class="landing-header">
      <div class="landing-header-inner">
        <router-link to="/" class="app-branding" aria-label="EggLedger home">
          <img src="/eggledger.png" alt="EggLedger Logo" class="app-logo" />
          <span class="app-title">EggLedger</span>
        </router-link>
        <div class="landing-header-actions">
          <button
            @click="themeStore.toggleTheme()"
            class="theme-toggle"
            type="button"
            :aria-label="themeStore.isDark ? 'Switch to light theme' : 'Switch to dark theme'"
          >
            <Sun v-if="themeStore.isDark" :size="18" aria-hidden="true" />
            <Moon v-else :size="18" aria-hidden="true" />
          </button>
          <router-link to="/accounts/login" class="btn btn-outline btn-outline-secondary btn-sm">
            Sign in
          </router-link>
        </div>
      </div>
    </header>

    <main>
      <section class="hero">
        <div class="hero-inner">
          <p class="eyebrow">For roommates & shared households</p>
          <h1>Track shared stock. Settle up instantly. Build the habit.</h1>
          <p class="hero-subtitle">
            EggLedger is the shared source of truth for your household's stock: log purchases and
            usage, split the cost automatically, and stay motivated with streaks and stats.
          </p>
          <div class="hero-actions">
            <router-link to="/accounts/signup" class="btn btn-primary btn-lg">
              Get started for free
            </router-link>
            <router-link to="/accounts/login" class="btn btn-outline btn-outline-secondary btn-lg">
              I already have an account
            </router-link>
          </div>
          <p class="hero-hint">No credit card. Takes about 30 seconds to create a room.</p>
        </div>
      </section>

      <section class="features page-shell" aria-labelledby="features-heading">
        <p class="eyebrow section-eyebrow">Why EggLedger</p>
        <h2 id="features-heading">Everything a shared household needs, in one place</h2>
        <div class="feature-grid">
          <article class="feature-card">
            <span class="feature-icon" aria-hidden="true">{{ resource.icon }}</span>
            <h3>Shared inventory, always up to date</h3>
            <p>
              See exactly what's in stock across your room in real time, down to how many
              {{ resource.plural }} are left, with fair, first-in-first-out consumption tracking.
            </p>
          </article>
          <article class="feature-card">
            <span class="feature-icon" aria-hidden="true"><HandCoins :size="28" /></span>
            <h3>Settle up instantly</h3>
            <p>
              A built-in ledger tracks who bought what and who owes whom, then simplifies it into
              the fewest payments needed, and lets you mark payments as received with one tap.
            </p>
          </article>
          <article class="feature-card">
            <span class="feature-icon" aria-hidden="true"><Flame :size="28" /></span>
            <h3>Streaks that keep you consistent</h3>
            <p>
              Turn healthy habits into a game. Track your streak, protein, and calories over 1-week,
              1-month, 1-year, or all-time views.
            </p>
          </article>
          <article class="feature-card">
            <span class="feature-icon" aria-hidden="true"><Lock :size="28" /></span>
            <h3>Rooms you control</h3>
            <p>
              Create public or private rooms, share an invite link with roommates, and approve who
              joins a private room before they get access.
            </p>
          </article>
        </div>
      </section>

      <section class="how-it-works page-shell" aria-labelledby="how-it-works-heading">
        <p class="eyebrow section-eyebrow">How it works</p>
        <h2 id="how-it-works-heading">Up and running in three steps</h2>
        <div class="steps-grid">
          <div class="step-card">
            <span class="step-number" aria-hidden="true">1</span>
            <h3>Create or join a room</h3>
            <p>Start a room for your household, or join one with a code or shared invite link.</p>
          </div>
          <div class="step-card">
            <span class="step-number" aria-hidden="true">2</span>
            <h3>Log purchases & usage</h3>
            <p>Everyone in the room sees the shared stock and balances update live.</p>
          </div>
          <div class="step-card">
            <span class="step-number" aria-hidden="true">3</span>
            <h3>Settle up & keep your streak</h3>
            <p>See who owes what, mark payments received, and watch your stats grow.</p>
          </div>
        </div>
      </section>

      <section class="final-cta">
        <div class="final-cta-inner">
          <h2>Ready to stop splitting the bill by memory?</h2>
          <router-link to="/accounts/signup" class="btn btn-primary btn-lg">
            Create your first room
          </router-link>
        </div>
      </section>
    </main>

    <footer class="landing-footer">
      <p>© {{ currentYear }} EggLedger</p>
    </footer>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { HandCoins, Flame, Lock, Sun, Moon } from '@lucide/vue'
import { useThemeStore } from '@/stores/theme.store'
import { resourceConfig as resource } from '@/config/resource.config'

const themeStore = useThemeStore()
const currentYear = computed(() => new Date().getFullYear())
</script>

<style scoped>
.landing-view {
  min-height: 100vh;
  background: var(--bg-secondary);
}

.landing-header {
  position: sticky;
  top: 0;
  z-index: var(--z-sticky);
  background: color-mix(in srgb, var(--bg-primary) 94%, transparent);
  border-bottom: 1px solid var(--border-light);
  backdrop-filter: blur(14px);
}

.landing-header-inner {
  max-width: var(--container-max-width);
  margin: 0 auto;
  min-height: var(--header-height);
  padding: var(--spacing-sm) var(--spacing-xl);
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.app-branding {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  color: var(--text-primary);
  text-decoration: none;
}

.app-logo {
  width: 38px;
  height: 38px;
  object-fit: contain;
}

.app-title {
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-bold);
  letter-spacing: -0.02em;
}

.landing-header-actions {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
}

.theme-toggle {
  display: inline-flex;
  min-width: 42px;
  min-height: 42px;
  align-items: center;
  justify-content: center;
  border: 1px solid transparent;
  border-radius: var(--radius-md);
  background: transparent;
  color: var(--text-secondary);
  font-size: var(--font-size-lg);
  cursor: pointer;
  transition: background-color var(--transition-fast);
}

.theme-toggle:hover {
  background: var(--bg-tertiary);
  color: var(--text-primary);
}

.hero {
  padding: var(--spacing-3xl) var(--spacing-xl);
  background: linear-gradient(135deg, var(--color-primary) 0%, var(--color-secondary) 100%);
  text-align: center;
}

.hero-inner {
  max-width: 720px;
  margin: 0 auto;
}

.hero .eyebrow {
  color: rgba(255, 255, 255, 0.85);
}

.hero h1 {
  margin: var(--spacing-sm) 0 var(--spacing-md);
  color: var(--text-inverse);
  font-size: clamp(2rem, 5vw, 3rem);
  letter-spacing: -0.03em;
  text-shadow: 0 2px 4px rgba(0, 0, 0, 0.2);
}

.hero-subtitle {
  margin: 0 auto var(--spacing-xl);
  max-width: 560px;
  color: rgba(255, 255, 255, 0.92);
  font-size: var(--font-size-lg);
}

.hero-actions {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  gap: var(--spacing-md);
}

.hero .btn-primary {
  background: var(--bg-primary);
  color: var(--color-primary);
}

.hero .btn-outline-secondary {
  color: var(--text-inverse);
  border-color: rgba(255, 255, 255, 0.6);
}

.hero .btn-outline-secondary:hover:not(:disabled) {
  background: rgba(255, 255, 255, 0.12);
  color: var(--text-inverse);
}

.hero-hint {
  margin: var(--spacing-lg) 0 0;
  color: rgba(255, 255, 255, 0.75);
  font-size: var(--font-size-sm);
}

.features,
.how-it-works {
  padding-block: var(--spacing-3xl);
  text-align: center;
}

.section-eyebrow {
  justify-content: center;
}

.features h2,
.how-it-works h2 {
  max-width: 640px;
  margin: var(--spacing-sm) auto var(--spacing-2xl);
  font-size: clamp(1.5rem, 4vw, 2.25rem);
}

.feature-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(min(100%, 250px), 1fr));
  gap: var(--spacing-lg);
  text-align: left;
}

.feature-card {
  padding: var(--spacing-xl);
  border: 1px solid var(--border-light);
  border-radius: var(--radius-xl);
  background: var(--bg-primary);
  box-shadow: var(--shadow-sm);
}

.feature-icon {
  display: grid;
  width: 48px;
  height: 48px;
  place-items: center;
  border-radius: var(--radius-lg);
  background: var(--color-primary-light);
  color: var(--color-primary);
}

.feature-card h3 {
  margin: var(--spacing-lg) 0 var(--spacing-sm);
  font-size: var(--font-size-lg);
}

.feature-card p {
  margin: 0;
  color: var(--text-secondary);
}

.steps-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(min(100%, 220px), 1fr));
  gap: var(--spacing-lg);
  text-align: left;
}

.step-card {
  padding: var(--spacing-xl);
  border: 1px solid var(--border-light);
  border-radius: var(--radius-xl);
  background: var(--bg-primary);
  box-shadow: var(--shadow-sm);
}

.step-number {
  display: grid;
  width: 40px;
  height: 40px;
  place-items: center;
  border-radius: 999px;
  background: var(--color-primary);
  color: var(--text-inverse);
  font-weight: var(--font-weight-bold);
}

.step-card h3 {
  margin: var(--spacing-lg) 0 var(--spacing-sm);
  font-size: var(--font-size-lg);
}

.step-card p {
  margin: 0;
  color: var(--text-secondary);
}

.final-cta {
  padding: var(--spacing-3xl) var(--spacing-xl);
  background: var(--bg-tertiary);
  text-align: center;
}

.final-cta-inner {
  display: grid;
  gap: var(--spacing-lg);
  justify-items: center;
}

.final-cta h2 {
  max-width: 520px;
  margin: 0;
  font-size: clamp(1.5rem, 4vw, 2rem);
}

.landing-footer {
  padding: var(--spacing-xl);
  color: var(--text-muted);
  font-size: var(--font-size-sm);
  text-align: center;
}

@media (max-width: 640px) {
  .app-title {
    display: none;
  }

  .hero {
    padding: var(--spacing-2xl) var(--spacing-md);
  }

  .hero-actions {
    flex-direction: column;
    align-items: stretch;
  }
}
</style>
