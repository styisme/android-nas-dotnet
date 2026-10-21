# Android NAS (.NET 10 / MAUI)

[![Platform](https://img.shields.io/badge/Platform-Android%20%7C%20Windows%20%7C%20Linux-brightgreen.svg)]()
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)]()
[![MAUI](https://img.shields.io/badge/UI-.NET%20MAUI-blue.svg)]()
[![API Compatible](https://img.shields.io/badge/API-v3.x%20compatible-success.svg)]()
[![License](https://img.shields.io/badge/License-MIT-orange.svg)](LICENSE)
[![Status](https://img.shields.io/badge/Status-v4.1.0--Stable-blue.svg)]()

> **把吃灰的 Android 手机变成全平台私有云控制中枢。**
> 
> 本项目是原 [android-nas](https://github.com/styisme/android-nas) (Java 版) 的 **.NET 10 / MAUI 全平台终极重构版**。我们不仅保留了原版极度硬核的底层局域网协议，更在此基础上完成了服务端与客户端的彻底解耦。现在的它，不再是一个臃肿的单体 APK，而是一个包含了安卓服务端、安卓客户端、电视端、手表端、PC 服务端与极客 CLI 终端的**六端分布式生态闭环**。

---

## 📚 全局文档矩阵 (Documentation)

本项目拥有极其详尽的文档体系，无论你是小白还是极客，都能找到你需要的说明书：

- 📖 **[终极使用与运维指南 (docs/USAGE.md)](docs/USAGE.md)**：
  从零部署、网络映射（WebDAV/FTP）、跨端协同配置、CLI 终端指令大全以及深度故障排查。这是你上手 NAS 的第一站。
- 🔌 **[开发者 API 接口规范 (docs/API_DOC.md)](docs/API_DOC.md)**：
  包含服务端 8080/8443 端口的所有 HTTP API 细节（上传、下载、流媒体、鉴权）、WebDAV 底层协议规范、跨端协同 UDP 协议以及特殊端口分配。
- 🏗️ **[底层架构深度解析 (docs/ARCHITECTURE.md)](docs/ARCHITECTURE.md)**：
  深入剖析纯原生 `HttpListener` 引擎、多目标编译原理、全盘突破机制以及由无声音频、AlarmManager 和 Root 注入构成的四层保活矩阵。
- 🛡️ **[安全策略与高危警告 (SECURITY.md)](SECURITY.md)**：
  **（高危必读）** 使用全盘模式、Web Shell 和公网穿透前，请务必阅读此文档，了解明文传输风险与防变砖守则。
- 🤝 **[开发者贡献指南 (CONTRIBUTING.md)](CONTRIBUTING.md)**：
  AI 生成代码的声明、深度项目结构图、JDK 编译环境隔离说明、代码提交规范（Conventional Commits）。
- 🤖 **[AI 生成声明 (docs/AI_DISCLOSURE.md)](docs/AI_DISCLOSURE.md)**：
  关于本项目 99% 代码由大模型生成的真实开发流程与局限性声明。
- 📝 **[版本更新日志 (CHANGELOG.md)](CHANGELOG.md)**：
  追溯项目从 v4.0.0-preview 到 v4.1.0 的完整演进史。

---

## 🚨 极度重要：关于下载与发版机制

由于本项目采用了极高规格的跨平台 Matrix 矩阵编译（同时编译 4 个安卓端 + 2 个 PC 端），全量编译极其耗时。**请直接前往 GitHub Actions 页面下载最新构建产物！**

👉 **[点此进入 Actions 获取最新版构建产物](https://github.com/styisme/android-nas-dotnet/actions)**
*(进入页面后，选择顶部最新一次的绿色构建记录，划到页面最底部，下载对应的 `Artifacts` 压缩包即可)*

> **💡 极客一键全量发版模式**：
> 本项目配置了双轨制 CI/CD：
> 1. **日常改代码**：推送到 `main` 分支，会触发 Matrix 并行流水线，瞬间拉起 4 个环境同时编译安卓端，外加 2 个 PC 环境编译服务端/客户端。
> 2. **全量发版**：每次修改根目录 `CHANGELOG.md` 并打上 `v*` 格式的 Tag（如 `v4.1.0`）并推送，CI 会自动拉取**所有 6 个端**的编译产物，最终打成一个名为 `AndroidNas-Full-Release.zip` 的单一压缩包，并自动发布到 Releases 页面，真正的“一键发版”。

---

## 🌟 为什么选择本项目？

市面上的手机 NAS 软件往往存在各种妥协：要么底层协议残缺，要么 UI 落后，或者把你绑死在一个 App 里。本项目专为极客打造，具备以下碾压级优势：

1. **六端彻底解耦，无缝流转**：服务端与客户端完全分离。闲置手机装服务端，主力机装客户端，电视装 TV 端，手表装 Watch 端，PC 跑命令行。彻底打破生态壁垒。
2. **原生 HttpListener 极速引擎**：为了在老旧设备上实现极低内存占用，我们抛弃了臃肿的 ASP.NET Core (Kestrel)，纯手写原生的 HttpListener 驱动 Web 服务，完美兼容 Android 5.1 (API 22) 到 Android 14。
3. **多协议制霸**：原生实现 WebDAV、FTP、LocalSend、DLNA。在开启 Root 全盘模式后，这些协议可直接穿透 Android 沙盒访问 `/data` 与 `/system`。
4. **极客特权（Root/ADB）**：支持 OOM 免死注入、静音音频保活、Web Shell 终端。可以直接在网页端或 PC 命令行修改端口、切换密码与启停服务。
5. **现代化规范与共存**：全套端采用 `com.androidnas.next.*` 命名规范，所有 Android 端均原生支持 `.debug` 后缀，开发版与正式版完美共存。

---

## 🏗️ 架构矩阵：六端配置一览

本项目的所有 Android 端均支持最低 API 22，PC 端以单文件形式发布（免安装 .NET 环境）。

| 端 | 产物工程 | 技术栈 | 最低支持 | 包名 (Release / Debug) |
|---|---|---|---|---|
| **安卓服务端** | `AndroidNas.App` | MAUI + HttpListener | Android 8.0 (API 26) | `com.androidnas.next.server` / `.debug` |
| **安卓客户端** | `AndroidNas.Client` | MAUI | Android 8.0 (API 26) | `com.androidnas.next.client` / `.debug` |
| **电视端** | `AndroidNas.TV` | 原生 Android (C#) | Android 5.1 (API 22) | `com.androidnas.next.tv` / `.debug` |
| **手表端** | `AndroidNas.Watch` | 原生 Android (C#) | Android 5.1 (API 22) | `com.androidnas.next.watch` / `.debug` |
| **电脑服务端** | `DesktopServer` | .NET 10 Console + WebUI | Win / Linux | - |
| **电脑客户端** | `DesktopClient` | .NET 10 CLI (TUI) | Win / Linux | - |

---

## ✨ 各端详细功能矩阵

### 📱 服务端 (Server - 装在闲置手机或 PC)
- **多端口并发调度**：底层开启多个线程池统一调度。
  - `8080`：Web 网页管理 / JSON API / 流媒体分片。
  - `8443`：自签 HTTPS 加密服务（与 HTTP 并存）。
  - `8081`：标准 WebDAV 协议（纯 Socket 手写实现，非第三方库）。
  - `2121`：原生 FTP 服务（支持 PASV 被动模式）。
  - `53317`：LocalSend 接收端（免装 App 互传）。
  - `8090`：跨端协同发现与投屏指令服务（UDP）。
- **全盘 Root 模式**：一键突破 Android 沙盒，直接浏览修改底层文件，并在网页端自带 Web Shell 控制台（内置 `rm -rf` 等高危指令拦截）。
- **无敌四层保活矩阵**：
  - `Layer 1`：`ForegroundService` + 常驻通知 + `START_STICKY`。
  - `Layer 2`：`WakeLock` (CPU锁) 与 `WifiManager` (Wi-Fi锁) 持续持有。
  - `Layer 3`：`SilentPlayer` 循环写入 0 字节 PCM，伪装音乐播放；`OnePixelActivity` 息屏占位。
  - `Layer 4`：Root 注入 `/proc/<pid>/oom_score_adj=-1000` 获得免死金牌，并强杀系统级杀后台进程。
- **极寒省电模式**：一键开启后，系统自动降级 Wi-Fi 锁频率，直接切断高耗电的内网穿透长连接，并关闭 LocalSend 和 DLNA 广播，仅保留核心传输，极限延长电池寿命。

### 📱 手机客户端 (Client - 主力机操控)
- **局域网一键扫描**：多线程并发探测 `8080` 端口，免去手动输入 IP 的烦恼。
- **多选与批处理**：长按文件补齐了全选、批量下载、批量删除功能。
- **跨端协同（投屏）**：一键下发 `PLAY:<url>` UDP 广播到电视，电视瞬间拉起播放器。
- **跨端协同（配置下发）**：手机通过 UDP 发现 TV 并下发 NAS 密码，电视端自动完成登录。

### 📺 TV 电视端 & ⌚ 手表端
- **原生极速启动**：纯原生 .NET for Android 编写，无任何第三方 UI 库，老旧电视盒子与手表秒开。
- **最低 API 22 兼容**：十年前的 Android 5.1 设备依然能流畅运行。
- **Leanback 与圆盘适配**：针对 TV 的大屏卡片流与手表的圆盘防遮挡 Padding 均做了底层适配。

### 💻 电脑服务端 / PC 命令行客户端 (CLI)
- **DesktopServer (PC 服务端)**：纯原生控制台程序。运行后自动拉起默认浏览器，将你的电脑变成 NAS 控制台。单文件 exe，双击即用。
- **DesktopClient (PC 客户端)**：
  - 纯交互式终端体验。支持 `ls`、`cd`、`get` 等操作。
  - **极客特权**：输入 `exec <cmd>` 可直接调用服务端的 Root 命令执行，体验极致的远程控制。

---

## 🔗 跨端协同与黑科技玩法

本项目实现了真正的局域网无缝协同，完全不需要连接外网：

1. **一键配置下发**：手机端通过底层 UDP 发现电视，并瞬间将 NAS 账号密码下发给电视端。
2. **手机投屏流转**：在手机客户端浏览文件时，点击视频“投屏到 TV”，手机会直接广播 `PLAY` 指令与视频流地址，电视端瞬间拉起播放器。
3. **Web 剪贴板同步**：手机复制的文字，网页端直接粘贴。

---

## 🛠 兼容性与底层架构浅析

| 端 | 最低兼容系统 | 核心技术栈 | 核心取舍 |
|---|---|---|---|
| 安卓服务端 | Android 8.0 (API 26) | MAUI + 原生 HttpListener | 提供最完善的现代保活与全盘操作体验。 |
| 安卓客户端 | Android 8.0 (API 26) | MAUI + Material Design 3 | 强制动态取色，适配 Android 13+ 通知权限。 |
| TV & Watch 端 | Android 5.1 (API 22) | 纯原生 Android 布局 | 零第三方依赖，极限兼容老旧硬件。 |
| 桌面端 / CLI | Win 7+ / Linux | .NET 10 Console + CLI | 免安装单文件，启动迅速，支持多语言。 |

- **版本号控制**：项目引入了 `sync_version.py`。Actions 会自动读取 `CHANGELOG.md` 的版本号，注入到所有 6 个端的工程配置中，彻底告别手动改版本号。
- **纯手写协议栈**：WebDAV 与 FTP 完全手写 Socket 报文解析，不依赖 Apache Commons 等重型库，保证了极低的包体积。

---

## ⚠️ 架构局限性与风险提示

1. **极寒模式副作用**：为了极致省电，开启极寒模式会强制关闭 LocalSend、内网穿透和 DLNA，仅保留核心传输。
2. **Root 模式的危险性**：全盘模式与 Web Shell 拥有极高的系统权限。在网页端操作 `/system` 等目录时请极其谨慎，严禁执行 `rm -rf /` 等破坏性指令。
3. **明文传输与自签证书**：默认采用明文 HTTP 传输。开启 8443 自签 HTTPS 后，部分老旧电视盒子可能不信任证书导致无法连接，请视情况关闭。

---

## 🚀 本地编译指南

```bash
# 1. 安装环境与工作负载
dotnet workload install android maui-android

# 2. 恢复依赖
dotnet restore

# 3. 编译服务端 APK
dotnet publish src/AndroidNas.App/AndroidNas.App.csproj -c Release -f net10.0-android

# 4. 编译 PC 端 (免安装单文件)
dotnet publish src/AndroidNas.DesktopServer/AndroidNas.DesktopServer.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

---

## 协议

本项目使用 [MIT License](LICENSE) © styisme