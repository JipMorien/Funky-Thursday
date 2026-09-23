#!/usr/bin/env python3
"""
Higgs Import - turns Higgsfield downloads into Funky Thursday pixel-art sprite sheets.

Commands (run from the Unity project root):
    python Tools/higgs_import.py base       HiggsfieldRaw/vesper_base.png --id vesper
    python Tools/higgs_import.py pose       HiggsfieldRaw/vesper_left.mp4 --id vesper --pose left
    python Tools/higgs_import.py background HiggsfieldRaw/bg_crypt-keeper.png --slug crypt-keeper
    python Tools/higgs_import.py background HiggsfieldRaw/bg_crypt-keeper_loop.mp4 --slug crypt-keeper --video

What each step does:
    base        Keys out the flat background, saves health-bar icons (normal + losing) and a one-frame idle so the
                character shows up in game immediately. Remembers the key colour for later poses.
    pose        Samples the clip (or the cells of a sprite sheet with --grid) at a fixed fps, keys every frame, crops every frame with the SAME box (measured on the
                first frame, which is the base image), downsamples to a pixel grid, shares one palette across all frames,
                adds a 1px outline and packs a sprite sheet. Fixed cropping means poses never jitter or change size.
    background  Cover-fits an image or clip to 320x180 pixels, shares one palette and packs a sheet. --video also writes
                a pixelated MP4 to StreamingAssets/Video for the optional VideoPlayer backdrop.

Every sheet gets a sidecar <name>.sheet.json that SpriteSheetPostprocessor.cs reads to slice it in Unity.

Requires: numpy, pillow, imageio[ffmpeg]      pip install numpy pillow "imageio[ffmpeg]"
"""
import argparse
import json
import math
import pathlib
import sys

import numpy as np
from PIL import Image

ROOT = pathlib.Path(__file__).resolve().parent.parent
ART = ROOT / "Assets" / "_FunkyThursday" / "Art"
VIDEO_DIR = ROOT / "Assets" / "StreamingAssets" / "Video"

INK = (0x12, 0x0A, 0x14)
IMAGE_EXTS = {".png", ".jpg", ".jpeg", ".webp", ".bmp"}

# Characters: body height in pixels and the world height it should occupy in Unity.
CHAR_HEIGHT_PX = 96
CHAR_WORLD_HEIGHT = 4.5
FEET_MARGIN_PX = 4
ICON_PX = 32

BG_SIZE = (320, 180)
BG_WORLD_HEIGHT = 10.0

POSE_DEFAULTS = {
    "idle": dict(start=0.0, end=None, fps=12, loop=True),
    "left": dict(start=0.0, end=0.75, fps=12, loop=False),
    "down": dict(start=0.0, end=0.75, fps=12, loop=False),
    "up": dict(start=0.0, end=0.75, fps=12, loop=False),
    "right": dict(start=0.0, end=0.75, fps=12, loop=False),
    "miss": dict(start=0.0, end=0.75, fps=12, loop=False),
}


# ── Reading ───────────────────────────────────────────────────────────────────

def has_transparency(image):
    return image.shape[2] == 4 and (image[..., 3] < 128).mean() > 0.05


def split_grid(image, grid):
    """Splits a sprite sheet (e.g. from Higgsfield AutoSprite) into cells, skipping empty ones."""
    try:
        columns, rows = (int(v) for v in grid.lower().split("x"))
    except ValueError:
        sys.exit("--grid must look like 5x5 (columns x rows)")
    h, w = image.shape[0] // rows, image.shape[1] // columns
    cells = [image[r * h:(r + 1) * h, c * w:(c + 1) * w] for r in range(rows) for c in range(columns)]
    cells = [c for c in cells if not has_transparency(c) or (c[..., 3] >= 128).any()]
    if not cells:
        sys.exit("Every grid cell was empty.")
    return [c if has_transparency(c) else c[..., :3] for c in cells]


