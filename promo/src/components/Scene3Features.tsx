import React from 'react';
import { interpolate, spring, useCurrentFrame, useVideoConfig } from 'remotion';
import { CalendarPanel } from './CalendarPanel';
import { Subtitles } from './Subtitles';
import { SubtitleItem } from '../types';

const subtitles: SubtitleItem[] = [
  {
    startFrame: 0,
    endFrame: 148,
    text: '不仅支持农历和二十四节气',
    highlightWords: ['农历', '二十四节气'],
  },
  {
    startFrame: 148,
    endFrame: 318,
    text: '每年的法定调休、放假安排',
    highlightWords: ['法定调休', '放假安排'],
  },
  {
    startFrame: 318,
    endFrame: 468,
    text: '自动同步，日程一目了然',
    highlightWords: ['自动同步', '一目了然'],
  },
];

// 第 3 幕：农历 / 节气 / 休班 功能特写
export const Scene3Features: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();

  // 标题入场
  const titleSpring = spring({
    frame,
    fps,
    config: { damping: 14, stiffness: 180 },
  });
  const titleY = interpolate(titleSpring, [0, 1], [-50, 0]);

  // 面板入场 + 缓慢推近（镜头特写），起始即已就位
  const panelSpring = spring({
    frame: frame + 22,
    fps,
    config: { damping: 14, stiffness: 150 },
  });
  const panelScale = interpolate(panelSpring, [0, 1], [0.9, 1]);
  const panelOpacity = 1;

  // 相机推近：先整体，再逐步聚焦
  const camZoom = interpolate(frame, [0, 60, 150, 300, 468], [1, 1.02, 1.05, 1.05, 1.02], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  // 高亮时机
  const highlightTerm = frame >= 40;
  const highlightHoliday = frame >= 150;

  // 说明卡片
  const cards = [
    {
      delay: 55,
      text: '🏮 农历 · 干支 · 节气',
      color: '#fbbf24',
      top: 1300,
    },
    {
      delay: 175,
      text: '🔴 休 / 🔵 班 一眼看清',
      color: '#f87171',
      top: 1300,
    },
  ];

  return (
    <div style={{ position: 'absolute', inset: 0, overflow: 'hidden' }}>
      {/* 顶部标题 */}
      <div
        style={{
          position: 'absolute',
          top: 96,
          left: 0,
          width: 1080,
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          transform: `translateY(${titleY}px)`,
          zIndex: 20,
        }}
      >
        <div
          style={{
            padding: '8px 26px',
            borderRadius: 20,
            background: 'rgba(251, 191, 36, 0.14)',
            border: '1px solid rgba(251, 191, 36, 0.4)',
            color: '#fbbf24',
            fontSize: 22,
            fontWeight: 700,
            letterSpacing: 2,
            marginBottom: 16,
          }}
        >
          中国式日历 · 一步到位
        </div>

        <h1
          style={{
            margin: 0,
            fontSize: 62,
            fontWeight: 900,
            color: '#f8fafc',
            textAlign: 'center',
            lineHeight: 1.28,
          }}
        >
          农历 · 节气 · 调休
          <br />
          <span
            style={{
              background: 'linear-gradient(135deg, #fde68a 0%, #f59e0b 100%)',
              WebkitBackgroundClip: 'text',
              WebkitTextFillColor: 'transparent',
            }}
          >
            一眼看清所有安排
          </span>
        </h1>
      </div>

      {/* 中央日历面板特写 */}
      <div
        style={{
          position: 'absolute',
          top: 480,
          left: 0,
          width: 1080,
          display: 'flex',
          justifyContent: 'center',
          transform: `scale(${panelScale * camZoom})`,
          opacity: panelOpacity,
          zIndex: 10,
        }}
      >
        <div
          style={{
            borderRadius: 20,
            overflow: 'hidden',
            boxShadow:
              '0 30px 90px rgba(0, 0, 0, 0.85), 0 0 60px rgba(251, 191, 36, 0.18)',
            border: '1.5px solid rgba(255, 255, 255, 0.14)',
            zoom: 1.7,
          }}
        >
          <CalendarPanel
            theme="dark"
            highlightTerm={highlightTerm}
            highlightHoliday={highlightHoliday}
            highlightStart={highlightTerm ? 40 : 0}
          />
        </div>
      </div>

      {/* 说明卡片 */}
      {cards.map((c, idx) => {
        const cSpring = spring({
          frame: frame - c.delay,
          fps,
          config: { damping: 13, stiffness: 210 },
        });
        const cScale = frame >= c.delay ? interpolate(cSpring, [0, 1], [0.5, 1]) : 0.5;
        const cOpacity = frame >= c.delay ? interpolate(cSpring, [0, 1], [0, 1]) : 0;

        return (
          <div
            key={idx}
            style={{
              position: 'absolute',
              top: c.top + idx * 96,
              left: 0,
              width: 1080,
              display: 'flex',
              justifyContent: 'center',
              transform: `scale(${cScale})`,
              opacity: cOpacity,
              zIndex: 25,
              pointerEvents: 'none',
            }}
          >
            <div
              style={{
                padding: '13px 30px',
                borderRadius: 18,
                background: 'rgba(15, 23, 42, 0.93)',
                border: `1.5px solid ${c.color}66`,
                color: c.color,
                fontSize: 25,
                fontWeight: 800,
                boxShadow: `0 12px 30px rgba(0, 0, 0, 0.6), 0 0 18px ${c.color}22`,
                whiteSpace: 'nowrap',
              }}
            >
              {c.text}
            </div>
          </div>
        );
      })}

      {/* 解说字幕 */}
      <Subtitles items={subtitles} />
    </div>
  );
};
