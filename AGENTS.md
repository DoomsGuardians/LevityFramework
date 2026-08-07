## Agent skills

### Issue tracker

Issues and specs are tracked in this repository's GitHub Issues（问题与规格统一记录在本仓库的 GitHub Issues 中）. See `Docs/agents/issue-tracker.md`.

### Triage labels

Use the five default canonical triage labels（使用五个默认的标准分诊标签）. See `Docs/agents/triage-labels.md`.

### Domain docs

Use the single-context domain documentation layout（使用单上下文领域文档布局）. See `Docs/agents/domain.md`.

### Documentation lint

After changing Markdown or public C# APIs, run `powershell -NoProfile -File Tools/doc-lint.ps1`. It checks local documentation links and verifies that symbols referenced by C# examples exist in the repository's public/protected source API.