def read_frames(path, start=0.0, end=None, fps=12, grid=None):
    """Returns frames sampled at `fps` between start and end (seconds). Images with real transparency come
    back as RGBA (no keying needed); everything else is RGB."""
    path = pathlib.Path(path)
    if not path.exists():
        sys.exit(f"File not found: {path}")
    if path.suffix.lower() in IMAGE_EXTS:
        image = np.asarray(Image.open(path).convert("RGBA"))
        if grid:
            return split_grid(image, grid)
        return [image if has_transparency(image) else image[..., :3]]

    import imageio.v2 as imageio
    reader = imageio.get_reader(str(path), "ffmpeg")
    meta = reader.get_meta_data()
    src_fps = float(meta.get("fps") or 24.0)
    duration = float(meta.get("duration") or 0.0)
    if end is None or (duration and end > duration):
        end = duration if duration else end
    if end is None or end <= start:
        sys.exit(f"Could not read the clip length of {path}; pass --end in seconds.")

    count = max(1, int(round((end - start) * fps)))
    targets = [start + i / fps for i in range(count)]
    frames, j = [], 0
    for i, frame in enumerate(reader):
        t = i / src_fps
        while j < len(targets) and t + 0.5 / src_fps >= targets[j]:
            frames.append(np.asarray(frame)[..., :3])
            j += 1
        if j >= len(targets):
            break
    reader.close()
    if not frames:
        sys.exit(f"No frames read from {path}.")
    return frames


# ── Keying ────────────────────────────────────────────────────────────────────

def to_chroma(rgb):
    rgb = rgb.astype(np.float32)
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    cb = 128 - 0.168736 * r - 0.331264 * g + 0.5 * b
    cr = 128 + 0.5 * r - 0.418688 * g - 0.081312 * b
    return cb, cr


