﻿using System;
using System.ComponentModel;
using System.Threading;
using System.Windows.Forms;
using WhiteMagic.WinAPI;
using WhiteMagic.WinAPI.Structures.Input;

namespace WhiteMagic.Input
{
    public class WindowKeyboardInput : IKeyboardInput
    {
        public IntPtr Handle { get; }
        public bool Recursive { get; set; }

        public WindowKeyboardInput(IntPtr Handle, bool Recursive = true)
        {
            this.Handle = Handle;
            this.Recursive = Recursive;
        }

        private static void SendKeyToWindow(IntPtr Window, Keys Key, bool Up, bool Recursive = false)
        {
            var lParam = 0u;
            lParam |= Up ? 1u : 0;

            var scanCode = User32.MapVirtualKey((uint)Key, MapVirtualKeyMapTypes.MAPVK_VK_TO_VSC);
            lParam |= (uint)scanCode << 16;

            if (Up)
            {
                lParam |= 1u << 30;
                lParam |= 1u << 31;
            }

            if (!User32.PostMessage(Window, Up ? WM.KEYUP : WM.KEYDOWN, (uint)Key, lParam))
                throw new Win32Exception();

            if (Recursive)
            {
                User32.EnumChildWindows(Window, (IntPtr hwnd, IntPtr param) =>
                {
                    SendKeyToWindow(hwnd, Key, Up, false);
                    return true;
                }, IntPtr.Zero);
            }
        }

        public override void SendKey(Keys Key, bool Up = false)
        {
            SendKeyToWindow(Handle, Key, Up, Recursive);
        }

        public override void KeyPress(Keys Key, TimeSpan KeyPressTime)
        {
            SendKeyToWindow(Handle, Key, false, Recursive);
            if (!KeypressTime.IsEmpty())
                Thread.Sleep((int)KeypressTime.TotalMilliseconds);
            SendKeyToWindow(Handle, Key, true, Recursive);
        }

        public override void SendChar(char c)
        {
            if (!User32.PostMessage(Handle, WM.CHAR, Convert.ToUInt32(c), 0))
                throw new Win32Exception();
        }
    }
}
