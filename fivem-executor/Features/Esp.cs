using System;
using System.Collections.Generic;
using System.Numerics;

namespace FiveM_AntiCheat_Executor.Features
{
    public class ESP
    {
        private MemoryManager _memory;
        private bool _enabled = false;
        private bool _showHealth = true;
        private bool _showArmor = true;
        private bool _showName = true;
        private bool _showDistance = true;
        private bool _showSkeleton = true;
        private float _maxDistance = 500f;

        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        public bool ShowHealth
        {
            get => _showHealth;
            set => _showHealth = value;
        }

        public bool ShowArmor
        {
            get => _showArmor;
            set => _showArmor = value;
        }

        public bool ShowName
        {
            get => _showName;
            set => _showName = value;
        }

        public bool ShowDistance
        {
            get => _showDistance;
            set => _showDistance = value;
        }

        public bool ShowSkeleton
        {
            get => _showSkeleton;
            set => _showSkeleton = value;
        }

        public float MaxDistance
        {
            get => _maxDistance;
            set => _maxDistance = Math.Max(50f, Math.Min(1000f, value));
        }

        public ESP(MemoryManager memory)
        {
            _memory = memory;
        }

        public List<PlayerData> GetPlayerList()
        {
            var players = new List<PlayerData>();
            if (!_enabled) return players;

            try
            {
                var replayInterface = IntPtr.Add(Offsets.BaseAddress, Offsets.ReplayInterface);
                var pedInterface = _memory.ReadPointer(replayInterface);
                
                if (pedInterface == IntPtr.Zero) return players;

                var pedList = _memory.ReadPointer(IntPtr.Add(pedInterface, 0x8));
                var pedCount = _memory.ReadInt(IntPtr.Add(pedInterface, 0x18));

                var localPlayer = GetLocalPlayer();

                for (int i = 0; i < Math.Min(pedCount, 256); i++)
                {
                    try
                    {
                        var pedPtr = _memory.ReadPointer(IntPtr.Add(pedList, i * 0x10));
                        if (pedPtr == IntPtr.Zero || pedPtr == localPlayer.Address) continue;

                        var playerData = GetPlayerData(pedPtr);
                        if (playerData != null && playerData.Distance <= _maxDistance)
                        {
                            players.Add(playerData);
                        }
                    }
                    catch { }
                }
            }
            catch { }

            return players;
        }

        private PlayerData GetLocalPlayer()
        {
            var worldPtr = IntPtr.Add(Offsets.BaseAddress, Offsets.WorldPtr);
            var playerBase = _memory.GetAddressFromPointerPath(worldPtr, new int[] { 0x8 });
            
            return new PlayerData
            {
                Address = playerBase,
                Position = GetPosition(playerBase)
            };
        }

        private PlayerData GetPlayerData(IntPtr pedPtr)
        {
            try
            {
                var position = GetPosition(pedPtr);
                var localPos = GetLocalPlayer().Position;
                var distance = Vector3.Distance(position, localPos);

                var health = _memory.ReadFloat(IntPtr.Add(pedPtr, Offsets.Health));
                var maxHealth = _memory.ReadFloat(IntPtr.Add(pedPtr, Offsets.MaxHealth));
                var armor = _memory.ReadFloat(IntPtr.Add(pedPtr, Offsets.Armor));

                return new PlayerData
                {
                    Address = pedPtr,
                    Position = position,
                    Distance = distance,
                    Health = health,
                    MaxHealth = maxHealth,
                    Armor = armor,
                    IsVisible = IsVisible(pedPtr)
                };
            }
            catch
            {
                return null;
            }
        }

        private Vector3 GetPosition(IntPtr pedPtr)
        {
            var coordsBase = IntPtr.Add(pedPtr, Offsets.PlayerCoords);
            return new Vector3(
                _memory.ReadFloat(coordsBase),
                _memory.ReadFloat(IntPtr.Add(coordsBase, 4)),
                _memory.ReadFloat(IntPtr.Add(coordsBase, 8))
            );
        }

        private bool IsVisible(IntPtr pedPtr)
        {
            try
            {
                var visibilityFlag = _memory.ReadInt(IntPtr.Add(pedPtr, Offsets.InvisibleFlag));
                return (visibilityFlag & 0x01) == 0;
            }
            catch
            {
                return true;
            }
        }

        public Vector3 GetBonePosition(IntPtr pedPtr, int boneId)
        {
            try
            {
                var boneMatrix = _memory.ReadPointer(IntPtr.Add(pedPtr, Offsets.BoneMatrix));
                if (boneMatrix == IntPtr.Zero) return Vector3.Zero;

                var boneAddress = IntPtr.Add(boneMatrix, boneId * 0x10);
                
                return new Vector3(
                    _memory.ReadFloat(boneAddress),
                    _memory.ReadFloat(IntPtr.Add(boneAddress, 4)),
                    _memory.ReadFloat(IntPtr.Add(boneAddress, 8))
                );
            }
            catch
            {
                return Vector3.Zero;
            }
        }

        public Dictionary<int, Vector3> GetSkeleton(IntPtr pedPtr)
        {
            var skeleton = new Dictionary<int, Vector3>();
            
            if (!_showSkeleton) return skeleton;

            var bones = new[]
            {
                Offsets.BoneIds.Head,
                Offsets.BoneIds.Neck,
                Offsets.BoneIds.Spine3,
                Offsets.BoneIds.Spine2,
                Offsets.BoneIds.Spine1,
                Offsets.BoneIds.Pelvis,
                Offsets.BoneIds.LeftShoulder,
                Offsets.BoneIds.LeftElbow,
                Offsets.BoneIds.LeftHand,
                Offsets.BoneIds.RightShoulder,
                Offsets.BoneIds.RightElbow,
                Offsets.BoneIds.RightHand,
                Offsets.BoneIds.LeftHip,
                Offsets.BoneIds.LeftKnee,
                Offsets.BoneIds.LeftFoot,
                Offsets.BoneIds.RightHip,
                Offsets.BoneIds.RightKnee,
                Offsets.BoneIds.RightFoot
            };

            foreach (var bone in bones)
            {
                skeleton[bone] = GetBonePosition(pedPtr, bone);
            }

            return skeleton;
        }
    }

    public class PlayerData
    {
        public IntPtr Address { get; set; }
        public Vector3 Position { get; set; }
        public float Distance { get; set; }
        public float Health { get; set; }
        public float MaxHealth { get; set; }
        public float Armor { get; set; }
        public bool IsVisible { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
