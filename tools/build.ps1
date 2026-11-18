param(
    [string]$UnityEditorPath,
    [Parameter(Mandatory)]
    [string]$GameDir
)

$ErrorActionPreference = 'Stop'
$GameDir = (Resolve-Path -LiteralPath $GameDir).Path
if (-not (Test-Path -LiteralPath (Join-Path $GameDir 'Deepest Sword_Data\Managed\Assembly-CSharp.dll')) -or
    -not (Test-Path -LiteralPath (Join-Path $GameDir 'BepInEx\core\BepInEx.dll'))) {
    throw 'GameDir must point to the Deepest Sword folder with BepInEx installed.'
}
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$unityProject = Join-Path $PSScriptRoot 'AssetBundleProject'
$unityLog = Join-Path $unityProject 'AssetBundleBuild.log'
$packageDirectory = Join-Path $repositoryRoot 'dist\RTLC.DeepestSword.RussianTranslate'

if ([string]::IsNullOrWhiteSpace($UnityEditorPath)) {
    $registryPaths = @(
        'HKCU:\SOFTWARE\Unity Technologies\Installer\Unity 2021.1.4f1',
        'HKLM:\SOFTWARE\Unity Technologies\Installer\Unity 2021.1.4f1',
        'HKLM:\SOFTWARE\WOW6432Node\Unity Technologies\Installer\Unity 2021.1.4f1'
    )

    foreach ($registryPath in $registryPaths) {
        if (Test-Path $registryPath) {
            $location = (Get-ItemProperty -LiteralPath $registryPath).'Location x64'
            if (-not [string]::IsNullOrWhiteSpace($location)) {
                $candidate = Join-Path $location 'Editor\Unity.exe'
                if (Test-Path -LiteralPath $candidate) {
                    $UnityEditorPath = $candidate
                    break
                }
            }
        }
    }
}

if ([string]::IsNullOrWhiteSpace($UnityEditorPath) -or -not (Test-Path -LiteralPath $UnityEditorPath)) {
    throw 'Unity Editor 2021.1.4f1 was not found. Pass its Editor\Unity.exe path with -UnityEditorPath.'
}

$editorVersion = (Get-Item -LiteralPath $UnityEditorPath).VersionInfo.ProductVersion
if (-not $editorVersion.StartsWith('2021.1.4f1', [StringComparison]::Ordinal)) {
    throw "The asset bundle project is pinned to Unity 2021.1.4f1; the selected editor reports '$editorVersion'."
}

$unityArgumentString = '-batchmode -nographics -quit -projectPath "{0}" -buildTarget StandaloneWindows64 -executeMethod RTLC.DeepestSword.RussianTranslate.Editor.AssetBundleBuilder.BuildBundle -logFile "{1}"' -f $unityProject, $unityLog
$unityProcess = Start-Process `
    -FilePath $UnityEditorPath `
    -ArgumentList $unityArgumentString `
    -PassThru `
    -Wait `
    -WindowStyle Hidden

if ($unityProcess.ExitCode -ne 0) {
    if (Test-Path -LiteralPath $unityLog) {
        Get-Content -LiteralPath $unityLog -Tail 80
    }
    throw "Unity AssetBundle build failed with exit code $($unityProcess.ExitCode)."
}

$projectFile = Join-Path $repositoryRoot 'src\RTLC.DeepestSword.RussianTranslate\RTLC.DeepestSword.RussianTranslate.csproj'
$dotnetArguments = @('build', $projectFile, '-c', 'Release')
if (-not [string]::IsNullOrWhiteSpace($GameDir)) {
    $dotnetArguments += "-p:GameDir=$GameDir"
}

& dotnet @dotnetArguments
if ($LASTEXITCODE -ne 0) {
    throw "Plugin build failed with exit code $LASTEXITCODE."
}

$pluginDll = Join-Path $repositoryRoot 'src\RTLC.DeepestSword.RussianTranslate\bin\Release\netstandard2.0\RTLC.DeepestSword.RussianTranslate.dll'
$bundleFile = Join-Path $packageDirectory 'rtlc.bundle'
if (-not (Test-Path -LiteralPath $pluginDll)) {
    throw "Plugin DLL was not produced: $pluginDll"
}
if (-not (Test-Path -LiteralPath $bundleFile)) {
    throw "AssetBundle was not produced: $bundleFile"
}

New-Item -ItemType Directory -Path $packageDirectory -Force | Out-Null
Copy-Item -LiteralPath $pluginDll -Destination (Join-Path $packageDirectory 'RTLC.DeepestSword.RussianTranslate.dll') -Force

Get-ChildItem -LiteralPath $packageDirectory -File | Select-Object Name, Length
Write-Host "Package ready: $packageDirectory"
