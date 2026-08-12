# Developer documentation

Documentation for contributors to the evitaDB C# client. For user-facing documentation (installation,
quickstart, API examples) see the repository [README](../README.md) and the official
[evitaDB documentation](https://evitadb.io/documentation).

| Document | Content |
|---|---|
| [architecture.md](architecture.md) | How the driver is layered: channels, interceptors, sessions, converters, models |
| [async-api.md](async-api.md) | The async-core / sync-facade convention and streaming APIs |
| [wire-compatibility.md](wire-compatibility.md) | Protocol pinning, deprecated wire fields, scoped-field fallbacks, version gating |
| [testing.md](testing.md) | Test suite layout, fixtures, environment variables, equivalence contract, known skips |
| [upgrading-evitadb.md](upgrading-evitadb.md) | Step-by-step process for adapting the driver to a newer evitaDB version |

A machine-assisted version of the upgrade guide exists as a Claude Code skill in
[`.claude/skills/evitadb-version-upgrade/`](../.claude/skills/evitadb-version-upgrade/SKILL.md).