def detect_key(rgb):
    rgb = rgb[..., :3]
    h, w, _ = rgb.shape
    b = max(2, min(h, w) // 50)
    border = np.concatenate([rgb[:b].reshape(-1, 3), rgb[-b:].reshape(-1, 3),
                             rgb[:, :b].reshape(-1, 3), rgb[:, -b:].reshape(-1, 3)])
    return np.median(border, axis=0)


def shift(mask, dy, dx):
    out = np.zeros_like(mask)
    h, w = mask.shape
    ys = slice(max(0, dy), h + min(0, dy))
    yd = slice(max(0, -dy), h + min(0, -dy))
    xs = slice(max(0, dx), w + min(0, dx))
    xd = slice(max(0, -dx), w + min(0, -dx))
    out[ys, xs] = mask[yd, xd]
    return out


def dilate(mask):
    out = mask.copy()
    for dy, dx in ((-1, 0), (1, 0), (0, -1), (0, 1), (-1, -1), (-1, 1), (1, -1), (1, 1)):
        out |= shift(mask, dy, dx)
    return out


def erode(mask):
    return ~dilate(~mask)


def key_out(rgb, key, tolerance):
    """Returns (rgb with de-spilled edges, boolean alpha). Frames that already carry transparency skip keying."""
    if rgb.shape[2] == 4:
        return rgb[..., :3].copy(), rgb[..., 3] >= 128
    cb, cr = to_chroma(rgb)
    kcb, kcr = to_chroma(np.asarray(key, dtype=np.float32)[None, None, :])
    alpha = np.hypot(cb - kcb, cr - kcr) > tolerance
    alpha = dilate(erode(alpha))  # remove specks

    # Push edge pixels' colour away from the key colour (removes the green/magenta fringe).
    edge = alpha & ~erode(erode(alpha))
    out = rgb.astype(np.float32)
    key = np.asarray(key, dtype=np.float32)
    kdir = key - key.mean()
    norm = np.linalg.norm(kdir)
    if norm > 1e-3:
        kdir /= norm
        px = out[edge]
        spill = np.clip(((px - px.mean(axis=1, keepdims=True)) * kdir).sum(axis=1), 0, None)
        out[edge] = np.clip(px - spill[:, None] * kdir * 0.9, 0, 255)
    return out.astype(np.uint8), alpha


def check_key_coverage(alpha, name):
    covered = 1.0 - alpha.mean()
    if covered < 0.2:
        print(f"  ! Only {covered:.0%} of {name} matched the key colour. Was the background flat green/magenta? "
              f"Try --tolerance, or regenerate with a flatter background.")


# ── Pixel art ─────────────────────────────────────────────────────────────────

def crop_box(image, box, size):
    """Crops (left, top, right, bottom) in float source pixels, padding with transparency, then box-downsamples."""
    left, top, right, bottom = box
    cropped = image.crop((int(math.floor(left)), int(math.floor(top)), int(math.ceil(right)), int(math.ceil(bottom))))
    return cropped.resize(size, Image.BOX)


def binarize_alpha(rgba):
    arr = np.asarray(rgba).copy()
    arr[..., 3] = np.where(arr[..., 3] >= 128, 255, 0)
    return arr


def add_outline(arr):
    solid = arr[..., 3] > 0
    ring = dilate(solid) & ~solid
    arr[ring, :3] = INK
    arr[ring, 3] = 255
    return arr


def quantize_shared(frames, colors):
    """Quantizes a list of RGBA arrays to one shared palette so colours don't flicker between frames."""
    h, w = frames[0].shape[:2]
    strip = np.concatenate(frames, axis=1)
    rgb = strip[..., :3].copy()
    rgb[strip[..., 3] == 0] = INK
    quant = Image.fromarray(rgb).quantize(colors=colors, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE)
    rgb = np.asarray(quant.convert("RGB"))
    strip = np.dstack([rgb, strip[..., 3]])
    return [strip[:, i * w:(i + 1) * w] for i in range(len(frames))]


def pack_sheet(frames, out_png, meta):
    count = len(frames)
    h, w = frames[0].shape[:2]
    columns = min(count, 8)
    rows = math.ceil(count / columns)
    sheet = np.zeros((rows * h, columns * w, 4), dtype=np.uint8)
    for i, f in enumerate(frames):
        r, c = divmod(i, columns)
        sheet[r * h:(r + 1) * h, c * w:(c + 1) * w] = f
    out_png.parent.mkdir(parents=True, exist_ok=True)
    Image.fromarray(sheet, "RGBA").save(out_png)
    meta = dict(meta, frameWidth=w, frameHeight=h, columns=columns, count=count)
    (out_png.parent / (out_png.stem + ".sheet.json")).write_text(json.dumps(meta, indent=2))
    print(f"  -> {out_png.relative_to(ROOT)}  ({count} frame{'s' if count != 1 else ''}, {w}x{h})")


# ── Characters ────────────────────────────────────────────────────────────────

def character_frames(frames, key, tolerance, height_px, outline, colors):
    """Keys all frames and crops them with one fixed box measured on frame 0."""
    keyed = [key_out(f, key, tolerance) for f in frames]
    if frames[0].shape[2] == 3:
        check_key_coverage(keyed[0][1], "the first frame")

    ys, xs = np.nonzero(keyed[0][1])
    if len(ys) == 0:
        sys.exit("Nothing left after keying the first frame. Check the key colour or --tolerance.")
    body_h = ys.max() + 1 - ys.min()
    anchor_x = (xs.min() + xs.max() + 1) / 2
    anchor_y = ys.max() + 1

    frame_w, frame_h = round(height_px * 1.25), round(height_px * 1.2)
    scale = height_px / body_h  # output px per source px
    box_w, box_h = frame_w / scale, frame_h / scale
    bottom = anchor_y + FEET_MARGIN_PX / scale
    box = (anchor_x - box_w / 2, bottom - box_h, anchor_x + box_w / 2, bottom)

    out = []
    for rgb, alpha in keyed:
        rgba = np.dstack([rgb, np.where(alpha, 255, 0).astype(np.uint8)])
        small = binarize_alpha(crop_box(Image.fromarray(rgba, "RGBA"), box, (frame_w, frame_h)))
        out.append(small)

    out = quantize_shared(out, colors)
    if outline:
        out = [add_outline(f) for f in out]
    return out, dict(pivotX=0.5, pivotY=FEET_MARGIN_PX / frame_h, pixelsPerUnit=round(height_px / CHAR_WORLD_HEIGHT, 3))


def make_icon(rgb, alpha, outline):
    ys, xs = np.nonzero(alpha)
    top, bottom = ys.min(), ys.max() + 1
    head_rows = alpha[top:top + max(1, int((bottom - top) * 0.3))]
    hx = np.nonzero(head_rows.any(axis=0))[0]
    cx = (hx.min() + hx.max() + 1) / 2
    side = max(hx.max() + 1 - hx.min(), (bottom - top) * 0.28) * 1.15
    box = (cx - side / 2, top - side * 0.06, cx + side / 2, top - side * 0.06 + side)

    rgba = np.dstack([rgb, np.where(alpha, 255, 0).astype(np.uint8)])
    icon = binarize_alpha(crop_box(Image.fromarray(rgba, "RGBA"), box, (ICON_PX, ICON_PX)))
    icon = quantize_shared([icon], 24)[0]
    losing = icon.copy()
    grey = losing[..., :3].mean(axis=2, keepdims=True)
    losing[..., :3] = np.clip((losing[..., :3] * 0.35 + grey * 0.65) * 0.7, 0, 255).astype(np.uint8)
    if outline:
        icon, losing = add_outline(icon), add_outline(losing)
    return icon, losing


def rig_path(cid):
    return ART / "Characters" / cid / f"{cid}.rig.json"


def cmd_base(args):
    folder = ART / "Characters" / args.id
    rgb = read_frames(args.input)[0]
    key = detect_key(rgb) if args.key is None else np.array(parse_hex(args.key))
    if rgb.shape[2] == 4:
        print(f"{args.id}: image already has transparency, no keying needed")
    else:
        print(f"{args.id}: key colour #{int(key[0]):02X}{int(key[1]):02X}{int(key[2]):02X}")

    frames, sheet_meta = character_frames([rgb], key, args.tolerance, args.height, not args.no_outline, args.colors)
    idle_png = folder / f"{args.id}_idle.png"
    if idle_png.exists() and not args.force:
        print(f"  (keeping existing {idle_png.name}; pass --force to overwrite it with the still)")
    else:
        pack_sheet(frames, idle_png, dict(sheet_meta, fps=1, loop=True, kind="character"))

    keyed_rgb, alpha = key_out(rgb, key, args.tolerance)
    icon, losing = make_icon(keyed_rgb, alpha, not args.no_outline)
    folder.mkdir(parents=True, exist_ok=True)
    Image.fromarray(icon, "RGBA").save(folder / f"{args.id}_icon.png")
    Image.fromarray(losing, "RGBA").save(folder / f"{args.id}_icon_losing.png")
    print(f"  -> {(folder / (args.id + '_icon.png')).relative_to(ROOT)} (+ _icon_losing)")

    rig_path(args.id).write_text(json.dumps(dict(key=[int(k) for k in key], tolerance=args.tolerance,
                                                  height=args.height, colors=args.colors), indent=2))


def cmd_pose(args):
    defaults = POSE_DEFAULTS[args.pose]
    start = args.start if args.start is not None else defaults["start"]
    end = args.end if args.end is not None else defaults["end"]
    fps = args.fps or defaults["fps"]

    rig = {}
    if rig_path(args.id).exists():
        rig = json.loads(rig_path(args.id).read_text())
    else:
        print(f"  (no {args.id}.rig.json yet; run the base command first for consistent keying)")

    frames = read_frames(args.input, start, end, fps, grid=args.grid)
    if args.grid and args.fps is None:
        fps = defaults["fps"]
    key = np.array(parse_hex(args.key)) if args.key else np.array(rig.get("key")) if rig.get("key") else detect_key(frames[0])
    tolerance = args.tolerance if args.tolerance_set else rig.get("tolerance", args.tolerance)
    height = args.height if args.height_set else rig.get("height", args.height)
    colors = args.colors if args.colors_set else rig.get("colors", args.colors)
    print(f"{args.id} {args.pose}: {len(frames)} frames @ {fps} fps")

    out, sheet_meta = character_frames(frames, key, tolerance, height, not args.no_outline, colors)
    loop = defaults["loop"] if args.loop is None else args.loop
    pack_sheet(out, ART / "Characters" / args.id / f"{args.id}_{args.pose}.png",
               dict(sheet_meta, fps=fps, loop=loop, kind="character"))


# ── Backgrounds ───────────────────────────────────────────────────────────────

def cover(rgb, size):
    img = Image.fromarray(rgb)
    w, h = img.size
    scale = max(size[0] / w, size[1] / h)
    rw, rh = max(size[0], round(w * scale)), max(size[1], round(h * scale))
    img = img.resize((rw, rh), Image.BOX)
    left, top = (rw - size[0]) // 2, (rh - size[1]) // 2
    return np.asarray(img.crop((left, top, left + size[0], top + size[1])))


def cmd_background(args):
    is_video = pathlib.Path(args.input).suffix.lower() not in IMAGE_EXTS
    fps = args.fps or 8
    frames = read_frames(args.input, args.start or 0.0, args.end, fps)
    print(f"{args.slug}: {len(frames)} frame{'s' if len(frames) != 1 else ''}")

    small = [np.dstack([cover(f, BG_SIZE), np.full(BG_SIZE[::-1], 255, np.uint8)]) for f in frames]
    small = quantize_shared(small, args.colors)
    pack_sheet(small, ART / "Backgrounds" / args.slug / f"bg_{args.slug}.png",
               dict(pivotX=0.5, pivotY=0.5, pixelsPerUnit=BG_SIZE[1] / BG_WORLD_HEIGHT, fps=fps, loop=True, kind="background"))

    if args.video:
        if not is_video:
            print("  ! --video needs a clip, not a still image; skipped.")
            return
        import imageio.v2 as imageio
        vfps = args.video_fps
        vframes = read_frames(args.input, args.start or 0.0, args.end, vfps)
        vsmall = quantize_shared([np.dstack([cover(f, BG_SIZE), np.full(BG_SIZE[::-1], 255, np.uint8)]) for f in vframes],
                                 args.colors)
        VIDEO_DIR.mkdir(parents=True, exist_ok=True)
        out = VIDEO_DIR / f"bg_{args.slug}.mp4"
        writer = imageio.get_writer(str(out), fps=vfps, codec="libx264", quality=8, pixelformat="yuv420p", macro_block_size=16)
        for f in vsmall:
            big = np.repeat(np.repeat(f[..., :3], 4, axis=0), 4, axis=1)  # 1280x720, hard pixel edges
            writer.append_data(big)
        writer.close()
        print(f"  -> {out.relative_to(ROOT)}  ({len(vsmall)} frames @ {vfps} fps, 1280x720)")


# ── CLI ───────────────────────────────────────────────────────────────────────

def parse_hex(value):
    value = value.lstrip("#")
    if len(value) != 6:
        sys.exit("--key must look like #00FF00")
    return [int(value[i:i + 2], 16) for i in (0, 2, 4)]


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = parser.add_subparsers(dest="command", required=True)

    def keying(p):
        p.add_argument("--key", help="Key colour like #00FF00 (default: detected from the image border)")
        p.add_argument("--tolerance", type=float, default=40.0, help="Chroma distance treated as background (default 40)")
        p.add_argument("--height", type=int, default=CHAR_HEIGHT_PX, help=f"Body height in pixels (default {CHAR_HEIGHT_PX})")
        p.add_argument("--colors", type=int, default=32, help="Palette size shared by all frames (default 32)")
        p.add_argument("--no-outline", action="store_true", help="Skip the 1px dark outline")

    p = sub.add_parser("base", help="Key a character base image: icons, rig and a one-frame idle")
    p.add_argument("input")
    p.add_argument("--id", required=True)
    p.add_argument("--force", action="store_true", help="Overwrite an existing animated idle with the still")
    keying(p)
    p.set_defaults(func=cmd_base)

    p = sub.add_parser("pose", help="Turn a pose clip into a sprite sheet")
    p.add_argument("input")
    p.add_argument("--id", required=True)
    p.add_argument("--pose", required=True, choices=sorted(POSE_DEFAULTS))
    p.add_argument("--start", type=float)
    p.add_argument("--end", type=float)
    p.add_argument("--fps", type=float)
    p.add_argument("--loop", type=lambda v: v.lower() in ("1", "true", "yes"), default=None)
    p.add_argument("--grid", help="Input is a sprite sheet image with this many COLUMNSxROWS cells (e.g. AutoSprite output)")
    keying(p)
    p.set_defaults(func=cmd_pose)

    p = sub.add_parser("background", help="Turn a background still or loop into a sheet (and optional MP4)")
    p.add_argument("input")
    p.add_argument("--slug", required=True)
    p.add_argument("--start", type=float)
    p.add_argument("--end", type=float)
    p.add_argument("--fps", type=float, help="Sheet frame rate (default 8)")
    p.add_argument("--colors", type=int, default=48)
    p.add_argument("--video", action="store_true", help="Also write a pixelated MP4 to StreamingAssets/Video")
    p.add_argument("--video-fps", type=float, default=24)
    p.set_defaults(func=cmd_background)

    args = parser.parse_args()
    # Remember which keying values were typed so pose can fall back to the rig for the rest.
    typed = set(a.split("=")[0] for a in sys.argv[2:] if a.startswith("--"))
    args.tolerance_set = "--tolerance" in typed
    args.height_set = "--height" in typed
    args.colors_set = "--colors" in typed
    args.func(args)


if __name__ == "__main__":
    main()
