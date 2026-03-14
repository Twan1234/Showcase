# Data classification

Sensitive data identified and classified. Handle per level below.

| Level | Description | In this app |
|-------|-------------|-------------|
| **Confidential** | No repo/logs. | Connection strings, User Secrets, API keys. |
| **Personal (PII)** | Consent; minimal retention. | Identity, contact form, session cookie. |
| **Internal** | Auth; not public. | Game sessions, high scores. |
| **Public** | Normal security. | Static content, consent choice (localStorage). |

**Procedures:** Confidential → secrets/env only. Personal → Privacy Policy, consent. Internal → access control. Public → HTTPS etc. Update when adding data types.
