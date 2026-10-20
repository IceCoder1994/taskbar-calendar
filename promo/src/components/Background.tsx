import React from 'react';
import { useCurrentFrame } from 'remotion';

// 炫酷动态背景：包含深色渐变、微光流光球与细致网格
export const Background: React.FC = () => {
  const frame = useCurrentFrame();

  // 计算流光球的平滑浮动位置
  const orb1X = Math.sin(frame * 0.02) * 120 + 200;
  const orb1Y = Math.cos(frame * 0.015) * 150 + 400;

  const orb2X = Math.cos(frame * 0.018) * 140 + 700;
  const orb2Y = Math.sin(frame * 0.022) * 180 + 1100;

  const orb3X = Math.sin(frame * 0.025) * 100 + 450;
  const orb3Y = Math.cos(frame * 0.02) * 120 + 1500;

  return (
    <div
      style={{
        position: 'absolute',
        top: 0,
        left: 0,
        width: 1080,
        height: 1920,
        backgroundColor: '#070b14',
        overflow: 'hidden',
        zIndex: 0,
      }}
    >
      {/* 科技感深邃径向渐变背景 */}
      <div
        style={{
          position: 'absolute',
          inset: 0,
          background: 'radial-gradient(circle at 50% 20%, #172554 0%, #090d16 60%, #030712 100%)',
        }}
      />

      {/* 动态流光光晕 1：天蓝色 */}
      <div
        style={{
          position: 'absolute',
          left: orb1X,
          top: orb1Y,
          width: 500,
          height: 500,
          borderRadius: '50%',
          background: 'radial-gradient(circle, rgba(56, 189, 248, 0.25) 0%, rgba(56, 189, 248, 0) 70%)',
          filter: 'blur(80px)',
          pointerEvents: 'none',
        }}
      />

      {/* 动态流光光晕 2：紫色 */}
      <div
        style={{
          position: 'absolute',
          left: orb2X,
          top: orb2Y,
          width: 600,
          height: 600,
          borderRadius: '50%',
          background: 'radial-gradient(circle, rgba(168, 85, 247, 0.22) 0%, rgba(168, 85, 247, 0) 70%)',
          filter: 'blur(90px)',
          pointerEvents: 'none',
        }}
      />

      {/* 动态流光光晕 3：青绿色 */}
      <div
        style={{
          position: 'absolute',
          left: orb3X,
          top: orb3Y,
          width: 550,
          height: 550,
          borderRadius: '50%',
          background: 'radial-gradient(circle, rgba(20, 184, 166, 0.18) 0%, rgba(20, 184, 166, 0) 70%)',
          filter: 'blur(85px)',
          pointerEvents: 'none',
        }}
      />

      {/* 细腻的 Win11 几何微网格底纹 */}
      <div
        style={{
          position: 'absolute',
          inset: 0,
          backgroundImage: `
            linear-gradient(rgba(255, 255, 255, 0.03) 1px, transparent 1px),
            linear-gradient(90deg, rgba(255, 255, 255, 0.03) 1px, transparent 1px)
          `,
          backgroundSize: '40px 40px',
          opacity: 0.8,
          pointerEvents: 'none',
        }}
      />

      {/* 顶部微弱暗角，烘托沉浸质感 */}
      <div
        style={{
          position: 'absolute',
          inset: 0,
          background: 'radial-gradient(ellipse at center, transparent 40%, rgba(0, 0, 0, 0.6) 100%)',
          pointerEvents: 'none',
        }}
      />
    </div>
  );
};
