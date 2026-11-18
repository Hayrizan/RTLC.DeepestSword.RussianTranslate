#!/usr/bin/env python3
"""Generate expanded 1024x1024 Cyrillic TMP font atlas and Russian pixel-art Logo.png
from Deepest Sword's original sharedassets0.assets.
"""
from __future__ import annotations

import argparse
import gzip
import math
import os
from pathlib import Path

import UnityPy
from PIL import Image, ImageDraw

REPO_ROOT = Path(__file__).resolve().parent.parent
ASSETS_OUT_DIR = REPO_ROOT / "src" / "RTLC.DeepestSword.RussianTranslate" / "Assets"
SCRATCH_DIR = REPO_ROOT / "dist" / "asset-previews"

FONT_UNIT_SCALE = 7.5  # 1 logical pixel = 7.5 units in 60pt pixelFJ8pt1__

# (char, bearing_x_units_in_pixels, bearing_y_in_pixels, advance_in_pixels, grid_rows)
# Note: advance defaults to cols + 1 unless overridden.
GLYPH_DEFS: list[tuple[str, int, int, int | None, list[str]]] = [
    # Uppercase Cyrillic (А-Я, Ё)
    ("А", 0, 7, None, [".####.", "#....#", "#....#", "######", "#....#", "#....#", "#....#"]),
    ("Б", 0, 7, None, ["######", "#.....", "#.....", "#####.", "#....#", "#....#", "#####."]),
    ("В", 0, 7, None, ["#####.", "#....#", "#....#", "#####.", "#....#", "#....#", "#####."]),
    ("Г", 0, 7, None, ["#####", "#....", "#....", "#....", "#....", "#....", "#...."]),
    ("Д", 0, 7, None, ["..####.", ".#...#.", ".#...#.", ".#...#.", ".#...#.", ".#...#.", "#######", "#.....#"]),
    ("Е", 0, 7, None, ["######", "#.....", "#.....", "#####.", "#.....", "#.....", "######"]),
    ("Ё", 0, 9, None, [".#..#.", "......", "######", "#.....", "#.....", "#####.", "#.....", "#.....", "######"]),
    ("Ж", 0, 7, None, ["#..#..#", "#..#..#", ".#.#.#.", "..###..", ".#.#.#.", "#..#..#", "#..#..#"]),
    ("З", 0, 7, None, [".####.", "#....#", ".....#", "..###.", ".....#", "#....#", ".####."]),
    ("И", 0, 7, None, ["#....#", "#....#", "#...##", "#..#.#", "#.#..#", "##...#", "#....#"]),
    ("Й", 0, 10, None, [".#..#.", "..##..", "......", "#....#", "#....#", "#...##", "#..#.#", "#.#..#", "##...#", "#....#"]),
    ("К", 0, 7, None, ["#....#", "#...#.", "#..#..", "###...", "#..#..", "#...#.", "#....#"]),
    ("Л", 0, 7, None, ["..####", ".#...#", ".#...#", ".#...#", ".#...#", ".#...#", "#....#"]),
    ("М", 0, 7, None, ["#.....#", "##...##", "#.#.#.#", "#..#..#", "#.....#", "#.....#", "#.....#"]),
    ("Н", 0, 7, None, ["#....#", "#....#", "#....#", "######", "#....#", "#....#", "#....#"]),
    ("О", 0, 7, None, [".####.", "#....#", "#....#", "#....#", "#....#", "#....#", ".####."]),
    ("П", 0, 7, None, ["######", "#....#", "#....#", "#....#", "#....#", "#....#", "#....#"]),
    ("Р", 0, 7, None, ["#####.", "#....#", "#....#", "#####.", "#.....", "#.....", "#....."]),
    ("С", 0, 7, None, [".####.", "#....#", "#.....", "#.....", "#.....", "#....#", ".####."]),
    ("Т", 0, 7, None, ["#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.."]),
    ("У", 0, 7, None, ["#....#", "#....#", "#....#", ".#####", ".....#", "#....#", ".####."]),
    ("Ф", 0, 7, None, ["...#...", ".#####.", "#..#..#", "#..#..#", "#..#..#", ".#####.", "...#..."]),
    ("Х", 0, 7, None, ["#.....#", ".#...#.", "..#.#..", "...#...", "..#.#..", ".#...#.", "#.....#"]),
    ("Ц", 0, 7, None, ["#....#.", "#....#.", "#....#.", "#....#.", "#....#.", "#....#.", "#######", "......#"]),
    ("Ч", 0, 7, None, ["#....#", "#....#", "#....#", ".#####", ".....#", ".....#", ".....#"]),
    ("Ш", 0, 7, None, ["#..#..#", "#..#..#", "#..#..#", "#..#..#", "#..#..#", "#..#..#", "#######"]),
    ("Щ", 0, 7, None, ["#..#..#.", "#..#..#.", "#..#..#.", "#..#..#.", "#..#..#.", "#..#..#.", "########", ".......#"]),
    ("Ъ", 0, 7, None, ["###....", "..#....", "..#....", "..####.", "..#...#", "..#...#", "..####."]),
    ("Ы", 0, 7, None, ["#......#", "#......#", "#......#", "####...#", "#...#..#", "#...#..#", "####...#"]),
    ("Ь", 0, 7, None, ["#.....", "#.....", "#.....", "#####.", "#....#", "#....#", "#####."]),
    ("Э", 0, 7, None, [".####.", "#....#", ".....#", "..####", ".....#", "#....#", ".####."]),
    ("Ю", 0, 7, None, ["#...####.", "#..#....#", "#..#....#", "####....#", "#..#....#", "#..#....#", "#...####."]),
    ("Я", 0, 7, None, [".#####", "#....#", "#....#", ".#####", "..#..#", ".#...#", "#....#"]),
    # Lowercase Cyrillic (а-я, ё)
    ("а", 0, 5, None, [".####", "#...#", "#...#", "#...#", ".####"]),
    ("б", 0, 7, None, [".####", "#....", "####.", "#...#", "#...#", "#...#", ".###."]),
    ("в", 0, 5, None, ["####.", "#...#", "####.", "#...#", "####."]),
    ("г", 0, 5, None, ["####", "#...", "#...", "#...", "#..."]),
    ("д", 0, 5, None, ["..###.", ".#..#.", ".#..#.", ".#..#.", "######", "#....#"]),
    ("е", 0, 5, None, [".###.", "#...#", "#####", "#....", ".###."]),
    ("ё", 0, 7, None, [".#.#.", ".....", ".###.", "#...#", "#####", "#....", ".###."]),
    ("ж", 0, 5, None, ["#..#..#", ".#.#.#.", "..###..", ".#.#.#.", "#..#..#"]),
    ("з", 0, 5, None, [".###.", "....#", "..##.", "....#", ".###."]),
    ("и", 0, 5, None, ["#...#", "#..##", "#.#.#", "##..#", "#...#"]),
    ("й", 0, 8, None, [".#.#.", "..#..", ".....", "#...#", "#..##", "#.#.#", "##..#", "#...#"]),
    ("к", 0, 5, None, ["#...#", "#..#.", "###..", "#..#.", "#...#"]),
    ("л", 0, 5, None, ["..###", ".#..#", ".#..#", ".#..#", "#...#"]),
    ("м", 0, 5, None, ["#.....#", "##...##", "#.#.#.#", "#..#..#", "#.....#"]),
    ("н", 0, 5, None, ["#...#", "#...#", "#####", "#...#", "#...#"]),
    ("о", 0, 5, None, [".###.", "#...#", "#...#", "#...#", ".###."]),
    ("п", 0, 5, None, ["#####", "#...#", "#...#", "#...#", "#...#"]),
    ("р", 0, 5, None, ["####.", "#...#", "#...#", "#...#", "####.", "#....", "#...."]),
    ("с", 0, 5, None, [".###.", "#...#", "#....", "#...#", ".###."]),
    ("т", 0, 5, None, ["#####", "..#..", "..#..", "..#..", "..#.."]),
    ("у", 0, 5, None, ["#...#", "#...#", "#...#", "#...#", ".####", "....#", ".###."]),
    ("ф", 0, 5, None, [".#####.", "#..#..#", "#..#..#", "#..#..#", ".#####.", "...#...", "...#..."]),
    ("х", 0, 5, None, ["#...#", ".#.#.", "..#..", ".#.#.", "#...#"]),
    ("ц", 0, 5, None, ["#...#.", "#...#.", "#...#.", "#...#.", "######", ".....#"]),
    ("ч", 0, 5, None, ["#...#", "#...#", ".####", "....#", "....#"]),
    ("ш", 0, 5, None, ["#..#..#", "#..#..#", "#..#..#", "#..#..#", "#######"]),
    ("щ", 0, 5, None, ["#..#..#.", "#..#..#.", "#..#..#.", "#..#..#.", "########", ".......#"]),
    ("ъ", 0, 5, None, ["##....", ".#....", ".####.", ".#...#", ".####."]),
    ("ы", 0, 5, None, ["#.....#", "#.....#", "####..#", "#..#..#", "####..#"]),
    ("ь", 0, 5, None, ["#....", "#....", "####.", "#...#", "####."]),
    ("э", 0, 5, None, [".###.", "....#", ".####", "....#", ".###."]),
    ("ю", 0, 5, None, ["#..###.", "#.#...#", "###...#", "#.#...#", "#..###."]),
    ("я", 0, 5, None, [".####", "#...#", ".####", ".#..#", "#...#"]),
    # Punctuation & typography symbols
    ("«", 0, 5, None, ["..#.#", ".#.#.", "#.#..", ".#.#.", "..#.#"]),
    ("»", 0, 5, None, ["#.#..", ".#.#.", "..#.#", ".#.#.", "#.#.."]),
    ("—", 0, 5, None, ["......", "......", "######", "......", "......"]),
    ("–", 0, 5, None, ["....", "....", "####", "....", "...."]),
    ("…", 0, 1, None, ["#.#.#"]),
    ("№", 0, 7, None, ["#...#..#.", "##..#.#.#", "#.#.#.#.#", "#..##..#.", "#...#....", "#...#.##.", "#...#...."]),
    # Non-breaking space (uses space advance = 4 * 7.5 = 30.0)
    ("\u00A0", 0, 0, 4, []),
]


