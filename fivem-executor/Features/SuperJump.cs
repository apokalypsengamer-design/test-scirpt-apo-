using System;

namespace FiveM_AntiCheat_Executor.Features
{
    public class SuperJump
    {
        private MemoryManager _memory;
        private bool _enabled = false;
        private float _multiplier = 2.0f;

        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        public float Multiplier
        {
            get => _multiplier;
            set => _multiplier = Math.Max(1.0f, Math.Min(10.0f, value));
        }

        public SuperJump(MemoryManager memory)
        {
            _memory = memory;
        }

        public void Update()
        {
            if (!_enabled) return;

            try
            {
                var worldPtr = IntPtr.Add(Offsets.BaseAddress, Offsets.WorldPtr);
                var playerBase = _memory.GetAddressFromPointerPath(worldPtr, new int[] { 0x8 });

                if (playerBase != IntPtr.Zero)
                {
                    var velocityAddr = IntPtr.Add(playerBase, Offsets.Velocity);
                    float currentVelocityZ = _memory.ReadFloat(IntPtr.Add(velocityAddr, 8));

                    if (currentVelocityZ > 0 && currentVelocityZ < 5.0f)
                    {
                        _memory.WriteFloat(IntPtr.Add(velocityAddr, 8), currentVelocityZ * _multiplier);
                    }
                }
            }
            catch { }
        }
    }
}
