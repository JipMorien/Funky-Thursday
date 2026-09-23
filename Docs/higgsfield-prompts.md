# Funky Thursday · Higgsfield prompt library

Generated for the Higgsfield free plan. Work top to bottom: every tier leaves the game fully playable.

**Free-plan costs (checked 23 Sep 2026):** GPT Image 2 (low, 1k) 0.5 credits per image. Wan 2.7 3 credits per 2 s clip, 6 per 4 s clip. Nano Banana and Z Image did not work on the free plan (paid-only / failed jobs). The free plan runs one job at a time.

## Rules that keep the set consistent

- **Characters:** make the base image first, then use it as the video's *start frame* for every pose, so the design never drifts.
- **Loops (idle, backgrounds):** use the same image as both *start* and *end* frame for a seamless loop.
- **Sing and miss poses:** use a start frame only; the pose must hold at the end.
- **Key colour:** every character stands on flat chroma green, except the Gatekeeper (magenta), because his eyes and coat are green.
- **Downloads:** save every file into `HiggsfieldRaw/` at the project root (outside `Assets/`) with the exact name shown, then run the import command.
- **Audio:** turn it off wherever the model offers the option; the game has its own music.

**Style prefix used in every image prompt:** 16-bit dark gothic pixel art, SNES-era game sprite style, crisp hard-edged pixels with a visible pixel grid, limited palette of deep violet, midnight blue, bone white and blood red, dramatic rim lighting, no anti-aliasing, no blur, no painterly brushwork, no text, no watermark, no border

## Tier 1 · Stills

5 character bases + 4 backgrounds, 0.5 credits each. Already generated on 23 Sep 2026: run Tools/fetch_tier1.py to download and import them.

### 01. Vesper · base image

- **Model:** GPT Image 2 · aspect ratio: 1:1 · quality: low · resolution: 1k
- **Save as:** `HiggsfieldRaw/vesper_base.png`
- **Import:** `python Tools/higgs_import.py base HiggsfieldRaw/vesper_base.png --id vesper`

```text
16-bit dark gothic pixel art, SNES-era game sprite style, crisp hard-edged pixels with a visible pixel grid, limited palette of deep violet, midnight blue, bone white and blood red, dramatic rim lighting, no anti-aliasing, no blur, no painterly brushwork, no text, no watermark, no border. Full-body side-view fighting-game character sprite of Vesper, a small hooded wraith bard: a deep navy-blue hooded cloak with a frayed, tattered hem, a face lost in shadow except for two glowing cyan eyes, thin pale bony hands, standing in a relaxed singing stance holding a silver microphone shaped like a candlestick, body turned three-quarters toward the LEFT side of the frame. The whole figure is visible and centered, feet near the bottom edge with a small margin, head in the upper third, arms slightly away from the body. Background: a perfectly flat, solid chroma-key green (#00FF00) with no floor, no cast shadow, no scenery and no gradient.
```

### 02. Graveyard · background still

- **Model:** GPT Image 2 · aspect ratio: 16:9 · quality: low · resolution: 1k
- **Save as:** `HiggsfieldRaw/bg_graveyard-gatekeeper.png`
- **Import:** `python Tools/higgs_import.py background HiggsfieldRaw/bg_graveyard-gatekeeper.png --slug graveyard-gatekeeper`

```text
16-bit dark gothic pixel art, SNES-era game sprite style, crisp hard-edged pixels with a visible pixel grid, limited palette of deep violet, midnight blue, bone white and blood red, dramatic rim lighting, no anti-aliasing, no blur, no painterly brushwork, no text, no watermark, no border. Wide 16:9 side-view 2D rhythm-game stage background of a stormy moonlit graveyard: crooked tombstones and stone crosses, a rusted iron cemetery gate, a dead oak with twisted branches, a full moon behind racing storm clouds, low ground fog. A flat, open ground strip runs across the bottom 15% of the frame for two characters to stand on. The centre of the frame is kept clear and slightly darker so characters read against it; the most detailed landmarks sit in the left and right thirds. No characters, no people, no text.
```

### 03. The Gatekeeper · base image

- **Model:** GPT Image 2 · aspect ratio: 1:1 · quality: low · resolution: 1k
- **Save as:** `HiggsfieldRaw/gatekeeper_base.png`
- **Import:** `python Tools/higgs_import.py base HiggsfieldRaw/gatekeeper_base.png --id gatekeeper`

```text
16-bit dark gothic pixel art, SNES-era game sprite style, crisp hard-edged pixels with a visible pixel grid, limited palette of deep violet, midnight blue, bone white and blood red, dramatic rim lighting, no anti-aliasing, no blur, no painterly brushwork, no text, no watermark, no border. Full-body side-view fighting-game character sprite of The Gatekeeper, a tall skeletal gravedigger in a tattered moss-green greatcoat and a wide-brimmed hat, an iron key ring hanging from his belt, glowing sickly green eyes in a bare skull, standing in a relaxed singing stance holding a rusted iron lantern held like a microphone, body turned three-quarters toward the RIGHT side of the frame. The whole figure is visible and centered, feet near the bottom edge with a small margin, head in the upper third, arms slightly away from the body. Background: a perfectly flat, solid chroma-key magenta (#FF00FF) with no floor, no cast shadow, no scenery and no gradient.
```

### 04. Crypt · background still

