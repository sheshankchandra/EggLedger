<template>
  <div class="stats-chart">
    <canvas ref="canvasRef" role="img" :aria-label="ariaLabel"></canvas>
  </div>
</template>

<script setup>
import { ref, onMounted, onBeforeUnmount, watch } from 'vue'
import Chart from 'chart.js/auto'
import { useThemeStore } from '@/stores/theme.store'

const props = defineProps({
  buckets: { type: Array, required: true }, // [{ label, eggsConsumed }]
  ariaLabel: { type: String, default: 'Consumption chart' },
})

const themeStore = useThemeStore()
const canvasRef = ref(null)
let chart = null

// Canvas fillStyle can't resolve CSS var() itself, so read the actual computed token value.
const readToken = (name) => getComputedStyle(document.documentElement).getPropertyValue(name).trim()

const buildConfig = () => ({
  type: 'bar',
  data: {
    labels: props.buckets.map((b) => b.label),
    datasets: [
      {
        label: 'Eggs consumed',
        data: props.buckets.map((b) => b.eggsConsumed),
        backgroundColor: readToken('--color-primary'),
        borderRadius: 6,
        maxBarThickness: 36,
      },
    ],
  },
  options: {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: false },
      tooltip: {
        callbacks: {
          label: (context) => ` ${context.parsed.y} eggs`,
        },
      },
    },
    scales: {
      x: {
        ticks: { color: readToken('--text-muted'), autoSkip: true, maxRotation: 0 },
        grid: { display: false },
      },
      y: {
        beginAtZero: true,
        ticks: { color: readToken('--text-muted'), precision: 0 },
        grid: { color: readToken('--border-light') },
      },
    },
  },
})

const render = () => {
  if (!canvasRef.value) return
  chart?.destroy()
  chart = new Chart(canvasRef.value, buildConfig())
}

// Theme toggles change CSS custom properties on <html> - wait a frame for the browser to
// recompute styles before re-reading them, otherwise the chart re-renders with stale colors.
const renderNextFrame = () => requestAnimationFrame(render)

onMounted(render)
onBeforeUnmount(() => chart?.destroy())

watch(() => props.buckets, render, { deep: true })
watch(() => themeStore.isDark, renderNextFrame)
</script>

<style scoped>
.stats-chart {
  position: relative;
  height: 240px;
}
</style>
