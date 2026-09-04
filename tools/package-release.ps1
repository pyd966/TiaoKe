param(
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version = '0.1.0'
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repositoryRoot 'src\TiaoKe.App\TiaoKe.App.csproj'
$localDotnet = Join-Path $repositoryRoot '.dotnet\dotnet.exe'
$dotnet = if (Test-Path -LiteralPath $localDotnet) { $localDotnet } else { 'dotnet' }

$env:DOTNET_ROOT = Join-Path $repositoryRoot '.dotnet'
$env:DOTNET_CLI_HOME = Join-Path $repositoryRoot '.dotnet-home'
$env:NUGET_PACKAGES = Join-Path $repositoryRoot '.nuget\packages'
$env:APPDATA = Join-Path $repositoryRoot '.appdata'
$env:DOTNET_NOLOGO = '1'

$releaseDirectory = Join-Path $repositoryRoot "artifacts\release\v$Version"
$publishDirectory = Join-Path $releaseDirectory 'win-x64'
$portableDirectory = Join-Path $releaseDirectory 'portable'
$assetBaseName = "TiaoKe-v$Version-win-x64"
$exeAsset = Join-Path $releaseDirectory "$assetBaseName.exe"
$zipAsset = Join-Path $releaseDirectory "$assetBaseName.zip"
$checksumAsset = Join-Path $releaseDirectory "TiaoKe-v$Version-SHA256.txt"

New-Item -ItemType Directory -Force -Path $publishDirectory, $portableDirectory | Out-Null

& $dotnet restore $projectPath -r win-x64
if ($LASTEXITCODE -ne 0) { throw 'dotnet restore failed.' }

& $dotnet publish $projectPath `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:PublishTrimmed=false `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    "-p:Version=$Version" `
    "-p:AssemblyVersion=$Version.0" `
    "-p:FileVersion=$Version.0" `
    "-p:InformationalVersion=$Version" `
    -p:IncludeSourceRevisionInInformationalVersion=false `
    --output $publishDirectory `
    --no-restore
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed.' }

$publishedExe = Join-Path $publishDirectory 'TiaoKe.exe'
if (-not (Test-Path -LiteralPath $publishedExe)) {
    throw "Published executable not found: $publishedExe"
}

Copy-Item -LiteralPath $publishedExe -Destination $exeAsset -Force
Copy-Item -LiteralPath $publishedExe -Destination (Join-Path $portableDirectory 'TiaoKe.exe') -Force

$portableReadme = @"
眺刻 TiaoKe v$Version

系统要求：Windows 10 22H2+ 或 Windows 11，x64。
运行方式：双击 TiaoKe.exe；程序会常驻系统托盘。
退出方式：右键托盘图标，选择“退出眺刻”。
项目主页：https://github.com/pyd966/TiaoKe

本程序为自包含版本，无需另行安装 .NET。
"@
[System.IO.File]::WriteAllText(
    (Join-Path $portableDirectory '使用说明.txt'),
    $portableReadme,
    [System.Text.UTF8Encoding]::new($true))

Compress-Archive `
    -Path (Join-Path $portableDirectory '*') `
    -DestinationPath $zipAsset `
    -CompressionLevel Optimal `
    -Force

$checksumLines = @(
    "$(Get-FileHash -Algorithm SHA256 -LiteralPath $exeAsset | Select-Object -ExpandProperty Hash) *$(Split-Path -Leaf $exeAsset)"
    "$(Get-FileHash -Algorithm SHA256 -LiteralPath $zipAsset | Select-Object -ExpandProperty Hash) *$(Split-Path -Leaf $zipAsset)"
)
[System.IO.File]::WriteAllLines(
    $checksumAsset,
    $checksumLines,
    [System.Text.UTF8Encoding]::new($false))

Get-Item -LiteralPath $exeAsset, $zipAsset, $checksumAsset |
    Select-Object Name, Length, LastWriteTime
