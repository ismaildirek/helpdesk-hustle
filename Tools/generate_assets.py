#!/usr/bin/env python3
"""Generates 2D top-down pixel-art office/IT assets into Assets/Art.

Style: 32px base grid, clean outline, colors matched to the existing
office sprite sheet (Assets/sprites/offiice).
"""
import os
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, "..", "Assets", "Art"))
T = 32  # base tile size

# ---------------------------------------------------------------- palette
def rgb(r, g, b, a=255):
    return (r, g, b, a)

OUT      = rgb(43, 43, 58)          # outline
WOOD     = rgb(217, 160, 102)
WOOD_D   = rgb(143, 91, 51)
WOOD_L   = rgb(233, 190, 138)
METAL    = rgb(154, 160, 168)
METAL_D  = rgb(90, 96, 108)
METAL_L  = rgb(209, 213, 219)
DARK     = rgb(47, 53, 66)
DARK_D   = rgb(30, 34, 44)
SCREEN   = rgb(96, 165, 250)
SCREEN_D = rgb(30, 41, 59)
GREEN    = rgb(74, 222, 128)
GREEN_D  = rgb(22, 163, 74)
BSOD     = rgb(37, 99, 235)
WALL     = rgb(232, 224, 208)
WALL_D   = rgb(196, 186, 166)
WHITE    = rgb(245, 245, 240)
BLACK    = rgb(20, 20, 28)
RED      = rgb(220, 60, 60)
RED_D    = rgb(160, 40, 40)
BLUE     = rgb(70, 120, 200)
BLUE_D   = rgb(45, 85, 160)
YELLOW   = rgb(240, 200, 60)
ORANGE   = rgb(240, 150, 60)
TEAL     = rgb(60, 180, 170)
PURPLE   = rgb(150, 100, 200)
SKIN     = rgb(240, 200, 160)
SKIN_D   = rgb(210, 165, 125)
SHADOW   = rgb(20, 20, 40, 60)

BOOK_COLORS = [RED, BLUE, GREEN_D, YELLOW, PURPLE, ORANGE, TEAL]

# ---------------------------------------------------------------- helpers
def canvas(w=T, h=T):
    return Image.new("RGBA", (w, h), (0, 0, 0, 0))

def R(img, x0, y0, x1, y1, c):
    ImageDraw.Draw(img).rectangle([x0, y0, x1, y1], fill=c)

def OR(img, x0, y0, x1, y1, c):
    d = ImageDraw.Draw(img)
    d.rectangle([x0, y0, x1, y1], outline=c)

def px(img, x, y, c):
    if 0 <= x < img.width and 0 <= y < img.height:
        img.putpixel((x, y), c)

def shadow(img, cx, cy, rx, ry):
    """Soft ellipse shadow, only on transparent pixels."""
    for y in range(cy - ry, cy + ry + 1):
        for x in range(cx - rx, cx + rx + 1):
            if rx > 0 and ry > 0 and ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 <= 1:
                if 0 <= x < img.width and 0 <= y < img.height:
                    if img.getpixel((x, y))[3] == 0:
                        img.putpixel((x, y), SHADOW)

OUT_DIR = ""
def save(img, name):
    path = os.path.join(ROOT, OUT_DIR)
    os.makedirs(path, exist_ok=True)
    img.save(os.path.join(path, name + ".png"))