def rasterize_grid(grid: list[str]) -> tuple[int, int, int, int, list[list[int]]]:
    if not grid:
        return 0, 0, 0, 0, []
    rows = len(grid)
    cols = len(grid[0])
    for r in grid:
        assert len(r) == cols, f"Inconsistent row length in {grid}"

    w_px = round(cols * FONT_UNIT_SCALE)
    h_px = round(rows * FONT_UNIT_SCALE)
    pixels = [[0] * w_px for _ in range(h_px)]

    for ry, line in enumerate(grid):
        y0 = round(ry * FONT_UNIT_SCALE)
        y1 = round((ry + 1) * FONT_UNIT_SCALE)
        for rx, ch in enumerate(line):
            if ch != "#":
                continue
            x0 = round(rx * FONT_UNIT_SCALE)
            x1 = round((rx + 1) * FONT_UNIT_SCALE)
            for py in range(max(0, y0), min(h_px, y1)):
                for px in range(max(0, x0), min(w_px, x1)):
                    pixels[py][px] = 255
    return cols, rows, w_px, h_px, pixels


def generate_font_atlas(orig_atlas_raw: bytes) -> None:
    assert len(orig_atlas_raw) == 512 * 512
    buf = bytearray(1024 * 1024)
    # Copy original 512x512 Alpha8 into bottom-left (x: 0..512, y_unity: 0..512)
    for y_u in range(512):
        src_off = y_u * 512
        dst_off = y_u * 1024
        buf[dst_off : dst_off + 512] = orig_atlas_raw[src_off : src_off + 512]

    pad = 8
    cur_x = 8
    cur_y_u = 520  # Top half of 1024x1024 texture (above original 0..512)
    row_max_h = 0

    tsv_lines: list[str] = []
    specimen_glyphs: dict[str, tuple[int, int, int, int, float, float, float, float, float]] = {}

    for ch, bx_u, by_u, adv_u, grid in GLYPH_DEFS:
        cols, rows, w_px, h_px, pixels = rasterize_grid(grid)
        adv_units = (adv_u if adv_u is not None else (cols + 1)) * FONT_UNIT_SCALE
        mw = cols * FONT_UNIT_SCALE
        mh = rows * FONT_UNIT_SCALE
        bx = bx_u * FONT_UNIT_SCALE
        by = by_u * FONT_UNIT_SCALE

        if w_px == 0 or h_px == 0:
            tsv_lines.append(
                f"{ord(ch)}\t0\t0\t0\t0\t{mw:.4f}\t{mh:.4f}\t{bx:.4f}\t{by:.4f}\t{adv_units:.4f}"
            )
            specimen_glyphs[ch] = (0, 0, 0, 0, mw, mh, bx, by, adv_units)
            continue

        if cur_x + w_px + pad > 1016:
            cur_x = 8
            cur_y_u += row_max_h + pad
            row_max_h = 0

        assert cur_y_u + h_px + pad <= 1020, f"Atlas overflow at {ch!r}: y={cur_y_u}, h={h_px}"

        # Write glyph into Unity Alpha8 buffer (y_unity=0 is bottom row, py=0 is top row)
        for py in range(h_px):
            y_unity = cur_y_u + (h_px - 1 - py)
            row_off = y_unity * 1024 + cur_x
            row_vals = pixels[py]
            for px in range(w_px):
                buf[row_off + px] = row_vals[px]

        tsv_lines.append(
            f"{ord(ch)}\t{cur_x}\t{cur_y_u}\t{w_px}\t{h_px}\t{mw:.4f}\t{mh:.4f}\t{bx:.4f}\t{by:.4f}\t{adv_units:.4f}"
        )
        specimen_glyphs[ch] = (cur_x, cur_y_u, w_px, h_px, mw, mh, bx, by, adv_units)

        cur_x += w_px + pad
        row_max_h = max(row_max_h, h_px)

    gz_path = ASSETS_OUT_DIR / "font_atlas_1024.alpha8.gz"
    tsv_path = ASSETS_OUT_DIR / "cyrillic_glyphs.tsv"
    gz_path.write_bytes(gzip.compress(bytes(buf), compresslevel=9))
    tsv_path.write_text("\n".join(tsv_lines) + "\n", encoding="utf-8")
    print(f"Wrote {gz_path} ({gz_path.stat().st_size} bytes) and {tsv_path} ({len(tsv_lines)} glyphs)")

    # Render specimen sheet from the generated 1024x1024 atlas
    atlas_pil = Image.frombytes("L", (1024, 1024), bytes(buf)).transpose(Image.Transpose.FLIP_TOP_BOTTOM)
    atlas_pil.save(SCRATCH_DIR / "font_atlas_1024_preview.png")

    sample_lines = [
        "ГЛУБОЧАЙШИЙ МЕЧ — ПРОНЗИ СЕРДЦЕ ДРАКОНА!",
        "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ",
        "абвгдеёжзийклмнопрстуфхцчшщъыьэюя",
        "«Спускайся в глубины Пещеры Томления.»",
        "ОЗ: 100/100   Длина: 5   Время: 0:12.345",
        "Управление: Влево / Вправо — Назад — Спидран",
    ]
    spec_w, spec_h = 1400, 560
    spec = Image.new("RGBA", (spec_w, spec_h), (24, 24, 34, 255))
    scale = 0.5  # draw at ~30px line height
    y_base = 80.0
    for line in sample_lines:
        x_cur = 30.0
        for ch in line:
            if ch == " ":
                x_cur += 30.0 * scale
                continue
            g = specimen_glyphs.get(ch)
            if not g:
                x_cur += 24.0 * scale
                continue
            gx, gy_u, gw, gh, _, _, bx, by, adv = g
            if gw > 0 and gh > 0:
                # In atlas_pil (top-down), Unity rect (gx, gy_u, gw, gh) is at y_top = 1024 - (gy_u + gh)
                y_top = 1024 - (gy_u + gh)
                glyph_crop = atlas_pil.crop((gx, y_top, gx + gw, y_top + gh))
                dw = max(1, round(gw * scale))
                dh = max(1, round(gh * scale))
                glyph_rgba = Image.new("RGBA", (gw, gh), (245, 242, 255, 255))
                glyph_rgba.putalpha(glyph_crop)
                glyph_resized = glyph_rgba.resize((dw, dh), Image.Resampling.NEAREST)
                dx = round(x_cur + bx * scale)
                dy = round(y_base - by * scale)
                spec.alpha_composite(glyph_resized, (dx, dy))
            x_cur += adv * scale
        y_base += 80.0
    spec.save(SCRATCH_DIR / "font_specimen.png")
    print(f"Saved font specimen to {SCRATCH_DIR / 'font_specimen.png'}")


