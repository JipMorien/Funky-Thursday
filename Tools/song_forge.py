#!/usr/bin/env python3
"""
Song Forge - Funky Thursday placeholder songs and charts from a single note list.

Every level is generated from one seeded list of notes. The same list is written out as the
chart JSON and rendered as the vocal track, so every arrow lands on an audible sung note and
each lane always sings the same chord tone (Left = root, Down = third, Up = fifth, Right = octave).

Outputs (relative to the Unity project root):
    Assets/_FunkyThursday/Charts/level<N>-<slug>.json
    Assets/_FunkyThursday/Audio/Music/<slug>/Inst.ogg
    Assets/_FunkyThursday/Audio/Music/<slug>/Voices.ogg

Usage:
    python Tools/song_forge.py              # all four levels
    python Tools/song_forge.py --level 3    # one level
    python Tools/song_forge.py --charts-only

Requires: numpy, scipy, soundfile  (pip install numpy scipy soundfile)
"""
import argparse
import json
import math
import pathlib
import random

import numpy as np
from scipy.signal import lfilter

SR = 44100
ROOT = pathlib.Path(__file__).resolve().parent.parent
CHART_DIR = ROOT / "Assets" / "_FunkyThursday" / "Charts"
MUSIC_DIR = ROOT / "Assets" / "_FunkyThursday" / "Audio" / "Music"

INTRO_BEATS = 8     # two instrumental bars before the first note
OUTRO_BEATS = 8     # two bars after the last phrase
PHRASE_BEATS = 8    # each call and each response is two bars

LEVELS = [
    dict(level=1, slug="graveyard-gatekeeper", song="Graveyard Gatekeeper", stage="Graveyard",
         opponent="The Gatekeeper", difficulty="Easy", bpm=100, speed=1.0, pairs=10, seed=101,
         key=("D", 3), chords=[("D", "m"), ("Bb", "M"), ("F", "M"), ("C", "M")],
         opponent_voice="triangle", opponent_octave=3, player_octave=4, mirror=0.0,
         ai=dict(healthDrainPerNote=0.0, drainFloor=0.0)),
    dict(level=2, slug="crypt-keeper", song="Crypt Keeper", stage="Crypt",
         opponent="The Crypt Keeper", difficulty="Medium", bpm=125, speed=1.1, pairs=12, seed=202,
         key=("E", 3), chords=[("E", "m"), ("A", "m"), ("C", "M"), ("B", "M")],
         opponent_voice="square", opponent_octave=3, player_octave=4, mirror=0.0,
         ai=dict(healthDrainPerNote=0.0, drainFloor=0.0)),
    dict(level=3, slug="cathedral-organist", song="Cathedral Organist", stage="Cathedral",
         opponent="The Organist", difficulty="Hard", bpm=145, speed=1.25, pairs=14, seed=303,
         key=("C", 4), chords=[("C", "m"), ("Ab", "M"), ("F", "m"), ("G", "M")],
         opponent_voice="organ", opponent_octave=4, player_octave=4, mirror=0.2,
         ai=dict(healthDrainPerNote=0.004, drainFloor=0.25)),
    dict(level=4, slug="gothic-monarch", song="Gothic Monarch", stage="VampireCastle",
         opponent="The Gothic Monarch", difficulty="Expert", bpm=165, speed=1.4, pairs=16, seed=404,
         key=("G", 3), chords=[("G", "m"), ("Eb", "M"), ("F", "M"), ("D", "M")],
         opponent_voice="saw", opponent_octave=3, player_octave=4, mirror=0.35,
         ai=dict(healthDrainPerNote=0.008, drainFloor=0.2)),
]

