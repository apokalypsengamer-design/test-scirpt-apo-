using System;
using System.Numerics;
using System.Threading;
using ImGuiNET;
using Veldrid;
using Veldrid.StartupUtilities;
using FiveM_AntiCheat_Executor.Features;

namespace FiveM_AntiCheat_Executor
{
    public class OverlayWindow
    {
        private Sdl2Window _window;
        private GraphicsDevice _gd;
        private CommandList _cl;
        private ImGuiController _controller;
        private MemoryManager _memory;
        private GlobalHotkey _hotkey;
        private bool _menuVisible = true;

        // Features
        private ESP _esp;
        private Aimbot _aimbot;
        private Fly _fly;
        private Noclip _noclip;
        private SpeedHack _speedHack;
        private SuperJump _superJump;
        private Teleport _teleport;
        private RadarHack _radarHack;

        // UI State
        private int _selectedTab = 0;
        private float[] _teleportCoords = new float[3];

        public OverlayWindow(MemoryManager memory)
        {
            _memory = memory;
            InitializeFeatures();
            InitializeWindow();
            InitializeHotkey();
        }

        private void InitializeFeatures()
        {
            var baseAddress = _memory.GetModuleBaseAddress("FiveM.exe");
            if (baseAddress == IntPtr.Zero)
                baseAddress = _memory.GetModuleBaseAddress("GTA5.exe");
            
            Offsets.Initialize(baseAddress);

            _esp = new ESP(_memory);
            _aimbot = new Aimbot(_memory);
            _fly = new Fly(_memory);
            _noclip = new Noclip(_memory);
            _speedHack = new SpeedHack(_memory);
            _superJump = new SuperJump(_memory);
            _teleport = new Teleport(_memory);
            _radarHack = new RadarHack(_memory);
        }

        private void InitializeWindow()
        {
            WindowCreateInfo wci = new WindowCreateInfo
            {
                X = 100,
                Y = 100,
                WindowWidth = 800,
                WindowHeight = 600,
                WindowTitle = "FiveM Executor"
            };

            _window = VeldridStartup.CreateWindow(ref wci);
            _window.Resized += () => _controller?.WindowResized(_window.Width, _window.Height);

            _gd = VeldridStartup.CreateGraphicsDevice(_window, new GraphicsDeviceOptions(true, null, true));
            _cl = _gd.ResourceFactory.CreateCommandList();
            _controller = new ImGuiController(_gd, _gd.MainSwapchain.Framebuffer.OutputDescription, _window.Width, _window.Height);

            SetupImGuiStyle();
        }

        private void InitializeHotkey()
        {
            _hotkey = new GlobalHotkey((visible) =>
            {
                _menuVisible = visible;
            });
            _hotkey.Start();
        }

