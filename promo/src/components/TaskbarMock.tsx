import React from 'react';
import { interpolate, spring, useCurrentFrame, useVideoConfig } from 'remotion';

interface TaskbarMockProps {
  // 鼠标点击发生的相对帧
  clickFrame?: number;
  // 是否高亮时钟区域
  highlightClock?: boolean;
  // 鼠标移动与点击模拟
  showCursor?: boolean;
}

// 模拟精致的 Windows 11 居中任务栏与右下角托盘时钟
export const TaskbarMock: React.FC<TaskbarMockProps> = ({
  clickFrame = 50,
  highlightClock = false,
  showCursor = true,
}) => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();

  // 鼠标光标从屏幕中心偏左移动到右下角任务栏时钟的轨迹动画
  const cursorProgress = interpolate(frame, [15, clickFrame], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  // 时钟中心物理坐标
  const clockTargetX = 980;
  const clockTargetY = 1865;

  const cursorX = interpolate(cursorProgress, [0, 1], [540, clockTargetX]);
  const cursorY = interpolate(cursorProgress, [0, 1], [1350, clockTargetY]);

  // 点击时的缩放弹力
  const isClicked = frame >= clickFrame;
  const clickSpring = spring({
    frame: frame - clickFrame,
    fps,
    config: {
      damping: 12,
      stiffness: 250,
    },
  });

  const clockScale = isClicked ? interpolate(clickSpring, [0, 0.4, 1], [1, 0.92, 1]) : 1;

  // 点击波纹涟漪动效
  const rippleProgress = frame >= clickFrame ? (frame - clickFrame) / 25 : 0;
  const rippleScale = interpolate(rippleProgress, [0, 1], [0.3, 2.2]);
  const rippleOpacity = interpolate(rippleProgress, [0, 0.8, 1], [0.8, 0.2, 0]);

  return (
    <div
      style={{
        position: 'absolute',
        bottom: 0,
        left: 0,
        width: 1080,
        height: 110,
        backgroundColor: 'rgba(15, 23, 42, 0.82)',
        backdropFilter: 'blur(30px)',
        borderTop: '1px solid rgba(255, 255, 255, 0.12)',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        padding: '0 32px',
        boxSizing: 'border-box',
        zIndex: 40,
      }}
    >
      {/* 左侧天气小组件占位 */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 12, opacity: 0.85 }}>
        <div
          style={{
            width: 28,
            height: 28,
            borderRadius: '50%',
            background: 'linear-gradient(135deg, #f59e0b, #ef4444)',
            boxShadow: '0 0 10px rgba(245, 158, 11, 0.5)',
          }}
        />
        <div style={{ display: 'flex', flexDirection: 'column' }}>
          <span style={{ fontSize: 18, color: '#fff', fontWeight: 600 }}>24°C</span>
          <span style={{ fontSize: 13, color: '#94a3b8' }}>晴朗</span>
        </div>
      </div>

      {/* 居中经典 Win11 图标列 */}
      <div
        style={{
          position: 'absolute',
          left: '50%',
          transform: 'translateX(-50%)',
          display: 'flex',
          alignItems: 'center',
          gap: 18,
        }}
      >
        {/* Win11 徽标图标 */}
        <div
          style={{
            width: 44,
            height: 44,
            display: 'grid',
            gridTemplateColumns: '1fr 1fr',
            gap: 4,
            padding: 8,
            boxSizing: 'border-box',
            backgroundColor: 'rgba(255, 255, 255, 0.06)',
            borderRadius: 8,
          }}
        >
          <div style={{ background: '#00adef', borderRadius: 2 }} />
          <div style={{ background: '#00adef', borderRadius: 2 }} />
          <div style={{ background: '#00adef', borderRadius: 2 }} />
          <div style={{ background: '#00adef', borderRadius: 2 }} />
        </div>

        {/* 其它应用图标模拟 */}
        {['#38bdf8', '#818cf8', '#34d399', '#f43f5e', '#fbbf24'].map((color, i) => (
          <div
            key={i}
            style={{
              width: 44,
              height: 44,
              borderRadius: 8,
              backgroundColor: 'rgba(255, 255, 255, 0.05)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
            }}
          >
            <div
              style={{
                width: 26,
                height: 26,
                borderRadius: 6,
                backgroundColor: color,
                opacity: 0.9,
              }}
            />
          </div>
        ))}
      </div>

      {/* 右侧系统托盘与时钟按钮 */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 16 }}>
        {/* 简易托盘图标 */}
        <div style={{ display: 'flex', gap: 10, opacity: 0.7, alignItems: 'center' }}>
          <span style={{ fontSize: 18, color: '#94a3b8' }}>▲</span>
          <span style={{ fontSize: 18, color: '#94a3b8' }}>📶</span>
          <span style={{ fontSize: 18, color: '#94a3b8' }}>🔊</span>
        </div>

        {/* 关键主角：时钟按钮 */}
        <div
          style={{
            position: 'relative',
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'flex-end',
            justifyContent: 'center',
            padding: '6px 14px',
            borderRadius: 8,
            backgroundColor:
              isClicked || highlightClock
                ? 'rgba(56, 189, 248, 0.2)'
                : 'rgba(255, 255, 255, 0.08)',
            border:
              isClicked || highlightClock
                ? '1px solid rgba(56, 189, 248, 0.6)'
                : '1px solid transparent',
            transform: `scale(${clockScale})`,
            transition: 'background 0.2s',
            boxShadow:
              isClicked || highlightClock
                ? '0 0 20px rgba(56, 189, 248, 0.4)'
                : 'none',
          }}
        >
          {/* 点击涟漪光圈 */}
          {frame >= clickFrame && frame <= clickFrame + 25 && (
            <div
              style={{
                position: 'absolute',
                inset: -10,
                borderRadius: 12,
                border: '2px solid #38bdf8',
                transform: `scale(${rippleScale})`,
                opacity: rippleOpacity,
                pointerEvents: 'none',
              }}
            />
          )}

          <span
            style={{
              fontSize: 19,
              fontWeight: 600,
              color: '#ffffff',
              lineHeight: 1.15,
            }}
          >
            14:28
          </span>
          <span
            style={{
              fontSize: 13,
              color: '#cbd5e1',
              lineHeight: 1.15,
            }}
          >
            2026/10/20
          </span>
        </div>
      </div>

      {/* 逼真的现代 Windows 鼠标指针 */}
      {showCursor && (
        <div
          style={{
            position: 'fixed',
            left: cursorX,
            top: cursorY,
            pointerEvents: 'none',
            zIndex: 999,
            transform: isClicked && frame < clickFrame + 8 ? 'scale(0.88)' : 'scale(1)',
            filter: 'drop-shadow(0 4px 10px rgba(0, 0, 0, 0.6))',
          }}
        >
          <svg width="34" height="34" viewBox="0 0 24 24" fill="none">
            <path
              d="M3 3L10.5 21L13.5 13.5L21 10.5L3 3Z"
              fill="#ffffff"
              stroke="#0f172a"
              strokeWidth="2"
              strokeLinejoin="round"
            />
          </svg>
        </div>
      )}
    </div>
  );
};