def generate_russian_logo(orig_logo_img: Image.Image) -> None:
    """Create pixel-art Russian Logo.png (240x135) matching the exact original palette & shield."""
    orig = orig_logo_img.convert("RGBA")
    out = orig.copy()

    # Exact palette from original Logo
    SHIELD_MAIN = (85, 113, 101, 255)
    SHIELD_DARK = (59, 78, 72, 255)
    OUTLINE = (39, 0, 38, 255)
    CORNER_OUT = (67, 54, 87, 255)
    HILIGHT = (183, 169, 188, 255)
    TOP_FILL = (131, 118, 156, 255)
    BOT_FILL = (67, 87, 159, 255)
    DEEP_FILL = (0, 56, 83, 255)

    # Shield crop sits at (77, 4) .. (162, 74) -> 85x70
    OX, OY = 77, 4
    crop = orig.crop((OX, OY, OX + 85, OY + 70))

    # Clean up text region y=25..55 inside the ouroboros dragon ring while preserving the exact outer silhouette
    # and the left/right dragon neck & tail shading.
    shield = crop.copy()
    for y in range(25, 56):
        xs = [x for x in range(85) if crop.getpixel((x, y))[3] > 0]
        if not xs:
            continue
        lx, rx = min(xs), max(xs)
        # Preserve left 5px (dragon neck rim/shading) and right 6px (dragon tail rim/shading)
        for x in range(lx + 5, rx - 5):
            # In the original sprite, the lower-left and top-left of the inner belly have subtle SHIELD_DARK shading
            orig_px = crop.getpixel((x, y))
            if y == 55 and orig_px == SHIELD_DARK:
                shield.putpixel((x, y), SHIELD_DARK)
            else:
                shield.putpixel((x, y), SHIELD_MAIN)

    # Restore central sword blade behind text at y=25..55 so it peeks cleanly between the two lines
    for y in range(25, 56):
        shield.putpixel((39, y), CORNER_OUT)
        shield.putpixel((40, y), HILIGHT)
        shield.putpixel((41, y), HILIGHT)
        shield.putpixel((42, y), (194, 195, 199, 255))
        shield.putpixel((43, y), (194, 195, 199, 255))
        shield.putpixel((44, y), (194, 195, 199, 255))
        shield.putpixel((45, y), CORNER_OUT)
        if y in (28, 29, 30, 43, 44, 45):
            # Preserve subtle blood glint along the left edge of the blade
            px = crop.getpixel((40, y))
            if px == (225, 212, 192, 255):
                shield.putpixel((40, y), px)

    # Pixel-art uppercase font for ГЛУБОЧАЙШИЙ (15px tall body, placed at y=25..39, outline y=26..40)
    # Matching original DEEPEST: top row uses HILIGHT with CORNER_OUT corners, bottom 2 rows use BOT_FILL,
    # and 1px OUTLINE wraps left/right/bottom/inner cutouts.
    TOP_LETTERS: dict[str, list[str]] = {
        "Г": [
            "#####",
            "#####",
            "#####",
            "##...",
            "##...",
            "##...",
            "##...",
            "##...",
            "##...",
            "##...",
            "##...",
            "##...",
            "##...",
            "##...",
            "##...",
        ],
        "Л": [
            ".####",
            "#####",
            "#####",
            "##.##",
            "##.##",
            "##.##",
            "##.##",
            "##.##",
            "##.##",
            "##.##",
            "##.##",
            "##.##",
            "##.##",
            "##.##",
            "##.##",
        ],
        "У": [
            "##.##",
            "##.##",
            "##.##",
            "##.##",
            "##.##",
            "##.##",
            "##.##",
            "#####",
            ".####",
            "...##",
            "...##",
            "...##",
            "#####",
            "#####",
            "####.",
        ],
        "Б": [
            "#####",
            "#####",
            "#####",
            "##...",
            "##...",
            "##...",
            "####.",
            "#####",
            "#####",
            "##.##",
            "##.##",
            "##.##",
            "#####",
            "#####",
            "####.",
        ],
        "О": [
            ".###.",
            "#####",
            "#####",
            "##.##",
            "##.##",
            "##.##",
            "##.##",
            "##.##",
            "##.##",
            "##.##",
            "##.##",
            "##.##",
            "#####",
            "#####",
            ".###.",
        ],
        "Ч": [
            "##.##",
            "##.##",
            "##.##",
            "##.##",
            "##.##",
            "##.##",
            "##.##",
            "#####",
            ".####",
            "...##",
            "...##",
            "...##",
            "...##",
            "...##",
            "...##",
        ],
        "А": [
            ".###.",
            "#####",
            "##.##",
            "##.##",
            "##.##",
            "##.##",
            "##.##",
            "#####",
            "#####",
            "##.##",
            "##.##",
            "##.##",
            "##.##",
            "##.##",
            "##.##",
        ],
        "Й": [
            "##..##",
            "##..##",
            "##..##",
            "##.###",
            "##.###",
            "######",
            "######",
            "###.##",
            "###.##",
            "##..##",
            "##..##",
            "##..##",
            "##..##",
            "##..##",
            "##..##",
        ],
        "Ш": [
            "##.##.##",
            "##.##.##",
            "##.##.##",
            "##.##.##",
            "##.##.##",
            "##.##.##",
            "##.##.##",
            "##.##.##",
            "##.##.##",
            "##.##.##",
            "##.##.##",
            "##.##.##",
            "########",
            "########",
            "########",
        ],
        "И": [
            "##..##",
            "##..##",
            "##..##",
            "##.###",
            "##.###",
            "######",
            "######",
            "###.##",
            "###.##",
            "##..##",
            "##..##",
            "##..##",
            "##..##",
            "##..##",
            "##..##",
        ],
    }

    # Chunky 12px tall pixel-art letters for МЕЧ (y=43..54, spanning x=14..69 like SWORD)
    BOT_LETTERS: dict[str, list[str]] = {
        "М": [
            "####......####",
            "#####....#####",
            "######..######",
            "##############",
            "####.####.####",
            "####..##..####",
            "####......####",
            "####......####",
            "####......####",
            "####......####",
            "####......####",
            "####......####",
        ],
        "Е": [
            ".############",
            "#############",
            "#############",
            "####.........",
            "####.........",
            "###########..",
            "###########..",
            "####.........",
            "####.........",
            "#############",
            "#############",
            ".############",
        ],
        "Ч": [
            "####.....####",
            "####.....####",
            "####.....####",
            "####.....####",
            "#############",
            "#############",
            ".############",
            ".........####",
            ".........####",
            ".........####",
            ".........####",
            ".........####",
        ],
    }

    def draw_word_beveled(
        canvas: Image.Image,
        word: str,
        glyphs: dict[str, list[str]],
        x_positions: list[int],
        y0: int,
        row_colors: list[tuple[int, int, int, int]],
        top_outline: bool,
    ) -> None:
        mask: set[tuple[int, int]] = set()
        for ch, gx in zip(word, x_positions):
            g = glyphs[ch]
            for r, row_s in enumerate(g):
                for c, cell in enumerate(row_s):
                    if cell == "#":
                        mask.add((gx + c, y0 + r))

        # Subtle shield drop-shadow 1px below bottom outline (matching original SHIELD_DARK under DEEPEST/SWORD)
        for px, py in mask:
            if (px, py + 1) not in mask and (px, py + 2) not in mask and 0 <= px < 85 and 0 <= py + 2 < 70:
                if canvas.getpixel((px, py + 2)) == SHIELD_MAIN:
                    canvas.putpixel((px, py + 2), SHIELD_DARK)

        # 1px dark outline (skip top-above-row-0 for DEEPEST style so top highlight sits crisp like original)
        outline_pts: set[tuple[int, int]] = set()
        for px, py in mask:
            for dy in (-1, 0, 1):
                for dx in (-1, 0, 1):
                    nx, ny = px + dx, py + dy
                    if (nx, ny) not in mask:
                        if not top_outline and py == y0 and dy == -1:
                            continue
                        outline_pts.add((nx, ny))
        for px, py in outline_pts:
            if 0 <= px < 85 and 0 <= py < 70:
                canvas.putpixel((px, py), OUTLINE)

        # Draw letter body with top-row highlight + gradient colors
        for px, py in mask:
            ri = min(len(row_colors) - 1, max(0, py - y0))
            col = row_colors[ri]
            if not top_outline and ri == 0:
                # Chamfer left/right exposed corners of top row with CORNER_OUT, middle with HILIGHT (just like DEEPEST!)
                if (px - 1, py) not in mask or (px + 1, py) not in mask:
                    col = CORNER_OUT
                else:
                    col = HILIGHT
            canvas.putpixel((px, py), col)

    # Top word: "ГЛУБОЧАЙШИЙ" -> 11 letters, widths total 61px + 10*1px shared outline = 71px, x=7..77
    top_word = "ГЛУБОЧАЙШИЙ"
    top_xs: list[int] = []
    cx = 7
    for ch in top_word:
        top_xs.append(cx)
        cx += len(TOP_LETTERS[ch][0]) + 1
    top_colors = [
        HILIGHT,
        TOP_FILL, TOP_FILL, TOP_FILL, TOP_FILL, TOP_FILL,
        TOP_FILL, TOP_FILL, TOP_FILL, TOP_FILL, TOP_FILL,
        TOP_FILL, TOP_FILL,
        BOT_FILL, BOT_FILL,
    ]
    draw_word_beveled(shield, top_word, TOP_LETTERS, top_xs, 25, top_colors, top_outline=False)

    # Draw crisp floating breves above both 'Й' letters (indices 7 and 10) at y=22..23
    for idx in (7, 10):
        bx = top_xs[idx]
        # 1px outline around a 4x2 breve (x: bx+1..bx+4, y: 22..23)
        for oy in range(21, 25):
            for ox in range(bx, bx + 6):
                if (ox, oy) not in ((bx + 1, 22), (bx + 2, 22), (bx + 3, 22), (bx + 4, 22),
                                     (bx + 1, 23), (bx + 2, 23), (bx + 3, 23), (bx + 4, 23)):
                    shield.putpixel((ox, oy), OUTLINE)
        for ox in range(bx + 1, bx + 5):
            shield.putpixel((ox, 22), HILIGHT)
            shield.putpixel((ox, 23), TOP_FILL)

    # Bottom word: "МЕЧ" -> М(14) at 15..28, Е(13) at 36..48, Ч(13) at 55..67 (spans 15..67, dead centered on 41.5!)
    bot_word = "МЕЧ"
    bot_xs = [15, 36, 55]
    bot_colors = [
        BOT_FILL, BOT_FILL, BOT_FILL, BOT_FILL, BOT_FILL, BOT_FILL,
        BOT_FILL, BOT_FILL, BOT_FILL, BOT_FILL,
        DEEP_FILL, DEEP_FILL,
    ]
    draw_word_beveled(shield, bot_word, BOT_LETTERS, bot_xs, 43, bot_colors, top_outline=True)

    out.paste(shield, (OX, OY))
    logo_path = ASSETS_OUT_DIR / "Logo.png"
    out.save(logo_path)
    print(f"Wrote {logo_path} ({out.size})")

    # Build 4x nearest-neighbor visual comparison sheet
    c_orig = orig.crop((OX - 8, OY - 2, OX + 93, OY + 72)).resize((101 * 4, 74 * 4), Image.Resampling.NEAREST)
    c_new = out.crop((OX - 8, OY - 2, OX + 93, OY + 72)).resize((101 * 4, 74 * 4), Image.Resampling.NEAREST)
    comp = Image.new("RGBA", (c_orig.width * 2 + 24, c_orig.height + 24), (32, 34, 48, 255))
    comp.alpha_composite(c_orig, (8, 12))
    comp.alpha_composite(c_new, (c_orig.width + 16, 12))
    comp.save(SCRATCH_DIR / "logo_comparison.png")
    print(f"Saved logo comparison to {SCRATCH_DIR / 'logo_comparison.png'}")


