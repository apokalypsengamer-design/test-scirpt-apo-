using System;

namespace FiveM_AntiCheat_Executor
{
    public static class Offsets
    {
        public static IntPtr BaseAddress;
        
        public const int WorldPtr = 0x2564CE0;
        public const int PlayerPtr = 0x8;
        public const int PlayerListPtr = 0x256ED58;
        public const int ReplayInterface = 0x1F48C20;
        
        public const int PlayerInfo = 0x10C8;
        public const int PlayerCoords = 0x90;
        public const int PlayerName = 0x7C;
        
        public const int VehiclePtr = 0xD30;
        public const int VehicleSpeed = 0x8C;
        
        public const int WantedLevel = 0x888;
        public const int Health = 0x280;
        public const int MaxHealth = 0x2A0;
        public const int Armor = 0x150C;
        public const int Stamina = 0xCE4;
        
        public const int Heading = 0x8C;
        public const int Velocity = 0x300;
        
        public const int NoClipFlag = 0x2E8;
        public const int GodModeFlag = 0x189;
        public const int CollisionFlag = 0x2E0;
        public const int InvisibleFlag = 0x2C;
        
        public const int RunSpeed = 0x14C;
        public const int SwimSpeed = 0x148;
        
        public const int WeaponManager = 0x10D8;
        public const int CurrentWeapon = 0x20;
        public const int WeaponSpread = 0x74;
        public const int WeaponRecoil = 0x2F4;
        public const int WeaponReload = 0x128;
        public const int AmmoInfo = 0x60;
        
        public const int ViewAngles = 0x3F0;
        public const int CameraPtr = 0x1F03DD0;
        public const int CameraPosition = 0x60;
        public const int CameraRotation = 0x40;
        
        public const int BoneMatrix = 0x430;
        public const int BoneCount = 0x2E;
        
        public const int RadarBlips = 0x1F43B20;
        public const int BlipList = 0x8;
        public const int BlipArraySize = 0x1700;
        
        public static class PlayerOffsets
        {
            public static int[] Coordinates = new int[] { PlayerPtr, PlayerCoords };
            public static int[] HealthOffset = new int[] { PlayerPtr, Health };
            public static int[] ArmorOffset = new int[] { PlayerPtr, Armor };
            public static int[] VelocityOffset = new int[] { PlayerPtr, Velocity };
            public static int[] HeadingOffset = new int[] { PlayerPtr, Heading };
            public static int[] WeaponOffset = new int[] { PlayerPtr, WeaponManager, CurrentWeapon };
        }
        
        public static class BoneIds
        {
            public const int Head = 0x796E;
            public const int Neck = 0xC4BB;
            public const int Spine3 = 0x60F0;
            public const int Spine2 = 0x60F1;
            public const int Spine1 = 0x60F2;
            public const int Pelvis = 0xE0FD;
            public const int LeftShoulder = 0x9D4D;
            public const int LeftElbow = 0x58B7;
            public const int LeftHand = 0xEB95;
            public const int RightShoulder = 0x29D2;
            public const int RightElbow = 0xBB0;
            public const int RightHand = 0x6E5C;
            public const int LeftHip = 0xB3FE;
            public const int LeftKnee = 0x3FCF;
            public const int LeftFoot = 0xCC4D;
            public const int RightHip = 0x90F8;
            public const int RightKnee = 0xCAC9;
            public const int RightFoot = 0x9000;
        }
        
        public static void Initialize(IntPtr baseAddress)
        {
            BaseAddress = baseAddress;
        }
    }
}
