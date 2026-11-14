param(
 [string]$GameRoot='C:\Games\Counter-Strike Source v34',
 [string]$Map='de_dust2',
 [int]$WaitSeconds=30,
 [switch]$Launch
)
$ErrorActionPreference='Stop'
$stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
$reportDir=Join-Path $GameRoot "PCBridge-Run-$stamp"
New-Item -ItemType Directory -Force $reportDir|Out-Null
$report=[ordered]@{started=(Get-Date).ToString('o');root=$GameRoot;map=$Map;checks=@();launch=$null}
function AddCheck($n,$ok,$detail){$report.checks+=@{name=$n;ok=[bool]$ok;detail="$detail"};Write-Host ("{0} {1}: {2}" -f $(if($ok){'PASS'}else{'FAIL'}),$n,$detail)}
if(!(Test-Path $GameRoot)){throw "Game folder missing: $GameRoot"}
$exe=Join-Path $GameRoot 'cstrike.exe';if(!(Test-Path $exe)){$exe=Join-Path $GameRoot 'hl2.exe'}
AddCheck 'Executable' (Test-Path $exe) $exe
foreach($rel in @('bin\server.dll','bin\client.dll','bin\engine.dll','cstrike\gameinfo.txt')){$p=Join-Path $GameRoot $rel;AddCheck $rel (Test-Path $p) $(if(Test-Path $p){(Get-Item $p).Length}else{'missing'})}
$cfg=Join-Path $GameRoot 'cstrike\cfg\autoexec.cfg'
if(Test-Path $cfg){Copy-Item $cfg (Join-Path $reportDir 'autoexec.cfg')}
if($Launch -and (Test-Path $exe)){
 $proc=Start-Process $exe -ArgumentList "-game cstrike -console -novid +map $Map" -WorkingDirectory $GameRoot -PassThru
 $report.launch=@{pid=$proc.Id;args="-game cstrike -console -novid +map $Map"}
 Start-Sleep $WaitSeconds;$proc.Refresh();$report.launch.alive=(-not $proc.HasExited)
 AddCheck 'Process alive' $report.launch.alive "PID=$($proc.Id)"
}
Get-Process -ErrorAction SilentlyContinue|Where-Object ProcessName -Match 'hl2|cstrike'|Select ProcessName,Id,StartTime|Export-Csv (Join-Path $reportDir 'processes.csv') -NoTypeInformation
$report|ConvertTo-Json -Depth 8|Set-Content (Join-Path $reportDir 'report.json') -Encoding UTF8
$report.checks|ConvertTo-Json -Depth 5|Set-Content (Join-Path $reportDir 'checks.json') -Encoding UTF8
Write-Host "Saved report: $reportDir"
