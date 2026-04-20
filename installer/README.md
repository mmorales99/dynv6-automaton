# Installer

Use the PowerShell installer to publish and register the service as a Windows service.

## Install

Run an elevated PowerShell session and execute:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\installer\install-service.ps1
```

The installer publishes the app, creates the service as `LocalSystem`, sets delayed auto-start, and configures restart recovery.

## Uninstall

Run:

```powershell
.\installer\uninstall-service.ps1
```

## Notes

- The service can run when no user is logged on because it is installed as a Windows service.
- The dashboard listens on `http://localhost:5050` by default.
- If you want a different publish folder, pass `-PublishFolder` to the scripts.
