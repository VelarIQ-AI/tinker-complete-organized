# 🚀 AI Quick Reference - Tinker Genie

## 📋 **Common Tasks & Patterns**

### **🎨 Adding a New UI Component**

1. **Check existing patterns** in `frontend/pwa/styles.css`
2. **Follow BEM naming convention**
3. **Use CSS custom properties** for colors/spacing
4. **Implement responsive design**
5. **Add accessibility attributes**

```css
/* Example: New notification component */
.notification {
    padding: var(--spacing-md);
    border-radius: var(--radius-md);
    margin-bottom: var(--spacing-md);
    display: flex;
    align-items: center;
    gap: var(--spacing-sm);
}

.notification--success {
    background: var(--success);
    color: white;
}
```

### **⚡ Adding JavaScript Functionality**

```javascript
// Example: New feature function
async function handleNewFeature(data) {
    console.log('handleNewFeature called with:', data);
    
    try {
        const response = await fetch(`${API_BASE_URL}/new-endpoint`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        });
        
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}`);
        }
        
        const result = await response.json();
        
        if (result.success) {
            showSuccessMessage('Feature completed successfully!');
            return result.data;
        } else {
            throw new Error(result.message || 'Feature failed');
        }
    } catch (error) {
        console.error('New feature error:', error);
        showErrorMessage('Feature failed. Please try again.');
        return null;
    }
}
```

---

## 🎯 **File Locations Quick Reference**

| Component | File Path | Purpose |
|-----------|-----------|---------|
| **Main HTML** | `frontend/pwa/index.html` | App structure |
| **Core JS** | `frontend/pwa/app.js` | Main functionality |
| **Styles** | `frontend/pwa/styles.css` | All styling |
| **Auth API** | `backend/TinkerGenie.API/Controllers/AuthController.cs` | Authentication |
| **Chat API** | `backend/TinkerGenie.API/Controllers/ChatController.cs` | Chat functionality |

---

## 🎨 **CSS Classes Quick Reference**

### **Layout**
```css
.container          /* Main container with max-width */
.grid              /* CSS Grid layout */
.flex              /* Flexbox layout */
```

### **Components**
```css
.btn               /* Base button */
.btn-primary       /* Primary button */
.input-field       /* Form input */
.card              /* Content card */
```

---

## 🔧 **JavaScript Functions Quick Reference**

### **Core Functions**
```javascript
init()                    // Initialize app
handleLogin()            // Process login
sendMessage()            // Send chat message
showPreferences()        // Show preferences screen
addMessage()             // Add message to chat
```

---

## 🚨 **Common Issues & Solutions**

### **Login Button Not Working**
1. Check if `handleLogin()` function exists
2. Verify form event listener is attached
3. Ensure button is not disabled
4. Check for JavaScript errors in console

### **API Calls Failing**
1. Verify `API_BASE_URL` is set correctly
2. Check network tab for actual request
3. Verify backend endpoint exists

---

## 📱 **Mobile-First Responsive Pattern**

```css
/* Mobile first (default) */
.component {
    padding: 16px;
    font-size: 16px;
}

/* Tablet and up */
@media (min-width: 768px) {
    .component {
        padding: 24px;
        font-size: 18px;
    }
}
```

---

## 🔒 **Security Checklist**

- [ ] **No hardcoded secrets** in code
- [ ] **Use environment variables** for configuration
- [ ] **Validate all inputs** on frontend and backend
- [ ] **Implement proper error handling**

---

**🎯 Use this quick reference to maintain consistency and quality across the Tinker Genie platform!**
