using System;

namespace FiveM_AntiCheat_Executor.Features
{
    public class Noclip
    {
        private MemoryManager _memory;
        private bool _enabled = false;
        private float _speed = 2.0f;
        private Random _random = new Random();
        private DateTime _lastCollisionToggle = DateTime.Now;

        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                if (_enabled)
                    Enable();
                else
                    Disable();
            }
        }

        public float Speed
        {
            get => _speed;
            set => _speed = Math.Max(0.5f, Math.Min(10.0f, value));
        }

        public Noclip(MemoryManager memory)
        {
            _memory = memory;
        }

        private void Enable()
        {
            try
            {
                var worldPtr = IntPtr.Add(Offsets.BaseAddress, Offsets.WorldPtr);
                var playerBase = _memory.GetAddressFromPointerPath(worldPtr, new int[] { 0x8 });

                if (playerBase != IntPtr.Zero)
                {
                    var collisionAddr = IntPtr.Add(playerBase, Offsets.CollisionFlag);
                    _memory.WriteInt(collisionAddr, 0);
                    
                    var godModeAddr = IntPtr.Add(playerBase, Offsets.GodModeFlag);
                    _memory.WriteLong(godModeAddr, 1);
                }
            }
            catch { }
        }

        private void Disable()
        {
            try
            {
                var worldPtr = IntPtr.Add(Offsets.BaseAddress, Offsets.WorldPtr);
                var playerBase = _memory.GetAddressFromPointerPath(worldPtr, new int[] { 0x8 });

                if (playerBase != IntPtr.Zero)
                {
                    var collisionAddr = IntPtr.Add(playerBase, Offsets.CollisionFlag);
                    _memory.WriteInt(collisionAddr, 1);
                    
                    var godModeAddr = IntPtr.Add(playerBase, Offsets.GodModeFlag);
                    _memory.WriteLong(godModeAddr, 0);
                }
            }
            catch { }
        }

        public void Update()
        {
            if (!_enabled) return;

            if ((DateTime.Now - _lastCollisionToggle).TotalMilliseconds > 100)
            {
                _lastCollisionToggle = DateTime.Now;
                
                var worldPtr = IntPtr.Add(Offsets.BaseAddress, Offsets.WorldPtr);
                var playerBase = _memory.GetAddressFromPointerPath(worldPtr, new int[] { 0x8 });

                if (playerBase != IntPtr.Zero)
                {
                    var collisionAddr = IntPtr.Add(playerBase, Offsets.CollisionFlag);
                    _memory.WriteInt(collisionAddr, 0);
                }
            }
        }

        public void MoveForward()
        {
            if (!_enabled) return;
            MoveInDirection(0, _speed);
        }

        public void MoveBackward()
        {
            if (!_enabled) return;
            MoveInDirection(0, -_speed);
        }

        public void MoveUp()
        {
            if (!_enabled) return;

            try
            {
                var worldPtr = IntPtr.Add(Offsets.BaseAddress, Offsets.WorldPtr);
                var playerBase = _memory.GetAddressFromPointerPath(worldPtr, new int[] { 0x8 });

                if (playerBase != IntPtr.Zero)
                {
                    var coordsBase = IntPtr.Add(playerBase, Offsets.PlayerCoords);
                    float z = _memory.ReadFloat(IntPtr.Add(coordsBase, 8));
                    _memory.WriteFloat(IntPtr.Add(coordsBase, 8), z + _speed);
                }
            }
            catch { }
        }

        public void MoveDown()
        {
            if (!_enabled) return;

            try
            {
                var worldPtr = IntPtr.Add(Offsets.BaseAddress, Offsets.WorldPtr);
                var playerBase = _memory.GetAddressFromPointerPath(worldPtr, new int[] { 0x8 });

                if (playerBase != IntPtr.Zero)
                {
                    var coordsBase = IntPtr.Add(playerBase, Offsets.PlayerCoords);
                    float z = _memory.ReadFloat(IntPtr.Add(coordsBase, 8));
                    _memory.WriteFloat(IntPtr.Add(coordsBase, 8), z - _speed);
                }
            }
            catch { }
        }

        private void MoveInDirection(float headingOffset, float distance)
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
                    float heading = _memory.ReadFloat(headingAddr) + headingOffset;

                    double radians = heading * (Math.PI / 180.0);

                    float newX = x + (float)(Math.Sin(-radians) * distance);
                    float newY = y + (float)(Math.Cos(-radians) * distance);

                    _memory.WriteFloat(coordsBase, newX);
                    _memory.WriteFloat(IntPtr.Add(coordsBase, 4), newY);
                }
            }
            catch { }
        }
    }
}
