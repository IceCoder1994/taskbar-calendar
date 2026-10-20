// 自动生成，请勿手动修改：由 tools/gen_calendar_data.py 生成
// 数据来源：国务院 2025/2026 年节假日安排 + 农历换算

export interface CellData {
  day: number;
  lunar: string;
  kind: 'lunar' | 'term' | 'fest';
  tag: '休' | '班' | null;
  today: boolean;
  outside: boolean;
}

export const OCT_2026_CELLS: CellData[] = [
  { day: 28, lunar: '十八', kind: 'lunar', tag: null, today: false, outside: true },
  { day: 29, lunar: '十九', kind: 'lunar', tag: null, today: false, outside: true },
  { day: 30, lunar: '二十', kind: 'lunar', tag: null, today: false, outside: true },
  { day: 1, lunar: '国庆节', kind: 'fest', tag: '休', today: false, outside: false },
  { day: 2, lunar: '廿二', kind: 'lunar', tag: '休', today: false, outside: false },
  { day: 3, lunar: '廿三', kind: 'lunar', tag: '休', today: false, outside: false },
  { day: 4, lunar: '廿四', kind: 'lunar', tag: '休', today: false, outside: false },
  { day: 5, lunar: '廿五', kind: 'lunar', tag: '休', today: false, outside: false },
  { day: 6, lunar: '廿六', kind: 'lunar', tag: '休', today: false, outside: false },
  { day: 7, lunar: '廿七', kind: 'lunar', tag: '休', today: false, outside: false },
  { day: 8, lunar: '寒露', kind: 'term', tag: null, today: false, outside: false },
  { day: 9, lunar: '廿九', kind: 'lunar', tag: null, today: false, outside: false },
  { day: 10, lunar: '九月', kind: 'lunar', tag: '班', today: false, outside: false },
  { day: 11, lunar: '初二', kind: 'lunar', tag: null, today: false, outside: false },
  { day: 12, lunar: '初三', kind: 'lunar', tag: null, today: false, outside: false },
  { day: 13, lunar: '初四', kind: 'lunar', tag: null, today: false, outside: false },
  { day: 14, lunar: '初五', kind: 'lunar', tag: null, today: false, outside: false },
  { day: 15, lunar: '初六', kind: 'lunar', tag: null, today: false, outside: false },
  { day: 16, lunar: '初七', kind: 'lunar', tag: null, today: false, outside: false },
  { day: 17, lunar: '初八', kind: 'lunar', tag: null, today: false, outside: false },
  { day: 18, lunar: '重阳节', kind: 'fest', tag: null, today: false, outside: false },
  { day: 19, lunar: '初十', kind: 'lunar', tag: null, today: false, outside: false },
  { day: 20, lunar: '十一', kind: 'lunar', tag: null, today: true, outside: false },
  { day: 21, lunar: '十二', kind: 'lunar', tag: null, today: false, outside: false },
  { day: 22, lunar: '十三', kind: 'lunar', tag: null, today: false, outside: false },
  { day: 23, lunar: '霜降', kind: 'term', tag: null, today: false, outside: false },
  { day: 24, lunar: '十五', kind: 'lunar', tag: null, today: false, outside: false },
  { day: 25, lunar: '十六', kind: 'lunar', tag: null, today: false, outside: false },
  { day: 26, lunar: '十七', kind: 'lunar', tag: null, today: false, outside: false },
  { day: 27, lunar: '十八', kind: 'lunar', tag: null, today: false, outside: false },
  { day: 28, lunar: '十九', kind: 'lunar', tag: null, today: false, outside: false },
  { day: 29, lunar: '二十', kind: 'lunar', tag: null, today: false, outside: false },
  { day: 30, lunar: '廿一', kind: 'lunar', tag: null, today: false, outside: false },
  { day: 31, lunar: '廿二', kind: 'lunar', tag: null, today: false, outside: false },
  { day: 1, lunar: '廿三', kind: 'lunar', tag: null, today: false, outside: true },
  { day: 2, lunar: '廿四', kind: 'lunar', tag: null, today: false, outside: true },
  { day: 3, lunar: '廿五', kind: 'lunar', tag: null, today: false, outside: true },
  { day: 4, lunar: '廿六', kind: 'lunar', tag: null, today: false, outside: true },
  { day: 5, lunar: '廿七', kind: 'lunar', tag: null, today: false, outside: true },
  { day: 6, lunar: '廿八', kind: 'lunar', tag: null, today: false, outside: true },
  { day: 7, lunar: '廿九', kind: 'lunar', tag: null, today: false, outside: true },
  { day: 8, lunar: '三十', kind: 'lunar', tag: null, today: false, outside: true },
];
