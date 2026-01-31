using System;

namespace FiveM_AntiCheat_Executor.Features
{
    public class RadarHack
    {
        private MemoryManager _memory;
        private bool _enabled = false;
        private bool _showInvisiblePlayers = true;
        private bool _showAllBlips = true;

        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        public bool ShowInvisiblePlayers
        {
            get => _showInvisiblePlayers;
            set => _showInvisiblePlayers = value;
        }

        public bool ShowAllBlips
        {
            get => _showAllBlips;
            set => _showAllBlips = value;
        }

        public RadarHack(MemoryManager memory)
        {
            _memory = memory;
        }

        public void Update()
        {
            if (!_enabled) return;

            try
            {
                if (_showInvisiblePlayers)
                {
                    RevealInvisiblePlayers();
                }

                if (_showAllBlips)
                {
                    RevealAllBlips();
                }
            }
            catch { }
        }

        private void RevealInvisiblePlayers()
        {
            try
            {
                var replayInterface = IntPtr.Add(Offsets.BaseAddress, Offsets.ReplayInterface);
                var pedInterface = _memory.ReadPointer(replayInterface);
                
                if (pedInterface == IntPtr.Zero) return;

                var pedList = _memory.ReadPointer(IntPtr.Add(pedInterface, 0x8));
                var pedCount = _memory.ReadInt(IntPtr.Add(pedInterface, 0x18));

                for (int i = 0; i < Math.Min(pedCount, 256); i++)
                {
                    try
                    {
                        var pedPtr = _memory.ReadPointer(IntPtr.Add(pedList, i * 0x10));
                        if (pedPtr == IntPtr.Zero) continue;

                        var visibilityAddr = IntPtr.Add(pedPtr, Offsets.InvisibleFlag);
                        var currentFlag = _memory.ReadInt(visibilityAddr);
                        
                        _memory.WriteInt(visibilityAddr, currentFlag | 0x01);
                    }
                    catch { }
                }
            }
            catch { }
        }

        private void RevealAllBlips()
        {
            try
            {
                var blipArray = IntPtr.Add(Offsets.BaseAddress, Offsets.RadarBlips);
                var blipList = _memory.ReadPointer(blipArray);
                
                if (blipList == IntPtr.Zero) return;

                for (int i = 0; i < 1500; i++)
                {
                    try
                    {
                        var blipPtr = IntPtr.Add(blipList, i * 0xB8);
                        var blipType = _memory.ReadInt(IntPtr.Add(blipPtr, 0x40));
                        
                        if (blipType == 0) continue;

                        var displayAddr = IntPtr.Add(blipPtr, 0x44);
                        _memory.WriteInt(displayAddr, 2);

                        var alphaAddr = IntPtr.Add(blipPtr, 0x48);
                        _memory.WriteInt(alphaAddr, 255);
                    }
                    catch { }
                }
            }
            catch { }
        }
    }
}
