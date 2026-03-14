# Stappenplan: security-portfolio inleverversie (eind morgen)

Dit plan is opgezet om **eind morgen** een inleverbare versie te hebben. Volg de stappen in volgorde; prioriteit = impact voor de beoordeling.

---

## Overzicht bestanden

| Bestand | Wat |
|---------|-----|
| `ASVS-nog-te-doen.csv` | Alle 242 ASVS-regels waar "Measures taken" nog leeg is |
| `ASVS-nog-te-doen-zonder-deleted.csv` | Zelfde, maar zonder [DELETED]-regels (234 regels) |

Voor een voldoende hoef je **niet** alle 234 in te vullen. Kies: **implementeren** (in code), **documenteren** (in portfolio/ASVS), of **N/A** (niet van toepassing + korte motivatie).

---

## Fase 1 – Code (vandaag, ~2–3 uur)

### Stap 1.1: Chat-sanitization (XSS) – moet echt in de app

**Doel:** V5.2.1, V5.2.2, V5.3.3 (untrusted input sanitizen, output encoding).

1. Open `Hubs/TicTacToeHub.cs`.
2. In `SendMessage(string msg)`:
   - **Max lengte:** bv. `if (string.IsNullOrWhiteSpace(msg) || msg.Length > 500) return;`
   - **Sanitize:** gebruik `System.Net.WebUtility.HtmlEncode(msg)` voordat je `msg` doorgeeft aan `SendAsync`. Dan kan geen HTML/script meer in de chat.
3. Optioneel: `username` in `JoinSpecificTicTacToeGameRoom` ook beperken (lengte + alleen veilige tekens).
4. Test: in de chat iets als `<script>alert(1)</script>` of `<iframe src="javascript:alert(1)">` typen → moet als platte tekst getoond worden.
5. In je ASVS-CSV: bij V5.2.1, V5.2.2, V5.3.3 in "Measures taken" invullen: *"SignalR chat: HtmlEncode op berichten, max 500 tekens; username gevalideerd."*

**Tijd:** ~30 min.

---

### Stap 1.2: Rate limiting (eenvoudigste optie)

**Doel:** V2.2.1, V8.1.4, V11.1.4 (anti-automation / abnormale aantallen requests).

**Makkelijkste:** middleware die per IP (of per user indien ingelogd) het aantal requests per minuut beperkt.

1. Package: `AspNetCoreRateLimit` (NuGet).
2. In `Program.cs`:
   - `services.AddMemoryCache()` en `services.AddInMemoryRateLimiting()`; configuratie met bv. max 100 requests per minuut per IP.
   - `app.UseIpRateLimiting()` (na UseRouting, voor UseAuthentication).
3. Documenteer in portfolio: "Rate limiting via AspNetCoreRateLimit, 100 req/min per IP."
4. ASVS: bij V2.2.1, V8.1.4, V11.1.4 in "Measures taken": *"AspNetCoreRateLimit middleware, 100 req/min per IP."*

**Tijd:** ~45 min (incl. testen).

Als je geen extra package wilt: alleen **documenteren** in portfolio + ASVS: "Geen rate limiting geïmplementeerd; Identity account lockout beschermt login; voor productie wordt rate limiting aanbevolen."

---

### Stap 1.3: Input-validatie op parameters (allowlist waar mogelijk)

**Doel:** V5.1.3 (allowlist voor input).

1. **SignalR:** `roomCode` en `username` in `JoinSpecificTicTacToeGameRoom`: max lengte (bv. 20), alleen alfanumeriek + koppelteken:  
   `Regex.IsMatch(roomCode, @"^[a-zA-Z0-9\-]{1,20}$")` → anders return.
2. **Controllers:** als je ergens een `sortOrder` of andere query-parameter hebt: alleen toegestane waarden accepteren (allowlist), anders default of 400.
3. ASVS V5.1.3: *"Allowlist voor roomCode/username (regex); queryparams gevalideerd."*

**Tijd:** ~20 min.

---

## Fase 2 – Tests (vandaag, ~1–1,5 uur)

### Stap 2.1: Playwright opzetten

1. In de solution (of in de map van de frontend/React):  
   `npm init -y` (als nog geen package.json)  
   `npm install -D @playwright/test`
2. `npx playwright install` (browsers).
3. Maak map `e2e` of `tests/e2e` en een config: `playwright.config.ts` (baseURL bv. `http://localhost:5000` of je dev-URL).

**Tijd:** ~15 min.

---

### Stap 2.2: Eén E2E-test voor security headers

1. Nieuwe test: bv. `e2e/security-headers.spec.ts`.
2. Test: pagina ophalen (bv. `/` of `/Home`), response headers uitlezen.
3. Assert:  
   - `x-frame-options` of `content-security-policy` aanwezig  
   - `x-content-type-options: nosniff`  
   (Playwright: `page.goto()` en dan via `page.request.fetch()` of via `response.headers()` de headers checken.)