# One-beat rhythm cells: (offset in beats, hold length in beats).
CELLS = {
    "rest": [],
    "q": [(0.0, 0.0)],
    "off": [(0.5, 0.0)],
    "e": [(0.0, 0.0), (0.5, 0.0)],
    "dot": [(0.0, 0.0), (0.75, 0.0)],
    "es": [(0.0, 0.0), (0.5, 0.0), (0.75, 0.0)],
    "se": [(0.0, 0.0), (0.25, 0.0), (0.5, 0.0)],
    "hold1": [(0.0, 0.5)],
}
WEIGHTS = {
    "Easy":   {"q": 6, "rest": 2, "hold2": 2, "e": 1.5, "off": 0.5, "hold1": 0.5},
    "Medium": {"q": 4, "e": 4, "rest": 1, "hold2": 1.5, "hold1": 1, "dot": 1, "off": 1},
    "Hard":   {"q": 2, "e": 5, "es": 2, "se": 2, "dot": 1.5, "hold2": 1, "hold1": 1, "jump": 1, "off": 1, "s": 0.6},
    "Expert": {"e": 4, "s": 3, "es": 3, "se": 3, "q": 1, "jump": 1.5, "hold1": 1, "hold2": 0.7, "dot": 1},
}
REPEAT_CHANCE = {"Easy": 0.25, "Medium": 0.2, "Hard": 0.15, "Expert": 0.12}
DENSE_CELLS = {"e", "es", "se", "s", "jump", "dot"}
PHRASE_ORDER = [0, 1, 0, 2, 3, 1, 2, 3]

NOTE_INDEX = {"C": 0, "C#": 1, "D": 2, "Eb": 3, "E": 4, "F": 5, "F#": 6, "G": 7, "Ab": 8, "A": 9, "Bb": 10, "B": 11}


# ── Chart generation ──────────────────────────────────────────────────────────

def reflect_lane(lane):
    if lane < 0:
        lane = -lane
    if lane > 3:
        lane = 6 - lane
    return max(0, min(3, lane))


class LaneWalker:
    def __init__(self, rng, repeat_chance):
        self.rng = rng
        self.repeat_chance = repeat_chance
        self.lane = rng.randrange(4)

    def next(self, avoid=None):
        r = self.rng.random()
        if r < self.repeat_chance:
            lane = self.lane
        elif r < self.repeat_chance + 0.5:
            lane = reflect_lane(self.lane + self.rng.choice((-1, 1)))
        else:
            lane = reflect_lane(self.lane + self.rng.choice((-3, -2, 2, 3)))
        if lane == avoid:
            lane = (lane + self.rng.choice((1, 2, 3))) % 4
        self.lane = lane
        return lane


def pick_cell(rng, weights):
    names = list(weights)
    return rng.choices(names, weights=[weights[n] for n in names])[0]


def build_phrase(rng, difficulty, dense):
    """Returns [(beat, [lanes], hold)] covering beats 0..6; beat 7 is left as a breath."""
    weights = dict(WEIGHTS[difficulty])
    if dense:
        for name in DENSE_CELLS & weights.keys():
            weights[name] *= 1.6

    walker = LaneWalker(rng, REPEAT_CHANCE[difficulty])
    notes = []
    beat = 0
    held_lane = None

    while beat < PHRASE_BEATS - 1:
        cell = pick_cell(rng, weights)
        if cell == "hold2" and PHRASE_BEATS - 1 - beat < 2:
            continue
        if beat == 0 and cell in ("rest", "off"):
            cell = "q"

        if cell == "hold2":
            lane = walker.next(avoid=held_lane)
            notes.append((float(beat), [lane], 1.5))
            held_lane = lane
            beat += 2
            continue

        if cell == "jump":
            pair = rng.choice([(0, 3), (1, 2), (0, 2), (1, 3)])
            notes.append((float(beat), list(pair), 0.0))
            walker.lane = pair[1]
            held_lane = None
        elif cell == "s":
            start = walker.lane
            shape = rng.choice(("roll_up", "roll_down", "trill"))
            if shape == "roll_up":
                lanes = [0, 1, 2, 3]
            elif shape == "roll_down":
                lanes = [3, 2, 1, 0]
            else:
                other = reflect_lane(start + rng.choice((-1, 1)))
                lanes = [start, other, start, other]
            if lanes[0] == held_lane:
                lanes = [3 - l for l in lanes]
            for i, lane in enumerate(lanes):
                notes.append((beat + i * 0.25, [lane], 0.0))
            walker.lane = lanes[-1]
            held_lane = None
        else:
            for offset, hold in CELLS[cell]:
                lane = walker.next(avoid=held_lane)
                notes.append((beat + offset, [lane], hold))
                held_lane = lane if hold > 0 else None
        beat += 1

    return notes


