# Threat Model - Showcase Applicatie

## Document Informatie
- **Versie**: 1.0
- **Laatste update**: 12-1-2026
- **Auteur**: Twan Kloek
- **Applicatie**: Showcase - TicTacToe
---

## 1. Applicatie Overzicht

De Showcase applicatie is een web-based multiplayer TicTacToe game met:
- Gebruikersauthenticatie (ASP.NET Core Identity)
- Real-time multiplayer gameplay via SignalR
- High score tracking
- Admin functionaliteit voor score beheer

**Technologie Stack:**
- ASP.NET Core 8.0, SQLite, SignalR, React

---

## 2. Belangrijkste Bedreigingen (STRIDE)

### Spoofing Identity
**Bedreiging**: Ongeautoriseerde toegang door gestolen credentials, session hijacking

**Mitigaties:**
- ✅ ASP.NET Core Identity met secure password hashing
- ✅ Secure session cookies
- ⚠️ **TODO**: CAPTCHA bij login (V2.7.1)

**Risico**: Medium → Laag

---

### Tampering with Data
**Bedreiging**: Manipulatie van high scores, game moves, SQL injection

**Mitigaties:**
- ✅ Entity Framework Core (SQL injection preventie)
- ✅ Server-side validatie van moves
- ✅ [Authorize] attributen op controllers
- ⚠️ **TODO**: Database encryptie at rest (V8.1.2)

**Risico**: Medium

---

### Information Disclosure
**Bedreiging**: Database bestanden toegankelijk, stack traces, gevoelige data in logs

**Mitigaties:**
- ✅ Error handling in non-dev mode (geen stack traces)
- ✅ CORS policy geconfigureerd
- ⚠️ **TODO**: Database encryptie at rest (V8.1.2)
- ⚠️ **TODO**: Content Security Policy (V11.2.1)

**Risico**: Hoog → Medium

---

### Denial of Service (DoS)
**Bedreiging**: Resource exhaustion door veel SignalR connections, brute force login

**Mitigaties:**
- ✅ Account lockout mechanisme
- ⚠️ **TODO**: Rate limiting op API endpoints (V13.1.2)
- ⚠️ **TODO**: Resource limits op game sessies (V12.3.1)

**Risico**: Medium

---

### Elevation of Privilege
**Bedreiging**: Privilege escalation naar Admin rol

**Mitigaties:**
- ✅ Role-based access control (Admin/Member)
- ✅ [Authorize(Roles = "Admin")] op admin endpoints
- ✅ Server-side autorisatie checks

**Risico**: Laag

---

## 3. Entry Points

| Entry Point | Authenticatie | Risico |
|-------------|--------------|--------|
| `/Home/Index` | Geen | Laag |
| `/Identity/Account/Login` | Geen | Medium |
| `/TicTacToe/Index` | Vereist | Laag |
| `/HighScore/Edit` | Admin | Medium |
| `/ReactTicTacToe` (SignalR) | ⚠️ Te verifiëren | Medium-Hoog |

---

## 4. Kritieke Openstaande Items

### High Priority (L1)
- [ ] V8.1.2: Database encryptie at rest
- [ ] V11.2.1: Content Security Policy (CSP)
- [ ] SignalR hub authentication verificatie

### Medium Priority
- [ ] V13.1.2: Rate limiting
- [ ] V2.7.1: CAPTCHA bij login
- [ ] V6.2.2, V6.3.1: Log security en monitoring

---

## 5. Mitigatie Status

| Threat Type | Preventie | Detectie | Response |
|-------------|-----------|----------|----------|
| Spoofing | ✅ Identity framework | ⚠️ Logging | ✅ Account lockout |
| Tampering | ✅ EF Core, Validation | ⚠️ Logging | ⚠️ TODO |
| Information Disclosure | ✅ Error handling | ⚠️ Monitoring | ⚠️ TODO |
| DoS | ⚠️ Rate limiting (TODO) | ⚠️ Monitoring | ⚠️ TODO |
| Elevation | ✅ RBAC | ⚠️ Audit logs | ✅ Access control |

---

## Referenties
- OWASP ASVS 4.0.3
- OWASP Top 10
- Zie `ASVS_OVERZICHT.md` voor volledige compliance status

---

*Dit threat model moet worden bijgewerkt bij nieuwe features of security incidents.*
