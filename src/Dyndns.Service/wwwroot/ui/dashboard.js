const latestMeta = document.getElementById("latestMeta");
const latestResume = document.getElementById("latestResume");
const latestStatus = document.getElementById("latestStatus");
const latestDuration = document.getElementById("latestDuration");
const latestRunAt = document.getElementById("latestRunAt");
const latestErrorType = document.getElementById("latestErrorType");
const runsBody = document.getElementById("runsBody");
const sessionBadge = document.getElementById("sessionBadge");
const logoutButton = document.getElementById("logoutButton");
const viewerNote = document.getElementById("viewerNote");
const runFeedback = document.getElementById("runFeedback");
const runButton = document.getElementById("runButton");
const settingsToggleButton = document.getElementById("settingsToggleButton");
const settingsAccessFeedback = document.getElementById(
  "settingsAccessFeedback",
);
const settingsOverlay = document.getElementById("settingsOverlay");
const settingsShell = document.getElementById("settingsShell");
const closeSettingsButton = document.getElementById("closeSettingsButton");
const settingsForm = document.getElementById("settingsForm");
const settingsFeedback = document.getElementById("settingsFeedback");
const zoneNameInput = document.getElementById("zoneName");
const keyInput = document.getElementById("key");
const forceUpdateInput = document.getElementById("forceUpdate");
const scheduledEveryInput = document.getElementById("scheduledEvery");
const lastPublicIpPathInput = document.getElementById("lastPublicIpPath");
const runtimeSettingsPathInput = document.getElementById("runtimeSettingsPath");
const runHistoryPathInput = document.getElementById("runHistoryPath");
let openDetailsKey = null;

function formatDate(value) {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "medium",
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
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#39;");
}

function statusMarkup(entry) {
  if (!entry.succeeded) {
    return '<span class="status bad">Failed</span>';
  }

  return entry.updated
    ? '<span class="status good">Updated</span>'
    : '<span class="status neutral">No change</span>';
}

function getLatestResumeText(entry) {
  if (!entry) {
    return "Launch a run to create the first entry.";
  }

  if (!entry.succeeded) {
    return entry.errorSummary ?? "Update failed.";
  }

  return entry.updated
    ? "The update completed successfully."
    : "No DNS changes were needed.";
}

function getLatestStatusText(entry) {
  if (!entry) {
    return "--";
  }

  if (!entry.succeeded) {
    return "FAILED";
  }

  return entry.updated ? "UPDATED" : "NO CHANGE";
}

function getLatestStatusClass(entry) {
  if (!entry) {
    return "status-warn";
  }

  if (!entry.succeeded) {
    return "status-bad";
  }

  return entry.updated ? "status-good" : "status-warn";
}

function getLatestErrorType(entry) {
  if (!entry || entry.succeeded) {
    return "--";
  }

  const errorSummary = (entry.errorSummary ?? "").trim();

  if (!errorSummary) {
    return "Unexpected Error";
  }

  if (/request failed/i.test(errorSummary)) {
    return "Request Failed";
  }

  if (/timed out/i.test(errorSummary)) {
    return "Timed Out";
  }

  if (/invalid ipv4/i.test(errorSummary)) {
    return "Invalid IPv4";
  }

  return errorSummary.replace(/\.$/, "");
}

function getLatestErrorTypeClass(entry) {
  if (entry && !entry.succeeded) {
    return "status-bad";
  }

  return "status-warn";
}

function getHistoryEntryKey(entry) {
  return `${entry.startedAt}`;
}

function handleDetailsToggle(event) {
  const details = event.currentTarget;
  const entryKey = details.dataset.entryKey ?? "";

  if (details.open) {
    openDetailsKey = entryKey;
    runsBody.querySelectorAll("details.trace-panel").forEach((otherDetails) => {
      if (otherDetails !== details) {
        otherDetails.open = false;
      }
    });
    return;
  }

  if (openDetailsKey === entryKey) {
    openDetailsKey = null;
  }
}

function readSettingsForm() {
  return {
    zoneName: zoneNameInput.value.trim(),
    key: keyInput.value,
    forceUpdate: forceUpdateInput.checked,
    scheduledEvery: scheduledEveryInput.value.trim(),
    lastPublicIpPath: lastPublicIpPathInput.value.trim(),
    runtimeSettingsPath: runtimeSettingsPathInput.value.trim(),
    runHistoryPath: runHistoryPathInput.value.trim(),
  };
}

