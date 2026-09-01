# -*- coding: utf-8 -*-
"""
Belediye (municipality) + gorev (task) + patron UI pixel-art asset uretici.
Staj_Projesi1 icin, mevcut Assets/Art stiline (32px grid, flat pixel art) uygun.

Calistir:  python Tools/generate_municipality_assets.py
Ciktilar: Assets/Art/{Tiles,Furniture,Props,UI}/ altina .png + .meta
Tekrar calistirilabilir: .meta dosyalari varsa guid korunur (uzerine yazilmaz).
"""
import os
import uuid
from PIL import Image, ImageDraw

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Assets", "Art")
ROOT = os.path.normpath(ROOT)

# --- mevcut paketten alinan palet ---
OUTLINE = (43, 43, 58, 255)
DARK = (30, 34, 44, 255)
BODY = (47, 53, 66, 255)
GRAY_D = (90, 96, 108, 255)
GRAY_M = (150, 150, 155, 255)
GRAY_L = (209, 213, 219, 255)
WHITE = (245, 245, 240, 255)
SHADOW = (20, 20, 40, 60)
WOOD_D = (143, 91, 51, 255)
WOOD_M = (200, 155, 106, 255)
WOOD_L = (217, 160, 102, 255)
WOOD_HL = (233, 190, 138, 255)
CREAM = (232, 224, 208, 255)
CREAM_L = (245, 240, 228, 255)
BLUE = (37, 99, 235, 255)
BLUE_L = (96, 165, 250, 255)
GREEN = (74, 222, 128, 255)
RED = (220, 60, 60, 255)
YELLOW = (240, 200, 60, 255)
SKIN = (240, 200, 160, 255)
HAIR = (60, 50, 45, 255)
T = (0, 0, 0, 0)

# --- 3x5 mini font ---
FONT = {
    "0": ["###", "#.#", "#.#", "#.#", "###"],
    "1": [".#.", "##.", ".#.", ".#.", "###"],
    "2": ["###", "..#", "###", "#..", "###"],
    "3": ["###", "..#", ".##", "..#", "###"],
    "4": ["#.#", "#.#", "###", "..#", "..#"],
    "5": ["###", "#..", "###", "..#", "###"],
    "6": ["###", "#..", "###", "#.#", "###"],
    "7": ["###", "..#", ".#.", ".#.", ".#."],
    "8": ["###", "#.#", "###", "#.#", "###"],
    "9": ["###", "#.#", "###", "..#", "###"],
    ".": ["...", "...", "...", "...", ".#."],
    "K": ["#.#", "#.#", "##.", "#.#", "#.#"],
    "A": [".#.", "#.#", "###", "#.#", "#.#"],
    "T": ["###", ".#.", ".#.", ".#.", ".#."],
    "i": [".#.", "...", ".#.", ".#.", ".#."],
    ":": [".", "#", ".", "#", "."],
    "(": ["#", "#", "#", "#", "#"],
}


def text_px(draw, x, y, s, color, scale=1):
    for ch in s:
        glyph = FONT.get(ch)
        if glyph:
            for gy, row in enumerate(glyph):
                for gx, c in enumerate(row):
                    if c == "#":
                        draw.rectangle(
                            [x + gx * scale, y + gy * scale,
                             x + (gx + 1) * scale - 1, y + (gy + 1) * scale - 1],
                            fill=color)
        x += (len(glyph[0]) + 1) * scale if glyph else 2 * scale


def canvas(w, h):
    im = Image.new("RGBA", (w, h), T)
    return im, ImageDraw.Draw(im)


