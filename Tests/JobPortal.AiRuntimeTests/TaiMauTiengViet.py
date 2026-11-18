"""Hai mau vi_vn/dev tu Google FLEURS (CC-BY-4.0), chi dung de kiem thu.
https://huggingface.co/datasets/google/fleurs
Chi doc phan dau tar qua streaming, khong tai ca bo du lieu.
"""
import csv
import io
import json
import struct
import tarfile
import urllib.request
import wave
from pathlib import Path
import numpy as np

thu_muc = Path(__file__).resolve().parents[2] / 'Backend' / 'Storage' / 'AI' / 'speech' / 'fixtures'
thu_muc.mkdir(parents=True, exist_ok=True)
goc = 'https://huggingface.co/datasets/google/fleurs/resolve/main/data/vi_vn/'
with urllib.request.urlopen(goc + 'dev.tsv', timeout=90) as phan_hoi:
    cac_dong = list(csv.reader(io.StringIO(phan_hoi.read().decode('utf-8')), delimiter='\t'))
bang = {dong[1]: dong for dong in cac_dong}
cac_mau = []
with urllib.request.urlopen(goc + 'audio/dev.tar.gz', timeout=120) as phan_hoi:
    with tarfile.open(fileobj=phan_hoi, mode='r|gz') as tep_nen:
        for muc in tep_nen:
            ten = Path(muc.name).name
            if not muc.isfile() or ten not in bang:
                continue
            du_lieu = tep_nen.extractfile(muc).read()
            vi_tri = 12
            while vi_tri + 8 <= len(du_lieu):
                nhan, do_dai = struct.unpack_from('<4sI', du_lieu, vi_tri)
                khoi = du_lieu[vi_tri + 8:vi_tri + 8 + do_dai]
                if nhan == b'fmt ':
                    loai, kenh, tan_so, _, _, bit = struct.unpack_from('<HHIIHH', khoi)
                elif nhan == b'data':
                    pcm = khoi
                vi_tri += 8 + do_dai + do_dai % 2
            if tan_so != 16000 or kenh != 1:
                raise ValueError('Unexpected fixture format')
            if loai == 3 and bit == 32:
                mau = (np.clip(np.frombuffer(pcm, dtype='<f4'), -1, 1) * 32767).astype('<i2')
            elif loai == 1 and bit == 32:
                mau = (np.frombuffer(pcm, dtype='<i4') >> 16).astype('<i2')
            elif loai == 1 and bit == 16:
                mau = np.frombuffer(pcm, dtype='<i2')
            else:
                raise ValueError(f'Unexpected sample type {loai}/{bit}')
            if not 3 <= len(mau) / tan_so <= 10:
                continue
            dich = thu_muc / ten
            with wave.open(str(dich), 'wb') as wav:
                wav.setparams((1, 2, 16000, 0, 'NONE', 'not compressed'))
                wav.writeframes(mau.tobytes())
            cac_mau.append({'file': ten, 'reference': bang[ten][2], 'seconds': len(mau) / tan_so})
            print(cac_mau[-1], flush=True)
            if len(cac_mau) == 2:
                break
(thu_muc / 'references.json').write_text(json.dumps(cac_mau, ensure_ascii=False, indent=2), encoding='utf-8')
