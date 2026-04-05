# Security Policy

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| Latest  | :white_check_mark: |

## Reporting a Vulnerability

If you discover a security vulnerability in this project, please report it responsibly.

**Do not open a public GitHub issue for security vulnerabilities.**

Instead, please email security@intodayshighlight.com with:

- A description of the vulnerability
- Steps to reproduce the issue
- Any relevant logs or screenshots

We will acknowledge your email within 48 hours and provide an estimated timeline for a fix.

## Security Considerations

This library processes arbitrary DOCX files. When using this library:

- **Validate input:** Only process DOCX files from trusted sources, or run in a sandboxed environment
- **Resource limits:** Large or malicious DOCX files may consume excessive memory or CPU; consider timeouts and memory limits
- **Font loading:** The library loads font files from configured directories; ensure these directories are trusted
- **Embedded content:** DOCX files may contain embedded images, OLE objects, and hyperlinks; the library renders these but does not execute any embedded code
