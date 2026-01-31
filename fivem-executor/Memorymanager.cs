using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace FiveM_AntiCheat_Executor
{
    public class MemoryManager
    {
        private Process _process;
        private IntPtr _processHandle;

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);

        private const int PROCESS_ALL_ACCESS = 0x1F0FFF;

        public MemoryManager(Process process)
        {
            _process = process;
            _processHandle = OpenProcess(PROCESS_ALL_ACCESS, false, process.Id);
        }

        public IntPtr GetModuleBaseAddress(string moduleName)
        {
            foreach (ProcessModule module in _process.Modules)
            {
                if (module.ModuleName.Equals(moduleName, StringComparison.OrdinalIgnoreCase))
                {
                    return module.BaseAddress;
                }
            }
            return IntPtr.Zero;
        }

        public byte[] ReadBytes(IntPtr address, int size)
        {
            byte[] buffer = new byte[size];
            int bytesRead = 0;
            ReadProcessMemory(_processHandle, address, buffer, size, ref bytesRead);
            return buffer;
        }

        public float ReadFloat(IntPtr address)
        {
            byte[] buffer = ReadBytes(address, 4);
            return BitConverter.ToSingle(buffer, 0);
        }

        public int ReadInt(IntPtr address)
        {
            byte[] buffer = ReadBytes(address, 4);
            return BitConverter.ToInt32(buffer, 0);
        }

        public long ReadLong(IntPtr address)
        {
            byte[] buffer = ReadBytes(address, 8);
            return BitConverter.ToInt64(buffer, 0);
        }

        public IntPtr ReadPointer(IntPtr address)
        {
            byte[] buffer = ReadBytes(address, IntPtr.Size);
            return (IntPtr)BitConverter.ToInt64(buffer, 0);
        }

        public bool WriteBytes(IntPtr address, byte[] value)
        {
            int bytesWritten;
            return WriteProcessMemory(_processHandle, address, value, value.Length, out bytesWritten);
        }

        public bool WriteFloat(IntPtr address, float value)
        {
            return WriteBytes(address, BitConverter.GetBytes(value));
        }

        public bool WriteInt(IntPtr address, int value)
        {
            return WriteBytes(address, BitConverter.GetBytes(value));
        }

        public bool WriteLong(IntPtr address, long value)
        {
            return WriteBytes(address, BitConverter.GetBytes(value));
        }

        public IntPtr GetAddressFromPointerPath(IntPtr baseAddress, int[] offsets)
        {
            IntPtr address = baseAddress;
            
            foreach (int offset in offsets)
            {
                address = ReadPointer(address);
                if (address == IntPtr.Zero)
                    return IntPtr.Zero;
                address = IntPtr.Add(address, offset);
            }
            
            return address;
        }

        ~MemoryManager()
        {
            if (_processHandle != IntPtr.Zero)
                CloseHandle(_processHandle);
        }
    }
}
