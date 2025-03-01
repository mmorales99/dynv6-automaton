This project aims to support highly configurable automaton for Dynv6 zones. https://dynv6.com/

It supports configuration via appsettings.json.
 + ForceUpdate will force the ip update every run.
 + EnvironmentKey will make the program to search for prefixed values. IE: "EnvironmentKey": "mcvingenieros" -->  mcvingenieros__hostname
 + LastPublicIpPath will overwrite the default save location. It will save only the last correct IP.
 + PublicIpProviders will give the program a list of public ip value providers, they have to return a JSON value as {"ip": "A.N.Y.IP"}
 + DyndnsAPIUrl will overwrite the dynv6 update url

For the program getting working, it needs 2 environment values: hostname and httptoken.
Once the 2 values are setted, its ready to work.

If you want to run the program every X days, make a scheduled task in windows.

For creating the Environmnet variables, this script could be used:

```
# Set the hostname (you can change this as needed)
$hostname = "MY_HOSTNAME"
# Set the prefix (you can change this as needed)
$prefix = "MY_APP"
# Set the token (replace with your actual token)
$token = "YOUR_ACTUAL_TOKEN_HERE"
# Create the environment variables
[Environment]::SetEnvironmentVariable("${prefix}__hostname", $hostname, "Process")
[Environment]::SetEnvironmentVariable("${prefix}__httptoken", $token, "Process")
# Verify the variables were created
Write-Host "Environment variables created:"
Write-Host "${prefix}__hostname = $([Environment]::GetEnvironmentVariable("${prefix}__hostname", "Process"))"
Write-Host "${prefix}__httptoken = $([Environment]::GetEnvironmentVariable("${prefix}__httptoken", "Process"))"

```
