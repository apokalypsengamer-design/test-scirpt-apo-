-- ==========================================
-- EXTERNAL MOVEMENT EXPLOIT TEST SUITE
-- NUI Menu Integration Version
-- ==========================================

local ExternalTests = {}
local ActiveMode = {
    speed = false,
    noclip = false,
    fly = false,
    superjump = false
}

local Config = {
    SpeedMultiplier = 1.0,
    NoclipSpeed = 2.0,
    FlySpeed = 5.0,
    SuperJumpMultiplier = 1.0
}

local menuOpen = false

-- ==========================================
-- NUI CALLBACKS
-- ==========================================

-- Toggle Menu
RegisterCommand("testmenu", function()
    menuOpen = not menuOpen
    SetNuiFocus(menuOpen, menuOpen)
    SendNUIMessage({
        action = "toggle",
        show = menuOpen
    })
end, false)

-- Close Menu Callback
RegisterNUICallback("close", function(data, cb)
    menuOpen = false
    SetNuiFocus(false, false)
    cb("ok")
end)

-- Menu Action Callback
RegisterNUICallback("menuAction", function(data, cb)
    local action = data.action
    
    if action == "speed" then
        ExternalTests.SpeedToggle(data.value)
    elseif action == "noclip" then
        Config.NoclipSpeed = data.value
        ExternalTests.NoclipToggle()
    elseif action == "fly" then
        Config.FlySpeed = data.value
        ExternalTests.FlyToggle()
    elseif action == "superjump" then
        ExternalTests.SuperJumpToggle(data.value)
    elseif action == "tp-forward" then
        ExternalTests.TeleportForward(data.distance)
    elseif action == "tp-waypoint" then
        ExternalTests.TeleportToWaypoint()
    elseif action == "tp-coords" then
        ExternalTests.TeleportToCoords(data.x, data.y, data.z)
    elseif action == "stop-all" then
        ExternalTests.DisableAll()
    end
    
    cb("ok")
end)

-- Update NUI Status
local function UpdateNUIStatus(type, active)
    SendNUIMessage({
        action = "updateStatus",
        type = type,
        active = active
    })
end

-- ==========================================
-- SPEED HACK - TOGGLE
-- ==========================================
ExternalTests.SpeedToggle = function(multiplier)
    local ped = PlayerPedId()
    local player = PlayerId()
    
    if ActiveMode.speed then
        SetPedMoveRateOverride(ped, 1.0)
        SetRunSprintMultiplierForPlayer(player, 1.0)
        ActiveMode.speed = false
        UpdateNUIStatus("speed", false)
        print("[EXTERNAL] Speed hack disabled")
    else
        Config.SpeedMultiplier = multiplier or 2.0
        SetPedMoveRateOverride(ped, Config.SpeedMultiplier)
        SetRunSprintMultiplierForPlayer(player, Config.SpeedMultiplier)
        ActiveMode.speed = true
        UpdateNUIStatus("speed", true)
        print(string.format("[EXTERNAL] Speed hack enabled: %.1fx", Config.SpeedMultiplier))
    end
end

Citizen.CreateThread(function()
    while true do
        if ActiveMode.speed then
            local ped = PlayerPedId()
            local player = PlayerId()
            SetPedMoveRateOverride(ped, Config.SpeedMultiplier)
            SetRunSprintMultiplierForPlayer(player, Config.SpeedMultiplier)
        end
        Wait(0)
    end
end)

-- ==========================================
-- TELEPORT - MANUAL
-- ==========================================
ExternalTests.TeleportForward = function(distance)
    local ped = PlayerPedId()
    local coords = GetEntityCoords(ped)
    local heading = GetEntityHeading(ped)
    local rad = math.rad(heading)
    
    distance = distance or 10
    
    local newPos = vector3(
        coords.x + math.sin(-rad) * distance,
        coords.y + math.cos(-rad) * distance,
        coords.z
    )
    
    SetEntityCoords(ped, newPos.x, newPos.y, newPos.z, false, false, false, true)
    print(string.format("[EXTERNAL] Teleported %dm forward", distance))
end

ExternalTests.TeleportToWaypoint = function()
    local ped = PlayerPedId()
    local waypoint = GetFirstBlipInfoId(8)
    
    if not DoesBlipExist(waypoint) then
        print("[EXTERNAL] No waypoint set")
        return
    end
    
    local coords = GetBlipCoords(waypoint)
    local ground, z = GetGroundZFor_3dCoord(coords.x, coords.y, 1000.0, false)
    
    if ground then
        SetEntityCoords(ped, coords.x, coords.y, z, false, false, false, true)
        print("[EXTERNAL] Teleported to waypoint")
    end
end

