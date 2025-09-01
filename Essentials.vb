Imports VbPixelGameEngine
Imports NAudio.Wave

Public Enum PlatformImage
    Normal = 0
    Fragile = 1
    Broken = 2
    BouncingBlock = 3
    SpikeTrap = 4
End Enum

Public Enum PlayerImage
    MovingLeft = 0
    MovingRight = 1
    Item = 2  ' The third frame is a collectible item.
End Enum

Public Enum GameState
    Playing = 0
    Paused = 1
    GameOver = 2
End Enum

Public Module Essentials
    ' Player Sprites -> 3 frames listed horizonally, 13x15 px each
    Public ReadOnly sprPlayerSprites As New Sprite("Assets/player_sprites.png")
    ' Platform Sprites -> 5 frames listed veritically, 30x15 px each
    Public ReadOnly sprPlatformSprites As New Sprite("Assets/platform_sprites.png")
    ' Conveyor Belt Left -> 4 frames listed veritically, 30x15 px each
    Public ReadOnly sprConveyorBeltL As New Sprite("Assets/conveyor_belt_L.png")
    ' Conveyor Belt Right -> 4 frames listed veritically, 30x15 px each
    Public ReadOnly sprConveyorBeltR As New Sprite("Assets/conveyor_belt_R.png")

    Public ReadOnly bgmMainThemeReader As New AudioFileReader("Assets/main_theme.mp3")
    Public ReadOnly bgmMainTheme As New WaveOutEvent

    Public ReadOnly playerSprSize As New Vf2d(13, 15)
    Public ReadOnly platformSprSize As New Vf2d(30, 10)

    Public Function CollideRect _
            (pos1 As Vf2d, size1 As Vf2d, pos2 As Vf2d, size2 As Vf2d) As Boolean
        Return pos1.x < pos2.x + size2.x AndAlso
               pos1.x + size1.x > pos2.x AndAlso
               pos1.y < pos2.y + size2.y AndAlso
               pos1.y + size1.y > pos2.y
    End Function
End Module