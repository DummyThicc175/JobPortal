param([switch]$CaiDat, [switch]$TaiModel)
$ErrorActionPreference = 'Stop'
$thuMucDuAn = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$thuMucAi = Join-Path $thuMucDuAn 'Backend/Storage/AI'
$thuMucChay = Join-Path $thuMucAi 'ollama'
$thuMucTai = Join-Path $thuMucAi 'downloads'
$thuMucGiongNoi = Join-Path $thuMucAi 'speech'
New-Item -ItemType Directory -Force $thuMucChay,$thuMucTai,$thuMucGiongNoi | Out-Null
# These variables apply to this shell and its child server, not the user's system.
$env:OLLAMA_MODELS = Join-Path $thuMucAi 'models'
$env:OLLAMA_TMPDIR = Join-Path $thuMucAi 'tmp'
$env:OLLAMA_HOST = '127.0.0.1:11434'
$env:OLLAMA_NUM_PARALLEL = '1'
$env:OLLAMA_MAX_LOADED_MODELS = '1'
$env:OLLAMA_CONTEXT_LENGTH = '4096'
$env:OLLAMA_NO_CLOUD = '1'
New-Item -ItemType Directory -Force $env:OLLAMA_MODELS,$env:OLLAMA_TMPDIR | Out-Null
$tepChay = Join-Path $thuMucChay 'ollama.exe'
$tepHoanTat = Join-Path $thuMucChay 'cpu-install.complete'
if ($CaiDat -and !(Test-Path -LiteralPath $tepHoanTat)) {
    & (Join-Path $PSScriptRoot 'TaiOllamaCpu.ps1') -ThuMucDich $thuMucChay
}
if (!(Test-Path -LiteralPath $tepChay)) { throw 'Chua co Ollama. Chay lai voi -CaiDat -TaiModel.' }
$dangChay = $false
try { $null = Invoke-RestMethod 'http://127.0.0.1:11434/api/tags' -TimeoutSec 3; $dangChay = $true } catch { }
if ($dangChay) {
    # Never pull into a server whose model storage directory we did not configure.
    $tepMaTienTrinh = Join-Path $thuMucAi 'ollama.pid'
    $tienTrinhDaLuu = $null
    if (Test-Path -LiteralPath $tepMaTienTrinh) {
        $maTienTrinh = 0
        if ([int]::TryParse((Get-Content -LiteralPath $tepMaTienTrinh -Raw).Trim(), [ref]$maTienTrinh)) { $tienTrinhDaLuu = Get-Process -Id $maTienTrinh -ErrorAction SilentlyContinue }
    }
    if (!$tienTrinhDaLuu -or $tienTrinhDaLuu.Path -ne $tepChay) { throw 'Cong 11434 dang duoc su dung boi Ollama khac. Dung Ollama cu de chac chan model duoc luu tren o E.' }
    if (!$TaiModel) { Write-Host 'Dich vu AI dang chay tren cong 11434, model nam trong Backend/Storage/AI.'; return }
}
if (!$dangChay) {
$tienTrinh = Start-Process -FilePath $tepChay -ArgumentList 'serve' -WorkingDirectory $thuMucAi -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $thuMucAi 'ollama-output.log') -RedirectStandardError (Join-Path $thuMucAi 'ollama-error.log')
$tienTrinh.Id | Set-Content -LiteralPath (Join-Path $thuMucAi 'ollama.pid')
$sanSang = $false
for ($lan = 0; $lan -lt 30; $lan++) {
    try { $null = Invoke-RestMethod 'http://127.0.0.1:11434/api/tags' -TimeoutSec 2; $sanSang = $true; break } catch { Start-Sleep -Seconds 1 }
}
if (!$sanSang) { throw "Ollama chua khoi dong duoc. Xem log trong $thuMucAi." }
}
if ($TaiModel) {
    & $tepChay pull 'qwen3:1.7b'
    if ($LASTEXITCODE -ne 0) { throw 'Tai model Qwen that bai. Chay lai sau khi dung dich vu vua khoi dong.' }
    $tepGiongNoi = Join-Path $thuMucGiongNoi 'phowhisper-small-source/pytorch_model.bin'
    if (!(Test-Path -LiteralPath $tepGiongNoi)) {
        & (Join-Path $PSScriptRoot 'NangCapNhanGiongNoi.ps1') -PhienBan small
    }
}
Write-Host "AI san sang. Model: $env:OLLAMA_MODELS; giong noi: $thuMucGiongNoi"
