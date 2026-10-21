# AI 生成声明

本项目（Android NAS .NET 版）在开发与重构过程中，深度使用了大型语言模型（LLM）辅助生成。为了开源协作的透明度，特此详细披露 AI 的参与程度。

**核心声明：本项目 99% 以上的代码（包括 C#、XAML、XML 布局、Python 构建脚本、YAML 流水线等），均由 AI 直接输出，人类开发者在此过程中没有手写过任何一行核心业务代码。**

从旧版的 Java/NanoHTTPD 架构完全推翻重来，到如今的 .NET 10 / MAUI 六端分布式生态，AI 承担了所有的“搬砖”与“架构翻译”工作。

---

## AI 参与的具体部分

### 1. 核心服务端协议栈 (Server & Protocols)
- `ServerHost.cs`：完全由 AI 手写实现了原生 `HttpListener` 的异步请求循环、Range 流媒体分片、以及路径路由分发，彻底替代了笨重的 ASP.NET Core (Kestrel)。
- `FtpServer.cs` / `WebDavServer.cs`：纯 Socket 报文解析的协议栈，支持 PASV 被动模式、跨平台文件系统访问。
- `HttpsHelper.cs`：利用 `X509CertificateLoader` 动态生成 10 年期自签 RSA 证书的加密握手逻辑。
- `LocalSendServer.cs` / `DlnaServer.cs`：基于 UDP 组播与 Kestrel 替代引擎实现的局域网发现与高速文件接收。

### 2. 跨平台与多端 UI (MAUI & Native Android)
- **安卓服务端 / 客户端 (MAUI)**：全套 Material Design 3 XAML 布局、文件浏览器、设置驾驶舱。
- **电视端 / 手表端 (原生 C#)**：针对大屏和圆盘屏幕手写的 `RelativeLayout` 与 `LinearLayout`，以及 Leanback 遥控器焦点逻辑。
- **PC 服务端 / CLI 端**：控制台交互逻辑、自动拉起系统浏览器、以及支持 `exec` 远程命令的 TUI 终端重写。

### 3. 安全与底层防御
- `SafePath.cs`：全套路径穿越（Zip Slip）防御与根目录逃逸校验。
- `AuthService.cs` / `Web Shell 黑名单`：Basic Auth 鉴权系统与拦截 `rm -rf /`、Fork 炸弹的底层字符串匹配逻辑。

### 4. CI/CD 自动化与打包
- `.github/workflows/android-build.yml`：高度复杂的 GitHub Actions 矩阵（Matrix）并行编译策略，包含 4 个安卓端与 2 个 PC 端的隔离构建。
- `sync_version.py` / `scaffold.py`：自动读取 CHANGELOG 注入版本号的 Python 脚本，以及一键生成全项目的脚手架脚本。

---

## 开发者（人类）负责的核心工作

- **产品定义与架构决策**：决定抛弃 Kestrel 改用原生 HttpListener；决定将单体 APK 拆分为 6 个独立的分布式应用；决定基于 UDP 8090 实现无延迟投屏流转。
- **编译环境踩坑与排雷**：在 Termux 手机终端、Pydroid 中运行 AI 生成的 Python 修复脚本，处理由于 .NET 10 预览版 SDK 本身带来的千奇百怪的编译报错。
- **CI/CD 报错反馈**：把 GitHub Actions 里满屏飘红的错误日志（如 RID 穿透、Mono.linux-x64 缺失、Java 关键字冲突）复制粘贴给 AI，由 AI 给出覆盖补丁。
- **真机极限测试**：把编译出的 APK 安装到旧手机、电视盒子（API 22）、智能手表上，测试保活能力、Wi-Fi 锁是否掉线，并反馈给 AI 修复。
- **最终验收**：决定某个 PR 是否能合入 `main` 分支，以及某个版本是否达到发布标准。

---

## 已知的局限性

1. **跨平台“玄学”Bug**：AI 无法在真实的 Windows、Linux、Android 硬件上运行。例如 .NET 10 的多目标框架（TargetFrameworks）在 CI 中引发的 RID 向下穿透问题，是 AI 预判不到的，必须依靠人类报错才能定位。
2. **API 断层**：AI 的训练数据包含了大量旧版 Xamarin 代码。在将代码迁移到 .NET 10 时，诸如 `WifiManager.WifiLockFlags`、`AudioTrack` 等 API 签名变动导致了大量编译错误，需经历多次迭代才打磨平滑。
3. **Root 模式的危险性**：AI 生成的 Web Shell 虽然内置了黑名单，但全盘模式与 OOM 免死注入依然具有极高的系统权限，操作不当（如误删 `/system`）极易导致真机变砖。
4. **自签 HTTPS 的信任危机**：AI 生成的自签证书可以跑通加密，但无法解决老旧电视盒子直接拒绝握手的问题。

---

## 协议

本项目遵循 MIT 协议。