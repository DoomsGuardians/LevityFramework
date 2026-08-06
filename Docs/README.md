# LevityFramework Documentation

本页是仓库文档入口。阅读文档前先确认其状态；“目标架构”描述已经接受但尚未全部实现的方向，不等同于当前代码能力。

## 当前实现

- [`../README.md`](../README.md)：面向使用者的当前代码概览、核心 API 与扩展方式。
- [`../CONTEXT.md`](../CONTEXT.md)：领域术语及统一用语。

当前实现仍以 `GameRoot`、`Services` 服务定位器、枚举标识、`Resources` 和直接的 `NaninovelService` 集成为主。若文档与源码冲突，当前源码和测试是实现事实；架构文档是迁移目标。

## 已接受的目标架构

- [`architecture/framework-usage.md`](architecture/framework-usage.md)：框架安装、组合、生命周期、Stage、输入、UI、存档及发行方式的目标架构。
- [`architecture/narrative-module.md`](architecture/narrative-module.md)：叙事模块及 Naninovel 适配边界的目标架构。
- [`adr/0001-own-narrative-contracts-and-adapt-naninovel.md`](adr/0001-own-narrative-contracts-and-adapt-naninovel.md)：叙事契约归 Levity、Naninovel 作为默认后端的已接受决策。

这些设计中的 Composition、独立程序集、强类型 ID、Unified Save、Narrative Core/Flow/Adapter 等能力尚未在当前代码中完整落地。

## 规格与任务

规格和实施状态以本仓库的 GitHub Issues 为准：

- [Issue #1：Levity Framework Usage and Narrative Integration](https://github.com/DoomsGuardians/LevityFramework/issues/1)
- [`prd/framework-usage-prd.md`](prd/framework-usage-prd.md)：Issue #1 的本地发布快照，不作为后续状态更新入口。

不要在架构文档或本地 PRD 中维护任务完成勾选；实施进度、验收讨论和拆分任务应记录在 GitHub Issues。

## Agent 操作说明

- [`../AGENTS.md`](../AGENTS.md)：仓库级入口。
- [`agents/issue-tracker.md`](agents/issue-tracker.md)：GitHub Issues 操作约定。
- [`agents/triage-labels.md`](agents/triage-labels.md)：标准分诊标签映射。
- [`agents/domain.md`](agents/domain.md)：领域文档布局与使用规则。

这些文件面向自动化代理，不是框架用户指南。

## 第三方资料

`Assets/Plugins/**` 与 `Packages/**` 下的 README、manifest、license 和 Naninovel PDF 由第三方包拥有，应随对应依赖保留。升级或移除依赖时按供应商包整体处理，不在项目文档整理中单独改写或删除。

## 维护规则

- 当前 API 或目录变化：统一更新根 `README.md`。
- 已接受但尚未实现的结构性决策：写 ADR 或更新 architecture 文档，并明确实现状态。
- 需求、优先级和完成状态：只在 GitHub Issues 维护。
- 被替代的方案和 roadmap：确认权威替代来源后删除，避免保留第二套架构或任务清单。
- 文档中的示例必须调用仓库里真实存在的公开 API。
