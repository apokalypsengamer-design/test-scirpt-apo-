using System;
using System.Numerics;
using System.Threading;
using System.Collections.Generic;
using ImGuiNET;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using FiveM_AntiCheat_Executor.Features;

namespace FiveM_AntiCheat_Executor
{
    public class OverlayWindow
    {
        private Sdl2Window _window;
        private GraphicsDevice _graphicsDevice;
        private CommandList _commandList;
        private ImGuiController _controller;
        private MemoryManager _memory;

        private SpeedHack _speedHack;
        private Teleport _teleport;
        private Noclip _noclip;
        private Fly _fly;
        private SuperJump _superJump;
        private ESP _esp;
        private Aimbot _aimbot;
        private RadarHack _radarHack;

        private bool _menuOpen = true;
        private int _selectedTab = 0;
        
        private float _tpDistance = 10f;
        private float _tpX = 0f;
        private float _tpY = 0f;
        private float _tpZ = 0f;

        public OverlayWindow(MemoryManager memory)
        {
            _memory = memory;
            
            Offsets.Initialize(_memory.GetModuleBaseAddress("GTA5.exe"));

            _speedHack = new SpeedHack(_memory);
            _teleport = new Teleport(_memory);
            _noclip = new Noclip(_memory);
            _fly = new Fly(_memory);
            _superJump = new SuperJump(_memory);
            _esp = new ESP(_memory);
            _aimbot = new Aimbot(_memory);
            _radarHack = new RadarHack(_memory);
        }

        public void Start()
        {
            VeldridStartup.CreateWindowAndGraphicsDevice(
                new WindowCreateInfo(50, 50, 1100, 750, WindowState.Normal, "FiveM Executor"),
                new GraphicsDeviceOptions(true, null, true, ResourceBindingModel.Improved, true, true),
                out _window,
                out _graphicsDevice);

            _window.Resizable = false;

            _commandList = _graphicsDevice.ResourceFactory.CreateCommandList();
            _controller = new ImGuiController(_graphicsDevice, _graphicsDevice.MainSwapchain.Framebuffer.OutputDescription, _window.Width, _window.Height);

            _window.KeyDown += OnKeyDown;

            var stylePtr = ImGui.GetStyle();
            SetupImGuiStyle(ref stylePtr);

            while (_window.Exists)
            {
                InputSnapshot snapshot = _window.PumpEvents();
                
                if (!_window.Exists) break;

                _controller.Update(1f / 60f, snapshot);

                Update();
                
                _commandList.Begin();
                _commandList.SetFramebuffer(_graphicsDevice.MainSwapchain.Framebuffer);
                _commandList.ClearColorTarget(0, new RgbaFloat(0.04f, 0.05f, 0.07f, 0.95f));
                
                if (_menuOpen)
                {
                    RenderUI();
                }

                _controller.Render(_graphicsDevice, _commandList);
                _commandList.End();
                _graphicsDevice.SubmitCommands(_commandList);
                _graphicsDevice.SwapBuffers(_graphicsDevice.MainSwapchain);

                Thread.Sleep(16);
            }

            _graphicsDevice.WaitForIdle();
            _controller.Dispose();
            _commandList.Dispose();
            _graphicsDevice.Dispose();
        }

        private void OnKeyDown(KeyEvent e)
        {
            if (e.Key == Key.F6)
            {
                _menuOpen = !_menuOpen;
            }
        }

        private void Update()
        {
            _speedHack.Update();
            _fly.Update();
            _superJump.Update();
            _aimbot.Update();
            _radarHack.Update();
            _noclip.Update();
        }

        private void RenderUI()
        {
            ImGui.SetNextWindowPos(Vector2.Zero);
            ImGui.SetNextWindowSize(new Vector2(1100, 750));

            ImGui.Begin("##MainWindow", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove);

            RenderHeader();
            RenderTabs();
            
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            switch (_selectedTab)
            {
                case 0: RenderMovementTab(); break;
                case 1: RenderCombatTab(); break;
                case 2: RenderVisualTab(); break;
                case 3: RenderTeleportTab(); break;
            }

            RenderFooter();

            ImGui.End();
        }

