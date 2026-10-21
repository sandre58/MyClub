# Security Policy

## Supported Versions

Security fixes are provided for the **`main`** branch and the **latest tagged release**.

| Version | Supported |
| ------- | --------- |
| Latest release (`v*` tag) | Yes |
| `main` | Yes |
| Older releases | Best effort only |

## Reporting a Vulnerability

**Please do not open a public GitHub issue for security vulnerabilities.**

Report privately to: **andre.cs2i@gmail.com**

Include:

- Affected component(s) and version(s)
- Steps to reproduce or a minimal proof of concept
- Impact assessment
- Any suggested fix

### What to expect

| Step | Target timeline |
| ---- | ---------------- |
| Acknowledgement | Within 5 business days |
| Initial assessment | Within 10 business days |
| Fix or mitigation plan | Depends on severity |

## Play'Up Host (local deployment)

The Play'Up Host is intended as a **local organizer tool** (loopback bind, anonymous API if reachable). Deliberate LAN, tunnel, or Internet exposure without authentication is a **misconfiguration**, not a supported deployment model.

See [docs/guides/host-exposure.md](docs/guides/host-exposure.md) for listen URLs, risk scenarios, and development guardrails. Product decision: Notion — Play'Up Host local exposure (AUD-0008).

## Out of Scope

- Issues in dependencies already fixed in a newer release listed in `Directory.Packages.props`
- Missing features that are not demonstrated vulnerabilities
- Reports that only restate the absence of authentication on a Host instance you intentionally bound to `0.0.0.0` or exposed via port forwarding without a gateway (documented misuse in `host-exposure.md`)
