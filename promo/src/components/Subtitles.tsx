import React from 'react';
import { interpolate, spring, useCurrentFrame, useVideoConfig } from 'remotion';
import { SubtitleItem } from '../types';

interface SubtitlesProps {
  items: SubtitleItem[];
}

// 抖音风动态高亮弹跳字幕组件
export const Subtitles: React.FC<SubtitlesProps> = ({ items }) => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();

  // 找到当前时间对应的字幕项
  const currentItem = items.find(
    (item) => frame >= item.startFrame && frame <= item.endFrame
  );

  if (!currentItem) {
    return null;
  }

  // 计算字幕切换时的弹簧缩放动画
  const timeIntoItem = frame - currentItem.startFrame;
  const enterSpring = spring({
    frame: timeIntoItem,
    fps,
    config: {
      damping: 14,
      stiffness: 220,
    },
  });

  const scale = interpolate(enterSpring, [0, 1], [0.85, 1]);
  const opacity = interpolate(timeIntoItem, [0, 4], [0.55, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  // 分词并高亮关键词
  const renderText = () => {
    const text = currentItem.text;
    const highlights = currentItem.highlightWords || [];

    if (highlights.length === 0) {
      return text;
    }

    // 简单分词高亮
    const regex = new RegExp(`(${highlights.join('|')})`, 'g');
    const parts = text.split(regex);

    return parts.map((part, idx) => {
      const isHighlight = highlights.includes(part);
      if (isHighlight) {
        return (
          <span
            key={idx}
            style={{
              color: '#38bdf8', // 醒目的荧光天蓝高亮
              textShadow: '0 0 16px rgba(56, 189, 248, 0.7)',
              fontWeight: 800,
            }}
          >
            {part}
          </span>
        );
      }
      return <span key={idx}>{part}</span>;
    });
  };

  return (
    <div
      style={{
        position: 'absolute',
        bottom: 220,
        left: 0,
        width: 1080,
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'center',
        zIndex: 50,
        pointerEvents: 'none',
      }}
    >
      <div
        style={{
          transform: `scale(${scale})`,
          opacity,
          background: 'rgba(15, 23, 42, 0.85)',
          backdropFilter: 'blur(16px)',
          border: '1.5px solid rgba(255, 255, 255, 0.15)',
          boxShadow: '0 12px 36px rgba(0, 0, 0, 0.5), 0 0 20px rgba(56, 189, 248, 0.15)',
          padding: '18px 42px',
          borderRadius: 40,
          maxWidth: 960,
          textAlign: 'center',
        }}
      >
        <span
          style={{
            fontSize: 40,
            fontWeight: 700,
            color: '#ffffff',
            letterSpacing: '0.04em',
            lineHeight: 1.4,
            fontFamily:
              'system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Microsoft YaHei", sans-serif',
          }}
        >
          {renderText()}
        </span>
      </div>
    </div>
  );
};
