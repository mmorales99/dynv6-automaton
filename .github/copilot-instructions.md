# INSTRUCTIONS

## WHAT IS PROJECT

Project is update manager for Dynv6 DynDNS provider; updates DNS Zone records.
Updates A records: (Here will add more records as project grows)

- wildcard record (`*`)

## HOW IS PROJECT

Project is windows service. Every X time updates records.
Configured from envrionment variables:

- PROJECT_NAME\_\_ZONE_NAME
- PROJECT_NAME\_\_KEY

Project only updates if ip changes. IP get from https://api.ipify.org

Only IPv4.

Project will do:

- Get zone's id from all zones response where zone's name equal to config
- Get record's id from all record for zone id
- Update `*` record

## API TO USE

DYNV6 REST API: https://dynv6.github.io/api-spec/
IPIFY: https://www.ipify.org/

## LANGUAGE

C# https://learn.microsoft.com/es-es/dotnet/csharp/language-reference/

## DEVELOPMENT STRATEGY

Follow Test driven development.