- **Model:** GPT Image 2 · aspect ratio: 16:9 · quality: low · resolution: 1k
- **Save as:** `HiggsfieldRaw/bg_crypt-keeper.png`
- **Import:** `python Tools/higgs_import.py background HiggsfieldRaw/bg_crypt-keeper.png --slug crypt-keeper`

```text
16-bit dark gothic pixel art, SNES-era game sprite style, crisp hard-edged pixels with a visible pixel grid, limited palette of deep violet, midnight blue, bone white and blood red, dramatic rim lighting, no anti-aliasing, no blur, no painterly brushwork, no text, no watermark, no border. Wide 16:9 side-view 2D rhythm-game stage background of an underground crypt: vaulted stone arches, walls of skull niches, iron candelabras with melting candles, eerie green torchlight, cobwebs, a sealed stone sarcophagus. A flat, open ground strip runs across the bottom 15% of the frame for two characters to stand on. The centre of the frame is kept clear and slightly darker so characters read against it; the most detailed landmarks sit in the left and right thirds. No characters, no people, no text.
```

### 05. The Crypt Keeper · base image

- **Model:** GPT Image 2 · aspect ratio: 1:1 · quality: low · resolution: 1k
- **Save as:** `HiggsfieldRaw/cryptkeeper_base.png`
- **Import:** `python Tools/higgs_import.py base HiggsfieldRaw/cryptkeeper_base.png --id cryptkeeper`

```text
16-bit dark gothic pixel art, SNES-era game sprite style, crisp hard-edged pixels with a visible pixel grid, limited palette of deep violet, midnight blue, bone white and blood red, dramatic rim lighting, no anti-aliasing, no blur, no painterly brushwork, no text, no watermark, no border. Full-body side-view fighting-game character sprite of The Crypt Keeper, a hunched ghoul wrapped in bone-grey burial linen, long crooked fingers, glowing amber eyes, a crown of melted candle wax dripping down its brow, standing in a relaxed singing stance holding a bone microphone, body turned three-quarters toward the RIGHT side of the frame. The whole figure is visible and centered, feet near the bottom edge with a small margin, head in the upper third, arms slightly away from the body. Background: a perfectly flat, solid chroma-key green (#00FF00) with no floor, no cast shadow, no scenery and no gradient.
```

### 06. Cathedral · background still

- **Model:** GPT Image 2 · aspect ratio: 16:9 · quality: low · resolution: 1k
- **Save as:** `HiggsfieldRaw/bg_cathedral-organist.png`
- **Import:** `python Tools/higgs_import.py background HiggsfieldRaw/bg_cathedral-organist.png --slug cathedral-organist`

```text
16-bit dark gothic pixel art, SNES-era game sprite style, crisp hard-edged pixels with a visible pixel grid, limited palette of deep violet, midnight blue, bone white and blood red, dramatic rim lighting, no anti-aliasing, no blur, no painterly brushwork, no text, no watermark, no border. Wide 16:9 side-view 2D rhythm-game stage background of the nave of a haunted gothic cathedral: a gigantic pipe organ, a stained-glass rose window casting red and violet light, rows of broken pews, tall stone pillars, dust drifting in beams of light. A flat, open ground strip runs across the bottom 15% of the frame for two characters to stand on. The centre of the frame is kept clear and slightly darker so characters read against it; the most detailed landmarks sit in the left and right thirds. No characters, no people, no text.
```

### 07. The Organist · base image

- **Model:** GPT Image 2 · aspect ratio: 1:1 · quality: low · resolution: 1k
- **Save as:** `HiggsfieldRaw/organist_base.png`
- **Import:** `python Tools/higgs_import.py base HiggsfieldRaw/organist_base.png --id organist`

```text
16-bit dark gothic pixel art, SNES-era game sprite style, crisp hard-edged pixels with a visible pixel grid, limited palette of deep violet, midnight blue, bone white and blood red, dramatic rim lighting, no anti-aliasing, no blur, no painterly brushwork, no text, no watermark, no border. Full-body side-view fighting-game character sprite of The Organist, a gaunt phantom in a flowing violet cassock and a cracked white half-mask, long spidery fingers, glowing violet eyes, standing in a relaxed singing stance holding a brass microphone shaped like an organ pipe, body turned three-quarters toward the RIGHT side of the frame. The whole figure is visible and centered, feet near the bottom edge with a small margin, head in the upper third, arms slightly away from the body. Background: a perfectly flat, solid chroma-key green (#00FF00) with no floor, no cast shadow, no scenery and no gradient.
```

### 08. Vampire Castle · background still

- **Model:** GPT Image 2 · aspect ratio: 16:9 · quality: low · resolution: 1k
- **Save as:** `HiggsfieldRaw/bg_gothic-monarch.png`
- **Import:** `python Tools/higgs_import.py background HiggsfieldRaw/bg_gothic-monarch.png --slug gothic-monarch`

```text
16-bit dark gothic pixel art, SNES-era game sprite style, crisp hard-edged pixels with a visible pixel grid, limited palette of deep violet, midnight blue, bone white and blood red, dramatic rim lighting, no anti-aliasing, no blur, no painterly brushwork, no text, no watermark, no border. Wide 16:9 side-view 2D rhythm-game stage background of the throne hall at the top of a vampire castle tower: blood-red banners, a black iron throne, a huge arched window showing a lightning storm, crystal chandeliers with red candles, a checkered marble floor. A flat, open ground strip runs across the bottom 15% of the frame for two characters to stand on. The centre of the frame is kept clear and slightly darker so characters read against it; the most detailed landmarks sit in the left and right thirds. No characters, no people, no text.
```

