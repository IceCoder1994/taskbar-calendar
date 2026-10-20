import React from 'react';
import { Composition, registerRoot } from 'remotion';
import { Main, TOTAL_FRAMES } from './Main';

// 竖屏抖音版宣传视频：1080x1920 @ 60fps
export const Root: React.FC = () => {
  return (
    <>
      <Composition
        id="Main"
        component={Main}
        durationInFrames={TOTAL_FRAMES}
        fps={60}
        width={1080}
        height={1920}
      />
    </>
  );
};

registerRoot(Root);
