# Security Policy

**Report a vulnerability:** s1156843@student.windesheim.nl (description, steps, versions, mitigations). 90-day disclosure.

## Secure SDLC (C1)

Security in all stages: **Planning** – threat model, data classification, I/O requirements. **Development** – validation, trusted deps (nuget.config, .npmrc). **Build** – CodeQL, dependency checks. **Deployment** – secrets/env, HTTPS, headers. **Operations** – report above; update docs on major changes.

Details: `THREAT_MODEL.md`, `DATA-CLASSIFICATION.md`, `INPUT-OUTPUT-REQUIREMENTS.md`.

## Password policy (C6)

| Vereiste | Hoe we voldoen |
|----------|----------------|
| Geen truncatie; meerdere spaties → één toegestaan | Identity kapt wachtwoorden niet af; spaties niet aangepast. |
| Alle printable Unicode (incl. spaties, emoji) toegestaan | Geen composition rules: `RequireDigit/Lowercase/Uppercase/NonAlphanumeric` = false. |
| Gebruiker kan wachtwoord wijzigen | Manage → Password (Identity UI ChangePassword-pagina). |
| Wijzig wachtwoord vereist huidig + nieuw wachtwoord | Identity `ChangePasswordAsync` met current + new. |
| Breached-wachtwoord check (registratie/login/wijziging) | Lokale validator `BreachedPasswordValidator` met lijst veelvoorkomende wachtwoorden. |
| Geen composition rules | Zie regel 2; geen eisen voor hoofdletters/cijfers/speciale tekens. |
| Geen periodieke rotatie of wachtwoordgeschiedenis | Niet geïmplementeerd. |
| Plakken, browser-help, wachtwoordmanagers toegestaan | Geen `autocomplete="off"` op wachtwoordvelden; juiste autocomplete (current-password/new-password). |

## Unneeded features, documentation and samples (ASVS)

| Maatregel | Implementatie |
|-----------|----------------|
| Geen dev-documentatie in productie-errorpagina | Error-pagina toont instructies voor Development-modus alleen als `IWebHostEnvironment.IsDevelopment()`; in productie alleen generieke foutmelding. |
| Geen sample-/default-app branding | React-manifest: "Create React App Sample" vervangen door appnaam "Showcase". |
| Geen onnodige code in build | RunQuery-map uitgesloten van compilatie (`Compile Remove="RunQuery\**"`); alleen benodigde docs (SECURITY, THREAT_MODEL, etc.) in repo. |

## ASVS – Measurements taken (implemented)

| ASVS | Measurement taken |
|------|-------------------|
| V2.1.1 | `IdentityOptions.Password.RequiredLength = 12` (min 12 chars). |
| V2.2.1 | `lockoutOnFailure: true` on login; Identity lockout: `MaxFailedAccessAttempts = 5`, `DefaultLockoutTimeSpan = 15` min; IP rate limit (AspNetCoreRateLimit) 100/min. |
| V3.4.3 | `options.Cookie.SameSite = SameSiteMode.Lax` on application cookie. |
| V3.4.4 | In production: `options.Cookie.Name = "__Host-.AspNetCore.Cookies"`. |
| V3.4 (Secure) | In production: `CookieSecurePolicy.Always`; otherwise `SameAsRequest`. |
| V7.1.3 / V7.2.1 | Log successful login (with email); log failed login attempt (email only, no password); log lockout (email). |
| V8.3.2 | Manage → Personal data: “Download my data” (JSON export); “Delete my account and data” (DeletePersonalData page with password confirmation, then `UserManager.DeleteAsync` + sign out). |