def build_chart_notes(cfg):
    rng = random.Random(cfg["seed"])
    difficulty = cfg["difficulty"]
    first_bank = [build_phrase(rng, difficulty, dense=False) for _ in range(4)]
    second_bank = [build_phrase(rng, difficulty, dense=True) for _ in range(4)]
    half = cfg["pairs"] // 2

    notes = []
    for k in range(cfg["pairs"]):
        bank, index = (first_bank, k) if k < half else (second_bank, k - half)
        phrase = bank[PHRASE_ORDER[index % len(PHRASE_ORDER)]]
        call_start = INTRO_BEATS + k * 2 * PHRASE_BEATS
        answer_start = call_start + PHRASE_BEATS
        mirrored = rng.random() < cfg["mirror"]

        for beat, lanes, hold in phrase:
            for lane in lanes:
                notes.append(dict(beat=call_start + beat, lane=lane, side=0, hold=hold))
                answer_lane = 3 - lane if mirrored else lane
                notes.append(dict(beat=answer_start + beat, lane=answer_lane, side=1, hold=hold))

    notes.sort(key=lambda n: (n["beat"], n["side"], n["lane"]))
    return notes


def song_length_beats(cfg):
    return INTRO_BEATS + cfg["pairs"] * 2 * PHRASE_BEATS + OUTRO_BEATS


def write_chart(cfg, notes):
    CHART_DIR.mkdir(parents=True, exist_ok=True)
    header = dict(
        formatVersion=1,
        song=cfg["song"],
        level=cfg["level"],
        stage=cfg["stage"],
        opponentName=cfg["opponent"],
        difficulty=cfg["difficulty"],
        bpm=cfg["bpm"],
        beatsPerBar=4,
        scrollSpeed=cfg["speed"],
        offsetMs=0,
        lengthBeats=song_length_beats(cfg),
        ai=cfg["ai"],
    )
    lines = ["{"]
    for key, value in header.items():
        lines.append(f'  "{key}": {json.dumps(value)},')
    lines.append('  "notes": [')
    for i, n in enumerate(notes):
        comma = "," if i < len(notes) - 1 else ""
        lines.append(f'    {{"beat": {n["beat"]:g}, "lane": {n["lane"]}, "side": {n["side"]}, "hold": {n["hold"]:g}}}{comma}')
    lines.append("  ]")
    lines.append("}")
    path = CHART_DIR / f'level{cfg["level"]}-{cfg["slug"]}.json'
    path.write_text("\n".join(lines) + "\n", encoding="utf-8")
    return path


# ── Harmony ───────────────────────────────────────────────────────────────────

def hz(midi):
    return 440.0 * 2.0 ** ((midi - 69) / 12.0)


def chord_root_midi(cfg, name):
    key_name, key_octave = cfg["key"]
    key_midi = 12 * (key_octave + 1) + NOTE_INDEX[key_name]
    root = 12 * (key_octave + 1) + NOTE_INDEX[name]
    while root > key_midi + 6:
        root -= 12
    while root < key_midi - 5:
        root += 12
    return root


def chord_for_bar(cfg, bar, total_bars):
    chords = cfg["chords"]
    if bar < 2:
        return chords[0] if bar == 0 else chords[3]
    if bar >= total_bars - 2:
        return chords[0]
    return chords[(bar - 2) % 4]


def chord_tones(cfg, chord, octave_shift=0):
    name, quality = chord
    root = chord_root_midi(cfg, name) + 12 * octave_shift
    third = 3 if quality == "m" else 4
    return [root, root + third, root + 7, root + 12]


# ── Synthesis ─────────────────────────────────────────────────────────────────

def lowpass(signal, cutoff):
    a = math.exp(-2.0 * math.pi * cutoff / SR)
    return lfilter([1.0 - a], [1.0, -a], signal)


def highpass(signal, cutoff):
    return signal - lowpass(signal, cutoff)


