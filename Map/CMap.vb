﻿Imports MapEditor.Item.Editor
Imports MapEditor.Item.Game
Imports MapEditor.Item
Imports System.IO
Imports SharpDX
Imports System.Text


Namespace Map
    Public Class CMap
        Public Property Creator As String = Environment.UserName
        Public Property Title As String = "Untitled"

        Public Property SignColl As New List(Of Sign)
        Public Property LayerColl As New List(Of CLayer)
        Public Property Grid As New UI.GridView(System.Drawing.Color.Black, New Size(20, 20), New Size(50, 50), True)
        Public Enum OpenResult
            Successful = 0
            Updated = 1
            Failed = 2
        End Enum
        Public Sub New()

        End Sub

#Region "Save"
        Private Function GetSaveString(ByVal toSave As Block) As String
            Dim waitToReturn As String = ""
            With toSave
                waitToReturn = .Type & "," & .Position.X & "," & .Position.Y & "," & .Size & "," & .Rotate
            End With
            Return waitToReturn
        End Function
        Private Function GetSignString(ByVal toSave As Sign) As String
            Dim waitToReturn As String = ""
            With toSave
                waitToReturn = .Position.X & "," & .Position.Y & "," & .Name
            End With
            Return waitToReturn
        End Function

        Private Function GetLayerString(ByVal toSave As CLayer) As String
            Dim waitToReturn As String = ""
            With toSave
                waitToReturn = .Name & "," & .Height & "," & .Visible
            End With
            Return waitToReturn
        End Function
        Public Sub SaveFile(ByVal FileName As String)
            Dim i As Integer
            Dim temp As String
            Dim strFilePath As String = FileName
            Dim sw As StreamWriter = New StreamWriter(strFilePath, False) 'true是指以追加的方式打开指定文件
            sw.WriteLine("ballance map format v2")

            sw.WriteLine()
            sw.WriteLine("[Metadata]")
            If Title <> "" Then
                sw.WriteLine("Title: " & Title)
            Else
                sw.WriteLine("Title: Unknown")
            End If
            If Creator <> "" Then
                sw.WriteLine("Creator: " & Creator)
            Else
                sw.WriteLine("Creator: Unknown")
            End If


            'sw.WriteLine()
            'sw.WriteLine("[Editor]")
            'sw.WriteLine("CanvasWidth: " & Grid.CanvasCount.Width)
            'sw.WriteLine("CanvasHeight: " & Grid.CanvasCount.Height)

            sw.WriteLine()
            sw.WriteLine("[Sign]")
            For i = 0 To SignColl.Count - 1
                temp = GetSignString(SignColl(i))
                sw.WriteLine(temp)
            Next

            sw.WriteLine()
            sw.WriteLine("[Layer]")
            For i = 0 To LayerColl.Count - 1
                temp = GetLayerString(LayerColl(i))
                sw.WriteLine(temp)
            Next

            sw.WriteLine()
            sw.WriteLine("[Block]")
            For j = 0 To LayerColl.Count - 1
                For i = 0 To LayerColl(j).BlockColl.Count - 1
                    temp = GetSaveString(LayerColl(j).BlockColl(i))
                    sw.WriteLine(temp & "," & j)
                Next
            Next
            sw.Flush()
            sw.Close()
            sw = Nothing
        End Sub
#End Region

