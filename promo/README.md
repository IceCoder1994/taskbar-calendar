# 任务栏日历 · 宣传短视频（Remotion）

用代码生成 **9:16 竖屏（1080×1920 / 60fps）** 的产品介绍短视频，面向抖音 / 小红书 / 视频号。
成片约 **31 秒**，包含 **晓晓（zh-CN-XiaoxiaoNeural）AI 配音 + 关键词高亮字幕 + 科技感 BGM + UI 音效**。

## 快速开始

```powershell
cd promo
npm install          # 首次安装依赖
npm run start        # 打开 Remotion Studio 逐帧预览（浏览器）
npm run render       # 渲染成片到 out/promo.mp4
```

> 首次渲染会自动下载 Chrome Headless Shell（约 120MB），之后复用缓存。

## 分镜脚本

| 时间 | 场景组件 | 画面 | 解说词（晓晓） |
| --- | --- | --- | --- |
| 0.0s | `Scene1Hook` 痛点 | Win11 任务栏 + "只有数字"的原生日历 → 红叉 + 三条痛点 | 你是不是也觉得，Windows 11 原生的时钟日历，功能实在太单薄了？ |
| 5.4s | `Scene2Reveal` 亮相 | 日历面板升起，深色 → 浅色主题交叉淡入 | 推荐你这款任务栏日历，点击时钟就能秒开，通透的磨砂质感，完美融入系统。 |
| 11.9s | `Scene3Features` 特写 | 网格镜头推近，节气金色高亮、休/班角标脉冲 | 不仅支持农历和二十四节气，每年的法定调休、放假安排都能自动同步，日程一目了然。 |
| 19.7s | `Scene4Metrics` 指标 | 三张数据卡依次砸落：~500KB / ~15MB / 0 广告 | 体积仅有 500KB，内存占用极低，零弹窗、零广告，纯粹又干净。 |
| 26.1s | `Scene5Outro` 收尾 | Logo 呼吸光晕 + 品牌名 + 卖点标签 + 「在抖音搜索」提示 | 完全免费开源，让每一次抬头看时间，都清清楚楚。 |

## 目录结构

```
promo/
├─ src/
│  ├─ index.tsx                 # 注册 Composition（1080x1920 / 60fps / 1866 帧）
│  ├─ Main.tsx                  # 时间轴总调度：5 幕 Sequence + BGM + 音效层
│  ├─ types.ts                  # 字幕类型
│  ├─ data/calendarData.ts      # 2026-10 日历格子数据（脚本生成）
│  └─ components/
│     ├─ Background.tsx         # 动态流光背景 + 网格底纹
│     ├─ TaskbarMock.tsx        # Win11 任务栏 / 时钟 / 鼠标指针模拟
│     ├─ CalendarPanel.tsx      # 日历面板高保真复刻（深浅主题可插值）
│     ├─ Subtitles.tsx          # 关键词高亮 + 弹簧缩放字幕
│     ├─ Scene1Hook.tsx         # 第 1 幕：痛点
│     ├─ Scene2Reveal.tsx       # 第 2 幕：亮相 + 主题切换
│     ├─ Scene3Features.tsx     # 第 3 幕：农历 / 节气 / 休班
│     ├─ Scene4Metrics.tsx      # 第 4 幕：轻量指标
│     └─ Scene5Outro.tsx        # 第 5 幕：品牌收尾（抖音安全版）
├─ public/
│  ├─ audio/                    # 晓晓配音（scene*.mp3）+ BGM + 音效
│  └─ images/                   # app-icon.png（应用图标）
└─ tools/
   └─ gen_calendar_data.py      # 生成日历格子数据
```

## 素材再生成

| 脚本 | 作用 |
| --- | --- |
| `python gen_tts.py` | 用 edge-tts 生成 5 段晓晓配音（`public/audio/scene*.mp3`） |
| `python gen_sfx.py` | 纯波形合成 BGM 与 UI 音效，直接输出 mp3（无版权风险） |
| `python tools/gen_calendar_data.py` | 依据 `../src/TaskbarCalendar/Data/holidays.json` 生成日历格子数据 |

依赖：`pip install edge-tts mutagen lunardate lameenc`

## 修改台词 / 时长

1. 编辑 `gen_tts.py` 里的 `SCENES` 文案，重新运行 `python gen_tts.py`；
2. 用 `mutagen` 读出每段 mp3 时长，换算成帧数（`秒 × 60`）更新 `src/Main.tsx` 的 `SCENE_DURATIONS`；
3. 各幕内部字幕时间点在同目录 `SceneXxx.tsx` 顶部的 `subtitles` 数组里调整；
4. 音效触发帧集中在 `src/Main.tsx` 的 `SFX` 对象，跟着画面关键帧一起改。

## 抖音合规说明

本版为**抖音安全版**，画面、口播、字幕均不含以下内容：

- ❌ 任何网址 / 域名（含 `*.qzz.io`）
- ❌ 二维码（图像识别可直接命中）
- ❌ 「下载」「扫码」「官网」「点击获取」等引流字样
- ❌ 广告法极限词（「最好用」「第一」「最强」等）

转化链路改为**站内闭环**：片尾「在抖音搜索：任务栏日历」→ 评论区置顶 → 主页简介 → 粉丝群。
发布时**标题、文案、话题、评论区**同样不要出现链接与「下载」字样，处罚通常比画面更直接。

> 若日后需要官网 / B 站 / GitHub 版本，可复制 `Scene5Outro.tsx` 为 `Scene5OutroWeb.tsx`
> 保留二维码与下载按钮，再新增一个 `MainWeb` Composition，两条片子共用前 4 幕。

## 出片建议

- 渲染产物：`out/promo.mp4`（H.264 / 1080×1920 / 60fps，约 10MB）；
- 若需统一响度（抖音推荐 -14 LUFS），用 ffmpeg 做一次 `loudnorm`：

```powershell
node_modules\@remotion\compositor-win32-x64-msvc\ffmpeg.exe -i out\promo.mp4 `
  -c:v copy -af "loudnorm=I=-14:TP=-1.5:LRA=11" -c:a aac -b:a 192k `
  -movflags +faststart -y out/promo-final.mp4
```
