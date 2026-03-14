# Threat Model – Showcase

Web-based TicTacToe: Identity, SignalR, high scores, admin. Stack: ASP.NET Core 8, SQLite, React.

## STRIDE – threats & mitigations

| Threat | Mitigation | Risk |
|--------|------------|------|
| Spoofing | Identity, secure cookies | Low |
| Tampering | EF Core, [Authorize], validation | Medium |
| Information disclosure | No stack traces (prod), CORS | Medium |
| DoS | Account lockout; TODO rate limiting | Medium |
| Elevation | RBAC, [Authorize(Roles)] | Low |

## Entry points

| Path | Auth | Risk |
|------|------|------|
| /Home, /Contact | No | Low |
| /Identity/Login | No | Medium |
| /TicTacToe, /HighScore | Yes / Admin | Low–Medium |
| SignalR /ReactTicTacToe | Verify | Medium |

## Use at design/sprint

At every design change and sprint planning: (1) Identify threats (STRIDE + entry points). (2) Plan countermeasures; track in this doc. (3) Decide risk response (accept/mitigate). (4) Use outcome to guide security testing. Update this doc when adding features or after incidents.

Ref: OWASP ASVS, Top 10.
