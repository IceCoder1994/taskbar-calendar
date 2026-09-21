# 贡献指南（Contributing）

感谢你有兴趣为「任务栏日历」做出贡献！

## 报告问题

- 通过 [Issues](https://github.com/IceCoder1994/taskbar-calendar/issues) 报告 Bug 或提出建议；
- 报告 Bug 时请附上：Windows 版本、复现步骤、日志文件（托盘菜单 → 打开日志目录）。

## 提交代码（Fork + Pull Request）

本仓库采用 GitHub 标准的 Fork + PR 协作流程，**无需写权限**即可贡献代码。

### 1. Fork 仓库

打开 https://github.com/IceCoder1994/taskbar-calendar ，点击右上角 **Fork** → **Create fork**。

完成后你会得到自己的副本：`https://github.com/<你的用户名>/taskbar-calendar`

### 2. 克隆自己的副本

```powershell
git clone https://github.com/<你的用户名>/taskbar-calendar.git
cd taskbar-calendar
git config user.name "<你的用户名>"
git config user.email "<你的GitHub邮箱>"
```

> **注意**：必须克隆**自己账号**下的副本（不要克隆原仓库），否则推送时会报 403 权限错误。
> 如果之前克隆的是原仓库，可修改远程地址后再推送：
>
> ```powershell
> git remote set-url origin https://github.com/<你的用户名>/taskbar-calendar.git
> git push
> ```

### 3. 开发与验证

需要 [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)：

```powershell
dotnet build TaskbarCalendar.sln -c Debug   # 构建
dotnet run --project src\TaskbarCalendar    # 运行调试（托盘驻留）
```

- 提交前请确保构建通过、并实际运行验证过功能；
- 项目结构与开发约定详见 [AGENTS.md](AGENTS.md)。

### 4. 提交并推送

```powershell
git add -A
git commit -m "fix: 修复某某问题"    # Angular 规范 + 中文
git push
```

### 5. 发起 Pull Request

推送后打开自己的仓库页面，点击 **Contribute → Open pull request**，
填写变更说明后点击 **Create pull request**，等待维护者审核合并。

### 保持与上游同步（可选）

原仓库更新后，在自己仓库的页面点击 **Sync fork → Update branch** 即可同步最新代码。

## 提交规范

- 提交信息遵循 Angular 规范 + 中文描述：
  - `feat: 新功能`
  - `fix: 修复问题`
  - `docs: 文档变更`
  - `chore: 构建 / 杂项`
- **所有代码注释必须使用中文**；
- 不要提交构建产物（`bin/`、`obj/`、`publish/`、`dist*/` 已在 `.gitignore` 中排除）。

## 行为准则

请保持友善与尊重，共同维护良好的交流氛围。
