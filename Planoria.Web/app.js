const API_URL = "http://localhost:5225";
const statusEl = document.getElementById("status");

async function checkApi() {
    try {
        const response = await fetch(`${API_URL}/health`);
        if (!response.ok) throw new Error(`HTTP ${response.status}`);
        const data = await response.json();
        statusEl.textContent = `API status: ${data.status}`;
    } catch (err) {
        statusEl.textContent = "API is not reachable.";
    }
}

checkApi();