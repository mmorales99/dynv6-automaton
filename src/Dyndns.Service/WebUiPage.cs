namespace Dyndns.Service;

internal static class WebUiPage
{
    public static string Html => """
<!doctype html>
<html lang="en">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>Dynv6 Automaton</title>
    <style>
        :root {
            color-scheme: dark;
            --bg: #101319;
            --panel: rgba(16, 19, 25, 0.86);
            --panel-border: rgba(255, 255, 255, 0.09);
            --text: #eef1f7;
            --muted: #a8b0be;
            --accent: #f59e0b;
            --accent-strong: #fb7185;
            --good: #22c55e;
            --bad: #ef4444;
            --line: rgba(255, 255, 255, 0.08);
            --shadow: 0 24px 80px rgba(0, 0, 0, 0.35);
        }

        * { box-sizing: border-box; }

        body {
            margin: 0;
            min-height: 100vh;
            font-family: Georgia, "Iowan Old Style", "Palatino Linotype", serif;
            color: var(--text);
            background:
                radial-gradient(circle at top left, rgba(245, 158, 11, 0.18), transparent 32%),
                radial-gradient(circle at top right, rgba(251, 113, 133, 0.16), transparent 24%),
                linear-gradient(180deg, #171b24 0%, #101319 100%);
        }

        body::before {
            content: "";
            position: fixed;
            inset: 0;
            pointer-events: none;
            opacity: 0.18;
            background-image: linear-gradient(rgba(255,255,255,0.06) 1px, transparent 1px), linear-gradient(90deg, rgba(255,255,255,0.06) 1px, transparent 1px);
            background-size: 28px 28px;
            mask-image: linear-gradient(180deg, rgba(0,0,0,0.8), transparent 92%);
        }

        .shell {
            position: relative;
            max-width: 1180px;
            margin: 0 auto;
            padding: 32px 20px 48px;
        }

        .hero {
            display: grid;
            gap: 18px;
            grid-template-columns: 1.5fr 1fr;
            align-items: end;
            margin-bottom: 22px;
        }

        .eyebrow {
            letter-spacing: 0.22em;
            text-transform: uppercase;
            color: var(--accent);
            font-size: 0.77rem;
            margin: 0 0 10px;
        }

        h1 {
            margin: 0;
            font-size: clamp(3rem, 6.6vw, 6rem);
            line-height: 0.9;
            font-weight: 700;
            max-width: 12ch;
        }

        .lede {
            margin: 16px 0 0;
            max-width: 60ch;
            color: var(--muted);
            font-size: 1.02rem;
            line-height: 1.6;
        }

        .actions {
            display: flex;
            justify-content: flex-end;
            gap: 12px;
            flex-wrap: wrap;
        }

        .session-bar {
            display: flex;
            justify-content: flex-end;
            align-items: center;
            gap: 12px;
            flex-wrap: wrap;
        }

        .session-chip {
            display: inline-flex;
            align-items: center;
            gap: 8px;
            padding: 8px 12px;
            border-radius: 999px;
            background: rgba(255, 255, 255, 0.07);
            color: var(--text);
            font-size: 0.84rem;
            border: 1px solid var(--line);
        }

        .viewer-note {
            margin: 0 0 18px;
            padding: 12px 14px;
            border: 1px solid var(--line);
            border-radius: 16px;
            background: rgba(255, 255, 255, 0.04);
            color: var(--muted);
        }

        .feedback.error {
            color: #fecaca;
        }

        button {
            border: 0;
            border-radius: 999px;
            padding: 14px 20px;
            font: inherit;
            font-weight: 700;
            cursor: pointer;
            transition: transform 140ms ease, box-shadow 140ms ease, opacity 140ms ease;
        }

        button:hover { transform: translateY(-1px); }
        button:disabled { cursor: wait; opacity: 0.7; transform: none; }

        .primary {
            color: #1a1204;
            background: linear-gradient(135deg, #fbbf24, #fb7185);
            box-shadow: 0 12px 30px rgba(251, 191, 36, 0.25);
        }

        .secondary {
            color: var(--text);
            background: rgba(255, 255, 255, 0.06);
            border: 1px solid var(--panel-border);
        }

        .grid {
            display: grid;
            grid-template-columns: 360px 1fr;
            gap: 18px;
            align-items: start;
        }

        .stack {
            display: grid;
            gap: 18px;
        }

        .settings-toggle {
            display: inline-flex;
            align-items: center;
            justify-content: center;
            gap: 10px;
            width: 100%;
        }

        .settings-shell {
            display: none;
        }

        .settings-shell.open {
            display: block;
        }

        .card {
            background: var(--panel);
            border: 1px solid var(--panel-border);
            border-radius: 24px;
            box-shadow: var(--shadow);
            backdrop-filter: blur(16px);
        }

        .summary {
            padding: 22px;
            position: sticky;
            top: 18px;
        }

        .summary.status-ok {
            border-color: rgba(34, 197, 94, 0.28);
            box-shadow: 0 0 0 1px rgba(34, 197, 94, 0.12), var(--shadow);
        }

        .summary.status-failed {
            border-color: rgba(239, 68, 68, 0.28);
            box-shadow: 0 0 0 1px rgba(239, 68, 68, 0.12), var(--shadow);
        }

        .summary.status-warn {
            border-color: rgba(245, 158, 11, 0.28);
            box-shadow: 0 0 0 1px rgba(245, 158, 11, 0.12), var(--shadow);
        }

        .summary h2, .history h2 {
            margin: 0;
            font-size: 1.1rem;
            letter-spacing: 0.02em;
        }

        .summary .value {
            display: block;
            margin-top: 8px;
            font-size: 1.8rem;
            line-height: 1.1;
        }

        .summary.status-ok .value {
            color: var(--good);
        }

        .summary.status-failed .value {
            color: var(--bad);
        }

        .summary.status-warn .value {
            color: var(--accent);
        }

        .summary .resume {
            margin-top: 10px;
            color: var(--text);
            line-height: 1.5;
            font-size: 0.96rem;
        }

        .summary .meta {
            margin-top: 14px;
            color: var(--muted);
            line-height: 1.65;
            font-size: 0.97rem;
        }

        .status-row {
            display: flex;
            gap: 8px;
            flex-wrap: wrap;
            margin-top: 18px;
        }

        .pill {
            display: inline-flex;
            align-items: center;
            gap: 8px;
            padding: 8px 12px;
            border-radius: 999px;
            background: rgba(255,255,255,0.07);
            color: var(--text);
            font-size: 0.82rem;
            border: 1px solid var(--line);
        }

        .history {
            padding: 22px;
            overflow: hidden;
        }

        .details-row td {
            padding-top: 0;
            border-bottom: 1px solid var(--line);
        }

        .trace-panel {
            margin: 0 0 14px;
            padding: 14px 16px;
            border: 1px solid var(--line);
            border-radius: 16px;
            background: rgba(255, 255, 255, 0.04);
        }

        .trace-panel summary {
            font-weight: 700;
            color: var(--text);
            cursor: pointer;
        }

        .trace-panel pre {
            margin: 12px 0 0;
            white-space: pre-wrap;
            overflow-wrap: anywhere;
            word-break: break-word;
            max-height: 320px;
            overflow: auto;
            line-height: 1.45;
            font-family: ui-monospace, SFMono-Regular, Consolas, "Liberation Mono", monospace;
            font-size: 0.82rem;
            color: #d7dce6;
        }

        .settings {
            padding: 22px;
        }

        .settings-form {
            display: grid;
            gap: 14px;
            margin-top: 14px;
        }

        .field {
            display: grid;
            gap: 8px;
        }

        .field label {
            color: var(--muted);
            font-size: 0.82rem;
            letter-spacing: 0.12em;
            text-transform: uppercase;
        }

        .field input {
            width: 100%;
            border-radius: 14px;
            border: 1px solid var(--line);
            background: rgba(255,255,255,0.05);
            color: var(--text);
            padding: 12px 14px;
            font: inherit;
        }

        .field input[type="checkbox"] {
            width: 18px;
            height: 18px;
            padding: 0;
            accent-color: var(--accent);
        }

        .field input[type="text"].schedule-input {
            font-family: ui-monospace, SFMono-Regular, Consolas, "Liberation Mono", monospace;
            letter-spacing: 0.04em;
        }

        .field-row {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 12px;
        }

        .field-inline {
            display: flex;
            align-items: center;
            gap: 10px;
        }

        .helper {
            color: var(--muted);
            font-size: 0.88rem;
            line-height: 1.5;
        }

        .feedback {
            min-height: 1.25rem;
            color: #b7f7c9;
            font-size: 0.9rem;
        }

        table {
            width: 100%;
            border-collapse: collapse;
            margin-top: 14px;
        }

        th, td {
            text-align: left;
            padding: 14px 12px;
            border-bottom: 1px solid var(--line);
            vertical-align: top;
        }

        th {
            color: var(--muted);
            font-size: 0.78rem;
            letter-spacing: 0.14em;
            text-transform: uppercase;
        }

        .status {
            display: inline-flex;
            align-items: center;
            gap: 8px;
            padding: 7px 11px;
            border-radius: 999px;
            font-size: 0.78rem;
            font-weight: 700;
            background: rgba(255,255,255,0.08);
            white-space: nowrap;
        }

        .status.good { color: #b7f7c9; }
        .status.bad { color: #fecaca; }
        .status.neutral { color: #fde68a; }

        .muted { color: var(--muted); }
        .nowrap { white-space: nowrap; }

        details {
            margin-top: 8px;
            color: var(--muted);
        }

        summary {
            cursor: pointer;
            color: var(--text);
        }

        @media (max-width: 920px) {
            .hero, .grid { grid-template-columns: 1fr; }
            .actions { justify-content: flex-start; }
            .summary { position: static; }
        }

        @media (max-width: 700px) {
            .shell { padding-inline: 14px; }
            .history { padding-inline: 14px; }
            .field-row { grid-template-columns: 1fr; }
            table, thead, tbody, th, td, tr { display: block; }
            thead { display: none; }
            tr {
                padding: 14px 0;
                border-bottom: 1px solid var(--line);
            }
            td {
                display: flex;
                justify-content: space-between;
                gap: 16px;
                border: 0;
                padding: 8px 0;
            }
            td::before {
                content: attr(data-label);
                color: var(--muted);
                font-size: 0.78rem;
                letter-spacing: 0.12em;
                text-transform: uppercase;
                flex: 0 0 38%;
            }

            .details-row td::before {
                content: none;
            }

            .details-row td {
                padding-top: 0;
            }
        }
    </style>
</head>
<body>
    <main class="shell">
        <section class="hero">
            <div>
                <p class="eyebrow">Service dashboard</p>
                <h1>Dynv6 service control</h1>
                <p class="lede">
                    See recent runs, check the result, and start an update whenever you need to.
                </p>
            </div>
            <div class="session-bar">
                <span class="session-chip" id="sessionBadge">Checking session...</span>
                <button class="secondary" id="logoutButton" type="button">Log out</button>
                <button class="primary" id="runButton" type="button">Run update now</button>
            </div>
        </section>

        <div class="viewer-note" id="viewerNote" hidden>
            You are signed in as viewer. You can review the dashboard and run updates with your password.
        </div>

        <div class="feedback" id="runFeedback"></div>

        <section class="grid">
            <div class="stack">
                <article class="card summary">
                    <h2>Last run resume</h2>
                    <span class="value" id="latestOutcome">Loading...</span>
                    <div class="meta" id="latestMeta">Fetching persisted history from disk.</div>
                    <div class="resume" id="latestResume"></div>
                    <div class="status-row" id="latestPills"></div>
                </article>

                <button class="secondary settings-toggle" id="settingsToggleButton" type="button">Show settings</button>
                <div class="feedback" id="settingsAccessFeedback"></div>

                <article class="card settings settings-shell" id="settingsShell" aria-hidden="true" hidden>
                    <h2>Settings</h2>
                    <div class="helper">Changes are saved to the runtime settings file and take effect immediately for manual and scheduled runs.</div>
                    <form class="settings-form" id="settingsForm">
                        <div class="field">
                            <label for="zoneName">Zone name</label>
                            <input id="zoneName" name="zoneName" type="text" autocomplete="off" />
                        </div>
                        <div class="field">
                            <label for="key">Dynv6 key</label>
                            <input id="key" name="key" type="password" autocomplete="off" />
                        </div>
                        <div class="field-row">
                            <div class="field">
                                <label for="lastPublicIpPath">Last public IP path</label>
                                <input id="lastPublicIpPath" name="lastPublicIpPath" type="text" autocomplete="off" />
                            </div>
                            <div class="field">
                                <label for="runtimeSettingsPath">Runtime settings path</label>
                                <input id="runtimeSettingsPath" name="runtimeSettingsPath" type="text" autocomplete="off" />
                            </div>
                        </div>
                        <div class="field-inline">
                            <input id="forceUpdate" name="forceUpdate" type="checkbox" />
                            <label for="forceUpdate" style="color: var(--text); text-transform: none; letter-spacing: normal;">Force update on every run</label>
                        </div>
                        <div class="field">
                            <label for="scheduledEvery">Every</label>
                            <input id="scheduledEvery" class="schedule-input" name="scheduledEvery" type="text" inputmode="numeric" placeholder="00:05:00" autocomplete="off" />
                            <div class="helper">Format: HH:MM:SS. Example: 00:05:00 runs every five minutes.</div>
                        </div>
                        <div class="field">
                            <label for="runHistoryPath">Run history path</label>
                            <input id="runHistoryPath" name="runHistoryPath" type="text" autocomplete="off" />
                        </div>
                        <div class="actions" style="justify-content: flex-start;">
                            <button class="primary" id="saveSettingsButton" type="submit">Save settings</button>
                        </div>
                        <div class="feedback" id="settingsFeedback"></div>
                    </form>
                </article>
            </div>

            <article class="card history">
                <h2>Run history</h2>
                <div class="muted" style="margin-top: 8px;">Newest entries appear first. The manual run button writes a new entry immediately.</div>
                <table aria-label="Run history">
                    <thead>
                        <tr>
                            <th>Started</th>
                            <th>Status</th>
                            <th>IPs</th>
                            <th>Message</th>
                            <th>Duration</th>
                        </tr>
                    </thead>
                    <tbody id="runsBody"></tbody>
                </table>
            </article>
        </section>
    </main>

    <script>
        const latestOutcome = document.getElementById('latestOutcome');
        const latestMeta = document.getElementById('latestMeta');
        const latestResume = document.getElementById('latestResume');
        const latestPills = document.getElementById('latestPills');
        const runsBody = document.getElementById('runsBody');
        const sessionBadge = document.getElementById('sessionBadge');
        const logoutButton = document.getElementById('logoutButton');
        const viewerNote = document.getElementById('viewerNote');
        const runFeedback = document.getElementById('runFeedback');
        const runButton = document.getElementById('runButton');
        const settingsToggleButton = document.getElementById('settingsToggleButton');
        const settingsAccessFeedback = document.getElementById('settingsAccessFeedback');
        const settingsShell = document.getElementById('settingsShell');
        const settingsForm = document.getElementById('settingsForm');
        const settingsFeedback = document.getElementById('settingsFeedback');
        const zoneNameInput = document.getElementById('zoneName');
        const keyInput = document.getElementById('key');
        const forceUpdateInput = document.getElementById('forceUpdate');
        const scheduledEveryInput = document.getElementById('scheduledEvery');
        const lastPublicIpPathInput = document.getElementById('lastPublicIpPath');
        const runtimeSettingsPathInput = document.getElementById('runtimeSettingsPath');
        const runHistoryPathInput = document.getElementById('runHistoryPath');

        function formatDate(value) {
            return new Intl.DateTimeFormat(undefined, {
                dateStyle: 'medium',
                timeStyle: 'medium'
            }).format(new Date(value));
        }

        function formatDuration(ms) {
            if (ms < 1000) {
                return `${ms} ms`;
            }

            return `${(ms / 1000).toFixed(1)} s`;
        }

        function escapeHtml(value) {
            return String(value)
                .replaceAll('&', '&amp;')
                .replaceAll('<', '&lt;')
                .replaceAll('>', '&gt;')
                .replaceAll('"', '&quot;')
                .replaceAll("'", '&#39;');
        }

        function statusMarkup(entry) {
            if (!entry.succeeded) {
                return '<span class="status bad">Failed</span>';
            }

            return entry.updated
                ? '<span class="status good">Updated</span>'
                : '<span class="status neutral">No change</span>';
        }

        function getLatestPills(entry) {
            const pills = [];
            pills.push(`<span class="pill">${escapeHtml(entry.trigger)}</span>`);
            if (entry.currentIp) {
                pills.push(`<span class="pill">Current IP: ${escapeHtml(entry.currentIp)}</span>`);
            }
            if (entry.previousIp) {
                pills.push(`<span class="pill">Previous IP: ${escapeHtml(entry.previousIp)}</span>`);
            }
            pills.push(`<span class="pill">${formatDuration(entry.durationMilliseconds)}</span>`);
            return pills.join('');
        }

        function getLatestStatusClass(entry) {
            if (!entry) {
                return 'status-warn';
            }

            if (!entry.succeeded) {
                return 'status-failed';
            }

            return entry.updated ? 'status-ok' : 'status-warn';
        }

        function getLatestResumeText(entry) {
            if (!entry) {
                return 'Launch a run to create the first entry.';
            }

            if (!entry.succeeded) {
                return entry.errorSummary ?? 'Update failed.';
            }

            return entry.updated
                ? 'The update completed successfully.'
                : 'No DNS changes were needed.';
        }

        function readSettingsForm() {
            return {
                zoneName: zoneNameInput.value.trim(),
                key: keyInput.value,
                forceUpdate: forceUpdateInput.checked,
                scheduledEvery: scheduledEveryInput.value.trim(),
                lastPublicIpPath: lastPublicIpPathInput.value.trim(),
                runtimeSettingsPath: runtimeSettingsPathInput.value.trim(),
                runHistoryPath: runHistoryPathInput.value.trim()
            };
        }

        function setSettingsForm(settings) {
            zoneNameInput.value = settings.zoneName ?? '';
            keyInput.value = settings.key ?? '';
            forceUpdateInput.checked = Boolean(settings.forceUpdate);
            scheduledEveryInput.value = settings.scheduledEvery ?? '00:05:00';
            lastPublicIpPathInput.value = settings.lastPublicIpPath ?? '';
            runtimeSettingsPathInput.value = settings.runtimeSettingsPath ?? '';
            runHistoryPathInput.value = settings.runHistoryPath ?? '';
        }

        function toggleSettings() {
            if (settingsShell.hidden) {
                return;
            }

            const isOpen = settingsShell.classList.toggle('open');
            settingsShell.setAttribute('aria-hidden', String(!isOpen));
            settingsToggleButton.textContent = isOpen ? 'Hide settings' : 'Show settings';
        }

        async function unlockSettings() {
            const password = window.prompt('Enter the admin password to open settings');
            if (password === null) {
                return;
            }

            settingsAccessFeedback.textContent = 'Checking admin password...';

            try {
                const response = await fetch('/api/admin/verify', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({ userName: 'admin', password })
                });

                if (!response.ok) {
                    throw new Error('Incorrect admin password.');
                }

                settingsAccessFeedback.textContent = '';
                settingsShell.hidden = false;
                settingsShell.classList.add('open');
                settingsShell.setAttribute('aria-hidden', 'false');
                settingsToggleButton.textContent = 'Hide settings';
                await loadSettings();
            } catch (error) {
                settingsAccessFeedback.textContent = error.message;
            }
        }

        async function loadSession() {
            const response = await fetch('/api/me');
            if (!response.ok) {
                throw new Error(`Session request failed with ${response.status}`);
            }

            const session = await response.json();
            sessionBadge.textContent = session.isAdmin
                ? `Signed in as ${session.name} · admin`
                : `Signed in as ${session.name} · viewer`;

            logoutButton.hidden = false;

            if (session.isAdmin) {
                runButton.hidden = false;
                settingsToggleButton.hidden = false;
                settingsShell.hidden = true;
                settingsShell.classList.remove('open');
                settingsShell.setAttribute('aria-hidden', 'true');
                settingsToggleButton.textContent = 'Show settings';
                viewerNote.hidden = true;
                return;
            }

            runButton.hidden = false;
            settingsToggleButton.hidden = true;
            settingsShell.hidden = true;
            settingsShell.classList.remove('open');
            viewerNote.hidden = false;
        }

        async function loadSettings() {
            const response = await fetch('/api/settings');
            if (!response.ok) {
                throw new Error(`Settings request failed with ${response.status}`);
            }

            const settings = await response.json();
            setSettingsForm(settings);
        }

        async function saveSettings(event) {
            event.preventDefault();
            settingsFeedback.textContent = 'Saving...';

            try {
                const response = await fetch('/api/settings', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(readSettingsForm())
                });

                if (!response.ok) {
                    throw new Error(`Save request failed with ${response.status}`);
                }

                const settings = await response.json();
                setSettingsForm(settings);
                settingsFeedback.textContent = 'Settings saved.';
            } catch (error) {
                settingsFeedback.textContent = error.message;
            }
        }

        function renderHistory(entries) {
            if (!entries.length) {
                runsBody.innerHTML = '<tr><td colspan="5" class="muted">No runs recorded yet.</td></tr>';
                latestOutcome.textContent = 'No runs yet';
                latestMeta.textContent = 'Launch a run to create the first entry.';
                latestResume.textContent = 'Launch a run to create the first entry.';
                latestOutcome.parentElement.className = 'card summary status-warn';
                latestPills.innerHTML = '';
                return;
            }

            const [latest] = entries;
            latestOutcome.parentElement.className = `card summary ${getLatestStatusClass(latest)}`;
            latestOutcome.textContent = latest.succeeded ? (latest.updated ? 'Updated' : 'No change') : 'Failed';
            latestMeta.textContent = `${formatDate(latest.startedAt)} • ${latest.message}`;
            latestResume.textContent = getLatestResumeText(latest);
            latestPills.innerHTML = getLatestPills(latest);

            runsBody.innerHTML = entries.map(entry => {
                const currentIp = entry.currentIp ? escapeHtml(entry.currentIp) : '—';
                const previousIp = entry.previousIp ? escapeHtml(entry.previousIp) : '—';
                const summaryLine = entry.succeeded
                    ? entry.updated
                        ? '<div class="muted" style="margin-top: 6px;">Update applied successfully.</div>'
                        : '<div class="muted" style="margin-top: 6px;">No change was required.</div>'
                    : `<div class="muted" style="margin-top: 6px;">${escapeHtml(entry.errorSummary ?? 'Update failed.')}</div>`;
                const detailsRow = entry.error
                    ? `
                        <tr class="details-row">
                            <td colspan="5">
                                <details class="trace-panel" open>
                                    <summary>Details</summary>
                                    <pre>${escapeHtml(entry.error)}</pre>
                                </details>
                            </td>
                        </tr>
                    `
                    : '';

                return `
                    <tr>
                        <td data-label="Started" class="nowrap">${formatDate(entry.startedAt)}</td>
                        <td data-label="Status">${statusMarkup(entry)}</td>
                        <td data-label="IPs">
                            <div>Current: ${currentIp}</div>
                            <div class="muted">Previous: ${previousIp}</div>
                        </td>
                        <td data-label="Message">${escapeHtml(entry.message)}${summaryLine}</td>
                        <td data-label="Duration" class="nowrap">${formatDuration(entry.durationMilliseconds)}</td>
                    </tr>
                    ${detailsRow}
                `;
            }).join('');
        }

        async function loadHistory() {
            try {
                const response = await fetch('/api/runs');
                if (!response.ok) {
                    throw new Error(`History request failed with ${response.status}`);
                }

                const entries = await response.json();
                renderHistory(entries);
            } catch (error) {
                runsBody.innerHTML = `<tr><td colspan="5" class="muted">${escapeHtml(error.message)}</td></tr>`;
                latestOutcome.textContent = 'Unavailable';
                latestMeta.textContent = error.message;
                latestResume.textContent = 'History could not be loaded.';
                latestOutcome.parentElement.className = 'card summary status-failed';
                latestPills.innerHTML = '';
            }
        }

        async function runNow() {
            const password = window.prompt('Enter the password to run the update');
            if (password === null) {
                return;
            }

            runFeedback.className = 'feedback';
            runFeedback.textContent = 'Checking password...';
            runButton.disabled = true;
            runButton.textContent = 'Running...';

            try {
                const accessResponse = await fetch('/api/run/check', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({ password })
                });

                if (!accessResponse.ok) {
                    throw new Error('Incorrect password.');
                }

                const access = await accessResponse.json();
                let forceUpdate = false;

                if (access.isAdmin) {
                    forceUpdate = window.confirm('Use force update for this run?');
                }

                const response = await fetch('/api/run', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({ password, forceUpdate })
                });

                if (!response.ok) {
                    throw new Error(`Run request failed with ${response.status}`);
                }

                await response.json();
                runFeedback.textContent = access.isAdmin && forceUpdate
                    ? 'Force update started.'
                    : 'Update started.';
                await loadHistory();
            } catch (error) {
                runFeedback.className = 'feedback error';
                runFeedback.textContent = error.message;
            } finally {
                runButton.disabled = false;
                runButton.textContent = 'Run update now';
            }
        }

        runButton.addEventListener('click', runNow);
        settingsToggleButton.addEventListener('click', async () => {
            if (settingsShell.hidden) {
                await unlockSettings();
                return;
            }

            toggleSettings();
        });
        settingsForm.addEventListener('submit', saveSettings);
        logoutButton.addEventListener('click', async () => {
            await fetch('/api/logout', { method: 'POST' });
            window.location.href = '/';
        });

        loadSession().catch(error => {
            sessionBadge.textContent = error.message;
        });
        loadHistory();
        setInterval(loadHistory, 15000);
    </script>
</body>
</html>
""";
}