### 09. The Gothic Monarch · base image

- **Model:** GPT Image 2 · aspect ratio: 1:1 · quality: low · resolution: 1k
- **Save as:** `HiggsfieldRaw/monarch_base.png`
- **Import:** `python Tools/higgs_import.py base HiggsfieldRaw/monarch_base.png --id monarch`

```text
16-bit dark gothic pixel art, SNES-era game sprite style, crisp hard-edged pixels with a visible pixel grid, limited palette of deep violet, midnight blue, bone white and blood red, dramatic rim lighting, no anti-aliasing, no blur, no painterly brushwork, no text, no watermark, no border. Full-body side-view fighting-game character sprite of The Gothic Monarch, a towering vampire king in black spiked armour and a crimson high-collared cape, an iron crown, pale grey skin, fangs, glowing blood-red eyes, standing in a relaxed singing stance holding a jewelled golden scepter used as a microphone, body turned three-quarters toward the RIGHT side of the frame. The whole figure is visible and centered, feet near the bottom edge with a small margin, head in the upper third, arms slightly away from the body. Background: a perfectly flat, solid chroma-key green (#00FF00) with no floor, no cast shadow, no scenery and no gradient.
```

## Tier 2 · Player animation

Vesper's idle loop and four sing poses. Vesper is on screen in every level, so this is the best use of video credits.

### 10. Vesper · idle

- **Model:** Wan 2.7 · duration: 2 s · aspect ratio: 1:1 · resolution: 720p
- **Start frame:** vesper_base.png
- **End frame:** vesper_base.png (same image, for a seamless loop)
- **Save as:** `HiggsfieldRaw/vesper_idle.mp4`
- **Import:** `python Tools/higgs_import.py pose HiggsfieldRaw/vesper_idle.mp4 --id vesper --pose idle`

```text
Vesper, the pixel-art character in the start frame. The character bobs to a slow, steady beat: shoulders dip and rise twice, the navy cloak sways, the cyan eyes blink once, then everything settles back into exactly the starting pose so the clip loops seamlessly. Locked-off static camera: no zoom, no pan, no cuts, no camera shake. The background stays a perfectly flat, even chroma-key green (#00FF00) for the entire clip. Keep the exact same character design, proportions, colours and pixel-art style as the start frame. Snappy, exaggerated 2D fighting-game animation with clear key poses.
```

### 11. Vesper · left

- **Model:** Wan 2.7 · duration: 2 s · aspect ratio: 1:1 · resolution: 720p
- **Start frame:** vesper_base.png
- **Save as:** `HiggsfieldRaw/vesper_left.mp4`
- **Import:** `python Tools/higgs_import.py pose HiggsfieldRaw/vesper_left.mp4 --id vesper --pose left`

```text
Vesper, the pixel-art character in the start frame. Within the first half second the character snaps into a singing pose leaning hard toward the LEFT edge of the frame (screen-left): the arm and a silver microphone shaped like a candlestick thrust to screen-left, mouth or jaw wide open singing, the navy cloak whips to the left. Then the character holds that pose, vibrating slightly with the note. Locked-off static camera: no zoom, no pan, no cuts, no camera shake. The background stays a perfectly flat, even chroma-key green (#00FF00) for the entire clip. Keep the exact same character design, proportions, colours and pixel-art style as the start frame. Snappy, exaggerated 2D fighting-game animation with clear key poses.
```

### 12. Vesper · down

- **Model:** Wan 2.7 · duration: 2 s · aspect ratio: 1:1 · resolution: 720p
- **Start frame:** vesper_base.png
- **Save as:** `HiggsfieldRaw/vesper_down.mp4`
- **Import:** `python Tools/higgs_import.py pose HiggsfieldRaw/vesper_down.mp4 --id vesper --pose down`

```text
Vesper, the pixel-art character in the start frame. Within the first half second the character snaps into a low crouch, head and a silver microphone shaped like a candlestick pointed down at the floor, singing hard, knees bent, the navy cloak pooling on the ground. Then holds that pose, vibrating slightly. Locked-off static camera: no zoom, no pan, no cuts, no camera shake. The background stays a perfectly flat, even chroma-key green (#00FF00) for the entire clip. Keep the exact same character design, proportions, colours and pixel-art style as the start frame. Snappy, exaggerated 2D fighting-game animation with clear key poses.
```

### 13. Vesper · up

- **Model:** Wan 2.7 · duration: 2 s · aspect ratio: 1:1 · resolution: 720p
- **Start frame:** vesper_base.png
- **Save as:** `HiggsfieldRaw/vesper_up.mp4`
- **Import:** `python Tools/higgs_import.py pose HiggsfieldRaw/vesper_up.mp4 --id vesper --pose up`

```text
Vesper, the pixel-art character in the start frame. Within the first half second the character stretches tall, rising onto tiptoe with the a silver microphone shaped like a candlestick raised high toward the top of the frame, singing upward, the navy cloak lifting. Then holds that pose, vibrating slightly. Locked-off static camera: no zoom, no pan, no cuts, no camera shake. The background stays a perfectly flat, even chroma-key green (#00FF00) for the entire clip. Keep the exact same character design, proportions, colours and pixel-art style as the start frame. Snappy, exaggerated 2D fighting-game animation with clear key poses.
```

