# ColorMC

[简体中文](./README.md)

A new Minecraft Launcher  
![](/image/pic.png)

## Supported Platform
- Linux
- Windows
- Mac OS

Due to the complexity of Linux distribution, everyone's compatibility is different. If it cannot be opened it, you can try modifying `/home/{user}/ColorMC/gui.json`

## Development environment

### Clone code

```
git clone https://github.com/Coloryr/ColorMC.git
cd ColorMC
```

### Insatll dotNet7

- Windows/Mac OS
[Download](https://dotnet.microsoft.com/zh-cn/download/dotnet/7.0)
- Linux
[Doc](https://learn.microsoft.com/zh-cn/dotnet/core/install/linux?WT.mc_id=dotnet-35129-website)

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

### Launch

Goto project `ColorMC.Launcher` directory

```
$ dotnet build
```
```
$ dotnet run
```

## Project description
- ColorMC.Core Basically core of Launcher
- ColorMC.Cmd CLI mode (No longer support)
- ColorMC.Gui Gui mode
- ColorMC.Launcher Launcher
- ColorMC.Test For launcher core testing

## Skin View

![](/image/GIF1.gif)  

## Referenced

[AvaloniaUI](https://github.com/AvaloniaUI/Avalonia) 跨平台UI框架  
[Heijden.Dns.Portable](https://github.com/softlion/Heijden.Dns) DNS解析  
[HtmlAgilityPack](https://html-agility-pack.net/) HTML解析器  
[Jint](https://github.com/sebastienros/jint) JS解析执行器  
[Newtonsoft.Json](https://www.newtonsoft.com/json) JSON解析器  
[SharpZipLib](https://github.com/icsharpcode/SharpZipLib) 压缩包处理  
[Tomlyn](https://github.com/xoofx/Tomlyn) TOML解析器  
[OpenTK](https://opentk.net/) openal音频  
[SixLabors](https://sixlabors.com/) 图片处理  
[CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) MVVM工具  
[NAudio](https://github.com/naudio/NAudio) Windows音频播放

## Use IDE

[Visual Studio Code](https://code.visualstudio.com/)  
[Visual Studio](https://visualstudio.microsoft.com/)
