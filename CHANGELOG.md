# Changelog

本项目遵循 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.0.0/) 规范，
版本号遵循 [Semantic Versioning](https://semver.org/lang/zh-CN/)。

> **本项目是 [android-nas](https://github.com/styisme/android-nas) 的 .NET 10 / MAUI 重写版。**
> 从 v4.0.0 起独立演进。HTTP API、端口、UDP 协议与原版 v3.x **100% 兼容**。

---

## [4.0.0-preview] - 2026-10-21

**从 Java 到 .NET：完整重写，功能对齐**

用 .NET 10 + MAUI 重写了整个服务端和 Android UI，同时保持与原版 v3.3.0 完全一致的 HTTP API 契约。原版的 TV / Watch / Desktop / CLI 客户端无需任何改动即可连接。

### Added (Server — 协议层)

- **HTTP API（Kestrel）** — 8080 端口，对齐原版全部端点：
  - `/api/list` / `/api/download`（Range 分片）/ `/api/upload`（multipart）/ PUT（流式二进制）
  - `/api/preview` / `/api/mkdir` / `/api/rename` / `/api/delete` / `/api/save` / `/api/unzip`
  - `/api/share` + `/share/{token}` 免密下载
  - `/api/search` / `/api/clipboard` / `/api/status` / `/api/settings` / `/api/password` / `/api/shell`
- **WebDAV** — 8081 端口，纯 Socket 手写：
  - `OPTIONS` / `PROPFIND` / `GET` / `HEAD` / `PUT` / `DELETE` / `MKCOL`
  - 支持 Windows 资源管理器、macOS Finder 原生挂载
- **FTP** — 2121 端口，纯 Socket 手写：
  - `USER` / `PASS` / `PASV` / `CWD` / `CDUP` / `LIST` / `NLST` / `RETR` / `STOR` / `DELE` / `MKD` / `RMD` / `RNFR` / `RNTO` / `SIZE`
  - PASV 被动模式
- **DLNA SSDP** — 1900/udp，组播 `239.255.255.250`，响应 `M-SEARCH`，`/dlna/description.xml`
- **LocalSend v2** — 53317/tcp + 53317/udp，组播 `224.0.0.167`：
  - `register` / `prepare-upload` / `upload` / `cancel` 四端点
  - PIN 校验，文件落 `/Download/LocalSend/`，同名自动 `(1)` `(2)` 重命名
- **跨端协同** — 8090/udp，`DISCOVER_TV` → `TV_HERE`，兼容原版协议

### Added (Server — 内网穿透)

- **FreeTunnelManager** — 7 个免费 SSH 节点自动切换：
  Tunnl.gg / Skytunnel / Serveo / Localhost.run / Pinggy / Tuns.sh / Localto.net
- **SshTunnelManager** — 自定义 SSH 反向隧道（`Renci.SshNet`）

### Added (Android — 保活矩阵)

- **L1** — `ForegroundService` + `WakeLock` + `WifiLock`（高 / 低功耗模式）
- **L2** — `SilentPlayer`（AudioTrack 循环写入 0 字节 PCM）
- **L3** — `AlarmManager` 每 5 分钟心跳唤醒
- **L4** — Root 增强：
  - `oom_score_adj=-1000` 免死注入
  - `am set-standby-bucket active` 活跃度提升
  - 小米 / 华为 / OPPO / vivo / 三星省电进程冻结
  - Magisk 特权模块生成
- **额外** — 1px Activity 锁屏保活、开机自启（`BOOT_COMPLETED`）、应用更新自启、`MulticastKeeper` 组播锁

### Added (Android — UI)

Material Design 3 设计语言，11 个页面：

- **首页驾驶舱** — 服务开关 + 端口一览 + 保活状态 + 9 格快捷入口
- **文件浏览** — 浏览 / 上传 / 预览 / 编辑 / 重命名 / 删除 / ZIP 解压 / 分享
- **文件预览** — 图片内嵌、文本内嵌、视频 / 音频外部播放器打开
- **设置** — 设备名 / 三端口 / 六服务开关 / 三高级开关 / 改密码
- **日志** — 最近 200 条记录，复制到剪贴板
- **保活** — Root 检测 / Doze 白名单 / 一键免死 / Magisk 模块 / 还原系统服务
- **Shell** — Root 终端，危险命令拦截
- **剪贴板** — 局域网剪贴板读写
- **穿透** — 节点选择 / 启动 / 停止 / 复制公网链接 / 自定义 SSH
- **WebView** — 内嵌 8080 控制台
- **关于** — 版本 / 端口 / 功能 / 许可

### Added (Web 控制台)

- 内嵌原版轻量版 `index.html`（作为 `EmbeddedResource` 打进 APK）
- 支持外部 `wwwroot/index.html` 覆盖
- Kestrel 提供 `/` 和 `/index.html` 兜底

### Added (CI / CD)

- **GitHub Actions** — 推送到 `main` / `net10-maui` 自动构建 APK
- **正式签名** — 仓库 Secrets 配置 `KEYSTORE_BASE64` / `KEYSTORE_PASS` / `KEY_ALIAS` / `KEY_PASS` 即启用签名
- **未配置时** — 自动降级为 debug 签名（仍可安装）
- **Release** — 打 `v*` 标签自动创建 Release 并挂载 APK

### Changed (相对于原版的架构差异)

- **服务端**：NanoHTTPD → Kestrel（ASP.NET Core）+ 手写 Socket
- **SSH 库**：JSch → SSH.NET
- **JSON**：`org.json` → `System.Text.Json`
- **存储**：SharedPreferences → `Microsoft.Maui.Storage.Preferences`
- **包名**：`com.androidnas` → `com.androidnas.next`（可同装共存）
- **最低 SDK**：API 8 → API 26

### Fixed (相对于原版的已知问题)

- 修复原版 `/api/upload` 未排空流导致的 `unexpected end of stream`
- 修复原版 FTP 写入未关闭数据连接导致的端口泄漏
- 修复原版服务端停止后端口未立即释放的假死问题
- 修复原版 WebDAV 目录 PROPFIND 的 XML 转义问题

### Known Issues

**未实现（计划 v4.1 补齐）：**

- 相册自动增量备份（`MediaStore` 扫描）
- 局域网一键扫描设备
- 极寒省电模式完整行为对齐
- 自签 HTTPS（8443）
- 配置从旧版自动迁移
- TV / Watch / Desktop / CLI 客户端（复用原版即可，API 兼容）

**实验性：**

- 内网穿透的免费节点成功率受网络环境影响，部分节点可能长期不可用
- Magisk 特权模块需重启生效，且部分 ROM 可能不兼容

---

## 版本规划

| 版本 | 目标 | 状态 |
|---|---|---|
| `4.0.0-preview` | 功能对齐 v3.3.0，API 100% 兼容 | ✅ 当前 |
| `4.0.0` | 修复 preview 阶段的所有兼容性问题 | 🚧 进行中 |
| `4.1.0` | 相册备份 / 局域网扫描 / 极寒模式 / HTTPS | 📅 计划 |
| `4.2.0` | 配置迁移 / 审计日志 / 频率限制 | 📅 计划 |

---

## 历史版本

> v1.0.0 至 v3.3.0 的完整演进史请见原项目 [CHANGELOG](https://github.com/styisme/android-nas/blob/master/CHANGELOG.md)。