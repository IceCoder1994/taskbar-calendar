# 录制日历面板演示：按时间轴模拟鼠标操作，同时连续抓屏保存 PNG 帧
# 用法: python record_demo.py --left 1500 --top 544 --right 1900 --bottom 1080 --panel-left 1520 --panel-top 564 --fps 10 --outdir <目录>
#
# 说明：元素相对坐标基于 UI Automation 实测（面板 360x460）：
#   上一月 "<"   : (22, 25)
#   下一月 ">"   : (140, 25)
#   日期格行高约 56.7px，首行中心 y=91，第 4 行中心 y=261；首列中心 x=35，列宽约 48.4
import argparse
import ctypes
import os
import threading
import time

from PIL import ImageGrab

user32 = ctypes.windll.user32

MOUSEEVENTF_LEFTDOWN = 0x0002
MOUSEEVENTF_LEFTUP = 0x0004


class POINT(ctypes.Structure):
    _fields_ = [('x', ctypes.c_long), ('y', ctypes.c_long)]


def move_to(x, y, steps=14, step_delay=0.008):
    """平滑移动鼠标（分步插值，避免瞬移造成的生硬感）"""
    pt = POINT()
    user32.GetCursorPos(ctypes.byref(pt))
    sx, sy = pt.x, pt.y
    for i in range(1, steps + 1):
        user32.SetCursorPos(int(sx + (x - sx) * i / steps), int(sy + (y - sy) * i / steps))
        time.sleep(step_delay)
    user32.SetCursorPos(x, y)


def click(x, y):
    move_to(x, y)
    time.sleep(0.06)
    user32.mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0)
    time.sleep(0.045)
    user32.mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--left', type=int, required=True)
    parser.add_argument('--top', type=int, required=True)
    parser.add_argument('--right', type=int, required=True)
    parser.add_argument('--bottom', type=int, required=True)
    parser.add_argument('--panel-left', type=int, required=True)
    parser.add_argument('--panel-top', type=int, required=True)
    parser.add_argument('--clock-x', type=int, required=True)
    parser.add_argument('--clock-y', type=int, required=True)
    parser.add_argument('--fps', type=int, default=10)
    parser.add_argument('--outdir', required=True)
    args = parser.parse_args()

    os.makedirs(args.outdir, exist_ok=True)
    for name in os.listdir(args.outdir):
        if name.startswith('f') and name.endswith('.png'):
            os.remove(os.path.join(args.outdir, name))

    pl, pt = args.panel_left, args.panel_top

    # 面板内元素绝对坐标（相对偏移来自 UIA 实测）
    clock = (args.clock_x, args.clock_y)       # 任务栏时钟中心（由调用方从程序日志实测传入）
    day25 = (pl + 228, pt + 261)               # 9 月 25 日（第 4 行第 5 列）
    prev_month = (pl + 22, pt + 25)            # 上一月
    next_month = (pl + 140, pt + 25)           # 下一月
    outside = (100, 100)                       # 面板外（用于关闭面板）

    # 时间轴（毫秒 -> 动作），在独立线程执行以免影响抓屏节奏
    timeline = [
        (500, clock, '点击时钟弹出面板'),
        (2000, day25, '选中日期'),
        (3200, next_month, '翻到下一月'),
        (4400, prev_month, '翻回上一月'),
        (5600, outside, '点击面板外关闭'),
    ]
    total_ms = 6800

    def run_actions():
        start = time.perf_counter()
        for at_ms, pos, label in timeline:
            wait = start + at_ms / 1000.0 - time.perf_counter()
            if wait > 0:
                time.sleep(wait)
            print(f'  [{at_ms:>5}ms] {label} -> {pos}')
            click(*pos)

    # 预置鼠标位置，避免录制开头出现长距离滑动
    user32.SetCursorPos(1750, 1000)

    worker = threading.Thread(target=run_actions, daemon=True)
    worker.start()

    bbox = (args.left, args.top, args.right, args.bottom)
    interval = 1.0 / args.fps
    total_frames = int(total_ms / 1000.0 * args.fps)

    start = time.perf_counter()
    count = 0
    cursor_track = []
    cur_pt = POINT()
    while count < total_frames:
        user32.GetCursorPos(ctypes.byref(cur_pt))
        frame = ImageGrab.grab(bbox=bbox, all_screens=False)
        frame.save(os.path.join(args.outdir, f'f{count:04d}.png'))
        # 记录帧内坐标（抓屏不含光标，合成 GIF 时再绘制）
        cursor_track.append((count, cur_pt.x - args.left, cur_pt.y - args.top))
        count += 1
        target = start + count * interval
        wait = target - time.perf_counter()
        if wait > 0:
            time.sleep(wait)

    with open(os.path.join(args.outdir, 'cursor.txt'), 'w', encoding='utf-8') as fh:
        for idx, cx, cy in cursor_track:
            fh.write(f'{idx},{cx},{cy}\n')

    worker.join(timeout=2)
    elapsed = time.perf_counter() - start
    print(f'抓取完成: {count} 帧 / {elapsed:.2f}s -> {args.outdir}')


if __name__ == '__main__':
    main()
