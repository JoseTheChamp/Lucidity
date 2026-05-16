using LucidDreamCompanion.Data.Entities;

namespace LucidDreamCompanion.Services;

/// <summary>One audio cue belonging to a technique template.</summary>
public record TechniqueCueSpec(
    string Label,
    CueTriggerMode Mode,
    TimeSpan? Offset,
    string AudioKey,
    int VolumePercent,
    int FadeInSeconds,
    int LoopCount);

/// <summary>Static metadata for a lucid-dreaming technique — its guide plus the cues it seeds.</summary>
public record TechniqueDefinition(
    string Key,
    string Name,
    string Tagline,
    string Overview,
    string[] Steps,
    string[] PairedTags,
    TechniqueCueSpec[] Cues);

public static class TechniqueDefinitions
{
    public const string BellTibetan = "Audio/bell-tibetan.wav";
    public const string BellSoft    = "Audio/bell-soft.wav";
    public const string Voice       = "Audio/voice-are-you-dreaming.wav";
    public const string PinkNoise   = "Audio/pink-noise.wav";

    public static readonly IReadOnlyList<TechniqueDefinition> All = new[]
    {
        new TechniqueDefinition(
            "WBTB", "WBTB", "Wake Back To Bed",
            "Wake after about five hours of sleep, stay up 20–30 minutes reading lucid-dreaming content, then return to bed holding the intention to become lucid. The simplest, most reliable starting point.",
            new[]
            {
                "Sleep normally for roughly five hours.",
                "When the cue wakes you, actually get out of bed.",
                "Stay up 20–30 minutes — read or think about lucid dreaming.",
                "Return to bed holding the intention to recognise you're dreaming.",
                "Let yourself drift back to sleep with your mind on lucidity.",
            },
            new[] { "WBTB", "lucid" },
            new[]
            {
                new TechniqueCueSpec("Wake for WBTB", CueTriggerMode.SleepRelative,
                    TimeSpan.FromHours(5), BellTibetan, 50, 8, 1),
            }),

        new TechniqueDefinition(
            "MILD", "MILD", "Mnemonic Induction",
            "No audio cues — MILD is a purely mental technique. As you fall asleep you rehearse the intention to notice the next time you're dreaming. Gentle; pairs well with journal reminders.",
            new[]
            {
                "Recall a recent dream in as much detail as you can.",
                "Identify a dream sign in it — something that couldn't happen awake.",
                "Visualise being back in that dream and becoming lucid at the dream sign.",
                "Repeat the mantra: “Next time I'm dreaming, I'll remember I'm dreaming.”",
                "Fall asleep with that intention held softly in mind.",
            },
            new[] { "MILD", "recurring" },
            Array.Empty<TechniqueCueSpec>()),

        new TechniqueDefinition(
            "FILD", "FILD", "Finger Induced Lucid Dream",
            "Wake briefly, then make tiny finger movements as you fall back asleep. The micro-movement keeps a thread of awareness alive across the transition into a dream.",
            new[]
            {
                "When the cue wakes you, stay still and keep your eyes closed.",
                "Gently move your index and middle fingers as if pressing two piano keys — barely move them.",
                "Continue the micro-movements for about 20 seconds as you drift off.",
                "Do a reality check — try to push a finger through your opposite palm.",
                "If it passes through, you're dreaming — stay calm and stabilise the dream.",
            },
            new[] { "FILD", "false awakening" },
            new[]
            {
                new TechniqueCueSpec("Move your fingers", CueTriggerMode.SleepRelative,
                    new TimeSpan(4, 30, 0), Voice, 30, 5, 1),
                new TechniqueCueSpec("Reality check", CueTriggerMode.SleepRelative,
                    new TimeSpan(4, 40, 0), BellSoft, 30, 5, 1),
            }),

        new TechniqueDefinition(
            "WILD", "WILD", "Wake Initiated Lucid Dream",
            "Pass directly from waking into a dream while keeping awareness intact. Advanced — best attempted at a REM peak after several hours of sleep.",
            new[]
            {
                "When the cue plays, lie completely still, ideally on your back.",
                "Let your body fall asleep while your mind stays quietly aware.",
                "Observe the hypnagogic imagery without grabbing at it.",
                "Hold a thin thread of awareness as the imagery forms a scene.",
                "Step into the forming dream — you're now lucid.",
            },
            new[] { "lucid", "false awakening" },
            new[]
            {
                new TechniqueCueSpec("Pink-noise window", CueTriggerMode.SleepRelative,
                    TimeSpan.FromHours(6), PinkNoise, 20, 5, 150),
            }),

        new TechniqueDefinition(
            "SSILD", "SSILD", "Senses Initiated Lucid Dream",
            "Cycle your attention through sight, sound and body sensation to prime the mind for lucidity, then let yourself sleep.",
            new[]
            {
                "When the cue sounds, wake gently and stay still.",
                "Focus on what you see behind closed eyelids for a few seconds.",
                "Shift your focus to the sounds around you.",
                "Shift your focus to the sensations in your body.",
                "Repeat the sight → sound → body cycle about 20 times, then let yourself sleep.",
            },
            new[] { "lucid", "recurring" },
            new[]
            {
                new TechniqueCueSpec("SSILD cue", CueTriggerMode.SleepRelative,
                    new TimeSpan(4, 30, 0), BellSoft, 40, 5, 1),
            }),
    };

    public static TechniqueDefinition? ByKey(string key)
        => All.FirstOrDefault(t => string.Equals(t.Key, key, StringComparison.OrdinalIgnoreCase));
}