### 14. Vesper · right

- **Model:** Wan 2.7 · duration: 2 s · aspect ratio: 1:1 · resolution: 720p
- **Start frame:** vesper_base.png
- **Save as:** `HiggsfieldRaw/vesper_right.mp4`
- **Import:** `python Tools/higgs_import.py pose HiggsfieldRaw/vesper_right.mp4 --id vesper --pose right`

```text
Vesper, the pixel-art character in the start frame. Within the first half second the character snaps into a singing pose leaning hard toward the RIGHT edge of the frame (screen-right): the arm and a silver microphone shaped like a candlestick thrust to screen-right, mouth or jaw wide open singing, the navy cloak whips to the right. Then the character holds that pose, vibrating slightly with the note. Locked-off static camera: no zoom, no pan, no cuts, no camera shake. The background stays a perfectly flat, even chroma-key green (#00FF00) for the entire clip. Keep the exact same character design, proportions, colours and pixel-art style as the start frame. Snappy, exaggerated 2D fighting-game animation with clear key poses.
```

## Tier 3 · Opponent animation

Idle + four sing poses per opponent, in level order.

### 15. The Gatekeeper · idle

- **Model:** Wan 2.7 · duration: 2 s · aspect ratio: 1:1 · resolution: 720p
- **Start frame:** gatekeeper_base.png
- **End frame:** gatekeeper_base.png (same image, for a seamless loop)
- **Save as:** `HiggsfieldRaw/gatekeeper_idle.mp4`
- **Import:** `python Tools/higgs_import.py pose HiggsfieldRaw/gatekeeper_idle.mp4 --id gatekeeper --pose idle`

```text
The Gatekeeper, the pixel-art character in the start frame. The character bobs to a slow, steady beat: shoulders dip and rise twice, the greatcoat sways, the lantern swings and its flame flickers, then everything settles back into exactly the starting pose so the clip loops seamlessly. Locked-off static camera: no zoom, no pan, no cuts, no camera shake. The background stays a perfectly flat, even chroma-key magenta (#FF00FF) for the entire clip. Keep the exact same character design, proportions, colours and pixel-art style as the start frame. Snappy, exaggerated 2D fighting-game animation with clear key poses.
```

### 16. The Gatekeeper · left

- **Model:** Wan 2.7 · duration: 2 s · aspect ratio: 1:1 · resolution: 720p
- **Start frame:** gatekeeper_base.png
- **Save as:** `HiggsfieldRaw/gatekeeper_left.mp4`
- **Import:** `python Tools/higgs_import.py pose HiggsfieldRaw/gatekeeper_left.mp4 --id gatekeeper --pose left`

```text
The Gatekeeper, the pixel-art character in the start frame. Within the first half second the character snaps into a singing pose leaning hard toward the LEFT edge of the frame (screen-left): the arm and a rusted iron lantern held like a microphone thrust to screen-left, mouth or jaw wide open singing, the greatcoat whips to the left. Then the character holds that pose, vibrating slightly with the note. Locked-off static camera: no zoom, no pan, no cuts, no camera shake. The background stays a perfectly flat, even chroma-key magenta (#FF00FF) for the entire clip. Keep the exact same character design, proportions, colours and pixel-art style as the start frame. Snappy, exaggerated 2D fighting-game animation with clear key poses.
```

### 17. The Gatekeeper · down

- **Model:** Wan 2.7 · duration: 2 s · aspect ratio: 1:1 · resolution: 720p
- **Start frame:** gatekeeper_base.png
- **Save as:** `HiggsfieldRaw/gatekeeper_down.mp4`
- **Import:** `python Tools/higgs_import.py pose HiggsfieldRaw/gatekeeper_down.mp4 --id gatekeeper --pose down`

```text
The Gatekeeper, the pixel-art character in the start frame. Within the first half second the character snaps into a low crouch, head and a rusted iron lantern held like a microphone pointed down at the floor, singing hard, knees bent, the greatcoat pooling on the ground. Then holds that pose, vibrating slightly. Locked-off static camera: no zoom, no pan, no cuts, no camera shake. The background stays a perfectly flat, even chroma-key magenta (#FF00FF) for the entire clip. Keep the exact same character design, proportions, colours and pixel-art style as the start frame. Snappy, exaggerated 2D fighting-game animation with clear key poses.
```

### 18. The Gatekeeper · up

- **Model:** Wan 2.7 · duration: 2 s · aspect ratio: 1:1 · resolution: 720p
- **Start frame:** gatekeeper_base.png
- **Save as:** `HiggsfieldRaw/gatekeeper_up.mp4`
- **Import:** `python Tools/higgs_import.py pose HiggsfieldRaw/gatekeeper_up.mp4 --id gatekeeper --pose up`

```text
The Gatekeeper, the pixel-art character in the start frame. Within the first half second the character stretches tall, rising onto tiptoe with the a rusted iron lantern held like a microphone raised high toward the top of the frame, singing upward, the greatcoat lifting. Then holds that pose, vibrating slightly. Locked-off static camera: no zoom, no pan, no cuts, no camera shake. The background stays a perfectly flat, even chroma-key magenta (#FF00FF) for the entire clip. Keep the exact same character design, proportions, colours and pixel-art style as the start frame. Snappy, exaggerated 2D fighting-game animation with clear key poses.
```

