import math
import struct
import wave
import os

SAMPLE_RATE = 44100

def write_wav(filename, samples):
    os.makedirs(os.path.dirname(filename), exist_ok=True)
    with wave.open(filename, 'wb') as wav_file:
        wav_file.setnchannels(2)  # 立体声
        wav_file.setsampwidth(2)  # 16-bit
        wav_file.setframerate(SAMPLE_RATE)
        
        raw_data = bytearray()
        for left, right in samples:
            # 限制在 -1.0 到 1.0 之间
            left = max(-1.0, min(1.0, left))
            right = max(-1.0, min(1.0, right))
            l_int = int(left * 32767)
            r_int = int(right * 32767)
            raw_data.extend(struct.pack('<hh', l_int, r_int))
            
        wav_file.writeframes(raw_data)
    print(f"Generated: {filename} ({len(samples)/SAMPLE_RATE:.2f}s)")

# 1. 鼠标点击音效 (Click)
def gen_click():
    duration = 0.08
    n_samples = int(SAMPLE_RATE * duration)
    samples = []
    for i in range(n_samples):
        t = i / SAMPLE_RATE
        env = math.exp(-t * 80)
        # 混合高频瞬态与轻微木质回响
        val = (math.sin(2 * math.pi * 1800 * t) * 0.6 + 
               math.sin(2 * math.pi * 900 * t) * 0.4) * env
        samples.append((val * 0.8, val * 0.8))
    write_wav("promo/public/audio/click.wav", samples)

# 2. 界面滑出呼呼声 (Whoosh)
def gen_whoosh():
    duration = 0.35
    n_samples = int(SAMPLE_RATE * duration)
    samples = []
    for i in range(n_samples):
        t = i / SAMPLE_RATE
        # 钟形包络
        env = math.sin(math.pi * (t / duration)) ** 2
        # 频率由低到高再回落
        freq = 200 + 400 * math.sin(math.pi * (t / duration))
        # 调频与白噪声质感
        val = math.sin(2 * math.pi * freq * t + math.sin(15 * t)) * env * 0.5
        # 左右立体声轻微偏移
        pan = t / duration
        samples.append((val * (1 - pan) * 0.7, val * pan * 0.7))
    write_wav("promo/public/audio/whoosh.wav", samples)

# 3. 气泡弹跳音 (Pop)
def gen_pop():
    duration = 0.12
    n_samples = int(SAMPLE_RATE * duration)
    samples = []
    for i in range(n_samples):
        t = i / SAMPLE_RATE
        env = math.exp(-t * 35)
        # 频率快速向上扫过产生弹力感
        freq = 300 + 600 * (1 - math.exp(-t * 50))
        val = math.sin(2 * math.pi * freq * t) * env * 0.6
        samples.append((val, val))
    write_wav("promo/public/audio/pop.wav", samples)

# 4. 成功/点亮清脆风铃 (Chime)
def gen_chime():
    duration = 0.6
    n_samples = int(SAMPLE_RATE * duration)
    samples = []
    notes = [1046.50, 1318.51, 1567.98, 2093.00] # C6, E6, G6, C7
    for i in range(n_samples):
        t = i / SAMPLE_RATE
        val = 0
        for idx, freq in enumerate(notes):
            note_start = idx * 0.06
            if t >= note_start:
                dt = t - note_start
                env = math.exp(-dt * 8)
                val += math.sin(2 * math.pi * freq * dt) * env * 0.25
        samples.append((val * 0.8, val * 0.8))
    write_wav("promo/public/audio/chime.wav", samples)

# 5. 节奏科技感背景音乐 (BGM: 32秒循环, 120 BPM)
def gen_bgm():
    duration = 34.0
    n_samples = int(SAMPLE_RATE * duration)
    samples = []
    bpm = 120
    beat_sec = 60.0 / bpm  # 0.5 秒一拍
    
    # 几个轻快科技感和弦 (Am7 - Fmaj7 - C - G)
    chords = [
        [220.00, 261.63, 329.63, 392.00], # A3, C4, E4, G4
        [174.61, 261.63, 329.63, 392.00], # F3, C4, E4, G4
        [261.63, 329.63, 392.00, 523.25], # C4, E4, G4, C5
        [196.00, 246.94, 293.66, 392.00]  # G3, B3, D4, G4
    ]
    
    for i in range(n_samples):
        t = i / SAMPLE_RATE
        total_beats = t / beat_sec
        bar = int(total_beats // 4)
        chord = chords[bar % len(chords)]
        
        # 1. 柔和底鼓 Kick (每小节第 1, 3 拍)
        beat_in_bar = total_beats % 4
        kick = 0
        for k_beat in [0, 2]:
            dt = total_beats - (bar * 4 + k_beat)
            if 0 <= dt < 0.25:
                kt = dt * beat_sec
                k_env = math.exp(-kt * 22)
                k_freq = 120 * math.exp(-kt * 30) + 40
                kick += math.sin(2 * math.pi * k_freq * kt) * k_env * 0.35
                
        # 2. 闭镲 Hihat (每 0.5 拍)
        hihat = 0
        dt_hat = total_beats % 0.5
        if dt_hat < 0.1:
            ht = dt_hat * beat_sec
            h_env = math.exp(-ht * 50)
            # 高频模拟
            hihat = (math.sin(2 * math.pi * 7500 * ht) * 0.5 + 
                     math.sin(2 * math.pi * 9200 * ht) * 0.5) * h_env * 0.12
                     
        # 3. 温暖的电子合成铺底 Pad (Arp / Chord)
        pad = 0
        for f in chord:
            # 轻微颤音 LFO
            lfo = 1.0 + 0.003 * math.sin(2 * math.pi * 4 * t)
            pad += math.sin(2 * math.pi * f * lfo * t) * 0.04
            
        # 4. 灵动的科技感琶音 (Arpeggio)
        arp_idx = int(total_beats * 4) % len(chord)
        arp_freq = chord[arp_idx] * 2  # 高八度
        arp_dt = (total_beats * 4) % 1.0
        arp_t = arp_dt * (beat_sec / 4)
        arp_env = math.exp(-arp_t * 12)
        arp = math.sin(2 * math.pi * arp_freq * arp_t) * arp_env * 0.08
        
        # 总体淡入淡出 (前 1s 淡入，最后 2s 淡出)
        fade = 1.0
        if t < 1.0:
            fade = t
        elif t > duration - 2.0:
            fade = (duration - t) / 2.0
            
        mix = (kick + hihat + pad + arp) * fade * 0.8
        samples.append((mix, mix))
        
    write_wav("promo/public/audio/bgm.wav", samples)

if __name__ == '__main__':
    gen_click()
    gen_whoosh()
    gen_pop()
    gen_chime()
    gen_bgm()
