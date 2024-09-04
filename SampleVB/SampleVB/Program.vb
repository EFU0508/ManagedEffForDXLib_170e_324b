Imports System.Threading
Imports DX


Module Program

    Structure effect
        Public EffectHandle As Integer
        Public EffectTime As Integer
        Public Position As VECTOR
    End Structure

    Sub Main()
        SetOutApplicationLogValidFlag(DX.FALSE)               ' log
        SetMainWindowText("ManagedEffForDXLib for VB.net")              ' タイトル
        ChangeWindowMode(DX.TRUE)                     ' 窓表示
        SetUseDirect3DVersion(DX_DIRECT3D_11)              ' directX ver
        SetGraphMode(1280, 720, 16)
        SetUseDirectInputFlag(DX.TRUE)                        ' DirectInput使用
        SetDirectInputMouseMode(DX.FALSE)                     ' DirectInputマウス使用
        SetWindowSizeChangeEnableFlag(DX.FALSE, DX.TRUE)         ' ウインドウサイズを手動変更不可、ウインドウサイズに合わせて拡大
        SetUsePixelLighting(DX.TRUE)                          ' ピクセルライティングの使用
        SetFullSceneAntiAliasingMode(4, 2)                 ' アンチエイリアス
        SetEnableXAudioFlag(DX.TRUE)                          ' XAudioを用いるか
        Set3DSoundOneMetre(1.0F)                           ' 3Dオーディオの基準距離指定
        SetWaitVSyncFlag(DX.FALSE)                             ' 垂直同期
        SetAlwaysRunFlag(DX.TRUE)                             '  非アクティブでも動作
        SetUseDXArchiveFlag(DX.TRUE)                          '  dxaファイルをフォルダとする 
        SetWindowUserCloseEnableFlag(DX.FALSE)                '  ×で勝手Windowを閉じないようにする
        Dim ret As Integer = DxLib_Init()
        If ret < 0 Then
            Throw New Exception("DxLib_Init Error")
        End If
        ' Effekseerを初期化する。
        ' 引数には画面に表示する最大パーティクル数を設定する。
        If Effekseer_Init(8000) = -1 Then
            DxLib_End()
            Throw New Exception("Effekseer_Init Error")
        End If
        SetDrawScreen(DX_SCREEN_BACK)                      '  描画先を裏画面にセット 
        ''SetWindowSize(SCREEN_WIDTH, SCREEN_HEIGHT)       ' これやるとおかしくなる
        MV1SetLoadModelUsePhysicsMode(DX_LOADMODEL_PHYSICS_LOADCALC)
        MV1SetLoadModelPhysicsWorldGravity(-9.8F)
        SetCameraNearFar(0.1F, 1000.0F)                    '  奥行0.1～1000までをカメラの描画範囲とする 
        SetUseLighting(DX.TRUE)                                '  ライティングを考慮しないモード 
        SetMouseDispFlag(DX.FALSE)                  ' マウス表示

        '' フルスクリーンウインドウの切り替えでリソースが消えるのを防ぐ。
        '' Effekseerを使用する場合は必ず設定する。
        SetChangeScreenModeGraphicsSystemResetFlag(DX.FALSE)

        '' DXライブラリのデバイスロストした時のコールバックを設定する。
        '' ウインドウとフルスクリーンの切り替えが発生する場合は必ず実行する。
        '' ただし、DirectX11を使用する場合は実行する必要はない。
        Effekseer_SetGraphicsDeviceLostCallbackFunctions()

        ''Zバッファを有効にする
        SetUseZBuffer3D(DX.TRUE)

        ''Zバッファの書き込みを有効にする
        SetWriteZBuffer3D(DX.TRUE)

        Dim pos As VECTOR = VGet(0, 0, 0)

        Dim direction As VECTOR = VGet(0.0F, 1.0F, 1.0F)
        Dim brightness As Single = 0.6F

        '' 平行光源の作成
        Dim lightHandle As Integer = CreateDirLightHandle(direction)

        '' 平行光源の方向を設定
        SetLightDirectionHandle(lightHandle, direction)

        '' ディフューズカラーの設定（白色光）
        SetLightDifColorHandle(lightHandle, GetColorF(brightness, brightness, brightness, 1.0F))

        '' スペキュラカラーの設定（白色光）
        SetLightSpcColorHandle(lightHandle, GetColorF(brightness, brightness, brightness, 1.0F))

        '' アンビエントカラーの設定（弱い白色光）
        SetLightAmbColorHandle(lightHandle, GetColorF(brightness, brightness, brightness, 1.0F))

        '' ライトを有効にする
        SetLightEnableHandle(lightHandle, True)

        '' カメラ位置更新
        SetCameraPositionAndTarget_UpVecY(VGet(0, 0, -10.0F), VGet(0, 0, 0))

        '' fps
        Dim mStartTime As Integer = 0      ''測定開始時刻
        Dim mCount As Integer = 0          ''カウンタ
        Dim mFps As Single = 0F          ''fps
        Const N As Integer = 60  ''平均を取るサンプル数
        Const FPS As Integer = 60  ''設定したFPS

        '' 移動速度
        Dim lastTime As Integer = GetNowCount()
        Const idouMax As Single = 100

        '' 玉
        Dim tamaKeyFlg As Boolean = False
        Dim tama As New List(Of VECTOR)()
        tama.Clear()

        '' 隕石
        Dim insekiTime As Integer = 0
        Dim inseki As New List(Of VECTOR)()
        inseki.Clear()

        '' 爆発
        Dim effectResourceHandle As Integer = LoadEffekseerEffect(".\Pierre02\Benediction.efkefc")
        Dim Bomb As New List(Of effect)()
        Bomb.Clear()

        Do While Not (ProcessMessage() = DX.TRUE) And Not (ClearDrawScreen() = DX.TRUE) And Not (CheckHitKey(KEY_INPUT_ESCAPE) = DX.TRUE)
            ' fps
            If mCount = 0 Then ' 1フレーム目なら時刻を記憶
                mStartTime = GetNowCount()
            End If
            If mCount = N Then ' 60フレーム目なら平均を計算する
                Dim t As Integer = GetNowCount()
                mFps = 1000.0F / ((t - mStartTime) / CSng(N))
                mCount = 0
                mStartTime = t
            End If
            mCount += 1

            ' 移動速度
            Dim nowTime As Integer = GetNowCount()
            Dim loopTime As Single = CSng(nowTime - lastTime) / 1000.0F
            lastTime = nowTime

            ' 自機の移動
            If CheckHitKey(KEY_INPUT_LEFT) = DX.TRUE Then
                pos = VSub(pos, VGet(loopTime * 4.0F, 0, 0))
            End If
            If CheckHitKey(KEY_INPUT_RIGHT) = DX.TRUE Then
                pos = VAdd(pos, VGet(loopTime * 4.0F, 0.0F, 0))
            End If
            If CheckHitKey(KEY_INPUT_DOWN) = DX.TRUE Then
                pos = VSub(pos, VGet(0, loopTime * 4.0F, 0))
            End If
            If CheckHitKey(KEY_INPUT_UP) = DX.TRUE Then
                pos = VAdd(pos, VGet(0, loopTime * 4.0F, 0))
            End If

            ' 発射
            If CheckHitKey(KEY_INPUT_SPACE) = DX.TRUE Then
                If Not tamaKeyFlg Then
                    tamaKeyFlg = True
                    Dim newTama As VECTOR = VSub(pos, VGet(0.0F, 0.0F, 1.0F))
                    tama.Add(newTama)
                End If
            Else
                tamaKeyFlg = False
            End If

            ' 玉の移動
            Dim i As Integer = 0
            Do While i < tama.Count
                tama(i) = VAdd(tama(i), VGet(0.0F, 0.0F, loopTime * 10.0F))
                If tama(i).z > idouMax Then
                    tama.RemoveAt(i)
                Else
                    i += 1
                End If
            Loop

            ' 隕石の移動
            insekiTime -= 1
            If insekiTime < 0 Then
                Dim newInseki As VECTOR = VGet(GetRand(16) - 8, GetRand(10) - 5, idouMax)
                inseki.Add(newInseki)
                insekiTime = GetRand(50)
            End If

            i = 0
            Do While i < inseki.Count
                inseki(i) = VSub(inseki(i), VGet(0.0F, 0.0F, loopTime * 50.0F))
                If inseki(i).z < 0.0F Then
                    inseki.RemoveAt(i)
                Else
                    i += 1
                End If
            Loop

            ' 球と球の当たり判定
            i = 0
            Do While i < tama.Count
                Dim hit As Boolean = False
                Dim j As Integer = 0
                Do While j < inseki.Count
                    ' (x2-x1)^2 + (y2-y1)^2 + (z2-z1)^2 <= (r1+r2)^2
                    Dim x As Single = CSng(Math.Pow(inseki(j).x - tama(i).x, 2))
                    Dim y As Single = CSng(Math.Pow(inseki(j).y - tama(i).y, 2))
                    Dim z As Single = CSng(Math.Pow(inseki(j).z - tama(i).z, 2))
                    If (x + y + z) <= CSng(Math.Pow(1.0F + 0.1F, 2)) Then
                        hit = True
                        inseki.RemoveAt(j)
                        Exit Do
                    Else
                        j += 1
                    End If
                Loop

                If hit Then
                    Dim eff As effect
                    eff.EffectHandle = PlayEffekseer3DEffect(effectResourceHandle)
                    eff.EffectTime = 100
                    eff.Position = tama(i)
                    Bomb.Add(eff)

                    ' 再生中のエフェクトを移動する。
                    SetPosPlayingEffekseer3DEffect(eff.EffectHandle, eff.Position.x, eff.Position.y, eff.Position.z)

                    tama.RemoveAt(i)
                Else
                    i += 1
                End If
            Loop

            ' 爆発
            i = 0
            Do While i < Bomb.Count
                Dim eff As effect = Bomb(i)
                eff.EffectTime -= 1
                Bomb(i) = eff
                If Bomb(i).EffectTime < 0 Then
                    Bomb.RemoveAt(i)
                Else
                    i += 1
                End If
            Loop

            ' Effekseerにより再生中のエフェクトを更新する。
            UpdateEffekseer3D()

            ' 画面のクリア
            ClearDrawScreen()

            DrawString(0, 0, mFps.ToString(), GetColor(255, 255, 255))

            ' 本体表示
            DrawCone3D(
                VAdd(pos, VGet(0.0F, 0.0F, 1.0F)),
                VSub(pos, VGet(0.0F, 0.0F, 1.0F)),
                0.5F,
                1,
                GetColor(0, 0, 255),
                GetColor(0, 255, 0),
                DX.TRUE)
            ' 翼
            DrawTriangle3D(
                VAdd(pos, VGet(0.0F, 0.0F, 1.0F)),
                VSub(pos, VGet(1.0F, 0.0F, 1.0F)),
                VSub(pos, VGet(-1.0F, 0.0F, 1.0F)),
                GetColor(0, 255, 255),
                DX.TRUE)

            ' 玉
            For Each vec As VECTOR In tama
                DrawSphere3D(vec, 0.1F, 1, GetColor(0, 255, 0), GetColor(255, 255, 255), DX.TRUE)
            Next

            ' 隕石
            For Each vec As VECTOR In inseki
                DrawSphere3D(vec, 1.0F, 1, GetColor(255, 255, 0), GetColor(255, 255, 255), DX.TRUE)
            Next

            ' DXライブラリのカメラとEffekseerのカメラを同期する。
            Effekseer_Sync3DSetting()

            ' Effekseerにより再生中のエフェクトを描画する。.
            DrawEffekseer3D()

            ' 裏画面の内容を表画面に反映させる
            ScreenFlip()

            ' fps
            Dim tookTime As Integer = GetNowCount() - mStartTime ' かかった時間
            Dim waitTime As Integer = mCount * 1000 / FPS - tookTime ' 待つべき時間
            If waitTime > 0 Then
                Thread.Sleep(waitTime) ' 待機
            End If
        Loop

        '' Effekseerを終了する。
        Effkseer_End()

        DxLib_End()             '' ＤＸライブラリ使用の終了処理
    End Sub

End Module