# ---------------------------------------------------------------- tiles
def floor_carpet(base, dot):
    img = canvas()
    R(img, 0, 0, 31, 31, base)
    for y in range(0, 32, 4):
        for x in range(0, 32, 4):
            px(img, x + (y % 8) // 2, y, dot)
            px(img, x + 2 - (y % 8) // 2, y + 2, dot)
    return img

def floor_tile(base, grout, subs=2):
    img = canvas()
    R(img, 0, 0, 31, 31, base)
    step = 32 // subs
    for i in range(subs):
        R(img, i * step, 0, i * step, 31, grout)
        R(img, 0, i * step, 31, i * step, grout)
    return img

def floor_wood():
    img = canvas()
    R(img, 0, 0, 31, 31, rgb(200, 155, 106))
    for y in (0, 8, 16, 24):
        R(img, 0, y, 31, y, WOOD_D)
    for y, jx in ((4, 10), (12, 22), (20, 6), (28, 18)):
        R(img, jx, y - 3, jx, y + 3, WOOD_D)
    for y in range(1, 32):
        if y % 8:
            px(img, (y * 7) % 31, y, rgb(215, 172, 125))
    return img

def wall_tile():
    img = canvas()
    R(img, 0, 0, 31, 31, WALL)
    R(img, 0, 0, 31, 2, rgb(245, 240, 228))   # top highlight
    R(img, 0, 26, 31, 26, WALL_D)             # baseboard
    R(img, 0, 27, 31, 31, METAL_L)
    R(img, 0, 31, 31, 31, METAL_D)
    return img

def wall_window():
    img = wall_tile()
    R(img, 3, 5, 28, 20, METAL_L)
    R(img, 4, 6, 27, 19, rgb(150, 200, 230))
    R(img, 4, 6, 27, 10, rgb(180, 220, 245))  # sky light
    R(img, 15, 6, 16, 19, METAL_L)            # mullion
    R(img, 6, 8, 10, 14, rgb(220, 240, 250))  # glare
    R(img, 2, 21, 29, 23, WALL_D)             # sill
    return img

def door(closed=True):
    img = wall_tile()
    R(img, 5, 1, 26, 31, WOOD_D)              # frame
    R(img, 6, 2, 25, 31, WOOD)
    R(img, 8, 4, 23, 14, WOOD_L)              # upper panel
    OR(img, 8, 4, 23, 14, WOOD_D)
    R(img, 8, 17, 23, 28, WOOD_L)
    OR(img, 8, 17, 23, 28, WOOD_D)
    R(img, 22, 15, 23, 17, METAL_D)           # handle
    if not closed:
        # ajar: dark opening on the left
        R(img, 6, 2, 12, 31, DARK_D)
        R(img, 13, 2, 25, 31, WOOD)
        OR(img, 13, 2, 25, 31, WOOD_D)
    return img

def make_tiles():
    global OUT_DIR
    OUT_DIR = "Tiles"
    save(floor_carpet(rgb(91, 127, 166), rgb(80, 114, 150)), "floor_carpet_blue")
    save(floor_carpet(rgb(138, 143, 152), rgb(124, 129, 138)), "floor_carpet_gray")
    save(floor_carpet(rgb(120, 160, 130), rgb(106, 146, 116)), "floor_carpet_green")
    save(floor_tile(rgb(232, 232, 226), rgb(200, 200, 192)), "floor_tile_light")
    save(floor_tile(rgb(58, 65, 82), rgb(44, 50, 64)), "floor_tile_dark")
    save(floor_wood(), "floor_wood")
    save(wall_tile(), "wall")
    save(wall_window(), "wall_window")
    save(door(True), "door_closed")
    save(door(False), "door_open")

# ---------------------------------------------------------------- furniture
def desk_base(w=64, h=32):
    img = canvas(w, h)
    shadow(img, w // 2, h - 3, w // 2 - 2, 3)
    R(img, 1, 4, w - 2, 14, WOOD)             # top surface
    OR(img, 1, 4, w - 2, 14, WOOD_D)
    R(img, 2, 5, w - 3, 8, WOOD_L)            # light edge
    R(img, 1, 15, w - 2, 17, WOOD_D)          # front edge
    R(img, 2, 18, 6, 27, WOOD_D)              # legs
    R(img, w - 7, 18, w - 3, 27, WOOD_D)
    return img

def monitor_mini(img, cx, cy, screen=SCREEN):
    R(img, cx - 7, cy - 6, cx + 7, cy + 3, DARK)   # bezel
    R(img, cx - 6, cy - 5, cx + 6, cy + 2, screen)
    R(img, cx - 2, cy + 4, cx + 2, cy + 6, METAL_D)  # stand
    R(img, cx - 5, cy + 6, cx + 5, cy + 7, DARK)

def keyboard_mini(img, x, y, w=14):
    R(img, x, y, x + w, y + 3, METAL_L)
    OR(img, x, y, x + w, y + 3, METAL_D)
    for i in range(1, w, 3):
        px(img, x + i, y + 1, METAL_D)

def desk_monitor():
    img = desk_base()
    monitor_mini(img, 32, 9)
    keyboard_mini(img, 24, 16)
    R(img, 44, 16, 48, 19, WHITE)             # papers
    return img

def desk_dual():
    img = desk_base()
    monitor_mini(img, 20, 9)
    monitor_mini(img, 44, 9, screen=SCREEN_D)
    for gx in range(39, 50, 2):               # code lines on dark screen
        px(img, gx, 6, GREEN); px(img, gx, 8, GREEN)
    keyboard_mini(img, 20, 16)
    keyboard_mini(img, 40, 16)
    return img

def desk_laptop():
    img = desk_base()
    R(img, 25, 4, 39, 11, DARK)               # laptop lid
    R(img, 26, 5, 38, 10, SCREEN)
    R(img, 23, 12, 41, 15, METAL_L)           # base
    OR(img, 23, 12, 41, 15, METAL_D)
    R(img, 48, 13, 52, 18, WHITE)             # mug
    px(img, 53, 15, WHITE)
    return img

def desk_plain():
    img = desk_base()
    R(img, 10, 7, 20, 12, WHITE)              # papers
    R(img, 44, 6, 52, 12, METAL)              # phone
    px(img, 45, 7, DARK)
    return img

def chair_base():
    """Office chair seen from top, backrest at bottom (facing up)."""
    img = canvas()
    shadow(img, 16, 17, 11, 9)
    R(img, 8, 6, 23, 18, BLUE_D)              # seat
    OR(img, 8, 6, 23, 18, OUT)
    R(img, 9, 7, 22, 12, BLUE)
    R(img, 6, 20, 25, 27, BLUE)               # backrest
    OR(img, 6, 20, 25, 27, OUT)
    R(img, 7, 21, 24, 23, rgb(90, 140, 210))
    R(img, 4, 8, 6, 17, METAL_D)              # armrests
    R(img, 25, 8, 27, 17, METAL_D)
    return img

def server_rack(lights_offset=0):
    img = canvas(32, 64)
    shadow(img, 16, 60, 13, 3)
    R(img, 3, 2, 28, 59, DARK)
    OR(img, 3, 2, 28, 59, DARK_D)
    R(img, 4, 3, 27, 5, METAL_D)              # top vent
    leds = [GREEN, YELLOW, GREEN, RED, GREEN, BLUE, GREEN, GREEN]
    for row in range(7):
        y = 7 + row * 8
        R(img, 5, y, 26, y + 6, DARK_D)       # unit
        OR(img, 5, y, 26, y + 6, METAL_D)
        for vx in range(7, 20, 2):            # vents
            px(img, vx, y + 2, rgb(55, 62, 78))
            px(img, vx, y + 4, rgb(55, 62, 78))
        px(img, 23, y + 2, leds[(row + lights_offset) % 8])
        px(img, 25, y + 4, leds[(row * 3 + lights_offset) % 8])
    return img

def bookshelf():
    img = canvas(64, 32)
    shadow(img, 32, 29, 29, 2)
    R(img, 1, 2, 62, 27, WOOD_D)
    R(img, 3, 4, 60, 25, WOOD)
    for sy in (5, 14):                        # shelves of books
        x = 4
        i = 0
        while x < 56:
            bw = 3 + (i % 2)
            R(img, x, sy + (8 - min(8, 5 + i % 4)), x + bw, sy + 8,
              BOOK_COLORS[i % len(BOOK_COLORS)])
            x += bw + 1
            i += 1
        R(img, 3, sy + 9, 60, sy + 10, WOOD_D)
    R(img, 1, 26, 62, 27, OUT)
    return img

def filing_cabinet():
    img = canvas()
    shadow(img, 16, 29, 11, 2)
    R(img, 7, 3, 24, 28, METAL)
    OR(img, 7, 3, 24, 28, METAL_D)
    for i, y in enumerate((5, 12, 19)):
        R(img, 9, y, 22, y + 5, METAL_L)
        OR(img, 9, y, 22, y + 5, METAL_D)
        R(img, 13, y + 2, 18, y + 3, METAL_D)  # handle
    return img

def printer():
    img = canvas()
    shadow(img, 16, 27, 12, 3)
    R(img, 5, 10, 26, 24, METAL_L)
    OR(img, 5, 10, 26, 24, METAL_D)
    R(img, 7, 6, 24, 9, METAL)                # top tray
    OR(img, 7, 6, 24, 9, METAL_D)
    R(img, 9, 2, 22, 5, WHITE)                # paper in
    R(img, 8, 25, 23, 28, WHITE)              # paper out
    OR(img, 8, 25, 23, 28, METAL_D)
    px(img, 24, 12, GREEN)                    # LED
    R(img, 6, 12, 20, 14, METAL)              # slot
    return img

def coffee_machine():
    img = canvas()
    shadow(img, 16, 28, 11, 3)
    R(img, 8, 3, 23, 27, DARK)
    OR(img, 8, 3, 23, 27, OUT)
    R(img, 10, 5, 21, 10, METAL_D)            # tank
    R(img, 10, 12, 21, 15, METAL)             # band
    px(img, 20, 13, RED)                      # LED
    R(img, 12, 17, 19, 19, METAL_L)           # spout plate
    R(img, 13, 20, 18, 25, WHITE)             # cup
    OR(img, 13, 20, 18, 25, METAL_D)
    R(img, 14, 21, 17, 22, rgb(120, 72, 40))  # coffee
    return img

def water_cooler():
    img = canvas()
    shadow(img, 16, 28, 10, 3)
    R(img, 10, 2, 21, 12, rgb(140, 200, 240))  # bottle
    OR(img, 10, 2, 21, 12, rgb(90, 150, 200))
    R(img, 12, 3, 19, 5, rgb(180, 225, 250))
    R(img, 9, 13, 22, 28, WHITE)
    OR(img, 9, 13, 22, 28, METAL_D)
    R(img, 11, 16, 20, 18, METAL_L)
    px(img, 12, 21, BLUE); px(img, 19, 21, RED)  # taps
    return img

def vending_machine():
    img = canvas(32, 48)
    shadow(img, 16, 44, 12, 3)
    R(img, 4, 2, 27, 43, RED_D)
    OR(img, 4, 2, 27, 43, OUT)
    R(img, 6, 4, 19, 30, rgb(200, 220, 235))   # window
    OR(img, 6, 4, 19, 30, OUT)
    for row in range(4):                       # snacks
        for col in range(3):
            x = 7 + col * 4
            y = 6 + row * 7
            R(img, x, y, x + 2, y + 3, BOOK_COLORS[(row + col) % 7])
    R(img, 21, 5, 25, 15, DARK)                # panel
    px(img, 23, 7, GREEN)
    R(img, 21, 18, 25, 22, METAL_D)            # coin slot
    R(img, 6, 33, 25, 40, DARK_D)              # pickup
    return img

def plant(big=True):
    img = canvas()
    shadow(img, 16, 27, 9, 3)
    R(img, 11, 20, 20, 28, rgb(190, 110, 70))  # pot
    OR(img, 11, 20, 20, 28, WOOD_D)
    R(img, 10, 19, 21, 21, WOOD_D)
    leaves = [(16, 6), (11, 10), (21, 10), (13, 14), (19, 14), (16, 12)] if big \
        else [(16, 12), (12, 15), (20, 15)]
    for lx, ly in leaves:
        R(img, lx - 3, ly - 2, lx + 3, ly + 2, GREEN_D)
        R(img, lx - 2, ly - 3, lx + 2, ly + 1, GREEN)
    return img

def trash_bin(recycle=None):
    img = canvas()
    shadow(img, 16, 26, 8, 3)
    body = METAL if recycle is None else recycle
    R(img, 10, 9, 21, 26, body)
    OR(img, 10, 9, 21, 26, METAL_D if recycle is None else OUT)
    R(img, 9, 7, 22, 10, METAL_D if recycle is None else OUT)  # rim
    for x in (13, 16, 19):
        R(img, x, 12, x, 24, METAL_D if recycle is None else rgb(255, 255, 255, 90))
    if recycle:
        px(img, 15, 15, WHITE); px(img, 16, 15, WHITE)
        px(img, 14, 17, WHITE); px(img, 17, 17, WHITE)
        px(img, 15, 19, WHITE); px(img, 16, 19, WHITE)
    return img

def whiteboard(with_diagram=False):
    img = canvas(64, 32)
    shadow(img, 32, 28, 28, 3)
    R(img, 2, 2, 61, 25, METAL_D)              # frame
    R(img, 4, 4, 59, 23, WHITE)
    if with_diagram:
        R(img, 8, 8, 20, 14, rgb(240, 240, 235))
        OR(img, 8, 8, 20, 14, RED)
        R(img, 30, 6, 44, 12, rgb(240, 240, 235))
        OR(img, 30, 6, 44, 12, BLUE)
        R(img, 30, 15, 44, 20, rgb(240, 240, 235))
        OR(img, 30, 15, 44, 20, GREEN_D)
        px(img, 25, 11, OUT); px(img, 26, 11, OUT); px(img, 27, 9, OUT)  # arrows
        px(img, 37, 13, OUT); px(img, 37, 14, OUT)
        R(img, 50, 7, 56, 7, RED)              # scribbles
        R(img, 50, 10, 57, 10, BLUE)
        R(img, 50, 13, 55, 13, GREEN_D)
    else:
        R(img, 8, 8, 30, 8, rgb(200, 200, 200))
        R(img, 8, 11, 40, 11, rgb(200, 200, 200))
        R(img, 8, 14, 25, 14, rgb(200, 200, 200))
    R(img, 20, 24, 44, 25, METAL_L)            # tray
    px(img, 24, 24, RED); px(img, 30, 24, BLUE)
    R(img, 12, 26, 15, 29, METAL_D)            # legs
    R(img, 48, 26, 51, 29, METAL_D)
    return img

def meeting_table():
    img = canvas(64, 64)
    shadow(img, 32, 34, 28, 26)
    R(img, 6, 6, 57, 57, WOOD)
    OR(img, 6, 6, 57, 57, WOOD_D)
    R(img, 8, 8, 55, 12, WOOD_L)
    OR(img, 10, 10, 53, 53, WOOD_D)
    R(img, 28, 28, 35, 35, METAL_L)            # speaker phone
    OR(img, 28, 28, 35, 35, METAL_D)
    for dx, dy in ((30, 30), (33, 30), (30, 33), (33, 33)):
        px(img, dx, dy, METAL_D)
    R(img, 14, 16, 22, 21, WHITE)              # papers
    R(img, 44, 40, 52, 45, WHITE)
    R(img, 45, 15, 50, 20, WHITE)              # mug
    return img

def couch():
    img = canvas(64, 32)
    shadow(img, 32, 25, 28, 5)
    R(img, 4, 6, 59, 24, BLUE_D)
    OR(img, 4, 6, 59, 24, OUT)
    R(img, 7, 9, 30, 21, BLUE)                 # cushion L
    R(img, 33, 9, 56, 21, BLUE)                # cushion R
    OR(img, 7, 9, 30, 21, BLUE_D)
    OR(img, 33, 9, 56, 21, BLUE_D)
    R(img, 1, 4, 6, 26, BLUE_D)                # armrests
    R(img, 57, 4, 62, 26, BLUE_D)
    OR(img, 1, 4, 6, 26, OUT)
    OR(img, 57, 4, 62, 26, OUT)
    R(img, 4, 2, 59, 6, BLUE_D)                # back
    return img

def reception_desk():
    img = canvas(64, 48)
    shadow(img, 32, 42, 28, 4)
    R(img, 3, 6, 60, 16, WOOD)                 # counter top
    OR(img, 3, 6, 60, 16, WOOD_D)
    R(img, 4, 7, 59, 10, WOOD_L)
    R(img, 3, 17, 60, 40, WOOD_D)              # front panel
    R(img, 5, 19, 58, 38, rgb(120, 75, 42))
    for x in range(10, 58, 10):
        R(img, x, 20, x + 1, 37, WOOD_D)
    R(img, 3, 40, 60, 41, OUT)
    monitor_mini(img, 20, 10)
    return img

def wall_clock():
    img = canvas()
    R(img, 8, 6, 23, 21, WHITE)
    ImageDraw.Draw(img).ellipse([7, 5, 24, 22], outline=OUT, width=2)
    px(img, 15, 13, OUT); px(img, 16, 13, OUT)
    R(img, 15, 9, 15, 13, OUT)                 # hour hand... vertical
    R(img, 16, 13, 20, 13, OUT)                # minute hand
    px(img, 15, 6, OUT); px(img, 15, 20, OUT)
    px(img, 9, 13, OUT); px(img, 22, 13, OUT)
    return img

def bulletin_board():
    img = canvas(48, 32)
    R(img, 2, 3, 45, 26, WOOD_D)
    R(img, 4, 5, 43, 24, rgb(210, 170, 120))   # cork
    notes = [(7, 8, YELLOW), (16, 7, WHITE), (26, 9, rgb(255, 160, 160)),
             (35, 8, YELLOW), (10, 16, rgb(160, 220, 160)), (22, 15, WHITE),
             (33, 16, YELLOW)]
    for nx, ny, c in notes:
        R(img, nx, ny, nx + 6, ny + 5, c)
        px(img, nx + 3, ny, RED)               # pin
    return img

def wifi_router():
    img = canvas()
    shadow(img, 16, 24, 10, 3)
    R(img, 8, 14, 23, 21, DARK)
    OR(img, 8, 14, 23, 21, OUT)
    R(img, 10, 5, 11, 13, METAL_D)             # antennas
    R(img, 20, 5, 21, 13, METAL_D)
    px(img, 11, 17, GREEN); px(img, 14, 17, GREEN); px(img, 17, 17, YELLOW)
    px(img, 20, 17, BLUE)
    R(img, 10, 22, 21, 23, METAL_D)
    return img

def toolbox():
    img = canvas()
    shadow(img, 16, 25, 11, 3)
    R(img, 6, 12, 25, 24, RED)
    OR(img, 6, 12, 25, 24, RED_D)
    R(img, 6, 12, 25, 15, RED_D)
    R(img, 12, 8, 19, 11, RED_D)               # handle
    R(img, 13, 9, 18, 10, rgb(200, 200, 200))
    R(img, 14, 16, 17, 19, METAL_L)            # latch
    return img

def partition():
    """Cubicle partition segment, 32x32."""
    img = canvas()
    R(img, 1, 2, 30, 10, rgb(150, 155, 165))
    OR(img, 1, 2, 30, 10, METAL_D)
    R(img, 1, 3, 30, 4, rgb(170, 175, 185))
    R(img, 2, 11, 4, 14, METAL_D)              # feet
    R(img, 27, 11, 29, 14, METAL_D)
    return img

def make_furniture():
    global OUT_DIR
    OUT_DIR = "Furniture"
    save(desk_plain(), "desk")
    save(desk_monitor(), "desk_monitor")
    save(desk_dual(), "desk_dual_monitor")
    save(desk_laptop(), "desk_laptop")
    ch = chair_base()
    save(ch, "chair_up")
    save(ch.rotate(180), "chair_down")
    save(ch.rotate(90, expand=True), "chair_left")
    save(ch.rotate(-90, expand=True), "chair_right")
    save(server_rack(0), "server_rack_a")
    save(server_rack(3), "server_rack_b")
    save(bookshelf(), "bookshelf")
    save(filing_cabinet(), "filing_cabinet")
    save(printer(), "printer")
    save(coffee_machine(), "coffee_machine")
    save(water_cooler(), "water_cooler")
    save(vending_machine(), "vending_machine")
    save(plant(True), "plant_big")
    save(plant(False), "plant_small")
    save(trash_bin(), "trash_bin")
    save(trash_bin(GREEN_D), "recycle_bin")
    save(whiteboard(False), "whiteboard")
    save(whiteboard(True), "whiteboard_diagram")
    save(meeting_table(), "meeting_table")
    save(couch(), "couch")
    save(reception_desk(), "reception_desk")
    save(wall_clock(), "wall_clock")
    save(bulletin_board(), "bulletin_board")
    save(wifi_router(), "wifi_router")
    save(toolbox(), "toolbox")
    save(partition(), "partition")

# ---------------------------------------------------------------- props
def monitor_prop(mode="on"):
    img = canvas()
    shadow(img, 16, 26, 12, 3)
    R(img, 5, 5, 26, 20, DARK)                 # bezel
    OR(img, 5, 5, 26, 20, OUT)
    if mode == "on":
        R(img, 6, 6, 25, 19, SCREEN_D)
        rows = [(7, 12, GREEN), (9, 18, SCREEN), (11, 10, GREEN),
                (13, 20, YELLOW), (15, 14, SCREEN), (17, 8, GREEN)]
        for ry, rw, c in rows:
            R(img, 8, ry, 8 + rw, ry, c)
    elif mode == "error":
        R(img, 6, 6, 25, 19, BSOD)
        R(img, 8, 8, 14, 10, WHITE)            # sad face :(
        px(img, 9, 8, BSOD); px(img, 12, 8, BSOD)
        R(img, 8, 13, 22, 13, WHITE)
        R(img, 8, 15, 18, 15, WHITE)
    else:
        R(img, 6, 6, 25, 19, rgb(20, 24, 32))
        px(img, 24, 18, rgb(60, 70, 90))
    R(img, 13, 21, 18, 23, METAL_D)            # stand
    R(img, 10, 24, 21, 26, DARK)
    return img

def keyboard_prop():
    img = canvas()
    shadow(img, 16, 18, 13, 4)
    R(img, 3, 10, 28, 21, METAL_L)
    OR(img, 3, 10, 28, 21, METAL_D)
    for ry in (12, 15, 18):
        for rx in range(5, 26, 3):
            px(img, rx, ry, METAL_D)
    R(img, 10, 18, 21, 18, METAL_D)            # spacebar
    return img

def mouse_prop():
    img = canvas()
    ImageDraw.Draw(img).ellipse([11, 9, 20, 22], fill=METAL_L, outline=METAL_D)
    R(img, 15, 9, 16, 15, METAL_D)
    px(img, 15, 11, SCREEN)
    return img

def laptop_prop(closed=False):
    img = canvas()
    shadow(img, 16, 22, 13, 3)
    if closed:
        R(img, 6, 9, 25, 21, METAL)
        OR(img, 6, 9, 25, 21, METAL_D)
        R(img, 8, 11, 23, 13, METAL_L)
        px(img, 15, 15, DARK); px(img, 16, 15, DARK)
    else:
        R(img, 7, 4, 24, 14, DARK)
        R(img, 8, 5, 23, 13, SCREEN)
        R(img, 9, 6, 14, 8, rgb(200, 230, 255))
        R(img, 5, 15, 26, 21, METAL_L)
        OR(img, 5, 15, 26, 21, METAL_D)
        R(img, 12, 17, 19, 19, METAL)          # touchpad
    return img

def pc_tower():
    img = canvas()
    shadow(img, 16, 27, 9, 3)
    R(img, 10, 4, 22, 27, DARK)
    OR(img, 10, 4, 22, 27, OUT)
    R(img, 12, 6, 20, 9, METAL_D)              # drive bay
    px(img, 19, 12, GREEN)                     # power LED
    for y in range(16, 25, 2):
        R(img, 12, y, 20, y, rgb(60, 68, 84))  # vents
    return img

def phone_prop():
    img = canvas()
    shadow(img, 16, 21, 11, 4)
    R(img, 6, 12, 25, 22, DARK)
    OR(img, 6, 12, 25, 22, OUT)
    R(img, 8, 8, 23, 12, METAL_D)              # handset
    OR(img, 8, 8, 23, 12, OUT)
    for ry in (15, 18):
        for rx in (10, 13, 16):
            px(img, rx, ry, METAL_L)
    R(img, 19, 14, 23, 19, SCREEN_D)           # display
    px(img, 20, 15, GREEN)
    return img

def mug_prop(hot=True):
    img = canvas()
    shadow(img, 16, 22, 8, 3)
    R(img, 11, 12, 20, 23, WHITE)
    OR(img, 11, 12, 20, 23, METAL_D)
    R(img, 21, 14, 23, 19, WHITE)              # handle
    OR(img, 21, 14, 23, 19, METAL_D)
    R(img, 12, 13, 19, 15, rgb(120, 72, 40))   # coffee
    if hot:
        px(img, 13, 8, rgb(200, 200, 200, 160))
        px(img, 17, 6, rgb(200, 200, 200, 160))
        px(img, 15, 9, rgb(200, 200, 200, 160))
    return img

def papers_prop():
    img = canvas()
    shadow(img, 16, 22, 10, 4)
    R(img, 8, 8, 23, 24, METAL_L)
    OR(img, 8, 8, 23, 24, METAL_D)
    R(img, 7, 6, 22, 22, WHITE)
    OR(img, 7, 6, 22, 22, METAL_D)
    for y in (9, 12, 15, 18):
        R(img, 10, y, 19, y, rgb(180, 180, 190))
    return img

def folder_prop():
    img = canvas()
    shadow(img, 16, 21, 12, 4)
    R(img, 5, 10, 26, 22, YELLOW)
    OR(img, 5, 10, 26, 22, rgb(200, 160, 40))
    R(img, 5, 7, 14, 10, YELLOW)               # tab
    OR(img, 5, 7, 14, 10, rgb(200, 160, 40))
    R(img, 7, 12, 24, 14, rgb(250, 230, 150))
    return img

def headset_prop():
    img = canvas()
    shadow(img, 16, 22, 10, 3)
    d = ImageDraw.Draw(img)
    d.arc([7, 5, 24, 26], start=180, end=360, fill=METAL_D, width=3)
    R(img, 5, 14, 9, 21, DARK)                 # ear pads
    R(img, 22, 14, 26, 21, DARK)
    for i in range(5):                         # mic boom
        px(img, 22 - i, 20 + i, METAL_D)
    px(img, 17, 24, RED)
    return img

def make_props():
    global OUT_DIR
    OUT_DIR = "Props"
    save(monitor_prop("on"), "monitor_on")
    save(monitor_prop("off"), "monitor_off")
    save(monitor_prop("error"), "monitor_error")
    save(keyboard_prop(), "keyboard")
    save(mouse_prop(), "mouse")
    save(laptop_prop(False), "laptop_open")
    save(laptop_prop(True), "laptop_closed")
    save(pc_tower(), "pc_tower")
    save(phone_prop(), "phone_desk")
    save(mug_prop(True), "mug_hot")
    save(mug_prop(False), "mug")
    save(papers_prop(), "papers")
    save(folder_prop(), "folder")
    save(headset_prop(), "headset")

# ---------------------------------------------------------------- characters
def char_frame(shirt, shirt_d, pants, hair, direction="down", step=0,
               headset=False, tie=None, hair_top=None):
    img = canvas(32, 40)
    shadow(img, 16, 36, 8, 2)
    # legs
    ly = 26
    if step:
        R(img, 12, ly, 14, ly + 8, pants)      # left leg forward
        R(img, 17, ly + 1, 19, ly + 8, pants)
        R(img, 12, ly + 8, 14, ly + 9, DARK)
        R(img, 17, ly + 9, 19, ly + 10, DARK)
    else:
        R(img, 12, ly, 14, ly + 9, pants)
        R(img, 17, ly, 19, ly + 9, pants)
        R(img, 12, ly + 9, 14, ly + 10, DARK)
        R(img, 17, ly + 9, 19, ly + 10, DARK)
    # torso
    R(img, 10, 14, 21, 25, shirt)
    OR(img, 10, 14, 21, 25, shirt_d)
    # arms
    if direction in ("left", "right"):
        R(img, 13, 15, 18, 24, shirt)          # slimmer side torso
        if direction == "left":
            R(img, 10, 15, 12, 22, shirt_d)
            px(img, 11, 23, SKIN)
        else:
            R(img, 19, 15, 21, 22, shirt_d)
            px(img, 20, 23, SKIN)
    else:
        R(img, 8, 15, 10, 22, shirt_d)
        R(img, 21, 15, 23, 22, shirt_d)
        px(img, 9, 23, SKIN); px(img, 22, 23, SKIN)
    if tie and direction == "down":
        R(img, 15, 15, 16, 21, tie)
    # head
    R(img, 11, 4, 20, 12, SKIN)
    if direction == "up":
        R(img, 11, 3, 20, 12, hair)            # all hair from behind
    elif direction == "left":
        R(img, 12, 3, 20, 8, hair)
        R(img, 11, 6, 13, 12, SKIN)
        px(img, 12, 9, OUT)                    # eye
    elif direction == "right":
        R(img, 11, 3, 19, 8, hair)
        R(img, 18, 6, 20, 12, SKIN)
        px(img, 19, 9, OUT)
    else:
        R(img, 11, 3, 20, 6, hair_top or hair)
        R(img, 11, 6, 12, 9, hair)
        R(img, 19, 6, 20, 9, hair)
        px(img, 13, 9, OUT); px(img, 18, 9, OUT)  # eyes
        px(img, 15, 11, SKIN_D)                # mouth
    if headset and direction != "up":
        R(img, 10, 5, 21, 6, DARK)             # band
        if direction == "down":
            R(img, 19, 9, 21, 10, DARK)        # mic
            px(img, 17, 11, DARK)
        elif direction == "left":
            R(img, 11, 9, 13, 10, DARK)
        else:
            R(img, 18, 9, 20, 10, DARK)
    return img

def character_set(name, **kw):
    global OUT_DIR
    OUT_DIR = "Characters"
    for direction in ("down", "up", "left", "right"):
        for step in (0, 1):
            f = char_frame(direction=direction, step=step, **kw)
            suffix = "idle" if step == 0 else "walk"
            save(f, f"{name}_{direction}_{suffix}")

def make_characters():
    character_set("it_technician",
                  shirt=BLUE, shirt_d=BLUE_D, pants=DARK,
                  hair=rgb(90, 60, 40))
    character_set("developer",
                  shirt=rgb(70, 70, 85), shirt_d=rgb(50, 50, 62),
                  pants=rgb(70, 90, 140), hair=rgb(30, 30, 35))
    character_set("manager",
                  shirt=WHITE, shirt_d=METAL_L, pants=METAL_D,
                  hair=rgb(150, 150, 155), tie=RED)
    character_set("support_agent",
                  shirt=TEAL, shirt_d=rgb(40, 140, 130), pants=DARK,
                  hair=rgb(220, 180, 90), headset=True)

# ---------------------------------------------------------------- UI icons
def icon_circle(bg, draw_fn):
    img = canvas()
    d = ImageDraw.Draw(img)
    d.ellipse([3, 3, 28, 28], fill=bg, outline=OUT, width=2)
    draw_fn(img)
    return img

def _draw_exclaim(img):
    R(img, 14, 8, 17, 18, WHITE); R(img, 14, 21, 17, 23, WHITE)

def _draw_question(img):
    R(img, 12, 8, 19, 13, WHITE); R(img, 12, 8, 14, 15, WHITE)
    R(img, 14, 13, 17, 17, WHITE); R(img, 14, 20, 17, 22, WHITE)

def _draw_check(img):
    for i in range(5):
        px(img, 9 + i, 16 + i // 1, WHITE) if i < 3 else None
    R(img, 9, 15, 11, 18, WHITE)
    for i in range(9):
        px(img, 12 + i, 19 - i, WHITE); px(img, 12 + i, 20 - i, WHITE)

def _draw_wrench(img):
    for i in range(10):
        px(img, 9 + i, 22 - i, WHITE); px(img, 10 + i, 22 - i, WHITE)
    ImageDraw.Draw(img).ellipse([17, 6, 24, 13], outline=WHITE, width=2)
    px(img, 20, 9, BLUE)

def _draw_bug(img):
    d = ImageDraw.Draw(img)
    d.ellipse([10, 11, 21, 22], fill=DARK)     # body
    d.ellipse([12, 7, 19, 12], fill=DARK)      # head
    R(img, 15, 12, 16, 21, rgb(120, 60, 60))   # shell split
    for ly in (13, 17, 20):                    # legs
        R(img, 7, ly, 9, ly, DARK)
        R(img, 22, ly, 24, ly, DARK)
    px(img, 13, 9, WHITE); px(img, 18, 9, WHITE)  # eyes
    px(img, 12, 6, DARK); px(img, 19, 6, DARK)    # antennae

def _draw_chat(img):
    R(img, 7, 8, 24, 19, WHITE)
    OR(img, 7, 8, 24, 19, OUT)
    R(img, 10, 19, 13, 23, WHITE)
    for x in (11, 15, 19):
        px(img, x, 13, METAL_D)

def _draw_coffee(img):
    R(img, 10, 11, 21, 23, WHITE)
    OR(img, 10, 11, 21, 23, OUT)
    R(img, 22, 13, 24, 18, WHITE); OR(img, 22, 13, 24, 18, OUT)
    R(img, 11, 12, 20, 14, rgb(120, 72, 40))
    px(img, 13, 7, METAL_L); px(img, 17, 5, METAL_L)

def _draw_wifi(img):
    d = ImageDraw.Draw(img)
    d.arc([6, 8, 26, 28], start=200, end=340, fill=WHITE, width=2)
    d.arc([10, 12, 22, 26], start=210, end=330, fill=WHITE, width=2)
    px(img, 15, 21, WHITE); px(img, 16, 21, WHITE)
    px(img, 15, 22, WHITE); px(img, 16, 22, WHITE)

def _draw_ticket(img):
    R(img, 9, 7, 22, 24, WHITE)
    OR(img, 9, 7, 22, 24, OUT)
    R(img, 12, 10, 19, 12, RED)
    R(img, 12, 15, 19, 15, METAL_D)
    R(img, 12, 18, 17, 18, METAL_D)

def _draw_coin(img):
    R(img, 13, 8, 18, 8, rgb(255, 230, 120))
    R(img, 11, 10, 20, 21, rgb(255, 230, 120))
    R(img, 14, 11, 17, 19, rgb(230, 190, 50))

def make_ui():
    global OUT_DIR
    OUT_DIR = "UI"
    save(icon_circle(RED, _draw_exclaim), "icon_alert")
    save(icon_circle(BLUE, _draw_question), "icon_question")
    save(icon_circle(GREEN_D, _draw_check), "icon_check")
    save(icon_circle(BLUE, _draw_wrench), "icon_wrench")
    save(icon_circle(rgb(230, 180, 60), _draw_bug), "icon_bug")
    save(icon_circle(TEAL, _draw_chat), "icon_chat")
    save(icon_circle(rgb(150, 100, 60), _draw_coffee), "icon_coffee")
    save(icon_circle(BLUE, _draw_wifi), "icon_wifi")
    save(icon_circle(ORANGE, _draw_ticket), "icon_ticket")
    save(icon_circle(YELLOW, _draw_coin), "icon_coin")

# ---------------------------------------------------------------- main
def contact_sheet():
    files = []
    for dirpath, _, names in os.walk(ROOT):
        for n in sorted(names):
            if n.endswith(".png"):
                files.append(os.path.join(dirpath, n))
    files.sort()
    cols = 10
    rows = (len(files) + cols - 1) // cols
    cell = 72
    sheet = Image.new("RGBA", (cols * cell, rows * cell), (40, 42, 54, 255))
    for i, f in enumerate(files):
        img = Image.open(f)
        x = (i % cols) * cell + (cell - img.width) // 2
        y = (i // cols) * cell + (cell - img.height) // 2
        sheet.paste(img, (x, y), img)
    out = os.path.join(HERE, "contact_sheet.png")
    sheet.save(out)
    print(f"contact sheet: {out} ({len(files)} sprites)")

if __name__ == "__main__":
    make_tiles()
    make_furniture()
    make_props()
    make_characters()
    make_ui()
    contact_sheet()
    print("done")
