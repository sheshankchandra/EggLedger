# CSS Migration Guide

This guide will help you migrate existing components from the old CSS structure to the new consolidated CSS architecture.

## What's Changed

### 1. New CSS Structure
- **Design Tokens**: All colors, spacing, typography, etc. are now in `src/assets/styles/variables.css`
- **Base Styles**: Global styles and utilities in `src/assets/styles/base.css`
- **Component Styles**: Reusable component styles in `src/assets/styles/components.css`
- **Main Entry**: All styles imported via `src/assets/styles/main.css`

### 2. CSS Custom Properties
Instead of hardcoded values, use CSS custom properties:

```css
/* ❌ Old way */
.button {
  background-color: #4caf50;
  padding: 1rem;
  border-radius: 6px;
}

/* ✅ New way */
.button {
  background-color: var(--color-primary);
  padding: var(--spacing-md);
  border-radius: var(--radius-md);
}
```

### 3. Utility Classes
Use utility classes instead of custom CSS:

```html
<!-- ❌ Old way -->
<div style="background: white; padding: 1.5rem; margin-bottom: 1rem; border-radius: 12px;">

<!-- ✅ New way -->
<div class="card p-4 mb-3">
```

## Migration Checklist

### Step 1: Update Component Imports
Make sure your component imports the new CSS:

```javascript
// In main.js (already done)
import './assets/styles/main.css'
```

### Step 2: Replace Hardcoded Values
Replace all hardcoded values with CSS custom properties:

| Old Value | New CSS Custom Property |
|-----------|------------------------|
| `#4caf50` | `var(--color-primary)` |
| `#f44336` | `var(--color-danger)` |
| `#ff9800` | `var(--color-warning)` |
| `1rem` | `var(--spacing-md)` |
| `1.5rem` | `var(--spacing-lg)` |
| `2rem` | `var(--spacing-xl)` |
| `6px` | `var(--radius-md)` |
| `12px` | `var(--radius-xl)` |

### Step 3: Use Component Classes
Replace custom button/form/modal styles with component classes:

#### Buttons
```html
<!-- ❌ Old -->
<button class="custom-btn">Click me</button>

<!-- ✅ New -->
<button class="btn btn-primary">Click me</button>
```

#### Forms
```html
<!-- ❌ Old -->
<div class="form-field">
  <label>Name</label>
  <input type="text" />
</div>

<!-- ✅ New -->
<div class="form-group">
  <label class="form-label">Name</label>
  <input type="text" class="form-input" />
</div>
```

#### Modals
```html
<!-- ❌ Old -->
<div class="custom-modal">
  <div class="modal-wrapper">
    <div class="modal-header">...</div>
  </div>
</div>

<!-- ✅ New -->
<div class="modal">
  <div class="modal-content">
    <div class="modal-header">...</div>
  </div>
</div>
```

### Step 4: Use Utility Classes
Replace custom spacing and layout with utility classes:

```html
<!-- ❌ Old -->
<div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem;">

<!-- ✅ New -->
<div class="flex justify-between items-center mb-3">
```

### Step 5: Component-Specific Styles
Keep component-specific styles in `<style scoped>` blocks using BEM methodology:

```vue
<style scoped>
/* ✅ Good - Component-specific styles */
.dashboard-widget {
  /* Component styles */
}

.dashboard-widget__header {
  /* Element styles */
}

.dashboard-widget--featured {
  /* Modifier styles */
}
</style>
```

## Common Migration Patterns

### 1. Button Migration
```css
/* ❌ Old button styles */
.btn {
  display: inline-block;
  padding: 0.75rem 1.5rem;
  border-radius: 6px;
  text-decoration: none;
  font-weight: 500;
  transition: all 0.2s ease;
  border: none;
  cursor: pointer;
}

.btn-primary {
  background: #4caf50;
  color: white;
}

.btn-primary:hover {
  background: #45a049;
}
```

```html
<!-- ✅ New - Use component classes -->
<button class="btn btn-primary">Click me</button>
```

### 2. Form Migration
```css
/* ❌ Old form styles */
.form-group {
  margin-bottom: 1rem;
}

.form-group label {
  display: block;
  margin-bottom: 0.5rem;
  font-weight: 500;
  color: #555;
}

.form-group input[type='text'] {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid #ccc;
  border-radius: 4px;
  box-sizing: border-box;
  font-size: 1rem;
}
```

```html
<!-- ✅ New - Use component classes -->
<div class="form-group">
  <label class="form-label">Name</label>
  <input type="text" class="form-input" />
</div>
```

### 3. Modal Migration
```css
/* ❌ Old modal styles */
.modal {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 1000;
}

.modal-content {
  background: white;
  border-radius: 12px;
  padding: 2rem;
  width: 90%;
  max-width: 600px;
}
```

```html
<!-- ✅ New - Use component classes -->
<div class="modal">
  <div class="modal-content">
    <!-- Modal content -->
  </div>
</div>
```

## Testing Your Migration

### 1. Visual Testing
- Check that all components look the same as before
- Verify hover states and transitions work correctly
- Test responsive behavior

### 2. Functionality Testing
- Ensure all interactive elements work (buttons, forms, modals)
- Test form validation and error states
- Verify accessibility features

### 3. Performance Testing
- Check that CSS is loading correctly
- Verify no console errors related to CSS
- Test on different browsers

## Troubleshooting

### Common Issues

1. **Styles not applying**: Make sure `main.css` is imported in `main.js`
2. **CSS custom properties not working**: Check browser support or add fallbacks
3. **Component styles conflicting**: Use `scoped` styles for component-specific CSS
4. **Utility classes not working**: Ensure the class names match exactly

### Fallback Strategy
For older browsers that don't support CSS custom properties:

```css
/* Add fallbacks for critical styles */
.button {
  background-color: #4caf50; /* Fallback */
  background-color: var(--color-primary);
  padding: 1rem; /* Fallback */
  padding: var(--spacing-md);
}
```

## Next Steps

1. **Migrate one component at a time** to avoid breaking changes
2. **Test thoroughly** after each migration
3. **Update documentation** for any new patterns
4. **Share knowledge** with the team about the new architecture

## Resources

- [CSS Custom Properties Guide](https://developer.mozilla.org/en-US/docs/Web/CSS/Using_CSS_custom_properties)
- [BEM Methodology](http://getbem.com/)
- [Utility-First CSS](https://tailwindcss.com/docs/utility-first)
- [Component CSS Architecture](https://www.smashingmagazine.com/2016/07/building-maintainable-css-with-css-custom-properties/) 