### 19. The Gatekeeper · right

- **Model:** Wan 2.7 · duration: 2 s · aspect ratio: 1:1 · resolution: 720p
- **Start frame:** gatekeeper_base.png
- **Save as:** `HiggsfieldRaw/gatekeeper_right.mp4`
- **Import:** `python Tools/higgs_import.py pose HiggsfieldRaw/gatekeeper_right.mp4 --id gatekeeper --pose right`

```text
The Gatekeeper, the pixel-art character in the start frame. Within the first half second the character snaps into a singing pose leaning hard toward the RIGHT edge of the frame (screen-right): the arm and a rusted iron lantern held like a microphone thrust to screen-right, mouth or jaw wide open singing, the greatcoat whips to the right. Then the character holds that pose, vibrating slightly with the note. Locked-off static camera: no zoom, no pan, no cuts, no camera shake. The background stays a perfectly flat, even chroma-key magenta (#FF00FF) for the entire clip. Keep the exact same character design, proportions, colours and pixel-art style as the start frame. Snappy, exaggerated 2D fighting-game animation with clear key poses.
```

### 20. The Crypt Keeper · idle

- **Model:** Wan 2.7 · duration: 2 s · aspect ratio: 1:1 · resolution: 720p
- **Start frame:** cryptkeeper_base.png
- **End frame:** cryptkeeper_base.png (same image, for a seamless loop)
- **Save as:** `HiggsfieldRaw/cryptkeeper_idle.mp4`
- **Import:** `python Tools/higgs_import.py pose HiggsfieldRaw/cryptkeeper_idle.mp4 --id cryptkeeper --pose idle`

```text
The Crypt Keeper, the pixel-art character in the start frame. The character bobs to a slow, steady beat: shoulders dip and rise twice, the burial linen sways, loose linen strips flutter, then everything settles back into exactly the starting pose so the clip loops seamlessly. Locked-off static camera: no zoom, no pan, no cuts, no camera shake. The background stays a perfectly flat, even chroma-key green (#00FF00) for the entire clip. Keep the exact same character design, proportions, colours and pixel-art style as the start frame. Snappy, exaggerated 2D fighting-game animation with clear key poses.
```

### 21. The Crypt Keeper · left

- **Model:** Wan 2.7 · duration: 2 s · aspect ratio: 1:1 · resolution: 720p
- **Start frame:** cryptkeeper_base.png
- **Save as:** `HiggsfieldRaw/cryptkeeper_left.mp4`
- **Import:** `python Tools/higgs_import.py pose HiggsfieldRaw/cryptkeeper_left.mp4 --id cryptkeeper --pose left`

```text
The Crypt Keeper, the pixel-art character in the start frame. Within the first half second the character snaps into a singing pose leaning hard toward the LEFT edge of the frame (screen-left): the arm and a bone microphone thrust to screen-left, mouth or jaw wide open singing, the burial linen whips to the left. Then the character holds that pose, vibrating slightly with the note. Locked-off static camera: no zoom, no pan, no cuts, no camera shake. The background stays a perfectly flat, even chroma-key green (#00FF00) for the entire clip. Keep the exact same character design, proportions, colours and pixel-art style as the start frame. Snappy, exaggerated 2D fighting-game animation with clear key poses.
```

### 22. The Crypt Keeper · down

- **Model:** Wan 2.7 · duration: 2 s · aspect ratio: 1:1 · resolution: 720p
- **Start frame:** cryptkeeper_base.png
- **Save as:** `HiggsfieldRaw/cryptkeeper_down.mp4`
- **Import:** `python Tools/higgs_import.py pose HiggsfieldRaw/cryptkeeper_down.mp4 --id cryptkeeper --pose down`

```text
The Crypt Keeper, the pixel-art character in the start frame. Within the first half second the character snaps into a low crouch, head and a bone microphone pointed down at the floor, singing hard, knees bent, the burial linen pooling on the ground. Then holds that pose, vibrating slightly. Locked-off static camera: no zoom, no pan, no cuts, no camera shake. The background stays a perfectly flat, even chroma-key green (#00FF00) for the entire clip. Keep the exact same character design, proportions, colours and pixel-art style as the start frame. Snappy, exaggerated 2D fighting-game animation with clear key poses.
```

### 23. The Crypt Keeper · up

- **Model:** Wan 2.7 · duration: 2 s · aspect ratio: 1:1 · resolution: 720p
- **Start frame:** cryptkeeper_base.png
- **Save as:** `HiggsfieldRaw/cryptkeeper_up.mp4`
- **Import:** `python Tools/higgs_import.py pose HiggsfieldRaw/cryptkeeper_up.mp4 --id cryptkeeper --pose up`

```text
The Crypt Keeper, the pixel-art character in the start frame. Within the first half second the character stretches tall, rising onto tiptoe with the a bone microphone raised high toward the top of the frame, singing upward, the burial linen lifting. Then holds that pose, vibrating slightly. Locked-off static camera: no zoom, no pan, no cuts, no camera shake. The background stays a perfectly flat, even chroma-key green (#00FF00) for the entire clip. Keep the exact same character design, proportions, colours and pixel-art style as the start frame. Snappy, exaggerated 2D fighting-game animation with clear key poses.
```