def shadow(d, cx, y, w, h=4):
    d.ellipse([cx - w // 2, y, cx + w // 2, y + h], fill=SHADOW)


OUT = []  # (relpath, image) toplama listesi


def add(relpath, im):
    OUT.append((relpath, im))


# ============================================================ TILES (32x32)
def tile_floor_marble():
    im, d = canvas(32, 32)
    d.rectangle([0, 0, 31, 31], fill=(226, 222, 214, 255))
    # damarlar
    for pts in [[(3, 20), (9, 14), (12, 14)], [(20, 30), (24, 22), (30, 21)],
                [(6, 4), (11, 7)], [(24, 6), (28, 10)]]:
        d.line(pts, fill=(212, 207, 197, 255), width=1)
    # derz
    d.rectangle([0, 0, 31, 0], fill=(200, 200, 192, 255))
    d.rectangle([0, 0, 0, 31], fill=(200, 200, 192, 255))
    d.point([(31, 31)], fill=(236, 232, 224, 255))
    return im


def tile_floor_corridor():
    im, d = canvas(32, 32)
    d.rectangle([0, 0, 31, 31], fill=(208, 212, 219, 255))
    d.rectangle([0, 0, 31, 1], fill=(178, 184, 194, 255))
    d.rectangle([0, 0, 1, 31], fill=(178, 184, 194, 255))
    d.rectangle([4, 14, 27, 17], fill=(196, 200, 208, 255))  # hafif serit
    return im


def tile_wall_municipality():
    im, d = canvas(32, 32)
    d.rectangle([0, 0, 31, 31], fill=CREAM_L)                 # ust: krem
    d.rectangle([0, 18, 31, 19], fill=(196, 186, 166, 255))   # supurgelik ustu cizgi
    d.rectangle([0, 20, 31, 28], fill=(176, 188, 164, 255))   # alt: yesilimsi lambri
    for x in range(4, 32, 8):
        d.rectangle([x, 21, x, 27], fill=(160, 172, 148, 255))
    d.rectangle([0, 29, 31, 31], fill=GRAY_D)                 # zemin supurgeligi
    return im


def tile_stairs(up=True):
    im, d = canvas(32, 32)
    d.rectangle([0, 0, 31, 31], fill=(170, 170, 162, 255))
    for i in range(5):
        y = i * 7 - 1
        d.rectangle([0, y, 31, y + 5], fill=(196, 196, 188, 255) if i % 2 == 0 else (182, 182, 174, 255))
        d.rectangle([0, y, 31, y], fill=(140, 140, 132, 255))
    # yon oku
    cx = 16
    ys = [10, 14, 18] if up else [22, 18, 14]
    d.polygon([(cx - 4, ys[1]), (cx + 4, ys[1]), (cx, ys[0])], fill=OUTLINE)
    d.polygon([(cx - 2, ys[2]), (cx + 2, ys[2]), (cx, ys[1])], fill=OUTLINE)
    return im


def tile_elevator_door():
    im, d = canvas(32, 32)
    d.rectangle([0, 0, 31, 31], fill=GRAY_D)                  # cerceve
    d.rectangle([3, 3, 28, 31], fill=(120, 126, 135, 255))    # ic cerceve
    d.rectangle([4, 8, 27, 31], fill=(200, 205, 212, 255))    # kapilar
    d.rectangle([15, 8, 16, 31], fill=(120, 126, 135, 255))   # kapi birlesimi
    d.rectangle([4, 8, 27, 9], fill=(176, 182, 191, 255))     # ust isik
    d.rectangle([10, 3, 21, 6], fill=DARK)                    # kat gostergesi
    d.point([(12, 4), (13, 4)], fill=GREEN)                   # yukari isigi
    d.point([(18, 5), (19, 5)], fill=RED)                     # asagi isigi
    return im


# ============================================================ FURNITURE
def furn_counter_desk():
    im, d = canvas(64, 32)
    shadow(d, 32, 26, 56)
    d.rectangle([4, 12, 59, 27], fill=WOOD_D)                 # on panel
    d.rectangle([4, 12, 59, 13], fill=(120, 74, 40, 255))
    for x in (16, 32, 48):
        d.rectangle([x, 15, x, 25], fill=(120, 74, 40, 255))
    d.rectangle([2, 8, 61, 12], fill=WOOD_L)                  # tezgah
    d.rectangle([2, 8, 61, 9], fill=WOOD_HL)
    d.rectangle([6, 2, 57, 8], fill=(180, 210, 230, 120))     # cam bolme
    d.rectangle([6, 2, 57, 3], fill=(210, 230, 245, 160))
    d.rectangle([6, 2, 7, 8], fill=GRAY_M)
    d.rectangle([56, 2, 57, 8], fill=GRAY_M)
    return im


def furn_info_desk():
    im, d = canvas(64, 32)
    shadow(d, 32, 26, 52)
    d.rectangle([8, 14, 55, 27], fill=(60, 90, 140, 255))     # govde (belediye mavisi)
    d.rectangle([8, 14, 55, 16], fill=(80, 110, 165, 255))
    d.rectangle([6, 10, 57, 14], fill=GRAY_L)                 # tezgah
    d.rectangle([6, 10, 57, 11], fill=WHITE)
    d.rectangle([30, 2, 33, 10], fill=GRAY_D)                 # tabela diregi
    d.ellipse([24, -2, 39, 13], fill=BLUE)                    # i tabelasi
    d.ellipse([25, -1, 38, 12], fill=(60, 120, 240, 255))
    text_px(d, 30, 2, "i", WHITE)
    return im


def furn_waiting_bench():
    im, d = canvas(64, 32)
    shadow(d, 32, 26, 56)
    for x0 in (6, 24, 42):
        d.rectangle([x0, 8, x0 + 15, 17], fill=(110, 135, 185, 255))   # sirtlik
        d.rectangle([x0, 8, x0 + 15, 10], fill=(135, 160, 210, 255))
        d.rectangle([x0 - 1, 17, x0 + 16, 23], fill=(95, 118, 168, 255))  # oturak
        d.rectangle([x0 - 1, 17, x0 + 16, 18], fill=(120, 145, 195, 255))
    for x in (8, 30, 52):
        d.rectangle([x, 23, x + 2, 27], fill=GRAY_D)          # ayaklar
    d.rectangle([4, 12, 5, 24], fill=GRAY_D)                  # kol dayama
    d.rectangle([58, 12, 59, 24], fill=GRAY_D)
    return im


def furn_ticket_kiosk():
    im, d = canvas(32, 64)
    shadow(d, 16, 58, 24)
    d.rectangle([7, 6, 24, 58], fill=GRAY_L)                  # govde
    d.rectangle([7, 6, 9, 58], fill=GRAY_M)
    d.rectangle([22, 6, 24, 58], fill=GRAY_M)
    d.rectangle([5, 54, 26, 60], fill=GRAY_D)                 # taban
    d.rectangle([9, 10, 22, 30], fill=DARK)                   # ekran
    d.rectangle([10, 12, 21, 14], fill=BLUE_L)                # baslik seridi
    d.rectangle([11, 17, 20, 25], fill=BODY)
    text_px(d, 13, 18, "7", GREEN, scale=1)                   # sira no
    d.rectangle([11, 27, 20, 28], fill=GRAY_D)
    d.rectangle([10, 36, 21, 42], fill=GRAY_M)                # bilet cikisi
    d.rectangle([12, 38, 19, 40], fill=DARK)
    d.rectangle([12, 34, 19, 37], fill=WHITE)                 # cikan bilet
    d.rectangle([12, 35, 19, 35], fill=GRAY_L)
    return im


def furn_flag_wall():
    im, d = canvas(32, 32)
    d.rectangle([3, 2, 28, 4], fill=WOOD_D)                   # aski cubugu
    d.rectangle([4, 4, 27, 27], fill=(200, 30, 45, 255))      # bayrak
    d.rectangle([4, 4, 27, 5], fill=(225, 60, 70, 255))
    # hilal
    d.ellipse([8, 11, 18, 21], fill=WHITE)
    d.ellipse([10, 12, 19, 20], fill=(200, 30, 45, 255))
    # yildiz (basit 5 kollu)
    d.point([(21, 14), (22, 15), (23, 15), (22, 16), (22, 17),
             (21, 18), (20, 17), (20, 16), (19, 15), (20, 15)], fill=WHITE)
    d.rectangle([4, 27, 27, 28], fill=(150, 20, 32, 255))
    return im


def furn_portrait_wall():
    im, d = canvas(32, 32)
    d.rectangle([5, 3, 26, 28], fill=WOOD_D)                  # cerceve
    d.rectangle([5, 3, 26, 4], fill=WOOD_L)
    d.rectangle([5, 3, 6, 28], fill=WOOD_L)
    d.rectangle([7, 5, 24, 26], fill=(88, 96, 112, 255))      # fon
    d.rectangle([7, 5, 24, 7], fill=(104, 112, 128, 255))
    d.rectangle([11, 19, 20, 26], fill=DARK)                  # omuz/takim
    d.polygon([(15, 19), (17, 19), (16, 24)], fill=WHITE)     # gomlek
    d.point([(16, 22)], fill=RED)                             # kravat
    d.rectangle([12, 10, 19, 19], fill=SKIN)                  # yuz
    d.rectangle([12, 10, 19, 12], fill=HAIR)                  # sac
    d.point([(14, 15), (17, 15)], fill=DARK)                  # gozler
    d.point([(14, 17), (17, 17)], fill=(120, 80, 60, 255))    # kas/bıyık cizgisi
    return im


def furn_floor_sign(no):
    im, d = canvas(32, 32)
    d.rectangle([4, 4, 27, 27], fill=WHITE)                   # plaka
    d.rectangle([4, 4, 27, 27], outline=OUTLINE, width=2)
    d.rectangle([6, 6, 25, 7], fill=BLUE)                     # ust serit
    text_px(d, 12, 9, str(no), OUTLINE, scale=2)              # buyuk rakam
    text_px(d, 8, 21, "KAT", GRAY_D, scale=1)
    return im


# ============================================================ PROPS
def prop_number_plate(n):
    im, d = canvas(16, 16)
    d.rectangle([1, 1, 14, 14], fill=BODY)
    d.rectangle([1, 1, 14, 14], outline=OUTLINE)
    d.rectangle([2, 2, 13, 3], fill=(60, 66, 82, 255))
    text_px(d, 6, 5, str(n), WHITE, scale=1)
    return im


def _monitor_base(d, screen_color):
    shadow(d, 16, 27, 22)
    d.rectangle([5, 4, 26, 21], fill=BODY)                    # kasa
    d.rectangle([5, 4, 26, 5], fill=(60, 66, 82, 255))
    d.rectangle([7, 6, 24, 18], fill=screen_color)            # ekran
    d.rectangle([13, 21, 18, 24], fill=GRAY_D)                # stand
    d.rectangle([10, 24, 21, 26], fill=GRAY_D)


def prop_monitor_no_wifi():
    im, d = canvas(32, 32)
    _monitor_base(d, BLUE)
    # wifi yaylari (beyaz)
    for r, y in ((9, 8), (6, 11), (3, 14)):
        d.arc([16 - r, y, 16 + r, y + 2 * r], 210, 330, fill=WHITE, width=1)
    d.point([(16, 17)], fill=WHITE)
    # kirmizi carpi
    d.line([(9, 7), (23, 18)], fill=RED, width=2)
    d.line([(23, 7), (9, 18)], fill=RED, width=2)
    return im


def prop_monitor_broken():
    im, d = canvas(32, 32)
    _monitor_base(d, DARK)
    # catlak
    d.line([(10, 7), (15, 12), (13, 17)], fill=(200, 205, 212, 255))
    d.line([(15, 12), (21, 9)], fill=(200, 205, 212, 255))
    d.line([(15, 12), (22, 16)], fill=(200, 205, 212, 255))
    d.point([(16, 13), (14, 11)], fill=WHITE)
    return im


def prop_monitor_bluescreen():
    im, d = canvas(32, 32)
    _monitor_base(d, BLUE)
    text_px(d, 10, 8, ":", WHITE, scale=1)
    d.arc([14, 8, 21, 16], 90, 270, fill=WHITE, width=1)      # "("
    d.rectangle([9, 15, 16, 15], fill=(120, 160, 240, 255))   # hata satirlari
    d.rectangle([9, 17, 21, 17], fill=(120, 160, 240, 255))
    return im


def prop_wifi_router_off():
    im, d = canvas(32, 32)
    shadow(d, 16, 26, 24)
    d.rectangle([15, 6, 16, 14], fill=BODY)                   # anten
    d.point([(15, 5)], fill=GRAY_M)
    d.rectangle([6, 14, 25, 24], fill=BODY)                   # govde
    d.rectangle([6, 14, 25, 15], fill=(60, 66, 82, 255))
    d.rectangle([8, 24, 10, 26], fill=DARK)                   # ayaklar
    d.rectangle([21, 24, 23, 26], fill=DARK)
    d.point([(10, 18), (11, 18)], fill=GRAY_M)                # sonuk ledler
    d.point([(14, 18)], fill=RED)                             # hata ledi
    d.point([(17, 18)], fill=GRAY_M)
    # kivilcim
    d.line([(26, 10), (28, 7)], fill=YELLOW)
    d.line([(28, 12), (30, 10)], fill=YELLOW)
    d.point([(27, 9)], fill=(255, 240, 150, 255))
    return im


def prop_printer_jammed():
    im, d = canvas(32, 32)
    shadow(d, 16, 27, 26)
    # sikisan kagit (ustte egik)
    d.polygon([(11, 2), (23, 5), (21, 13), (9, 10)], fill=WHITE)
    d.line([(12, 5), (20, 7)], fill=GRAY_L)
    d.line([(12, 8), (19, 10)], fill=GRAY_L)
    d.rectangle([4, 12, 27, 25], fill=GRAY_L)                 # govde
    d.rectangle([4, 12, 27, 14], fill=WHITE)
    d.rectangle([4, 12, 27, 25], outline=OUTLINE)
    d.rectangle([8, 18, 23, 21], fill=DARK)                   # kagit cikisi
    d.rectangle([10, 21, 21, 27], fill=WHITE)                 # cikan kagit
    d.line([(12, 23), (19, 23)], fill=GRAY_L)
    d.line([(12, 25), (18, 25)], fill=GRAY_L)
    d.point([(24, 15), (25, 15)], fill=RED)                   # hata ledi
    return im


def prop_fuse_box_off():
    im, d = canvas(32, 32)
    d.rectangle([6, 3, 25, 28], fill=GRAY_M)                  # kutu
    d.rectangle([6, 3, 25, 28], outline=OUTLINE)
    d.rectangle([8, 5, 23, 8], fill=YELLOW)                   # uyari seridi
    d.point([(9, 6), (11, 6), (13, 6), (15, 6), (17, 6), (19, 6), (21, 6)], fill=DARK)
    for i, x in enumerate((10, 15, 20)):                      # sigortalar
        d.rectangle([x - 1, 12, x + 1, 19], fill=DARK)
        if i == 1:
            d.rectangle([x - 1, 16, x + 1, 18], fill=RED)     # inik sigorta
        else:
            d.rectangle([x - 1, 12, x + 1, 14], fill=GREEN)
    d.rectangle([8, 22, 23, 25], fill=BODY)
    # kivilcim
    d.line([(27, 12), (29, 9)], fill=YELLOW)
    d.line([(26, 16), (29, 15)], fill=YELLOW)
    d.point([(28, 11)], fill=(255, 240, 150, 255))
    return im


def prop_ceiling_light_off():
    im, d = canvas(32, 32)
    d.rectangle([4, 6, 27, 12], fill=GRAY_L)                  # armatür
    d.rectangle([4, 6, 27, 12], outline=OUTLINE)
    d.rectangle([7, 8, 24, 10], fill=GRAY_D)                  # sonuk florasan
    d.line([(14, 8), (17, 10)], fill=DARK)                    # catlak
    d.line([(16, 8), (13, 10)], fill=DARK)
    d.rectangle([14, 2, 17, 6], fill=GRAY_M)                  # tavan baglantisi
    d.point([(26, 9)], fill=RED)                              # ariza ledi
    # kivilcim
    d.line([(8, 15), (6, 18)], fill=YELLOW)
    d.line([(11, 15), (10, 18)], fill=YELLOW)
    return im


# ============================================================ UI
def _icon_circle(d, bg=BODY):
    d.ellipse([2, 2, 29, 29], fill=bg)
    d.ellipse([2, 2, 29, 29], outline=OUTLINE, width=2)
    d.arc([5, 4, 26, 26], 120, 240, fill=(70, 76, 92, 255))


def ui_icon_wifi_off():
    im, d = canvas(32, 32)
    _icon_circle(d)
    for r, y in ((10, 7), (7, 11), (4, 15)):
        d.arc([16 - r, y, 16 + r, y + 2 * r], 215, 325, fill=BLUE_L, width=2)
    d.point([(15, 19), (16, 19), (15, 20), (16, 20)], fill=BLUE_L)
    d.line([(7, 8), (24, 23)], fill=RED, width=3)
    d.line([(24, 8), (7, 23)], fill=RED, width=3)
    return im


def ui_icon_pc_broken():
    im, d = canvas(32, 32)
    _icon_circle(d)
    d.rectangle([8, 8, 23, 19], fill=DARK)                    # monitor
    d.rectangle([8, 8, 23, 19], outline=GRAY_L)
    d.line([(11, 10), (15, 14), (13, 17)], fill=GRAY_L)       # catlak
    d.line([(15, 14), (20, 11)], fill=GRAY_L)
    d.rectangle([13, 20, 18, 22], fill=GRAY_L)
    d.rectangle([11, 22, 20, 23], fill=GRAY_L)
    d.point([(24, 6), (25, 5), (26, 6), (25, 7)], fill=RED)   # uyari kıvılcımı
    return im


def ui_icon_printer():
    im, d = canvas(32, 32)
    _icon_circle(d)
    d.rectangle([10, 6, 21, 11], fill=GRAY_L)                 # ust kagit
    d.rectangle([8, 11, 23, 19], fill=GRAY_M)                 # govde
    d.rectangle([8, 11, 23, 19], outline=OUTLINE)
    d.rectangle([11, 19, 20, 25], fill=WHITE)                 # cikan kagit
    d.line([(13, 21), (18, 21)], fill=GRAY_L)
    d.line([(13, 23), (17, 23)], fill=GRAY_L)
    d.point([(20, 13)], fill=GREEN)
    return im


def ui_icon_bolt():
    im, d = canvas(32, 32)
    _icon_circle(d)
    d.polygon([(18, 5), (10, 17), (15, 17), (13, 26), (22, 14), (17, 14)],
              fill=YELLOW, outline=OUTLINE)
    return im


def ui_anger_bar_bg():
    im, d = canvas(96, 16)
    d.rectangle([1, 1, 94, 14], fill=BODY)
    d.rectangle([1, 1, 94, 14], outline=OUTLINE, width=1)
    d.rectangle([3, 3, 92, 12], fill=DARK)
    d.rectangle([3, 3, 92, 4], fill=(40, 45, 58, 255))
    return im


def ui_anger_bar_fill():
    im, d = canvas(96, 16)
    for x in range(3, 93):
        t = (x - 3) / 89.0
        if t < 0.5:   # yesil -> sari
            u = t * 2
            c = tuple(int(GREEN[i] + (YELLOW[i] - GREEN[i]) * u) for i in range(3))
        else:         # sari -> kirmizi
            u = (t - 0.5) * 2
            c = tuple(int(YELLOW[i] + (RED[i] - YELLOW[i]) * u) for i in range(3))
        d.rectangle([x, 4, x, 11], fill=c + (255,))
    d.rectangle([3, 4, 92, 5], fill=(255, 255, 255, 40))      # parlama
    return im


def _boss_head(d, skin, mouth, brows, extra=None):
    d.rectangle([8, 26, 23, 31], fill=BODY)                   # ceket
    d.polygon([(13, 26), (18, 26), (16, 31)], fill=WHITE)     # gomlek
    d.point([(16, 29)], fill=RED)                             # kravat
    d.rectangle([9, 6, 22, 26], fill=skin)                    # yuz
    d.rectangle([9, 6, 22, 10], fill=HAIR)                    # sac
    d.rectangle([9, 10, 10, 14], fill=HAIR)
    d.rectangle([21, 10, 22, 14], fill=HAIR)
    d.point([(13, 16), (18, 16)], fill=DARK)                  # gozler
    d.point([(13, 15), (18, 15)], fill=WHITE)
    brows(d)
    mouth(d)
    if extra:
        extra(d)


def ui_face_calm():
    im, d = canvas(32, 32)
    def brows(d):
        d.rectangle([11, 12, 14, 13], fill=HAIR)
        d.rectangle([17, 12, 20, 13], fill=HAIR)
    def mouth(d):
        d.arc([12, 18, 19, 24], 20, 160, fill=(120, 70, 50, 255), width=1)
    _boss_head(d, SKIN, mouth, brows)
    return im


def ui_face_annoyed():
    im, d = canvas(32, 32)
    def brows(d):
        d.line([(11, 12), (14, 13)], fill=HAIR, width=1)
        d.line([(20, 12), (17, 13)], fill=HAIR, width=1)
    def mouth(d):
        d.rectangle([13, 22, 18, 22], fill=(120, 70, 50, 255))
    def extra(d):
        d.point([(7, 14)], fill=(150, 200, 240, 255))         # ter
    _boss_head(d, SKIN, mouth, brows, extra)
    return im


def ui_face_angry():
    im, d = canvas(32, 32)
    def brows(d):
        d.line([(11, 11), (14, 14)], fill=HAIR, width=2)
        d.line([(20, 11), (17, 14)], fill=HAIR, width=2)
    def mouth(d):
        d.arc([12, 20, 19, 26], 200, 340, fill=(120, 70, 50, 255), width=1)
    def extra(d):
        # sinir damari
        for p in [(26, 6), (28, 8), (26, 10), (24, 8), (26, 8)]:
            d.point([p], fill=RED)
    _boss_head(d, (240, 175, 150, 255), mouth, brows, extra)
    return im


def ui_face_furious():
    im, d = canvas(32, 32)
    def brows(d):
        d.line([(11, 10), (14, 14)], fill=HAIR, width=2)
        d.line([(20, 10), (17, 14)], fill=HAIR, width=2)
    def mouth(d):
        d.rectangle([12, 20, 19, 24], fill=(90, 30, 30, 255))  # disler
        d.rectangle([13, 21, 18, 23], fill=WHITE)
        d.line([(13, 22), (18, 22)], fill=(90, 30, 30, 255))
        d.point([(15, 21), (16, 21), (15, 23), (16, 23)], fill=(90, 30, 30, 255))
    def extra(d):
        for p in [(26, 5), (28, 7), (26, 9), (24, 7), (26, 7)]:
            d.point([p], fill=RED)
        # buhar
        d.point([(5, 9), (4, 7), (6, 5), (3, 4)], fill=(220, 225, 230, 200))
        d.point([(27, 13), (28, 12)], fill=(220, 225, 230, 200))
    _boss_head(d, (232, 130, 115, 255), mouth, brows, extra)
    return im


# ============================================================ META
META_TEMPLATE = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 32
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings: []
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData: 
    physicsShape: []
    bones: []
    spriteID: 
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def build_all():
    add("Tiles/floor_marble.png", tile_floor_marble())
    add("Tiles/floor_corridor.png", tile_floor_corridor())
    add("Tiles/wall_municipality.png", tile_wall_municipality())
    add("Tiles/stairs_up.png", tile_stairs(True))
    add("Tiles/stairs_down.png", tile_stairs(False))
    add("Tiles/elevator_door.png", tile_elevator_door())

    add("Furniture/counter_desk.png", furn_counter_desk())
    add("Furniture/info_desk.png", furn_info_desk())
    add("Furniture/waiting_bench.png", furn_waiting_bench())
    add("Furniture/ticket_kiosk.png", furn_ticket_kiosk())
    add("Furniture/flag_wall.png", furn_flag_wall())
    add("Furniture/portrait_wall.png", furn_portrait_wall())
    for n in (1, 2, 3):
        add(f"Furniture/floor_sign_{n}.png", furn_floor_sign(n))

    for n in range(1, 10):
        add(f"Props/number_plate_{n}.png", prop_number_plate(n))
    add("Props/monitor_no_wifi.png", prop_monitor_no_wifi())
    add("Props/monitor_broken.png", prop_monitor_broken())
    add("Props/monitor_bluescreen.png", prop_monitor_bluescreen())
    add("Props/wifi_router_off.png", prop_wifi_router_off())
    add("Props/printer_jammed.png", prop_printer_jammed())
    add("Props/fuse_box_off.png", prop_fuse_box_off())
    add("Props/ceiling_light_off.png", prop_ceiling_light_off())

    add("UI/icon_wifi_off.png", ui_icon_wifi_off())
    add("UI/icon_pc_broken.png", ui_icon_pc_broken())
    add("UI/icon_printer.png", ui_icon_printer())
    add("UI/icon_bolt.png", ui_icon_bolt())
    add("UI/anger_bar_bg.png", ui_anger_bar_bg())
    add("UI/anger_bar_fill.png", ui_anger_bar_fill())
    add("UI/face_calm.png", ui_face_calm())
    add("UI/face_annoyed.png", ui_face_annoyed())
    add("UI/face_angry.png", ui_face_angry())
    add("UI/face_furious.png", ui_face_furious())


def save_all():
    for rel, im in OUT:
        path = os.path.join(ROOT, rel)
        os.makedirs(os.path.dirname(path), exist_ok=True)
        im.save(path)
        meta = path + ".meta"
        if not os.path.exists(meta):
            with open(meta, "w", newline="\n") as f:
                f.write(META_TEMPLATE.format(guid=uuid.uuid4().hex))
        print("OK", rel)


def contact_sheet():
    scale = 4
    cell = 40 * scale
    cols = 8
    rows = (len(OUT) + cols - 1) // cols
    sheet = Image.new("RGBA", (cols * cell, rows * cell + 20), (60, 60, 70, 255))
    sd = ImageDraw.Draw(sheet)
    # dama tahtasi
    for y in range(0, sheet.height, 8):
        for x in range(0, sheet.width, 8):
            if (x // 8 + y // 8) % 2 == 0:
                sd.rectangle([x, y, x + 7, y + 7], fill=(75, 75, 88, 255))
    from PIL import ImageFont
    try:
        font = ImageFont.truetype("consola.ttf", 11)
    except Exception:
        font = ImageFont.load_default()
    for i, (rel, im) in enumerate(OUT):
        cx, cy = (i % cols) * cell, (i // cols) * cell
        big = im.resize((im.width * scale, im.height * scale), Image.NEAREST)
        sheet.alpha_composite(big, (cx + (cell - big.width) // 2, cy + (cell - big.height) // 2))
        sd.text((cx + 2, cy + cell + 4), os.path.basename(rel).replace(".png", ""), fill=(255, 255, 255, 255), font=font)
    out = os.path.join(os.path.dirname(os.path.abspath(__file__)), "belediye_assets_contact_sheet.png")
    sheet.save(out)
    print("CONTACT_SHEET", out)


if __name__ == "__main__":
    build_all()
    save_all()
    contact_sheet()
    print(f"Toplam {len(OUT)} asset uretildi -> {ROOT}")
