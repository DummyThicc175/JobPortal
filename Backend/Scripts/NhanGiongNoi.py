"""PhoWhisper chay offline; nhan PCM WAV qua stdin, chi tra JSON qua stdout."""
import io
import json
import sys
import wave


def chia_am_thanh(mau):
    import numpy as np

    bat_dau = 0
    while bat_dau < len(mau):
        if len(mau) - bat_dau <= 30 * 16000:
            yield mau[bat_dau:]
            break
        ket_thuc = min(bat_dau + 25 * 16000, len(mau))
        chong_lan = 0
        if ket_thuc < len(mau):
            # Tim khoang nghi 120ms trong giay 20-25, thay vi cat ngang mot tu.
            tim_tu = bat_dau + 20 * 16000
            vung = mau[tim_tu:ket_thuc]
            nang_luong = np.mean(vung[:len(vung) // 320 * 320].reshape(-1, 320) ** 2, axis=1)
            trung_binh = np.convolve(nang_luong, np.ones(6) / 6, mode="valid")
            vi_tri = int(np.argmin(trung_binh))
            if np.sqrt(trung_binh[vi_tri]) < .012:
                ket_thuc = tim_tu + vi_tri * 320 + 960
            else:
                # Neu noi lien tuc, giu 150ms chung de han che mat am o bien.
                # Khong dung thuat toan gop chu de loai cac cau noi lap co that.
                chong_lan = 2400
        yield mau[bat_dau:ket_thuc]
        bat_dau = ket_thuc - chong_lan


def nhan_giong_noi():
    import numpy as np
    import torch
    from transformers import WhisperForConditionalGeneration, WhisperProcessor
    from transformers.utils import logging

    logging.set_verbosity_error()
    torch.set_num_threads(4)
    du_lieu = sys.stdin.buffer.read(3840045)
    with wave.open(io.BytesIO(du_lieu), "rb") as wav:
        if wav.getnchannels() != 1 or wav.getsampwidth() != 2 or wav.getframerate() != 16000 or wav.getnframes() > 1920000:
            raise ValueError("Invalid WAV")
        mau = np.frombuffer(wav.readframes(wav.getnframes()), dtype="<i2").astype(np.float32) / 32768.0
    thu_muc = sys.argv[1]
    model = WhisperForConditionalGeneration.from_pretrained(thu_muc, local_files_only=True, use_safetensors=False)
    bo_xu_ly = WhisperProcessor.from_pretrained(thu_muc, local_files_only=True)
    cac_doan = []
    with torch.inference_mode():
        for doan in chia_am_thanh(mau):
            if np.sqrt(np.mean(doan ** 2)) < .003:
                continue
            dau_vao = bo_xu_ly(doan, sampling_rate=16000, return_tensors="pt", return_attention_mask=True)
            cac_ma = model.generate(**dau_vao, language="vi", task="transcribe", return_timestamps=False,
                                   num_beams=5, do_sample=False, max_new_tokens=440)
            cac_doan.append(bo_xu_ly.batch_decode(cac_ma, skip_special_tokens=True)[0].strip())
    print(json.dumps({"text": " ".join(cac_doan).strip()[:4000]}, ensure_ascii=False), flush=True)


if __name__ == "__main__":
    try:
        nhan_giong_noi()
    except Exception:
        # Khong in am thanh, ban chu hay traceback chua duong dan rieng tu.
        print('{"error":"speech_failed"}', flush=True)
        sys.exit(1)
