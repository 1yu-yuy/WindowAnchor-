$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) {
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
if (-not (Test-Path -LiteralPath $compiler)) { throw '.NET Framework C# compiler not found.' }
$source = Join-Path $PSScriptRoot 'src\WindowAnchor.cs'
$interface = Join-Path $PSScriptRoot 'src\Interface.cs'
$icon = Join-Path $PSScriptRoot 'assets\App.ico'
$outputDir = Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
$output = Join-Path $outputDir 'WindowAnchor.exe'
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ "/out:$output" "/win32icon:$icon" "/resource:$icon,WindowAnchor.App.ico" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll $source $interface
if ($LASTEXITCODE -ne 0) { throw 'Build failed. Close the app before rebuilding.' }
Write-Output "Built: $output"
