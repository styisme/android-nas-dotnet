# 底层架构深度解析 (ARCHITECTURE)

本项目经历了从旧版 Java (NanoHTTPD) 到 .NET 10 全平台矩阵的史诗级重构。本文将从通信引擎、编译架构、保活矩阵、跨端协同等多个维度，深度剖析 .NET 版是如何在极限资源下榨干系统性能，并实现 6 端无缝流转的。

---

## 1. 通信引擎革命：告别 Kestrel，拥抱 HttpListener

### 1.1 为什么要抛弃 ASP.NET Core (Kestrel)？
在重构初期，我们使用了 .NET 原生最强的 `ASP.NET Core (Kestrel)` 作为 Web 服务器。但随即在 Android 平台上遭遇了毁灭性打击：
- **体积臃肿**：Kestrel 会引入庞大的运行时库，导致 APK 体积暴增。
- **Runtime Pack 灾难**：在 GitHub Actions 跨平台交叉编译时，MSBuild 会尝试在 Android 目标框架下寻找 Linux/Windows 宿主机的底层包（如 `Mono.linux-x64`），导致 `NETSDK1082` 和 `NU1102` 等无解报错。

### 1.2 原生 HttpListener 的极速替代
为了彻底摆脱平台限制，我们将引擎替换为 `System.Net.HttpListener`。它直接基于操作系统的网络栈，没有任何第三方依赖。
- **异步路由分发**：在 `ServerHost.cs` 中，我们手写了 `AcceptLoopAsync` 循环。
  ```csharp
  var ctx = await _listener.GetContextAsync();
  _ = Task.Run(() => HandleRequestAsync(ctx));
  ```
  每一个 TCP 请求都会被分配到一个独立的线程池任务中处理，避免阻塞主循环。
- **手写流媒体分片 (Range)**：针对视频播放的进度条拖拽，我们在 `HandleDownloadAsync` 中手动解析了 HTTP `Range` 头，计算 `start` 和 `end`，返回 `206 Partial Content`。文件读取采用 80KB 缓冲区按需流式输出，内存占用极低。

---

## 2. 多目标编译与跨平台发布机制

### 2.1 巧妙的双目标框架 (TargetFrameworks)
核心逻辑库 `AndroidNas.Server` 被设计为双目标编译：
`<TargetFrameworks>net10.0;net10.0-android</TargetFrameworks>`
- 当编译安卓服务端 (`AndroidNas.App`) 时，编译器自动选择 `net10.0-android`。
- 当编译 PC 服务端 (`AndroidNas.DesktopServer`) 时，编译器自动选择 `net10.0`。

### 2.2 破解“RID 穿透”诅咒
在 CI 打包 PC 端时，如果我们执行 `dotnet publish -r linux-x64`，这个 `linux-x64` 的标识符（RID）会**向下穿透**到 `AndroidNas.Server`，导致 `net10.0-android` 目标疯狂去寻找 Linux 的安卓宿主包，最终崩溃。
**终极解法**：在 CI 打包 PC 端时强制隔离目标框架，斩断穿透。
```bash
dotnet publish src/AndroidNas.DesktopServer/... -r linux-x64 -p:TargetFrameworks=net10.0
```

---

## 3. 无敌四层保活矩阵 (Android)

安卓系统的 Doze 休眠机制和各大 ROM 的杀后台策略极其残暴。为了保证 NAS 服务 7x24 小时在线，我们构建了四层防御体系：

### L1：系统级前台保活
- 继承 `Service` 并重写 `OnStartCommand` 返回 `StartCommandResult.Sticky`。当系统内存极度紧张杀掉服务时，系统会自动尝试重新拉起。
- 启动 `ForegroundService`，并在通知栏常驻一条不可滑动的通知，将进程优先级提至最高。

### L2：硬件锁级保活
- 使用 `PowerManager.WakeLock` (PARTIAL_WAKE_LOCK)：强制 CPU 保持运行，绝不进入休眠。
- 使用 `WifiManager.WifiLock`：锁定 Wi-Fi 芯片，防止息屏后 Wi-Fi 模块断电断网。

### L3：伪装级保活 (黑科技)
- **SilentPlayer (静音音频欺骗)**：初始化一个 `AudioTrack`，在后台死循环向系统写入全 0 的 PCM 字节流。系统会误以为当前有音乐正在播放，从而极大降低该进程被杀的概率。
- **OnePixelActivity (1像素提权)**：监听屏幕息屏广播 `ACTION_SCREEN_OFF`。一旦息屏，立刻启动一个 1x1 尺寸、透明且无焦点、不可触摸的 `Activity`。这能让 App 在锁屏状态下强行停留在前台活跃队列。屏幕亮起时自动 `Finish`。

### L4：Root/ADB 免死金牌
若设备已获取 Root 权限，可通过 `su` 执行以下指令实现物理级不死：
- `echo -1000 > /proc/<pid>/oom_score_adj`：将 OOM 权重拉到最低，即使系统内存爆炸，系统也不会杀这个进程。
- `am set-standby-bucket <pkg> active`：强制设置活跃度分桶，关闭电池优化限制。
- `pm disable-user --user 0 <厂商杀后台服务>`：暴力冻结小米、华为等厂家的省电守护进程。

---

## 4. 跨端协同协议栈 (UDP 8090)

局域网的设备发现与投屏没有走复杂的 DLNA，而是采用了极轻量、极速的 UDP 广播协议。

### 4.1 发现与配置下发
1. 手机端向局域网 `255.255.255.255:8090` 发送字符串 `DISCOVER_TV`。
2. TV 端收到后，向发送方源 IP 单播回复 `TV_HERE`。
3. 手机获取到 TV IP 后，直接向 `http://<TV_IP>:8090/sync` 发送配置，实现极速配对。

### 4.2 投屏流转 (PLAY 指令)
1. 在手机客户端勾选文件并点击投屏。
2. 手机通过 UDP 广播发送 `PLAY:<绝对URL>`。
3. TV 端在 `StartUdpListener` 中捕获该报文，解析出 URL，直接调用 Android 系统的 `Intent.ACTION_VIEW` 拉起原生播放器进行流媒体播放。

---

## 5. 安全与防护底层设计

### 5.1 路径穿越防御 (SafePath)
NAS 最怕的就是恶意构造 `../../etc/passwd` 这样的路径。我们在 `SafePath.cs` 中做了严格校验：
- 拦截任何包含 `..` 的路径段。
- 将相对路径转换为绝对路径后，校验其前缀是否仍然在 `RootPath` 之下，若越界直接抛出 `SecurityException`。
- ZIP 在线解压时，对每一个 Entry 的目标路径进行同样校验，防止 Zip Slip 攻击。

### 5.2 Web Shell 黑名单拦截
在执行 `su` 命令前，`ServerHost.cs` 会对 `cmd` 字符串进行强力匹配，直接封杀：
`rm -rf /`、`mkfs`、`dd if=`、`reboot`、`shutdown`、`:(){:|:&};:` (Fork 炸弹) 等。

### 5.3 自签 HTTPS 动态绑定
在开启了 HTTPS 开关后，利用 `X509CertificateLoader.LoadPkcs12` 动态在内存中生成一个 10 年有效期的自签 RSA 证书。无需在文件系统生成物理 `.pfx` 文件，随用随建，内存销毁即销毁。