#!/usr/bin/env python3
# -*- coding: utf-8 -*-
import re
import os

def main():
    root = os.path.dirname(os.path.abspath(__file__))
    changelog_path = os.path.join(root, "CHANGELOG.md")
    if not os.path.exists(changelog_path): return

    with open(changelog_path, "r", encoding="utf-8") as f:
        content = f.read()

    match = re.search(r"## \[(.*?)\]", content)
    if not match: return
    version = match.group(1)

    csprojs = [
        "src/AndroidNas.App/AndroidNas.App.csproj",
        "src/AndroidNas.Client/AndroidNas.Client.csproj",
        "src/AndroidNas.Server/AndroidNas.Server.csproj",
        "src/AndroidNas.Core/AndroidNas.Core.csproj",
        "src/AndroidNas.DesktopServer/AndroidNas.DesktopServer.csproj",
        "src/AndroidNas.DesktopClient/AndroidNas.DesktopClient.csproj"
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
