# Input / output requirements

Handling by type and content; aligned with [DATA-CLASSIFICATION.md](DATA-CLASSIFICATION.md) and [Privacy Policy](Views/Home/Privacy.cshtml). GDPR: consent, purpose, minimal data, retention per policy.

| Data | Input | Processing | Output |
|------|--------|------------|--------|
| **Contact form** (name, email, phone, subject, message) | Type/length/pattern in UI; consent required. | To Web3Forms; not in app DB. | Not shown in UI. |
| **Identity** (email, password, name, 2FA) | Identity validation; password hashed. | Auth DB. | Only to account holder; password never. |
| **Game/high scores** | App-validated. | TicTacToe DB. | To participants / leaderboard. |
| **Session cookie** | Server-set only. | HttpOnly, Secure, encrypted. | Never in URL or script. |
| **Consent** (GDPR choice) | Accept/Decline. | localStorage. | Footer status only. |

Invalid input rejected. Output only as above. Update this and DATA-CLASSIFICATION when adding flows.
