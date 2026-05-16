# Generates the bundled audio assets for Lucid Dream Companion.
# Outputs mono 16-bit PCM WAVs at 22050 Hz so MediaPlayer can stream them straight from MauiAssets.
# These are placeholders synthesized in code — replace with curated samples whenever you like
# (just keep the filenames so BundledAudioSeeder still finds them).

param([string]$OutDir = $PSScriptRoot)

function Write-Wav {
    param(
        [string]$Path,
        [double[]]$Samples,
        [int]$SampleRate = 22050
    )
    $byteRate = $SampleRate * 2
    $dataLen = $Samples.Length * 2
    $fs = [System.IO.File]::Create($Path)
    $bw = New-Object System.IO.BinaryWriter($fs)
    try {
        $bw.Write([System.Text.Encoding]::ASCII.GetBytes('RIFF'))
        $bw.Write([int]($dataLen + 36))
        $bw.Write([System.Text.Encoding]::ASCII.GetBytes('WAVE'))
        $bw.Write([System.Text.Encoding]::ASCII.GetBytes('fmt '))
        $bw.Write([int]16)
        $bw.Write([int16]1)            # PCM
        $bw.Write([int16]1)            # mono
        $bw.Write([int]$SampleRate)
        $bw.Write([int]$byteRate)
        $bw.Write([int16]2)            # block align
        $bw.Write([int16]16)           # bits per sample
        $bw.Write([System.Text.Encoding]::ASCII.GetBytes('data'))
        $bw.Write([int]$dataLen)
        foreach ($s in $Samples) {
            $v = [int]([Math]::Max(-1.0, [Math]::Min(1.0, $s)) * 32760)
            $bw.Write([int16]$v)
        }
    } finally {
        $bw.Close()
        $fs.Close()
    }
}

function Bell {
    param([double]$Fund, [double]$Seconds, [int]$SampleRate = 22050)
    # Decaying sum of harmonics with a soft attack
    $n = [int]($Seconds * $SampleRate)
    $samples = New-Object 'double[]' $n
    $attack = [int]($SampleRate * 0.01)
    for ($i = 0; $i -lt $n; $i++) {
        $t = $i / [double]$SampleRate
        $env = [Math]::Exp(-$t * 2.6)
        if ($i -lt $attack) { $env *= ($i / [double]$attack) }
        $v  = [Math]::Sin(2 * [Math]::PI * $Fund * $t)         * 0.6
        $v += [Math]::Sin(2 * [Math]::PI * $Fund * 2.0 * $t)   * 0.25 * [Math]::Exp(-$t * 4.0)
        $v += [Math]::Sin(2 * [Math]::PI * $Fund * 3.01 * $t)  * 0.12 * [Math]::Exp(-$t * 5.5)
        $samples[$i] = $v * $env
    }
    return $samples
}

function PinkNoise {
    param([double]$Seconds, [int]$SampleRate = 22050)
    # Voss-McCartney pink noise approximation
    $n = [int]($Seconds * $SampleRate)
    $samples = New-Object 'double[]' $n
    $r = New-Object Random 42
    $b0 = 0.0; $b1 = 0.0; $b2 = 0.0; $b3 = 0.0; $b4 = 0.0; $b5 = 0.0; $b6 = 0.0
    for ($i = 0; $i -lt $n; $i++) {
        $w = $r.NextDouble() * 2.0 - 1.0
        $b0 = 0.99886 * $b0 + $w * 0.0555179
        $b1 = 0.99332 * $b1 + $w * 0.0750759
        $b2 = 0.96900 * $b2 + $w * 0.1538520
        $b3 = 0.86650 * $b3 + $w * 0.3104856
        $b4 = 0.55000 * $b4 + $w * 0.5329522
        $b5 = -0.7616 * $b5 - $w * 0.0168980
        $pink = $b0 + $b1 + $b2 + $b3 + $b4 + $b5 + $b6 + $w * 0.5362
        $b6 = $w * 0.115926
        $samples[$i] = $pink * 0.11
    }
    return $samples
}

function BrownNoise {
    param([double]$Seconds, [int]$SampleRate = 22050)
    $n = [int]($Seconds * $SampleRate)
    $samples = New-Object 'double[]' $n
    $r = New-Object Random 17
    $last = 0.0
    for ($i = 0; $i -lt $n; $i++) {
        $w = $r.NextDouble() * 2.0 - 1.0
        $last = ($last + (0.02 * $w)) * 0.99
        $samples[$i] = $last * 3.5
    }
    return $samples
}

function BinauralBeat {
    param([double]$Carrier, [double]$Beat, [double]$Seconds, [int]$SampleRate = 22050)
    # Mono "beating" approximation: two close frequencies summed produces a slow amplitude wobble.
    # True binaural requires stereo + headphones; this version is the audible mono cue.
    $n = [int]($Seconds * $SampleRate)
    $samples = New-Object 'double[]' $n
    $fA = $Carrier - $Beat / 2.0
    $fB = $Carrier + $Beat / 2.0
    for ($i = 0; $i -lt $n; $i++) {
        $t = $i / [double]$SampleRate
        $a = [Math]::Sin(2 * [Math]::PI * $fA * $t)
        $b = [Math]::Sin(2 * [Math]::PI * $fB * $t)
        $env = 0.5
        if ($i -lt 4410) { $env *= ($i / 4410.0) }
        if ($i -gt ($n - 4410)) { $env *= (($n - $i) / 4410.0) }
        $samples[$i] = ($a + $b) * 0.5 * $env
    }
    return $samples
}

function VoiceDing {
    param([double]$Seconds = 1.6, [int]$SampleRate = 22050)
    # Stand-in for the spoken "Are you dreaming?" prompt: a two-syllable bell pattern that's recognizable.
    $n = [int]($Seconds * $SampleRate)
    $samples = New-Object 'double[]' $n
    $bell1 = Bell 880.0 0.6
    $bell2 = Bell 660.0 0.7
    $gap = [int](0.35 * $SampleRate)
    for ($i = 0; $i -lt $bell1.Length; $i++) {
        $samples[$i] = $bell1[$i]
    }
    for ($i = 0; $i -lt $bell2.Length; $i++) {
        $idx = $gap + [int](0.35 * $SampleRate) + $i
        if ($idx -ge $n) { break }
        $samples[$idx] = $bell2[$i]
    }
    return $samples
}

Write-Host "Generating bell-soft.wav…"
Write-Wav -Path (Join-Path $OutDir 'bell-soft.wav') -Samples (Bell 587.33 1.6)

Write-Host "Generating bell-tibetan.wav…"
Write-Wav -Path (Join-Path $OutDir 'bell-tibetan.wav') -Samples (Bell 196.0 4.0)

Write-Host "Generating voice-are-you-dreaming.wav…"
Write-Wav -Path (Join-Path $OutDir 'voice-are-you-dreaming.wav') -Samples (VoiceDing)

Write-Host "Generating pink-noise.wav…"
Write-Wav -Path (Join-Path $OutDir 'pink-noise.wav') -Samples (PinkNoise 8.0)

Write-Host "Generating brown-noise.wav…"
Write-Wav -Path (Join-Path $OutDir 'brown-noise.wav') -Samples (BrownNoise 8.0)

Write-Host "Generating binaural-theta.wav…"
Write-Wav -Path (Join-Path $OutDir 'binaural-theta.wav') -Samples (BinauralBeat 200.0 7.0 12.0)

Write-Host "Done."
