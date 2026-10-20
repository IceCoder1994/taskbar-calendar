import React from 'react';
import { interpolate, spring, useCurrentFrame, useVideoConfig } from 'remotion';
import { Subtitles } from './Subtitles';
import { TaskbarMock } from './TaskbarMock';
import { SubtitleItem } from '../types';

const subtitles: SubtitleItem[] = [
  {
    startFrame: 0,
    endFrame: 92,
    text: '你是不是也觉得',
    highlightWords: ['你是不是也觉得'],
  },
  {
    startFrame: 92,
    endFrame: 210,
    text: 'Windows 11 原生的时钟日历',
    highlightWords: ['Windows 11', '原生'],
  },
  {
    startFrame: 210,
    endFrame: 321,
    text: '功能实在太单薄了？',
    highlightWords: ['太单薄了'],
  },
];

// 第 1 幕：痛点引入（先抑后扬）
export const Scene1Hook: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();

  // 标题弹簧入场
  const titleSpring = spring({
    frame,
    fps,
    config: { damping: 15, stiffness: 200 },
  });
  const titleY = interpolate(titleSpring, [0, 1], [-60, 0]);
  const titleOpacity = interpolate(frame, [0, 18], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  // 开场即已弹出"简陋"的原生日历（避免开场空镜），鼠标点击动作稍后重复演示
  const clickFrame = 34;
  const isPopped = true;
  const popSpring = spring({
    frame: frame + 6,
    fps,
    config: { damping: 15, stiffness: 190 },
  });
  const popScale = interpolate(popSpring, [0, 1], [0.9, 1]);
  const popY = interpolate(popSpring, [0, 1], [40, 0]);
  const popOpacity = 1;

  // 红色警示大叉
  const crossFrame = 96;
  const showCross = frame >= crossFrame;
  const crossSpring = spring({
    frame: frame - crossFrame,
    fps,
    config: { damping: 9, stiffness: 220 },
  });
  const crossScale = showCross ? interpolate(crossSpring, [0, 1], [0.2, 1]) : 0;

  // 三条痛点
  const painPoints = ['缺失农历与干支纪年', '没有二十四节气标识', '看不到法定调休与放假'];

  return (
    <div style={{ position: 'absolute', inset: 0, overflow: 'hidden' }}>
      {/* 顶部标题 */}
      <div
        style={{
          position: 'absolute',
          top: 110,
          left: 0,
          width: 1080,
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          transform: `translateY(${titleY}px)`,
          opacity: titleOpacity,
          zIndex: 20,
        }}
      >
        <div
          style={{
            padding: '8px 24px',
            borderRadius: 20,
            background: 'rgba(239, 68, 68, 0.14)',
            border: '1px solid rgba(239, 68, 68, 0.4)',
            color: '#f87171',
            fontSize: 22,
            fontWeight: 700,
            letterSpacing: 2,
            marginBottom: 18,
          }}
        >
          痛点直击 · 每天都在点
        </div>

        <h1
          style={{
            margin: 0,
            fontSize: 62,
            fontWeight: 900,
            color: '#f8fafc',
            letterSpacing: 1,
            textAlign: 'center',
            lineHeight: 1.28,
            textShadow: '0 4px 20px rgba(0, 0, 0, 0.6)',
          }}
        >
          还在忍受 Win 11
          <br />
          <span
            style={{
              background: 'linear-gradient(135deg, #fca5a5 0%, #ef4444 100%)',
              WebkitBackgroundClip: 'text',
              WebkitTextFillColor: 'transparent',
            }}
          >
            简陋的原生时钟吗？
          </span>
        </h1>
      </div>

      {/* 中间：Win11 原生日历（功能缺失演示） */}
      {isPopped && (
        <div
          style={{
            position: 'absolute',
            top: 450,
            left: 0,
            width: 1080,
            display: 'flex',
            justifyContent: 'center',
            transform: `translateY(${popY}px) scale(${popScale})`,
            opacity: popOpacity,
            zIndex: 15,
          }}
        >
          <div
            style={{
              position: 'relative',
              width: 600,
              borderRadius: 16,
              background: 'rgba(32, 36, 44, 0.9)',
              backdropFilter: 'blur(24px)',
              border: '1px solid rgba(255, 255, 255, 0.1)',
              boxShadow: '0 24px 60px rgba(0, 0, 0, 0.7)',
              padding: '26px 30px 30px',
              boxSizing: 'border-box',
            }}
          >
            {/* 头部 */}
            <div
              style={{
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'center',
                paddingBottom: 14,
              }}
            >
              <span style={{ fontSize: 26, fontWeight: 700, color: '#d8dde5' }}>
                2026 年 10 月
              </span>
              <span style={{ fontSize: 20, color: '#7c8698', letterSpacing: 6 }}>‹ ›</span>
            </div>

            {/* 星期头 */}
            <div
              style={{
                display: 'grid',
                gridTemplateColumns: 'repeat(7, 1fr)',
                textAlign: 'center',
                padding: '10px 0 12px',
                fontSize: 17,
                color: '#69737f',
                fontWeight: 600,
              }}
            >
              {['一', '二', '三', '四', '五', '六', '日'].map((w, idx) => (
                <span key={idx}>{w}</span>
              ))}
            </div>

            {/* 纯数字网格（无农历、无节气、无休班） */}
            <div
              style={{
                display: 'grid',
                gridTemplateColumns: 'repeat(7, 1fr)',
                gap: 6,
              }}
            >
              {Array.from({ length: 35 }).map((_, idx) => {
                // 2026 年 10 月 1 日是周四，周一起始时前面空 3 格
                const day = idx - 2;
                const isValid = day >= 1 && day <= 31;
                const isToday = day === 20;
                return (
                  <div
                    key={idx}
                    style={{
                      height: 56,
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      fontSize: 22,
                      fontWeight: isToday ? 700 : 500,
                      color: isToday ? '#ffffff' : isValid ? '#b9c1cc' : '#4a525e',
                      backgroundColor: isToday ? '#2563eb' : 'transparent',
                      borderRadius: 8,
                    }}
                  >
                    {isValid ? day : ''}
                  </div>
                );
              })}
            </div>

            {/* 红色警示层 */}
            {showCross && (
              <div
                style={{
                  position: 'absolute',
                  inset: 0,
                  borderRadius: 16,
                  background: 'rgba(239, 68, 68, 0.16)',
                  border: '2px solid rgba(239, 68, 68, 0.6)',
                  display: 'flex',
                  flexDirection: 'column',
                  alignItems: 'center',
                  justifyContent: 'center',
                  transform: `scale(${crossScale})`,
                  boxShadow: '0 0 60px rgba(239, 68, 68, 0.35) inset',
                }}
              >
                <div
                  style={{
                    fontSize: 100,
                    lineHeight: 1,
                    marginBottom: 6,
                    filter: 'drop-shadow(0 8px 24px rgba(239, 68, 68, 0.8))',
                  }}
                >
                  ❌
                </div>
                <span
                  style={{
                    fontSize: 26,
                    fontWeight: 800,
                    color: '#fee2e2',
                    backgroundColor: 'rgba(239, 68, 68, 0.9)',
                    padding: '6px 22px',
                    borderRadius: 18,
                  }}
                >
                  功能严重缺失
                </span>
              </div>
            )}
          </div>
        </div>
      )}

      {/* 底部：三条痛点依次浮现 */}
      <div
        style={{
          position: 'absolute',
          top: 1010,
          left: 0,
          width: 1080,
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          gap: 14,
          zIndex: 30,
        }}
      >
        {painPoints.map((text, idx) => {
          const itemDelay = crossFrame + 12 + idx * 12;
          const itemSpring = spring({
            frame: frame - itemDelay,
            fps,
            config: { damping: 14, stiffness: 200 },
          });
          const itemY = frame >= itemDelay ? interpolate(itemSpring, [0, 1], [40, 0]) : 40;
          const itemOp = frame >= itemDelay ? interpolate(itemSpring, [0, 1], [0, 1]) : 0;

          return (
            <div
              key={idx}
              style={{
                width: 760,
                transform: `translateY(${itemY}px)`,
                opacity: itemOp,
                backgroundColor: 'rgba(15, 23, 42, 0.92)',
                border: '1.5px solid rgba(239, 68, 68, 0.45)',
                boxShadow: '0 12px 30px rgba(0, 0, 0, 0.5)',
                padding: '16px 28px',
                borderRadius: 16,
                color: '#fca5a5',
                fontSize: 27,
                fontWeight: 700,
                display: 'flex',
                alignItems: 'center',
                gap: 14,
                boxSizing: 'border-box',
              }}
            >
              <span style={{ fontSize: 24 }}>✕</span>
              {text}
            </div>
          );
        })}
      </div>

      {/* 底部 Win11 任务栏模拟 */}
      <TaskbarMock clickFrame={clickFrame} showCursor={true} />

      {/* 解说字幕 */}
      <Subtitles items={subtitles} />
    </div>
  );
};
