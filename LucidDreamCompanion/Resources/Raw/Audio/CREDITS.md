# Bundled audio credits

All six bundled `.wav` files in this folder are **algorithmically synthesized** by `_generate.ps1` at build/setup time. No third-party samples are included or required.

| File | What it is | How it's generated |
| --- | --- | --- |
| `bell-soft.wav`            | A short D5 (~587 Hz) bell with a quick decay. | Sum of harmonics 1× / 2× / 3× the fundamental, exponential envelope. |
| `bell-tibetan.wav`         | A long, low ~196 Hz (G3) bell. | Same shape as above, lower fundamental, longer decay. |
| `voice-are-you-dreaming.wav` | **Placeholder** — two soft bells representing the cadence of "Are you dreaming?". Replace with a real recording when one is available. | Two sequential bells at 880 Hz and 660 Hz. |
| `pink-noise.wav`           | 8 s of pink noise. | Voss-McCartney filter on uniform noise (seed 42 → deterministic). |
| `brown-noise.wav`          | 8 s of brown (red) noise. | Low-pass random walk (seed 17). |
| `binaural-theta.wav`       | 12 s mono "theta-beat" tone. | Two sinusoids at 196.5 Hz and 203.5 Hz summed; the slow 7 Hz beat is in the theta band. True binaural needs stereo + headphones — this is a mono cue. |

Format: 22050 Hz, 16-bit PCM, mono. Android `MediaPlayer` plays these natively; no codec dependency.

**Regenerate** with: `powershell -ExecutionPolicy Bypass -File _generate.ps1`

If you want to swap in curated samples (e.g. CC0 from freesound.org), keep the same filenames so `BundledAudioSeeder` continues to match — or update that seeder to point at new keys.
