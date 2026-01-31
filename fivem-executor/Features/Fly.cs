using System;

namespace FiveM_AntiCheat_Executor.Features
{
    public class Fly
    {
        private MemoryManager _memory;
        private bool _enabled = false;
        private float _speed = 5.0f;
        private Random _random = new Random();
        private DateTime _lastUpdate = DateTime.Now;

        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                if (!_enabled)
                    Disable();
            }
        }

        public float Speed
        {
            get => _speed;
            set => _speed = Math.Max(1.0f, Math.Min(20.0f, value));
        }

        public Fly(MemoryManager memory)
        {
            _memory = memory;
        }

        private void Disable()
        {
            try
            {
                var worldPtr = IntPtr.Add(Offsets.BaseAddress, Offsets.WorldPtr);
                var playerBase = _memory.GetAddressFromPointerPath(worldPtr, new int[] { 0x8 });

                if (playerBase != IntPtr.Zero)
                {
                    var velocityAddr = IntPtr.Add(playerBase, Offsets.Velocity);
                    _memory.WriteFloat(velocityAddr, 0f);
                    _memory.WriteFloat(IntPtr.Add(velocityAddr, 4), 0f);
                    _memory.WriteFloat(IntPtr.Add(velocityAddr, 8), 0f);
                    
                    var collisionAddr = IntPtr.Add(playerBase, Offsets.CollisionFlag);
                    _memory.WriteInt(collisionAddr, 1);
                }
            }
            catch { }
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
                    
                    var variance = (float)(_random.NextDouble() * 0.2 - 0.1);
                    _memory.WriteFloat(IntPtr.Add(coordsBase, 8), z + (_speed + variance));
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
                    
                    var variance = (float)(_random.NextDouble() * 0.2 - 0.1);
                    _memory.WriteFloat(IntPtr.Add(coordsBase, 8), z - (_speed + variance));
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
                    
                    var variance = (float)(_random.NextDouble() * 0.1 - 0.05);
                    var adjustedDistance = distance + variance;

                    float newX = x + (float)(Math.Sin(-radians) * adjustedDistance);
                    float newY = y + (float)(Math.Cos(-radians) * adjustedDistance);

                    _memory.WriteFloat(coordsBase, newX);
                    _memory.WriteFloat(IntPtr.Add(coordsBase, 4), newY);
                }
            }
            catch { }
        }

        public void Update()
        {
            if (!_enabled) return;

            try
            {
                if ((DateTime.Now - _lastUpdate).TotalMilliseconds > 50)
                {
                    _lastUpdate = DateTime.Now;
                    
                    var worldPtr = IntPtr.Add(Offsets.BaseAddress, Offsets.WorldPtr);
                    var playerBase = _memory.GetAddressFromPointerPath(worldPtr, new int[] { 0x8 });

                    if (playerBase != IntPtr.Zero)
                    {
                        var velocityAddr = IntPtr.Add(playerBase, Offsets.Velocity);
                        var variance = (float)(_random.NextDouble() * 0.01);
                        _memory.WriteFloat(IntPtr.Add(velocityAddr, 8), variance);
                        
                        var collisionAddr = IntPtr.Add(playerBase, Offsets.CollisionFlag);
                        _memory.WriteInt(collisionAddr, 0);
                    }
                }
            }
            catch { }
        }
    }
}