def envelope(n, attack=0.008, release=0.04, sustain=0.85, decay=0.12):
    t = np.arange(n) / SR
    env = np.ones(n)
    a = max(1, int(attack * SR))
    env[:a] = np.linspace(0.0, 1.0, a)
    env *= sustain + (1.0 - sustain) * np.exp(-np.maximum(t - attack, 0) / decay)
    r = min(n, max(1, int(release * SR)))
    env[-r:] *= np.linspace(1.0, 0.0, r)
    return env


def phase_for(freq, n, vibrato=0.0, vibrato_rate=5.5, vibrato_delay=0.08):
    t = np.arange(n) / SR
    depth = (2.0 ** (vibrato / 12.0) - 1.0) * np.clip((t - vibrato_delay) / 0.1, 0.0, 1.0)
    inst = freq * (1.0 + depth * np.sin(2 * np.pi * vibrato_rate * t))
    return 2 * np.pi * np.cumsum(inst) / SR


def wave(kind, phase):
    cycle = (phase / (2 * np.pi)) % 1.0
    if kind == "sine":
        return np.sin(phase)
    if kind == "square":
        return np.where(cycle < 0.5, 0.6, -0.6)
    if kind == "pulse":
        return np.where(cycle < 0.25, 0.6, -0.6)
    if kind == "saw":
        return (2.0 * cycle - 1.0) * 0.7
    if kind == "triangle":
        return 2.0 * np.abs(2.0 * cycle - 1.0) - 1.0
    if kind == "organ":
        return sum(amp * np.sin(k * phase) for k, amp in ((1, 0.6), (2, 0.35), (3, 0.2), (4, 0.12), (6, 0.06)))
    raise ValueError(kind)


def add(buffer, start_seconds, signal, gain=1.0):
    i = int(round(start_seconds * SR))
    if i >= len(buffer) or i + len(signal) <= 0:
        return
    j = min(len(buffer), i + len(signal))
    buffer[i:j] += signal[: j - i] * gain


def sung_note(kind, midi, seconds):
    n = max(1, int(seconds * SR))
    return wave(kind, phase_for(hz(midi), n, vibrato=0.25)) * envelope(n)


def pad_chord(midis, seconds, style):
    n = int(seconds * SR)
    t = np.arange(n) / SR
    partials = ((0.5, 0.25), (1, 0.6), (2, 0.4), (3, 0.25), (4, 0.18), (6, 0.1), (8, 0.08)) if style == "cathedral" \
        else ((1, 0.6), (2, 0.25), (3, 0.1))
    out = np.zeros(n)
    for m in midis:
        f = hz(m)
        for mult, amp in partials:
            out += amp * np.sin(2 * np.pi * f * mult * t + mult)
    trem = 1.0 + 0.06 * np.sin(2 * np.pi * 5.0 * t)
    return out * trem * envelope(n, attack=0.15, release=0.3, sustain=1.0) / len(midis)


def bell(midi, seconds=1.6):
    n = int(seconds * SR)
    t = np.arange(n) / SR
    f = hz(midi)
    body = np.sin(2 * np.pi * f * t) + 0.4 * np.sin(2 * np.pi * 2.76 * f * t) + 0.2 * np.sin(2 * np.pi * 5.4 * f * t)
    return body * np.exp(-t * 3.0) * envelope(n, attack=0.002, release=0.05, sustain=1.0)


def pluck(midi, seconds):
    n = int(seconds * SR)
    t = np.arange(n) / SR
    return wave("saw", 2 * np.pi * hz(midi) * t) * np.exp(-t * 7.0) * envelope(n, attack=0.002, release=0.02, sustain=1.0)


def bass_note(kind, midi, seconds):
    n = int(seconds * SR)
    t = np.arange(n) / SR
    f = hz(midi)
    if kind == "sine":
        body = np.sin(2 * np.pi * f * t)
    elif kind == "square":
        body = wave("square", 2 * np.pi * f * t) * 1.3
    elif kind == "pedal":
        body = np.sin(2 * np.pi * f * t) + 0.3 * wave("saw", 2 * np.pi * f * t)
    else:
        body = np.tanh(2.5 * wave("saw", 2 * np.pi * f * t))
    return body * envelope(n, attack=0.005, release=0.03, sustain=0.8, decay=0.2)


