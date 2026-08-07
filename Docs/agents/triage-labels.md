# Triage Labels

The skills speak in terms of five canonical triage roles. This file maps those roles to the actual label strings used in this repo's issue tracker.

技能使用五种标准分诊角色；本文件将这些角色映射到仓库 Issue Tracker 中的实际标签名称。

| Label in mattpocock/skills | Label in our tracker | Meaning / 含义                         |
| -------------------------- | -------------------- | -------------------------------------- |
| `needs-triage`             | `needs-triage`       | Maintainer needs to evaluate / 待维护者评估 |
| `needs-info`               | `needs-info`         | Waiting for more information / 等待补充信息 |
| `ready-for-agent`          | `ready-for-agent`    | Fully specified, ready for an agent / 已明确，可由代理处理 |
| `ready-for-human`          | `ready-for-human`    | Requires human implementation / 需要人工实现 |
| `wontfix`                  | `wontfix`            | Will not be actioned / 不予处理        |

When a skill mentions a role (e.g. "apply the AFK-ready triage label"), use the corresponding label string from this table.

当技能提到某个分诊角色时，应使用表中对应的实际标签字符串。

`ready-for-human` means the issue is sufficiently specified to begin, but completion
requires human judgement, hands-on evaluation, credentials, or another interaction
that an unattended agent cannot perform. Do not also apply `ready-for-agent`.

`ready-for-human` 表示问题已经足够明确，可以开始执行，但其完成需要人工判断、实机体验、
凭据或其他无法由无人值守代理完成的交互。不要同时添加 `ready-for-agent`。

## Wayfinder type labels

Wayfinder type labels describe the kind of work in a ticket. They are independent
from the triage labels above, which describe who can act on the ticket and whether
it is ready.

Wayfinder 类型标签描述工单中的工作种类；它们与上面的分诊标签相互独立。分诊标签负责说明
由谁执行以及是否已可开始。

| Label | Meaning / 含义 |
| ----- | -------------- |
| `wayfinder:research` | Gather evidence and produce a durable finding or decision; use `ready-for-human` when the research requires hands-on human evaluation. / 收集证据并形成可留存的结论或决策；若研究需要人工实机评估，则同时使用 `ready-for-human`。 |
| `wayfinder:prototype` | Build a disposable experiment to answer a bounded design question; production implementation is a follow-up ticket after the result is accepted. / 构建一次性实验以回答有边界的设计问题；结论被接受后，再用后续工单实现生产版本。 |

Edit the right-hand column to match whatever vocabulary you actually use.

如果仓库以后改用其他标签命名，只需修改右侧映射列。
