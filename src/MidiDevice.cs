using Midi; // Here and only here!
using System;
using System.Collections.Generic;

namespace instruments
{
    /// <summary>
    /// Abstraction wrapper for all Midi device operations.
    /// </summary>
    internal class MidiDevice
    {
        private static InputDevice activeDevice = null;

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
            activeDevice = null;

            // TODO:
            // Is there a better way than this to unhook all the events? (including possible lambdas and such?)
            if (onNoteOn!=null)
            {
                Delegate[] onNoteOnInvocationList = onNoteOn.GetInvocationList();
                    foreach (Delegate noteOnCallback in onNoteOnInvocationList)
                        onNoteOn -= (NoteOn)noteOnCallback;
            }

            if (onNoteOff != null)
            {
                Delegate[] onNoteOffInvocationList = onNoteOff.GetInvocationList();
                    foreach (Delegate noteOffCallback in onNoteOffInvocationList)
                        onNoteOff -= (NoteOff)noteOffCallback;
            }
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

        public delegate void NoteOn(Channel channel, Pitch pitch, int velocity, float time);
        public delegate void NoteOff(Channel channel, Pitch pitch, int velocity, float time);


        // TODO:
        //  Have these polled and raised from the main thread instead!

        private static void NoteOnHandler(NoteOnMessage message)
        {
            lock (activeDevice)
            {
                onNoteOn?.Invoke((Channel)message.Channel, (Pitch)message.Pitch, message.Velocity, message.Time);
            }
        }
        private static void NoteOffHandler(NoteOffMessage message)
        {
            lock (activeDevice)
            {
                onNoteOff?.Invoke((Channel)message.Channel, (Pitch)message.Pitch, message.Velocity, message.Time);
            }
        }


        public static event NoteOn onNoteOn;
        public static event NoteOff onNoteOff;

        public static bool PitchToNoteFrequency(Pitch pitch, out NoteFrequency frequency)
        {
            // TODO: This is currently just a workaround for how the
            // notes are defined by the `NoteFrequency` in `noteMap`.
            // investigate this further, maybe we can just move octave
            // by shifting a pitch or something? (Some sound person may know)
            if (pitch < Pitch.A3 || pitch > Pitch.A5)
            {
                frequency = default;
                return false;
            }

            int index = (int)pitch - (int)Pitch.A3;
            frequency = Definitions.GetInstance().GetFrequency(index);
            return true;
        }
    }
}
