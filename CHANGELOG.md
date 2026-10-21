# Changelog

本项目遵循 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.0.0/) 规范，
版本号遵循 [Semantic Versioning](https://semver.org/lang/zh-CN/)。

> **注意：这是原 [android-nas](https://github.com/styisme/android-nas) (Java 版) 的 .NET 10 完全重写版。**
> 自 v4.0.0 起独立演进。底层协议、HTTP API、端口分配与原版 v3.x **100% 兼容**。

---

## [4.1.0] - 2026-10-22

**全平台帝国：六端齐聚，原生引擎重构与极限并行发版**

这是一个极具里程碑意义的版本。我们不仅将后端从笨重的 ASP.NET Core (Kestrel) 彻底剥离，替换为性能极致、零外部依赖的原生 `HttpListener` 引擎；更完成了服务端与客户端的物理分离，真正实现了从手机、电视、手表到 PC 的六端分布式生态。

### Added (全平台矩阵与客户端)

- **6 端彻底解耦与物理分离**：
  - 服务端：`AndroidNas.App` (手机端) 与 `DesktopServer` (PC 控制台)。
  - 客户端：`AndroidNas.Client` (手机端) 与 `DesktopClient` (PC CLI)。
  - 大屏与穿戴：`AndroidNas.TV` 与 `AndroidNas.Watch` 端。
- **TV 与 Watch 端极速兼容**：针对老旧设备，采用纯原生 C# 编写 UI，**最低支持至 Android 5.1 (API 22)**，包体极小，秒开无卡顿。
- **多语言支持 (i18n)**：底层全面引入 `LocalizationManager`。PC 端在控制台首启时询问语言偏好；安卓端在设置中提供 `zh-CN` 与 `en-US` 的无缝下拉切换。
- **PC 客户端 CLI 终端**：全新的 `DesktopClient` 提供极客风格的交互式命令行（支持 `ls`、`cd`、`get`），并特别新增 `exec <cmd>` 命令，直接在电脑端远程执行 NAS 服务端的 Root 指令。
- **TV 投屏协同 (UDP 协议)**：手机客户端选中视频后，通过 UDP `8090` 端口向局域网广播 `PLAY:<视频绝对URL>`，TV 端收到报文瞬间调起系统原生播放器播放。
- **配置极速迁移**：新增 `/api/config/share` 接口，支持将系统全部设置（含端口、密码、各协议开关）一键导出为 JSON，并在新设备上无缝导入。

### Added (服务端与协议层)

- **原生 HttpListener 引擎**：核心 Web 服务不再依赖庞大的 ASP.NET Core。手写异步 `AcceptLoopAsync` 与 80KB 缓冲区的 Range 流媒体分片，彻底解决内存占用问题。
- **自签 HTTPS 动态下发**：开启 HTTPS 开关后，利用 `X509CertificateLoader` 在内存中动态生成有效期为 10 年的自签 RSA 证书，同时监听 HTTP `8080` 与 HTTPS `8443` 端口。
- **Web Shell 危险指令拦截增强**：扩充底层拦截黑名单，加入 `reboot`、`shutdown`、Fork 炸弹 (`:(){:|:&};:`) 及 `mv /*` 等极危操作。
- **相册备份预留接口**：新增 `/api/photos/upload` 端点，为后续客户端相册自动增量备份铺平道路。
- **极寒省电模式联动**：开启后不再仅是 UI 降级，底层会强制降级 WakeLock 频率，自动切断内网穿透，并强制关闭 DLNA 与 LocalSend 广播。

### Changed (架构与规范)

- **包名彻底规范化与共存**：全面启用 `com.androidnas.next.*` 命名空间（如 `com.androidnas.next.server`、`.client` 等）。所有 Android 端原生支持 `.debug` 后缀，允许开发版与正式版同机完美共存。
- **CI/CD 采用 Matrix 并行矩阵**：告别串行编译。GitHub Actions 会瞬间拉起 6 个环境，4 个安卓端与 2 个 PC 端同时构建，耗时大幅缩短。
- **聚合发版机制**：推送 `v*` 形式的 Tag 时，CI 会自动拉取所有产物，打包成一个名为 `AndroidNas-Full-Release.zip` 的单文件并自动发布至 Releases 页面。

### Fixed (底层坑点与遗留修复)

- **彻底解决跨平台 RID 穿透**：通过在 `csproj` 中隔离 `TargetFrameworks`，并在 CI 发布时强制注入 `-p:TargetFrameworks=net10.0`，根治了打包 PC 端时因 RID 穿透导致的 `Mono.linux-x64` 缺失报错。
- **避开 Java 关键字冲突**：将包名中的 `new` 修正为 `next`，彻底解决了 AAPT2 在编译 `R.java` 时因 `package com.androidnas.new...` 导致的语法识别错误。
- **修复 .NET 10 Android API 断层**：适配了 `WifiManagerLockFlags` 的新枚举名、重构了 `AudioTrack` 的构建器（解决废弃警告）、解决了 `Results.Stream` 移除 `encoding` 参数后的编码异常。

### Known Issues & Roadmap

- **相册自动增量备份**：目前服务端仅预留了 `/api/photos/upload` 上传接口，客户端的 `MediaStore` 遍历扫描与同名文件比对逻辑将在 v4.2 实现。
- **TV & Watch 深度 UI**：目前双端已跑通基础数据流转，v4.2 将引入类似 Leanback 的卡片流与遥控器 1.1 倍焦点放大动画。

---

## [4.0.0-preview] - 2026-10-21

**史前预热：从 Java 到 .NET 10 的初步跨越**

这是重构的起步阶段。我们搭好了 Core、Server、App 的初步骨架，跑通了基础的 Kestrel 服务与前台保活机制，但随后在跨平台打包和 Android Runtime Pack 缺失的泥潭中挣扎，最终在 v4.1.0 迎来了彻底翻身。

### Added
- 初步建立 `AndroidNas.Core` / `Server` / `App` 三件套分层架构。
- 跑通 MAUI 基础的 MD3 首页与设备状态显示。
- 尝试复刻四层保活矩阵与 Root 特权执行逻辑。

### Known Issues (已在 v4.1.0 修复)
- CI 流水线因缺少 Android 宿主机的 Runtime Pack 而无法出包。
- 包名仍为 `com.androidnas.next`，未能分离客户端与服务端。
- 部分基于 Kestrel 的流媒体分片响应在 Android 端频繁断流。

---

## 历史版本

v1.0.0 至 v3.3.0 的完整演进史（Java 版），请查阅原项目仓库：
👉 **[android-nas CHANGELOG](https://github.com/styisme/android-nas/blob/master/CHANGELOG.md)**