param(
  [string]$GameRoot = 'C:\Games\Counter-Strike Source v34',
  [string]$Map = 'de_dust2',
  [int]$WaitSeconds = 25
)
$ErrorActionPreference = 'Stop'
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$out = Join-Path $GameRoot "PCBridge-Diagnostics-$stamp"
New-Item -ItemType Directory -Force -Path $out | Out-Null
$report = [ordered]@{ time=(Get-Date).ToString('o'); gameRoot=$GameRoot; checks=@(); launch=$null }
function Check($name,$ok,$detail){$report.checks += [ordered]@{name=$name;ok=[bool]$ok;detail=$detail};Write-Host ("[{0}] {1}: {2}" -f ($(if($ok){'OK'}else{'FAIL'}),$name,$detail))}
if(!(Test-Path $GameRoot)){throw "Game root not found: $GameRoot"}
$exe=Join-Path $GameRoot 'cstrike.exe'
if(!(Test-Path $exe)){$exe=Join-Path $GameRoot 'hl2.exe'}
Check 'Executable' (Test-Path $exe) $exe
$required=@('bin\server.dll','bin\client.dll','bin\engine.dll','cstrike\gameinfo.txt')
foreach($rel in $required){$p=Join-Path $GameRoot $rel;Check $rel (Test-Path $p) $(if(Test-Path $p){(Get-Item $p).Length}else{'missing'})}
$logs=Get-ChildItem (Join-Path $GameRoot 'cstrike') -File -Include '*.log','*.txt','*.cfg' -ErrorAction SilentlyContinue | Where-Object {$_.Name -match 'console|error|exception|log'} | Select-Object -First 30
foreach($f in $logs){Copy-Item $f.FullName $out -Force}
$report.checks | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $out 'checks.json') -Encoding UTF8
if(Test-Path $exe){
  $args="-game cstrike -console -novid +map $Map"
  $p=Start-Process -FilePath $exe -ArgumentList $args -PassThru -WorkingDirectory $GameRoot
  $report.launch=[ordered]@{pid=$p.Id;arguments=$args}
  Start-Sleep -Seconds $WaitSeconds
  $p.Refresh();$report.launch.alive=(-not $p.HasExited)
  Check 'Game process after wait' $report.launch.alive "PID $($p.Id)"
  Get-Process | Where-Object {$_.ProcessName -match 'hl2|cstrike'} | Select-Object ProcessName,Id,StartTime | Export-Csv (Join-Path $out 'processes.csv') -NoTypeInformation -Encoding UTF8
}
$report | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $out 'report.json') -Encoding UTF8
Write-Host "Report: $out\report.json"
