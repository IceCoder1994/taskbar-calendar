import React from 'react';
import { interpolate, useCurrentFrame } from 'remotion';
import { OCT_2026_CELLS, CellData } from '../data/calendarData';

export type Theme = 'dark' | 'light';

interface CalendarPanelProps {
  // 主题：深色 / 浅色
  theme: Theme;
  // 主题切换进度（0 = 深色，1 = 浅色），用于平滑过渡
  themeProgress?: number;
  // 是否高亮"休/班"角标
  highlightHoliday?: boolean;
  // 是否高亮节气
  highlightTerm?: boolean;
  // 高亮动画起始帧（相对）
  highlightStart?: number;
  // 是否显示底部详情栏
  showFooter?: boolean;
}

// 颜色定义（贴近程序真实渲染）
const COLORS = {
  dark: {
    bg: '#2c2c2c',
    text: '#e8e8e8',
    textDim: '#9a9a9a',
    textFaint: '#6f6f6f',
    weekend: '#e07a7a',
    divider: 'rgba(255,255,255,0.09)',
    cellHover: 'rgba(255,255,255,0.06)',
    accent: '#2563eb',
    accentText: '#ffffff',
    holidayBadge: '#c0504d',
    workdayBadge: '#3a6ea5',
  },
  light: {
    bg: '#f3f3f3',
    text: '#1b1b1b',
    textDim: '#8a8a8a',
    textFaint: '#b0b0b0',
    weekend: '#c94b4b',
    divider: 'rgba(0,0,0,0.08)',
    cellHover: 'rgba(0,0,0,0.04)',
    accent: '#2563eb',
    accentText: '#ffffff',
    holidayBadge: '#c0504d',
    workdayBadge: '#3a6ea5',
  },
} as const;

// 在两个主题之间做颜色插值
const mix = (a: string, b: string, t: number) => {
  const pa = parseInt(a.slice(1), 16);
  const pb = parseInt(b.slice(1), 16);
  const ar = (pa >> 16) & 255;
  const ag = (pa >> 8) & 255;
  const ab = pa & 255;
  const br = (pb >> 16) & 255;
  const bg = (pb >> 8) & 255;
  const bb = pb & 255;
  const r = Math.round(ar + (br - ar) * t);
  const g = Math.round(ag + (bg - ag) * t);
  const bl = Math.round(ab + (bb - ab) * t);
  return `rgb(${r}, ${g}, ${bl})`;
};

