window.AppConfig = {
    API_BASE_URL: (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') 
        ? 'http://localhost:5000/api' 
        : 'https://tinker.twobrain.ai/api'
};
