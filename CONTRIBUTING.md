# 开发者贡献指南 (CONTRIBUTING)

感谢你对 **Android NAS (.NET 版)** 项目的关注！这个项目从单体 Java 应用演进为一个庞大的全平台分布式生态，离不开社区的贡献。

我们欢迎任何形式的贡献：提交 Bug、优化文档、重构代码、或者提交新功能。在提交 Pull Request 之前，请花几分钟阅读本指南。

---

## 🏗️ 一、 项目结构深度解析

在动手改代码之前，请先了解本项目的物理架构。由于采用了彻底的服务端/客户端分离，我们拥有 7 个核心工程：

```text
├── src/
│   ├── AndroidNas.Core/         # 核心跨平台逻辑库
│   │   ├── Settings/            # AppSettings 配置管理 (含 JSON 导入导出)
│   │   ├── Auth/                # 鉴权逻辑与账号管理
│   │   ├── Files/               # SafePath 路径穿越防御
│   │   └── Localization/        # 多语言字典与动态响应
│   │
│   ├── AndroidNas.Server/       # 核心服务端协议栈 (纯跨平台，零 UI)
│   │   ├── Endpoints/           # HTTP 路由与处理逻辑 (基于原生 HttpListener)
│   │   ├── Protocols/           # 手写 FTP / WebDAV / DLNA / LocalSend 协议层
│   │   ├── Tunneling/           # SSH 内网穿透与多节点自动切换
│   │   └── ServerHost.cs        # 整个 Web 引擎的启动与调度中心
│   │
│   ├── AndroidNas.App/          # 【端】安卓服务端壳 (MAUI)
│   │   ├── Platforms/Android/   # ForegroundService, WakeLock, 四层保活矩阵
│   │   ├── Pages/               # 驾驶舱 UI (文件、设置、日志、终端等)
│   │   └── Resources/           # MD3 样式、图标
│   │
│   ├── AndroidNas.Client/       # 【端】安卓客户端壳 (MAUI)
│   │   ├── Pages/               # 设备发现、文件浏览、投屏控制
│   │   └── Resources/           # 客户端专属 UI
│   │
│   ├── AndroidNas.TV/           # 【端】电视端 (纯原生 Android C#, API 22+)
│   │   ├── MainActivity.cs      # UDP 投屏接收、原生 ListView 文件流
│   │   └── Resources/layout/    # 电视大屏 XML 布局
│   │
│   ├── AndroidNas.Watch/        # 【端】手表端 (纯原生 Android C#, API 22+)
│   │   ├── MainActivity.cs      # 紧凑型界面逻辑
│   │   └── Resources/layout/    # 圆盘适配 XML 布局
│   │
│   ├── AndroidNas.DesktopServer/# 【端】PC 服务端 (控制台拉取浏览器 WebUI)
│   │
│   └── AndroidNas.DesktopClient/# 【端】PC 客户端 (极客 CLI 交互式终端)
```

---

## 💻 二、 本地开发环境搭建

为了保证多端编译顺利，你的开发机需要准备以下环境：

### 1. 核心工具链
- **.NET SDK 10.0**：必须是 10.0 及以上版本。
- **JDK 17**：编译 Android 端必需。
- **Android SDK**：需安装 Android API 35 SDK 与 Build-Tools。

### 2. 安装工作负载 (Workloads)
首次克隆代码后，请务必执行以下命令安装 MAUI 和 Android 编译依赖：
```bash
dotnet workload install android maui-android
```

### 3. 编译注意事项（防踩坑）
- **RID 穿透问题**：本项目大量使用了多目标框架（`net10.0;net10.0-android`）。当你在 PC 上测试编译 `DesktopServer` 时，如果加了 `-r win-x64` 参数，请务必同时加上 `-p:TargetFrameworks=net10.0`，否则 MSBuild 会尝试拉取不存在的 Android 宿主机包导致报错。
- **AOT 与 Trim 冲突**：TV 端和 Watch 端移除了显式的 AOT 配置，采用了最基础的默认编译，请勿在它们的 `.csproj` 中强制开启 `RunAOTCompilation` 或关闭 `PublishTrimmed`，否则会导致 CI 失败。
- **包名后缀**：所有 Android 端均配置了动态包名。在 `Debug` 环境下，包名会自动加上 `.debug` 后缀（如 `com.androidnas.next.server.debug`），这允许你同时安装正式版和开发版。