// 程序内日历面板的高保真复刻组件（360x460 逻辑像素，按需缩放）
export const CalendarPanel: React.FC<CalendarPanelProps> = ({
  theme,
  themeProgress = 0,
  highlightHoliday = false,
  highlightTerm = false,
  highlightStart = 0,
  showFooter = true,
}) => {
  const frame = useCurrentFrame();
  const t = themeProgress;
  const c = {
    bg: mix(COLORS.dark.bg, COLORS.light.bg, t),
    text: mix(COLORS.dark.text, COLORS.light.text, t),
    textDim: mix(COLORS.dark.textDim, COLORS.light.textDim, t),
    textFaint: mix(COLORS.dark.textFaint, COLORS.light.textFaint, t),
    weekend: mix(COLORS.dark.weekend, COLORS.light.weekend, t),
    divider: t > 0.5 ? COLORS.light.divider : COLORS.dark.divider,
  };

  // 高亮脉冲
  const pulse = 0.5 + 0.5 * Math.sin((frame - highlightStart) * 0.18);

  const renderCell = (cell: CellData, idx: number) => {
    const isWeekend = idx % 7 >= 5;
    const isTerm = cell.kind === 'term';
    const isFest = cell.kind === 'fest';
    const showTerm = highlightTerm && isTerm;
    const showBadge = highlightHoliday && !!cell.tag;

    // 单元格高亮边框
    let ringColor = 'transparent';
    if (showTerm) ringColor = `rgba(251, 191, 36, ${0.45 + pulse * 0.45})`;
    if (showBadge) {
      ringColor =
        cell.tag === '休'
          ? `rgba(224, 90, 90, ${0.45 + pulse * 0.45})`
          : `rgba(90, 160, 224, ${0.45 + pulse * 0.45})`;
    }

    const lunarColor = isTerm
      ? mix('#d4a017', '#b8860b', t)
      : isFest
      ? mix('#e07a7a', '#c94b4b', t)
      : c.textDim;

    return (
      <div
        key={idx}
        style={{
          position: 'relative',
          height: 52,
          borderRadius: 9,
          backgroundColor: cell.today ? COLORS.dark.accent : 'transparent',
          border: `1.5px solid ${ringColor}`,
          boxShadow: ringColor !== 'transparent' ? `0 0 14px ${ringColor}` : 'none',
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          justifyContent: 'center',
          opacity: cell.outside ? 0.38 : 1,
        }}
      >
        <span
          style={{
            fontSize: 20,
            lineHeight: 1.05,
            fontWeight: cell.today ? 600 : 400,
            color: cell.today ? COLORS.dark.accentText : isWeekend ? c.weekend : c.text,
          }}
        >
          {cell.day}
        </span>
        <span
          style={{
            fontSize: 12,
            lineHeight: 1.1,
            marginTop: 1,
            color: cell.today ? 'rgba(255,255,255,0.9)' : lunarColor,
            fontWeight: isTerm || isFest ? 600 : 400,
            whiteSpace: 'nowrap',
          }}
        >
          {cell.lunar}
        </span>

        {/* 右上角 休 / 班 角标 */}
        {cell.tag && (
          <div
            style={{
              position: 'absolute',
              top: 2,
              right: 3,
              width: 13,
              height: 13,
              borderRadius: 3,
              backgroundColor:
                cell.tag === '休' ? COLORS.dark.holidayBadge : COLORS.dark.workdayBadge,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              fontSize: 8.5,
              fontWeight: 700,
              color: '#fff',
              opacity: highlightHoliday ? 1 : 0.85,
              boxShadow: showBadge ? `0 0 8px ${ringColor}` : 'none',
            }}
          >
            {cell.tag}
          </div>
        )}
      </div>
    );
  };

  return (
    <div
      style={{
        width: 360,
        borderRadius: 12,
        backgroundColor: c.bg,
        overflow: 'hidden',
        display: 'flex',
        flexDirection: 'column',
        padding: '14px 12px 0',
        boxSizing: 'border-box',
        fontFamily:
          '"Segoe UI", system-ui, -apple-system, "Microsoft YaHei", sans-serif',
      }}
    >
      {/* 头部：年月 + 今天 */}
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          paddingBottom: 8,
        }}
      >
        <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
          <span style={{ fontSize: 13, color: c.textFaint }}>‹</span>
          <span style={{ fontSize: 17, fontWeight: 600, color: c.text, letterSpacing: 0.5 }}>
            2026年
          </span>
          <span style={{ fontSize: 17, fontWeight: 600, color: c.text }}>10月</span>
          <span style={{ fontSize: 13, color: c.textFaint }}>›</span>
        </div>
        <span style={{ fontSize: 14, color: c.textDim }}>今天</span>
      </div>

      {/* 星期表头 */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(7, 1fr)',
          paddingBottom: 6,
        }}
      >
        {['一', '二', '三', '四', '五', '六', '日'].map((w, i) => (
          <span
            key={i}
            style={{
              textAlign: 'center',
              fontSize: 13.5,
              color: i >= 5 ? c.weekend : c.textDim,
            }}
          >
            {w}
          </span>
        ))}
      </div>

      {/* 日期网格（6 行 × 7 列） */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(7, 1fr)',
          gap: 1,
        }}
      >
        {OCT_2026_CELLS.map((cell, idx) => renderCell(cell, idx))}
      </div>

      {/* 底部详情栏 */}
      {showFooter && (
        <div
          style={{
            marginTop: 8,
            borderTop: `1px solid ${c.divider}`,
            padding: '10px 4px 12px',
            display: 'flex',
            flexDirection: 'column',
            gap: 3,
          }}
        >
          <span style={{ fontSize: 15, fontWeight: 600, color: c.text }}>10月20日 周二</span>
          <span style={{ fontSize: 12.5, color: c.textDim }}>丙午马年 九月十一</span>
        </div>
      )}
    </div>
  );
};
