# ColorMC 全新的MC启动器

[English](./README_EN.md)

使用dotNet 7作为运行环境，XAML作为前端语言，C#作为后端语言

[用户手册](./guide/Main.md)

![](/image/run.png)  
![](/image/GIF.gif)  
![](/image/GIF1.gif)  

## 支持平台
- Linux
- Windows
- Mac OS

Linux由于发行版过于复杂，每个人的电脑兼容性都不一样，如果打不开可以尝试修改`/home/{user}/ColorMC/gui.json`

## 开发环境搭建

### 克隆源码

```
git clone https://github.com/Coloryr/ColorMC.git
cd ColorMC
```

### 安装dotNet7

- Windows/Mac OS
[下载](https://dotnet.microsoft.com/zh-cn/download/dotnet/7.0)里面的SDK安装包安装即可
- Linux
[教程](https://learn.microsoft.com/zh-cn/dotnet/core/install/linux?WT.mc_id=dotnet-35129-website)

```
# Ubuntu
$ wget https://packages.microsoft.com/config/ubuntu/22.10/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
$ sudo dpkg -i packages-microsoft-prod.deb
$ rm packages-microsoft-prod.deb

# Debian
$ wget https://packages.microsoft.com/config/debian/11/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
$ sudo dpkg -i packages-microsoft-prod.deb
$ rm packages-microsoft-prod.deb

$ sudo apt-get update
$ sudo apt-get install -y dotnet-sdk-7.0
```

### 启动

先选择项目`ColorMC.Launcher`进入

```
$ dotnet build
```
```
$ dotnet run
```

## 项目说明
- ColorMC.Android 安卓程序
- ColorMC.Core 启动器底层核心
- ColorMC.Cmd CLI模式 (已放弃)
- ColorMC.Gui Gui模式
- ColorMC.Launcher 启动器
- ColorMC.Test 用于启动器核心测试

## 依赖/引用的项目

[AvaloniaUI](https://github.com/AvaloniaUI/Avalonia) 跨平台UI框架  
[CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) MVVM工具  
[Heijden.Dns.Portable](https://github.com/softlion/Heijden.Dns) DNS解析  
[HtmlAgilityPack](https://html-agility-pack.net/) HTML解析器  
[Jint](https://github.com/sebastienros/jint) JS解析执行器  
[NAudio](https://github.com/naudio/NAudio) Windows音频播放  
[Newtonsoft.Json](https://www.newtonsoft.com/json) JSON解析器  
[OpenTK.OpenAL](https://opentk.net/) openal音频  
[SharpZipLib](https://github.com/icsharpcode/SharpZipLib) 压缩包处理  
[SixLabors](https://sixlabors.com/) 图片处理  
[Tomlyn](https://github.com/xoofx/Tomlyn) TOML解析器  
[ForgeWrapper](https://github.com/ZekerZhayard/ForgeWrapper) Forge启动器  

## 使用的IDE

[Visual Studio Code](https://code.visualstudio.com/)  
[Visual Studio 2022](https://visualstudio.microsoft.com/)
