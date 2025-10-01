using Midi; // Here and only here!
using System;
using System.Collections.Generic;

namespace instruments
{
    /// <summary>
    /// Abstraction wrapper for all Midi device operations.
    /// </summary>
    internal static class MidiApi
    {
        private static InputDevice activeDevice = null;

        public static bool hasDevice
        {
            get => activeDevice != null;
        }
        public static string deviceName
        {
            get => activeDevice.Name;
        }

        public static void Enumerate(List<string> midiDeviceNames)
        {
            foreach (InputDevice device in InputDevice.InstalledDevices)
            {
                midiDeviceNames.Add(device.Name);
            }
        }
        private static InputDevice Find(string midiDeviceName)
        {
            foreach (InputDevice device in InputDevice.InstalledDevices)
            {
                if (string.Compare(device.Name, midiDeviceName) == 0)
                    return device;
            }

            return null;
        }

        public static bool Activate(string midiDeviceName)
        {
            InputDevice inputDevice = Find(midiDeviceName);
            if (inputDevice == null)
            {
                return false;
            }

            Deactivate();

            activeDevice = inputDevice;
            activeDevice.Open();
            activeDevice.StartReceiving(null); // Receive notes in different thread

            // All the "Midi" messages are delegated through here for two reasons,
            // 1. To simplify and prevent the "game code" from manipulating the input devices directly
            // 2. To be able to switch the underlying library without too much work if necessary

            activeDevice.NoteOn += NoteOnHandler;
            activeDevice.NoteOff += NoteOffHandler;
            activeDevice.ControlChange += ControlChangeHandler;

            return true;
        }
        public static void Deactivate()
        {
            if (activeDevice != null)
            {
                if (activeDevice.IsReceiving)
                    activeDevice.StopReceiving();

                activeDevice.RemoveAllEventHandlers();

                if (activeDevice.IsOpen)
                    activeDevice.Close();
            }
            UnsubscribeAllEvents();
            activeDevice = null;
        }

        public static bool Exists(string midiDeviceName)
        {
            return Find(midiDeviceName) != null;
        }

        

        private static void UnsubcribeAllEvents<T>(ref T eventDelegate) where T : Delegate
        {
            if (eventDelegate == null)
                return;

            foreach (Delegate d in eventDelegate.GetInvocationList())
            {
                eventDelegate = (T)Delegate.Remove(eventDelegate, d);
            }
        }

        private static void UnsubscribeAllEvents()
        {
            UnsubcribeAllEvents(ref onNoteOn);
            UnsubcribeAllEvents(ref onNoteOff);
            UnsubcribeAllEvents(ref onSustainChange);
            UnsubcribeAllEvents(ref onReverbChange);
        }

        public enum Channel
        {
            Channel1,
            Channel2,
            Channel3,
            Channel4,
            Channel5,
            Channel6,
            Channel7,
            Channel8,
            Channel9,
            Channel10,
            Channel11,
            Channel12,
            Channel13,
            Channel14,
            Channel15,
            Channel16
        }
        public enum Pitch
        {
            CNeg1,
            CSharpNeg1,
            DNeg1,
            DSharpNeg1,
            ENeg1,
            FNeg1,
            FSharpNeg1,
            GNeg1,
            GSharpNeg1,
            ANeg1,
            ASharpNeg1,
            BNeg1,
            C0,
            CSharp0,
            D0,
            DSharp0,
            E0,
            F0,
            FSharp0,
            G0,
            GSharp0,
            A0,
            ASharp0,
            B0,
            C1,
            CSharp1,
            D1,
            DSharp1,
            E1,
            F1,
            FSharp1,
            G1,
            GSharp1,
            A1,
            ASharp1,
            B1,
            C2,
            CSharp2,
            D2,
            DSharp2,
            E2,
            F2,
            FSharp2,
            G2,
            GSharp2,
            A2,
            ASharp2,
            B2,
            C3,
            CSharp3,
            D3,
            DSharp3,
            E3,
            F3,
            FSharp3,
            G3,
            GSharp3,
            A3,
            ASharp3,
            B3,
            C4,
            CSharp4,
            D4,
            DSharp4,
            E4,
            F4,
            FSharp4,
            G4,
            GSharp4,
            A4,
            ASharp4,
            B4,
            C5,
            CSharp5,
            D5,
            DSharp5,
            E5,
            F5,
            FSharp5,
            G5,
            GSharp5,
            A5,
            ASharp5,
            B5,
            C6,
            CSharp6,
            D6,
            DSharp6,
            E6,
            F6,
            FSharp6,
            G6,
            GSharp6,
            A6,
            ASharp6,
            B6,
            C7,
            CSharp7,
            D7,
            DSharp7,
            E7,
            F7,
            FSharp7,
            G7,
            GSharp7,
            A7,
            ASharp7,
            B7,
            C8,
            CSharp8,
            D8,
            DSharp8,
            E8,
            F8,
            FSharp8,
            G8,
            GSharp8,
            A8,
            ASharp8,
            B8,
            C9,
            CSharp9,
            D9,
            DSharp9,
            E9,
            F9,
            FSharp9,
            G9
        }