ExternalTests.TeleportToCoords = function(x, y, z)
    local ped = PlayerPedId()
    SetEntityCoords(ped, x, y, z, false, false, false, true)
    print(string.format("[EXTERNAL] Teleported to %.1f, %.1f, %.1f", x, y, z))
end

-- ==========================================
-- NOCLIP - TOGGLE
-- ==========================================
ExternalTests.NoclipToggle = function()
    ActiveMode.noclip = not ActiveMode.noclip
    
    if ActiveMode.noclip then
        UpdateNUIStatus("noclip", true)
        print("[EXTERNAL] Noclip enabled - W/S/A/D/Q/Z to move")
    else
        local ped = PlayerPedId()
        SetEntityCollision(ped, true, true)
        SetEntityInvincible(ped, false)
        FreezeEntityPosition(ped, false)
        UpdateNUIStatus("noclip", false)
        print("[EXTERNAL] Noclip disabled")
    end
end

Citizen.CreateThread(function()
    while true do
        if ActiveMode.noclip then
            local ped = PlayerPedId()
            local coords = GetEntityCoords(ped)
            local heading = GetEntityHeading(ped)
            
            SetEntityCollision(ped, false, false)
            SetEntityInvincible(ped, true)
            SetEntityVisible(ped, true, 0)
            FreezeEntityPosition(ped, true)
            
            if IsControlPressed(0, 32) then -- W
                local offset = GetOffsetFromEntityInWorldCoords(ped, 0.0, Config.NoclipSpeed, 0.0)
                SetEntityCoordsNoOffset(ped, offset.x, offset.y, offset.z, true, true, true)
            end
            
            if IsControlPressed(0, 33) then -- S
                local offset = GetOffsetFromEntityInWorldCoords(ped, 0.0, -Config.NoclipSpeed, 0.0)
                SetEntityCoordsNoOffset(ped, offset.x, offset.y, offset.z, true, true, true)
            end
            
            if IsControlPressed(0, 34) then -- A
                SetEntityHeading(ped, heading + 2.0)
            end
            
            if IsControlPressed(0, 35) then -- D
                SetEntityHeading(ped, heading - 2.0)
            end
            
            if IsControlPressed(0, 85) then -- Q
                SetEntityCoordsNoOffset(ped, coords.x, coords.y, coords.z + Config.NoclipSpeed, true, true, true)
            end
            
            if IsControlPressed(0, 48) then -- Z
                SetEntityCoordsNoOffset(ped, coords.x, coords.y, coords.z - Config.NoclipSpeed, true, true, true)
            end
        end
        Wait(0)
    end
end)

-- ==========================================
-- SUPER JUMP - TOGGLE
-- ==========================================
ExternalTests.SuperJumpToggle = function(multiplier)
    ActiveMode.superjump = not ActiveMode.superjump
    
    if ActiveMode.superjump then
        Config.SuperJumpMultiplier = multiplier or 2.0
        UpdateNUIStatus("superjump", true)
        print(string.format("[EXTERNAL] Super jump enabled: %.1fx", Config.SuperJumpMultiplier))
    else
        UpdateNUIStatus("superjump", false)
        print("[EXTERNAL] Super jump disabled")
    end
end

Citizen.CreateThread(function()
    while true do
        if ActiveMode.superjump then
            local ped = PlayerPedId()
            
            if IsPedJumping(ped) then
                SetSuperJumpThisFrame(PlayerId())
                SetEntityVelocity(ped, 0.0, 0.0, Config.SuperJumpMultiplier)
            end
        end
        Wait(0)
    end
end)

-- ==========================================
-- FLY MODE - TOGGLE
-- ==========================================
ExternalTests.FlyToggle = function()
    ActiveMode.fly = not ActiveMode.fly
    
    if ActiveMode.fly then
        UpdateNUIStatus("fly", true)
        print("[EXTERNAL] Fly mode enabled - W/S/Space/Ctrl to move")
    else
        local ped = PlayerPedId()
        SetEntityCollision(ped, true, true)
        UpdateNUIStatus("fly", false)
        print("[EXTERNAL] Fly mode disabled")
    end
end

