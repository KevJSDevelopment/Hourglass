let websiteUrls = {};
let connectionActive = false;
let retryCount = 0;
const MAX_RETRIES = 5;
const RETRY_DELAY = 5000; // 5 seconds
// Setup WebSocket connection
function setupWebSocket() {
    const ws = new WebSocket('ws://localhost:5095/websocket/tabs');
    ws.onopen = () => {
        console.log('Connected to App Limiter service');
        connectionActive = true;
        retryCount = 0;
        // Send initial state
        sendUrlsToService(ws);
    };
    ws.onclose = () => {
        console.log('Disconnected from App Limiter service');
        connectionActive = false;

        // Attempt to reconnect if not at max retries
        if (retryCount < MAX_RETRIES) {
            retryCount++;
            setTimeout(() => {
                console.log(`Attempting reconnection(${ retryCount } / ${ MAX_RETRIES })`);
                setupWebSocket();
            }, RETRY_DELAY);
        }
    };
    ws.onerror = (error) => {
        console.error('WebSocket error:', error);
    };
    return ws;
}
let ws = setupWebSocket();
// Monitor tabs for URL changes
chrome.tabs.onUpdated.addListener((tabId, changeInfo, tab) => {
    if (changeInfo.url) {
        websiteUrls[tabId] = changeInfo.url;
        if (connectionActive) {
            sendUrlsToService(ws);
        }
    }
});
// Monitor tab closures
chrome.tabs.onRemoved.addListener((tabId) => {
    delete websiteUrls[tabId];
    if (connectionActive) {
        sendUrlsToService(ws);
    }
});
// Get initial tab states
chrome.tabs.query({}, (tabs) => {
    tabs.forEach(tab => {
        if (tab.url) {
            websiteUrls[tab.id] = tab.url;
        }
    });
    if (connectionActive) {
        sendUrlsToService(ws);
    }
});
function sendUrlsToService(ws) {
    try {
        ws.send(JSON.stringify({
            type: 'tabUpdate',
            urls: Object.values(websiteUrls)
        }));
    } catch (error) {
        console.error('Error sending update:', error);
        connectionActive = false;
        if (retryCount < MAX_RETRIES) {
            ws = setupWebSocket();
        }
    }
}