param([Parameter(Mandatory=$true)][string]$FrameworkDir)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$FrameworkDir=(Resolve-Path -LiteralPath $FrameworkDir).Path
foreach($name in @('Tizen.NUI.dll','Tizen.Applications.Common.dll','Tizen.System.dll','Tizen.TV.System.dll','Tizen.TV.System.Boost.dll','Tizen.dll')){if(!(Test-Path -LiteralPath (Join-Path $FrameworkDir $name))){throw "Missing private reference: $name"}}
if(!(Test-Path -LiteralPath (Join-Path $root '.local/catalog.json'))){throw 'Generate .local/catalog.json first; see docs/installation.md'}
$build=Join-Path $root '.local/build'
& dotnet build (Join-Path $root 'launcher/src/Home.csproj') "-p:TV_FRAMEWORK_DIR=$FrameworkDir" -p:NuGetAudit=false -o $build
if($LASTEXITCODE -ne 0){throw 'Launcher build failed'}
& dotnet build (Join-Path $root 'launcher/probe/HomeProbe.csproj') -p:NuGetAudit=false -o (Join-Path $root '.local/probe')
if($LASTEXITCODE -ne 0){throw 'Probe build failed'}
$stage=Join-Path $root '.local/stage'
if(Test-Path -LiteralPath $stage){throw 'Stage already exists; rename it before building another reviewable stage'}
New-Item -ItemType Directory -Path (Join-Path $stage 'res/quiet-home') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $build 'BixbyPromotion.dll'),(Join-Path $build 'BixbyPromotion.runtimeconfig.json') -Destination $stage
Get-ChildItem -LiteralPath (Join-Path $root 'launcher/res') | Copy-Item -Destination (Join-Path $stage 'res/quiet-home') -Recurse
Copy-Item -LiteralPath (Join-Path $root '.local/catalog.json') -Destination (Join-Path $stage 'res/quiet-home/catalog.json')
$localAssets=Join-Path $root '.local/assets'
if(Test-Path -LiteralPath $localAssets){Get-ChildItem -LiteralPath $localAssets | Copy-Item -Destination (Join-Path $stage 'res/quiet-home') -Recurse -Force}
Copy-Item -LiteralPath (Join-Path $root '.local/probe/HomeProbe.dll'),(Join-Path $root '.local/probe/HomeProbe.runtimeconfig.json') -Destination $stage
Write-Output "Stage prepared: $stage"
Get-FileHash -LiteralPath (Join-Path $stage 'BixbyPromotion.dll') -Algorithm SHA256
