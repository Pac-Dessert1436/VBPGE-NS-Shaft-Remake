Option Strict On
Option Infer On
Imports NAudio.Wave

Public NotInheritable Class LoopingStream
    Inherits WaveStream

    Private ReadOnly sourceStream As WaveStream, introEndPosition As Long
    Private hasPlayedIntro As Boolean = False

    ''' <summary>
    ''' Initailizes a new looping stream with an optional non-looping intro section.
    ''' </summary>
    ''' <param name="sourceStream">The source audio stream.</param>
    ''' <param name="introDuration">
    ''' Duration of the intro section in seconds (0 for no intro). </param>
    Public Sub New(sourceStream As WaveStream, Optional introDuration As Double = 0)
        Me.sourceStream = sourceStream

        ' Calculate the byte position where the intro ends.
        If introDuration > 0 Then
            Dim bytesPerSecond As Double = sourceStream.WaveFormat.AverageBytesPerSecond
            introEndPosition = CLng(introDuration * bytesPerSecond)

            ' Ensure we don't exceed the stream length - fall back to full looping if
            ' intro is too long.
            If introEndPosition >= sourceStream.Length Then introEndPosition = 0
        Else
            introEndPosition = 0
        End If
    End Sub

    Public Overrides ReadOnly Property WaveFormat As WaveFormat
        Get
            Return sourceStream.WaveFormat
        End Get
    End Property

    Public Overrides ReadOnly Property Length As Long
        Get
            Return sourceStream.Length
        End Get
    End Property

    Public Overrides Property Position As Long
        Get
            Return sourceStream.Position
        End Get
        Set(value As Long)
            ' When setting position, reset our intro tracking if we're before the intro end.
            If value < introEndPosition Then hasPlayedIntro = False
            sourceStream.Position = value
        End Set
    End Property

    Public Overrides Function Read _
            (buffer As Byte(), offset As Integer, count As Integer) As Integer
        Dim totalBytesRead As Integer = 0

        Do While totalBytesRead < count
            Dim bytesRead As Integer =
                sourceStream.Read(buffer, offset + totalBytesRead, count - totalBytesRead)

            If bytesRead = 0 Then
                ' If we've reached the end of the stream...
                If Not hasPlayedIntro AndAlso introEndPosition > 0 Then
                    ' First time reaching end - we need to play the intro.
                    hasPlayedIntro = True
                    sourceStream.Position = 0
                Else
                    ' Subsequent times - loop back to after the intro.
                    sourceStream.Position = introEndPosition
                End If
            Else
                ' Check if we've crossed the intro end position during this read.
                If Not hasPlayedIntro AndAlso introEndPosition > 0 AndAlso
                   sourceStream.Position >= introEndPosition Then hasPlayedIntro = True

                totalBytesRead += bytesRead
            End If
        Loop

        Return totalBytesRead
    End Function
End Class