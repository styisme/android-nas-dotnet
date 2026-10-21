#!/usr/bin/env python3
# -*- coding: utf-8 -*-
import re
import os

def main():
    root = os.path.dirname(os.path.abspath(__file__))
    changelog_path = os.path.join(root, "CHANGELOG.md")
    if not os.path.exists(changelog_path):
        print("CHANGELOG.md not found.")
        return

    with open(changelog_path, "r", encoding="utf-8") as f:
        content = f.read()

    # 匹配 [4.0.0-preview] 格式
    match = re.search(r"## \[(.*?)\]", content)
    if not match:
        print("No version found in CHANGELOG.md")
        return

    version = match.group(1)
    print(f"Syncing version: {version}")

    csprojs = [
        "src/AndroidNas.App/AndroidNas.App.csproj",
        "src/AndroidNas.Server/AndroidNas.Server.csproj",
        "src/AndroidNas.Core/AndroidNas.Core.csproj",
        "src/AndroidNas.Cli/AndroidNas.Cli.csproj",
        "src/AndroidNas.Desktop/AndroidNas.Desktop.csproj"
    ]

    for csproj in csprojs:
        path = os.path.join(root, csproj)
        if not os.path.exists(path): continue
        with open(path, "r", encoding="utf-8") as f:
            data = f.read()
        if "<Version>" in data:
            data = re.sub(r"<Version>.*?</Version>", f"<Version>{version}</Version>", data)
        else:
            data = data.replace("<TargetFramework>", f"<Version>{version}</Version>\n    <TargetFramework>")
        with open(path, "w", encoding="utf-8") as f:
            f.write(data)

if __name__ == "__main__":
    main()
