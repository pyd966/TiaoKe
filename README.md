# 眺刻 TiaoKe

眺刻是一款 Windows 桌面休息提醒工具。它会在设定的工作时间结束后，于屏幕角落持续显示一条不抢焦点的提醒；只有当用户主动点击“开始远眺”时，休息倒计时才开始。提醒不会遮挡全屏，也不会反复打断当前思路。

## 产品原则

- 不强迫：提醒到点后保持可见，但不锁屏、不抢焦点、不自动开始休息。
- 够轻巧：只面向 Windows，常驻托盘，避免不必要的后台服务和复杂依赖。
- 状态清楚：随时能看到下一次休息时间，并能立即休息、重置或暂停提醒。
- 安静而精致：界面信息少但层级完整，动画克制，默认不播放声音。

## 技术方向

- `.NET 8` + `WPF`，目标平台 `Windows 10 22H2+ / Windows 11`
- 原生托盘图标与菜单，当前用户级开机自启动
- 本地 JSON 设置，不需要账号、网络或数据库
- 单进程运行，计时核心与界面解耦，便于自动化测试

## 文档

- [项目实施方案](docs/PROJECT_PLAN.md)
- [GUI 与交互方案](docs/GUI_DESIGN.md)

## 当前状态

首个可运行版本已建立：包含托盘常驻、持续提醒、休息倒计时、立即休息、暂停休息提醒、设置持久化、开机启动开关和单实例唤起。核心状态测试位于 `tests/TiaoKe.Tests`。

项目内安装了隔离的 .NET 8 SDK，可直接运行：

```powershell
$env:DOTNET_CLI_HOME="$PWD\.dotnet-home"
$env:NUGET_PACKAGES="$PWD\.nuget\packages"
$env:APPDATA="$PWD\.appdata"
.\.dotnet\dotnet.exe build .\TiaoKe.sln
.\.dotnet\dotnet.exe run --project .\tests\TiaoKe.Tests\TiaoKe.Tests.csproj
```
