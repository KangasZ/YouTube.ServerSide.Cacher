

const status = document.getElementById('status');
const stats = document.getElementById('stats');
const fill = document.getElementById('fill');
const fillTotal = document.getElementById('fill-total');
let pollTimer = null;

function extractVideoId(raw) {
    const value = raw.trim();
    if (!value) return null;
    try {
        const url = new URL(value);
        const host = url.hostname.replace(/^www\./, '');
        if (host === 'youtu.be') return url.pathname.slice(1).split('/')[0] || null;
        if (host.endsWith('youtube.com') || host.endsWith('youtube-nocookie.com')) {
            const v = url.searchParams.get('v');
            if (v) return v;
            const m = url.pathname.match(/^\/(?:embed|shorts|live|v)\/([^/?#]+)/);
            if (m) return m[1];
        }
    } catch { /* not a URL */
    }
    if (/^[A-Za-z0-9_-]{6,}$/.test(value)) return value;
    return null;
}

function setField(id, value) {
    document.getElementById(id).textContent = value ?? '-';
}

const TERMINAL_STATES = new Set(['Success', 'Cached', 'Failed', 'Canceled']);
const DONE_STATES = new Set(['Success', 'Cached']);
const FAIL_STATES = new Set(['Failed', 'Canceled']);

class DownloadInformation {
    constructor(response) {
        this.site = response.site;
        this.siteId = response.siteId;
        this.status = response.status ?? '-';
        this.totalProgress = clampProgressPercent(response.totalProgress);
        this.totalSize = response.totalSize ?? '-';
        this.currentDownloadSpeed = response.currentDownloadSpeed ?? '-';
        this.startTime = response.startTime ?? null;
        this.endTime = response.endTime ?? null;
    }
}

function clampProgressPercent(n) {
    const v = typeof n === 'number' ? n : 0;
    return Math.min(100, Math.max(0, v));
}

function formatTimeSince(startIso, endIso) {
    if (!startIso) {
        return '-';
    }
    const endTime = endIso ? new Date(endIso).getTime() : Date.now();
    const ms = endTime - new Date(startIso).getTime();
    if (!isFinite(ms) || ms < 0) return '-';
    const s = Math.floor(ms / 1000);
    const m = Math.floor(s / 60);
    return m > 0 ? `${m}m ${s % 60}s` : `${s}s`;
}

function renderStats(apiResponse) {
    console.log("Rendering Stats");
    stats.classList.add('visible');
    stats.classList.remove('state-done', 'state-failed');

    const downloadResponse = new DownloadInformation(apiResponse);

    fillTotal.style.width = `${downloadResponse.totalProgress.toFixed(1)}%`;
    setField('s-tpct', `${downloadResponse.totalProgress.toFixed(1)}%`);
    setField('s-speed', downloadResponse.currentDownloadSpeed);
    setField('s-status', downloadResponse.status);
    setField('s-total-size', downloadResponse.totalSize);
    setField('s-elapsed', formatTimeSince(downloadResponse.startTime, downloadResponse.endTime));

    if (DONE_STATES.has(downloadResponse.status)) stats.classList.add('state-done');
    if (FAIL_STATES.has(downloadResponse.status)) stats.classList.add('state-failed');

    return downloadResponse;
}

async function pollOnce(id) {
    try {
        const r = await fetch(`/api/status/youtube/${encodeURIComponent(id)}`, {method: 'GET'});
        if (r.status === 404) {
            stopPolling();
            return;
        }
        if (!r.ok) {
            throw new Error(r.statusText);
        }
        const apiResponse = await r.json();
        const changedResponse = renderStats(apiResponse);
        if (TERMINAL_STATES.has(changedResponse.status)) stopPolling();
    } catch (err) {
        console.log(err);
        status.textContent = `status error: ${err}`;
        stopPolling();
    }
}

// Status updates

function startPolling(id) {
    stopPolling();
    pollOnce(id);
    pollTimer = setInterval(() => pollOnce(id), 1000);
}

function stopPolling() {
    if (pollTimer) {
        clearInterval(pollTimer);
        pollTimer = null;
    }
}

// Main submit function

async function submit() {
    const id = extractVideoId(input.value);
    if (!id) {
        status.textContent = 'Invalid video ID or URL';
        return;
    }
    const encodedVideoId = encodeURIComponent(id);
    const watchUrl = `${window.location.origin}/w/y/${encodedVideoId}`
    try {
        await navigator.clipboard.writeText(watchUrl);
        status.textContent = `Copied: ${watchUrl}`;
    } catch {
        status.textContent = `URL: ${watchUrl}`;
    }

    try {
        const r = await fetch(`/api/queue/youtube/${encodedVideoId}`, {method: 'GET'});
        if (!r.ok) throw new Error(r.statusText);
        startPolling(id);
    } catch (err) {
        status.textContent += ` (queue error: ${err.message})`;
    }
}

// Handle button inputs

// GO button
const button = document.getElementById('buttonGo');
button.addEventListener('click', submit);

// Input Text Field
const input = document.getElementById('inputVideoId');
input.addEventListener('keydown', e => {
    if (e.key === 'Enter') submit();
});

// Paste Button
const pasteBtn = document.getElementById('buttonPaste');
pasteBtn.addEventListener('click', async () => {
    try {
        const text = await navigator.clipboard.readText();
        input.value = text;
        input.focus();
    } catch (err) {
        status.textContent = `paste error: ${err.message}`;
    }
});

// Clear Button
const clearBtn = document.getElementById('buttonClear');

clearBtn.addEventListener('click', () => {
    input.value = '';
    input.focus();
});