### 24. The Crypt Keeper · right

- **Model:** Wan 2.7 · duration: 2 s · aspect ratio: 1:1 · resolution: 720p
- **Start frame:** cryptkeeper_base.png
- **Save as:** `HiggsfieldRaw/cryptkeeper_right.mp4`
- **Import:** `python Tools/higgs_import.py pose HiggsfieldRaw/cryptkeeper_right.mp4 --id cryptkeeper --pose right`

```text
The Crypt Keeper, the pixel-art character in the start frame. Within the first half second the character snaps into a singing pose leaning hard toward the RIGHT edge of the frame (screen-right): the arm and a bone microphone thrust to screen-right, mouth or jaw wide open singing, the burial linen whips to the right. Then the character holds that pose, vibrating slightly with the note. Locked-off static camera: no zoom, no pan, no cuts, no camera shake. The background stays a perfectly flat, even chroma-key green (#00FF00) for the entire clip. Keep the exact same character design, proportions, colours and pixel-art style as the start frame. Snappy, exaggerated 2D fighting-game animation with clear key poses.
```

### 25. The Organist · idle

- **Model:** Wan 2.7 · duration: 2 s · aspect ratio: 1:1 · resolution: 720p
- **Start frame:** organist_base.png
- **End frame:** organist_base.png (same image, for a seamless loop)
- **Save as:** `HiggsfieldRaw/organist_idle.mp4`
- **Import:** `python Tools/higgs_import.py pose HiggsfieldRaw/organist_idle.mp4 --id organist --pose idle`

```text
The Organist, the pixel-art character in the start frame. The character bobs to a slow, steady beat: shoulders dip and rise twice, the cassock sways, the spidery fingers drum the air as if playing keys, then everything settles back into exactly the starting pose so the clip loops seamlessly. Locked-off static camera: no zoom, no pan, no cuts, no camera shake. The background stays a perfectly flat, even chroma-key green (#00FF00) for the entire clip. Keep the exact same character design, proportions, colours and pixel-art style as the start frame. Snappy, exaggerated 2D fighting-game animation with clear key poses.
```

### 26. The Organist · left

- **Model:** Wan 2.7 · duration: 2 s · aspect ratio: 1:1 · resolution: 720p
- **Start frame:** organist_base.png
- **Save as:** `HiggsfieldRaw/organist_left.mp4`
- **Import:** `python Tools/higgs_import.py pose HiggsfieldRaw/organist_left.mp4 --id organist --pose left`

```text
The Organist, the pixel-art character in the start frame. Within the first half second the character snaps into a singing pose leaning hard toward the LEFT edge of the frame (screen-left): the arm and a brass microphone shaped like an organ pipe thrust to screen-left, mouth or jaw wide open singing, the cassock whips to the left. Then the character holds that pose, vibrating slightly with the note. Locked-off static camera: no zoom, no pan, no cuts, no camera shake. The background stays a perfectly flat, even chroma-key green (#00FF00) for the entire clip. Keep the exact same character design, proportions, colours and pixel-art style as the start frame. Snappy, exaggerated 2D fighting-game animation with clear key poses.
```

### 27. The Organist · down

- **Model:** Wan 2.7 · duration: 2 s · aspect ratio: 1:1 · resolution: 720p
- **Start frame:** organist_base.png
- **Save as:** `HiggsfieldRaw/organist_down.mp4`
- **Import:** `python Tools/higgs_import.py pose HiggsfieldRaw/organist_down.mp4 --id organist --pose down`

```text
The Organist, the pixel-art character in the start frame. Within the first half second the character snaps into a low crouch, head and a brass microphone shaped like an organ pipe pointed down at the floor, singing hard, knees bent, the cassock pooling on the ground. Then holds that pose, vibrating slightly. Locked-off static camera: no zoom, no pan, no cuts, no camera shake. The background stays a perfectly flat, even chroma-key green (#00FF00) for the entire clip. Keep the exact same character design, proportions, colours and pixel-art style as the start frame. Snappy, exaggerated 2D fighting-game animation with clear key poses.
```

### 28. The Organist · up

- **Model:** Wan 2.7 · duration: 2 s · aspect ratio: 1:1 · resolution: 720p
- **Start frame:** organist_base.png
- **Save as:** `HiggsfieldRaw/organist_up.mp4`
- **Import:** `python Tools/higgs_import.py pose HiggsfieldRaw/organist_up.mp4 --id organist --pose up`

```text
The Organist, the pixel-art character in the start frame. Within the first half second the character stretches tall, rising onto tiptoe with the a brass microphone shaped like an organ pipe raised high toward the top of the frame, singing upward, the cassock lifting. Then holds that pose, vibrating slightly. Locked-off static camera: no zoom, no pan, no cuts, no camera shake. The background stays a perfectly flat, even chroma-key green (#00FF00) for the entire clip. Keep the exact same character design, proportions, colours and pixel-art style as the start frame. Snappy, exaggerated 2D fighting-game animation with clear key poses.
```

### 29. The Organist · right