def generate_russian_ending(orig_ending_img: Image.Image) -> None:
    """Create ending.png (240x135) with translated Cat speech bubble 'Ты пришёл за мной!'."""
    orig = orig_ending_img.convert("RGBA")
    out = orig.copy()

    WHITE = (244, 244, 244, 255)
    BLACK = (8, 16, 16, 255)

    # Expand ONLY the left side of the speech bubble from x=118 to x=108 into the flat purple cape area,
    # keeping right border (x=194..196) and bottom tail (x=157..161) intact.
    for x in range(110, 195):
        out.putpixel((x, 48), BLACK)

    out.putpixel((109, 49), BLACK)
    for x in range(110, 195):
        out.putpixel((x, 49), WHITE)
    out.putpixel((195, 49), BLACK)

    for y in range(50, 57):
        out.putpixel((108, y), BLACK)
        for x in range(109, 196):
            out.putpixel((x, y), WHITE)
        out.putpixel((196, y), BLACK)

    out.putpixel((109, 57), BLACK)
    for x in range(110, 195):
        out.putpixel((x, 57), WHITE)
    out.putpixel((195, 57), BLACK)

    for x in range(110, 157):
        out.putpixel((x, 58), BLACK)
    for x in range(157, 162):
        out.putpixel((x, 58), WHITE)
    for x in range(162, 195):
        out.putpixel((x, 58), BLACK)

    # (y0, rows) matching the exact 4px/5px micro-pixel style of the original speech bubble
    BUBBLE_GLYPHS: dict[str, tuple[int, list[str]]] = {
        "Т": (51, ["#####", "..#..", "..#..", "..#..", "..#.."]),
        "ы": (52, ["#...#", "###.#", "#.#.#", "###.#"]),
        "п": (52, ["####", "#..#", "#..#", "#..#"]),
        "р": (52, ["###.", "#..#", "###.", "#...", "#..."]),
        "и": (52, ["#..#", "#.##", "##.#", "#..#"]),
        "ш": (52, ["#.#.#", "#.#.#", "#.#.#", "#####"]),
        "ё": (50, ["#..#", "....", ".##.", "#.##", "##..", ".##."]),
        "л": (52, [".###", ".#.#", ".#.#", "#..#"]),
        "з": (52, ["###.", "...#", ".##.", "...#", "###."]),
        "а": (52, [".###", "#..#", "#..#", ".###"]),
        "м": (52, ["#...#", "##.##", "#.#.#", "#...#"]),
        "н": (52, ["#..#", "####", "#..#", "#..#"]),
        "о": (52, [".##.", "#..#", "#..#", ".##."]),
        "й": (50, [".##.", "....", "#..#", "#.##", "##.#", "#..#"]),
        "!": (51, ["#", "#", "#", ".", "#"]),
    }

    phrase = "Ты пришёл за мной!"
    # Compute total width: 1px between letters, 3px for spaces
    total_w = 0
    for i, ch in enumerate(phrase):
        if ch == " ":
            total_w += 2  # +1 from previous char's gap = 3px total space
        else:
            if i > 0 and phrase[i - 1] != " ":
                total_w += 1
            elif i > 0 and phrase[i - 1] == " ":
                total_w += 1
            total_w += len(BUBBLE_GLYPHS[ch][1][0])

    # Center horizontally inside x=109..195 (87px wide)
    cur_x = 109 + (87 - total_w) // 2
    for ch in phrase:
        if ch == " ":
            cur_x += 2
            continue
        y0, rows = BUBBLE_GLYPHS[ch]
        w = len(rows[0])
        for ry, row_s in enumerate(rows):
            for rx, cell in enumerate(row_s):
                if cell == "#":
                    out.putpixel((cur_x + rx, y0 + ry), BLACK)
        cur_x += w + 1

    ending_path = ASSETS_OUT_DIR / "ending.png"
    out.save(ending_path)
    print(f"Wrote {ending_path} ({out.size}, text_width={total_w}px)")

    # Build 4x nearest-neighbor visual comparison of the speech bubble region
    box = (96, 40, 212, 84)
    c_orig = orig.crop(box).resize(((box[2] - box[0]) * 4, (box[3] - box[1]) * 4), Image.Resampling.NEAREST)
    c_new = out.crop(box).resize(((box[2] - box[0]) * 4, (box[3] - box[1]) * 4), Image.Resampling.NEAREST)
    comp = Image.new("RGBA", (c_orig.width + 16, c_orig.height * 2 + 24), (24, 24, 34, 255))
    comp.alpha_composite(c_orig, (8, 8))
    comp.alpha_composite(c_new, (8, c_orig.height + 16))
    comp.save(SCRATCH_DIR / "ending_comparison.png")
    print(f"Saved ending comparison to {SCRATCH_DIR / 'ending_comparison.png'}")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("game_dir", type=Path, help="Folder containing Deepest Sword.exe")
    game_data_dir = parser.parse_args().game_dir / "Deepest Sword_Data"
    ASSETS_OUT_DIR.mkdir(parents=True, exist_ok=True)
    SCRATCH_DIR.mkdir(parents=True, exist_ok=True)

    env0 = UnityPy.load(str(game_data_dir / "sharedassets0.assets"))
    af0 = next(iter(env0.files.values()))

    atlas_tex = af0.objects[33].read()
    assert atlas_tex.m_Name == "pixelFJ8pt1__ Atlas"
    generate_font_atlas(atlas_tex.get_image_data())

    logo_tex = af0.objects[11].read()
    assert logo_tex.m_Name == "Logo"
    generate_russian_logo(logo_tex.image)

    ending_tex = af0.objects[32].read()
    assert ending_tex.m_Name == "ending"
    generate_russian_ending(ending_tex.image)


if __name__ == "__main__":
    main()
