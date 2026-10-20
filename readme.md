# Android NAS (.NET)

[![Platform](https://img.shields.io/badge/Platform-Android-brightgreen.svg)]()
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)]()
[![MAUI](https://img.shields.io/badge/UI-.NET%20MAUI-blue.svg)]()
[![Min SDK](https://img.shields.io/badge/Min_SDK-API_26-blue.svg)]()
[![API Compatible](https://img.shields.io/badge/API-v3.x%20compatible-success.svg)]()
[![License](https://img.shields.io/badge/License-MIT-orange.svg)](LICENSE)
[![Status](https://img.shields.io/badge/Status-Preview-yellow.svg)]()

> **[android-nas](https://github.com/styisme/android-nas) 的 .NET 10 / MAUI 重写版。**
> 保留原版全部 HTTP API、局域网协议与端口分配。原版的 TV 端、Watch 端、桌面 GUI 与 CLI 客户端可**直接连接本版**，无需任何改动。

---

## 项目关系

| | 原版 | 本版 |
|---|---|---|
| 仓库 | [android-nas](https://github.com/styisme/android-nas) | android-nas-dotnet |
| 技术栈 | Java + NanoHTTPD + 纯 Socket | .NET 10 + ASP.NET Core (Kestrel) + MAUI |
| 最低 Android | API 8 | API 26 |
| 包名 | `com.androidnas` | `com.androidnas.next` |
| 客户端 | 服务端 + Web + 手机 + TV + Watch + 桌面 + CLI | 服务端 + Web + 手机（合一） |
| 协议栈 | 全部手写 Socket | Kestrel (HTTP) + 手写 Socket (FTP/WebDAV) |
| 状态 | 维护中 | 🚧 Preview |

**为什么单独开项目？** 原版锁定 `targetSdk 22` 以极限兼容 Android 2.2，无法上架 Google Play；新版走现代工具链，最低 Android 8.0。两者包名不同，可以**同装共存**。

---

## 核心承诺

- ✅ 所有 HTTP 端点路径、参数、响应格式与原版一致
- ✅ Basic Auth 头格式一致
- ✅ 默认端口完全一致（8080 / 8081 / 2121 / 53317 / 8090）
- ✅ UDP 8090 广播字符串一致（`DISCOVER_TV` / `TV_HERE`）
- ✅ DLNA SSDP、LocalSend v2 协议行为一致
- ✅ 原版 TV / Watch / Desktop / CLI 客户端可直接对接

任何不兼容行为请提交 [Issue](https://github.com/styisme/android-nas-dotnet/issues)。

---

## 功能矩阵

### 服务端（内置）

| 端口 | 协议 | 实现 |
|---|---|---|
| `8080` | HTTP API / Web UI / Range 流媒体 | Kestrel |
| `8081` | WebDAV | 纯 Socket 手写 |
| `2121` | FTP (PASV) | 纯 Socket 手写 |
| `53317` | LocalSend v2 | Kestrel 独立实例 |
| `53317/udp` | LocalSend 发现（224.0.0.167） | UdpClient |
| `1900/udp` | DLNA SSDP（239.255.255.250） | UdpClient |
| `8090/udp` | 跨端协同 | UdpClient |

### Web 控制台（内嵌 APK）

- Material Design 3，深色 / 浅色自动跟随
- 全局 401 拦截，自动弹认证框
- 文件列表 / 上传 / 下载 / 重命名 / 删除 / 解压 / 分享
- Range 拖动播放
- 局域网剪贴板

### Android UI（MAUI + MD3）

11 个页面：首页驾驶舱 / 文件浏览 / 文件预览 / 设置 / 日志 / 保活 / Root Shell / 剪贴板 / 内网穿透 / WebView / 关于

### 保活（四层矩阵）

- **L1**：ForegroundService + WakeLock + WifiLock
- **L2**：AudioTrack 静音循环
- **L3**：AlarmManager 5 分钟心跳
- **L4**：Root 注入 `oom_score_adj=-1000` + 厂商省电进程冻结
- 额外：开机自启 / 应用更新自启 / 1px Activity 锁屏保活

### 内网穿透

- 7 个免费节点自动切换（Tunnl / Skytunnel / Serveo / Localhost.run / Pinggy / Tuns.sh / Localto）
- 自定义 SSH 反向隧道

### Root 增强

- Web Shell（危险命令拦截）
- 全盘模式（`/data`, `/system`）
- Magisk 特权模块生成

---

## 未覆盖的功能

以下原版功能尚未在 .NET 版实现，请使用原版：

- 相册自动增量备份
- 局域网一键扫描（新版需手填 IP）
- TV / Watch / Desktop / CLI 客户端（复用原版即可）
- 极寒省电模式（新版已有基础开关，但行为未完全对齐）

> 计划在 v4.1 补齐。优先修复兼容性问题，而非追新功能。

---

## 文档

- 🔌 **[开发者 API 接口规范 (API_DOC.md)](docs/API_DOC.md)**：
  服务端 8080 端口的所有 HTTP API 细节，与原版保持 100% 兼容。
- 🛡️ **[安全策略与高危警告 (SECURITY.md)](SECURITY.md)**：
  **（高危必读）** 使用全盘模式、Web Shell 和公网穿透前，请务必阅读。
- 📝 **[版本更新日志 (CHANGELOG.md)](CHANGELOG.md)**：
  记录从 4.0.0-preview 开始的演进史。

> 原 Java 版的详细使用指南与底层架构剖析等文档同样完全适用于本项目，请参阅原仓库：
> - [终极使用与运维指南 (USAGE.md)](https://github.com/styisme/android-nas/blob/master/docs/USAGE.md)
> - [底层架构深度解析 (ARCHITECTURE.md)](https://github.com/styisme/android-nas/blob/master/docs/ARCHITECTURE.md)
> - [AI 生成声明 (AI_DISCLOSURE.md)](https://github.com/styisme/android-nas/blob/master/docs/AI_DISCLOSURE.md)
---

## 快速开始

### 环境要求

- **.NET SDK 10.0**
- **Android SDK API 35**（编译目标）
- **JDK 17**

```bash
dotnet workload install android maui-android
```

### 构建

```bash
git clone https://github.com/styisme/android-nas-dotnet.git
cd android-nas-dotnet

dotnet new sln -n AndroidNas
dotnet sln add src/AndroidNas.Core src/AndroidNas.Server src/AndroidNas.App

dotnet restore
dotnet build src/AndroidNas.App -f net10.0-android -c Debug
```

### 打包 APK

```bash
dotnet publish src/AndroidNas.App/AndroidNas.App.csproj \
  -c Release -f net10.0-android \
  -p:AndroidPackageFormat=apk
```

产物：`src/AndroidNas.App/bin/Release/net10.0-android/*.apk`

### 真机部署

```bash
dotnet build src/AndroidNas.App -f net10.0-android -t:Run
```

---

## 首次运行

打开 App → 授予权限 → 首页点击「启动」

**授权清单：**

1. **通知权限**（Android 13+）— 前台服务常驻通知
2. **所有文件访问权限**（Android 11+）— 读写 `/storage/emulated/0`
3. **忽略电池优化**（Android 6+）— 防止 Doze 杀服务

启动后：

- 状态栏出现 `Android NAS Next` 常驻通知
- 浏览器访问 `http://<手机IP>:8080`（默认 `admin` / `admin`）
- Windows 资源管理器挂载 `http://<手机IP>:8081` 访问 WebDAV

---

## 端口分配

| 端口 | 协议 | 用途 |
|---|---|---|
| `8080` | TCP | HTTP API / Web UI / 流媒体 |
| `8081` | TCP | WebDAV |
| `2121` | TCP | FTP (PASV) |
| `53317` | TCP | LocalSend HTTP |
| `53317` | UDP | LocalSend 发现（`224.0.0.167`） |
| `1900` | UDP | DLNA SSDP（`239.255.255.250`） |
| `8090` | UDP | 跨端协同（`DISCOVER_TV`） |

---

## 目录结构

```
├── src/
│   ├── AndroidNas.Core/         # 纯逻辑：设置 / 认证 / 路径安全 / 日志 / Root
│   ├── AndroidNas.Server/       # Kestrel + API 端点 + FTP/WebDAV/DLNA/LocalSend
│   └── AndroidNas.App/          # MAUI Android 壳 + MD3 UI
├── docs/
│   └── API_DOC.md               # HTTP API 规范
├── .github/workflows/
│   └── android-build.yml
├── SECURITY.md
├── CHANGELOG.md
└── README.md
```

---

## CI / CD

推送到 `main` 或 `net10-maui` 分支自动构建 APK。打 `v*` 标签自动创建 Release 并挂载 APK。

**正式签名配置**（可选）：仓库 Settings → Secrets 添加：

```
KEYSTORE_BASE64     # keystore 的 base64
KEYSTORE_PASS       # keystore 密码
KEY_ALIAS           # key 别名
KEY_PASS            # key 密码
```

不配置则使用 debug 签名（同样可安装，只是无法上架 Google Play）。

---

## 双版本共存

新版包名 `com.androidnas.next`，旧版 `com.androidnas`，可以同装。

**端口冲突**：两版默认端口相同，同时启动时后启动方会失败，修改任一版本的端口即可。

**配置迁移**：暂无自动迁移，需要手动在 Web 控制台重新配置（Roadmap: v4.1）。

---

## 常见问题

**Q: 原版 TV 端能直接连新版吗？**
A: 能。API、端口、Basic Auth 全部兼容，无需改动客户端。

**Q: 为什么不用 Kestrel 跑 FTP/WebDAV？**
A: Kestrel 是纯 HTTP 服务器。FTP / WebDAV / LocalSend 走独立 `TcpListener`，与 Kestrel 共享 `ServerHost` 生命周期。

**Q: WebDAV 在 Windows 挂载失败？**
A: Windows 默认禁用 Basic Auth over HTTP。修改注册表：

```
HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WebClient\Parameters\BasicAuthLevel = 2
```

重启 `WebClient` 服务。

**Q: 免费穿透节点稳定吗？**
A: 免费节点限速且易断，仅作应急。生产环境用自建 VPS 的 SSH 反向隧道（App 内支持自定义）。

---

## 风险提示

1. **Root 全盘模式**可直接访问 `/data` 与 `/system`，误操作可导致系统变砖
2. **Web Shell** 内置危险命令拦截（`rm -rf /`、`mkfs`、`dd if=`），但无法穷举，谨慎使用
3. **HTTP 明文传输**，公共网络请自建 SSH 隧道加密

详见 [SECURITY.md](SECURITY.md)。

---

## 协议

[MIT License](LICENSE) © styisme

与原版 [android-nas](https://github.com/styisme/android-nas) 一致。

---

## 致谢

- 原项目：[android-nas](https://github.com/styisme/android-nas)
- [.NET MAUI](https://github.com/dotnet/maui)
- [SSH.NET](https://github.com/sshnet/SSH.NET)
- [Material Design 3](https://m3.material.io/)