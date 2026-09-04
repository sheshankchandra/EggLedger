<template>
  <div class="not-found">
    <h1>404</h1>
    <p>This is not the web page you are looking for.</p>
    <p>
      You can go back to the
      <router-link to="/">homepage</router-link>.
    </p>
  </div>
</template>

<script>
export default {
  name: 'NotFound',
}
</script>

<style scoped>
.not-found {
  text-align: center;
  margin-top: 60px;
  font-family: var(--font-family-sans);
  color: var(--text-primary);
}
.not-found h1 {
  font-size: 3rem;
  color: var(--color-danger);
}
.not-found p {
  font-size: 1.2rem;
}
.not-found a {
  color: var(--color-primary);
  text-decoration: none;
}
.not-found a:hover {
  text-decoration: underline;
}
</style>
