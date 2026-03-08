# Security Policy

## Reporting a Vulnerability
To report a security issue, please email s1156843@student.windesheim.nl
with a description of the issue, the steps you took to create the issue, affected versions, and, if known, mitigations for the issue. 
This project follows a 90 day disclosure timeline.

## Security Overview

This document provides a high-level overview of security practices for the Showcase application. For detailed security requirements, see:
- `THREAT_MODEL.md` - Threat modeling and risk analysis
- `ASVS_OVERZICHT.md` - OWASP ASVS compliance status

### Security Principles
- **Defense in Depth**: Multiple layers of security controls
- **Least Privilege**: Users and services have minimum required permissions
- **Secure by Default**: Secure configurations are the default
- **Fail Securely**: Errors don't expose sensitive information

### Key Security Features
- ✅ ASP.NET Core Identity for authentication
- ✅ Role-based access control (Admin/Member)
- ✅ Entity Framework Core for SQL injection prevention
- ✅ HTTPS/TLS enforcement
- ✅ Secure session management
- ✅ Input validation and output encoding

### Known Security Considerations
See `ASVS_OVERZICHT.md` for items that still need implementation, including:
- Content Security Policy (CSP)
- Database encryption at rest
- Rate limiting
- Enhanced logging and monitoring