#Region "Open v2"
        Public Result As OpenResult = OpenResult.Failed
        Private Function GetText(ByVal strFile As String, ByVal strStart As String, ByVal strEnd As String) As String
            Return Strings.Mid(strFile, ((Strings.InStr(strFile, strStart) + strStart.Length) + 2), ((Strings.InStr(strFile, strEnd) - Strings.InStr(strFile, strStart)) - (strStart.Length + 4)))
        End Function
        Private Function GetBlockItem(ByVal InfoString() As String) As Block
            Dim waitToReturn As New Block
            With waitToReturn
                Dim Location As New System.Drawing.Point(Val(InfoString(1)), Val(InfoString(2)))
                .Position = Location
                .Size = Val(InfoString(3))
                .Type = InfoString(0)
                .Rotate = InfoString(4)
                .Image = UI.Skin.GetSkin(.Type)
            End With
            Return (waitToReturn)
        End Function
        Public Sub OpenFile(ByVal MapPath As String)
            Dim t As New System.Threading.Thread(AddressOf thdOpenFile)
            t.Start(MapPath)
            t.Join()
        End Sub
        Dim WithEvents SolveFileText As New TextBox
        Private Sub thdOpenFile(ByVal MapPath As String)
            On Error GoTo err
Start:
            Dim FileText As String = ""
            Dim reader As New StreamReader(MapPath)

            Do While (reader.Peek > -1)
                Dim str As String = reader.ReadLine
                FileText = (FileText & str & vbCrLf)
            Loop
            SolveFileText.Text = FileText

            If SolveFileText.Lines(0).Split("v")(1) = 2 Then
                SolveFileText.Text = GetText(FileText, "[Metadata]", "[Sign]")
                Title = Mid(SolveFileText.Lines(0), 8)
                Creator = Mid(SolveFileText.Lines(1), 10)

                'SolveFileText.Text = GetText(FileText, "[Editor]", "[Sign]")
                'Grid = New UI.GridView(System.Drawing.Color.Black, New Size(20, 20), New Size(SolveFileText.Lines(1).Split(" ")(1), SolveFileText.Lines(0).Split(" ")(1)), True)

                SolveFileText.Text = GetText(FileText, "[Sign]", "[Layer]")

                For i = 0 To SolveFileText.Lines.Length - 2
                    Dim str1 = SolveFileText.Lines(i)
                    Dim spl As String() = str1.Split(",")
                    SignColl.Add(New Sign(spl(2), New System.Drawing.Point(spl(0), spl(1))))
                Next

                SolveFileText.Text = GetText(FileText, "[Layer]", "[Block]")
                For i = 0 To SolveFileText.Lines.Length - 2
                    Dim str1 = SolveFileText.Lines(i)
                    Dim spl As String() = str1.Split(",")
                    LayerColl.Add(New CLayer(spl(0), spl(1), spl(2)))
                Next

                SolveFileText.Text = Strings.Mid(FileText, (Strings.InStr(FileText, "[Block]") + 9))
                For i = 0 To SolveFileText.Lines.Length - 2
                    Dim str2 As String = SolveFileText.Lines(i)
                    Dim spl As String() = str2.Split(",")
                    If str2 <> "" Then
                        LayerColl(spl(5)).BlockColl.Add(GetBlockItem(spl))
                    End If
                Next
                If Result <> OpenResult.Updated Then Result = OpenResult.Successful
                Exit Sub
            End If

            If SolveFileText.Lines(0).Split("v")(1) = 1 Then
                reader.Close()
                FileCopy(MapPath, MapPath & ".bak")
                Dim Updater As New Updater.Updater_1to2
                Dim Res As Updater.Updater_1to2.UpdateResult
                Res = Updater.FileUpdate(SolveFileText.Lines.ToArray, MapPath)
                If Res = Map.Updater.Updater_1to2.UpdateResult.Successful Then
                    Result = OpenResult.Updated
                End If
                GoTo Start
            End If
            Exit Sub
err:
            Result = OpenResult.Failed
        End Sub
#End Region

#Region "Draw"
        Public Sub Draw(ByVal e As Direct2D1.RenderTarget)
            e.Clear(New SharpDX.Color4(SharpDX.Color.White.ToColor4))
            Grid.Draw(e)
            For i = 0 To LayerColl.Count - 1
                If LayerColl(i).Visible Then
                    LayerColl(i).Draw(e)
                End If
            Next
            For i = 0 To SignColl.Count - 1
                If SignColl(i).Position <> New System.Drawing.Point(0, 0) Then
                    SignColl(i).Draw(e)
                End If
            Next
        End Sub