Citizen.CreateThread(function()
    while true do
        if ActiveMode.fly then
            local ped = PlayerPedId()
            local coords = GetEntityCoords(ped)
            local heading = GetEntityHeading(ped)
            
            SetEntityCollision(ped, false, false)
            
            if IsControlPressed(0, 32) then -- W
                local rad = math.rad(heading)
                local newCoords = vector3(
                    coords.x + math.sin(-rad) * Config.FlySpeed,
                    coords.y + math.cos(-rad) * Config.FlySpeed,
                    coords.z
                )
                SetEntityCoordsNoOffset(ped, newCoords.x, newCoords.y, newCoords.z, false, false, false)
            end
            
            if IsControlPressed(0, 33) then -- S
                local rad = math.rad(heading)
                local newCoords = vector3(
                    coords.x - math.sin(-rad) * Config.FlySpeed,
                    coords.y - math.cos(-rad) * Config.FlySpeed,
                    coords.z
                )
                SetEntityCoordsNoOffset(ped, newCoords.x, newCoords.y, newCoords.z, false, false, false)
            end
            
            if IsControlPressed(0, 22) then -- Space
                SetEntityCoordsNoOffset(ped, coords.x, coords.y, coords.z + Config.FlySpeed, false, false, false)
            end
            
            if IsControlPressed(0, 36) then -- Ctrl
                SetEntityCoordsNoOffset(ped, coords.x, coords.y, coords.z - Config.FlySpeed, false, false, false)
            end
        end
        Wait(0)
    end
end)

-- ==========================================
-- SPEED ADJUSTMENTS
-- ==========================================
ExternalTests.SetSpeed = function(multiplier)
    Config.SpeedMultiplier = multiplier
    print(string.format("[EXTERNAL] Speed set to: %.1fx", multiplier))
end

ExternalTests.SetNoclipSpeed = function(speed)
    Config.NoclipSpeed = speed
    print(string.format("[EXTERNAL] Noclip speed set to: %.1f", speed))
end

ExternalTests.SetFlySpeed = function(speed)
    Config.FlySpeed = speed
    print(string.format("[EXTERNAL] Fly speed set to: %.1f", speed))
end

ExternalTests.SetJumpHeight = function(multiplier)
    Config.SuperJumpMultiplier = multiplier
    print(string.format("[EXTERNAL] Jump height set to: %.1fx", multiplier))
end

-- ==========================================
-- DISABLE ALL
-- ==========================================
ExternalTests.DisableAll = function()
    local ped = PlayerPedId()
    local player = PlayerId()
    
    SetPedMoveRateOverride(ped, 1.0)
    SetRunSprintMultiplierForPlayer(player, 1.0)
    SetEntityCollision(ped, true, true)
    SetEntityInvincible(ped, false)
    FreezeEntityPosition(ped, false)
    
    ActiveMode.speed = false
    ActiveMode.noclip = false
    ActiveMode.fly = false
    ActiveMode.superjump = false
    
    -- Update all NUI statuses
    UpdateNUIStatus("speed", false)
    UpdateNUIStatus("noclip", false)
    UpdateNUIStatus("fly", false)
    UpdateNUIStatus("superjump", false)
    
    print("[EXTERNAL] All exploits disabled")
end

-- ==========================================
-- LEGACY COMMANDS (still work)
-- ==========================================
RegisterCommand("speed", function(source, args)
    local multiplier = tonumber(args[1]) or 2.0
    ExternalTests.SpeedToggle(multiplier)
end, false)

RegisterCommand("setspeed", function(source, args)
    local multiplier = tonumber(args[1]) or 2.0
    ExternalTests.SetSpeed(multiplier)
end, false)

RegisterCommand("tp", function(source, args)
    local distance = tonumber(args[1]) or 10
    ExternalTests.TeleportForward(distance)
end, false)

RegisterCommand("tpw", function()
    ExternalTests.TeleportToWaypoint()
end, false)

RegisterCommand("tpc", function(source, args)
    local x = tonumber(args[1])
    local y = tonumber(args[2])
    local z = tonumber(args[3])
    
    if x and y and z then
        ExternalTests.TeleportToCoords(x, y, z)
    else
        print("[EXTERNAL] Usage: /tpc <x> <y> <z>")
    end
end, false)

RegisterCommand("noclip", function()
    ExternalTests.NoclipToggle()
end, false)

RegisterCommand("setnoclipspeed", function(source, args)
    local speed = tonumber(args[1]) or 2.0
    ExternalTests.SetNoclipSpeed(speed)
end, false)

RegisterCommand("superjump", function(source, args)
    local multiplier = tonumber(args[1]) or 2.0
    ExternalTests.SuperJumpToggle(multiplier)
end, false)

RegisterCommand("setjump", function(source, args)
    local multiplier = tonumber(args[1]) or 2.0
    ExternalTests.SetJumpHeight(multiplier)
end, false)

RegisterCommand("fly", function()
    ExternalTests.FlyToggle()
end, false)

RegisterCommand("setflyspeed", function(source, args)
    local speed = tonumber(args[1]) or 5.0
    ExternalTests.SetFlySpeed(speed)
end, false)

RegisterCommand("stopall", function()
    ExternalTests.DisableAll()
end, false)

print("^2[EXTERNAL]^7 AntiCheat Test Suite loaded")
print("^3[INFO]^7 Open menu with: ^2/testmenu^7")
print("^3[INFO]^7 Legacy commands still available (check console)")