function setSettingsForm(settings) {
  zoneNameInput.value = settings.zoneName ?? "";
  keyInput.value = settings.key ?? "";
  forceUpdateInput.checked = Boolean(settings.forceUpdate);
  scheduledEveryInput.value = settings.scheduledEvery ?? "00:05:00";
  lastPublicIpPathInput.value = settings.lastPublicIpPath ?? "";
  runtimeSettingsPathInput.value = settings.runtimeSettingsPath ?? "";
  runHistoryPathInput.value = settings.runHistoryPath ?? "";
}

function toggleSettings() {
  if (settingsOverlay.hidden) {
    return;
  }

  const isOpen = settingsOverlay.classList.toggle("open");
  settingsOverlay.setAttribute("aria-hidden", String(!isOpen));
  settingsShell.setAttribute("aria-hidden", String(!isOpen));
  settingsToggleButton.textContent = isOpen ? "CLOSE SETTINGS" : "SETTINGS";
}

function closeSettings() {
  settingsOverlay.classList.remove("open");
  settingsOverlay.hidden = true;
  settingsOverlay.setAttribute("aria-hidden", "true");
  settingsShell.setAttribute("aria-hidden", "true");
  settingsToggleButton.textContent = "SETTINGS";
}

async function unlockSettings() {
  const password = globalThis.prompt(
    "Enter the admin password to open settings",
  );
  if (password === null) {
    return;
  }

  settingsAccessFeedback.textContent = "Checking admin password...";

  try {
    const response = await fetch("/api/admin/verify", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ userName: "admin", password }),
    });

    if (!response.ok) {
      throw new Error("Incorrect admin password.");
    }

    settingsAccessFeedback.textContent = "";
    settingsOverlay.hidden = false;
    settingsOverlay.classList.add("open");
    settingsOverlay.setAttribute("aria-hidden", "false");
    settingsShell.setAttribute("aria-hidden", "false");
    settingsToggleButton.textContent = "CLOSE SETTINGS";
    await loadSettings();
  } catch (error) {
    settingsAccessFeedback.textContent = error.message;
  }
}

async function loadSession() {
  const response = await fetch("/api/me");
  if (!response.ok) {
    throw new Error(`Session request failed with ${response.status}`);
  }

  const session = await response.json();
  sessionBadge.textContent = `Signed in as ${session.name}`;

  logoutButton.hidden = false;

  if (session.isAdmin) {
    runButton.hidden = false;
    settingsToggleButton.hidden = false;
    closeSettings();
    viewerNote.hidden = true;
    return;
  }

  runButton.hidden = false;
  settingsToggleButton.hidden = true;
  closeSettings();
  viewerNote.hidden = false;
}

async function loadSettings() {
  const response = await fetch("/api/settings");
  if (!response.ok) {
    throw new Error(`Settings request failed with ${response.status}`);
  }

  const settings = await response.json();
  setSettingsForm(settings);
}

async function saveSettings(event) {
  event.preventDefault();
  settingsFeedback.textContent = "Saving...";

  try {
    const response = await fetch("/api/settings", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(readSettingsForm()),
    });

    if (!response.ok) {
      throw new Error(`Save request failed with ${response.status}`);
    }

    const settings = await response.json();
    setSettingsForm(settings);
    settingsFeedback.textContent = "Settings saved.";
    closeSettings();
  } catch (error) {
    settingsFeedback.textContent = error.message;
  }
}

