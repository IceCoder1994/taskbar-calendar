import asyncio
import json

import edge_tts
from mutagen.mp3 import MP3

# 配音声音：微软「晓晓」温和知性女声
VOICE = 'zh-CN-XiaoxiaoNeural'

# 语速：+12% 让 30 秒短视频节奏更紧凑（抖音常见语速）
RATE = '+12%'

# 五幕台词，与 src/components/SceneXxx.tsx 中的字幕一一对应
SCENES = [
    ('scene1', '你是不是也觉得，Windows 11 原生的时钟日历，功能实在太单薄了？'),
    ('scene2', '推荐你这款任务栏日历，点击时钟就能秒开，通透的磨砂质感，完美融入系统。'),
    ('scene3', '不仅支持农历和二十四节气，每年的法定调休、放假安排都能自动同步，日程一目了然。'),
    ('scene4', '体积仅有 500KB，内存占用极低，零弹窗、零广告，纯粹又干净。'),
    ('scene5', '完全免费开源，让每一次抬头看时间，都清清楚楚。'),
]


async def generate():
    report = []
    total = 0.0

    for scene_id, text in SCENES:
        mp3_path = f'public/audio/{scene_id}.mp3'

        communicate = edge_tts.Communicate(text=text, voice=VOICE, rate=RATE)
        await communicate.save(mp3_path)

        # 读取实际时长，换算成 60fps 下的帧数
        duration = MP3(mp3_path).info.length
        frames = round(duration * 60)
        total += duration
        report.append({'id': scene_id, 'text': text, 'seconds': round(duration, 2), 'frames': frames})
        print(f'{scene_id}: {duration:.2f}s -> {frames} 帧')

    print(f'\n合计：{total:.2f}s -> {round(total * 60)} 帧')
    print('\n请把上面的 frames 值填进 src/Main.tsx 的 SCENE_DURATIONS：')
    for item in report:
        print(f"  {item['id']}: {item['frames']},")

    with open('public/audio/manifest.json', 'w', encoding='utf-8') as f:
        json.dump(report, f, ensure_ascii=False, indent=2)


if __name__ == '__main__':
    asyncio.run(generate())