4. In portfolio: "Security headers gecontroleerd met Playwright E2E-test."

**Tijd:** ~30 min.

---

### Stap 2.3: CI (GitHub Actions) voor build + tests

1. Als je nog geen workflow hebt: `.github/workflows/build-and-test.yml`.
2. Stappen: checkout → .NET restore + build → (optioneel) `dotnet test` als je unit tests hebt → (optioneel) npm install + Playwright test.
3. Portfolio: "DTAP Testing: GitHub Actions voert build en Playwright E2E-tests uit."

**Tijd:** ~30 min (kunnen ook alleen build zijn als Playwright niet op de runner draait).

---

## Fase 3 – ASVS & portfolio (morgen, ~2–3 uur)

### Stap 3.1: ASVS-lijst afronden voor inleveren

1. Open `ASVS-nog-te-doen-zonder-deleted.csv` (of je originele ASVS Excel).
2. Per regel kiezen:
   - **Implementeren** → je hebt stap 1 gedaan: invullen bij "Measures taken" (zoals bij 1.1, 1.2, 1.3).
   - **N/A** → invullen: "N/A – geen file upload / geen federated login / etc." (kort).
   - **Documenteren** → bv. "Identity default gedrag; zie SECURITY.md."
3. Richtlijn voor een voldoende: **alle Level-1 (V) items** die op jouw app van toepassing zijn, hebben ofwel een maatregel ofwel N/A. De rest mag je weglaten of N/A doen als ze niet van toepassing zijn.
4. Sla de bijgewerkte ASVS op (CSV/Excel) en voeg die toe als bijlage bij je portfolio.

**Tijd:** ~1–1,5 uur (niet alles tot op de letter; focus op V1, V2, V4, V5, V7, V8, V14).

---

### Stap 3.2: Portfolio-tekst bijwerken

Per rubric-criterium kort bijwerken:

1. **DTAP Testing**  
   - Verwijder "Moet nog gedaan worden!".  
   - Beschrijf: Playwright (of andere) E2E-tests, waar ze op draaien (lokaal/CI), en dat security headers getest worden.

2. **Secure componenten en interfaces**  
   - Welke maatregelen je hebt (chat-sanitization, rate limiting, headers, CSP, validatie).  
   - Hoe je test: Playwright voor headers; handmatig of unit test voor sanitization.

3. **Acceptatie webapplicatie**  
   - Korte structurele analyse: welke maatregelen, waar getest.  
   - Eigen aanvallen: bv. XSS in chat (voor/na sanitization), eventueel ZAP-resultaten.  
   - Advies: wat is goed, wat zou je nog doen (bijv. uitgebreidere rate limiting in productie).

4. **Ethical hacking**  
   - Eén alinea: ethische kaders (toestemming, geen echte schade, melden bij anderen).

**Tijd:** ~1 uur.

---

### Stap 3.3: Bijlagen controleren

- [ ] Ingevulde ASVS-lijst (Excel/CSV) toegevoegd.
- [ ] Testrapportage: welke tests, resultaat (geslaagd/fout), hoe uitgevoerd (lokaal/CI).
- [ ] ZAP: screenshots/rapport blijven staan zoals je al had.

---

## Volgorde aanbevolen (eind morgen inleveren)

| Moment | Stap | Geschatte tijd |
|--------|------|-----------------|
| **Vandaag** | 1.1 Chat-sanitization | 30 min |
| | 1.2 Rate limiting (of alleen documenteren) | 45 min |
| | 1.3 Input-validatie (roomCode, username, params) | 20 min |
| | 2.1 Playwright opzetten | 15 min |
| | 2.2 E2E-test security headers | 30 min |
| | 2.3 CI workflow (build + optioneel tests) | 30 min |
| **Morgen** | 3.1 ASVS "Measures taken" / N/A invullen | 1–1,5 uur |
| | 3.2 Portfolio-tekst (DTAP, componenten, acceptatie, ethiek) | 1 uur |
| | 3.3 Bijlagen nalopen | 15 min |

Totaal ruwweg **5–6 uur** werk. Als je rate limiting overslaat en alleen documenteert, bespaar je ~45 min.

---

## Minimale inleverversie (als tijd te kort is)

Als je echt moet snijden:

1. **Verplicht:** Stap 1.1 (chat-sanitization) + 3.1 (ASVS voor de items die je hebt aangepakt) + 3.2 (portfolio bijwerken).
2. **Sterk aan te raden:** Stap 2.2 (één Playwright header-test) + 3.3 (bijlagen).
3. **Optioneel voor voldoende:** Rate limiting (1.2), uitgebreide CI (2.3), rest van ASVS tot op de letter.

Succes met inleveren.
