# Yeni zemin, hali ve kadin karakter sprite'lari uretir.
# Mevcut Assets/Art altindaki pixel-art sablonlerini birebir takip eder.
import random
from PIL import Image

ART = "C:/Users/admin/Staj_Projesi1/Assets/Art"
TILES = ART + "/Tiles"
CHARS = ART + "/Characters"


def new_img(w, h):
    return Image.new("RGBA", (w, h), (0, 0, 0, 0))


# ---------------------------------------------------------------- zeminler
def floor_tile_beige():
    """floor_tile_light ile ayni derzli fayans yapisi, bej tonlarda."""
    grout = (188, 178, 158, 255)
    tile = (223, 213, 193, 255)
    im = new_img(32, 32)
    for y in range(32):
        for x in range(32):
            im.putpixel((x, y), grout if x % 8 == 0 or y % 8 == 0 else tile)
    im.save(TILES + "/floor_tile_beige.png")


def floor_checkered():
    """8x8'lik acik/koyu gri dama zemini."""
    a = (214, 214, 208, 255)
    b = (164, 164, 160, 255)
    im = new_img(32, 32)
    for y in range(32):
        for x in range(32):
            im.putpixel((x, y), a if ((x // 8) + (y // 8)) % 2 == 0 else b)
    im.save(TILES + "/floor_checkered.png")


def floor_parquet():
    """floor_wood tarzinda, 8x8 bloklarda sepet orgusu parke."""
    dark = (122, 82, 46, 255)
    base = (186, 141, 96, 255)
    hi = (201, 158, 114, 255)
    rng = random.Random(7)
    im = new_img(32, 32)
    for y in range(32):
        for x in range(32):
            bx, by = x // 8, y // 8
            vertical = (bx + by) % 2 == 0
            edge = (x % 8 == 0) if vertical else (y % 8 == 0)
            c = dark if edge else base
            if not edge and rng.random() < 0.06:
                c = hi if rng.random() < 0.5 else dark
            im.putpixel((x, y), c)
    im.save(TILES + "/floor_parquet.png")


# --------------------------------------------------- sakin zeminler (2. tur)
def floor_cream():
    """Neredeyse duz krem; cok dusuk kontrastli iki tonlu doku."""
    rng = random.Random(11)
    c1 = (240, 234, 218, 255)
    c2 = (244, 239, 226, 255)
    im = new_img(32, 32)
    for y in range(32):
        for x in range(32):
            im.putpixel((x, y), c1 if rng.random() < 0.5 else c2)
    im.save(TILES + "/floor_cream.png")


def floor_tile_softgray():
    """floor_tile_light derz yapisi, derz ve fayans arasi cok az fark."""
    grout = (222, 222, 217, 255)
    tile = (233, 233, 229, 255)
    im = new_img(32, 32)
    for y in range(32):
        for x in range(32):
            im.putpixel((x, y), grout if x % 8 == 0 or y % 8 == 0 else tile)
    im.save(TILES + "/floor_tile_softgray.png")


def floor_wood_pale():
    """Soluk ahsap; 8 pikselde bir ince derz, seyrek ve silik damar."""
    grout = (214, 200, 178, 255)
    base = (226, 213, 192, 255)
    grain = (219, 206, 185, 255)
    rng = random.Random(23)
    im = new_img(32, 32)
    for y in range(32):
        for x in range(32):
            c = grout if y % 8 == 0 else base
            if c == base and rng.random() < 0.05:
                c = grain
            im.putpixel((x, y), c)
    im.save(TILES + "/floor_wood_pale.png")


# ---------------------------------------------------------------- halilar
def carpet(name, c1, c2):
    """floor_carpet_blue ile ayni iki tonlu doku gurultusu."""
    rng = random.Random(hash(name) & 0xFFFF)
    im = new_img(32, 32)
    for y in range(32):
        for x in range(32):
            im.putpixel((x, y), c1 if rng.random() < 0.5 else c2)
    im.save(TILES + "/" + name + ".png")


# ---------------------------------------------------------------- karakterler
SKIN = (240, 200, 160)
SKIN_DARK = (210, 165, 125)   # agiz
EYE = (43, 43, 58)


def draw_head_down(px, hair, long_hair=True):
    def box(x0, y0, x1, y1, c):
        for yy in range(y0, y1 + 1):
            for xx in range(x0, x1 + 1):
                px[xx, yy] = c
    box(11, 3, 20, 6, hair)                    # ust sac
    if long_hair:
        box(11, 7, 12, 13, hair)               # sol perde
        box(19, 7, 20, 13, hair)               # sag perde
        box(13, 7, 18, 12, SKIN)               # yuz
    else:
        box(11, 7, 12, 9, hair)
        box(19, 7, 20, 9, hair)
        box(13, 7, 18, 12, SKIN)
    px[13, 9] = EYE                             # gozler
    px[18, 9] = EYE
    px[15, 11] = SKIN_DARK                      # agiz


def draw_head_up(px, hair, long_hair=True):
    bottom = 13 if long_hair else 12
    for yy in range(3, bottom + 1):
        for xx in range(11, 21):
            px[xx, yy] = hair


def draw_head_side(px, hair, long_hair=True, mirror=False):
    def put(x, y, c):
        px[31 - x if mirror else x, y] = c

    def box(x0, y0, x1, y1, c):
        for yy in range(y0, y1 + 1):
            for xx in range(x0, x1 + 1):
                put(xx, yy, c)
    box(12, 3, 20, 3, hair)
    box(12, 4, 20, 5, hair); box(11, 4, 11, 5, SKIN)
    box(14, 6, 20, 8, hair); box(11, 6, 13, 8, SKIN)
    if long_hair:
        box(16, 9, 20, 13, hair)
        box(11, 9, 15, 12, SKIN)
    else:
        box(14, 9, 20, 9, hair)
        box(11, 9, 13, 9, SKIN); box(15, 9, 20, 9, SKIN)
        box(11, 10, 20, 12, SKIN)
    put(12, 9, EYE)


def draw_body(px, outline, top, hand=SKIN):
    def box(x0, y0, x1, y1, c):
        for yy in range(y0, y1 + 1):
            for xx in range(x0, x1 + 1):
                px[xx, yy] = c
    box(10, 14, 21, 14, outline)
    for yy in range(15, 23):
        box(8, yy, 10, yy, outline)
        box(11, yy, 20, yy, top)
        box(21, yy, 23, yy, outline)
    px[9, 23] = hand
    box(10, 23, 10, 23, outline)
    box(11, 23, 20, 23, top)
    px[21, 23] = outline
    px[22, 23] = hand
    box(10, 24, 10, 24, outline)
    box(11, 24, 20, 24, top)
    px[21, 24] = outline
    box(10, 25, 21, 25, outline)


def draw_body_side(px, outline, top, hand=SKIN, mirror=False):
    def put(x, y, c):
        px[31 - x if mirror else x, y] = c

    def box(x0, y0, x1, y1, c):
        for yy in range(y0, y1 + 1):
            for xx in range(x0, x1 + 1):
                put(xx, yy, c)
    box(10, 14, 21, 14, outline)
    for yy in range(15, 23):
        box(10, yy, 12, yy, outline)
        box(13, yy, 19, yy, top)
        put(20, yy, outline)
    put(10, 23, outline); put(11, 23, hand)
    box(12, 23, 19, 23, top); put(20, 23, outline)
    put(10, 24, outline); box(11, 24, 19, 24, top); put(20, 24, outline)
    box(10, 25, 21, 25, outline)


def draw_legs_pants(px, pants, shoe, walk=False):
    def box(x0, y0, x1, y1, c):
        for yy in range(y0, y1 + 1):
            for xx in range(x0, x1 + 1):
                px[xx, yy] = c
    if not walk:
        box(12, 26, 14, 34, pants)
        box(17, 26, 19, 34, pants)
        box(12, 35, 14, 36, shoe)
        box(17, 35, 19, 36, shoe)
    else:
        box(12, 26, 14, 33, pants)
        box(17, 27, 19, 34, pants)
        box(12, 34, 14, 35, shoe)
        box(17, 35, 19, 36, shoe)


def draw_legs_skirt(px, skirt, legs, shoe, walk=False):
    def box(x0, y0, x1, y1, c):
        for yy in range(y0, y1 + 1):
            for xx in range(x0, x1 + 1):
                px[xx, yy] = c
    box(11, 26, 20, 27, skirt)
    box(10, 28, 21, 29, skirt)
    if not walk:
        box(12, 30, 14, 34, legs)
        box(17, 30, 19, 34, legs)
        box(12, 35, 14, 36, shoe)
        box(17, 35, 19, 36, shoe)
    else:
        box(12, 30, 14, 33, legs)
        box(17, 30, 19, 34, legs)
        box(12, 34, 14, 35, shoe)
        box(17, 35, 19, 36, shoe)


def make_character(name, hair, top, outline, bottom, shoe, skirt):
    legs = bottom if skirt else bottom
    for direction in ("down", "up", "left", "right"):
        for motion in ("idle", "walk"):
            walk = motion == "walk"
            im = new_img(32, 40)
            px = im.load()
            mirror = direction == "right"
            if direction == "down":
                draw_head_down(px, hair)
                draw_body(px, outline, top)
            elif direction == "up":
                draw_head_up(px, hair)
                draw_body(px, outline, top)
            else:
                draw_head_side(px, hair, mirror=mirror)
                draw_body_side(px, outline, top, mirror=mirror)
            if skirt:
                draw_legs_skirt(px, bottom, SKIN, shoe, walk)
            else:
                draw_legs_pants(px, bottom, shoe, walk)
            im.save("%s/%s_%s_%s.png" % (CHARS, name, direction, motion))


# ---------------------------------------------------------------- uret
if __name__ == "__main__":
    floor_tile_beige()
    floor_checkered()
    floor_parquet()

    # 2. tur: sakin renkli/desensiz zeminler
    floor_cream()
    floor_tile_softgray()
    floor_wood_pale()

    carpet("floor_carpet_red", (150, 70, 70, 255), (166, 82, 82, 255))
    carpet("floor_carpet_purple", (118, 80, 148, 255), (132, 94, 164, 255))
    carpet("floor_carpet_orange", (188, 118, 58, 255), (203, 133, 73, 255))

    make_character(
        name="businesswoman",
        hair=(90, 55, 30),            # kahverengi uzun sac
        top=(72, 72, 88),             # koyu blazer
        outline=(48, 48, 60),
        bottom=(56, 56, 70),          # etek
        shoe=(40, 40, 46),
        skirt=True,
    )
    make_character(
        name="designer",
        hair=(28, 28, 34),            # siyah uzun sac
        top=(64, 158, 146),           # turkuaz bluz
        outline=(44, 112, 103),
        bottom=(58, 58, 68),          # pantolon
        shoe=(44, 44, 52),
        skirt=False,
    )
    make_character(
        name="hr_specialist",
        hair=(212, 176, 92),          # sari uzun sac
        top=(152, 72, 108),           # bordo bluz
        outline=(108, 50, 76),
        bottom=(54, 62, 92),          # lacivert etek
        shoe=(70, 50, 40),
        skirt=True,
    )

    print("Tamam: 6 zemin, 3 hali, 3 kadin karakter (24 sprite) uretildi.")
