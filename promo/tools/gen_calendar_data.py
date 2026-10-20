import json
import io
import datetime
from lunardate import LunarDate

# 生成视频中展示的 2026 年 10 月日历格子数据（与程序真实渲染保持一致）
# 数据来源：src/TaskbarCalendar/Data/holidays.json（国务院公告）+ 农历换算

names = ['正', '二', '三', '四', '五', '六', '七', '八', '九', '十', '冬', '腊']
daynames = ['初一', '初二', '初三', '初四', '初五', '初六', '初七', '初八', '初九', '初十',
            '十一', '十二', '十三', '十四', '十五', '十六', '十七', '十八', '十九', '二十',
            '廿一', '廿二', '廿三', '廿四', '廿五', '廿六', '廿七', '廿八', '廿九', '三十']

# 2026 年 10 月的节气与传统节日（与程序 LunarService 的寿星公式结果一致）
TERMS = {'2026-10-08': '寒露', '2026-10-23': '霜降'}
FESTS = {'2026-10-01': '国庆节', '2026-10-18': '重阳节'}

days = json.load(io.open('src/TaskbarCalendar/Data/holidays.json', encoding='utf-8'))['days']

start = datetime.date(2026, 9, 28)  # 周一，与程序"周一起始"设置一致
TODAY = '2026-10-20'

cells = []
for i in range(42):
    d = start + datetime.timedelta(days=i)
    key = d.strftime('%Y-%m-%d')
    l = LunarDate.fromSolarDate(d.year, d.month, d.day)
    if key in TERMS:
        label, kind = TERMS[key], 'term'
    elif key in FESTS:
        label, kind = FESTS[key], 'fest'
    elif l.day == 1:
        label, kind = names[l.month - 1] + '月', 'lunar'
    else:
        label, kind = daynames[l.day - 1], 'lunar'

    h = days.get(key)
    tag = '休' if h and h['type'] == 'holiday' else ('班' if h and h['type'] == 'workday' else None)

    cells.append({
        'day': d.day,
        'lunar': label,
        'kind': kind,
        'tag': tag,
        'today': key == TODAY,
        'outside': d.month != 10,
    })

lines = []
lines.append('// 自动生成，请勿手动修改：由 tools/gen_calendar_data.py 生成')
lines.append('// 数据来源：国务院 2025/2026 年节假日安排 + 农历换算')
lines.append('')
lines.append('export interface CellData {')
lines.append('  day: number;')
lines.append('  lunar: string;')
lines.append("  kind: 'lunar' | 'term' | 'fest';")
lines.append("  tag: '休' | '班' | null;")
lines.append('  today: boolean;')
lines.append('  outside: boolean;')
lines.append('}')
lines.append('')
lines.append('export const OCT_2026_CELLS: CellData[] = [')
for c in cells:
    tag = "'休'" if c['tag'] == '休' else ("'班'" if c['tag'] == '班' else 'null')
    lines.append(
        f"  {{ day: {c['day']}, lunar: '{c['lunar']}', kind: '{c['kind']}', tag: {tag}, "
        f"today: {str(c['today']).lower()}, outside: {str(c['outside']).lower()} }},"
    )
lines.append('];')
lines.append('')

with io.open('promo/src/data/calendarData.ts', 'w', encoding='utf-8', newline='\n') as f:
    f.write('\n'.join(lines))

print('生成完成：promo/src/data/calendarData.ts')
for c in cells:
    print(c['day'], c['lunar'], c['kind'], c['tag'], 'today' if c['today'] else '')
