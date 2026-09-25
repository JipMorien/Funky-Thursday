# FMOD spike · Unity Audio vs. FMOD

Verkennend onderzoek op branch `fmod-spike`. Deze branch wordt **niet** gemerged; hij bestaat alleen om beide audio-engines eerlijk te vergelijken.

## Wat is er anders op deze branch

- `Core/Audio/IMusicBackend.cs`: één interface voor het afspelen van de muziek.
- `Core/Audio/UnityMusicBackend.cs`: de bestaande aanpak (AudioSource + `AudioSettings.dspTime`).
- `Core/Audio/FmodMusicBackend.cs`: dezelfde nummers via de FMOD Core API (`setDelay` op de DSP-klok). Alleen actief met de scripting define `FT_FMOD`.
- `Core/Conductor.cs`: speelt af via de gekozen engine; de smoothing- en beatlogica is voor beide identiek. Meet daarnaast hoe de audioklok zich gedraagt.
- `Gameplay/TimingLogger.cs`: schrijft na elke gespeelde ronde één regel naar `fmod-spike-timing.csv`.
- Options → **AUDIO ENGINE UNITY / FMOD** om per ronde te wisselen.
- Menu **Funky Thursday → FMOD Spike**: nummers kopiëren naar StreamingAssets en de logmap openen.

De menumuziek en geluidseffecten blijven op Unity Audio; alleen de gameplay-muziek wordt vergeleken.

## Testprotocol

1. Zelfde computer, zelfde koptelefoon, zelfde Timing-modus (STANDARD) en Note Offset.
2. Speel per engine hetzelfde nummer minimaal 3 keer volledig uit (bijv. Crypt Keeper).
3. Wissel de volgorde af (Unity, FMOD, Unity, ...) zodat oefening geen voordeel geeft.
4. Laat bij voorkeur een teamgenoot de engine kiezen zonder te zeggen welke (blind).
5. Open de CSV via Funky Thursday → FMOD Spike → Open Timing Log Folder.

## Kolommen in de CSV

| Kolom | Betekenis |
|---|---|
| `mean_offset_ms` | Gemiddelde afwijking van de hits (negatief = te vroeg). Zegt vooral iets over latency. |
| `sd_offset_ms` | Spreiding van de hits. Lager = strakkere timing. **Belangrijkste maat.** |
| `mean_clock_step_ms` | Hoe vaak de audioklok bijwerkt (bufferlengte). Lager = fijnere klok. |
| `mean_clock_error_ms` / `max_clock_error_ms` | Verschil tussen de audioklok en de gesmoothte songpositie. |
| `hard_resyncs` | Aantal keer dat de conductor hard moest bijspringen (haperingen). |

## Resultaten

| Engine | Rondes | Gem. offset (ms) | SD offset (ms) | Klokstap (ms) | Max klokfout (ms) | Resyncs |
|---|---|---|---|---|---|---|
| Unity |  |  |  |  |  |  |
| FMOD |  |  |  |  |  |  |

## Bevindingen tijdens de implementatie

- FMOD kan Unity-AudioClips niet afspelen: de nummers moeten als losse bestanden in StreamingAssets staan (dubbele opslag).
- Bestanden laden via een pad werkt niet zomaar in een WebGL-build.
- FMOD negeert `AudioListener.volume`; het volume moest apart worden doorgegeven.
- Extra installatie per teamlid (FMOD for Unity-package + scripting define).
- Aanvullen na het testen: ...

## Conclusie

Invullen na het testen.