        public delegate void NoteOn(Channel channel, Pitch pitch, byte velocity, float time);
        public delegate void NoteOff(Channel channel, Pitch pitch, byte velocity, float time);
        public delegate void SustainChange(Channel channel, byte value, float time);
        public delegate void ReverbChange(Channel channel, byte value, float time);


        // TODO:
        //  Have these polled and raised from the main thread instead!

        private static void NoteOnHandler(NoteOnMessage message)
        {
            lock (activeDevice)
            {
                onNoteOn?.Invoke((Channel)message.Channel, (Pitch)message.Pitch, (byte)message.Velocity, message.Time);
            }
        }
        private static void NoteOffHandler(NoteOffMessage message)
        {
            lock (activeDevice)
            {
                onNoteOff?.Invoke((Channel)message.Channel, (Pitch)message.Pitch, (byte)message.Velocity, message.Time);
            }
        }
        private static void ControlChangeHandler(ControlChangeMessage message)
        {
            switch (message.Control)
            {
                case Control.SustainPedal:
                    changeSustainValue();
                    break;
                case Control.ReverbLevel:
                    changeReverbValue();
                    break;
                default:
                    break;
            }

            void changeSustainValue()
            {
                lock (activeDevice)
                {
                    onSustainChange?.Invoke((Channel)message.Channel, (byte)message.Value, message.Time);
                }
            }

            void changeReverbValue()
            {
                lock (activeDevice)
                {
                    onReverbChange?.Invoke((Channel)message.Channel, (byte)message.Value, message.Time);
                }
            }
        }


        public static event NoteOn onNoteOn;
        public static event NoteOff onNoteOff;
        public static event SustainChange onSustainChange;
        public static event ReverbChange onReverbChange;

