using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

namespace FiveM_AntiCheat_Executor.Features
{
    public class Aimbot
    {
        private MemoryManager _memory;
        private bool _enabled = false;
        private bool _silentAim = false;
        private bool _smoothAim = false;
        private int _targetBone = Offsets.BoneIds.Head;
        private float _fov = 90f;
        private float _smoothSpeed = 5f;
        private float _maxDistance = 300f;

        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        public bool SilentAim
        {
            get => _silentAim;
            set => _silentAim = value;
        }

        public bool SmoothAim
        {
            get => _smoothAim;
            set => _smoothAim = value;
        }

        public float FOV
        {
            get => _fov;
            set => _fov = Math.Max(10f, Math.Min(180f, value));
        }

        public float SmoothSpeed
        {
            get => _smoothSpeed;
            set => _smoothSpeed = Math.Max(1f, Math.Min(20f, value));
        }

        public float MaxDistance
        {
            get => _maxDistance;
            set => _maxDistance = Math.Max(50f, Math.Min(1000f, value));
        }

        public int TargetBone
        {
            get => _targetBone;
            set => _targetBone = value;
        }

        public Aimbot(MemoryManager memory)
        {
            _memory = memory;
        }

        public void Update()
        {
            if (!_enabled) return;

            try
            {
                var target = GetBestTarget();
                if (target == IntPtr.Zero) return;

                var targetPos = GetBonePosition(target, _targetBone);
                var localPos = GetLocalPosition();

                if (Vector3.Distance(localPos, targetPos) > _maxDistance) return;

                var angles = CalculateAngles(localPos, targetPos);

                if (_silentAim)
                {
                    ApplySilentAim(angles);
                }
                else if (_smoothAim)
                {
                    ApplySmoothAim(angles);
                }
                else
                {
                    ApplyDirectAim(angles);
                }
            }
            catch { }
        }

