# App Limiter Browser Extension

This browser extension works with the App Limiter service to track and limit website usage.

## Installation

1. Open Chrome/Edge and navigate to `chrome://extensions` or `edge://extensions`
2. Enable "Developer mode" in the top right
3. Click "Load unpacked" and select this directory

## Development

To modify the extension:
1. Make your changes to the source files
2. Click the refresh icon on the extension card in chrome://extensions
3. Or reload the extension using the Extensions menu

## Files

- `manifest.json` - Extension configuration and permissions
- `background.js` - Background service worker that tracks tabs
- `images/` - Extension icons

## Testing

1. Make sure the App Limiter service is running on port 5095
2. Load the extension
3. Open the browser's developer tools and check the "Console" for connection messages
4. Visit different websites to test URL tracking

## Building for Distribution

For Chrome Web Store distribution:
1. Zip the contents of this directory
2. Upload to the Chrome Web Store Developer Dashboard

## Permissions

This extension requires:
- `tabs` - To monitor tab URLs
- `storage` - For future settings storage
- `webNavigation` - For detailed navigation tracking
- Connection to localhost:5095 - To communicate with the App Limiter service