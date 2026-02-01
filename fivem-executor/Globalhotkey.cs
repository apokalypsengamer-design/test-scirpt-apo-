using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace FiveM_AntiCheat_Executor
{
    public class GlobalHotkey
    {
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private const int VK_INSERT = 0x2D;
        private const int VK_HOME = 0x24;
        private const int VK_END = 0x23;

        private bool _running = true;
        private Action<bool> _onToggle;
        private bool _menuState = true;

        public GlobalHotkey(Action<bool> onToggle)
        {
            _onToggle = onToggle;
        }

        public void Start()
        {
            Thread thread = new Thread(HotkeyLoop);
            thread.IsBackground = true;
            thread.Start();
        }

        public void Stop()
        {
            _running = false;
        }

        private void HotkeyLoop()
        {
            bool wasPressed = false;

            while (_running)
            {
                bool isPressed = (GetAsyncKeyState(VK_INSERT) & 0x8000) != 0;

                if (isPressed && !wasPressed)
                {
                    _menuState = !_menuState;
                    _onToggle?.Invoke(_menuState);

                    PlayToggleSound();
                    
                    if (_menuState)
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("[*] Menu OPENED (INSERT)");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("[*] Menu CLOSED (INSERT)");
                    }
                    Console.ResetColor();
                }

                wasPressed = isPressed;
                Thread.Sleep(50);
            }
        }

        private void PlayToggleSound()
        {
            try
            {
                Console.Beep(800, 100);
            }
            catch { }
        }
    }
}
