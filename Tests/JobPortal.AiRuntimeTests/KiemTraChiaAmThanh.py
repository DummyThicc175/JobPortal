"""Kiem tra chia ban ghi khong mat mau, khong can tai/chay model."""
import importlib.util
from pathlib import Path
import unittest
import numpy as np

tep = Path(__file__).resolve().parents[2] / "Backend" / "Scripts" / "NhanGiongNoi.py"
cau_hinh = importlib.util.spec_from_file_location("nhan_giong_noi", tep)
bo_chia = importlib.util.module_from_spec(cau_hinh)
cau_hinh.loader.exec_module(bo_chia)


class KiemTraChiaAmThanh(unittest.TestCase):
    def kiem_tra_phu_kin(self, mau, cac_doan):
        da_phu = 0
        for doan in cac_doan:
            self.assertTrue(np.shares_memory(doan, mau))
            bat_dau = (doan.ctypes.data - mau.ctypes.data) // mau.itemsize
            self.assertLessEqual(bat_dau, da_phu, "Khong duoc bo sot am thanh giua hai doan")
            self.assertGreater(bat_dau + len(doan), da_phu)
            self.assertLessEqual(len(doan), 30 * 16000)
            da_phu = bat_dau + len(doan)
        self.assertEqual(da_phu, len(mau), "Phai xu ly den het ban ghi")

    def test_giu_du_ban_ghi_lien_tuc(self):
        mau = np.full(120 * 16000, .1, dtype=np.float32)
        cac_doan = list(bo_chia.chia_am_thanh(mau))
        self.kiem_tra_phu_kin(mau, cac_doan)
        self.assertGreater(len(cac_doan), 1)

    def test_tach_tai_khoang_nghi(self):
        mau = np.full(42 * 16000, .1, dtype=np.float32)
        mau[21 * 16000:22 * 16000] = 0
        cac_doan = list(bo_chia.chia_am_thanh(mau))
        self.kiem_tra_phu_kin(mau, cac_doan)
        self.assertGreater(len(cac_doan[0]), 21 * 16000)
        self.assertLess(len(cac_doan[0]), 22 * 16000)

    def test_khong_tao_duoi_qua_ngan(self):
        for giay in (1, 25.01, 26, 29.9, 30):
            mau = np.full(int(giay * 16000), .1, dtype=np.float32)
            cac_doan = list(bo_chia.chia_am_thanh(mau))
            self.assertEqual(len(cac_doan), 1)
            self.kiem_tra_phu_kin(mau, cac_doan)


if __name__ == "__main__":
    unittest.main()
