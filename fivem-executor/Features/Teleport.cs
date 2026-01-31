using System;

namespace FiveM_AntiCheat_Executor.Features
{
    public class Teleport
    {
        private MemoryManager _memory;

        public Teleport(MemoryManager memory)
        {
            _memory = memory;
        }

        public void TeleportToCoordinates(float x, float y, float z)
        {
            try
            {
                var worldPtr = IntPtr.Add(Offsets.BaseAddress, Offsets.WorldPtr);
                var playerBase = _memory.GetAddressFromPointerPath(worldPtr, new int[] { 0x8 });

                if (playerBase != IntPtr.Zero)
                {
                    var coordsBase = IntPtr.Add(playerBase, Offsets.PlayerCoords);

                    _memory.WriteFloat(coordsBase, x);
                    _memory.WriteFloat(IntPtr.Add(coordsBase, 4), y);
                    _memory.WriteFloat(IntPtr.Add(coordsBase, 8), z);
                }
            }
            catch { }
        }

        public void TeleportForward(float distance)
        {
            try
            {
                var worldPtr = IntPtr.Add(Offsets.BaseAddress, Offsets.WorldPtr);
                var playerBase = _memory.GetAddressFromPointerPath(worldPtr, new int[] { 0x8 });

                if (playerBase != IntPtr.Zero)
                {
                    var coordsBase = IntPtr.Add(playerBase, Offsets.PlayerCoords);
                    var headingAddr = IntPtr.Add(playerBase, Offsets.Heading);

                    float x = _memory.ReadFloat(coordsBase);
                    float y = _memory.ReadFloat(IntPtr.Add(coordsBase, 4));
                    float z = _memory.ReadFloat(IntPtr.Add(coordsBase, 8));
                    float heading = _memory.ReadFloat(headingAddr);

                    double radians = heading * (Math.PI / 180.0);

                    float newX = x + (float)(Math.Sin(-radians) * distance);
                    float newY = y + (float)(Math.Cos(-radians) * distance);

                    _memory.WriteFloat(coordsBase, newX);
                    _memory.WriteFloat(IntPtr.Add(coordsBase, 4), newY);
                }
            }
            catch { }
        }

        public (float x, float y, float z) GetCurrentPosition()
        {
            try
            {
                var worldPtr = IntPtr.Add(Offsets.BaseAddress, Offsets.WorldPtr);
                var playerBase = _memory.GetAddressFromPointerPath(worldPtr, new int[] { 0x8 });

                if (playerBase != IntPtr.Zero)
                {
                    var coordsBase = IntPtr.Add(playerBase, Offsets.PlayerCoords);

                    float x = _memory.ReadFloat(coordsBase);
                    float y = _memory.ReadFloat(IntPtr.Add(coordsBase, 4));
                    float z = _memory.ReadFloat(IntPtr.Add(coordsBase, 8));

                    return (x, y, z);
                }
            }
            catch { }

            return (0, 0, 0);
        }
    }
}
