param(
    [Parameter(Mandatory)] [string] $DriverBinary,
    [Parameter(Mandatory)] [string] $Inf2Cat,
    [string] $Output = (Join-Path $PSScriptRoot 'submission')
)

$ErrorActionPreference = 'Stop'
$source = (Resolve-Path $PSScriptRoot).Path
$binary = (Resolve-Path $DriverBinary).Path
$outputPath = [System.IO.Path]::GetFullPath($Output)
$stage = Join-Path $outputPath 'stage'
$inf = Join-Path $source 'IddSampleDriver/IddSampleDriver.inf'
$pdb = Join-Path (Split-Path $binary) 'IddSampleDriver.pdb'
foreach ($file in @($inf, $pdb)) {
    if (-not (Test-Path $file -PathType Leaf)) { throw "Missing submission file: $file" }
}

# Only prepare an unsigned submission; never use the VM test certificate here.
if (Test-Path $outputPath) { Remove-Item $outputPath -Recurse -Force }
New-Item $stage -ItemType Directory -Force | Out-Null
Copy-Item $inf, $binary, $pdb $stage
& $Inf2Cat "/driver:$stage" /os:10_GE_X64
if ($LASTEXITCODE -ne 0) { throw 'Inf2Cat failed: submission package is not valid.' }

$cab = Join-Path $outputPath 'Disk1/Kitescreen-Driver-Submission.cab'
$ddf = @'
.OPTION EXPLICIT
.Set CabinetFileCountThreshold=0
.Set FolderFileCountThreshold=0
.Set FolderSizeThreshold=0
.Set MaxCabinetSize=0
.Set MaxDiskFileCount=0
.Set MaxDiskSize=0
.Set CompressionType=MSZIP
.Set Cabinet=on
.Set Compress=on
.Set CabinetNameTemplate=Kitescreen-Driver-Submission.cab
.Set DiskDirectoryTemplate=Disk1
.Set DestinationDir=Kitescreen
"stage\IddSampleDriver.inf"
"stage\IddSampleDriver.dll"
"stage\IddSampleDriver.pdb"
"stage\IddSampleDriver.cat"
'@
$ddfPath = Join-Path $outputPath 'submission.ddf'
Set-Content $ddfPath $ddf -Encoding ASCII
Push-Location $outputPath
try {
    & makecab.exe /F $ddfPath
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path $cab -PathType Leaf)) {
        throw 'MakeCAB failed to create the driver submission.'
    }
}
finally { Pop-Location }
Write-Host "Unsigned CAB for EV signing and Partner Center submission: $cab"