- **Model:** Wan 2.7 · duration: 2 s · aspect ratio: 1:1 · resolution: 720p
- **Start frame:** organist_base.png
- **Save as:** `HiggsfieldRaw/organist_right.mp4`
- **Import:** `python Tools/higgs_import.py pose HiggsfieldRaw/organist_right.mp4 --id organist --pose right`

```text
The Organist, the pixel-art character in the start frame. Within the first half second the character snaps into a singing pose leaning hard toward the RIGHT edge of the frame (screen-right): the arm and a brass microphone shaped like an organ pipe thrust to screen-right, mouth or jaw wide open singing, the cassock whips to the right. Then the character holds that pose, vibrating slightly with the note. Locked-off static camera: no zoom, no pan, no cuts, no camera shake. The background stays a perfectly flat, even chroma-key green (#00FF00) for the entire clip. Keep the exact same character design, proportions, colours and pixel-art style as the start frame. Snappy, exaggerated 2D fighting-game animation with clear key poses.
```

### 30. The Gothic Monarch · idle

- **Model:** Wan 2.7 · duration: 2 s · aspect ratio: 1:1 · resolution: 720p
- **Start frame:** monarch_base.png
- **End frame:** monarch_base.png (same image, for a seamless loop)
- **Save as:** `HiggsfieldRaw/monarch_idle.mp4`
- **Import:** `python Tools/higgs_import.py pose HiggsfieldRaw/monarch_idle.mp4 --id monarch --pose idle`

```text
The Gothic Monarch, the pixel-art character in the start frame. The character bobs to a slow, steady beat: shoulders dip and rise twice, the crimson cape sways, the cape billows behind him, then everything settles back into exactly the starting pose so the clip loops seamlessly. Locked-off static camera: no zoom, no pan, no cuts, no camera shake. The background stays a perfectly flat, even chroma-key green (#00FF00) for the entire clip. Keep the exact same character design, proportions, colours and pixel-art style as the start frame. Snappy, exaggerated 2D fighting-game animation with clear key poses.
```

### 31. The Gothic Monarch · left

- **Model:** Wan 2.7 · duration: 2 s · aspect ratio: 1:1 · resolution: 720p
- **Start frame:** monarch_base.png
- **Save as:** `HiggsfieldRaw/monarch_left.mp4`
- **Import:** `python Tools/higgs_import.py pose HiggsfieldRaw/monarch_left.mp4 --id monarch --pose left`

```text
The Gothic Monarch, the pixel-art character in the start frame. Within the first half second the character snaps into a singing pose leaning hard toward the LEFT edge of the frame (screen-left): the arm and a jewelled golden scepter used as a microphone thrust to screen-left, mouth or jaw wide open singing, the crimson cape whips to the left. Then the character holds that pose, vibrating slightly with the note. Locked-off static camera: no zoom, no pan, no cuts, no camera shake. The background stays a perfectly flat, even chroma-key green (#00FF00) for the entire clip. Keep the exact same character design, proportions, colours and pixel-art style as the start frame. Snappy, exaggerated 2D fighting-game animation with clear key poses.
```

### 32. The Gothic Monarch · down

- **Model:** Wan 2.7 · duration: 2 s · aspect ratio: 1:1 · resolution: 720p
- **Start frame:** monarch_base.png
- **Save as:** `HiggsfieldRaw/monarch_down.mp4`
- **Import:** `python Tools/higgs_import.py pose HiggsfieldRaw/monarch_down.mp4 --id monarch --pose down`

```text
The Gothic Monarch, the pixel-art character in the start frame. Within the first half second the character snaps into a low crouch, head and a jewelled golden scepter used as a microphone pointed down at the floor, singing hard, knees bent, the crimson cape pooling on the ground. Then holds that pose, vibrating slightly. Locked-off static camera: no zoom, no pan, no cuts, no camera shake. The background stays a perfectly flat, even chroma-key green (#00FF00) for the entire clip. Keep the exact same character design, proportions, colours and pixel-art style as the start frame. Snappy, exaggerated 2D fighting-game animation with clear key poses.
```

### 33. The Gothic Monarch · up

- **Model:** Wan 2.7 · duration: 2 s · aspect ratio: 1:1 · resolution: 720p
- **Start frame:** monarch_base.png
- **Save as:** `HiggsfieldRaw/monarch_up.mp4`
- **Import:** `python Tools/higgs_import.py pose HiggsfieldRaw/monarch_up.mp4 --id monarch --pose up`

```text
The Gothic Monarch, the pixel-art character in the start frame. Within the first half second the character stretches tall, rising onto tiptoe with the a jewelled golden scepter used as a microphone raised high toward the top of the frame, singing upward, the crimson cape lifting. Then holds that pose, vibrating slightly. Locked-off static camera: no zoom, no pan, no cuts, no camera shake. The background stays a perfectly flat, even chroma-key green (#00FF00) for the entire clip. Keep the exact same character design, proportions, colours and pixel-art style as the start frame. Snappy, exaggerated 2D fighting-game animation with clear key poses.
```

### 34. The Gothic Monarch · right

- **Model:** Wan 2.7 · duration: 2 s · aspect ratio: 1:1 · resolution: 720p
- **Start frame:** monarch_base.png
- **Save as:** `HiggsfieldRaw/monarch_right.mp4`
- **Import:** `python Tools/higgs_import.py pose HiggsfieldRaw/monarch_right.mp4 --id monarch --pose right`

