param([Parameter(Mandatory=$true)][string]$ThuMucDich)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
# Read only the CPU files from the official ZIP over HTTPS byte ranges.
# CUDA/Vulkan archives account for most of the 1.6GB distribution and are unnecessary here.
Add-Type -TypeDefinition @'
using System;
using System.IO;
using System.Net;
public sealed class LuongZipTuXa : Stream {
    private readonly string diaChi;
    private readonly long doDai;
    private long viTri, dauBoDem = -1;
    private byte[] boDem = new byte[0];
    public LuongZipTuXa(string url, long length) { diaChi = url; doDai = length; }
    public override bool CanRead { get { return true; } }
    public override bool CanSeek { get { return true; } }
    public override bool CanWrite { get { return false; } }
    public override long Length { get { return doDai; } }
    public override long Position { get { return viTri; } set { viTri = value; } }
    public override int Read(byte[] buffer, int offset, int count) {
        if (viTri >= doDai || count == 0) return 0;
        if (viTri < dauBoDem || viTri >= dauBoDem + boDem.Length) {
            long cuoi = Math.Min(doDai - 1, viTri + 2 * 1024 * 1024 - 1);
            var yeuCau = (HttpWebRequest)WebRequest.Create(diaChi);
            yeuCau.AddRange(viTri, cuoi); yeuCau.Timeout = 120000; yeuCau.ReadWriteTimeout = 120000;
            yeuCau.UserAgent = "JobPortal-local-AI-setup";
            using (var phanHoi = (HttpWebResponse)yeuCau.GetResponse()) {
                if (phanHoi.StatusCode != HttpStatusCode.PartialContent ||
                    phanHoi.Headers["Content-Range"] != "bytes " + viTri + "-" + cuoi + "/" + doDai)
                    throw new IOException("May chu khong ho tro tai tung phan ZIP.");
                using (var luong = phanHoi.GetResponseStream()) {
                    boDem = new byte[cuoi - viTri + 1]; int daDoc = 0;
                    while (daDoc < boDem.Length) { int soDoc = luong.Read(boDem, daDoc, boDem.Length - daDoc); if (soDoc == 0) throw new EndOfStreamException(); daDoc += soDoc; }
                }
            }
            dauBoDem = viTri;
        }
        int soLuong = (int)Math.Min(count, dauBoDem + boDem.Length - viTri);
        Buffer.BlockCopy(boDem, (int)(viTri - dauBoDem), buffer, offset, soLuong); viTri += soLuong; return soLuong;
    }
    public override long Seek(long offset, SeekOrigin origin) { viTri = origin == SeekOrigin.Begin ? offset : origin == SeekOrigin.Current ? viTri + offset : doDai + offset; return viTri; }
    public override void Flush() { }
    public override void SetLength(long value) { throw new NotSupportedException(); }
    public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
}
'@
$ThuMucDich = [IO.Path]::GetFullPath($ThuMucDich)
$gocChoPhep = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../Storage/AI')) + [IO.Path]::DirectorySeparatorChar
if (!$ThuMucDich.StartsWith($gocChoPhep, [StringComparison]::OrdinalIgnoreCase)) { throw 'Chi cai bo chay trong Backend/Storage/AI.' }
$phienBan = 'v0.40.16'
$thongTin = Invoke-RestMethod "https://api.github.com/repos/ollama/ollama/releases/tags/$phienBan" -TimeoutSec 30
$goi = $thongTin.assets | Where-Object { $_.name -eq 'ollama-windows-amd64.zip' }
if (!$goi -or $goi.browser_download_url -ne "https://github.com/ollama/ollama/releases/download/$phienBan/ollama-windows-amd64.zip") { throw 'Khong tim duoc goi Ollama chinh thuc.' }
$luong = New-Object LuongZipTuXa($goi.browser_download_url, [long]$goi.size)
$zip = New-Object IO.Compression.ZipArchive($luong, [IO.Compression.ZipArchiveMode]::Read)
try {
    foreach ($muc in $zip.Entries) {
        if ($muc.FullName -ne 'ollama.exe' -and $muc.FullName -notmatch '^lib/ollama/[^/]+\.(dll|exe)$' -and $muc.FullName -notmatch '^lib/ollama/cpu[^/]*/[^/]+\.(dll|exe)$') { continue }
        $dich = [IO.Path]::GetFullPath((Join-Path $ThuMucDich $muc.FullName))
        if (!$dich.StartsWith($ThuMucDich + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Duong dan ZIP khong hop le.' }
        New-Item -ItemType Directory -Force ([IO.Path]::GetDirectoryName($dich)) | Out-Null
        Write-Host "Dang tai $($muc.FullName) ($([Math]::Round($muc.Length / 1MB, 1)) MB)"
        [IO.Compression.ZipFileExtensions]::ExtractToFile($muc, $dich, $true)
    }
} finally { $zip.Dispose(); $luong.Dispose() }
[IO.File]::WriteAllText((Join-Path $ThuMucDich 'cpu-install.complete'), $phienBan)
