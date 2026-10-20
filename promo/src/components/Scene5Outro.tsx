import React from 'react';
import { interpolate, spring, staticFile, useCurrentFrame, useVideoConfig } from 'remotion';
import { Subtitles } from './Subtitles';
import { SubtitleItem } from '../types';

const subtitles: SubtitleItem[] = [
  {
    startFrame: 0,
    endFrame: 118,
    text: '完全免费开源',
    highlightWords: ['免费', '开源'],
  },
  {
    startFrame: 118,
    endFrame: 281,
    text: '让每一次抬头看时间，都清清楚楚',
    highlightWords: ['清清楚楚'],
  },
];

// 第 5 幕：品牌收尾（抖音安全版：不含网址、二维码、下载引导）
export const Scene5Outro: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();

  // Logo 弹性入场
  const logoSpring = spring({
    frame,
    fps,
    config: { damping: 12, stiffness: 150 },
  });
  const logoScale = interpolate(logoSpring, [0, 1], [0.4, 1]);
  const logoRotate = interpolate(logoSpring, [0, 1], [-12, 0]);

  // 呼吸光晕
  const breathe = 0.5 + 0.5 * Math.sin(frame * 0.08);
  const glowSize = interpolate(breathe, [0, 1], [80, 135]);

  // 品牌名入场
  const titleSpring = spring({
    frame: frame - 14,
    fps,
    config: { damping: 14, stiffness: 180 },
  });
  const titleY = interpolate(titleSpring, [0, 1], [50, 0]);
  const titleOpacity = interpolate(frame - 14, [0, 12], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  // 卖点标签入场
  const tagSpring = spring({
    frame: frame - 34,
    fps,
    config: { damping: 13, stiffness: 190 },
  });
  const tagScale = interpolate(tagSpring, [0, 1], [0.6, 1]);
  const tagOpacity = interpolate(frame - 34, [0, 12], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  // 搜索提示入场（站内引导，合规）
  const hintSpring = spring({
    frame: frame - 76,
    fps,
    config: { damping: 12, stiffness: 175 },
  });
  const hintScale = interpolate(hintSpring, [0, 1], [0.7, 1]);
  const hintOpacity = interpolate(frame - 76, [0, 14], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  // 提示卡呼吸微动
  const hintPulse = 1 + 0.012 * Math.sin(frame * 0.09);

  return (
    <div style={{ position: 'absolute', inset: 0, overflow: 'hidden' }}>
      {/* 中央 Logo */}
      <div
        style={{
          position: 'absolute',
          top: 470,
          left: 0,
          width: 1080,
          display: 'flex',
          justifyContent: 'center',
          transform: `scale(${logoScale}) rotate(${logoRotate}deg)`,
          zIndex: 15,
        }}
      >
        <div style={{ position: 'relative' }}>
          {/* 呼吸光晕 */}
          <div
            style={{
              position: 'absolute',
              top: '50%',
              left: '50%',
              width: glowSize * 2,
              height: glowSize * 2,
              transform: 'translate(-50%, -50%)',
              borderRadius: '50%',
              background:
                'radial-gradient(circle, rgba(56, 189, 248, 0.45) 0%, rgba(56, 189, 248, 0) 70%)',
              filter: 'blur(34px)',
            }}
          />

          <div
            style={{
              width: 210,
              height: 210,
              borderRadius: 48,
              overflow: 'hidden',
              boxShadow:
                '0 20px 60px rgba(0, 0, 0, 0.8), 0 0 46px rgba(56, 189, 248, 0.45)',
              border: '2px solid rgba(255, 255, 255, 0.2)',
              backgroundColor: '#0f172a',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              position: 'relative',
              zIndex: 2,
            }}
          >
            <img
              src={staticFile('images/app-icon.png')}
              alt="任务栏日历"
              style={{ width: 210, height: 210, objectFit: 'contain' }}
            />
          </div>
        </div>
      </div>

      {/* 品牌名与标语 */}
      <div
        style={{
          position: 'absolute',
          top: 762,
          left: 0,
          width: 1080,
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          transform: `translateY(${titleY}px)`,
          opacity: titleOpacity,
          zIndex: 15,
        }}
      >
        <h1
          style={{
            margin: 0,
            fontSize: 86,
            fontWeight: 900,
            color: '#f8fafc',
            letterSpacing: 3,
            textShadow: '0 6px 30px rgba(0, 0, 0, 0.8)',
          }}
        >
          任务栏日历
        </h1>
        <p
          style={{
            margin: '18px 0 0',
            fontSize: 30,
            fontWeight: 600,
            color: '#94a3b8',
            letterSpacing: 2,
          }}
        >
          给 Windows 11 一个像样的日历
        </p>
      </div>

      {/* 卖点标签 */}
      <div
        style={{
          position: 'absolute',
          top: 952,
          left: 0,
          width: 1080,
          display: 'flex',
          justifyContent: 'center',
          gap: 18,
          transform: `scale(${tagScale})`,
          opacity: tagOpacity,
          zIndex: 15,
        }}
      >
        {['🎁 完全免费', '🔓 开源无广告', '💻 Win10/11 通用'].map((tag, i) => (
          <div
            key={i}
            style={{
              padding: '11px 24px',
              borderRadius: 22,
              background: 'rgba(56, 189, 248, 0.12)',
              border: '1px solid rgba(56, 189, 248, 0.35)',
              color: '#7dd3fc',
              fontSize: 22,
              fontWeight: 700,
            }}
          >
            {tag}
          </div>
        ))}
      </div>

      {/* 站内搜索提示（无外链、无二维码、无下载引导） */}
      <div
        style={{
          position: 'absolute',
          top: 1136,
          left: 0,
          width: 1080,
          display: 'flex',
          justifyContent: 'center',
          transform: `scale(${hintScale * hintPulse})`,
          opacity: hintOpacity,
          zIndex: 15,
        }}
      >
        <div
          style={{
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            gap: 14,
            padding: '30px 54px',
            borderRadius: 28,
            background: 'rgba(15, 23, 42, 0.9)',
            border: '1.5px solid rgba(56, 189, 248, 0.4)',
            boxShadow: '0 16px 44px rgba(0, 0, 0, 0.6), 0 0 26px rgba(56, 189, 248, 0.18)',
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
            <span style={{ fontSize: 32 }}>🔍</span>
            <span style={{ fontSize: 30, fontWeight: 700, color: '#cbd5e1' }}>
              在抖音搜索
            </span>
          </div>

          <div
            style={{
              padding: '12px 30px',
              borderRadius: 18,
              background: 'rgba(56, 189, 248, 0.14)',
              border: '1px dashed rgba(56, 189, 248, 0.55)',
            }}
          >
            <span
              style={{
                fontSize: 42,
                fontWeight: 900,
                color: '#38bdf8',
                letterSpacing: 4,
              }}
            >
              任务栏日历
            </span>
          </div>

          <span style={{ fontSize: 22, color: '#64748b', fontWeight: 600 }}>
            评论区置顶有更多介绍
          </span>
        </div>
      </div>

      {/* 底部解说字幕 */}
      <Subtitles items={subtitles} />
    </div>
  );
};
