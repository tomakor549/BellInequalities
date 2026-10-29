using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BitArena
{
    // Keyboard.current stays empty unless the Game view is focused, and OnGUI can take the
    // key event before the Input System sees it. Listen to both, and keep an OnGUI press
    // until the next Update has had a chance to read it.
    public static class GameInput
    {
        static readonly HashSet<KeyCode> held = new HashSet<KeyCode>();
        static readonly Dictionary<KeyCode, int> pressedOnFrame = new Dictionary<KeyCode, int>();
        static int clickFrame = -1;
        static bool deviceLive;
        static bool installed;
        static InputSettings.EditorInputBehaviorInPlayMode savedEditorBehavior;
        static InputSettings.BackgroundBehavior savedBackground;

        public static void Install()
        {
            if (installed)
                return;
            installed = true;
            var settings = InputSystem.settings;
            savedEditorBehavior = settings.editorInputBehaviorInPlayMode;
            savedBackground = settings.backgroundBehavior;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;

            foreach (var device in InputSystem.devices)
            {
                if (device is Keyboard keyboard && !keyboard.enabled)
                    InputSystem.EnableDevice(keyboard);
            }
        }

        public static void Shutdown()
        {
            if (!installed)
                return;
            installed = false;
            var settings = InputSystem.settings;
            settings.editorInputBehaviorInPlayMode = savedEditorBehavior;
            settings.backgroundBehavior = savedBackground;
            held.Clear();
            pressedOnFrame.Clear();
            clickFrame = -1;
            deviceLive = false;
        }

        public static void Observe(Event evt)
        {
            if (evt == null || deviceLive)
                return;

            if (evt.type == EventType.KeyDown)
            {
                KeyCode code = evt.keyCode != KeyCode.None ? evt.keyCode : FromChar(evt.character);
                if (code == KeyCode.None)
                    return;
                if (!held.Contains(code))
                    pressedOnFrame[code] = Time.frameCount;
                held.Add(code);
            }
            else if (evt.type == EventType.KeyUp)
            {
                KeyCode code = evt.keyCode != KeyCode.None ? evt.keyCode : FromChar(evt.character);
                if (code != KeyCode.None)
                    held.Remove(code);
            }
            else if (evt.type == EventType.MouseDown && evt.button == 0)
            {
                clickFrame = Time.frameCount;
            }
        }

        public static void EndFrame()
        {
            if (pressedOnFrame.Count > 0)
            {
                scratch.Clear();
                foreach (var pair in pressedOnFrame)
                {
                    if (pair.Value < Time.frameCount)
                        scratch.Add(pair.Key);
                }
                for (int i = 0; i < scratch.Count; i++)
                    pressedOnFrame.Remove(scratch[i]);
            }

            if (clickFrame >= 0 && clickFrame < Time.frameCount)
                clickFrame = -1;
        }

        static readonly List<KeyCode> scratch = new List<KeyCode>();

        public static bool Held(KeyCode code)
        {
            if (DeviceHeld(code))
            {
                deviceLive = true;
                return true;
            }
            return !deviceLive && held.Contains(code);
        }

        public static bool Down(KeyCode code)
        {
            if (DeviceDown(code))
            {
                deviceLive = true;
                return true;
            }
            if (deviceLive)
                return false;
            return pressedOnFrame.TryGetValue(code, out int frame) && frame < Time.frameCount;
        }

        public static bool Clicked =>
            (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            || (!deviceLive && clickFrame >= 0 && clickFrame < Time.frameCount);

        static bool DeviceHeld(KeyCode code)
        {
            var key = Map(code);
            if (key == Key.None)
                return false;
            foreach (var device in InputSystem.devices)
            {
                if (device is Keyboard keyboard && keyboard.enabled && keyboard[key].isPressed)
                    return true;
            }
            return false;
        }

        static bool DeviceDown(KeyCode code)
        {
            var key = Map(code);
            if (key == Key.None)
                return false;
            foreach (var device in InputSystem.devices)
            {
                if (device is Keyboard keyboard && keyboard.enabled && keyboard[key].wasPressedThisFrame)
                    return true;
            }
            return false;
        }

        static KeyCode FromChar(char character)
        {
            switch (char.ToLowerInvariant(character))
            {
                case 'a': return KeyCode.A;
                case 'd': return KeyCode.D;
                case 'w': return KeyCode.W;
                case 's': return KeyCode.S;
                case 'j': return KeyCode.J;
                case 'k': return KeyCode.K;
                case 'r': return KeyCode.R;
                case ' ': return KeyCode.Space;
                case '\n':
                case '\r': return KeyCode.Return;
                default: return KeyCode.None;
            }
        }

        static Key Map(KeyCode code)
        {
            switch (code)
            {
                case KeyCode.A: return Key.A;
                case KeyCode.D: return Key.D;
                case KeyCode.W: return Key.W;
                case KeyCode.S: return Key.S;
                case KeyCode.J: return Key.J;
                case KeyCode.K: return Key.K;
                case KeyCode.R: return Key.R;
                case KeyCode.Space: return Key.Space;
                case KeyCode.Escape: return Key.Escape;
                case KeyCode.Return: return Key.Enter;
                case KeyCode.KeypadEnter: return Key.NumpadEnter;
                case KeyCode.LeftArrow: return Key.LeftArrow;
                case KeyCode.RightArrow: return Key.RightArrow;
                case KeyCode.UpArrow: return Key.UpArrow;
                case KeyCode.DownArrow: return Key.DownArrow;
                case KeyCode.LeftControl: return Key.LeftCtrl;
                case KeyCode.RightControl: return Key.RightCtrl;
                default: return Key.None;
            }
        }
    }
}
