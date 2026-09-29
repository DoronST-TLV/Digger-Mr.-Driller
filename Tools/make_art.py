from PIL import Image, ImageDraw, ImageFilter
import math, os

OUT = "Assets/_Project/Art"   # run from the project root
os.makedirs(OUT, exist_ok=True)
S = 120

def new():
    return Image.new("RGBA", (S, S), (0, 0, 0, 0))

# ---- block.png: light grey bevelled tile, tinted at runtime by SpriteRenderer.color (multiply) ----
img = new(); d = ImageDraw.Draw(img)
d.rounded_rectangle((2, 2, S - 3, S - 3), radius=18, fill=(150, 150, 150, 255))            # rim (darker)
d.rounded_rectangle((7, 7, S - 8, S - 8), radius=14, fill=(228, 228, 228, 255))            # face
d.rounded_rectangle((7, 7, S - 8, S // 2), radius=14, fill=(255, 255, 255, 255))           # top half highlight
d.rounded_rectangle((7, S // 2 - 10, S - 8, S // 2 + 10), fill=(228, 228, 228, 255))       # blend the seam
d.rounded_rectangle((7, S - 34, S - 8, S - 8), radius=14, fill=(196, 196, 196, 255))       # bottom shade
d.rounded_rectangle((7, S - 40, S - 8, S - 30), fill=(228, 228, 228, 255))
# small highlight spark
d.rounded_rectangle((16, 14, 44, 24), radius=5, fill=(255, 255, 255, 255))
img.save(f"{OUT}/block.png")

# ---- hard.png: grey rock with an X, not tinted ----
img = new(); d = ImageDraw.Draw(img)
d.rounded_rectangle((2, 2, S - 3, S - 3), radius=18, fill=(62, 62, 68, 255))
d.rounded_rectangle((7, 7, S - 8, S - 8), radius=14, fill=(112, 112, 120, 255))
d.rounded_rectangle((7, 7, S - 8, 40), radius=14, fill=(130, 130, 138, 255))
d.rounded_rectangle((7, 30, S - 8, 50), fill=(112, 112, 120, 255))
for (x0, y0, x1, y1) in [(20, 70, 40, 80), (70, 22, 92, 30), (84, 84, 100, 96)]:
    d.rounded_rectangle((x0, y0, x1, y1), radius=4, fill=(90, 90, 98, 255))
d.line((30, 30, S - 30, S - 30), fill=(40, 40, 46, 255), width=16)
d.line((S - 30, 30, 30, S - 30), fill=(40, 40, 46, 255), width=16)
d.line((30, 30, S - 30, S - 30), fill=(58, 58, 64, 255), width=8)
d.line((S - 30, 30, 30, S - 30), fill=(58, 58, 64, 255), width=8)
img.save(f"{OUT}/hard.png")

# ---- capsule.png: air bubble on a transparent cell ----
img = new(); d = ImageDraw.Draw(img)
glow = new(); gd = ImageDraw.Draw(glow)
gd.ellipse((14, 14, S - 14, S - 14), fill=(46, 230, 214, 120))
glow = glow.filter(ImageFilter.GaussianBlur(8))
img.alpha_composite(glow)
d.ellipse((26, 26, S - 26, S - 26), fill=(46, 230, 214, 255), outline=(255, 255, 255, 255), width=4)
d.ellipse((38, 34, 60, 50), fill=(255, 255, 255, 210))
d.ellipse((64, 66, 76, 78), fill=(255, 255, 255, 150))
img.save(f"{OUT}/capsule.png")

# ---- player.png: a small driller ----
img = new(); d = ImageDraw.Draw(img)
ink = (20, 22, 28, 255)
# drill (behind the body, on the right, pointing down)
d.polygon([(84, 62), (112, 62), (98, 112)], fill=(150, 156, 166, 255), outline=ink)
for y in (72, 84, 96):
    w = (112 - 84) * (1 - (y - 62) / 50)
    d.line((98 - w / 2 + 2, y, 98 + w / 2 - 2, y), fill=(105, 110, 120, 255), width=3)
# boots
d.rounded_rectangle((30, 98, 56, 114), radius=6, fill=(40, 42, 50, 255), outline=ink)
d.rounded_rectangle((62, 98, 88, 114), radius=6, fill=(40, 42, 50, 255), outline=ink)
# body
d.rounded_rectangle((28, 66, 90, 104), radius=12, fill=(62, 123, 250, 255), outline=ink, width=2)
d.rectangle((28, 84, 90, 90), fill=(38, 80, 190, 255))              # belt
d.rounded_rectangle((52, 84, 66, 90), radius=2, fill=(245, 197, 24, 255))  # buckle
# face
d.rounded_rectangle((34, 38, 84, 76), radius=12, fill=(247, 212, 176, 255), outline=ink, width=2)
# goggles
d.rectangle((34, 48, 84, 52), fill=(40, 42, 50, 255))
for cx in (48, 70):
    d.ellipse((cx - 11, 40, cx + 11, 62), fill=(40, 42, 50, 255), outline=ink)
    d.ellipse((cx - 7, 44, cx + 7, 58), fill=(120, 220, 240, 255))
    d.ellipse((cx - 4, 46, cx, 50), fill=(255, 255, 255, 230))
# smile
d.arc((50, 60, 68, 72), start=10, end=170, fill=ink, width=2)
# helmet
d.chord((26, 14, 92, 62), start=180, end=360, fill=(245, 166, 35, 255), outline=ink, width=2)
d.rounded_rectangle((22, 36, 96, 46), radius=5, fill=(245, 166, 35, 255), outline=ink, width=2)
d.chord((28, 16, 90, 60), start=200, end=330, fill=(252, 196, 90, 255))
# headlamp
d.ellipse((50, 18, 68, 34), fill=(255, 240, 150, 255), outline=ink, width=2)
d.ellipse((55, 22, 61, 28), fill=(255, 255, 255, 255))
img.save(f"{OUT}/player.png")

# ---- background.png: 270 x 480, vertical gradient (PPU 30 → 9 x 16 units) ----
W, H = 270, 480
bg = Image.new("RGBA", (W, H))
top, bottom = (28, 34, 52), (11, 13, 21)
px = bg.load()
for y in range(H):
    t = y / (H - 1)
    c = tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(3)) + (255,)
    for x in range(W):
        px[x, y] = c
bd = ImageDraw.Draw(bg)
# (strata lines removed: they rendered as light gaps)
for y in []:
    bd.line((0, y, W, y), fill=(255, 255, 255, 6))
bg.save(f"{OUT}/background.png")
print("art ok")