#End Region

#Region "Bulid"
#Region "CommandMaker"
        Private Function ReturnPosition(ByVal Point As PointF, Height As Double, IsBlender As Boolean) As String
            If IsBlender Then
                Return "(" & Point.X & "," & Point.Y & "," & Height & ")"
            Else
                Return "pos:[" & Point.X & "," & Point.Y & "," & Height & "]"
            End If
        End Function
        Private Function ConvertToCommand_FullBlock(ByVal Num As Integer, ByVal Block As Block, ByVal Height As Double, IsBlender As Boolean) As String
            If IsBlender Then
                Return "helper('" & Block.Text & "_" & Block.Rotate & "'," & ReturnPosition(New PointF(((Block.Position.Y - 1) / 8 + 1.25), ((Block.Position.X - 1) / 8) + 1.25), Height, IsBlender) & ",'A_Floor_done" & Num & "')"
            Else
                Return "copy $" & Block.Text & "_" & Block.Rotate & " " & ReturnPosition(New PointF(((Block.Position.Y - 1) / 8 + 1.25), ((Block.Position.X - 1) / 8) + 1.25), Height, IsBlender) & " name:""A_Floor_done" & Num & """"
            End If
        End Function
        Private Function ConvertToCommand_BigBlock(ByVal Num As Integer, ByVal Block As Block, ByVal Height As Double, IsBlender As Boolean) As String
            Dim waitToReturn As String = ""
            If IsBlender Then
                waitToReturn += "helper('" & Block.Text & "_1_" & Block.Rotate & "'," & ReturnPosition(New PointF((Block.Position.Y - 1) / 8, (Block.Position.X - 1) / 8), Height, IsBlender) & ",'A_Floor_done" & Num & "_1')" & Environment.NewLine
                waitToReturn += "helper('" & Block.Text & "_2_" & Block.Rotate & "'," & ReturnPosition(New PointF((Block.Position.Y - 1) / 8 + 2.5, (Block.Position.X - 1) / 8), Height, IsBlender) & ",'A_Floor_done" & Num & "_2')" & Environment.NewLine
                waitToReturn += "helper('" & Block.Text & "_3_" & Block.Rotate & "'," & ReturnPosition(New PointF((Block.Position.Y - 1) / 8, (Block.Position.X - 1) / 8 + 2.5), Height, IsBlender) & ",'A_Floor_done" & Num & "_3')" & Environment.NewLine
                waitToReturn += "helper('" & Block.Text & "_4_" & Block.Rotate & "'," & ReturnPosition(New PointF((Block.Position.Y - 1) / 8 + 2.5, (Block.Position.X - 1) / 8 + 2.5), Height, IsBlender) & ",'A_Floor_done" & Num & "_4')"
            Else
                waitToReturn += "copy $" & Block.Text & "_1_" & Block.Rotate & " " & ReturnPosition(New PointF((Block.Position.Y - 1) / 8, (Block.Position.X - 1) / 8), Height, IsBlender) & " name:""A_Floor_done" & Num & "_1""" & Environment.NewLine
                waitToReturn += "copy $" & Block.Text & "_2_" & Block.Rotate & " " & ReturnPosition(New PointF((Block.Position.Y - 1) / 8 + 2.5, (Block.Position.X - 1) / 8), Height, IsBlender) & " name:""A_Floor_done" & Num & "_2""" & Environment.NewLine
                waitToReturn += "copy $" & Block.Text & "_3_" & Block.Rotate & " " & ReturnPosition(New PointF((Block.Position.Y - 1) / 8, (Block.Position.X - 1) / 8 + 2.5), Height, IsBlender) & " name:""A_Floor_done" & Num & "_3""" & Environment.NewLine
                waitToReturn += "copy $" & Block.Text & "_4_" & Block.Rotate & " " & ReturnPosition(New PointF((Block.Position.Y - 1) / 8 + 2.5, (Block.Position.X - 1) / 8 + 2.5), Height, IsBlender) & " name:""A_Floor_done" & Num & "_4"""
            End If
            Return waitToReturn
        End Function
        Private Function ConvertToCommand_SmallBlock(ByVal Num As Integer, ByVal Block As Block, ByVal Height As Double, IsBlender As Boolean) As String
            If IsBlender Then
                Return "helper('" & Block.Text & "_" & Block.Rotate & "'," & ReturnPosition(New PointF((Block.Position.Y - 1) / 8, (Block.Position.X - 1) / 8), Height, IsBlender) & ",'A_Floor_done" & Num & "')"
            Else
                Return "copy $" & Block.Text & "_" & Block.Rotate & " " & ReturnPosition(New PointF((Block.Position.Y - 1) / 8, (Block.Position.X - 1) / 8), Height, IsBlender) & " name:""A_Floor_done" & Num & """"
            End If
        End Function
#End Region


        Public Sub Build()
            Build(Title)
        End Sub
        Public Sub Build(ByVal FileName As String)
            Dim i As Integer

            Dim msFilePath As String = FileName & ".ms"
            Dim pyFilePath As String = FileName & ".py"
            Dim msfs As StreamWriter = New StreamWriter(msFilePath, False, Encoding.UTF8) 'true是指以追加的方式打开指定文件
            Dim pyfs As StreamWriter = New StreamWriter(pyFilePath, False, Encoding.UTF8)

            ' pre-write for py script
            pyfs.WriteLine("import bpy")
            pyfs.WriteLine("def helper(origin,loc,current):")
            pyfs.WriteLine("    bpy.ops.object.select_all(action='DESELECT')")
            pyfs.WriteLine("    bpy.context.scene.objects[origin].select_set(True)")
            pyfs.WriteLine("    bpy.ops.object.duplicate()")
            pyfs.WriteLine("    bpy.context.selected_objects[0].location = loc")
            pyfs.WriteLine("    bpy.context.selected_objects[0].name= current")
            pyfs.WriteLine("")

            ' write real code
            For j = 0 To LayerColl.Count - 1
                For i = 0 To LayerColl(j).BlockColl.Count - 1
                    If LayerColl(j).BlockColl(i).Size = BlockSize.BigBlock Then
                        If (LayerColl(j).BlockColl(i).Type <> BlockType.NormalChange_Su2Fl) And (LayerColl(j).BlockColl(i).Type <> BlockType.WoodTrafo) And (LayerColl(j).BlockColl(i).Type <> BlockType.PaperTrafo) And (LayerColl(j).BlockColl(i).Type <> BlockType.StoneTrafo) Then
                            msfs.WriteLine(ConvertToCommand_BigBlock(i, LayerColl(j).BlockColl(i), LayerColl(j).Height, False))
                            pyfs.WriteLine(ConvertToCommand_BigBlock(i, LayerColl(j).BlockColl(i), LayerColl(j).Height, True))
                        Else
                            msfs.WriteLine(ConvertToCommand_FullBlock(i, LayerColl(j).BlockColl(i), LayerColl(j).Height, False))
                            pyfs.WriteLine(ConvertToCommand_FullBlock(i, LayerColl(j).BlockColl(i), LayerColl(j).Height, True))
                        End If
                    Else
                        msfs.WriteLine(ConvertToCommand_SmallBlock(i, LayerColl(j).BlockColl(i), LayerColl(j).Height, False))
                        pyfs.WriteLine(ConvertToCommand_SmallBlock(i, LayerColl(j).BlockColl(i), LayerColl(j).Height, True))
                    End If
                Next
            Next



            msfs.Close()
            pyfs.Close()
            MsgBox("File has been generated in " & FileName & ".ms and " & FileName & ".py. Please use 3ds Max or Blender to load script.", , "Notice")
        End Sub
#End Region

    End Class
End Namespace