def make_drums(rng):
    def kick():
        n = int(0.35 * SR)
        t = np.arange(n) / SR
        freq = 45 + 75 * np.exp(-t * 30)
        return np.sin(2 * np.pi * np.cumsum(freq) / SR) * np.exp(-t * 14)

    def snare():
        n = int(0.25 * SR)
        t = np.arange(n) / SR
        noise = highpass(np.array([rng.uniform(-1, 1) for _ in range(n)]), 1500)
        return 0.7 * noise * np.exp(-t * 20) + 0.4 * np.sin(2 * np.pi * 185 * t) * np.exp(-t * 30)

    def hat():
        n = int(0.06 * SR)
        t = np.arange(n) / SR
        noise = highpass(np.array([rng.uniform(-1, 1) for _ in range(n)]), 7000)
        return noise * np.exp(-t * 70)

    def crash():
        n = int(1.6 * SR)
        t = np.arange(n) / SR
        noise = highpass(np.array([rng.uniform(-1, 1) for _ in range(n)]), 5000)
        return noise * np.exp(-t * 2.2)

    return dict(kick=kick(), snare=snare(), hat=hat(), crash=crash())


STYLE = {
    "Graveyard": dict(kick=[0], snare=[8], hat=[0, 4, 8, 12], arp="bell", arp_step=4, bass="sine", bass_step=16,
                      pad=0.45, arp_gain=0.22, bass_gain=0.5, drum_gain=0.7, crash_every=0, fill=False),
    "Crypt": dict(kick=[0, 6, 8], snare=[4, 12], hat=[0, 2, 4, 6, 8, 10, 12, 14], arp="pluck", arp_step=2,
                  bass="square", bass_step=2, pad=0.3, arp_gain=0.2, bass_gain=0.32, drum_gain=0.8, crash_every=8, fill=False),
    "Cathedral": dict(kick=[0, 7, 8, 10], snare=[4, 12], hat=[0, 2, 4, 6, 8, 10, 12, 14], arp=None, arp_step=0,
                      bass="pedal", bass_step=8, pad=0.75, arp_gain=0.0, bass_gain=0.45, drum_gain=0.8, crash_every=8, fill=True),
    "VampireCastle": dict(kick=[0, 4, 8, 12, 14], snare=[4, 12], hat=list(range(16)), arp="pluck", arp_step=1,
                          bass="dist", bass_step=2, pad=0.4, arp_gain=0.16, bass_gain=0.3, drum_gain=0.85, crash_every=4, fill=True),
}