        private IntPtr GetBestTarget()
        {
            try
            {
                var replayInterface = IntPtr.Add(Offsets.BaseAddress, Offsets.ReplayInterface);
                var pedInterface = _memory.ReadPointer(replayInterface);
                
                if (pedInterface == IntPtr.Zero) return IntPtr.Zero;

                var pedList = _memory.ReadPointer(IntPtr.Add(pedInterface, 0x8));
                var pedCount = _memory.ReadInt(IntPtr.Add(pedInterface, 0x18));

                var localPlayer = GetLocalPlayerAddress();
                var localPos = GetLocalPosition();
                var cameraAngles = GetCameraAngles();

                IntPtr bestTarget = IntPtr.Zero;
                float bestScore = float.MaxValue;

                for (int i = 0; i < Math.Min(pedCount, 256); i++)
                {
                    try
                    {
                        var pedPtr = _memory.ReadPointer(IntPtr.Add(pedList, i * 0x10));
                        if (pedPtr == IntPtr.Zero || pedPtr == localPlayer) continue;

                        var targetPos = GetBonePosition(pedPtr, _targetBone);
                        var distance = Vector3.Distance(localPos, targetPos);

                        if (distance > _maxDistance) continue;

                        var angles = CalculateAngles(localPos, targetPos);
                        var angleDiff = GetAngleDifference(cameraAngles, angles);

                        if (angleDiff > _fov) continue;

                        var score = distance + (angleDiff * 2f);
                        
                        if (score < bestScore)
                        {
                            bestScore = score;
                            bestTarget = pedPtr;
                        }
                    }
                    catch { }
                }

                return bestTarget;
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        private IntPtr GetLocalPlayerAddress()
        {
            var worldPtr = IntPtr.Add(Offsets.BaseAddress, Offsets.WorldPtr);
            return _memory.GetAddressFromPointerPath(worldPtr, new int[] { 0x8 });
        }

        private Vector3 GetLocalPosition()
        {
            var playerBase = GetLocalPlayerAddress();
            var coordsBase = IntPtr.Add(playerBase, Offsets.PlayerCoords);
            
            return new Vector3(
                _memory.ReadFloat(coordsBase),
                _memory.ReadFloat(IntPtr.Add(coordsBase, 4)),
                _memory.ReadFloat(IntPtr.Add(coordsBase, 8))
            );
        }

        private Vector3 GetBonePosition(IntPtr pedPtr, int boneId)
        {
            try
            {
                var boneMatrix = _memory.ReadPointer(IntPtr.Add(pedPtr, Offsets.BoneMatrix));
                if (boneMatrix == IntPtr.Zero)
                {
                    var coordsBase = IntPtr.Add(pedPtr, Offsets.PlayerCoords);
                    return new Vector3(
                        _memory.ReadFloat(coordsBase),
                        _memory.ReadFloat(IntPtr.Add(coordsBase, 4)),
                        _memory.ReadFloat(IntPtr.Add(coordsBase, 8)) + 0.5f
                    );
                }

                var boneAddress = IntPtr.Add(boneMatrix, boneId * 0x10);
                
                return new Vector3(
                    _memory.ReadFloat(boneAddress),
                    _memory.ReadFloat(IntPtr.Add(boneAddress, 4)),
                    _memory.ReadFloat(IntPtr.Add(boneAddress, 8))
                );
            }
            catch
            {
                var coordsBase = IntPtr.Add(pedPtr, Offsets.PlayerCoords);
                return new Vector3(
                    _memory.ReadFloat(coordsBase),
                    _memory.ReadFloat(IntPtr.Add(coordsBase, 4)),
                    _memory.ReadFloat(IntPtr.Add(coordsBase, 8))
                );
            }
        }

        private Vector2 GetCameraAngles()
        {
            try
            {
                var cameraPtr = IntPtr.Add(Offsets.BaseAddress, Offsets.CameraPtr);
                var camera = _memory.ReadPointer(cameraPtr);
                
                if (camera == IntPtr.Zero) return Vector2.Zero;

                var rotationAddr = IntPtr.Add(camera, Offsets.CameraRotation);
                
                return new Vector2(
                    _memory.ReadFloat(rotationAddr),
                    _memory.ReadFloat(IntPtr.Add(rotationAddr, 4))
                );
            }
            catch
            {
                return Vector2.Zero;
            }
        }

        private Vector2 CalculateAngles(Vector3 from, Vector3 to)
        {
            var delta = to - from;
            
            var yaw = (float)Math.Atan2(delta.Y, delta.X);
            var pitch = (float)Math.Atan2(-delta.Z, Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y));

            return new Vector2(pitch * (180f / (float)Math.PI), yaw * (180f / (float)Math.PI));
        }

        private float GetAngleDifference(Vector2 current, Vector2 target)
        {
            var diff = new Vector2(
                NormalizeAngle(target.X - current.X),
                NormalizeAngle(target.Y - current.Y)
            );

            return (float)Math.Sqrt(diff.X * diff.X + diff.Y * diff.Y);
        }

        private float NormalizeAngle(float angle)
        {
            while (angle > 180f) angle -= 360f;
            while (angle < -180f) angle += 360f;
            return angle;
        }

        private void ApplyDirectAim(Vector2 angles)
        {
            try
            {
                var cameraPtr = IntPtr.Add(Offsets.BaseAddress, Offsets.CameraPtr);
                var camera = _memory.ReadPointer(cameraPtr);
                
                if (camera == IntPtr.Zero) return;

                var rotationAddr = IntPtr.Add(camera, Offsets.CameraRotation);
                
                _memory.WriteFloat(rotationAddr, angles.X);
                _memory.WriteFloat(IntPtr.Add(rotationAddr, 4), angles.Y);
            }
            catch { }
        }

        private void ApplySmoothAim(Vector2 targetAngles)
        {
            try
            {
                var currentAngles = GetCameraAngles();
                var delta = new Vector2(
                    NormalizeAngle(targetAngles.X - currentAngles.X),
                    NormalizeAngle(targetAngles.Y - currentAngles.Y)
                );

                var smoothedAngles = new Vector2(
                    currentAngles.X + delta.X / _smoothSpeed,
                    currentAngles.Y + delta.Y / _smoothSpeed
                );

                var cameraPtr = IntPtr.Add(Offsets.BaseAddress, Offsets.CameraPtr);
                var camera = _memory.ReadPointer(cameraPtr);
                
                if (camera == IntPtr.Zero) return;

                var rotationAddr = IntPtr.Add(camera, Offsets.CameraRotation);
                
                _memory.WriteFloat(rotationAddr, smoothedAngles.X);
                _memory.WriteFloat(IntPtr.Add(rotationAddr, 4), smoothedAngles.Y);
            }
            catch { }
        }

        private void ApplySilentAim(Vector2 angles)
        {
            try
            {
                var playerBase = GetLocalPlayerAddress();
                var weaponManager = _memory.ReadPointer(IntPtr.Add(playerBase, Offsets.WeaponManager));
                
                if (weaponManager == IntPtr.Zero) return;

                var currentWeapon = _memory.ReadPointer(IntPtr.Add(weaponManager, Offsets.CurrentWeapon));
                
                if (currentWeapon == IntPtr.Zero) return;

                var spreadAddr = IntPtr.Add(currentWeapon, Offsets.WeaponSpread);
                var recoilAddr = IntPtr.Add(currentWeapon, Offsets.WeaponRecoil);

                _memory.WriteFloat(spreadAddr, 0f);
                _memory.WriteFloat(recoilAddr, 0f);
            }
            catch { }
        }
    }
}
