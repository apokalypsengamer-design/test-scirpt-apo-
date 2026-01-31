using System;

namespace FiveM_AntiCheat_Executor.Features
{
    public class SpeedHack
    {
        private MemoryManager _memory;
        private bool _enabled = false;
        private float _multiplier = 2.0f;
        private Random _random = new Random();
        private DateTime _lastUpdate = DateTime.Now;
        private float _currentMultiplier = 1.0f;

        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                if (!_enabled)
                {
                    Disable();
                }
            }
        }

        public float Multiplier
        {
            get => _multiplier;
            set => _multiplier = Math.Max(1.0f, Math.Min(5.0f, value));
        }

        public SpeedHack(MemoryManager memory)
        {
            _memory = memory;
        }

        public void Update()
        {
            if (!_enabled) return;

            try
            {
                if ((DateTime.Now - _lastUpdate).TotalMilliseconds > 50)
                {
                    _lastUpdate = DateTime.Now;
                    
                    var variance = (float)(_random.NextDouble() * 0.1 - 0.05);
                    _currentMultiplier = _multiplier + variance;
                    _currentMultiplier = Math.Max(1.0f, _currentMultiplier);
                }

                var worldPtr = IntPtr.Add(Offsets.BaseAddress, Offsets.WorldPtr);
                var playerBase = _memory.GetAddressFromPointerPath(worldPtr, new int[] { 0x8 });

                if (playerBase != IntPtr.Zero)
                {
                    var runSpeedAddr = IntPtr.Add(playerBase, Offsets.RunSpeed);
                    var swimSpeedAddr = IntPtr.Add(playerBase, Offsets.SwimSpeed);

                    _memory.WriteFloat(runSpeedAddr, 1.0f * _currentMultiplier);
                    _memory.WriteFloat(swimSpeedAddr, 1.0f * _currentMultiplier);
                    
                    var vehiclePtr = _memory.ReadPointer(IntPtr.Add(playerBase, Offsets.VehiclePtr));
                    if (vehiclePtr != IntPtr.Zero)
                    {
                        var vehicleSpeedAddr = IntPtr.Add(vehiclePtr, Offsets.VehicleSpeed);
                        var currentSpeed = _memory.ReadFloat(vehicleSpeedAddr);
                        _memory.WriteFloat(vehicleSpeedAddr, currentSpeed * (_currentMultiplier * 0.5f));
                    }
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
                    var runSpeedAddr = IntPtr.Add(playerBase, Offsets.RunSpeed);
                    var swimSpeedAddr = IntPtr.Add(playerBase, Offsets.SwimSpeed);

                    _memory.WriteFloat(runSpeedAddr, 1.0f);
                    _memory.WriteFloat(swimSpeedAddr, 1.0f);
                }
            }
            catch { }
        }
    }
}