def render_song(cfg, notes):
    spb = 60.0 / cfg["bpm"]
    length_beats = song_length_beats(cfg)
    total_bars = length_beats // 4
    tail = 2.0
    n_total = int((length_beats * spb + tail) * SR)
    style = STYLE[cfg["stage"]]
    rng = random.Random(cfg["seed"] * 7)
    drums = make_drums(rng)

    pad = np.zeros(n_total)
    arp = np.zeros(n_total)
    bass = np.zeros(n_total)
    kit = np.zeros(n_total)
    step = spb / 4.0

    for bar in range(total_bars):
        chord = chord_for_bar(cfg, bar, total_bars)
        tones = chord_tones(cfg, chord)
        bar_start = bar * 4 * spb
        is_intro = bar < 2
        is_outro = bar >= total_bars - 2
        phrase_bar = (bar - 2) % 4

        pad_len = 4 * spb * (2.0 if bar == total_bars - 1 else 1.02)
        add(pad, bar_start, pad_chord([tones[0] - 12, tones[1] - 12, tones[2] - 12], pad_len, cfg["stage"]))

        if style["arp"] and not (is_outro and bar == total_bars - 1):
            arp_tones = [t + 12 for t in tones]
            for i, s in enumerate(range(0, 16, style["arp_step"])):
                m = arp_tones[i % len(arp_tones)]
                start = bar_start + s * step
                if style["arp"] == "bell":
                    add(arp, start, bell(m))
                else:
                    add(arp, start, pluck(m, max(0.12, style["arp_step"] * step * 1.5)))

        bass_root = tones[0] - 24
        for s in range(0, 16, style["bass_step"]):
            if is_outro and bar == total_bars - 1 and s > 0:
                break
            length = style["bass_step"] * step * 0.92
            add(bass, bar_start + s * step, bass_note(style["bass"], bass_root, length))

        if is_outro:
            if bar == total_bars - 2:
                add(kit, bar_start, drums["crash"], 0.8)
            continue

        kicks = [0] if is_intro else style["kick"]
        for s in kicks:
            add(kit, bar_start + s * step, drums["kick"], 1.0)
        if not is_intro:
            for s in style["snare"]:
                add(kit, bar_start + s * step, drums["snare"], 0.8)
            for s in style["hat"]:
                add(kit, bar_start + s * step, drums["hat"], 0.35 if s % 4 == 0 else 0.22)
            if style["fill"] and phrase_bar == 3:
                for s in (13, 14, 15):
                    add(kit, bar_start + s * step, drums["snare"], 0.55)
            if style["crash_every"] and (bar - 2) % style["crash_every"] == 0:
                add(kit, bar_start, drums["crash"], 0.6)

    arp = lowpass(arp, 3500)
    bass = lowpass(bass, 1200)
    inst = pad * style["pad"] + arp * style["arp_gain"] * 3 + bass * style["bass_gain"] + kit * style["drum_gain"]

    # Vocals: the chart notes themselves.
    vocals = np.zeros(n_total)
    by_side = {0: [n for n in notes if n["side"] == 0], 1: [n for n in notes if n["side"] == 1]}
    for side, side_notes in by_side.items():
        beats = sorted({n["beat"] for n in side_notes})
        next_beat = {b: (beats[i + 1] if i + 1 < len(beats) else b + 1.0) for i, b in enumerate(beats)}
        kind = cfg["opponent_voice"] if side == 0 else "pulse"
        octave = cfg["opponent_octave"] if side == 0 else cfg["player_octave"]
        key_octave = cfg["key"][1]
        simultaneous = {}
        for n in side_notes:
            simultaneous[n["beat"]] = simultaneous.get(n["beat"], 0) + 1

        for n in side_notes:
            bar = int(n["beat"] // 4)
            tones = chord_tones(cfg, chord_for_bar(cfg, bar, total_bars), octave - key_octave)
            midi = tones[n["lane"]]
            if n["hold"] > 0:
                seconds = (n["hold"] + 0.15) * spb
            else:
                seconds = max(0.07, min(next_beat[n["beat"]] - n["beat"], 0.5) * spb * 0.92)
            gain = 0.55 if simultaneous[n["beat"]] > 1 else 0.8
            add(vocals, n["beat"] * spb, sung_note(kind, midi, seconds), gain)

    vocals = lowpass(vocals, 4200)
    return master(inst, 0.89), master(vocals, 0.8)


def master(signal, peak):
    signal = np.tanh(signal * 1.2) / math.tanh(1.2)
    top = np.max(np.abs(signal))
    return signal * (peak / top) if top > 0 else signal


def write_audio(cfg, inst, vocals):
    import soundfile as sf
    folder = MUSIC_DIR / cfg["slug"]
    folder.mkdir(parents=True, exist_ok=True)
    for name, signal in (("Inst.ogg", inst), ("Voices.ogg", vocals)):
        # Written in blocks: some libsndfile builds crash on large single Vorbis writes.
        with sf.SoundFile(folder / name, "w", SR, 1, format="OGG", subtype="VORBIS") as out:
            data = signal.astype(np.float32)
            for i in range(0, len(data), 8192):
                out.write(data[i:i + 8192])
    return folder


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--level", type=int, choices=[1, 2, 3, 4])
    parser.add_argument("--charts-only", action="store_true")
    args = parser.parse_args()

    for cfg in LEVELS:
        if args.level and cfg["level"] != args.level:
            continue
        notes = build_chart_notes(cfg)
        chart_path = write_chart(cfg, notes)
        seconds = song_length_beats(cfg) * 60.0 / cfg["bpm"]
        print(f'Level {cfg["level"]} {cfg["song"]}: {len(notes)} notes, {seconds:.1f}s -> {chart_path.relative_to(ROOT)}')
        if not args.charts_only:
            inst, vocals = render_song(cfg, notes)
            folder = write_audio(cfg, inst, vocals)
            print(f"    audio -> {folder.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