        public static float PitchToFrequency(Pitch pitch)
        {
            switch (pitch)
            {
                case Pitch.CNeg1: return 8.1758f;
                case Pitch.CSharpNeg1: return 8.66196f;
                case Pitch.DNeg1: return 9.17702f;
                case Pitch.DSharpNeg1: return 9.72272f;
                case Pitch.ENeg1: return 10.3009f;
                case Pitch.FNeg1: return 10.9134f;
                case Pitch.FSharpNeg1: return 11.5623f;
                case Pitch.GNeg1: return 12.2499f;
                case Pitch.GSharpNeg1: return 12.9783f;
                case Pitch.ANeg1: return 13.75f;
                case Pitch.ASharpNeg1: return 14.5676f;
                case Pitch.BNeg1: return 15.4339f;

                case Pitch.C0: return 16.3516f;
                case Pitch.CSharp0: return 17.3239f;
                case Pitch.D0: return 18.3540f;
                case Pitch.DSharp0: return 19.4454f;
                case Pitch.E0: return 20.6017f;
                case Pitch.F0: return 21.8268f;
                case Pitch.FSharp0: return 23.1247f;
                case Pitch.G0: return 24.4997f;
                case Pitch.GSharp0: return 25.9565f;
                case Pitch.A0: return 27.5f;
                case Pitch.ASharp0: return 29.1352f;
                case Pitch.B0: return 30.8677f;

                case Pitch.C1: return 32.7032f;
                case Pitch.CSharp1: return 34.6478f;
                case Pitch.D1: return 36.7081f;
                case Pitch.DSharp1: return 38.8909f;
                case Pitch.E1: return 41.2034f;
                case Pitch.F1: return 43.6535f;
                case Pitch.FSharp1: return 46.2493f;
                case Pitch.G1: return 48.9994f;
                case Pitch.GSharp1: return 51.9131f;
                case Pitch.A1: return 55f;
                case Pitch.ASharp1: return 58.2705f;
                case Pitch.B1: return 61.7354f;

                case Pitch.C2: return 65.4064f;
                case Pitch.CSharp2: return 69.2957f;
                case Pitch.D2: return 73.4162f;
                case Pitch.DSharp2: return 77.7817f;
                case Pitch.E2: return 82.4069f;
                case Pitch.F2: return 87.3071f;
                case Pitch.FSharp2: return 92.4986f;
                case Pitch.G2: return 97.9989f;
                case Pitch.GSharp2: return 103.826f;
                case Pitch.A2: return 110f;
                case Pitch.ASharp2: return 116.541f;
                case Pitch.B2: return 123.471f;

                case Pitch.C3: return 130.813f;
                case Pitch.CSharp3: return 138.591f;
                case Pitch.D3: return 146.832f;
                case Pitch.DSharp3: return 155.563f;
                case Pitch.E3: return 164.814f;
                case Pitch.F3: return 174.614f;
                case Pitch.FSharp3: return 184.997f;
                case Pitch.G3: return 195.998f;
                case Pitch.GSharp3: return 207.652f;
                case Pitch.A3: return 220f;
                case Pitch.ASharp3: return 233.082f;
                case Pitch.B3: return 246.942f;

                case Pitch.C4: return 261.626f;
                case Pitch.CSharp4: return 277.183f;
                case Pitch.D4: return 293.665f;
                case Pitch.DSharp4: return 311.127f;
                case Pitch.E4: return 329.628f;
                case Pitch.F4: return 349.228f;
                case Pitch.FSharp4: return 369.994f;
                case Pitch.G4: return 391.995f;
                case Pitch.GSharp4: return 415.305f;
                case Pitch.A4: return 440f;
                case Pitch.ASharp4: return 466.164f;
                case Pitch.B4: return 493.883f;

                case Pitch.C5: return 523.251f;
                case Pitch.CSharp5: return 554.365f;
                case Pitch.D5: return 587.330f;
                case Pitch.DSharp5: return 622.254f;
                case Pitch.E5: return 659.255f;
                case Pitch.F5: return 698.456f;
                case Pitch.FSharp5: return 739.989f;
                case Pitch.G5: return 783.991f;
                case Pitch.GSharp5: return 830.609f;
                case Pitch.A5: return 880f;
                case Pitch.ASharp5: return 932.328f;
                case Pitch.B5: return 987.767f;

                case Pitch.C6: return 1046.50f;
                case Pitch.CSharp6: return 1108.73f;
                case Pitch.D6: return 1174.66f;
                case Pitch.DSharp6: return 1244.51f;
                case Pitch.E6: return 1318.51f;
                case Pitch.F6: return 1396.91f;
                case Pitch.FSharp6: return 1479.98f;
                case Pitch.G6: return 1567.98f;
                case Pitch.GSharp6: return 1661.22f;
                case Pitch.A6: return 1760f;
                case Pitch.ASharp6: return 1864.66f;
                case Pitch.B6: return 1975.53f;

                case Pitch.C7: return 2093f;
                case Pitch.CSharp7: return 2217.46f;
                case Pitch.D7: return 2349.32f;
                case Pitch.DSharp7: return 2489.02f;
                case Pitch.E7: return 2637.02f;
                case Pitch.F7: return 2793.83f;
                case Pitch.FSharp7: return 2959.96f;
                case Pitch.G7: return 3135.96f;
                case Pitch.GSharp7: return 3322.44f;
                case Pitch.A7: return 3520f;
                case Pitch.ASharp7: return 3729.31f;
                case Pitch.B7: return 3951.07f;

                case Pitch.C8: return 4186.01f;
                case Pitch.CSharp8: return 4434.92f;
                case Pitch.D8: return 4698.64f;
                case Pitch.DSharp8: return 4978.03f;
                case Pitch.E8: return 5274.04f;
                case Pitch.F8: return 5587.65f;
                case Pitch.FSharp8: return 5919.91f;
                case Pitch.G8: return 6271.93f;
                case Pitch.GSharp8: return 6644.88f;
                case Pitch.A8: return 7040f;
                case Pitch.ASharp8: return 7458.62f;
                case Pitch.B8: return 7902.13f;

                case Pitch.C9: return 8372.02f;
                case Pitch.CSharp9: return 8869.84f;
                case Pitch.D9: return 9397.27f;
                case Pitch.DSharp9: return 9956.06f;
                case Pitch.E9: return 10548.1f;
                case Pitch.F9: return 11175.3f;
                case Pitch.FSharp9: return 11839.8f;
                case Pitch.G9: return 12543.9f;
            }
            return 0f; // fallback
        }
        public static float ToFrequency(this Pitch pitch)
        {
            return PitchToFrequency(pitch);
        }
    }
}
