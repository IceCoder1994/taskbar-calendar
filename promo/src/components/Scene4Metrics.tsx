import React from 'react';
import { interpolate, spring, useCurrentFrame, useVideoConfig } from 'remotion';
import { Subtitles } from './Subtitles';
import { SubtitleItem } from '../types';

const subtitles: SubtitleItem[] = [
  {
    startFrame: 0,
    endFrame: 140,
    text: '体积仅有 500KB',
    highlightWords: ['500KB'],
  },
  {
    startFrame: 140,
    endFrame: 250,
    text: '内存占用极低',
    highlightWords: ['内存占用极低'],
  },
  {
    startFrame: 250,
    endFrame: 385,
    text: '零弹窗、零广告，纯粹又干净',
    highlightWords: ['零弹窗', '零广告'],
  },
];

// 第 4 幕：硬核轻量指标徽章
export const Scene4Metrics: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();

  // 标题入场
  const titleSpring = spring({
    frame,
    fps,
    config: { damping: 14, stiffness: 180 },
  });
  const titleY = interpolate(titleSpring, [0, 1], [-50, 0]);

  // 三组硬核数据指标
  const metrics = [
    {
      delay: 15,
      value: '~500',
      unit: 'KB',
      label: '安装包体积',
      sub: '比表情包还小',
      color: '#38bdf8',
      icon: '📦',
    },
    {
      delay: 55,
      value: '~15',
      unit: 'MB',
      label: '内存占用',
      sub: '比标签页还轻',
      color: '#a78bfa',
      icon: '⚡',
    },
    {
      delay: 95,
      value: '0',
      unit: '广告',
      label: '弹窗与广告',
      sub: '100% 开源免费',
      color: '#34d399',
      icon: '🛡️',
    },
  ];

  return (
    <div style={{ position: 'absolute', inset: 0, overflow: 'hidden' }}>
      {/* 顶部标题 */}
      <div
        style={{
          position: 'absolute',
          top: 130,
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
            background: 'rgba(52, 211, 153, 0.15)',
            border: '1px solid rgba(52, 211, 153, 0.4)',
            color: '#34d399',
            fontSize: 22,
            fontWeight: 700,
            letterSpacing: 2,
            marginBottom: 16,
          }}
        >
          极致轻量 · 干净纯粹
        </div>

        <h1
          style={{
            margin: 0,
            fontSize: 66,
            fontWeight: 900,
            color: '#f8fafc',
            textAlign: 'center',
            lineHeight: 1.25,
          }}
        >
          比想象中更轻
          <br />
          <span
            style={{
              background: 'linear-gradient(135deg, #34d399 0%, #38bdf8 100%)',
              WebkitBackgroundClip: 'text',
              WebkitTextFillColor: 'transparent',
            }}
          >
            快到感觉不到存在
          </span>
        </h1>
      </div>

      {/* 三张指标大卡片 */}
      <div
        style={{
          position: 'absolute',
          top: 600,
          left: 0,
          width: 1080,
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          gap: 48,
          zIndex: 10,
        }}
      >
        {metrics.map((m, idx) => {
          const mSpring = spring({
            frame: frame - m.delay,
            fps,
            config: { damping: 12, stiffness: 200 },
          });
          const mY = frame >= m.delay ? interpolate(mSpring, [0, 1], [120, 0]) : 120;
          const mScale = frame >= m.delay ? interpolate(mSpring, [0, 1], [0.7, 1]) : 0.7;
          const mOpacity = frame >= m.delay ? interpolate(mSpring, [0, 1], [0, 1]) : 0;

          // 数字滚动计数效果
          const countProgress = frame >= m.delay ? interpolate(mSpring, [0, 1], [0, 1]) : 0;

          return (
            <div
              key={idx}
              style={{
                width: 860,
                position: 'relative',
                display: 'flex',
                alignItems: 'center',
                gap: 32,
                padding: '34px 40px',
                borderRadius: 28,
                background: 'rgba(15, 23, 42, 0.9)',
                backdropFilter: 'blur(25px)',
                border: `2px solid ${m.color}55`,
                boxShadow: `0 20px 50px rgba(0, 0, 0, 0.6), 0 0 30px ${m.color}22`,
                transform: `translateY(${mY}px) scale(${mScale})`,
                opacity: mOpacity,
              }}
            >
              {/* 左侧图标 */}
              <div
                style={{
                  width: 90,
                  height: 90,
                  borderRadius: 22,
                  backgroundColor: `${m.color}22`,
                  border: `2px solid ${m.color}55`,
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  fontSize: 44,
                  flexShrink: 0,
                }}
              >
                {m.icon}
              </div>

              {/* 中间数值 */}
              <div style={{ display: 'flex', flexDirection: 'column', flex: 1 }}>
                <div style={{ display: 'flex', alignItems: 'baseline', gap: 8 }}>
                  <span
                    style={{
                      fontSize: 76,
                      fontWeight: 900,
                      color: m.color,
                      lineHeight: 1,
                      textShadow: `0 0 30px ${m.color}66`,
                      letterSpacing: -2,
                    }}
                  >
                    {m.value}
                  </span>
                  <span
                    style={{
                      fontSize: 32,
                      fontWeight: 800,
                      color: m.color,
                      opacity: 0.9,
                    }}
                  >
                    {m.unit}
                  </span>
                </div>
                <span
                  style={{
                    fontSize: 24,
                    fontWeight: 700,
                    color: '#e2e8f0',
                    marginTop: 8,
                  }}
                >
                  {m.label}
                </span>
              </div>

              {/* 右侧副说明 */}
              <div
                style={{
                  padding: '12px 18px',
                  borderRadius: 14,
                  backgroundColor: `${m.color}15`,
                  border: `1px solid ${m.color}44`,
                  color: m.color,
                  fontSize: 18,
                  fontWeight: 700,
                  textAlign: 'center',
                  lineHeight: 1.35,
                  width: 168,
                  flexShrink: 0,
                }}
              >
                {m.sub}
              </div>

              {/* 底部进度条展示 */}
              <div
                style={{
                  position: 'absolute',
                  bottom: 0,
                  left: 0,
                  height: 4,
                  width: `${countProgress * 100}%`,
                  backgroundColor: m.color,
                  borderRadius: '0 0 0 28px',
                  boxShadow: `0 0 12px ${m.color}`,
                }}
              />
            </div>
          );
        })}
      </div>

      {/* 底部解说字幕 */}
      <Subtitles items={subtitles} />
    </div>
  );
};
