This project aims to support highly configurable automaton for Dynv6 zones. https://dynv6.com/

It supports configuration via appsettings.json.

- ForceUpdate will force the ip update every run.
- Environment variables are read from the fixed prefix `DYNV6_UPDATER`. Example: `DYNV6_UPDATER__ZONE_NAME` and `DYNV6_UPDATER__KEY`.
- PasswordPepper strengthens the stored user passwords. Set it with `DYNV6_UPDATER__PASSWORD_PEPPER` and keep it secret.
- LastPublicIpPath will overwrite the default save location. It will save only the last correct IP.
- RunHistoryPath stores the last run history used by the web dashboard.
- PublicIpProviders will give the program a list of public ip value providers, they have to return a JSON value as {"ip": "A.N.Y.IP"}
- DyndnsAPIUrl will overwrite the dynv6 update url
- WebUi:Url controls the local dashboard address. The default is http://localhost:5050.

On first startup, the web app asks you to create two users and their passwords:

- `viewer` with role `none`
- `admin` with role `admin`

The users are stored in a read-only BSON file after setup completes. After that, sign in with the account you need:

- `viewer` can read the dashboard and history.
- `admin` can also run updates and change settings.

If you want to run the program every X days, make a scheduled task in windows.

## Service Installer

The repository includes a PowerShell installer under `installer/`.

- Run `installer/install-service.ps1` from an elevated PowerShell session.
- It publishes the service, installs it as `LocalSystem`, and sets it to start automatically even when no user is logged on.
- Use `installer/uninstall-service.ps1` to remove it.

## Web Dashboard

The service now exposes a small web dashboard that shows the latest runs and lets you launch an update manually.

- Open the configured `WebUi:Url` in a browser while the service is running.
- Use the **Run update now** button to trigger an immediate cycle.
- The page shows persisted run history, including success state, IP values, and failure details.

For creating the Environmnet variables, this script could be used:

```
# Set the zone name (you can change this as needed)
$zoneName = "MY_HOSTNAME"
# Set the token (replace with your actual token)
$token = "YOUR_ACTUAL_TOKEN_HERE"
# Create the environment variables
[Environment]::SetEnvironmentVariable("DYNV6_UPDATER__ZONE_NAME", $zoneName, "Process")
[Environment]::SetEnvironmentVariable("DYNV6_UPDATER__KEY", $token, "Process")
# Verify the variables were created
Write-Host "Environment variables created:"
Write-Host "DYNV6_UPDATER__ZONE_NAME = $([Environment]::GetEnvironmentVariable("DYNV6_UPDATER__ZONE_NAME", "Process"))"
Write-Host "DYNV6_UPDATER__KEY = $([Environment]::GetEnvironmentVariable("DYNV6_UPDATER__KEY", "Process"))"

```

## Scaffold

The repository now includes a .NET solution with:

- `src/Dyndns.Service` for the Windows service host and Dynv6 integration boundaries
- `test/Dyndns.Service.Tests` for the first unit-test slice

To build and test the scaffold:

```bash
dotnet build Dyndns.slnx
dotnet test Dyndns.slnx
```

## Failure Notifications

If the update flow fails, the service now sends an email notification and switches to retry mode.

- Normal health checks run every 5 minutes.
- Failed updates retry every hour.
- Failure emails are sent every hour during daytime and every 8 hours during nighttime.

Configure SMTP settings under the `Notifications` section in `src/Dyndns.Service/appsettings.json`.

## Container

The service can be packaged as a Linux container with Podman using the root-level `Containerfile`.

Build the image:

```bash
podman build -t dynv6-automaton -f Containerfile .
```

Run the container with a writable data volume:

```bash
podman run --rm -p 8080:8080 -v dynv6-data:/data dynv6-automaton
```

The container listens on port `8080` and stores its runtime files in `/data`:

- `/data/users.bson`
- `/data/last-public-ip.txt`
- `/data/run-history.jsonl`
- `/data/dynv6-runtime-settings.json`

If you want to use bind mounts instead of a named volume, mount any writable folder to `/data` and keep the same file paths.

When running in a container, set `DYNV6_UPDATER__PASSWORD_PEPPER` to a long random secret.
