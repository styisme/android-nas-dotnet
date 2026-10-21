---
name: 🐛 提交 Bug 报告
about: 创建报告以帮助我们改进 Android NAS (.NET 版)
title: '[BUG] '
labels: bug, needs-triage
assignees: ''
---

**⚠️ 注意：在提交之前，请确保你已经前往 GitHub Actions 页面，下载了最新一次成功构建的产物（Artifacts）并进行了测试。如果是编译报错，请附带 Actions 链接。**

---

### 📝 问题描述
请简要且清晰地描述你遇到的 Bug 是什么。

### 📱 发生在哪个端？ (必填)
- [ ] 服务端 - Android (`AndroidNas.App`)
- [ ] 客户端 - Android (`AndroidNas.Client`)
- [ ] 电视端 - TV (`AndroidNas.TV`)
- [ ] 手表端 - Watch (`AndroidNas.Watch`)
- [ ] 服务端 - 电脑 (`DesktopServer`)
- [ ] 客户端 - 电脑 (`DesktopClient` CLI)
- [ ] 网页端控制台 (Web UI)
- [ ] GitHub Actions 编译流水线

### 💻 运行环境 (必填)
- **设备型号**：(例如：小米 6、当贝电视盒子、Wear OS 模拟器、Windows 11 电脑)
- **操作系统版本**：(例如：Android 5.1 (API 22)、Android 13、Windows 11、Ubuntu 22.04)
- **安装的包名**：(如果你测的是安卓端，请说明是正式版 `com.androidnas.next.server` 还是开发版 `.debug`)
- **是否开启了 Root / 全盘模式**：(是 / 否)
- **是否开启了自签 HTTPS**：(是 / 否)

### 🚶‍♂️ 复现步骤
1. 打开 App...
2. 点击了...
3. 输入了...

### 🎯 期望行为
描述你期望发生什么。（例如：“应该成功下载文件”）

### 💥 实际行为 / 报错日志
描述实际发生了什么。如果有红字报错、App 闪退，或者 CI 构建失败的日志，请直接粘贴在此处。

```text
// 如果是 Android 闪退，请提供 adb logcat 的报错堆栈。
// 如果是 GitHub Actions 构建失败，请粘贴 Error 级别的日志。
[将报错信息粘贴在这里]
```

### 📸 截图 / 录屏 (可选)
如果方便，请拖拽相关截图到此处，这能极大帮助我们定位 UI 问题和投屏协议问题。