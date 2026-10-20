// 宣传视频通用类型定义

export interface SubtitleItem {
  // 起始帧（相对于场景自身）
  startFrame: number;
  // 结束帧（相对于场景自身）
  endFrame: number;
  // 字幕文本
  text: string;
  // 强调关键词
  highlightWords?: string[];
}

export interface SceneProps {
  // 场景当前帧
  frame: number;
}
