# 将 PNG 帧序列合成为优化后的 GIF（统一调色板 + 合成鼠标指针）
# 用法: python build_gif.py --indir <帧目录> --out site/assets/demo-dark.gif --fps 10 [--scale 1.0] [--colors 128] [--cursor <轨迹文件>]
import argparse
import glob
import os

from PIL import Image, ImageDraw

# 鼠标指针形状（相对坐标，模拟 Windows 默认箭头光标）
CURSOR_SHAPE = [(0, 0), (0, 17), (4, 13), (7, 20), (10, 19), (7, 12), (12, 12)]


def draw_cursor(img, pos, scale=1.0):
    """在帧上绘制鼠标指针（系统抓屏不含光标，需手工合成）"""
    x, y = pos
    if scale != 1.0:
        x, y = x * scale, y * scale
    w = 12 * scale
    h = 20 * scale
    if x < -w or y < -h or x > img.width + w or y > img.height + h:
        return  # 光标在画面外，跳过
    pts = [(int(x + px * scale), int(y + py * scale)) for px, py in CURSOR_SHAPE]
    draw = ImageDraw.Draw(img)
    draw.line(pts + [pts[0]], fill=(0, 0, 0), width=max(1, int(2 * scale)), joint='curve')
    draw.polygon(pts, fill=(255, 255, 255))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--indir', required=True)
    parser.add_argument('--out', required=True)
    parser.add_argument('--fps', type=int, default=10)
    parser.add_argument('--scale', type=float, default=1.0)
    parser.add_argument('--colors', type=int, default=128)
    parser.add_argument('--cursor', help='光标轨迹文件（每行: 帧号,帧内x,帧内y）')
    parser.add_argument('--palette-frame', type=int, default=-1,
                        help='用指定帧作为全局调色板来源，默认取中间帧')
    parser.add_argument('--start', type=int, default=0)
    parser.add_argument('--end', type=int, default=-1)
    args = parser.parse_args()

    files = sorted(glob.glob(os.path.join(args.indir, 'f*.png')))
    if not files:
        raise SystemExit(f'未在 {args.indir} 找到帧文件')
    files = files[args.start:] if args.end < 0 else files[args.start:args.end + 1]

    # 读取光标轨迹（帧号 -> 帧内坐标）
    cursor_map = {}
    if args.cursor and os.path.exists(args.cursor):
        with open(args.cursor, encoding='utf-8') as fh:
            for line in fh:
                parts = line.strip().split(',')
                if len(parts) == 3:
                    cursor_map[int(parts[0])] = (int(parts[1]), int(parts[2]))
        print(f'已载入光标轨迹 {len(cursor_map)} 帧')

    images = []
    for index, path in enumerate(files):
        img = Image.open(path).convert('RGB')
        if args.scale != 1.0:
            img = img.resize((int(img.width * args.scale), int(img.height * args.scale)), Image.LANCZOS)
        if index in cursor_map:
            draw_cursor(img, cursor_map[index], args.scale)
        images.append(img)

    # 用指定帧（默认中间帧）构建全局调色板，保证各帧颜色一致
    palette_index = args.palette_frame if args.palette_frame >= 0 else len(images) // 2
    palette_index = min(palette_index, len(images) - 1)
    palette = images[palette_index].quantize(colors=args.colors, method=Image.MEDIANCUT)

    frames = [img.quantize(palette=palette, dither=Image.FLOYDSTEINBERG) for img in images]

    duration = max(1, int(round(1000 / args.fps)))
    os.makedirs(os.path.dirname(os.path.abspath(args.out)), exist_ok=True)
    frames[0].save(
        args.out,
        save_all=True,
        append_images=frames[1:],
        duration=duration,
        loop=0,
        optimize=True,
        disposal=2,
    )

    size_kb = os.path.getsize(args.out) / 1024
    print(f'GIF 已生成: {args.out}')
    print(f'  {len(frames)} 帧 · {duration}ms/帧 · {frames[0].width}x{frames[0].height} · {size_kb:.1f} KB')


if __name__ == '__main__':
    main()
