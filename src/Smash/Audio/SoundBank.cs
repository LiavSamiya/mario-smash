using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace Smash
{
    enum Sfx
    {
        Jump,
        DoubleJump,
        HitLight,
        HitHeavy,
        ShieldHit,
        ShieldBreak,
        Clank,
        KO,
        MenuMove,
        MenuSelect,
        Countdown,
        Go,
        Game,
        LedgeGrab,
    }

    enum MusicTrack
    {
        Title,
        Battle,
        Select,
    }

    //all sound effects and the music loop are synthesised in code, so the game ships no audio files
    class SoundBank
    {
        const int Rate = 22050;

        public static readonly SoundBank Silent = new SoundBank();

        const float MusicBase = 0.3f;

        readonly Dictionary<Sfx, SoundEffect> effects = new Dictionary<Sfx, SoundEffect>();
        readonly Dictionary<MusicTrack, SoundEffectInstance> tracks = new Dictionary<MusicTrack, SoundEffectInstance>();
        SoundEffectInstance music;
        readonly Random rng = new Random(1);
        bool muted;
        float musicVolume = 0.7f, sfxVolume = 0.8f;

        public bool Enabled { get; private set; }

        public bool Muted
        {
            get => muted;
            set
            {
                muted = value;
                ApplyMusicVolume();
            }
        }

        //0 to 1
        public float MusicVolume
        {
            get => musicVolume;
            set
            {
                musicVolume = MathHelper.Clamp(value, 0, 1);
                ApplyMusicVolume();
            }
        }

        //0 to 1
        public float SfxVolume
        {
            get => sfxVolume;
            set => sfxVolume = MathHelper.Clamp(value, 0, 1);
        }

        void ApplyMusicVolume()
        {
            if (music != null)
                music.Volume = muted ? 0 : MusicBase * musicVolume;
        }

        SoundBank() { }

        //creates the sounds, or returns a silent bank if there is no audio device
        public static SoundBank Create()
        {
            var bank = new SoundBank();
            try
            {
                bank.Build();
                bank.Enabled = true;
            }
            catch (Exception e) when (e is NoAudioHardwareException || e is InvalidOperationException || e is TypeInitializationException || e is DllNotFoundException)
            {
                bank.effects.Clear();
            }
            return bank;
        }

        public void Play(Sfx sfx, float volume = 0.6f)
        {
            if (!Enabled || muted || !effects.TryGetValue(sfx, out SoundEffect effect))
                return;
            float v = volume * sfxVolume;
            if (v > 0)
                effect.Play(Math.Min(1, v), 0, 0);
        }

        //switches to a music track (does nothing if it is already playing)
        public void PlayMusic(MusicTrack track = MusicTrack.Title)
        {
            if (!Enabled || !tracks.TryGetValue(track, out SoundEffectInstance next))
                return;
            if (next == music && music.State == SoundState.Playing)
                return;
            music?.Stop();
            music = next;
            ApplyMusicVolume();
            music.Play();
        }

        public void StopMusic() => music?.Stop();

        #region synthesis
        void Build()
        {
            effects[Sfx.Jump] = Make(Mix(Hop(0.16, 190, 560, 0.55), Noise(0.03, 0.12)));
            effects[Sfx.DoubleJump] = Make(Mix(Hop(0.18, 280, 840, 0.5), Delay(Hop(0.08, 1400, 1900, 0.15), 0.1)));
            effects[Sfx.LedgeGrab] = Make(Mix(Noise(0.04, 0.35), Hop(0.06, 300, 220, 0.35)));
            effects[Sfx.HitLight] = Make(Mix(Noise(0.07, 0.5), Sweep(0.07, 220, 90, Square, 0.4)));
            effects[Sfx.HitHeavy] = Make(Mix(Noise(0.2, 0.7), Sweep(0.2, 160, 45, Square, 0.6)));
            effects[Sfx.ShieldHit] = Make(Sweep(0.08, 1100, 900, Triangle, 0.5));
            effects[Sfx.ShieldBreak] = Make(Mix(Noise(0.4, 0.5), Sweep(0.4, 900, 120, Square, 0.4)));
            effects[Sfx.Clank] = Make(Mix(Sweep(0.12, 1500, 1400, Square, 0.3), Sweep(0.12, 2250, 2100, Triangle, 0.3)));
            effects[Sfx.KO] = Make(Mix(Noise(0.7, 0.8), Sweep(0.7, 600, 40, Square, 0.5)));
            effects[Sfx.MenuMove] = Make(Sweep(0.04, 700, 700, Square, 0.25));
            effects[Sfx.MenuSelect] = Make(Concat(Sweep(0.06, 520, 520, Square, 0.3), Sweep(0.1, 1040, 1040, Square, 0.3)));
            effects[Sfx.Countdown] = Make(Sweep(0.15, 600, 600, Square, 0.35));
            effects[Sfx.Go] = Make(Sweep(0.35, 900, 900, Square, 0.35));
            effects[Sfx.Game] = Make(Concat(Sweep(0.15, 523, 523, Square, 0.3), Sweep(0.15, 659, 659, Square, 0.3), Sweep(0.4, 784, 784, Square, 0.3)));

            tracks[MusicTrack.Title] = Loop(TitleMusic());
            tracks[MusicTrack.Battle] = Loop(BattleMusic());
            tracks[MusicTrack.Select] = Loop(SelectMusic());
        }

        static SoundEffectInstance Loop(float[] samples)
        {
            SoundEffectInstance instance = Make(samples).CreateInstance();
            instance.IsLooped = true;
            return instance;
        }

        //a soft sine "hop": the pitch rises quickly (exponentially) and the sound fades out smoothly
        static float[] Hop(double seconds, double from, double to, double volume)
        {
            int n = (int)(seconds * Rate);
            var s = new float[n];
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                double t = i / (double)n;
                double freq = from * Math.Pow(to / from, Math.Sqrt(t));
                phase += freq / Rate;
                double env = Math.Min(1, i / 40.0) * Math.Pow(1 - t, 1.6);
                double wave = Math.Sin(phase * Math.PI * 2) + 0.25 * Math.Sin(phase * Math.PI * 4);
                s[i] = (float)(wave * env * volume);
            }
            return s;
        }

        static float[] Delay(float[] samples, double seconds)
        {
            int d = (int)(seconds * Rate);
            var s = new float[samples.Length + d];
            Array.Copy(samples, 0, s, d, samples.Length);
            return s;
        }

        static double Square(double phase) => phase % 1 < 0.5 ? 1 : -1;
        static double Triangle(double phase) => 4 * Math.Abs(phase % 1 - 0.5) - 1;

        //a tone gliding from one frequency to another with a quick attack and a linear fade out
        static float[] Sweep(double seconds, double from, double to, Func<double, double> wave, double volume)
        {
            int n = (int)(seconds * Rate);
            var s = new float[n];
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                double t = i / (double)n;
                phase += (from + (to - from) * t) / Rate;
                double env = Math.Min(1, i / 60.0) * (1 - t);
                s[i] = (float)(wave(phase) * env * volume);
            }
            return s;
        }

        float[] Noise(double seconds, double volume)
        {
            int n = (int)(seconds * Rate);
            var s = new float[n];
            for (int i = 0; i < n; i++)
            {
                double env = 1 - i / (double)n;
                s[i] = (float)((rng.NextDouble() * 2 - 1) * env * env * volume);
            }
            return s;
        }

        static float[] Mix(params float[][] parts)
        {
            int n = 0;
            foreach (float[] p in parts) n = Math.Max(n, p.Length);
            var s = new float[n];
            foreach (float[] p in parts)
                for (int i = 0; i < p.Length; i++)
                    s[i] += p[i];
            return s;
        }

        static float[] Concat(params float[][] parts)
        {
            var list = new List<float>();
            foreach (float[] p in parts) list.AddRange(p);
            return list.ToArray();
        }

        //NES style pulse wave; a narrow duty cycle sounds thin and bright, 50% is a plain square
        static Func<double, double> Pulse(double duty) => phase => phase % 1 < duty ? 1 : -1;

        //a chord: root note (MIDI) and whether it is minor
        struct Chord
        {
            public int Root;
            public bool Minor;
            public Chord(int root, bool minor) { Root = root; Minor = minor; }
            public int[] Notes => new[] { Root, Root + (Minor ? 3 : 4), Root + 7 };
        }

        //plays a melody written as one entry per step: a MIDI note, -1 = keep holding the previous note, 0 = rest
        static void AddMelody(float[] s, int[] notes, double stepSeconds, double startSeconds, Func<double, double> wave, double volume, double vibrato)
        {
            for (int i = 0; i < notes.Length; i++)
            {
                if (notes[i] <= 0)
                    continue;
                int steps = 1;
                while (i + steps < notes.Length && notes[i + steps] == -1)
                    steps++;
                AddLead(s, (int)((startSeconds + i * stepSeconds) * Rate), (int)(steps * stepSeconds * Rate * 0.95), Freq(notes[i]), wave, volume, vibrato);
            }
        }

        //a lead note with a quick attack, a slight decay and vibrato that fades in on long notes
        static void AddLead(float[] s, int start, int length, double freq, Func<double, double> wave, double volume, double vibrato)
        {
            double phase = 0;
            for (int i = 0; i < length && start + i < s.Length; i++)
            {
                double t = i / (double)Rate;
                double depth = vibrato * Math.Min(1, Math.Max(0, t - 0.12) * 4);
                phase += freq * (1 + depth * Math.Sin(t * 2 * Math.PI * 6)) / Rate;
                double env = Math.Min(1, i / 60.0) * (0.75 + 0.25 * Math.Exp(-t * 6)) * Math.Min(1, (length - i) / 200.0);
                s[start + i] += (float)(wave(phase) * env * volume);
            }
        }

        //a long soft note for pads and held bass (slow attack and release)
        static void AddPad(float[] s, int start, int length, double freq, Func<double, double> wave, double volume)
        {
            for (int i = 0; i < length && start + i < s.Length; i++)
            {
                double env = Math.Min(1, i / (Rate * 0.25)) * Math.Min(1, (length - i) / (Rate * 0.3));
                s[start + i] += (float)(wave(freq * i / Rate) * env * volume);
            }
        }

        //a crash cymbal: long bright noise
        static void AddCrash(float[] s, int start, Random noise)
        {
            int n = (int)(1.2 * Rate);
            double last = 0;
            for (int i = 0; i < n && start + i < s.Length; i++)
            {
                double w = noise.NextDouble() * 2 - 1;
                s[start + i] += (float)((w - last) * 0.18 * Math.Exp(-i / (double)n * 4));
                last = w;
            }
        }

        //notes that ring past the end of the loop are mixed into its start, so the loop has no gap
        static float[] Seamless(float[] s, double loopSeconds)
        {
            int n = (int)(loopSeconds * Rate);
            var loop = new float[n];
            for (int i = 0; i < s.Length; i++)
                loop[i % n] += s[i];
            return loop;
        }

        static float[] Normalize(float[] s)
        {
            float max = 0;
            foreach (float v in s) max = Math.Max(max, Math.Abs(v));
            if (max > 0.9f)
                for (int i = 0; i < s.Length; i++) s[i] *= 0.9f / max;
            return s;
        }

        //the match theme: an original, hype 8-bit fighting track in A minor at 160 BPM.
        //octave-jumping pulse bass, a sixteenth-note arpeggio, noise drums and a pulse lead with vibrato.
        //bars 1-8 are the verse, bars 9-16 climb to a big chorus, then it loops
        internal static float[] BattleMusic()
        {
            const double bpm = 160;
            double eighth = 60 / bpm / 2, sixteenth = eighth / 2;
            var noise = new Random(11);
            Chord Am = new Chord(57, true), F = new Chord(53, false), G = new Chord(55, false), Em = new Chord(52, true), E = new Chord(52, false), C = new Chord(48, false);
            Chord[] chords = { Am, F, G, Em, Am, F, G, E, F, G, Am, Am, F, G, E, E };
            int[] melody =
            {
                76, 76, -1, 72, 74, -1, 76, -1,   77, -1, 76, 74, 72, -1, 69, -1,
                71, -1, 74, -1, 79, -1, 77, 76,   76, -1, -1, -1, 71, 72, 74, -1,
                76, 76, -1, 72, 74, -1, 76, 79,   81, -1, 79, 77, 76, -1, 72, -1,
                74, -1, 76, -1, 77, -1, 79, -1,   80, -1, -1, -1, 76, -1, -1, -1,
                81, -1, 79, 77, 76, -1, 77, -1,   79, -1, 77, 76, 74, -1, 71, -1,
                72, 74, 76, 79, 81, -1, 84, -1,   81, -1, -1, -1, -1, -1, 0, 0,
                77, 77, -1, 81, -1, 84, -1, 81,   83, -1, 81, 79, -1, 77, 79, -1,
                80, -1, 83, -1, 88, -1, -1, -1,   86, 84, 83, 80, -1, -1, 0, 0,
            };
            var s = new float[(int)(chords.Length * 16 * sixteenth * Rate) + Rate / 4];
            for (int bar = 0; bar < chords.Length; bar++)
            {
                int[] chord = chords[bar].Notes;
                int root = chords[bar].Root - 12;
                bool chorus = bar >= 8;
                for (int k = 0; k < 16; k++)
                {
                    int start = (int)((bar * 16 + k) * sixteenth * Rate);
                    int len16 = (int)(sixteenth * Rate);
                    //drums: kick on the beat plus a push before beat 3, snare on 2 and 4, hats on every eighth
                    if (k % 4 == 0 || k == 7 || (chorus && k == 14)) AddKick(s, start);
                    if (k == 4 || k == 12) AddSnare(s, start, noise);
                    if (chorus && k == 15) AddSnare(s, start, noise);
                    if (k % 2 == 0) AddHat(s, start, noise, k % 4 == 2 ? 0.1 : 0.05);
                    //octave-jumping bass on every eighth note
                    if (k % 2 == 0)
                        AddNote(s, start, len16 * 2, Freq(k % 4 == 0 ? root : root + 12), Pulse(0.5), 0.13);
                    //fast arpeggio, brighter in the chorus
                    AddNote(s, start, len16, Freq(chord[k % 3] + 12), Pulse(0.125), chorus ? 0.06 : 0.045);
                }
                if (bar == 0 || bar == 8)
                    AddCrash(s, (int)(bar * 16 * sixteenth * Rate), noise);
            }
            AddMelody(s, melody, eighth, 0, Pulse(0.25), 0.17, 0.012);
            //a quieter harmony an octave below in the chorus
            int[] harmony = melody.Select((n, i) => i >= 64 && n > 0 ? n - 12 : (i >= 64 ? n : 0)).ToArray();
            AddMelody(s, harmony, eighth, 0, Triangle, 0.1, 0);
            return Normalize(Seamless(s, chords.Length * 16 * sixteenth));
        }

        //the opening theme (title and menus): an original, inspirational track in C major at 96 BPM.
        //it starts with soft pads and a heartbeat bass, the melody enters in bar 5, and drums and crashes
        //lift it in bars 9-16 like a hero's theme, then it loops
        internal static float[] TitleMusic()
        {
            const double bpm = 96;
            double beat = 60 / bpm, eighth = beat / 2;
            var noise = new Random(5);
            Chord C = new Chord(60, false), G = new Chord(55, false), Am = new Chord(57, true), F = new Chord(53, false);
            Chord[] chords = { C, G, Am, F, C, G, F, F, Am, F, C, G, F, G, C, C };
            //one entry per beat (4 per bar); the melody starts in bar 5
            int[] melody =
            {
                0, 0, 0, 0,   0, 0, 0, 0,   0, 0, 0, 0,   0, 0, 0, 0,
                72, -1, 76, 79,   79, -1, -1, 74,   77, 76, 74, 72,   74, -1, -1, -1,
                76, -1, 72, 76,   77, -1, 81, -1,   79, -1, 76, 72,   74, -1, -1, 79,
                81, -1, 79, 77,   79, -1, 83, -1,   84, -1, -1, -1,   84, -1, -1, 0,
            };
            var s = new float[(int)(chords.Length * 4 * beat * Rate) + Rate];
            for (int bar = 0; bar < chords.Length; bar++)
            {
                int[] chord = chords[bar].Notes;
                int barStart = (int)(bar * 4 * beat * Rate);
                int barLength = (int)(4 * beat * Rate);
                //warm pad chord held for the whole bar
                foreach (int n in chord)
                    AddPad(s, barStart, barLength, Freq(n), Triangle, 0.07);
                //bass on beats 1 and 3
                AddPad(s, barStart, barLength / 2, Freq(chords[bar].Root - 24), Triangle, 0.16);
                AddPad(s, barStart + barLength / 2, barLength / 2, Freq(chords[bar].Root - 24), Triangle, 0.16);
                //a rising broken chord from bar 3, getting stronger
                if (bar >= 2)
                {
                    int[] broken = { chord[0], chord[1], chord[2], chord[0] + 12, chord[2], chord[1], chord[2], chord[0] + 12 };
                    for (int k = 0; k < 8; k++)
                        AddNote(s, barStart + (int)(k * eighth * Rate), (int)(eighth * Rate), Freq(broken[k] + 12), Pulse(0.5), bar >= 8 ? 0.05 : 0.035);
                }
                //drums join in the second half: kick on 1 and 3, then snare on 2 and 4
                if (bar >= 8)
                {
                    for (int b = 0; b < 4; b++)
                    {
                        int start = barStart + (int)(b * beat * Rate);
                        if (b % 2 == 0) AddKick(s, start);
                        if (bar >= 12 && b % 2 == 1) AddSnare(s, start, noise);
                        AddHat(s, start + (int)(eighth * Rate), noise, 0.05);
                    }
                }
                if (bar == 8 || bar == 12)
                    AddCrash(s, barStart, noise);
            }
            AddMelody(s, melody, beat, 0, Pulse(0.25), 0.16, 0.015);
            //the last phrase doubled an octave up, for a bright finish
            int[] sparkle = melody.Select((n, i) => i >= 48 && n > 0 ? n + 12 : (i >= 48 ? n : 0)).ToArray();
            AddMelody(s, sparkle, beat, 0, Pulse(0.125), 0.05, 0.015);
            return Normalize(Seamless(s, chords.Length * 4 * beat));
        }

        //a driving fighting-game style loop for the character select screen (inspired by arcade fighters like Tekken):
        //four-on-the-floor kick, snare on 2 and 4, sixteenth hi-hats, a distorted minor bass riff, synth stabs and a lead
        internal static float[] SelectMusic()
        {
            const double bpm = 146;
            double step = 60 / bpm / 4;
            const int bars = 8;
            var s = new float[(int)(bars * 16 * step * Rate) + Rate / 2];
            var noise = new Random(7);

            //E minor riff, one note per sixteenth
            int[] riff = { 40, 40, 52, 40, 40, 50, 40, 47, 40, 40, 52, 40, 43, 45, 47, 50 };
            //the riff moves with the chords: Em Em C D Em Em Am B
            int[] shift = { 0, 0, -4, -2, 0, 0, 5, 7 };
            bool[] minor = { true, true, false, false, true, true, true, false };
            //lead melody for the second half, one note per eighth (0 = rest)
            int[] lead = { 76, 0, 79, 76, 74, 0, 71, 74, 76, 0, 79, 83, 81, 79, 76, 74 };

            for (int bar = 0; bar < bars; bar++)
            {
                for (int k = 0; k < 16; k++)
                {
                    int start = (int)((bar * 16 + k) * step * Rate);
                    if (k % 4 == 0) AddKick(s, start);
                    if (k == 4 || k == 12) AddSnare(s, start, noise);
                    AddHat(s, start, noise, k % 4 == 2 ? 0.12 : 0.06);
                    AddSaw(s, start, (int)(step * Rate * 0.9), Freq(riff[k] + shift[bar]), 0.22);
                    //syncopated chord stabs
                    if (k == 0 || k == 3 || k == 6 || k == 10)
                    {
                        int root = 64 + shift[bar];
                        foreach (int n in new[] { root, root + (minor[bar] ? 3 : 4), root + 7 })
                            AddNote(s, start, (int)(step * Rate * 1.5), Freq(n), Square, 0.05);
                    }
                    if (bar >= 4 && k % 2 == 0 && lead[(bar % 2) * 8 + k / 2] > 0)
                        AddNote(s, start, (int)(step * Rate * 1.9), Freq(lead[(bar % 2) * 8 + k / 2] + shift[bar] - 12), Triangle, 0.14);
                }
            }
            return Normalize(Seamless(s, bars * 16 * step));
        }

        static void AddKick(float[] s, int start)
        {
            int n = (int)(0.18 * Rate);
            double phase = 0;
            for (int i = 0; i < n && start + i < s.Length; i++)
            {
                double t = i / (double)n;
                phase += (45 + 110 * Math.Exp(-t * 18)) / Rate;
                s[start + i] += (float)(Math.Sin(phase * Math.PI * 2) * Math.Exp(-t * 5) * 0.6);
            }
        }

        static void AddSnare(float[] s, int start, Random noise)
        {
            int n = (int)(0.16 * Rate);
            for (int i = 0; i < n && start + i < s.Length; i++)
            {
                double t = i / (double)n;
                double env = Math.Exp(-t * 7);
                s[start + i] += (float)(((noise.NextDouble() * 2 - 1) * 0.35 + Math.Sin(i * 190.0 / Rate * Math.PI * 2) * 0.2) * env);
            }
        }

        static void AddHat(float[] s, int start, Random noise, double volume)
        {
            int n = (int)(0.035 * Rate);
            double last = 0;
            for (int i = 0; i < n && start + i < s.Length; i++)
            {
                double w = noise.NextDouble() * 2 - 1;
                //the difference of two noise samples keeps only the high frequencies
                s[start + i] += (float)((w - last) * 0.5 * volume * (1 - i / (double)n));
                last = w;
            }
        }

        //a distorted sawtooth bass note
        static void AddSaw(float[] s, int start, int length, double freq, double volume)
        {
            for (int i = 0; i < length && start + i < s.Length; i++)
            {
                double t = i / (double)length;
                double saw = freq * i / Rate % 1 * 2 - 1;
                double env = Math.Min(1, i / 30.0) * (1 - t * 0.6);
                s[start + i] += (float)(Math.Tanh(saw * 2.5) * env * volume);
            }
        }

        static double Freq(int midi) => 440 * Math.Pow(2, (midi - 69) / 12.0);

        static void AddNote(float[] s, int start, int length, double freq, Func<double, double> wave, double volume)
        {
            for (int i = 0; i < length && start + i < s.Length; i++)
            {
                double t = i / (double)length;
                double env = Math.Min(1, i / 80.0) * (1 - t * 0.7);
                s[start + i] += (float)(wave(freq * i / Rate) * env * volume);
            }
        }

        static SoundEffect Make(float[] samples)
        {
            var bytes = new byte[samples.Length * 2];
            for (int i = 0; i < samples.Length; i++)
            {
                short v = (short)(Math.Clamp(samples[i], -1f, 1f) * short.MaxValue);
                bytes[i * 2] = (byte)(v & 0xff);
                bytes[i * 2 + 1] = (byte)((v >> 8) & 0xff);
            }
            return new SoundEffect(bytes, Rate, AudioChannels.Mono);
        }
        #endregion
    }
}
