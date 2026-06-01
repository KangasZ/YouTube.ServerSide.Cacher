const input = document.getElementById('vid');
const button = document.getElementById('go');
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

// const fillVideo = document.getElementById('fill-video');
// const fillAudio = document.getElementById('fill-audio');

function clampPct(n) {
    const v = typeof n === 'number' ? n : 0;
    return Math.min(100, Math.max(0, v));
}

function fmtElapsed(startIso) {
    if (!startIso) return '-';
    const ms = Date.now() - new Date(startIso).getTime();
    if (!isFinite(ms) || ms < 0) return '-';
    const s = Math.floor(ms / 1000);
    const m = Math.floor(s / 60);
    return m > 0 ? `${m}m ${s % 60}s` : `${s}s`;
}

function renderStats(info) {
    stats.classList.add('visible');
    stats.classList.remove('state-done', 'state-failed');

    const state = info.status ?? '-';
    const vPct = clampPct(info.videoProgress);
    const aPct = clampPct(info.audioProgress);
    const vSize = info.videoSize ?? '-';
    const aSize = info.audioSize ?? '-';
    const totalSize = info.totalSize ?? '-';
    const start = info.startTime ?? null;

    // fillVideo.style.width = `${vPct.toFixed(1)}%`;
    // fillAudio.style.width = `${aPct.toFixed(1)}%`;
    const tPct = clampPct(info.totalProgress);
    const tSpeed = info.currentDownloadSpeed ?? '-';

    fillTotal.style.width = `${tPct.toFixed(1)}%`;
    setField('s-tpct', `${tPct.toFixed(1)}%`);
    setField('s-speed', tSpeed);
    setField('s-status', state);
    setField('s-total-size', totalSize);
    setField('s-elapsed', fmtElapsed(start));
    // setField('s-vpct', `${vPct.toFixed(1)}%`);
    // setField('s-vsize', vSize);
    // setField('s-apct', `${aPct.toFixed(1)}%`);
    // setField('s-asize', aSize);

    if (DONE_STATES.has(state)) stats.classList.add('state-done');
    if (FAIL_STATES.has(state)) stats.classList.add('state-failed');
}

async function pollOnce(id) {
    try {
        const r = await fetch(`/status?v=${encodeURIComponent(id)}`, {method: 'POST'});
        if (r.status === 404) {
            stopPolling();
            return;
        }
        if (!r.ok) throw new Error(r.statusText);
        const info = await r.json();
        renderStats(info);
        if (TERMINAL_STATES.has(info.status)) stopPolling();
    } catch (err) {
        status.textContent = `status error: ${err.message}`;
        stopPolling();
    }
}

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

async function submit() {
    const id = extractVideoId(input.value);
    if (!id) {
        status.textContent = 'Invalid video ID or URL';
        return;
    }

    const url = `${window.location.origin}/watch?v=${encodeURIComponent(id)}`;
    try {
        await navigator.clipboard.writeText(url);
        status.textContent = `Copied: ${url}`;
    } catch {
        status.textContent = `URL: ${url}`;
    }

    try {
        const r = await fetch(`/queue?v=${encodeURIComponent(id)}`, {method: 'POST'});
        if (!r.ok) throw new Error(r.statusText);
        startPolling(id);
    } catch (err) {
        status.textContent += ` (queue error: ${err.message})`;
    }
}

button.addEventListener('click', submit);
input.addEventListener('keydown', e => {
    if (e.key === 'Enter') submit();
});

const pasteBtn = document.getElementById('paste');
const clearBtn = document.getElementById('clear');

clearBtn.addEventListener('click', () => {
    input.value = '';
    input.focus();
});

pasteBtn.addEventListener('click', async () => {
    try {
        const text = await navigator.clipboard.readText();
        input.value = text;
        input.focus();
    } catch (err) {
        status.textContent = `paste error: ${err.message}`;
    }
});
