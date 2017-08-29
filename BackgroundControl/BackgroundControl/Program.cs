using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace BackgroundControl
{
    [StructLayout(LayoutKind.Sequential)]
    public struct JOYINFOEX
    {
        public int dwSize;
        public int dwFlags;
        public int dwXpos;
        public int dwYpos;
        public int dwZpos;
        public int dwRpos;
        public int dwUpos;
        public int dwVpos;
        public int dwButtons;
        public int dwButtonNumber;
        public int dwPOV;
        public int dwReserved1;
        public int dwReserved2;
    }

    internal sealed class BackgroundControl
    {

        /// <summary>
        /// Loops quickly without killing the CPU
        /// From StackOverflow: https://stackoverflow.com/questions/7402146/cpu-friendly-infinite-loop
        /// </summary>
        [STAThread]
        private static void Main() //TODO figure out why this is failing after a few minutes
        {
            Console.WriteLine($"Volume Up: In on left stick | Volume Down: In on right stick\n" +
                $"Play/Pause: Y | Last Song: Back | Next Song: Start");
            EventWaitHandle waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, "CF2D4313-33DE-489D-9721-6AFF69841DEA", out bool createdNew);
            bool signaled = false;

            if (!createdNew)
            {
                waitHandle.Set();
                return;
            }
            
            var timer = new Timer(OnTimerElapsed, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(150));

            while (!signaled)
            {
                signaled = waitHandle.WaitOne(200);
            }
        }

        private static void OnTimerElapsed(object state)
        {
            ControlCombos control = new ControlCombos();
        }
    }

    internal sealed class ControlCombos
    {
        [DllImport("user32", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32")]
        static extern IntPtr GetConsoleWindow();

        const UInt32 WM_APPCOMMAND = 0x0319;
        const UInt32 APPCOMMAND_VOLUME_DOWN = 9;
        const UInt32 APPCOMMAND_VOLUME_UP = 10;
        const UInt32 APPCOMMAND_PLAY_PAUSE = 14;
        const UInt32 APPCOMMAND_LAST_SONG = 12;
        const UInt32 APPCOMMAND_NEXT_SONG = 11;

        public ControlCombos()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Controller controller;
            Stopwatch timer = new Stopwatch();
            int[] combos = { 0, 0, 0, 0, 0, 0 }; //vol up, vol down, pause/play, next song, last song
            string[] comboStrings = { "4+5+8", "4+5+9", "4+5+3", "4+5+6", "4+5+7", "4+5+2" };
            int combo = 0;
            
            foreach (string item in comboStrings)
            {
                foreach (string button in item.Split('+')) //Find Button Combo that is required
                {
                    if (int.TryParse(button, out int oVal))
                    {
                        combos[combo] += (int)Math.Pow(2, oVal);
                    }
                }
                combo += 1;
            }
            
            controller = new Controller(combos);

            if (controller.ComboPressed() == -1)
            {
                timer.Restart();
            }
            else if (controller.ComboPressed() == combos[0])
            {
                SendMessage(GetConsoleWindow(), WM_APPCOMMAND, GetConsoleWindow(), new IntPtr(APPCOMMAND_VOLUME_UP << 16));
                Console.WriteLine("volume up");
                return;
            }
            else if (controller.ComboPressed() == combos[1])
            {
                SendMessage(GetConsoleWindow(), WM_APPCOMMAND, GetConsoleWindow(), new IntPtr(APPCOMMAND_VOLUME_DOWN << 16));
                Console.WriteLine("volume down");
                return;
            }
            else if (controller.ComboPressed() == combos[2])
            {
                SendMessage(GetConsoleWindow(), WM_APPCOMMAND, GetConsoleWindow(), new IntPtr(APPCOMMAND_PLAY_PAUSE << 16));
                Console.WriteLine("play/pause");
                return;
            }
            else if (controller.ComboPressed() == combos[3])
            {
                SendMessage(GetConsoleWindow(), WM_APPCOMMAND, GetConsoleWindow(), new IntPtr(APPCOMMAND_LAST_SONG << 16));
                Console.WriteLine("last");
                return;
            }
            else if (controller.ComboPressed() == combos[4])
            {
                SendMessage(GetConsoleWindow(), WM_APPCOMMAND, GetConsoleWindow(), new IntPtr(APPCOMMAND_NEXT_SONG << 16));
                Console.WriteLine("next");
                return;
            }
            else if (controller.ComboPressed() == combos[5])
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"Volume Up: In on left stick | Volume Down: In on right stick\n" +
                $"Play/Pause: Y | Last Song: Back | Next Song: Start");
                return;
            }
            else
            {
                Console.WriteLine(controller.ComboPressed());
            }

            return;
        }
    }

    internal sealed class Controller
    {
        [DllImport("winmm.dll")]
        internal static extern int joyGetPosEx(int uJoyID, ref JOYINFOEX pji); //Get the state of a controller with their ID
        [DllImport("winmm.dll")]
        public static extern Int32 joyGetNumDevs(); //How many controllers are plugged in

        int[] combos;
        private JOYINFOEX state = new JOYINFOEX();

        public Controller(int[] c)
        {
            combos = c;
            state.dwFlags = 128;
            state.dwSize = Marshal.SizeOf(typeof(JOYINFOEX));
        }
        
        public int ComboPressed()
        {
            try
            {
                for (int controller = 0; controller < joyGetNumDevs(); controller++)
                {
                    joyGetPosEx(controller, ref state);
                    for (int combo = 0; combo < 6; combo++)
                    {
                        if (combos[combo] == state.dwButtons)
                        {
                            return combos[combo];
                        }
                    }
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Failed to run command. Data was corrupt");
            }

            return -1;
        }
    }
}