        private void SetupImGuiStyle()
        {
            var style = ImGui.GetStyle();
            
            style.WindowRounding = 6.0f;
            style.FrameRounding = 4.0f;
            style.GrabRounding = 3.0f;
            style.ScrollbarRounding = 4.0f;
            style.WindowBorderSize = 1.0f;
            style.FrameBorderSize = 0.0f;
            style.PopupBorderSize = 1.0f;

            var colors = style.Colors;
            colors[(int)ImGuiCol.WindowBg] = new Vector4(0.10f, 0.10f, 0.10f, 0.95f);
            colors[(int)ImGuiCol.Border] = new Vector4(0.20f, 0.80f, 0.20f, 0.50f);
            colors[(int)ImGuiCol.TitleBg] = new Vector4(0.15f, 0.15f, 0.15f, 1.00f);
            colors[(int)ImGuiCol.TitleBgActive] = new Vector4(0.20f, 0.70f, 0.20f, 1.00f);
            colors[(int)ImGuiCol.FrameBg] = new Vector4(0.20f, 0.20f, 0.20f, 1.00f);
            colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.25f, 0.75f, 0.25f, 0.40f);
            colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.20f, 0.70f, 0.20f, 0.67f);
            colors[(int)ImGuiCol.Button] = new Vector4(0.25f, 0.75f, 0.25f, 0.40f);
            colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.30f, 0.80f, 0.30f, 1.00f);
            colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.20f, 0.70f, 0.20f, 1.00f);
            colors[(int)ImGuiCol.Header] = new Vector4(0.25f, 0.75f, 0.25f, 0.31f);
            colors[(int)ImGuiCol.HeaderHovered] = new Vector4(0.30f, 0.80f, 0.30f, 0.80f);
            colors[(int)ImGuiCol.HeaderActive] = new Vector4(0.20f, 0.70f, 0.20f, 1.00f);
            colors[(int)ImGuiCol.CheckMark] = new Vector4(0.30f, 0.90f, 0.30f, 1.00f);
            colors[(int)ImGuiCol.SliderGrab] = new Vector4(0.28f, 0.78f, 0.28f, 1.00f);
            colors[(int)ImGuiCol.SliderGrabActive] = new Vector4(0.35f, 0.85f, 0.35f, 1.00f);
            colors[(int)ImGuiCol.Tab] = new Vector4(0.20f, 0.20f, 0.20f, 0.86f);
            colors[(int)ImGuiCol.TabHovered] = new Vector4(0.30f, 0.80f, 0.30f, 0.80f);
            colors[(int)ImGuiCol.TabActive] = new Vector4(0.25f, 0.75f, 0.25f, 1.00f);
            colors[(int)ImGuiCol.TextSelectedBg] = new Vector4(0.25f, 0.75f, 0.25f, 0.35f);
        }

        public void Start()
        {
            Thread renderThread = new Thread(RenderLoop);
            renderThread.IsBackground = false;
            renderThread.Start();
        }

        private void RenderLoop()
        {
            while (_window.Exists)
            {
                var snapshot = _window.PumpEvents();
                if (!_window.Exists) break;

                _controller.Update(1f / 60f, snapshot);

                UpdateFeatures();

                if (_menuVisible)
                {
                    DrawMenu();
                }

                _cl.Begin();
                _cl.SetFramebuffer(_gd.MainSwapchain.Framebuffer);
                _cl.ClearColorTarget(0, new RgbaFloat(0, 0, 0, 0));
                _controller.Render(_gd, _cl);
                _cl.End();
                _gd.SubmitCommands(_cl);
                _gd.SwapBuffers(_gd.MainSwapchain);

                Thread.Sleep(16);
            }
        }

        private void UpdateFeatures()
        {
            _noclip.Update();
            _fly.Update();
            _speedHack.Update();
            _superJump.Update();
            _radarHack.Update();
            _aimbot.Update();
        }

        private void DrawMenu()
        {
            ImGui.SetNextWindowSize(new Vector2(700, 500), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowPos(new Vector2(50, 50), ImGuiCond.FirstUseEver);

            if (ImGui.Begin("APO's Test Executor v1.0.0", ImGuiWindowFlags.NoCollapse))
            {
                ImGui.Text($"FPS: {ImGui.GetIO().Framerate:F1}");
                ImGui.SameLine();
                ImGui.TextColored(new Vector4(0.3f, 0.9f, 0.3f, 1.0f), "| Press INSERT to toggle menu");
                
                ImGui.Separator();

                if (ImGui.BeginTabBar("MainTabs"))
                {
                    if (ImGui.BeginTabItem("Combat"))
                    {
                        DrawCombatTab();
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Movement"))
                    {
                        DrawMovementTab();
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Visual"))
                    {
                        DrawVisualTab();
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Teleport"))
                    {
                        DrawTeleportTab();
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Misc"))
                    {
                        DrawMiscTab();
                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();
                }
            }
            ImGui.End();
        }

        private void DrawCombatTab()
        {
            ImGui.BeginChild("CombatChild", new Vector2(0, 0), true);

            ImGui.TextColored(new Vector4(0.3f, 0.9f, 0.3f, 1.0f), "AIMBOT");
            ImGui.Separator();

            bool aimbotEnabled = _aimbot.Enabled;
            if (ImGui.Checkbox("Enable Aimbot", ref aimbotEnabled))
                _aimbot.Enabled = aimbotEnabled;

            if (_aimbot.Enabled)
            {
                ImGui.Indent();

                bool silentAim = _aimbot.SilentAim;
                if (ImGui.Checkbox("Silent Aim", ref silentAim))
                    _aimbot.SilentAim = silentAim;

                bool smoothAim = _aimbot.SmoothAim;
                if (ImGui.Checkbox("Smooth Aim", ref smoothAim))
                    _aimbot.SmoothAim = smoothAim;

                if (_aimbot.SmoothAim)
                {
                    float smoothSpeed = _aimbot.SmoothSpeed;
                    if (ImGui.SliderFloat("Smooth Speed", ref smoothSpeed, 1f, 20f))
                        _aimbot.SmoothSpeed = smoothSpeed;
                }

                float fov = _aimbot.FOV;
                if (ImGui.SliderFloat("FOV", ref fov, 10f, 180f))
                    _aimbot.FOV = fov;

                float maxDistance = _aimbot.MaxDistance;
                if (ImGui.SliderFloat("Max Distance", ref maxDistance, 50f, 1000f))
                    _aimbot.MaxDistance = maxDistance;

                ImGui.Text("Target Bone:");
                ImGui.SameLine();
                if (ImGui.RadioButton("Head", _aimbot.TargetBone == Offsets.BoneIds.Head))
                    _aimbot.TargetBone = Offsets.BoneIds.Head;
                ImGui.SameLine();
                if (ImGui.RadioButton("Neck", _aimbot.TargetBone == Offsets.BoneIds.Neck))
                    _aimbot.TargetBone = Offsets.BoneIds.Neck;
                ImGui.SameLine();
                if (ImGui.RadioButton("Chest", _aimbot.TargetBone == Offsets.BoneIds.Spine3))
                    _aimbot.TargetBone = Offsets.BoneIds.Spine3;

                ImGui.Unindent();
            }

            ImGui.EndChild();
        }

        private void DrawMovementTab()
        {
            ImGui.BeginChild("MovementChild", new Vector2(0, 0), true);

            // Fly
            ImGui.TextColored(new Vector4(0.3f, 0.9f, 0.3f, 1.0f), "FLY");
            ImGui.Separator();

            bool flyEnabled = _fly.Enabled;
            if (ImGui.Checkbox("Enable Fly", ref flyEnabled))
                _fly.Enabled = flyEnabled;

            if (_fly.Enabled)
            {
                ImGui.Indent();
                float flySpeed = _fly.Speed;
                if (ImGui.SliderFloat("Fly Speed", ref flySpeed, 1f, 20f))
                    _fly.Speed = flySpeed;

                ImGui.Text("Controls: W/S = Forward/Back, SPACE/CTRL = Up/Down");
                ImGui.Unindent();
            }

            ImGui.Spacing();
            ImGui.Spacing();

            // Noclip
            ImGui.TextColored(new Vector4(0.3f, 0.9f, 0.3f, 1.0f), "NOCLIP");
            ImGui.Separator();

            bool noclipEnabled = _noclip.Enabled;
            if (ImGui.Checkbox("Enable Noclip", ref noclipEnabled))
                _noclip.Enabled = noclipEnabled;

            if (_noclip.Enabled)
            {
                ImGui.Indent();
                float noclipSpeed = _noclip.Speed;
                if (ImGui.SliderFloat("Noclip Speed", ref noclipSpeed, 0.5f, 10f))
                    _noclip.Speed = noclipSpeed;
                ImGui.Unindent();
            }

            ImGui.Spacing();
            ImGui.Spacing();

            // Speed Hack
            ImGui.TextColored(new Vector4(0.3f, 0.9f, 0.3f, 1.0f), "SPEED HACK");
            ImGui.Separator();

            bool speedEnabled = _speedHack.Enabled;
            if (ImGui.Checkbox("Enable Speed Hack", ref speedEnabled))
                _speedHack.Enabled = speedEnabled;

            if (_speedHack.Enabled)
            {
                ImGui.Indent();
                float speedMultiplier = _speedHack.Multiplier;
                if (ImGui.SliderFloat("Speed Multiplier", ref speedMultiplier, 1f, 5f))
                    _speedHack.Multiplier = speedMultiplier;
                ImGui.Unindent();
            }

            ImGui.Spacing();
            ImGui.Spacing();

            // Super Jump
            ImGui.TextColored(new Vector4(0.3f, 0.9f, 0.3f, 1.0f), "SUPER JUMP");
            ImGui.Separator();

            bool jumpEnabled = _superJump.Enabled;
            if (ImGui.Checkbox("Enable Super Jump", ref jumpEnabled))
                _superJump.Enabled = jumpEnabled;

            if (_superJump.Enabled)
            {
                ImGui.Indent();
                float jumpMultiplier = _superJump.Multiplier;
                if (ImGui.SliderFloat("Jump Multiplier", ref jumpMultiplier, 1f, 10f))
                    _superJump.Multiplier = jumpMultiplier;
                ImGui.Unindent();
            }

            ImGui.EndChild();
        }

        private void DrawVisualTab()
        {
            ImGui.BeginChild("VisualChild", new Vector2(0, 0), true);

            ImGui.TextColored(new Vector4(0.3f, 0.9f, 0.3f, 1.0f), "ESP");
            ImGui.Separator();

            bool espEnabled = _esp.Enabled;
            if (ImGui.Checkbox("Enable ESP", ref espEnabled))
                _esp.Enabled = espEnabled;

            if (_esp.Enabled)
            {
                ImGui.Indent();

                bool showHealth = _esp.ShowHealth;
                if (ImGui.Checkbox("Show Health", ref showHealth))
                    _esp.ShowHealth = showHealth;

                bool showArmor = _esp.ShowArmor;
                if (ImGui.Checkbox("Show Armor", ref showArmor))
                    _esp.ShowArmor = showArmor;

                bool showName = _esp.ShowName;
                if (ImGui.Checkbox("Show Name", ref showName))
                    _esp.ShowName = showName;

                bool showDistance = _esp.ShowDistance;
                if (ImGui.Checkbox("Show Distance", ref showDistance))
                    _esp.ShowDistance = showDistance;

                bool showSkeleton = _esp.ShowSkeleton;
                if (ImGui.Checkbox("Show Skeleton", ref showSkeleton))
                    _esp.ShowSkeleton = showSkeleton;

                float maxDistance = _esp.MaxDistance;
                if (ImGui.SliderFloat("Max Distance", ref maxDistance, 50f, 1000f))
                    _esp.MaxDistance = maxDistance;

                ImGui.Unindent();

                ImGui.Spacing();
                ImGui.Text($"Players in range: {_esp.GetPlayerList().Count}");
            }

            ImGui.Spacing();
            ImGui.Spacing();

            ImGui.TextColored(new Vector4(0.3f, 0.9f, 0.3f, 1.0f), "RADAR HACK");
            ImGui.Separator();

            bool radarEnabled = _radarHack.Enabled;
            if (ImGui.Checkbox("Enable Radar Hack", ref radarEnabled))
                _radarHack.Enabled = radarEnabled;

            if (_radarHack.Enabled)
            {
                ImGui.Indent();

                bool showInvisible = _radarHack.ShowInvisiblePlayers;
                if (ImGui.Checkbox("Show Invisible Players", ref showInvisible))
                    _radarHack.ShowInvisiblePlayers = showInvisible;

                bool showAllBlips = _radarHack.ShowAllBlips;
                if (ImGui.Checkbox("Show All Blips", ref showAllBlips))
                    _radarHack.ShowAllBlips = showAllBlips;

                ImGui.Unindent();
            }

            ImGui.EndChild();
        }

        private void DrawTeleportTab()
        {
            ImGui.BeginChild("TeleportChild", new Vector2(0, 0), true);

            ImGui.TextColored(new Vector4(0.3f, 0.9f, 0.3f, 1.0f), "TELEPORT");
            ImGui.Separator();

            var currentPos = _teleport.GetCurrentPosition();
            ImGui.Text($"Current Position: X: {currentPos.x:F2}, Y: {currentPos.y:F2}, Z: {currentPos.z:F2}");

            ImGui.Spacing();

            ImGui.InputFloat("X", ref _teleportCoords[0]);
            ImGui.InputFloat("Y", ref _teleportCoords[1]);
            ImGui.InputFloat("Z", ref _teleportCoords[2]);

            if (ImGui.Button("Teleport to Coordinates", new Vector2(-1, 30)))
            {
                _teleport.TeleportToCoordinates(_teleportCoords[0], _teleportCoords[1], _teleportCoords[2]);
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            ImGui.Text("Quick Teleport:");
            
            float distance = 10f;
            ImGui.SliderFloat("Distance", ref distance, 5f, 100f);

            if (ImGui.Button("Teleport Forward", new Vector2(-1, 30)))
            {
                _teleport.TeleportForward(distance);
            }

            ImGui.EndChild();
        }

        private void DrawMiscTab()
        {
            ImGui.BeginChild("MiscChild", new Vector2(0, 0), true);

            ImGui.TextColored(new Vector4(0.3f, 0.9f, 0.3f, 1.0f), "INFORMATION");
            ImGui.Separator();

            ImGui.Text("Test Executor for FiveM Anti-Cheat");
            ImGui.Text("Created by APO");
            ImGui.Spacing();
            ImGui.Text("Features:");
            ImGui.BulletText("Aimbot (Silent & Smooth)");
            ImGui.BulletText("ESP (Health, Armor, Skeleton)");
            ImGui.BulletText("Fly & Noclip");
            ImGui.BulletText("Speed Hack & Super Jump");
            ImGui.BulletText("Teleport");
            ImGui.BulletText("Radar Hack");

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            ImGui.TextColored(new Vector4(0.3f, 0.9f, 0.3f, 1.0f), "CONTROLS");
            ImGui.Separator();
            ImGui.Text("INSERT = Toggle Menu");
            ImGui.Text("W/A/S/D = Movement (Fly/Noclip)");
            ImGui.Text("SPACE = Up (Fly/Noclip)");
            ImGui.Text("CTRL = Down (Fly/Noclip)");

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            if (ImGui.Button("Exit Executor", new Vector2(-1, 40)))
            {
                Environment.Exit(0);
            }

            ImGui.EndChild();
        }
    }
}