---

## 🚀 三、 CI/CD 与自动化发版机制

本项目配置了极高规格的 GitHub Actions 流水线，采用**双轨制**：

### 1. 日常开发轨道 (Matrix 并行)
当你向 `main` 分支推送代码，或发起 Pull Request 时，CI 会触发。
- **并行策略**：采用 `matrix` 策略，瞬间拉起 4 个 Ubuntu 环境，同时编译 App、Client、TV、Watch 四个安卓端。
- **PC 端构建**：同时在 Linux 和 Windows 环境下编译 `DesktopServer` 和 `DesktopClient`。
- **产物下载**：编译完成后，你可以去 Actions 页面的底部，分别下载各个端的 `Artifacts` 压缩包用于真机测试。

### 2. 正式发版轨道 (自动聚合 Release)
当你需要发布新版本时：
1. 修改 `CHANGELOG.md`，写入新版本的更新日志。
2. 在 GitHub 仓库中创建一个以 `v` 开头的 Tag（例如 `v4.1.0`）并推送。
3. **全自动魔法**：CI 在编译完 6 个端之后，会额外触发 `release-all` Job。它会下载所有端的产物，将其重命名规范，打包成一个名为 `AndroidNas-Full-Release.zip` 的超级压缩包，并自动发布到 GitHub Releases 页面。

---

## 📝 四、 代码提交规范 (Conventional Commits)

为了保证版本历史的清晰可读，请严格遵循以下 Commit Message 前缀：

- `feat:` 新增功能 (例如：`feat: 为 TV 端增加焦点放大动画`)
- `fix:` 修复 Bug (例如：`fix: 解决 Android 14 前台服务崩溃问题`)
- `docs:` 仅修改文档 (例如：`docs: 补全 API 文档中的投屏协议`)
- `ci:` 修改 CI/CD 或脚本 (例如：`ci: 增加 Windows 端单文件构建任务`)
- `refactor:` 代码重构，不改变业务逻辑 (例如：`refactor: 剥离 Kestrel 依赖`)
- `chore:` 杂项修改，如更新 `.gitignore` 或依赖库版本。

---

## 🤖 五、 关于 AI 辅助开发

**本项目不排斥任何 AI 生成的代码**。事实上，本项目的骨架和大部分业务逻辑都是由大语言模型生成的。

我们鼓励你这样参与开发：
1. **利用 AI 阅读代码**：在提出 Issue 或修改代码前，你可以把相关源码发给 GPT/Claude，让它解释逻辑。
2. **利用 AI 编写代码**：你可以直接把需求丢给 AI 生成 PR。但请务必保证：
   - 本地能成功 `dotnet build` 通过。
   - 不要提交含有大量无法解释的冗余代码。
   - 如果发现是 AI 幻觉导致的错误 API 调用，请在提交前剔除。
3. **保持开源精神**：如果你在开发中遇到反直觉的底层 Bug（如我们遇到的 `Mono.linux-x64` 缺失），欢迎在 Issue 中分享排查过程，这比单纯贴代码更有价值。

---

## 🐛 六、 Issue 提交规范

在提交 Issue 时，请提供以下信息，否则我们可能无法及时响应：
- **复现步骤**：具体在哪个端（如 TV 端）、什么系统（如 Android 5.1）、点击了什么。
- **错误日志**：如果是编译报错，请提供完整的 Actions 日志链接；如果是运行时闪退，请通过 `adb logcat` 抓取崩溃堆栈。
- **网络环境**：是否开启了 Root？是否开启了 HTTPS？是否在内网？

再次感谢你的阅读与贡献！