        private void RenderHeader()
        {
            ImGui.PushFont(ImGui.GetIO().Fonts.Fonts[0]);
            
            var textColor = new Vector4(0f, 1f, 0.25f, 1f);
            ImGui.PushStyleColor(ImGuiCol.Text, textColor);
            ImGui.SetCursorPosY(20);
            
            var title = "EXECUTOR";
            var titleSize = ImGui.CalcTextSize(title);
            ImGui.SetCursorPosX((1100 - titleSize.X) / 2);
            ImGui.Text(title);
            
            ImGui.PopStyleColor();
            ImGui.PopFont();

            ImGui.SetCursorPosY(55);
            var subtitle = "FIVEM TESTING SUITE";
            var subtitleSize = ImGui.CalcTextSize(subtitle);
            ImGui.SetCursorPosX((1100 - subtitleSize.X) / 2);
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.48f, 0.55f, 0.58f, 1f));
            ImGui.Text(subtitle);
            ImGui.PopStyleColor();

            ImGui.SetCursorPosY(80);
            ImGui.Separator();
            ImGui.Spacing();
        }

        private void RenderTabs()
        {
            ImGui.SetCursorPosY(100);
            
            var tabWidth = 250f;
            var spacing = 20f;
            var totalWidth = (tabWidth * 4) + (spacing * 3);
            ImGui.SetCursorPosX((1100 - totalWidth) / 2);

            if (TabButton("MOVEMENT", 0, tabWidth)) _selectedTab = 0;
            ImGui.SameLine(0, spacing);
            if (TabButton("COMBAT", 1, tabWidth)) _selectedTab = 1;
            ImGui.SameLine(0, spacing);
            if (TabButton("VISUALS", 2, tabWidth)) _selectedTab = 2;
            ImGui.SameLine(0, spacing);
            if (TabButton("TELEPORT", 3, tabWidth)) _selectedTab = 3;
        }

        private bool TabButton(string label, int tabIndex, float width)
        {
            var isActive = _selectedTab == tabIndex;
            
            if (isActive)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0f, 1f, 0.25f, 0.3f));
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0f, 1f, 0.25f, 1f));
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0f, 1f, 0.25f, 0.05f));
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.6f, 0.6f, 0.6f, 1f));
            }

            var result = ImGui.Button(label, new Vector2(width, 40));
            
            ImGui.PopStyleColor(2);
            
            return result;
        }

        private void RenderMovementTab()
        {
            ImGui.BeginChild("MovementTab", new Vector2(1060, 530), true);

            ImGui.Columns(2, "MovementColumns", false);
            ImGui.SetColumnWidth(0, 520);
            ImGui.SetColumnWidth(1, 520);

            RenderSpeedHack();
            ImGui.NextColumn();
            RenderNoclip();
            ImGui.NextColumn();
            RenderFlyMode();
            ImGui.NextColumn();
            RenderSuperJump();

            ImGui.Columns(1);
            ImGui.EndChild();
        }

        private void RenderSpeedHack()
        {
            ImGui.BeginChild("SpeedHack", new Vector2(500, 130), true);
            
            ImGui.Text("SPEED MULTIPLIER");
            ImGui.SameLine(380);
            StatusBadge(_speedHack.Enabled);

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            float multiplier = _speedHack.Multiplier;
            ImGui.SetNextItemWidth(480);
            ImGui.SliderFloat("##SpeedSlider", ref multiplier, 1.0f, 5.0f, $"{multiplier:F1}x");
            _speedHack.Multiplier = multiplier;

            ImGui.Spacing();
            
            if (ImGui.Button(_speedHack.Enabled ? "DISABLE" : "ENABLE", new Vector2(480, 30)))
            {
                _speedHack.Enabled = !_speedHack.Enabled;
            }

            ImGui.EndChild();
        }

        private void RenderNoclip()
        {
            ImGui.BeginChild("Noclip", new Vector2(500, 130), true);
            
            ImGui.Text("NOCLIP");
            ImGui.SameLine(380);
            StatusBadge(_noclip.Enabled);

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            float speed = _noclip.Speed;
            ImGui.SetNextItemWidth(480);
            ImGui.SliderFloat("##NoclipSlider", ref speed, 0.5f, 10.0f, $"{speed:F1}");
            _noclip.Speed = speed;

            ImGui.Spacing();
            
            if (ImGui.Button(_noclip.Enabled ? "DISABLE" : "ENABLE", new Vector2(480, 30)))
            {
                _noclip.Enabled = !_noclip.Enabled;
            }

            ImGui.EndChild();
        }

        private void RenderFlyMode()
        {
            ImGui.BeginChild("FlyMode", new Vector2(500, 130), true);
            
            ImGui.Text("FLY MODE");
            ImGui.SameLine(380);
            StatusBadge(_fly.Enabled);

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            float speed = _fly.Speed;
            ImGui.SetNextItemWidth(480);
            ImGui.SliderFloat("##FlySlider", ref speed, 1.0f, 20.0f, $"{speed:F1}");
            _fly.Speed = speed;

            ImGui.Spacing();
            
            if (ImGui.Button(_fly.Enabled ? "DISABLE" : "ENABLE", new Vector2(480, 30)))
            {
                _fly.Enabled = !_fly.Enabled;
            }

            ImGui.EndChild();
        }

        private void RenderSuperJump()
        {
            ImGui.BeginChild("SuperJump", new Vector2(500, 130), true);
            
            ImGui.Text("SUPER JUMP");
            ImGui.SameLine(380);
            StatusBadge(_superJump.Enabled);

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            float multiplier = _superJump.Multiplier;
            ImGui.SetNextItemWidth(480);
            ImGui.SliderFloat("##JumpSlider", ref multiplier, 1.0f, 10.0f, $"{multiplier:F1}x");
            _superJump.Multiplier = multiplier;

            ImGui.Spacing();
            
            if (ImGui.Button(_superJump.Enabled ? "DISABLE" : "ENABLE", new Vector2(480, 30)))
            {
                _superJump.Enabled = !_superJump.Enabled;
            }

            ImGui.EndChild();
        }

        private void RenderCombatTab()
        {
            ImGui.BeginChild("CombatTab", new Vector2(1060, 530), true);

            RenderAimbot();
            ImGui.Spacing();
            RenderRadarHack();

            ImGui.EndChild();
        }

        private void RenderAimbot()
        {
            ImGui.BeginChild("Aimbot", new Vector2(1040, 320), true);
            
            ImGui.Text("AIMBOT CONFIGURATION");
            ImGui.SameLine(920);
            StatusBadge(_aimbot.Enabled);

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            ImGui.Columns(2, "AimbotCols", false);

            bool aimbotEnabled = _aimbot.Enabled;
            if (ImGui.Checkbox("Enable Aimbot", ref aimbotEnabled))
                _aimbot.Enabled = aimbotEnabled;
            
            bool silentAim = _aimbot.SilentAim;
            if (ImGui.Checkbox("Silent Aim", ref silentAim))
                _aimbot.SilentAim = silentAim;
            
            bool smoothAim = _aimbot.SmoothAim;
            if (ImGui.Checkbox("Smooth Aim", ref smoothAim))
                _aimbot.SmoothAim = smoothAim;

            float fov = _aimbot.FOV;
            ImGui.SetNextItemWidth(250);
            ImGui.SliderFloat("FOV", ref fov, 10f, 180f, $"{fov:F0}°");
            _aimbot.FOV = fov;

            ImGui.NextColumn();

            float smoothSpeed = _aimbot.SmoothSpeed;
            ImGui.SetNextItemWidth(250);
            ImGui.SliderFloat("Smooth Speed", ref smoothSpeed, 1f, 20f, $"{smoothSpeed:F1}");
            _aimbot.SmoothSpeed = smoothSpeed;

            float maxDist = _aimbot.MaxDistance;
            ImGui.SetNextItemWidth(250);
            ImGui.SliderFloat("Max Distance", ref maxDist, 50f, 1000f, $"{maxDist:F0}m");
            _aimbot.MaxDistance = maxDist;

            ImGui.Text("Target Bone:");
            ImGui.SameLine();
            
            int selectedBone = _aimbot.TargetBone == Offsets.BoneIds.Head ? 0 : 
                              _aimbot.TargetBone == Offsets.BoneIds.Neck ? 1 : 2;
            
            ImGui.SetNextItemWidth(250);
            if (ImGui.Combo("##TargetBone", ref selectedBone, "Head\0Neck\0Chest\0"))
            {
                _aimbot.TargetBone = selectedBone == 0 ? Offsets.BoneIds.Head :
                                    selectedBone == 1 ? Offsets.BoneIds.Neck :
                                    Offsets.BoneIds.Spine3;
            }

            ImGui.Columns(1);
            ImGui.EndChild();
        }

        private void RenderRadarHack()
        {
            ImGui.BeginChild("Radar", new Vector2(1040, 140), true);
            
            ImGui.Text("RADAR MANIPULATION");
            ImGui.SameLine(920);
            StatusBadge(_radarHack.Enabled);

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            bool radarEnabled = _radarHack.Enabled;
            if (ImGui.Checkbox("Enable Radar Hack", ref radarEnabled))
                _radarHack.Enabled = radarEnabled;
            
            ImGui.SameLine(300);
            
            bool showInvisible = _radarHack.ShowInvisiblePlayers;
            if (ImGui.Checkbox("Show Invisible Players", ref showInvisible))
                _radarHack.ShowInvisiblePlayers = showInvisible;
            
            ImGui.SameLine(600);
            
            bool showAllBlips = _radarHack.ShowAllBlips;
            if (ImGui.Checkbox("Show All Blips", ref showAllBlips))
                _radarHack.ShowAllBlips = showAllBlips;

            ImGui.EndChild();
        }

        private void RenderVisualTab()
        {
            ImGui.BeginChild("VisualTab", new Vector2(1060, 530), true);

            ImGui.Text("ESP CONFIGURATION");
            ImGui.SameLine(940);
            StatusBadge(_esp.Enabled);

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            ImGui.Columns(2, "ESPCols", false);

            bool espEnabled = _esp.Enabled;
            if (ImGui.Checkbox("Enable ESP", ref espEnabled))
                _esp.Enabled = espEnabled;
            
            bool showHealth = _esp.ShowHealth;
            if (ImGui.Checkbox("Show Health", ref showHealth))
                _esp.ShowHealth = showHealth;
            
            bool showArmor = _esp.ShowArmor;
            if (ImGui.Checkbox("Show Armor", ref showArmor))
                _esp.ShowArmor = showArmor;
            
            bool showName = _esp.ShowName;
            if (ImGui.Checkbox("Show Name", ref showName))
                _esp.ShowName = showName;

            ImGui.NextColumn();

            bool showDistance = _esp.ShowDistance;
            if (ImGui.Checkbox("Show Distance", ref showDistance))
                _esp.ShowDistance = showDistance;
            
            bool showSkeleton = _esp.ShowSkeleton;
            if (ImGui.Checkbox("Show Skeleton", ref showSkeleton))
                _esp.ShowSkeleton = showSkeleton;

            float maxDist = _esp.MaxDistance;
            ImGui.SetNextItemWidth(250);
            ImGui.SliderFloat("Max Distance", ref maxDist, 50f, 1000f, $"{maxDist:F0}m");
            _esp.MaxDistance = maxDist;

            ImGui.Columns(1);

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            ImGui.Text("ESP PREVIEW (Players in range will be shown)");

            ImGui.EndChild();
        }

        private void RenderTeleportTab()
        {
            ImGui.BeginChild("TeleportTab", new Vector2(1060, 530), true);

            ImGui.Columns(2, "TeleportCols", false);

            ImGui.BeginChild("TPForward", new Vector2(500, 150), true);
            ImGui.Text("FORWARD TELEPORT");
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            
            ImGui.SetNextItemWidth(480);
            ImGui.InputFloat("Distance (m)", ref _tpDistance);
            ImGui.Spacing();
            
            if (ImGui.Button("EXECUTE", new Vector2(480, 35)))
            {
                _teleport.TeleportForward(_tpDistance);
            }
            ImGui.EndChild();

            ImGui.NextColumn();

            ImGui.BeginChild("TPCoords", new Vector2(500, 150), true);
            ImGui.Text("COORDINATE TELEPORT");
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            
            ImGui.Columns(3, "CoordInputs", false);
            ImGui.SetNextItemWidth(150);
            ImGui.InputFloat("##TPX", ref _tpX);
            ImGui.NextColumn();
            ImGui.SetNextItemWidth(150);
            ImGui.InputFloat("##TPY", ref _tpY);
            ImGui.NextColumn();
            ImGui.SetNextItemWidth(150);
            ImGui.InputFloat("##TPZ", ref _tpZ);
            
            ImGui.Columns(1);
            ImGui.Spacing();
            
            if (ImGui.Button("EXECUTE", new Vector2(480, 35)))
            {
                _teleport.TeleportToCoordinates(_tpX, _tpY, _tpZ);
            }
            ImGui.EndChild();

            ImGui.Columns(1);
            ImGui.EndChild();
        }

        private void RenderFooter()
        {
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            if (ImGui.Button("EMERGENCY STOP - DISABLE ALL", new Vector2(1060, 45)))
            {
                _speedHack.Enabled = false;
                _noclip.Enabled = false;
                _fly.Enabled = false;
                _superJump.Enabled = false;
                _esp.Enabled = false;
                _aimbot.Enabled = false;
                _radarHack.Enabled = false;
            }

            ImGui.Spacing();
            var info = "Press F6 to toggle | Randomized values for detection evasion";
            var infoSize = ImGui.CalcTextSize(info);
            ImGui.SetCursorPosX((1100 - infoSize.X) / 2);
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.48f, 0.55f, 0.58f, 1f));
            ImGui.Text(info);
            ImGui.PopStyleColor();
        }

        private void StatusBadge(bool enabled)
        {
            if (enabled)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0f, 1f, 0.25f, 1f));
                ImGui.Text("ACTIVE");
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.48f, 0.55f, 0.58f, 1f));
                ImGui.Text("INACTIVE");
            }
            ImGui.PopStyleColor();
        }

        private void SetupImGuiStyle(ref ImGuiStylePtr style)
        {
            style.WindowPadding = new Vector2(20, 20);
            style.WindowRounding = 0;
            style.FramePadding = new Vector2(10, 8);
            style.FrameRounding = 2;
            style.ItemSpacing = new Vector2(10, 10);
            style.ItemInnerSpacing = new Vector2(10, 10);
            style.ScrollbarSize = 12;
            style.ScrollbarRounding = 2;
            style.GrabMinSize = 12;
            style.GrabRounding = 2;

            var colors = style.Colors;
            colors[(int)ImGuiCol.WindowBg] = new Vector4(0.06f, 0.08f, 0.11f, 0.95f);
            colors[(int)ImGuiCol.ChildBg] = new Vector4(0.06f, 0.08f, 0.10f, 1.00f);
            colors[(int)ImGuiCol.Border] = new Vector4(0f, 1f, 0.25f, 0.2f);
            colors[(int)ImGuiCol.FrameBg] = new Vector4(0f, 1f, 0.25f, 0.1f);
            colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0f, 1f, 0.25f, 0.15f);
            colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0f, 1f, 0.25f, 0.2f);
            colors[(int)ImGuiCol.SliderGrab] = new Vector4(0f, 1f, 0.25f, 1.0f);
            colors[(int)ImGuiCol.SliderGrabActive] = new Vector4(0f, 1f, 0.25f, 0.8f);
            colors[(int)ImGuiCol.Button] = new Vector4(0f, 1f, 0.25f, 0.15f);
            colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0f, 1f, 0.25f, 0.25f);
            colors[(int)ImGuiCol.ButtonActive] = new Vector4(0f, 1f, 0.25f, 0.35f);
            colors[(int)ImGuiCol.Header] = new Vector4(0f, 1f, 0.25f, 0.2f);
            colors[(int)ImGuiCol.HeaderHovered] = new Vector4(0f, 1f, 0.25f, 0.3f);
            colors[(int)ImGuiCol.HeaderActive] = new Vector4(0f, 1f, 0.25f, 0.4f);
            colors[(int)ImGuiCol.Separator] = new Vector4(0f, 1f, 0.25f, 0.2f);
            colors[(int)ImGuiCol.Text] = new Vector4(0.91f, 0.96f, 0.97f, 1.0f);
            colors[(int)ImGuiCol.CheckMark] = new Vector4(0f, 1f, 0.25f, 1.0f);
        }
    }
}
