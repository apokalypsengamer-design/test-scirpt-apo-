using System;
using System.Threading;
using System.Diagnostics;

namespace FiveM_AntiCheat_Executor
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "FiveM AntiCheat Executor";
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(@"
    ╔═══════════════════════════════════════╗
    ║          Apo´s EXECUTOR               ║
    ║            v1.0.0                     ║
    ╚═══════════════════════════════════════╝
            ");
            Console.ResetColor();

            var processName = "FiveM";
            Process targetProcess = null;

            while (targetProcess == null)
            {
                Console.WriteLine("[*] Searching for FiveM process...");
                var processes = Process.GetProcessesByName(processName);
                
                if (processes.Length > 0)
                {
                    targetProcess = processes[0];
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"[+] FiveM process found! PID: {targetProcess.Id}");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("[!] FiveM not found. Waiting...");
                    Console.ResetColor();
                    Thread.Sleep(2000);
                }
            }

            var memory = new MemoryManager(targetProcess);
            var overlay = new OverlayWindow(memory);

            Console.WriteLine("[*] Starting overlay...");
            overlay.Start();
        }
    }
}
