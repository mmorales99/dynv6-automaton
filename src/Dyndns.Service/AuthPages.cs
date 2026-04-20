namespace Dyndns.Service;

internal static class AuthPages
{
    public static string LoginHtml(string? errorMessage = null) => BuildPage(
        "Sign in",
        "Access the Dynv6 dashboard with the viewer or admin password.",
        $$"""
            <form class="auth-form" id="loginForm">
                <div class="field">
                    <label for="password">Password</label>
                    <input id="password" name="password" type="password" autocomplete="current-password" required />
                </div>
                <button class="primary" type="submit">Sign in</button>
                <div class="feedback" id="feedback">{{Escape(errorMessage)}}</div>
            </form>
            <script>
                const loginForm = document.getElementById('loginForm');
                const feedback = document.getElementById('feedback');
                loginForm.addEventListener('submit', async event => {
                    event.preventDefault();
                    feedback.textContent = 'Signing in...';
                    const payload = {
                        password: document.getElementById('password').value
                    };

                    try {
                        const response = await fetch('/api/login', {
                            method: 'POST',
                            headers: { 'Content-Type': 'application/json' },
                            body: JSON.stringify(payload)
                        });

                        if (!response.ok) {
                            const message = await response.text();
                            throw new Error(message || `Login failed with ${response.status}`);
                        }

                        window.location.href = '/dashboard';
                    } catch (error) {
                        feedback.textContent = error.message;
                    }
                });
            </script>
        """
    );

    public static string SetupHtml(string? errorMessage = null) => BuildPage(
        "First setup",
        "Create the two local users. The resulting BSON file is stored read-only after it is created.",
        $$"""
            <form class="auth-form" id="setupForm">
                <div class="field">
                    <label for="adminPassword">Admin password</label>
                    <input id="adminPassword" name="adminPassword" type="password" autocomplete="new-password" required />
                </div>
                <div class="field">
                    <label for="viewerPassword">Viewer password</label>
                    <input id="viewerPassword" name="viewerPassword" type="password" autocomplete="new-password" required />
                </div>
                <button class="primary" type="submit">Create users</button>
                <div class="feedback" id="feedback">{{Escape(errorMessage)}}</div>
            </form>
            <script>
                const setupForm = document.getElementById('setupForm');
                const adminPasswordInput = document.getElementById('adminPassword');
                const viewerPasswordInput = document.getElementById('viewerPassword');
                const feedback = document.getElementById('feedback');

                function validatePasswords() {
                    if (adminPasswordInput.value && viewerPasswordInput.value && adminPasswordInput.value === viewerPasswordInput.value) {
                        feedback.textContent = 'Viewer and admin passwords must be different.';
                        return false;
                    }

                    feedback.textContent = '';
                    return true;
                }

                adminPasswordInput.addEventListener('input', validatePasswords);
                viewerPasswordInput.addEventListener('input', validatePasswords);

                setupForm.addEventListener('submit', async event => {
                    event.preventDefault();

                    if (!validatePasswords()) {
                        return;
                    }

                    feedback.textContent = 'Creating users...';
                    const payload = {
                        adminPassword: document.getElementById('adminPassword').value,
                        viewerPassword: document.getElementById('viewerPassword').value
                    };

                    try {
                        const response = await fetch('/api/setup', {
                            method: 'POST',
                            headers: { 'Content-Type': 'application/json' },
                            body: JSON.stringify(payload)
                        });

                        if (!response.ok) {
                            const message = await response.text();
                            throw new Error(message || `Setup failed with ${response.status}`);
                        }

                        window.location.href = '/';
                    } catch (error) {
                        feedback.textContent = error.message;
                    }
                });
            </script>
        """
    );

    private static string BuildPage(string title, string subtitle, string bodyContent) => $$"""
<!doctype html>
<html lang="en">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>Dynv6 Automaton - {{Escape(title)}}</title>
    <style>
        :root {
            color-scheme: dark;
            --bg: #101319;
            --panel: rgba(16, 19, 25, 0.86);
            --panel-border: rgba(255, 255, 255, 0.09);
            --text: #eef1f7;
            --muted: #a8b0be;
            --accent: #f59e0b;
            --good: #22c55e;
            --line: rgba(255, 255, 255, 0.08);
            --shadow: 0 24px 80px rgba(0, 0, 0, 0.35);
        }

        * { box-sizing: border-box; }

        body {
            margin: 0;
            min-height: 100vh;
            display: grid;
            place-items: center;
            font-family: Georgia, "Iowan Old Style", "Palatino Linotype", serif;
            color: var(--text);
            background:
                radial-gradient(circle at top left, rgba(245, 158, 11, 0.18), transparent 32%),
                radial-gradient(circle at top right, rgba(251, 113, 133, 0.16), transparent 24%),
                linear-gradient(180deg, #171b24 0%, #101319 100%);
            padding: 24px;
        }

        .card {
            width: min(540px, 100%);
            background: var(--panel);
            border: 1px solid var(--panel-border);
            border-radius: 24px;
            box-shadow: var(--shadow);
            backdrop-filter: blur(16px);
            padding: 28px;
        }

        h1 {
            margin: 0;
            font-size: clamp(2.1rem, 5vw, 3.6rem);
            line-height: 0.95;
        }

        .lede {
            margin: 12px 0 0;
            color: var(--muted);
            line-height: 1.6;
        }

        .note {
            margin-top: 12px;
            padding: 12px 14px;
            border-radius: 16px;
            border: 1px solid var(--line);
            color: var(--muted);
            background: rgba(255,255,255,0.04);
            line-height: 1.5;
        }

        .auth-form {
            display: grid;
            gap: 14px;
            margin-top: 20px;
        }

        .field {
            display: grid;
            gap: 8px;
        }

        label {
            color: var(--muted);
            font-size: 0.82rem;
            letter-spacing: 0.12em;
            text-transform: uppercase;
        }

        input {
            width: 100%;
            border-radius: 14px;
            border: 1px solid var(--line);
            background: rgba(255,255,255,0.05);
            color: var(--text);
            padding: 12px 14px;
            font: inherit;
        }

        button {
            border: 0;
            border-radius: 999px;
            padding: 14px 20px;
            font: inherit;
            font-weight: 700;
            cursor: pointer;
            color: #1a1204;
            background: linear-gradient(135deg, #fbbf24, #fb7185);
            box-shadow: 0 12px 30px rgba(251, 191, 36, 0.25);
        }

        .feedback {
            min-height: 1.25rem;
            color: #b7f7c9;
            font-size: 0.9rem;
        }

        .footer {
            margin-top: 18px;
            color: var(--muted);
            font-size: 0.9rem;
            line-height: 1.6;
        }
    </style>
</head>
<body>
    <main class="card">
        <p class="lede">{{Escape(subtitle)}}</p>
        <h1>{{Escape(title)}}</h1>
        <div class="note">{{bodyContent}}</div>
        <div class="footer">Dynv6 Automaton</div>
    </main>
</body>
</html>
""";

    private static string Escape(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&#39;");
}
