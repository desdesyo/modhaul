Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
[Windows.Forms.Application]::EnableVisualStyles()
$script:busy=$false
$worker=New-Object ComponentModel.BackgroundWorker
$worker.WorkerReportsProgress=$true;$worker.WorkerSupportsCancellation=$true
$form=New-Object Windows.Forms.Form;$form.Text='CSS v34 — Game Recovery';$form.Size='780,570';$form.StartPosition='CenterScreen'
function AddLabel($t,$x,$y,$w){$c=New-Object Windows.Forms.Label;$c.Text=$t;$c.Location=New-Object Drawing.Point($x,$y);$c.Size=New-Object Drawing.Size($w,22);$form.Controls.Add($c)}
AddLabel 'Папка игры (корень с hl2.exe и bin):' 15 12 700
$game=New-Object Windows.Forms.TextBox;$game.SetBounds(15,36,610,25);$game.Text='C:\Games\Counter-Strike Source v34';$form.Controls.Add($game)
$bg=New-Object Windows.Forms.Button;$bg.Text='Обзор';$bg.SetBounds(635,34,110,28);$form.Controls.Add($bg)
AddLabel 'Чистая копия CSS v34 (скачайте установщик, затем выберите папку игры):' 15 72 730
$src=New-Object Windows.Forms.TextBox;$src.SetBounds(15,96,450,25);$form.Controls.Add($src)
$dl=New-Object Windows.Forms.Button;$dl.Text='Скачать CSS v34';$dl.SetBounds(475,94,150,28);$form.Controls.Add($dl)
$bs=New-Object Windows.Forms.Button;$bs.Text='Обзор';$bs.SetBounds(635,94,110,28);$form.Controls.Add($bs)
$scan=New-Object Windows.Forms.Button;$scan.Text='Проверить файлы';$scan.SetBounds(15,135,170,36);$form.Controls.Add($scan)
$fix=New-Object Windows.Forms.Button;$fix.Text='Восстановить из копии';$fix.SetBounds(195,135,190,36);$form.Controls.Add($fix)
$stop=New-Object Windows.Forms.Button;$stop.Text='Остановить';$stop.SetBounds(395,135,120,36);$stop.Enabled=$false;$form.Controls.Add($stop)
$status=New-Object Windows.Forms.Label;$status.Text='Готово. Начните с проверки.';$status.SetBounds(15,180,730,24);$form.Controls.Add($status)
$bar=New-Object Windows.Forms.ProgressBar;$bar.SetBounds(15,207,730,15);$form.Controls.Add($bar)
$log=New-Object Windows.Forms.TextBox;$log.SetBounds(15,235,730,285);$log.Multiline=$true;$log.ScrollBars='Vertical';$log.ReadOnly=$true;$form.Controls.Add($log)
function Log($s){$log.AppendText(('['+(Get-Date -Format HH:mm:ss)+'] '+$s+[Environment]::NewLine))}
$fd=New-Object Windows.Forms.FolderBrowserDialog
$bg.Add_Click({if($fd.ShowDialog() -eq 'OK'){$game.Text=$fd.SelectedPath}})
$bs.Add_Click({if($fd.ShowDialog() -eq 'OK'){$src.Text=$fd.SelectedPath}})
$dl.Add_Click({try{Start-Process 'https://dl.cs-love.info/CSS_v34.exe';Log 'Открыта загрузка чистой CSS v34 с CS-Love. Установите её в отдельную папку и укажите эту папку как источник.'}catch{Log ('Не удалось открыть ссылку: '+$_.Exception.Message)}})
function State($p){
 if(!(Test-Path -LiteralPath $p -PathType Leaf)){return @{Exists=$false;Valid=$false;Hash=''}}
 $f=[IO.File]::Open($p,'Open','Read','ReadWrite');$valid=$false
 try{$br=New-Object IO.BinaryReader($f);if($f.Length -ge 4096 -and $br.ReadUInt16() -eq 0x5A4D){$f.Position=0x3c;$o=$br.ReadInt32();if($o -gt 0 -and $o -lt ($f.Length-6)){$f.Position=$o;$valid=($br.ReadUInt32() -eq 0x4550 -and $br.ReadUInt16() -eq 0x14c)}}}finally{$f.Dispose()}
 return @{Exists=$true;Valid=$valid;Hash=(Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash}
}
$worker.add_DoWork({
 param($sender,$e);$a=$e.Argument;$root=$a.Root
 if(!(Test-Path -LiteralPath $root -PathType Container)){throw "Папка игры не найдена: $root"}
 if(Get-Process hl2 -ErrorAction SilentlyContinue){throw 'Закройте hl2.exe перед проверкой/восстановлением.'}
 $rels=@('bin\engine.dll','bin\client.dll','bin\server.dll')
 if($a.Mode -eq 'Scan'){foreach($r in $rels){if($sender.CancellationPending){$e.Cancel=$true;return};$p=Join-Path $root $r;$s=State $p;$sender.ReportProgress(0,("SCAN|{0}|{1}|{2}|{3}" -f $r,$s.Exists,$s.Valid,$s.Hash))};return}
 $source=$a.Source
 if(!(Test-Path -LiteralPath $source -PathType Container)){throw 'Выберите папку чистой копии игры.'}
 if([IO.Path]::GetFullPath($source).TrimEnd('\') -eq [IO.Path]::GetFullPath($root).TrimEnd('\')){throw 'Источник и игра совпадают.'}
 $backup=Join-Path (Split-Path -Parent $root) ((Split-Path -Leaf $root)+'.CSSBC_Backup_'+(Get-Date -Format yyyyMMdd_HHmmss))
 New-Item -ItemType Directory -Path $backup -Force|Out-Null;$sender.ReportProgress(0,"LOG|Резервная папка: $backup")
 foreach($r in $rels){
  if($sender.CancellationPending){$e.Cancel=$true;return}
  $sp=Join-Path $source $r;$dp=Join-Path $root $r
  if(!(Test-Path -LiteralPath $sp -PathType Leaf)){$sender.ReportProgress(0,"LOG|Нет в источнике, пропуск: $r");continue}
  $ss=State $sp;if(!$ss.Valid){$sender.ReportProgress(0,"LOG|Источник отклонён: не PE32 x86: $r");continue}
  $ds=State $dp;if($ds.Exists -and $ds.Hash -eq $ss.Hash){$sender.ReportProgress(0,"LOG|Уже совпадает: $r");continue}
  $bf=Join-Path $backup $r
  if($ds.Exists){New-Item -ItemType Directory -Path (Split-Path -Parent $bf) -Force|Out-Null;Copy-Item -LiteralPath $dp -Destination $bf -Force;if((Get-FileHash -LiteralPath $bf -Algorithm SHA256).Hash -ne $ds.Hash){throw "Backup SHA256 mismatch: $r"};$sender.ReportProgress(0,"LOG|Резервная копия проверена: $r")}
  $tmp=$dp+'.cssbc.tmp'
  try{
   Copy-Item -LiteralPath $sp -Destination $tmp -Force
   if((Get-FileHash -LiteralPath $tmp -Algorithm SHA256).Hash -ne $ss.Hash -or !(State $tmp).Valid){throw "Проверка временного файла не пройдена: $r"}
   if($sender.CancellationPending){$e.Cancel=$true;return}
   if(Test-Path -LiteralPath $dp){$old=$dp+'.cssbc.old';if(Test-Path $old){Remove-Item $old -Force};[IO.File]::Replace($tmp,$dp,$old,$true);Remove-Item $old -Force -ErrorAction SilentlyContinue}else{Move-Item $tmp $dp}
   if((Get-FileHash -LiteralPath $dp -Algorithm SHA256).Hash -ne $ss.Hash){if(Test-Path $bf){Copy-Item $bf $dp -Force};throw "Хэш после замены не совпал; откат: $r"}
   $sender.ReportProgress(0,"LOG|Восстановлен и проверен: $r")
  }finally{if(Test-Path $tmp){Remove-Item $tmp -Force -ErrorAction SilentlyContinue}}
 }
 $sender.ReportProgress(0,'LOG|Восстановление завершено.')
})
$worker.add_ProgressChanged({param($s,$e);$m=[string]$e.UserState;if($m.StartsWith('LOG|')){Log $m.Substring(4)}else{$p=$m.Split('|');$v=if($p[2] -eq 'False'){'ОТСУТСТВУЕТ'}elseif($p[3] -eq 'True'){'PE32 x86'}else{'НЕВЕРНЫЙ ФОРМАТ'};Log("$($p[1]): $v; SHA256=$($p[4])")}})
$worker.add_RunWorkerCompleted({param($s,$e);$script:busy=$false;$scan.Enabled=$true;$fix.Enabled=$true;$stop.Enabled=$false;$bar.Style='Continuous';$bar.Value=0;if($e.Error){$status.Text='Ошибка — см. журнал';Log('ОШИБКА: '+$e.Error.Exception.Message)}elseif($e.Cancelled){$status.Text='Операция остановлена'}else{$status.Text='Операция завершена'}})
function StartWork($mode){if($script:busy){return};$script:busy=$true;$scan.Enabled=$false;$fix.Enabled=$false;$stop.Enabled=$true;$bar.Style='Marquee';$status.Text='Работа выполняется...';$worker.RunWorkerAsync(@{Mode=$mode;Root=$game.Text.Trim();Source=$src.Text.Trim()})}
$scan.Add_Click({StartWork 'Scan'})
$fix.Add_Click({if([Windows.Forms.MessageBox]::Show('Заменять только три DLL из выбранной чистой копии? Перед заменой будет создана и проверена резервная копия.','Подтверждение','YesNo','Warning') -eq 'Yes'){StartWork 'Repair'}})
$stop.Add_Click({if($script:busy){$worker.CancelAsync();$status.Text='Запрошена остановка...'}})
$form.Add_FormClosing({param($s,$e);if($script:busy){$e.Cancel=$true;$worker.CancelAsync()}})
Log 'Источник: CS-Love — чистая CSS v34. Нажмите «Скачать CSS v34», установите отдельно и выберите папку как источник.'
[void]$form.ShowDialog()
