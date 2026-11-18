param([ValidateSet('base','small')][string]$PhienBan = 'small')
$ErrorActionPreference = 'Stop'
$thuMucDuAn = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$thuMucAi = Join-Path $thuMucDuAn 'Backend/Storage/AI'
$thuMucNguon = Join-Path $thuMucAi "speech/phowhisper-$PhienBan-source"
$thuMucPython = Join-Path $thuMucAi 'tools/python'
$thuMucTam = Join-Path $thuMucAi 'tmp'
New-Item -ItemType Directory -Force $thuMucNguon,$thuMucPython,$thuMucTam | Out-Null
$env:TEMP = $thuMucTam
$env:TMP = $thuMucTam
$env:PIP_CACHE_DIR = Join-Path $thuMucAi 'tools/pip-cache'
$env:HF_HOME = Join-Path $thuMucAi 'tools/hf-cache'
$env:TORCH_HOME = Join-Path $thuMucAi 'tools/torch-cache'
$env:HF_HUB_OFFLINE = '1'
$env:TRANSFORMERS_OFFLINE = '1'
$env:PYTHONNOUSERSITE = '1'
$tepPython = Join-Path $thuMucPython 'python.exe'
if (!(Test-Path -LiteralPath $tepPython)) {
    $tepNen = Join-Path $thuMucTam 'python-3.12.10-embed-amd64.zip'
    & curl.exe --fail --location --retry 3 --silent --show-error --output $tepNen 'https://www.python.org/ftp/python/3.12.10/python-3.12.10-embed-amd64.zip'
    if ($LASTEXITCODE -ne 0) { throw 'Tai Python nhan giong noi that bai.' }
    Expand-Archive -LiteralPath $tepNen -DestinationPath $thuMucPython -Force
    $tepTimKiem = Join-Path $thuMucPython 'python312._pth'
    [IO.File]::WriteAllText($tepTimKiem, "python312.zip`n.`nLib/site-packages`nimport site`n")
}
if (!(Test-Path -LiteralPath (Join-Path $thuMucPython 'Lib/site-packages/pip'))) {
    $tepPip = Join-Path $thuMucTam 'get-pip.py'
    & curl.exe --fail --location --retry 3 --silent --show-error --output $tepPip 'https://bootstrap.pypa.io/get-pip.py'
    if ($LASTEXITCODE -ne 0) { throw 'Tai cong cu cai thu vien that bai.' }
    & $tepPython $tepPip --no-warn-script-location
    if ($LASTEXITCODE -ne 0) { throw 'Cai pip that bai.' }
}
& $tepPython -m pip install --no-warn-script-location 'numpy==2.2.6' 'torch==2.7.1+cpu' 'transformers==4.53.3' --extra-index-url 'https://download.pytorch.org/whl/cpu'
if ($LASTEXITCODE -ne 0) { throw 'Cai thu vien nhan giong noi that bai.' }
$thongTin = Invoke-RestMethod "https://huggingface.co/api/models/vinai/PhoWhisper-$PhienBan`?blobs=true" -TimeoutSec 60
$maPhienBan = $thongTin.sha
foreach ($ten in @('config.json','vocab.json','pytorch_model.bin','added_tokens.json','generation_config.json','merges.txt','normalizer.json','preprocessor_config.json','special_tokens_map.json','tokenizer.json','tokenizer_config.json')) {
    $tep = Join-Path $thuMucNguon $ten
    $nguon = $thongTin.siblings | Where-Object { $_.rfilename -eq $ten }
    if (!$nguon) { throw "Khong tim thay file chinh thuc: $ten" }
    $canTai = !(Test-Path -LiteralPath $tep)
    if (!$canTai -and $nguon.lfs.sha256) { $canTai = (Get-FileHash -LiteralPath $tep -Algorithm SHA256).Hash -ne $nguon.lfs.sha256 }
    if ($canTai) {
        $tepTam = $tep + '.download'
        & curl.exe --fail --location --retry 3 --continue-at - --silent --show-error --output $tepTam "https://huggingface.co/vinai/PhoWhisper-$PhienBan/resolve/$maPhienBan/$ten"
        if ($LASTEXITCODE -ne 0) { throw "Tai $ten that bai." }
        if ($nguon.lfs.sha256 -and (Get-FileHash -LiteralPath $tepTam -Algorithm SHA256).Hash -ne $nguon.lfs.sha256) { throw 'Checksum checkpoint khong khop.' }
        Move-Item -LiteralPath $tepTam -Destination $tep -Force
    }
}
& $tepPython -c 'import numpy, torch, transformers'
if ($LASTEXITCODE -ne 0) { throw 'Thu vien nhan giong noi chua san sang; cau hinh cu duoc giu lai.' }
$tepCauHinh = Join-Path $thuMucDuAn 'appsettings.json'
$cauHinh = Get-Content -LiteralPath $tepCauHinh -Raw -Encoding utf8 | ConvertFrom-Json
if (!$cauHinh.PSObject.Properties['AiInterview']) { $cauHinh | Add-Member -NotePropertyName AiInterview -NotePropertyValue ([pscustomobject]@{}) }
$cauHinh.AiInterview | Add-Member -NotePropertyName SpeechModelPath -NotePropertyValue "Backend/Storage/AI/speech/phowhisper-$PhienBan-source" -Force
$cauHinh.AiInterview | Add-Member -NotePropertyName SpeechPythonPath -NotePropertyValue 'Backend/Storage/AI/tools/python/python.exe' -Force
[IO.File]::WriteAllText($tepCauHinh, ($cauHinh | ConvertTo-Json -Depth 100), (New-Object Text.UTF8Encoding($false)))
Write-Host "Da cau hinh PhoWhisper-$PhienBan tai o du an. Khoi dong lai website de su dung."