```text
The Gothic Monarch, the pixel-art character in the start frame. Within the first half second the character snaps into a singing pose leaning hard toward the RIGHT edge of the frame (screen-right): the arm and a jewelled golden scepter used as a microphone thrust to screen-right, mouth or jaw wide open singing, the crimson cape whips to the right. Then the character holds that pose, vibrating slightly with the note. Locked-off static camera: no zoom, no pan, no cuts, no camera shake. The background stays a perfectly flat, even chroma-key green (#00FF00) for the entire clip. Keep the exact same character design, proportions, colours and pixel-art style as the start frame. Snappy, exaggerated 2D fighting-game animation with clear key poses.
```

## Tier 4 · Background loops

Ambient 4-second loops. Stills already look fine, so these are polish.

### 35. Graveyard · ambient loop

- **Model:** Wan 2.7 · duration: 4 s · aspect ratio: 16:9 · resolution: 720p
- **Start frame:** bg_graveyard-gatekeeper.png
- **End frame:** bg_graveyard-gatekeeper.png (same image, for a seamless loop)
- **Save as:** `HiggsfieldRaw/bg_graveyard-gatekeeper_loop.mp4`
- **Import:** `python Tools/higgs_import.py background HiggsfieldRaw/bg_graveyard-gatekeeper_loop.mp4 --slug graveyard-gatekeeper --video`

```text
An ambient loop of this exact pixel-art scene. The camera is completely static. Only subtle motion: ground fog drifts slowly from left to right, storm clouds crawl across the moon, the dead oak's branches sway, one distant lightning flicker. No characters appear. Everything returns to exactly the first frame at the end so the clip loops seamlessly. Keep the crisp 16-bit pixel-art look of the start frame.
```

### 36. Crypt · ambient loop

- **Model:** Wan 2.7 · duration: 4 s · aspect ratio: 16:9 · resolution: 720p
- **Start frame:** bg_crypt-keeper.png
- **End frame:** bg_crypt-keeper.png (same image, for a seamless loop)
- **Save as:** `HiggsfieldRaw/bg_crypt-keeper_loop.mp4`
- **Import:** `python Tools/higgs_import.py background HiggsfieldRaw/bg_crypt-keeper_loop.mp4 --slug crypt-keeper --video`

```text
An ambient loop of this exact pixel-art scene. The camera is completely static. Only subtle motion: candle and torch flames flicker, the green light pulses softly, dust motes float, cobwebs sway. No characters appear. Everything returns to exactly the first frame at the end so the clip loops seamlessly. Keep the crisp 16-bit pixel-art look of the start frame.
```

### 37. Cathedral · ambient loop

- **Model:** Wan 2.7 · duration: 4 s · aspect ratio: 16:9 · resolution: 720p
- **Start frame:** bg_cathedral-organist.png
- **End frame:** bg_cathedral-organist.png (same image, for a seamless loop)
- **Save as:** `HiggsfieldRaw/bg_cathedral-organist_loop.mp4`
- **Import:** `python Tools/higgs_import.py background HiggsfieldRaw/bg_cathedral-organist_loop.mp4 --slug cathedral-organist --video`

```text
An ambient loop of this exact pixel-art scene. The camera is completely static. Only subtle motion: light beams through the rose window shimmer, dust drifts through them, candle flames flicker, faint mist rolls along the floor. No characters appear. Everything returns to exactly the first frame at the end so the clip loops seamlessly. Keep the crisp 16-bit pixel-art look of the start frame.
```

### 38. Vampire Castle · ambient loop

- **Model:** Wan 2.7 · duration: 4 s · aspect ratio: 16:9 · resolution: 720p
- **Start frame:** bg_gothic-monarch.png
- **End frame:** bg_gothic-monarch.png (same image, for a seamless loop)
- **Save as:** `HiggsfieldRaw/bg_gothic-monarch_loop.mp4`
- **Import:** `python Tools/higgs_import.py background HiggsfieldRaw/bg_gothic-monarch_loop.mp4 --slug gothic-monarch --video`

```text
An ambient loop of this exact pixel-art scene. The camera is completely static. Only subtle motion: rain streaks past the window, one bright lightning flash lights the hall, banners ripple, chandelier candles flicker. No characters appear. Everything returns to exactly the first frame at the end so the clip loops seamlessly. Keep the crisp 16-bit pixel-art look of the start frame.
```

## Tier 5 · Miss reaction

Without it, Vesper flashes grey on a miss, which already reads well.

### 39. Vesper · miss

- **Model:** Wan 2.7 · duration: 2 s · aspect ratio: 1:1 · resolution: 720p
- **Start frame:** vesper_base.png
- **Save as:** `HiggsfieldRaw/vesper_miss.mp4`
- **Import:** `python Tools/higgs_import.py pose HiggsfieldRaw/vesper_miss.mp4 --id vesper --pose miss`

```text
Vesper, the pixel-art character in the start frame. The character flinches backward in shock: the hood droops, the glowing eyes dim to grey, arms flail, a small puff of dark smoke pops beside the head. Then holds the stunned, embarrassed pose. Locked-off static camera: no zoom, no pan, no cuts, no camera shake. The background stays a perfectly flat, even chroma-key green (#00FF00) for the entire clip. Keep the exact same character design, proportions, colours and pixel-art style as the start frame. Snappy, exaggerated 2D fighting-game animation with clear key poses.
```