function renderHistory(entries) {
  if (!entries.length) {
    runsBody.innerHTML =
      '<tr><td colspan="5" class="muted">No runs recorded yet.</td></tr>';
    latestStatus.textContent = "--";
    latestStatus.className = "status-value status-warn";
    latestMeta.textContent = "Launch a run to create the first entry.";
    latestResume.textContent = "Launch a run to create the first entry.";
    latestDuration.textContent = "--";
    latestRunAt.textContent = "--";
    latestErrorType.textContent = "--";
    latestErrorType.className = "status-value status-warn";
    latestErrorType.hidden = true;
    return;
  }

  const [latest] = entries;
  latestStatus.textContent = getLatestStatusText(latest);
  latestStatus.className = `status-value ${getLatestStatusClass(latest)}`;
  latestMeta.textContent = `${formatDate(latest.startedAt)} • ${latest.message}`;
  latestResume.textContent = getLatestResumeText(latest);
  latestDuration.textContent = formatDuration(latest.durationMilliseconds);
  latestRunAt.textContent = formatDate(latest.startedAt);
  latestErrorType.textContent = getLatestErrorType(latest);
  latestErrorType.className = `status-value ${getLatestErrorTypeClass(latest)}`;
  latestErrorType.hidden = latest.succeeded;

  runsBody.innerHTML = entries
    .map((entry) => {
      const entryKey = getHistoryEntryKey(entry);
      const isDetailsOpen = openDetailsKey === entryKey;
      const detailsOpenAttribute = isDetailsOpen ? " open" : "";
      const currentIp = entry.currentIp ? escapeHtml(entry.currentIp) : "—";
      const previousIp = entry.previousIp ? escapeHtml(entry.previousIp) : "—";
      let summaryLine = `<div class="muted" style="margin-top: 6px;">${escapeHtml(entry.errorSummary ?? "Update failed.")}</div>`;

      if (entry.succeeded) {
        summaryLine = entry.updated
          ? '<div class="muted" style="margin-top: 6px;">Update applied successfully.</div>'
          : '<div class="muted" style="margin-top: 6px;">No change was required.</div>';
      }
      const detailsRow = entry.error
        ? `
                <tr class="details-row">
                    <td colspan="5">
                <details class="trace-panel" data-entry-key="${escapeHtml(entryKey)}"${detailsOpenAttribute}>
                            <summary>Details</summary>
                            <pre>${escapeHtml(entry.error)}</pre>
                        </details>
                    </td>
                </tr>
            `
        : "";

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
    })
    .join("");

  runsBody
    .querySelectorAll("tr")
    .forEach((row) => row.classList.remove("has-details"));
  runsBody
    .querySelectorAll("tr:not(.details-row) + tr.details-row")
    .forEach((detailsRow) => {
      const previousRow = detailsRow.previousElementSibling;
      if (previousRow) {
        previousRow.classList.add("has-details");
      }
    });

  runsBody.querySelectorAll("details.trace-panel").forEach((detailsElement) => {
    detailsElement.addEventListener("toggle", handleDetailsToggle);
  });
}

async function loadHistory() {
  try {
    const response = await fetch("/api/runs");
    if (!response.ok) {
      throw new Error(`History request failed with ${response.status}`);
    }

    const entries = await response.json();
    renderHistory(entries);
  } catch (error) {
    runsBody.innerHTML = `<tr><td colspan="5" class="muted">${escapeHtml(error.message)}</td></tr>`;
    latestStatus.textContent = "--";
    latestStatus.className = "status-value status-warn";
    latestMeta.textContent = error.message;
    latestResume.textContent = "History could not be loaded.";
    latestDuration.textContent = "--";
    latestRunAt.textContent = "--";
    latestErrorType.textContent = "--";
    latestErrorType.className = "status-value status-warn";
    latestErrorType.hidden = true;
    openDetailsKey = null;
  }
}

async function runNow() {
  const password = globalThis.prompt("Enter the password to run the update");
  if (password === null) {
    return;
  }

  runFeedback.className = "feedback";
  runFeedback.textContent = "Checking password...";
  runButton.disabled = true;
  runButton.textContent = "RUNNING...";

  try {
    const accessResponse = await fetch("/api/run/check", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ password }),
    });

    if (!accessResponse.ok) {
      throw new Error("Incorrect password.");
    }

    const access = await accessResponse.json();
    let forceUpdate = false;

    if (access.isAdmin) {
      forceUpdate = globalThis.confirm("Use force update for this run?");
    }

    const response = await fetch("/api/run", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ password, forceUpdate }),
    });

    if (!response.ok) {
      throw new Error(`Run request failed with ${response.status}`);
    }

    await response.json();
    runFeedback.textContent =
      access.isAdmin && forceUpdate
        ? "Force update started."
        : "Update started.";
    await loadHistory();
  } catch (error) {
    runFeedback.className = "feedback error";
    runFeedback.textContent = error.message;
  } finally {
    runButton.disabled = false;
    runButton.textContent = "RUN";
  }
}

runButton.addEventListener("click", runNow);
settingsToggleButton.addEventListener("click", async () => {
  if (settingsOverlay.hidden) {
    await unlockSettings();
    return;
  }

  toggleSettings();
});
closeSettingsButton.addEventListener("click", closeSettings);
settingsForm.addEventListener("submit", saveSettings);
logoutButton.addEventListener("click", async () => {
  await fetch("/api/logout", { method: "POST" });
  globalThis.location.href = "/";
});

try {
  await loadSession();
  await loadHistory();
} catch (error) {
  sessionBadge.textContent = error.message;
}
setInterval(loadHistory, 15000);
