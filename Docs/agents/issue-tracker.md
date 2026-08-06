# Issue tracker: GitHub

Issues and specs for this repo live as GitHub issues. Use the `gh` CLI for all operations.

本仓库的问题与规格均存放在 GitHub Issues 中，所有操作使用 `gh` CLI 完成。

## Conventions

- **Create an issue / 创建问题**: `gh issue create --title "..." --body "..."`. Use a heredoc for multi-line bodies.
- **Read an issue / 读取问题**: `gh issue view <number> --comments`, filtering comments by `jq` and also fetching labels.
- **List issues / 列出问题**: `gh issue list --state open --json number,title,body,labels,comments --jq '[.[] | {number, title, body, labels: [.labels[].name], comments: [.comments[].body]}]'` with appropriate `--label` and `--state` filters.
- **Comment on an issue / 评论问题**: `gh issue comment <number> --body "..."`
- **Apply / remove labels / 添加或移除标签**: `gh issue edit <number> --add-label "..."` / `--remove-label "..."`
- **Close / 关闭问题**: `gh issue close <number> --comment "..."`

Infer the repo from `git remote -v` — `gh` does this automatically when run inside a clone.

仓库由 `git remote -v` 推断；在克隆目录中运行时，`gh` 会自动识别当前仓库。

## Pull requests as a triage surface

**PRs as a request surface: no.** _(Set to `yes` if this repo treats external PRs as feature requests; `/triage` reads this flag.)_

当前不将 PR 作为分诊请求入口。只有当外部 PR 也被视为功能请求时，才将上面的值改为 `yes`。

When set to `yes`, PRs run through the same labels and states as issues, using the `gh pr` equivalents:

- **Read a PR**: `gh pr view <number> --comments` and `gh pr diff <number>` for the diff.
- **List external PRs for triage**: `gh pr list --state open --json number,title,body,labels,author,authorAssociation,comments` then keep only `authorAssociation` of `CONTRIBUTOR`, `FIRST_TIME_CONTRIBUTOR`, or `NONE` (drop `OWNER`/`MEMBER`/`COLLABORATOR`).
- **Comment / label / close**: `gh pr comment`, `gh pr edit --add-label`/`--remove-label`, `gh pr close`.

GitHub shares one number space across issues and PRs, so a bare `#42` may be either — resolve with `gh pr view 42` and fall back to `gh issue view 42`.

GitHub 的 Issue 与 PR 共用编号空间，因此 `#42` 可能指任意一种；先尝试读取 PR，失败后再读取 Issue。

## When a skill says "publish to the issue tracker"

Create a GitHub issue.

即：创建一个 GitHub Issue。

## When a skill says "fetch the relevant ticket"

Run `gh issue view <number> --comments`.

即：读取对应 Issue 及其评论。

## Wayfinding operations

Used by `/wayfinder`. The **map** is a single issue with **child** issues as tickets.

供 `/wayfinder` 使用：**map** 是一个总 Issue，具体工作项以其 **child issues** 表示。

- **Map**: a single issue labelled `wayfinder:map`, holding the Notes / Decisions-so-far / Fog body. `gh issue create --label wayfinder:map`.
- **Child ticket**: an issue linked to the map as a GitHub sub-issue (`gh api` on the sub-issues endpoint). Where sub-issues aren't enabled, add the child to a task list in the map body and put `Part of #<map>` at the top of the child body. Labels: `wayfinder:<type>` (`research`/`prototype`/`grilling`/`task`). Once claimed, the ticket is assigned to the driving dev.
- **Blocking**: GitHub's **native issue dependencies** — the canonical, UI-visible representation. Add an edge with `gh api --method POST repos/<owner>/<repo>/issues/<child>/dependencies/blocked_by -F issue_id=<blocker-db-id>`, where `<blocker-db-id>` is the blocker's numeric **database id** (`gh api repos/<owner>/<repo>/issues/<n> --jq .id`, _not_ the `#number` or `node_id`). GitHub reports `issue_dependencies_summary.blocked_by` (open blockers only — the live gate). Where dependencies aren't available, fall back to a `Blocked by: #<n>, #<n>` line at the top of the child body. A ticket is unblocked when every blocker is closed.
- **Frontier query**: list the map's open children (`gh issue list --state open`, scoped to the map's sub-issues / task list), drop any with an open blocker (`issue_dependencies_summary.blocked_by > 0`, or an open issue in the `Blocked by` line) or an assignee; first in map order wins.
- **Claim**: `gh issue edit <n> --add-assignee @me` — the session's first write.
- **Resolve**: `gh issue comment <n> --body "<answer>"`, then `gh issue close <n>`, then append a context pointer (gist + link) to the map's Decisions-so-far.

简要规则：从 map 中选择未分配且未被阻塞的首个子任务，认领后再开始写操作；解决后评论、关闭，并把上下文链接补回 map。
