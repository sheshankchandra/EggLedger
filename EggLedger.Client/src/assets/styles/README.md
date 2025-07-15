# CSS Architecture Documentation

This directory contains the consolidated CSS architecture for the EggLedger Vue.js application.

## File Structure

```
assets/styles/
├── main.css          # Main entry point that imports all CSS files
├── variables.css     # CSS custom properties (design tokens)
├── base.css          # Base styles, reset, and utility classes
├── components.css    # Reusable component styles
└── README.md         # This documentation file
```

## CSS Architecture Principles

### 1. Design Tokens (variables.css)
- **Purpose**: Centralized design system variables
- **Contains**: Colors, spacing, typography, shadows, transitions, etc.
- **Usage**: Use CSS custom properties for consistent theming

### 2. Base Styles (base.css)
- **Purpose**: CSS reset, global styles, and utility classes
- **Contains**: Typography, layout utilities, spacing utilities, etc.
- **Usage**: Provides foundation for all components

### 3. Component Styles (components.css)
- **Purpose**: Reusable component styles
- **Contains**: Buttons, forms, modals, cards, alerts, etc.
- **Usage**: Consistent component styling across the application

## Best Practices

### 1. Use CSS Custom Properties
```css
/* ✅ Good */
.button {
  background-color: var(--color-primary);
  padding: var(--spacing-md);
}

/* ❌ Avoid */
.button {
  background-color: #4caf50;
  padding: 1rem;
}
```

### 2. Use Utility Classes
```html
<!-- ✅ Good -->
<div class="card p-4 mb-3">
  <h3 class="text-xl font-semibold mb-2">Title</h3>
  <p class="text-secondary">Content</p>
</div>

<!-- ❌ Avoid -->
<div style="background: white; padding: 1.5rem; margin-bottom: 1rem; border-radius: 12px;">
  <h3 style="font-size: 1.25rem; font-weight: 600; margin-bottom: 0.5rem;">Title</h3>
  <p style="color: #666;">Content</p>
</div>
```

### 3. Component-Specific Styles
For component-specific styles that don't fit the global patterns:

```vue
<template>
  <div class="dashboard-widget">
    <!-- Component content -->
  </div>
</template>

<style scoped>
/* Use BEM methodology for component-specific styles */
.dashboard-widget {
  /* Component-specific styles */
}

.dashboard-widget__header {
  /* Element styles */
}

.dashboard-widget--featured {
  /* Modifier styles */
}
</style>
```

### 4. Responsive Design
```css
/* Use CSS custom properties for breakpoints */
@media (max-width: 768px) {
  .container {
    padding: 0 var(--spacing-md);
  }
}
```

## Available Components

### Buttons
- `.btn` - Base button class
- `.btn-primary`, `.btn-secondary`, `.btn-success`, `.btn-danger`, `.btn-warning`, `.btn-info`
- `.btn-sm`, `.btn-lg` - Size variants
- `.btn-outline-primary`, `.btn-outline-secondary` - Outline variants

### Forms
- `.form-group` - Form field container
- `.form-label` - Form labels
- `.form-input`, `.form-select`, `.form-textarea` - Form inputs
- `.form-feedback` - Validation messages

### Cards
- `.card` - Base card class
- `.card-header`, `.card-body`, `.card-footer` - Card sections
- `.card-title`, `.card-subtitle` - Card typography

### Modals
- `.modal` - Modal backdrop
- `.modal-content` - Modal container
- `.modal-header`, `.modal-body`, `.modal-footer` - Modal sections
- `.close-btn` - Close button

### Alerts
- `.alert` - Base alert class
- `.alert-success`, `.alert-error`, `.alert-warning`, `.alert-info` - Alert variants

### Badges
- `.badge` - Base badge class
- `.badge-primary`, `.badge-secondary`, `.badge-success`, `.badge-danger`, `.badge-warning`, `.badge-info`

## Utility Classes

### Spacing
- `.m-0` to `.m-5` - Margins
- `.mt-0` to `.mt-5` - Margin top
- `.mb-0` to `.mb-5` - Margin bottom
- `.p-0` to `.p-5` - Padding
- `.pt-0` to `.pt-5` - Padding top
- `.pb-0` to `.pb-5` - Padding bottom

### Typography
- `.text-xs`, `.text-sm`, `.text-base`, `.text-lg`, `.text-xl`, `.text-2xl` - Font sizes
- `.font-normal`, `.font-medium`, `.font-semibold`, `.font-bold` - Font weights
- `.text-primary`, `.text-secondary`, `.text-muted`, `.text-inverse` - Text colors
- `.text-center`, `.text-left`, `.text-right` - Text alignment

### Layout
- `.flex`, `.grid`, `.block`, `.inline`, `.hidden` - Display
- `.flex-col`, `.flex-row` - Flex direction
- `.items-center`, `.items-start`, `.items-end` - Align items
- `.justify-center`, `.justify-between`, `.justify-start`, `.justify-end` - Justify content
- `.gap-1` to `.gap-5` - Gap spacing

### Colors
- `.bg-primary`, `.bg-secondary`, `.bg-tertiary` - Background colors
- `.text-primary`, `.text-secondary`, `.text-muted`, `.text-inverse` - Text colors

### Borders & Shadows
- `.border`, `.border-0` - Border
- `.rounded`, `.rounded-sm`, `.rounded-lg`, `.rounded-xl` - Border radius
- `.shadow-sm`, `.shadow-md`, `.shadow-lg`, `.shadow-xl` - Box shadows

## Migration Guide

When updating existing components:

1. **Remove inline styles** and replace with utility classes
2. **Replace hardcoded values** with CSS custom properties
3. **Use component classes** instead of custom CSS where possible
4. **Keep component-specific styles** in `<style scoped>` blocks
5. **Use BEM methodology** for component-specific styles

## Adding New Styles

### For Global Components
Add new component styles to `components.css`

### For Design Tokens
Add new variables to `variables.css`

### For Utilities
Add new utility classes to `base.css`

### For Component-Specific Styles
Keep them in the component's `<style scoped>` block using BEM methodology

## Browser Support

This CSS architecture uses modern CSS features:
- CSS Custom Properties (CSS Variables)
- CSS Grid
- Flexbox
- Modern selectors

Ensure your target browsers support these features or provide appropriate fallbacks. 