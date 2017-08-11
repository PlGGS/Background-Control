using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

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

    internal sealed class Controller
    {
        [DllImport("winmm.dll")]
        internal static extern int joyGetPosEx(int uJoyID, ref JOYINFOEX pji); //Get the state of a controller with their ID
        [DllImport("winmm.dll")]
        public static extern Int32 joyGetNumDevs(); //How many controllers are plugged in
        
        private int volCombo;
        private int briCombo;
        private JOYINFOEX state = new JOYINFOEX();

        public Controller(int vc, int bc)
        {
            volCombo = vc;
            briCombo = bc;
            state.dwFlags = 128;
            state.dwSize = Marshal.SizeOf(typeof(JOYINFOEX));
        }

        public bool VolComboPressed()
        {
            for (int i = 0; i < joyGetNumDevs(); i++)
            {
                joyGetPosEx(i, ref state);
                if (volCombo == state.dwButtons)
                {
                    Console.WriteLine($"combo: {volCombo} state.dwButtons: {state.dwButtons}");
                    return true;
                }
            }
            return false;
        }

        public bool BriComboPressed()
        {
            for (int i = 0; i < joyGetNumDevs(); i++)
            {
                joyGetPosEx(i, ref state);
                if (briCombo == state.dwButtons)
                {
                    Console.WriteLine($"combo: {briCombo} state.dwButtons: {state.dwButtons}");
                    return true;
                }
            }
            return false;
        }
    }

    internal sealed class BackgroundControl
    {
        [STAThread]
        private static void Main()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Controller controller;
            Stopwatch timer = new Stopwatch();
            float time = 1;
            int volumeCombo = 0;
            int brightnessCombo = 0;
            string volumeString = "4+5"; //TODO find out why this continues to call after buttons are no longer pressed
            string brightnessString = "6+7";
            int oVal = 0;

            foreach (var b in volumeString.Split('+')) //Find Button Combo that is required
            {
                if (int.TryParse(b, out oVal))
                {
                    volumeCombo += (int)Math.Pow(2, oVal);
                }
                oVal = 0;
            }

            foreach (var b in brightnessString.Split('+')) //Find Button Combo that is required
            {
                if (int.TryParse(b, out oVal))
                {
                    brightnessCombo += (int)Math.Pow(2, oVal);
                }
                oVal = 0;
            }

            Console.WriteLine($"volumeCombo: {volumeCombo} | brightnessCombo: {brightnessCombo}");
            controller = new Controller(volumeCombo, brightnessCombo); //Controller class that handles button presses when checked

            while (true)
            {
                if (!controller.VolComboPressed() && !controller.BriComboPressed())
                {
                    timer.Restart();
                }
                else if (timer.ElapsedMilliseconds >= time)
                {
                    Console.WriteLine("combo pressed");
                    //return;
                }

                System.Threading.Thread.Sleep(35);
            }
        }
    }
}
