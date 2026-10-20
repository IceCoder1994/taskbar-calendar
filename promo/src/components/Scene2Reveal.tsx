import React from 'react';
import { interpolate, spring, useCurrentFrame, useVideoConfig } from 'remotion';
import { CalendarPanel } from './CalendarPanel';
import { Subtitles } from './Subtitles';
import { TaskbarMock } from './TaskbarMock';
import { SubtitleItem } from '../types';

const subtitles: SubtitleItem[] = [
  {
    startFrame: 0,
    endFrame: 120,
    text: '推荐你这款任务栏日历',
    highlightWords: ['任务栏日历'],
  },
  {
    startFrame: 120,
    endFrame: 232,
    text: '点击时钟就能秒开',
    highlightWords: ['秒开'],
  },
  {
    startFrame: 232,
    endFrame: 392,
    text: '通透的磨砂质感，完美融入系统',
    highlightWords: ['磨砂质感', '完美融入'],
  },
];

// 第 2 幕：惊艳亮相 + 深浅主题自适应
export const Scene2Reveal: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();

  // 标题入场
  const titleSpring = spring({
    frame,
    fps,
    config: { damping: 14, stiffness: 180 },
  });
  const titleY = interpolate(titleSpring, [0, 1], [-50, 0]);
  // 场景硬切衔接：首帧即显示标题，避免出现空白帧
  const titleOpacity = 1;

  // 面板从任务栏上方弹起（起始帧提前，保证场景切入时面板已可见，不出现空屏）
  const modalSpring = spring({
    frame: frame + 22,
    fps,
    config: { damping: 13, stiffness: 165 },
  });
  const modalY = interpolate(modalSpring, [0, 1], [260, 0]);
  const modalScale = interpolate(modalSpring, [0, 1], [0.88, 1]);
  const modalOpacity = 1;

  // 第 210 帧开始切到浅色主题
  const switchStart = 210;
  const themeProgress = interpolate(frame, [switchStart, switchStart + 30], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  // 环绕特性标签（统一放在面板下方，避免遮挡面板内容）
  const badges = [
    { text: '✨ Win11 云母磨砂质感', delay: 40, top: 1382 },
    { text: '⚡ 点击时钟 · 毫秒级秒开', delay: 80, top: 1452 },
  ];

  return (
    <div style={{ position: 'absolute', inset: 0, overflow: 'hidden' }}>
      {/* 顶部标题 */}
      <div
        style={{
          position: 'absolute',
          top: 100,
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
            padding: '8px 26px',
            borderRadius: 20,
            background: 'rgba(56, 189, 248, 0.14)',
            border: '1px solid rgba(56, 189, 248, 0.4)',
            color: '#38bdf8',
            fontSize: 22,
            fontWeight: 700,
            letterSpacing: 2,
            marginBottom: 16,
          }}
        >
          全新质感 · 原生适配
        </div>

        <h1
          style={{
            margin: 0,
            fontSize: 64,
            fontWeight: 900,
            color: '#f8fafc',
            textAlign: 'center',
            lineHeight: 1.28,
            letterSpacing: 1,
          }}
        >
          任务栏日历
          <br />
          <span
            style={{
              background: 'linear-gradient(135deg, #38bdf8 0%, #818cf8 100%)',
              WebkitBackgroundClip: 'text',
              WebkitTextFillColor: 'transparent',
            }}
          >
            点击即现 · 丝滑无缝
          </span>
        </h1>
      </div>

      {/* 日历面板：从任务栏上方升起 */}
      <div
        style={{
          position: 'absolute',
          top: 470,
          left: 0,
          width: 1080,
          display: 'flex',
          justifyContent: 'center',
          transform: `translateY(${modalY}px) scale(${modalScale})`,
          opacity: modalOpacity,
          zIndex: 15,
        }}
      >
        <div
          style={{
            position: 'relative',
            borderRadius: 20,
            overflow: 'hidden',
            boxShadow:
              themeProgress > 0.5
                ? '0 30px 80px rgba(0, 0, 0, 0.35), 0 0 40px rgba(255, 255, 255, 0.25)'
                : '0 30px 80px rgba(0, 0, 0, 0.85), 0 0 50px rgba(56, 189, 248, 0.28)',
            border:
              themeProgress > 0.5
                ? '1.5px solid rgba(0, 0, 0, 0.12)'
                : '1.5px solid rgba(255, 255, 255, 0.16)',
            // 使用 CSS zoom 让布局尺寸同步放大，保证居中与避让计算准确
            zoom: 1.75,
          }}
        >
          {/* 使用真实数据复刻的面板：深色与浅色交叉淡入，避免中间态低对比 */}
          <div style={{ position: 'relative' }}>
            <div style={{ opacity: 1 - themeProgress }}>
              <CalendarPanel theme="dark" themeProgress={0} />
            </div>
            <div style={{ position: 'absolute', inset: 0, opacity: themeProgress }}>
              <CalendarPanel theme="light" themeProgress={1} />
            </div>
          </div>

          {/* 面板表面流光扫过 */}
          <div
            style={{
              position: 'absolute',
              top: 0,
              left: interpolate(frame, [20, 90], [-140, 400], {
                extrapolateLeft: 'clamp',
                extrapolateRight: 'clamp',
              }),
              width: 90,
              height: '100%',
              background:
                'linear-gradient(90deg, transparent, rgba(255,255,255,0.28), transparent)',
              transform: 'skewX(-22deg)',
              pointerEvents: 'none',
            }}
          />
        </div>
      </div>

      {/* 主题切换提示 */}
      {frame >= switchStart - 12 && (
        <div
          style={{
            position: 'absolute',
            top: 1296,
            left: 0,
            width: 1080,
            display: 'flex',
            justifyContent: 'center',
            zIndex: 25,
          }}
        >
          <div
            style={{
              padding: '8px 22px',
              borderRadius: 16,
              backgroundColor:
                themeProgress > 0.5 ? 'rgba(250, 250, 250, 0.95)' : 'rgba(24, 24, 27, 0.95)',
              color: themeProgress > 0.5 ? '#111' : '#fafafa',
              border: '1px solid rgba(125, 211, 252, 0.5)',
              fontSize: 20,
              fontWeight: 700,
              boxShadow: '0 8px 24px rgba(0, 0, 0, 0.45)',
            }}
          >
            {themeProgress > 0.5 ? '☀️ 浅色主题' : '🌙 深色主题'}
          </div>
        </div>
      )}

      {/* 环绕特性标签 */}
      {badges.map((b, idx) => {
        const bSpring = spring({
          frame: frame - b.delay,
          fps,
          config: { damping: 13, stiffness: 220 },
        });
        const bScale = frame >= b.delay ? interpolate(bSpring, [0, 1], [0.4, 1]) : 0.4;
        const bOpacity = frame >= b.delay ? interpolate(bSpring, [0, 1], [0, 1]) : 0;

        return (
          <div
            key={idx}
            style={{
              position: 'absolute',
              top: b.top,
              left: 0,
              width: 1080,
              display: 'flex',
              justifyContent: 'center',
              transform: `scale(${bScale})`,
              opacity: bOpacity,
              zIndex: 30,
              pointerEvents: 'none',
            }}
          >
            <div
              style={{
                background: 'rgba(15, 23, 42, 0.92)',
                backdropFilter: 'blur(20px)',
                border: '1.5px solid rgba(56, 189, 248, 0.4)',
                boxShadow: '0 12px 30px rgba(0, 0, 0, 0.6), 0 0 15px rgba(56, 189, 248, 0.18)',
                padding: '13px 26px',
                borderRadius: 22,
                color: '#ffffff',
                fontSize: 23,
                fontWeight: 700,
                whiteSpace: 'nowrap',
              }}
            >
              {b.text}
            </div>
          </div>
        );
      })}

      {/* 底部任务栏 */}
      <TaskbarMock highlightClock={true} showCursor={false} />

      {/* 解说字幕 */}
      <Subtitles items={subtitles} />
    </div>
  );
};
