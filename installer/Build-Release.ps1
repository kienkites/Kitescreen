param(
    [Parameter(Mandatory)] [string] $DriverPackage,
    [Parameter(Mandatory)] [string] $PlatformTools,
    [string] $Iscc = 'ISCC.exe',
    [string] $Output = (Join-Path $PSScriptRoot 'out')
)

$ErrorActionPreference = 'Stop'
$driver = (Resolve-Path $DriverPackage).Path
$adb = (Resolve-Path $PlatformTools).Path
foreach ($file in @('IddSampleDriver.inf', 'IddSampleDriver.cat', 'IddSampleDriver.dll')) {
    if (-not (Test-Path (Join-Path $driver $file))) { throw "Missing driver file: $file" }
}
foreach ($file in @('adb.exe', 'AdbWinApi.dll', 'AdbWinUsbApi.dll')) {
    if (-not (Test-Path (Join-Path $adb $file))) { throw "Missing Android Platform-Tools file: $file" }
}
if (Test-Path (Join-Path $driver 'Kitescreen-Test.cer')) {
    throw 'The CI VM test driver cannot be distributed in the Windows installer.'
}

$catalog = Get-AuthenticodeSignature (Join-Path $driver 'IddSampleDriver.cat')
if ($catalog.Status -ne 'Valid' -or
    $catalog.SignerCertificate.Subject -notmatch 'Microsoft Windows Hardware Compatibility Publisher') {
    throw 'Supply a release driver catalog signed by Microsoft Windows Hardware Compatibility Publisher.'
}
$inf = Get-Content (Join-Path $driver 'IddSampleDriver.inf') -Raw
if ($inf -notmatch 'Root\\KitescreenPrototypeDisplay') { throw 'The driver hardware ID is unexpected.' }

$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$stage = Join-Path $PSScriptRoot 'stage'
$app = Join-Path $stage 'app'
$helper = Join-Path $stage 'helper'
Remove-Item $stage -Recurse -Force -ErrorAction SilentlyContinue
New-Item $app, $helper, $Output -ItemType Directory -Force | Out-Null

dotnet publish (Join-Path $root 'windows/Kitescreen.Host/Kitescreen.Host.csproj') -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o $app
if ($LASTEXITCODE -ne 0) { throw 'Host publish failed.' }
dotnet publish (Join-Path $root 'windows/Kitescreen.DriverSetup/Kitescreen.DriverSetup.csproj') -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o $helper
if ($LASTEXITCODE -ne 0) { throw 'Driver setup helper publish failed.' }

& $Iscc "/DAppDir=$app" "/DHelperDir=$helper" "/DDriverDir=$driver" "/DAdbDir=$adb" "/DOutputDir=$Output" (Join-Path $PSScriptRoot 'Kitescreen.iss')
if ($LASTEXITCODE -ne 0) { throw 'Windows installer compilation failed.' }
Write-Host "Installer: $(Join-Path $Output 'Kitescreen-Windows-Setup.exe')"
