// Use CDN ONLY - simpler and works immediately
const CDN = 'https://esm.run/@material/web';

try {
  // Import common bundle (includes buttons + text fields)
  await import(`${CDN}/common.js`);
  
  // Typography styles
  const {styles: typescaleStyles} = await import(`${CDN}/typography/md-typescale-styles.js`);
  if (document.adoptedStyleSheets) {
    document.adoptedStyleSheets.push(typescaleStyles.styleSheet);
  }

  console.log('✓ Material Web loaded successfully');
  
  // Verify registration
  setTimeout(() => {
    const check = ['md-filled-button','md-outlined-button','md-text-button','md-outlined-text-field','md-icon-button'];
    check.forEach(tag => {
      const ok = customElements.get(tag);
      console.log(`${ok ? '✓' : '✗'} ${tag}`);
    });
  }, 500);
  
} catch (err) {
  console.error('✗ Material Web failed:', err);
}