import React from 'react';
import { AbsoluteFill, Audio, Sequence, staticFile } from 'remotion';
import { Background } from './components/Background';
import { Scene1Hook } from './components/Scene1Hook';
import { Scene2Reveal } from './components/Scene2Reveal';
import { Scene3Features } from './components/Scene3Features';
import { Scene4Metrics } from './components/Scene4Metrics';
import { Scene5Outro } from './components/Scene5Outro';

// 各场景时长（与 TTS 音频长度对齐，60fps）
const SCENE_DURATIONS = {
  scene1: 321,
  scene2: 392,
  scene3: 468,
  scene4: 384,
  scene5: 281,
};

// 场景之间不留空帧（避免出现短暂"空屏"），靠各场景自身的入场动效衔接
const GAP = 0;

// 计算各场景起始帧
const SCENE_STARTS = {
  scene1: 0,
  scene2: SCENE_DURATIONS.scene1 + GAP,
  scene3: SCENE_DURATIONS.scene1 + SCENE_DURATIONS.scene2 + GAP * 2,
  scene4:
    SCENE_DURATIONS.scene1 + SCENE_DURATIONS.scene2 + SCENE_DURATIONS.scene3 + GAP * 3,
  scene5:
    SCENE_DURATIONS.scene1 +
    SCENE_DURATIONS.scene2 +
    SCENE_DURATIONS.scene3 +
    SCENE_DURATIONS.scene4 +
    GAP * 4,
};

// 视频总时长（末尾留 20 帧余韵）
export const TOTAL_FRAMES =
  SCENE_DURATIONS.scene1 +
  SCENE_DURATIONS.scene2 +
  SCENE_DURATIONS.scene3 +
  SCENE_DURATIONS.scene4 +
  SCENE_DURATIONS.scene5 +
  GAP * 4 +
  20;

// 音效触发帧（全局帧）
const SFX = {
  // 第 1 幕：点击任务栏时钟
  click1: SCENE_STARTS.scene1 + 34,
  // 第 2 幕：日历弹出 + 主题就绪提示音
  whoosh2: SCENE_STARTS.scene2 + 10,
  chime2: SCENE_STARTS.scene2 + 34,
  // 第 3 幕：节气高亮 + 休班角标点亮
  pop3a: SCENE_STARTS.scene3 + 46,
  pop3b: SCENE_STARTS.scene3 + 60,
  chime3: SCENE_STARTS.scene3 + 165,
  // 第 4 幕：三张指标卡片依次砸落
  pop4a: SCENE_STARTS.scene4 + 15,
  pop4b: SCENE_STARTS.scene4 + 55,
  pop4c: SCENE_STARTS.scene4 + 95,
  // 第 5 幕：品牌收尾出现
  chime5: SCENE_STARTS.scene5 + 60,
};

// 音效播放辅助组件
const Sfx: React.FC<{ at: number; file: string; volume?: number }> = ({
  at,
  file,
  volume = 0.6,
}) => (
  <Sequence from={at} durationInFrames={120} layout="none">
    <Audio src={staticFile(file)} volume={volume} />
  </Sequence>
);

export const Main: React.FC = () => {
  return (
    <AbsoluteFill style={{ backgroundColor: '#070b14' }}>
      {/* 全局持续的动态科技背景 */}
      <Background />

      {/* ============ 第 1 幕：痛点引入 ============ */}
      <Sequence
        from={SCENE_STARTS.scene1}
        durationInFrames={SCENE_DURATIONS.scene1}
        name="01-痛点引入"
      >
        <Scene1Hook />
        <Audio src={staticFile('audio/scene1.mp3')} volume={1} />
      </Sequence>

      {/* ============ 第 2 幕：惊艳亮相 ============ */}
      <Sequence
        from={SCENE_STARTS.scene2}
        durationInFrames={SCENE_DURATIONS.scene2}
        name="02-惊艳亮相"
      >
        <Scene2Reveal />
        <Audio src={staticFile('audio/scene2.mp3')} volume={1} />
      </Sequence>

      {/* ============ 第 3 幕：功能特写 ============ */}
      <Sequence
        from={SCENE_STARTS.scene3}
        durationInFrames={SCENE_DURATIONS.scene3}
        name="03-功能特写"
      >
        <Scene3Features />
        <Audio src={staticFile('audio/scene3.mp3')} volume={1} />
      </Sequence>

      {/* ============ 第 4 幕：轻量指标 ============ */}
      <Sequence
        from={SCENE_STARTS.scene4}
        durationInFrames={SCENE_DURATIONS.scene4}
        name="04-轻量指标"
      >
        <Scene4Metrics />
        <Audio src={staticFile('audio/scene4.mp3')} volume={1} />
      </Sequence>

      {/* ============ 第 5 幕：行动号召 ============ */}
      <Sequence
        from={SCENE_STARTS.scene5}
        durationInFrames={SCENE_DURATIONS.scene5}
        name="05-行动号召"
      >
        <Scene5Outro />
        <Audio src={staticFile('audio/scene5.mp3')} volume={1} />
      </Sequence>

      {/* ============ 背景音乐（全程循环，低音量铺垫） ============ */}
      <Audio
        src={staticFile('audio/bgm.wav')}
        volume={(f) => {
          // 开头 20 帧淡入
          const fadeIn = Math.min(1, f / 20);
          // 结尾 40 帧淡出
          const fadeOut = Math.min(1, (TOTAL_FRAMES - f) / 40);
          return 0.14 * fadeIn * fadeOut;
        }}
        loop
      />

      {/* ============ 音效层 ============ */}
      <Sfx at={SFX.click1} file="audio/click.wav" volume={0.75} />
      <Sfx at={SFX.whoosh2} file="audio/whoosh.wav" volume={0.5} />
      <Sfx at={SFX.chime2} file="audio/chime.wav" volume={0.35} />
      <Sfx at={SFX.pop3a} file="audio/pop.wav" volume={0.4} />
      <Sfx at={SFX.pop3b} file="audio/pop.wav" volume={0.4} />
      <Sfx at={SFX.chime3} file="audio/chime.wav" volume={0.35} />
      <Sfx at={SFX.pop4a} file="audio/pop.wav" volume={0.45} />
      <Sfx at={SFX.pop4b} file="audio/pop.wav" volume={0.45} />
      <Sfx at={SFX.pop4c} file="audio/pop.wav" volume={0.45} />
      <Sfx at={SFX.chime5} file="audio/chime.wav" volume={0.4} />
    </AbsoluteFill>
  );
};
