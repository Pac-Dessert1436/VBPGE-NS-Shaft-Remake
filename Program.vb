Option Strict On
Option Infer On
Imports VbPixelGameEngine

Public NotInheritable Class Program
    Inherits PixelGameEngine

    Private Const INITIAL_HEALTH As Integer = 12
    Private Const GRAVITY As Single = 0.8F
    Private Const LEVEL_SCROLL_SPEED As Single = 3.0F
    Private Const UI_HEIGHT As Integer = 50
    Private Const OBJECT_SIZE As Integer = 2
    Private Const CONVEYOR_ANIMATION_SPEED As Single = 0.15F
    Private Const ITEM_SPAWN_CHANCE As Single = 0.1F
    Private Const PLATFORM_SPAWN_INTERVAL As Single = 0.4F
    Private Const BASEMENT_INCREMENT_INTERVAL As Single = 2.75F

    Private m_basement As Integer
    Private m_health As Integer
    Private m_playerPos As Vf2d
    Private m_playerVelocity As Vf2d
    Private m_gameState As GameState = GameState.Playing
    Private m_playerDirection As Integer = 1
    Private m_conveyorAnimTimer As Single = 0
    Private m_playerOnGround As Boolean = False
    Private m_levelOffset As Single = 0.0F
    Private m_platformSpawnTimer As Single = 0F
    Private m_basementTimer As Single = 0F

    Private ReadOnly m_platforms As New List(Of Platform)
    Private ReadOnly m_conveyors As New List(Of Conveyor)
    Private ReadOnly m_items As New List(Of Item)

    Private ReadOnly m_playerRectSize As Vf2d = playerSprSize * OBJECT_SIZE
    Private ReadOnly m_platformRectSize As Vf2d = platformSprSize * OBJECT_SIZE
    Private ReadOnly m_itemSize As Vf2d = playerSprSize * OBJECT_SIZE
    Private ReadOnly m_wallRectSize As New Vf2d(10, 450)
    Private ReadOnly m_topSpikeRectSize As New Vf2d(400, 10)

    Private Class Platform
        Public Property Position As Vf2d
        Public Property Type As PlatformImage
        Public Property HasSpawnedItem As Boolean = False

        Public Sub New(pos As Vf2d, type As PlatformImage)
            Position = pos : Me.Type = type
        End Sub
    End Class

    Private Class Conveyor
        Public Property Position As Vf2d
        Public Property Direction As Integer
        Public Property CurrentFrame As Integer = 0

        Public Sub New(pos As Vf2d, dir As Integer)
            Position = pos : Direction = dir
        End Sub
    End Class

    Private Class Item
        Public Property Position As Vf2d
        Public Property IsCollected As Boolean = False

        Public Sub New(pos As Vf2d)
            Position = pos
        End Sub
    End Class

    Public Sub New()
        AppName = "VBPGE NS-Shaft Remake"
    End Sub

    Friend Shared Sub Main()
        With New Program
            If .ConstructConsole(400, 600, 1, 1) Then .Start()
        End With
    End Sub

    Protected Overrides Function OnUserCreate() As Boolean
        SetPixelMode(Pixel.Mode.Mask)
        bgmMainTheme.Init(New LoopingStream(bgmMainThemeReader, 1.2))
        bgmMainTheme.Play()
        ResetGame()
        GenerateLevel()
        Return True
    End Function

    Private Sub GenerateLevel()
        m_platforms.Clear()
        m_conveyors.Clear()
        m_items.Clear()

        Dim initialPlatform As New Platform(
            New Vf2d(ScreenWidth \ 2 - m_platformRectSize.x / 2.0F, ScreenHeight - 50),
            PlatformImage.Normal
        )
        m_platforms.Add(initialPlatform)
        TrySpawnItemOnPlatform(initialPlatform)

        For i As Integer = 1 To 6
            Dim platformType As PlatformImage = PlatformImage.Normal
            If i = 2 Then platformType = PlatformImage.Fragile
            If i = 4 Then platformType = PlatformImage.BouncingBlock
            If i = 6 Then platformType = PlatformImage.SpikeTrap

            Dim xPos As Single = CSng(20 + Rnd() * (ScreenWidth - 40 - m_platformRectSize.x))
            Dim yPos As Single = ScreenHeight - 50 - i * 70
            Dim newPlatform As New Platform(New Vf2d(xPos, yPos), platformType)
            m_platforms.Add(newPlatform)
            TrySpawnItemOnPlatform(newPlatform)
        Next i

        m_playerPos = New Vf2d(
            ScreenWidth \ 2 - m_playerRectSize.x / 2.0F,
            ScreenHeight - 50 - m_playerRectSize.y
        )
        m_playerVelocity = New Vf2d(0, 0)
        m_levelOffset = 0F
        m_playerOnGround = True
    End Sub

    Private Sub TrySpawnItemOnPlatform(platform As Platform)
        If (platform.Type = PlatformImage.Normal OrElse
            platform.Type = PlatformImage.Fragile) AndAlso
            Not platform.HasSpawnedItem AndAlso Rnd() < ITEM_SPAWN_CHANCE Then
            Dim itemX As Single =
                platform.Position.x + (m_platformRectSize.x - m_itemSize.x) / 2.0F
            Dim itemY As Single = platform.Position.y - m_itemSize.y - 2.0F
            m_items.Add(New Item(New Vf2d(itemX, itemY)))
            platform.HasSpawnedItem = True
        End If
    End Sub

    Private Sub ResetGame()
        m_basement = 0
        m_health = INITIAL_HEALTH
        m_gameState = GameState.Playing
        m_platforms.Clear()
        m_conveyors.Clear()
        m_items.Clear()
        m_basementTimer = 0F
    End Sub

    Private Sub DrawUI()
        FillRect(0, 0, ScreenWidth, UI_HEIGHT, Presets.Black)
        DrawString(10, 15, "LIFE", Presets.Mint, 2)

        Const HEALTH_BAR_WIDTH As Integer = 10
        Const HEALTH_BAR_SPACING As Integer = 2
        Const HEALTH_BAR_START_X As Integer = 80
        Const HEALTH_BAR_Y As Integer = 13
        For i As Integer = 0 To INITIAL_HEALTH - 1
            Dim barColor As Pixel = If(i < INITIAL_HEALTH / 3, Presets.Red,
                                     If(i < INITIAL_HEALTH * 2 / 3, Presets.Yellow, Presets.Lime))
            If i < m_health Then
                FillRect(HEALTH_BAR_START_X + i * (HEALTH_BAR_WIDTH + HEALTH_BAR_SPACING),
                         HEALTH_BAR_Y, HEALTH_BAR_WIDTH, 20, barColor)
            Else
                DrawRect(HEALTH_BAR_START_X + i * (HEALTH_BAR_WIDTH + HEALTH_BAR_SPACING),
                         HEALTH_BAR_Y, HEALTH_BAR_WIDTH, 20, Presets.DarkGrey)
            End If
        Next i

        DrawString(300, 15, "B " & m_basement.ToString().PadLeft(3, "0"c), Presets.Mint, 2)
    End Sub

    Private Sub DrawWallsAndTopSpikes()
        FillRect(0, UI_HEIGHT, 10, ScreenHeight - UI_HEIGHT, Presets.Blue)
        FillRect(ScreenWidth - 10, UI_HEIGHT, 10, ScreenHeight - UI_HEIGHT, Presets.Blue)

        Const SPIKE_W As Integer = 10, SPIKE_H As Integer = 10
        For x As Integer = 10 To ScreenWidth - SPIKE_W - 10 Step SPIKE_W
            FillTriangle(
                x, UI_HEIGHT, x + SPIKE_W \ 2, UI_HEIGHT + SPIKE_H, x + SPIKE_W, UI_HEIGHT,
                Presets.White
            )
            DrawTriangle(
                x, UI_HEIGHT, x + SPIKE_W \ 2, UI_HEIGHT + SPIKE_H, x + SPIKE_W, UI_HEIGHT,
                Presets.Gray
            )
        Next x
    End Sub

    Private Sub UpdatePlayer(dt As Single)
        Dim moveSpeed As Single = 150.0F * dt
        If GetKey(Key.LEFT).Held Then
            m_playerVelocity.x = -moveSpeed
            m_playerDirection = -1
        ElseIf GetKey(Key.RIGHT).Held Then
            m_playerVelocity.x = moveSpeed
            m_playerDirection = 1
        Else
            m_playerVelocity.x *= 0.8F
        End If

        If Not m_playerOnGround Then
            m_playerVelocity.y += GRAVITY * dt
        Else
            m_playerVelocity.y = 0
        End If

        m_playerPos += m_playerVelocity

        If m_playerPos.x < 10 Then
            m_playerPos.x = 10
            m_playerVelocity.x = 0
        End If
        If m_playerPos.x > ScreenWidth - 10 - m_playerRectSize.x Then
            m_playerPos.x = ScreenWidth - 10 - m_playerRectSize.x
            m_playerVelocity.x = 0
        End If

        If m_playerPos.y < UI_HEIGHT + 15 Then
            m_health -= 4
            If m_health <= 0 Then
                m_gameState = GameState.GameOver
            Else
                m_playerPos.y = UI_HEIGHT + 20
                m_playerVelocity.y = 1.75F
            End If
        End If

        If m_playerPos.y > ScreenHeight Then
            m_health = 0
            m_gameState = GameState.GameOver
        End If
    End Sub

    Private Sub UpdateLevel(dt As Single)
        m_basementTimer += dt
        If m_basementTimer >= BASEMENT_INCREMENT_INTERVAL Then
            m_basement = Math.Clamp(m_basement + 1, 0, 999)
            m_basementTimer = 0
        End If

        m_platformSpawnTimer += dt
        If m_platformSpawnTimer >= PLATFORM_SPAWN_INTERVAL Then
            SpawnNewPlatform()
            m_platformSpawnTimer = 0
        End If

        For Each platform As Platform In m_platforms
            platform.Position = New Vf2d(
                platform.Position.x, platform.Position.y - LEVEL_SCROLL_SPEED * dt * 60.0F
            )
        Next platform

        For Each conveyor As Conveyor In m_conveyors
            conveyor.Position = New Vf2d(
                conveyor.Position.x, conveyor.Position.y - LEVEL_SCROLL_SPEED * dt * 60.0F
            )
        Next conveyor

        For Each item As Item In m_items
            item.Position = New Vf2d(
                item.Position.x, item.Position.y - LEVEL_SCROLL_SPEED * dt * 60.0F
            )
        Next item

        m_platforms.RemoveAll(Function(p) p.Position.y + m_platformRectSize.y < UI_HEIGHT)
        m_conveyors.RemoveAll(Function(c) c.Position.y + m_platformRectSize.y < UI_HEIGHT)
        m_items.RemoveAll(Function(i) i.Position.y + m_itemSize.y < UI_HEIGHT OrElse i.IsCollected)
    End Sub

    Private Sub SpawnNewPlatform()
        Dim xPos As Single = CSng(20 + Rnd() * (ScreenWidth - 40 - m_platformRectSize.x))
        Dim yPos As Single = ScreenHeight

        If Rnd() < 0.15 Then
            Dim direction As Integer = If(Rnd() < 0.5, 1, -1)
            m_conveyors.Add(New Conveyor(New Vf2d(xPos, yPos), direction))
        Else
            Dim platformType As PlatformImage
            Select Case Rnd()
                Case Is < 0.2
                    platformType = PlatformImage.Fragile
                Case Is < 0.3
                    platformType = PlatformImage.BouncingBlock
                Case Is < 0.35
                    platformType = PlatformImage.SpikeTrap
                Case Else
                    platformType = PlatformImage.Normal
            End Select
            Dim newPlatform As New Platform(New Vf2d(xPos, yPos), platformType)
            m_platforms.Add(newPlatform)
            TrySpawnItemOnPlatform(newPlatform)
        End If
    End Sub

    Private Sub CheckCollisions()
        Static spikeTrapTimer As Single = 0F
        m_playerOnGround = False
        spikeTrapTimer += 0.01F

        For Each platform As Platform In m_platforms
            If platform.Type = PlatformImage.Broken Then Continue For

            If CollideRect(
                m_playerPos, m_playerRectSize,
                platform.Position, m_platformRectSize
            ) Then
                If m_playerVelocity.y > 0 AndAlso
                   m_playerPos.y + m_playerRectSize.y <= platform.Position.y + 5 Then

                    m_playerOnGround = True
                    m_playerPos.y = platform.Position.y - m_playerRectSize.y
                    m_playerVelocity.y = 0

                    If platform.Type = PlatformImage.Fragile Then
                        platform.Type = PlatformImage.Broken
                    ElseIf platform.Type = PlatformImage.BouncingBlock Then
                        m_playerVelocity.y = -GRAVITY * 0.9F
                        m_playerOnGround = False
                    End If
                End If

                If platform.Type = PlatformImage.SpikeTrap AndAlso
                    m_playerOnGround AndAlso spikeTrapTimer > 1 Then
                    m_health -= 2
                    If m_health <= 0 Then
                        m_gameState = GameState.GameOver
                    End If
                    spikeTrapTimer = 0
                End If
            End If
        Next platform

        For Each conveyor As Conveyor In m_conveyors
            If CollideRect(
                m_playerPos, m_playerRectSize,
                conveyor.Position, m_platformRectSize
            ) Then
                If m_playerVelocity.y > 0 AndAlso
                    m_playerPos.y + m_playerRectSize.y <= conveyor.Position.y + 5 Then

                    m_playerOnGround = True
                    m_playerPos.y = conveyor.Position.y - m_playerRectSize.y
                    m_playerVelocity.y = 0
                End If
                If m_playerOnGround Then m_playerVelocity.x += conveyor.Direction * 0.5F
            End If
        Next conveyor

        For Each item As Item In m_items
            If Not item.IsCollected AndAlso CollideRect(
                m_playerPos, m_playerRectSize,
                item.Position, m_itemSize) Then
                item.IsCollected = True
                m_health = Math.Min(m_health + 1, INITIAL_HEALTH)
            End If
        Next item
    End Sub

    Private Sub DrawPlayer()
        Dim frame As Integer = If(m_playerDirection > 0, PlayerImage.MovingRight, PlayerImage.MovingLeft)
        Dim sourceRect As New Vi2d(frame * CInt(playerSprSize.x), 0)
        DrawPartialSprite(m_playerPos, sprPlayerSprites, sourceRect, playerSprSize, OBJECT_SIZE)
    End Sub

    Private Sub DrawItems()
        For Each item As Item In m_items
            If Not item.IsCollected Then
                Dim sourceRect As New Vi2d(PlayerImage.Item * CInt(playerSprSize.x), 0)
                DrawPartialSprite(
                    item.Position, sprPlayerSprites, sourceRect, playerSprSize, OBJECT_SIZE
                )
            End If
        Next
    End Sub

    Private Sub DrawPlatforms()
        For Each platform As Platform In m_platforms
            Dim sourceRect As New Vi2d(0, platform.Type * CInt(platformSprSize.y))
            DrawPartialSprite(
                platform.Position, sprPlatformSprites, sourceRect, platformSprSize, OBJECT_SIZE
            )
        Next platform
    End Sub

    Private Sub DrawConveyors(elapsedTime As Single)
        m_conveyorAnimTimer += elapsedTime
        If m_conveyorAnimTimer > CONVEYOR_ANIMATION_SPEED Then
            m_conveyorAnimTimer = 0
            For Each conveyor As Conveyor In m_conveyors
                conveyor.CurrentFrame = (conveyor.CurrentFrame + 1) Mod 4
            Next conveyor
        End If

        For Each conveyor As Conveyor In m_conveyors
            Dim sprite As Sprite = If(conveyor.Direction > 0, sprConveyorBeltR, sprConveyorBeltL)
            Dim sourceRect As New Vi2d(0, conveyor.CurrentFrame * CInt(platformSprSize.y))
            DrawPartialSprite(conveyor.Position, sprite, sourceRect, platformSprSize, OBJECT_SIZE)
        Next conveyor
    End Sub

    Private Sub DrawGameObjects(dt As Single)
        DrawPlayer()
        DrawPlatforms()
        DrawConveyors(dt)
        DrawItems()
        DrawWallsAndTopSpikes()
    End Sub

    Protected Overrides Function OnUserUpdate(elapsedTime As Single) As Boolean
        Select Case m_gameState
            Case GameState.Playing
                UpdatePlayer(elapsedTime)
                UpdateLevel(elapsedTime)
                CheckCollisions()
                If GetKey(Key.P).Pressed Then m_gameState = GameState.Paused
            Case GameState.Paused
                If GetKey(Key.P).Pressed Then m_gameState = GameState.Playing
            Case GameState.GameOver
                If GetKey(Key.R).Pressed Then
                    ResetGame()
                    GenerateLevel()
                End If
        End Select

        Clear(Presets.Teal)
        DrawGameObjects(elapsedTime)
        DrawUI()
        
        If m_gameState <> GameState.Playing Then
            FillRect(15, 230, ScreenWidth - 30, 125, Presets.Black)
            Dim textPos1 As New Vi2d(ScreenWidth \ 2 - 110, ScreenHeight \ 2 - 50)
            Dim textPos2 As New Vi2d(ScreenWidth \ 2 - 160, ScreenHeight \ 2 + 20)
            If m_gameState = GameState.Paused Then
                DrawString(textPos1, "PAUSED...", Presets.Green, 3)
                DrawString(textPos2, "Press 'P' to resume", Presets.White, 2)
            ElseIf m_gameState = GameState.GameOver Then
                DrawString(textPos1, "GAME OVER", Presets.Red, 3)
                DrawString(textPos2, "Press 'R' to restart", Presets.White, 2)
            End If
        End If
        DrawString(25, ScreenHeight - 12, "LEFT/RIGHT = Move   'P' = Pause   ESC = Quit")

        Return Not GetKey(Key.ESCAPE).Pressed
    End Function

    Protected Overrides Sub Finalize()
        bgmMainTheme.Stop()
        bgmMainThemeReader.Dispose()
    End Sub
End Class