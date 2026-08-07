# Domain Docs

How the engineering skills should consume this repo's domain documentation when exploring the codebase.

本文规定工程技能在探索代码库时如何读取和使用本仓库的领域文档。

## Before exploring, read these

- **`CONTEXT.md`** at the repo root, or
- **`CONTEXT-MAP.md`** at the repo root if it exists — it points at one `CONTEXT.md` per context. Read each one relevant to the topic.
- **`Docs/adr/`** — read ADRs that touch the area you're about to work in. In multi-context repos, also check `src/<context>/docs/adr/` for context-scoped decisions.

开始探索前，应读取根目录的领域上下文以及与当前工作区域有关的 ADR。若存在 `CONTEXT-MAP.md`，则读取其中指向的相关上下文文档。

If any of these files don't exist, **proceed silently**. Don't flag their absence; don't suggest creating them upfront. The `/domain-modeling` skill (reached via `/grill-with-docs` and `/improve-codebase-architecture`) creates them lazily when terms or decisions actually get resolved.

如果这些文件尚不存在，**静默继续**，不要主动报告缺失或预先建议创建。只有在术语或决策真正明确后，才由 `/domain-modeling` 等工作流按需创建。

## File structure

Single-context repo (most repos):

```text
/
├── CONTEXT.md
├── Docs/adr/
│   ├── 0001-event-sourced-orders.md
│   └── 0002-postgres-for-write-model.md
└── src/
```

本仓库采用上述 **single-context（单上下文）** 布局。

Multi-context repo (presence of `CONTEXT-MAP.md` at the root):

```text
/
├── CONTEXT-MAP.md
├── Docs/adr/                          ← system-wide decisions
└── src/
    ├── ordering/
    │   ├── CONTEXT.md
    │   └── docs/adr/                  ← context-specific decisions
    └── billing/
        ├── CONTEXT.md
        └── docs/adr/
```

## Use the glossary's vocabulary

When your output names a domain concept (in an issue title, a refactor proposal, a hypothesis, a test name), use the term as defined in `CONTEXT.md`. Don't drift to synonyms the glossary explicitly avoids.

输出中出现领域概念时，应使用 `CONTEXT.md` 定义的术语，不要改用词汇表明确排除的同义词。

If the concept you need isn't in the glossary yet, that's a signal — either you're inventing language the project doesn't use (reconsider) or there's a real gap (note it for `/domain-modeling`).

如果所需概念尚未出现在词汇表中，应先判断它是未经验证的新说法，还是需要交给 `/domain-modeling` 补充的真实缺口。

## Flag ADR conflicts

If your output contradicts an existing ADR, surface it explicitly rather than silently overriding:

> _Contradicts ADR-0007 (event-sourced orders) — but worth reopening because…_

若输出与现有 ADR 冲突，必须明确指出冲突及重新讨论的理由，不得静默覆盖既有决策。
