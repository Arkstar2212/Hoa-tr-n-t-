using System;
using System.Collections.Generic;
using UnityEngine;

namespace HoaVaMau.Gameplay
{
    public enum GameAction
    {
        MoveLeft,
        MoveRight,
        MoveUp,
        MoveDown,
        Jump,
        Sprint,
        Crouch,
        Interact,
        RopeGrab
    }

    /// <summary>
    /// Centralized Keybinding System for "Hoa trên Đất Đỏ".
    /// Supports:
    /// - Rebinding for all character locomotion & interaction actions.
    /// - Clean persistence via PlayerPrefs.
    /// - Secondary key fallbacks (e.g. Arrow keys for directional movement).
    /// - Clean Event triggers when bindings change.
    /// </summary>
    public static class KeybindingManager
    {
        public static event Action OnKeybindingsChanged;

        private static readonly Dictionary<GameAction, KeyCode> DefaultBindings = new Dictionary<GameAction, KeyCode>
        {
            { GameAction.MoveLeft, KeyCode.A },
            { GameAction.MoveRight, KeyCode.D },
            { GameAction.MoveUp, KeyCode.W },
            { GameAction.MoveDown, KeyCode.S },
            { GameAction.Jump, KeyCode.Space },
            { GameAction.Sprint, KeyCode.LeftShift },
            { GameAction.Crouch, KeyCode.C },
            { GameAction.Interact, KeyCode.E },
            { GameAction.RopeGrab, KeyCode.F }
        };

        private static readonly Dictionary<GameAction, KeyCode> SecondaryBindings = new Dictionary<GameAction, KeyCode>
        {
            { GameAction.MoveLeft, KeyCode.LeftArrow },
            { GameAction.MoveRight, KeyCode.RightArrow },
            { GameAction.MoveUp, KeyCode.UpArrow },
            { GameAction.MoveDown, KeyCode.DownArrow },
            { GameAction.Jump, KeyCode.None },
            { GameAction.Sprint, KeyCode.RightShift },
            { GameAction.Crouch, KeyCode.LeftControl },
            { GameAction.Interact, KeyCode.None },
            { GameAction.RopeGrab, KeyCode.None }
        };

        private static readonly Dictionary<GameAction, KeyCode> CurrentBindings = new Dictionary<GameAction, KeyCode>();

        static KeybindingManager()
        {
            LoadBindings();
        }

        public static void LoadBindings()
        {
            CurrentBindings.Clear();
            foreach (var kvp in DefaultBindings)
            {
                string prefKey = "Keybind_" + kvp.Key.ToString();
                if (PlayerPrefs.HasKey(prefKey))
                {
                    string keyName = PlayerPrefs.GetString(prefKey);
                    if (Enum.TryParse(keyName, out KeyCode savedKey))
                    {
                        CurrentBindings[kvp.Key] = savedKey;
                        continue;
                    }
                }
                CurrentBindings[kvp.Key] = kvp.Value;
            }
        }

        public static void SaveBindings()
        {
            foreach (var kvp in CurrentBindings)
            {
                PlayerPrefs.SetString("Keybind_" + kvp.Key.ToString(), kvp.Value.ToString());
            }
            PlayerPrefs.Save();
            OnKeybindingsChanged?.Invoke();
        }

        public static KeyCode GetKey(GameAction action)
        {
            if (CurrentBindings.TryGetValue(action, out KeyCode key))
            {
                return key;
            }
            return DefaultBindings[action];
        }

        public static void SetKey(GameAction action, KeyCode newKey)
        {
            CurrentBindings[action] = newKey;
            SaveBindings();
        }

        public static void ResetDefaults()
        {
            CurrentBindings.Clear();
            foreach (var kvp in DefaultBindings)
            {
                CurrentBindings[kvp.Key] = kvp.Value;
                PlayerPrefs.DeleteKey("Keybind_" + kvp.Key.ToString());
            }
            PlayerPrefs.Save();
            OnKeybindingsChanged?.Invoke();
        }

        public static bool IsActionPressed(GameAction action)
        {
            KeyCode primary = GetKey(action);
            if (primary != KeyCode.None && Input.GetKeyDown(primary)) return true;

            if (SecondaryBindings.TryGetValue(action, out KeyCode sec) && sec != KeyCode.None)
            {
                if (Input.GetKeyDown(sec)) return true;
            }
            return false;
        }

        public static bool IsActionHeld(GameAction action)
        {
            KeyCode primary = GetKey(action);
            if (primary != KeyCode.None && Input.GetKey(primary)) return true;

            if (SecondaryBindings.TryGetValue(action, out KeyCode sec) && sec != KeyCode.None)
            {
                if (Input.GetKey(sec)) return true;
            }
            return false;
        }

        public static bool IsActionUp(GameAction action)
        {
            KeyCode primary = GetKey(action);
            if (primary != KeyCode.None && Input.GetKeyUp(primary)) return true;

            if (SecondaryBindings.TryGetValue(action, out KeyCode sec) && sec != KeyCode.None)
            {
                if (Input.GetKeyUp(sec)) return true;
            }
            return false;
        }

        public static string GetActionDisplayName(GameAction action)
        {
            switch (action)
            {
                case GameAction.MoveLeft: return "Di chuyển Trái";
                case GameAction.MoveRight: return "Di chuyển Phải";
                case GameAction.MoveUp: return "Trèo / Leo lên";
                case GameAction.MoveDown: return "Cúi / Bò trườn";
                case GameAction.Jump: return "Nhảy cao";
                case GameAction.Sprint: return "Chạy nước rút";
                case GameAction.Crouch: return "Ngồi xổm";
                case GameAction.Interact: return "Tương tác / Nhặt đồ";
                case GameAction.RopeGrab: return "Bám / Thả dây";
                default: return action.ToString();
            }
        }

        public static string GetKeyDisplayName(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.LeftShift: return "L-Shift";
                case KeyCode.RightShift: return "R-Shift";
                case KeyCode.LeftControl: return "L-Ctrl";
                case KeyCode.RightControl: return "R-Ctrl";
                case KeyCode.Space: return "Phím Cách (Space)";
                case KeyCode.Return: return "Enter";
                case KeyCode.Escape: return "Esc";
                case KeyCode.LeftArrow: return "Mũi tên Trái";
                case KeyCode.RightArrow: return "Mũi tên Phải";
                case KeyCode.UpArrow: return "Mũi tên Lên";
                case KeyCode.DownArrow: return "Mũi tên Xuống";
                case KeyCode.None: return "Chưa gán";
                default: return key.ToString();
            }
        }
    }
}
