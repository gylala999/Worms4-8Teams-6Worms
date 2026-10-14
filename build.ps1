param([switch]$RunChecks, [string]$GameFolder)
$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$compilerCandidates = @(
    (Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'),
    (Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe')
)
$compiler = $compilerCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $compiler) { throw 'The .NET Framework 4.x C# compiler was not found.' }
$modules = @('W4TestLauncher','Profile.eight-stress-v33','TeamConfig','RunEvidence','NativeTeamCache','ManagerTeamLists','OrangeAlliance','InventoryTeamDelays','HudFive','NativeWormCaches64','NativeActorStorage64','NativeScans64','NativeCpuTables64','NativeLoadingPools','NativeParticleCache64','NativeMeshPool')
$references = @('/r:System.Windows.Forms.dll','/r:System.Drawing.dll','/r:System.IO.Compression.dll','/r:System.IO.Compression.FileSystem.dll')
$distPath = Join-Path $projectRoot 'dist'
$artifactPath = Join-Path $projectRoot 'artifacts'
$payloadPath = Join-Path $distPath 'CodexTeamTest'
$null = New-Item -ItemType Directory -Path $distPath,$artifactPath,$payloadPath -Force
function Compile-Source([string]$Output, [string[]]$Sources, [string]$Target = 'exe', [string]$Entry = '') {
    $arguments = @('/nologo','/platform:x86','/optimize+',('/target:'+$Target),('/out:'+$Output)) + $references
    if ($Entry) { $arguments += '/main:'+$Entry }
    & $compiler @arguments @Sources
    if ($LASTEXITCODE -ne 0) { throw ('Compilation failed: '+$Output) }
}
function Run-Check([string]$Program, [string[]]$Arguments, [int]$Expected, [string]$LogFolder) {
    $lines = & $Program @Arguments 2>&1
    $result = $LASTEXITCODE
    $name = [IO.Path]::GetFileNameWithoutExtension($Program)
    $lines | Set-Content -LiteralPath (Join-Path $LogFolder ($name+'.log')) -Encoding UTF8
    if ($result -ne 0) { throw ($name+' failed; see its local log.') }
    $count = @($lines | Where-Object { $_.ToString().StartsWith('PASS ') }).Count
    if ($count -ne $Expected) { throw ($name+' returned an unexpected check count: '+$count) }
    Write-Host ('PASS '+$name+': '+$count+' checks')
}
$productionSources = @($modules | ForEach-Object { Join-Path $projectRoot ($_+'.cs') })
$launcherPath = Join-Path $distPath 'W4-EightStressTest-v33.exe'
Compile-Source $launcherPath $productionSources 'winexe'
$payloadNames = @('stdvs.eight-config-v31.lua','Local.eight-config-v31.xml','Persist.eight-config-v31.xml','HudTwk.eight-config-v31.xml','AITwk.eight-config-v31.xml')
foreach ($name in $payloadNames) { Copy-Item -LiteralPath (Join-Path $projectRoot $name) -Destination (Join-Path $payloadPath $name) -Force }
Copy-Item -LiteralPath (Join-Path $projectRoot 'README-Friend-Test.txt') -Destination (Join-Path $distPath 'README-EightStress-v33.txt') -Force
$zipPath = Join-Path $artifactPath 'W4-EightStressTest-v33-source-build.zip'
Compress-Archive -Path $launcherPath,(Join-Path $distPath 'README-EightStress-v33.txt'),$payloadPath -DestinationPath $zipPath -Force
Write-Host ('Built '+$launcherPath)
Write-Host ('Packaged '+$zipPath)
if ($RunChecks) {
    if (-not $GameFolder) { throw '-RunChecks requires -GameFolder pointing to your compatible game.' }
    if (Get-Process -Name 'WORMS 4 MAYHEM' -ErrorAction SilentlyContinue) { throw 'Close the game before running the fixtures.' }
    $gamePath = (Resolve-Path -LiteralPath $GameFolder).Path
    $gameExe = Join-Path $gamePath 'WORMS 4 MAYHEM.EXE'
    if ((Get-FileHash -LiteralPath $gameExe -Algorithm SHA256).Hash -ne 'A5C51FDCA84AD42FD11A15194AE812F1C09E075C42731C36391D0E8756A53E0C') { throw 'Incompatible game executable.' }
    $checkPath = Join-Path $artifactPath ([Guid]::NewGuid().ToString('N'))
    $null = New-Item -ItemType Directory -Path $checkPath
    $meshExe = Join-Path $checkPath 'CheckMeshPool.exe'
    Compile-Source $meshExe @('CheckMeshPool','MeshFixtureData','NativeMeshPool','NativeWormCaches64' | ForEach-Object { Join-Path $projectRoot ($_+'.cs') })
    Run-Check $meshExe @($gameExe) 12856 $checkPath
    $arenaExe = Join-Path $checkPath 'CheckArena64.exe'
    Compile-Source $arenaExe @((Join-Path $projectRoot 'CheckArena64.cs'))
    Run-Check $arenaExe @($launcherPath,$gameExe) 2 $checkPath
    $fixturePath = Join-Path $checkPath 'portable-restore-fixture'
    $profileSource = [IO.File]::ReadAllText((Join-Path $projectRoot 'Profile.eight-stress-v33.cs'))
    $profileSource = $profileSource.Replace('System.IO.Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory)', ('@"'+$fixturePath.Replace('"','""')+'"'))
    $fixtureProfile = Join-Path $checkPath 'Profile.fixture.cs'
    [IO.File]::WriteAllText($fixtureProfile,$profileSource)
    $fixtureSources = @((Join-Path $projectRoot 'CheckFriendLauncher.cs')) + @($modules | ForEach-Object { if ($_ -eq 'Profile.eight-stress-v33') { $fixtureProfile } else { Join-Path $projectRoot ($_+'.cs') } })
    $friendExe = Join-Path $checkPath 'CheckFriendLauncher.exe'
    Compile-Source $friendExe $fixtureSources 'exe' 'CheckFriendLauncher'
    Run-Check $friendExe @($gamePath,$projectRoot) 73 $checkPath
    $configExe = Join-Path $checkPath 'CheckTeamConfig.exe'
    Compile-Source $configExe @((Join-Path $projectRoot 'TeamConfig.cs'),(Join-Path $projectRoot 'CheckTeamConfig.cs'))
    Run-Check $configExe @((Join-Path $projectRoot 'stdvs.eight-config-v31.lua'),(Join-Path $checkPath 'configuration-fixtures')) 32 $checkPath
    Write-Host ('PASS all 12963 isolated checks. Logs: '+$checkPath)
}
