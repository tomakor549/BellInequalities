using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace BitArena
{
    // Keyboard.current stays empty unless the Game view is focused, and OnGUI can take the
    // key event before the Input System sees it. The results table does this to Enter while
    // timeScale is 0, so OnGUI presses stay armed even after the device has already worked.
    public static class GameInput
    {
        static readonly HashSet<KeyCode> held = new HashSet<KeyCode>();
        static readonly Dictionary<KeyCode, int> pressedOnFrame = new Dictionary<KeyCode, int>();
        static readonly Dictionary<KeyCode, int> deviceDownOnFrame = new Dictionary<KeyCode, int>();
        static readonly Dictionary<KeyCode, bool> deviceHeld = new Dictionary<KeyCode, bool>();
        static readonly HashSet<KeyCode> latched = new HashSet<KeyCode>();
        static int clickFrame = -1;
        static bool deviceLive;
        static bool installed;
        static bool acceptEdge;
        static bool restartEdge;
        static InputAction acceptAction;
        static InputAction restartAction;
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

            InputSystem.onEvent += OnInput;
            ArmMenuActions();
        }

        static void ArmMenuActions()
        {
            acceptAction = new InputAction("Accept", InputActionType.Button);
            acceptAction.AddBinding("<Keyboard>/enter");
            acceptAction.AddBinding("<Keyboard>/numpadEnter");
            acceptAction.AddBinding("<Keyboard>/space");
            acceptAction.performed += _ => acceptEdge = true;
            acceptAction.Enable();

            restartAction = new InputAction("Restart", InputActionType.Button);
            restartAction.AddBinding("<Keyboard>/r");
            restartAction.performed += _ => restartEdge = true;
            restartAction.Enable();
        }

        public static void Shutdown()
        {
            if (!installed)
                return;
            installed = false;
            InputSystem.onEvent -= OnInput;
            DisposeAction(ref acceptAction);
            DisposeAction(ref restartAction);
            acceptEdge = false;
            restartEdge = false;
            var settings = InputSystem.settings;
            settings.editorInputBehaviorInPlayMode = savedEditorBehavior;
            settings.backgroundBehavior = savedBackground;
            held.Clear();
            pressedOnFrame.Clear();
            deviceDownOnFrame.Clear();
            deviceHeld.Clear();
            latched.Clear();
            clickFrame = -1;
            deviceLive = false;
        }

        static void OnInput(InputEventPtr eventPtr, InputDevice device)
        {
            if (device is not Keyboard keyboard)
                return;
            foreach (var control in eventPtr.GetAllButtonPresses())
            {
                if (control == keyboard.enterKey)
                    latched.Add(KeyCode.Return);
                else if (control == keyboard.numpadEnterKey)
                    latched.Add(KeyCode.KeypadEnter);
                else if (control == keyboard.spaceKey)
                    latched.Add(KeyCode.Space);
                else if (control == keyboard.rKey)
                    latched.Add(KeyCode.R);
                else if (control == keyboard.escapeKey)
                    latched.Add(KeyCode.Escape);
            }
        }

        public static void Observe(Event evt)
        {
            if (evt == null)
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

            if (deviceDownOnFrame.Count > 0)
            {
                scratch.Clear();
                foreach (var pair in deviceDownOnFrame)
                {
                    if (pair.Value < Time.frameCount)
                        scratch.Add(pair.Key);
                }
                for (int i = 0; i < scratch.Count; i++)
                    deviceDownOnFrame.Remove(scratch[i]);
            }
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
            if (latched.Remove(code))
            {
                deviceLive = true;
                deviceHeld[code] = true;
                deviceDownOnFrame[code] = Time.frameCount;
                pressedOnFrame.Remove(code);
                return true;
            }

            if (DeviceDown(code))
            {
                deviceLive = true;
                deviceDownOnFrame[code] = Time.frameCount;
                pressedOnFrame.Remove(code);
                return true;
            }

            if (!pressedOnFrame.TryGetValue(code, out int frame) || frame >= Time.frameCount)
                return false;
            pressedOnFrame.Remove(code);
            return !deviceDownOnFrame.TryGetValue(code, out int deviceFrame) || deviceFrame != frame;
        }

        public static bool Clicked =>
            (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            || (clickFrame >= 0 && clickFrame < Time.frameCount);

        public static bool ConsumeAccept()
        {
            bool value = acceptEdge;
            acceptEdge = false;
            return value;
        }

        public static bool ConsumeRestart()
        {
            bool value = restartEdge;
            restartEdge = false;
            return value;
        }

        static void DisposeAction(ref InputAction action)
        {
            if (action == null)
                return;
            action.Disable();
            action.Dispose();
            action = null;
        }

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
            bool pressed = DeviceHeld(code);
            deviceHeld.TryGetValue(code, out bool wasHeld);
            deviceHeld[code] = pressed;
            if (pressed && !wasHeld)
                return true;

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
