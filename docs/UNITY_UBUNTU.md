# Ubuntuで元のOperaSimVRを起動する

Unity **2022.3.4f1**で元の`Assets/Scenes/SampleScene.unity`を開きます。元のモデル・地形・C#・PhysXを使用します。このPCはUbuntu ARM64のため、x86_64版UnityをBox64で動かす実験的な構成です。[Unity公式ではLinux ARM64のEditorとLicensing Clientはサポート対象外です](https://docs.unity.com/en-us/hub/install-hub-linux)。このPCのUnity Hubでの認証は完了しています。

**コーン機能統合時の修復（2026-11-20追記）:** C#とREADMEに残っていた競合記号を除去し、欠落していたUbuntu起動・ビルドスクリプト、補助クラス、パッケージ修正ツールを復元しました。コーン編集中の入力制御も保持しています。`SampleScene` と `SimpleScene` のTerrainにあった、ScanPattern未設定で参照先のないLiDARコンポーネントを除去しました。LiDARを使用する場合は、必要な位置にセンサーを追加し、実際のScanPatternを設定してからPlayしてください。以下の初回検証記録には、この修復前に発生したLiDAR例外も記録しています。

修復後は、34件のコーン用NUnitアサーションと、通常のLinux Playerビルドが終了コード0で完了しました。新しいPlayerのローカルTCPテストはLiDAR例外の許容オプションなしで成功し、実行時例外0件・Player終了コード0でした。コーンの配置・間隔変更・JSON復元も再確認しました。[コーン機能の再検証結果](CONE_PLACEMENT.md#修復後の再検証2026-11-20)と `Logs/cone-placement-fix/` を参照してください。

**OperaSimPhysXとの実連動確認（2026-11-20追記）:** 既存のOperaSimPhysX接続（ROS TCP Endpoint 10000番）を維持し、ROS Domain 10の実際の`/zx200/base_link/pose`と`/zx200/joint_state`を、別の受信専用Endpoint 10001番経由でUbuntuのPlayerへ渡しました。受信したCDRバイト列と転送したTCPペイロードのSHA256も一致しています。位置・関節角の合成データは生成せず、VRからの送信データもROSへ転送しません。

別の検証ノードから正規の走行指令`/zx200/tracks/cmd_vel`だけを操作し、PhysXが配信する位置が約1.616m前進し、車体の方向転換も起こることを確認しました。VR側でも同じ位置変化と車体の向きの変化を確認し、停止指令後のROSとVRのX/Z位置誤差は2cm以内（最終照合時は約0.013mm以内）、4関節の角度は0.5度以内で一致しました。関節角は受信値との一致確認で、4関節それぞれを動かす試験ではありません。PhysX配信元は各トピック1つで、検証中も同じPublisher GIDでした。最後の走行指令は速度・角速度ともゼロです。

実連動の証拠は`Logs/ubuntu-physx-ros-live/integration-result.json`、指令とPhysXの応答は`motion-check.json`、実画面の等速録画は同フォルダーの`physx-linked.gif`です。`source.jsonl`と`forwarded.jsonl`には実データの時刻・値・CDRハッシュを記録しています。この確認で使った経路は下記の受信専用中継です。PhysXのプロセス停止・再起動時の復帰、各関節の動的操作、ヘッドセットは未検証です。先に作成した`Logs/ubuntu-live-demo/`の動画は合成信号による表示デモで、この実連動の証拠には使用しません。

**再検証（2026-11-20）:** Ubuntu 22.04.5 LTS／ARM64＋Box64、Unity 2022.3.4f1でデスクトップ版を再ビルドし、終了コード0を確認しました。生成したPlayerをOpenGL 4.5／llvmpipeで起動し、元の地形・重機の描画を画面でも確認しました。独立したローカルTCPのテスト用Endpointに接続し、PoseStampedとJointStateを2通りに変更すると、ZX200のUnity X位置が正確に2m変化し、4関節の角度差も期待値と0.5度以内で一致しました。Playerはウィンドウの終了操作で終了コード0でした。新しい検証スクリプトによる最終記録は`Logs/ubuntu-recheck-20261120-viewer-final/result.json`、画像は同フォルダーの`scene.png`、ビルドログは`Logs/ubuntu-recheck-20261120-build.log`です。実ROSノードとの相互接続を検証したものではありません。

シミュレーターモードも`NormalModeSimulator`で起動し、フレームと機体の物理状態が更新され、F6・W・PageUpによる視点変化を画面で確認しました。このPlayerも終了コード0でした。証拠は`Logs/ubuntu-recheck-20261120-simulator-controls/`のログ、`before.png`、`after.png`です。F6再押下による元の視点への復帰は、この画面検証では確定できていません。起動スクリプトの既存のスタブ検証は83項目すべて成功しました。LiDARの未設定ScanPatternによる例外は両モードで再現しており、例外なしの動作完了とは扱いません。

VR版も再ビルドして終了コード0となり、生成物にOpenVRのネイティブライブラリ2つとSingle Pass Instancedの設定が含まれることを確認しました。ビルド直前・直後のシーン・XR・ProjectSettingsの追跡対象55ファイルはSHA256が一致しています。ログは`Logs/ubuntu-recheck-20261120-vr-build.log`、比較結果は`Logs/ubuntu-recheck-20261120-vr-settings-result.json`です。VR起動も再試行しましたが、このVMではVulkanの物理デバイスを選択できず、OpenGLへのフォールバックをVRブリッジが拒否し、終了コード2となりました（`Logs/ubuntu-recheck-20261120-vr-startup.log`）。この失敗時にはCompute Shaderの初期化エラー等も記録されており、VRの動作成功とは扱いません。これはヘッドセットでの動作検証ではありません。

**このPCでの確認結果（2026-11-19）:** Linux PlayerのフルビルドがBurst有効のまま成功し、終了コード0を確認しました。完成したPlayerでも元の地形・重機が描画され、フレーム番号1→115、ゲーム時間0→37.69秒へ更新しました。ローカルTCPのテストデータ（Pose／JointStateを各2,344件送信）に応じてZX200の位置と関節が変化し、F6＋Wの視点移動とPlayerの終了コード0も確認しました。画面は`Logs/ubuntu-port-player-final.png`、ビルドと実行のログは`Logs/ubuntu-port-linux-integrated-build.log`・`Logs/ubuntu-port-player-final.log`、記録は`Logs/ubuntu-port-verification.json`です。

VR版もビルドが終了コード0で成功し、OpenVRのネイティブライブラリ2つとVulkan用の描画設定を出力に含むことを確認しました。ビルド前後で、元のシーン・XR・ProjectSettingsの追跡対象55ファイルのSHA256が一致しました。ログは`Logs/ubuntu-port-linux-vr-build-release.log`、設定復元の記録は`Logs/quest3-research/vr-settings-rollback-result.json`です。Vulkan GPUを選択できないこのVMでは、VR起動失敗のログと終了コード2も確認しました（`Logs/ubuntu-port-vr-startup-failure-final.log`）。

**残る制約:** ARM64＋llvmpipeの960×540表示は非常に遅く、実用的な速度には達していません。Intel／AMDの実機、Quest 3、掘削・侵食の結果、全モードの動作は未検証です。LiDARの走査設定と点群生成も未検証です。このVMにはUnityが選択できるVulkan GPUがなく、VR実行の検証には進めません。

## 起動

Intel／AMDのUbuntuではUnity 2022.3.4f1とLinux Build Support (Mono)を使用します。Python 3・Git・Git LFSと、URDF Importerのネイティブ依存`libminizip.so.1`が必要です。Ubuntuのパッケージは次で用意できます。

```bash
sudo apt install python3 git git-lfs libminizip1 mesa-utils
```

このPCのARM64では、以下に記載したBox64・x86_64ライブラリ・ARM64 ILPP Runnerを追加で使用します。Intel／AMD向けのUnityを互換実行する構成で、ネイティブARM64版のUnity／Playerを生成するものではありません。

プロジェクトのルートで実行します。

```bash
./operasimvr-unity-ubuntu
```

初回はアセットのインポートとC#のコンパイルに時間がかかります。進行状況・エラーは`Logs/ubuntu-unity-editor.log`で確認できます。通常起動は元のシーンに保存されたモード・トピック・ROS接続先を使用します。起動補助はシーンのアセットを保存しません。

新規取得した環境で初めてビルドする場合は、先に`./operasimvr-unity-ubuntu --open-only`で開き、パッケージ取得・Meta XR／UnitySensorsの修正生成・コンパイルが完了してからEditorを閉じてください。初回の修正生成はパッケージ取得を待つ別プロセスで行うため、未準備の状態からの`--build`一回での成功は検証していません。修正生成のログは`Logs/meta-xr-linux-patch.log`と`Logs/unity-sensors-patch.log`です。

### 元の機能とモードを維持する

Ubuntu対応でも、元の機体・PhysX・掘削・熱侵食のCompute Shader・地形・ROS送受信・プレビューのコードとアセットを使用します。通常起動でZX200の受信専用設定へ一律に変更しません。以下は明示的なモード指定です。

| 指定 | 元のModeSelector | 接続・受信設定 |
| --- | --- | --- |
| 指定なし／`--mode scene` | シーンの設定を保持 | シーンの設定を保持 |
| `--mode viewer` | PlayMode | ZX200のPose・JointStateを直接受信。既定は`127.0.0.1:10001` |
| `--mode simulator` | NormalModeSimulator | シーンの設定を保持 |
| `--mode preview` | PreviewModeForTeleop | シーンの設定を保持 |
| `--mode preview-and-play` | PreviewAndPlay | シーンの設定を保持 |
| `--mode preview-ar` | PreviewAR | シーンの設定を保持 |

接続先だけを上書きする場合は、例えば次のように実行します。

```bash
./operasimvr-unity-ubuntu --mode viewer --ros-ip 127.0.0.1 --ros-port 10001 --software-rendering
```

`OPERASIMVR_MODE`・`OPERASIMVR_ROS_IP`・`OPERASIMVR_ROS_PORT`でも同じ設定を指定できます。ビルド後のPlayerもこれらを読みます。元の`ROS_IP`環境変数とは別です。表示確認用のカメラ移動・追加の受信件数計測は、Editorで明示的にviewerを選んだ場合だけ行います。

Linuxではヘッドセットが有効でない場合、マウスの左クリックで機体・カメラを選択し、元の`V`で乗車・`B`で降車する処理を使います。地形のポイント指定もマウス位置から行えます。`DesignateVehicleFromInspector`が有効な場合はInspectorの指定を優先します。

降車中の移動は元の`PlayerKeyMover`を維持します。矢印の上下で前後、左右で旋回、左Shift＋上下で上下移動、左Shift＋左右で横移動、右Shift＋上下で視点を上下に傾けます。この既存処理はCharacterControllerを無効にして直接移動する設計なので、別の歩行・重力処理を同時に追加しません。

俯瞰操作は`F6`で開始／終了、`W/A/S/D`で水平移動、`PageUp/PageDown`で上下移動、`Q/E`で旋回です。既存のVR入力とキーボード操作も維持しています。選択した機体の指令用キーボード入力は`F7`で切り替えます（元の各Publisherの`key`設定）。これだけでは指令を有効化せず、元の乗車・`X`による操作開始の手順を使います。走行は矢印キー、ZX200の関節は`Y/H`・`U/J`・`I/K`・`O/L`です。

診断時は`OPERASIMVR_DIAGNOSTICS=1`で実フレーム番号・ゲーム時間・ZX200の姿勢を10秒間隔でログへ記録できます。同時に`OPERASIMVR_SCREENSHOT=/絶対パス/画像.png`を指定すると、6フレーム目以降に画面を一度保存します。これらは通常起動では無効です。

Meta/Oculusの既存XR設定は保持しています。導入済みOculus XR Plugin 4.0.0のヘッドセット対応はWindows／Androidで、Linuxは対象外です（[Unity公式のOculus XR Plugin資料](https://docs.unity3d.com/Packages/com.unity.xr.oculus@4.0/manual/index.html)、ローカルの`OculusLoader.IsDeviceSupported`も同様）。Linuxには以下のOpenVR経路を追加しています。Quest 3実機での確認はまだです。

### Meta Quest 3をWi-Fiで使う（実機検証前）

Quest 3のUbuntu接続は、**Quest 3 → ALVR → SteamVR → Linux版OperaSimVR**を対象にしています。Meta公式のLink／Air LinkはWindows PCが対象です。ここで追加するLinux向け経路は、既存のMeta/Oculus用XR設定とは別に、明示的に選んだときだけ起動します。[Meta公式のLink要件](https://www.meta.com/help/quest/140991407990979/)、[ALVR公式資料](https://github.com/alvr-org/ALVR)を参照してください。

ALVRはQuest 3とLinuxを対応対象に挙げ、SteamVRと、描画・動画のハードウェアエンコードに対応したGPUを要求しています。Questは5 GHz以上のWi-Fi、PCはルーターへ有線接続する構成が推奨されています。この作業ではALVR、SteamVR、Quest側アプリはインストールしておらず、ヘッドセットとの接続も未検証です。まず[ALVR公式の導入手順](https://github.com/alvr-org/ALVR/wiki/Installation-guide)に従ってQuest 3をSteamVRへ接続し、映像と左右コントローラーが認識される状態を用意します。ALVRのコントローラー設定はQuest 3 Touch PlusまたはQuest 2 Touchを使用します。これらは上流で`oculus_touch`プロファイルとして公開され、左右のスティックとアナログのトリガー／グリップを既存の入力処理へ渡せます。

Unity Editorを閉じ、VR用Playerを別にビルドします。

```bash
./operasimvr-unity-ubuntu --build --vr
```

起動スクリプトは必要なOpenVRパッケージを、確認済みの固定コミットから生成します。出力先は`Build/LinuxVR/OperaSimVR.x86_64`です。通常のデスクトップ用`Build/Linux`とは分かれています。VRビルド時だけ一時的にOpenVR・Vulkan・Single Pass Instancedを選び、ビルド後に元の設定へ戻します。Windows／AndroidのXR設定や元のシーンを保存し直しません。

SteamVRにQuest 3が接続された状態で、次を実行します。

```bash
./operasimvr-player-ubuntu --vr
```

`--vr`は`OPERASIMVR_XR=steamvr`とVulkanを選びます。モード・トピック・ROS接続先は、通常起動と同じく既存シーンの設定を保持します。例えば受信ビューアとして起動する場合だけ、次のように指定します。

```bash
OPERASIMVR_MODE=viewer OPERASIMVR_ROS_IP=127.0.0.1 OPERASIMVR_ROS_PORT=10001 \
  ./operasimvr-player-ubuntu --vr
```

VR指定なしではOpenVRを起動しません。PlayerのVR起動と`--software-rendering`は併用できません。このVRビルドはVulkan＋Single Pass Instanced用です。Valveの既知の問題としてLinuxのVulkan＋Multi Pass、OpenGL＋Single Pass Instancedはクラッシュするため、別の描画方式へ単純に切り替えないでください。[Valve公式パッケージの既知の問題](https://github.com/ValveSoftware/unity-xr-plugin/tree/35e0a1dce5c96d8f4a56312ae84acfa8e4c05dc7)を参照してください。

Vulkan GPUが使えずUnityがOpenGLへ切り替わった場合や、OpenVRの初期化に失敗した場合は、VR Playerは原因をログに出して終了コード2で終了します。通常表示を使う場合は、VR指定を外して`./operasimvr-player-ubuntu`を実行します。

OpenVRは**1.2.3の固定コミット`35e0a1dce5c96d8f4a56312ae84acfa8e4c05dc7`**を使用します。公式1.2.4のLinuxネイティブライブラリはGLIBC 2.38／GLIBCXX 3.4.32を要求し、Ubuntu 22.04標準環境では読み込めません。1.2.3はGLIBC 2.34／GLIBCXX 3.4.29までの要求で、Ubuntu 22.04の標準ライブラリに収まります。取得内容とSHA256を検証し、確認していない版や改変内容へ自動追従しません。この選択はライブラリの読込条件を満たすためのもので、特定のGPUやSteamVR版での動作保証ではありません。

既存の`OVRCameraRig`階層と各アンカー、`OVRInput.Get/GetDown/GetUp`を使うアプリ・SDK UIの呼び出しを維持します。LinuxでVRを有効にしたときだけ、頭・左右コントローラーの姿勢をUnity XRから更新し、ボタンと軸はMeta同梱のOpenVR APIから既存の入力状態へ渡します。A/B/X/Y、左右スティックと押し込み、アナログのトリガー／グリップを扱い、元の機体選択・乗降・走行／関節操作・俯瞰・UIへつなぎます。元のOculusネイティブ初期化をLinuxで強制的に有効化することはありません。元シーンのEyeLevel／FloorLevelを尊重し、姿勢が取れない間は指令用入力をゼロにします。

SteamVRがダッシュボード等で入力を消費している間は、アプリの入力と振動をゼロにし、頭・コントローラーの姿勢更新だけを続けます。入力が戻ったときは、押しっぱなしのボタンを新たな押下として扱いません。この切替も実機では未検証です。

System／MetaボタンはSteamVR等が予約する場合があります。触覚は既存OpenVR経路の振幅パルスを扱い、周波数・指ごとの振動・波形バッファを同等に再現するものではありません。Meta固有のハンドトラッキングやパススルーも、このLinux経路では対応済みと扱いません。既存アプリで設定していたヘッドセットの表示周波数は、LinuxではSteamVR／ALVR側で管理します。

入力パッチと起動コードは、実Unity DLLを参照したLinux Editor／Player向けコンパイルを確認しました。修正したPlayer用Oculus.VRアセンブリに対して、全ボタン・軸・入力の押下／解放・切断時のクリア・ローカル姿勢API・左右の触覚予約／停止を61項目の単体検証で確認しています。コントローラーモデルとUI入力に使う`OVRControllerHelper`のOculus初期化待ちも、Linux OpenVRの稼働を確認した場合だけ通過させます。Windows／Android向けに条件コンパイルした両SDK修正の有効コードは元と同一です。ただし、**Quest 3内の映像、実際のトラッキングと振動、座位／立位と乗車時の目線、Wi-Fi切断からの復帰、実用的な遅延とフレームレートは未検証**です。

Intel／AMDのUbuntu 22.04以降でのネイティブ実行を主なVR検証先とします。このARM64 PCでは、Unity 2022.3とPlayer、OpenVRのネイティブ部品をx86_64互換実行する実験構成です。virgl／ソフトウェア描画でのデスクトップ検証が成功しても、VR描画と動画エンコードが実用になるとは判断できません。ARM64のVR対応や、Quest側へアプリ本体を移す構成は、今回のデスクトップ移植と分けて検証が必要です。

### Linux Playerのビルド

Linux Build Support (Mono)を導入したUnity 2022.3.4f1で、Editorを閉じて実行します。

```bash
./operasimvr-unity-ubuntu --build
```

出力先は`Build/Linux/OperaSimVR.x86_64`、変更する場合は`OPERASIMVR_BUILD_PATH`を指定します。Editorメニューの`OperaSimVR/Ubuntu/Build Linux x86_64 player`からも実行できます。Build Settingsの有効なシーンのうち、このリポジトリーに存在するSampleSceneとCamera Sceneを含めます。存在しないSensorPodScene2〜4への元の参照は警告を出し、Build Settings自体は変更しません。

ビルド後はEditorを使わずに起動できます。

```bash
./operasimvr-player-ubuntu
# 明示的にZX200の受信ビューアとして起動する例
OPERASIMVR_MODE=viewer OPERASIMVR_ROS_IP=127.0.0.1 OPERASIMVR_ROS_PORT=10001 \
  ./operasimvr-player-ubuntu --software-rendering
```

Player起動スクリプトも、Compute Shader非対応のOpenGL環境を判別できた場合はMesaへ切り替えます。追加のUnity Player引数はそのまま渡せます。出力先を変更した場合は`OPERASIMVR_PLAYER=/絶対パス/OperaSimVR.x86_64`で実行ファイルを指定します。Intel／AMDでは直接実行し、このPCのARM64では上記の比較用Box64とプライベートライブラリを使用します。別のARM64環境への自動インストール機能はありません。Intel／AMDへビルドを移す場合は、実行ファイルだけでなく`Build/Linux`の内容一式が必要です。

### 生成したPlayerの描画・ROS TCP追従の自動検証

ビルド後、X11またはXwaylandのデスクトップから次を実行します。ROS 2のPythonメッセージと`python3-pil`、`libx11-6`が必要です。このPCでは導入済みです。

```bash
source /opt/ros/humble/setup.bash
python3 tools/verify-ubuntu-player.py --software-rendering \
  --output Logs/ubuntu-player-check-new
```

GPUで検証する場合は`--software-rendering`を省略します。空いているローカルポートを検証用に割り当て、このプロセスがPlayer専用のTCP Endpointを提供します。ROSノードは作らず、受け取った送信データをROSネットワークへ転送しません。2種類の位置・4関節の状態への追従、フレーム更新、Compute Shader対応の表示、PNGのデコードと画素の変化、Playerの正常終了を検査し、`result.json`と実行ログ・画面を保存します。描画内容の目視確認と、掘削・侵食の実際の結果の検証は別途必要です。キー操作はこの自動検証に含みません。

検証中はPlayerのウィンドウを前面に保ってください。元のPlayer設定ではフォーカスを外すと更新が止まる場合があります。既存の証拠がある出力先は上書きせず、既定では420秒でタイムアウトします。例外は検証失敗（終了コード1）になります。修復前のPlayerを再検証するための`--allow-known-lidar-error`もあります。上記の2026-11-20の初回記録ではこのオプションを使用しました。この場合も`runtime_clean: false`と例外のスタックを記録し、別の例外は許容しません。修復後のPlayerはこのオプションを使わずに検証します。

### Player向けのパッケージ修正

Playerで使わないUnityEditor参照を実行時コードから除き、HeightmapのEditorツールはEditorビルドだけに含めます。これはLinux Playerを作るための修正で、シミュレーション機能は除外しません。

Meta XR SDK 72.0.0のInspectorには、Linuxで変数`manager`が宣言されずコンパイルできない箇所があります。起動スクリプトはPython 3でパッケージのバージョンとSHA256を確認し、`Packages/com.meta.xr.sdk.core`にコピーを生成して、宣言を条件コンパイルの外へ移します。[Unityのembedded package方式](https://docs.unity3d.com/2022.3/Documentation/Manual/upm-embed.html)を使うため、再起動で修正が消えません。生成される約212MBのパッケージはGit管理の対象外で、修正はスクリプトから再現できます。元のパッケージキャッシュには書き込みません。以前の確認済みパッチが残るキャッシュも読み取り専用で扱います。

差分は`patches/meta-xr-72-linux-editor.patch`・`patches/meta-xr-72-linux-openvr-input.patch`・`patches/meta-xr-72-linux-controller-helper.patch`、適用処理は`tools/patch-meta-xr-linux.py`にあります。初回ダウンロード中は最長20分待って生成し、ログを`Logs/meta-xr-linux-patch.log`に残します。確認済みの版・内容と異なる場合は適用を停止します。

UnitySensorsのEditor用asmdefには対象プラットフォームの指定がなく、Linux PlayerビルドでもUnityEditorを要求して失敗していました。`tools/patch-unity-sensors.py`は確認済みのコミット`b1a50503bb38017f902cb232b489de2de334d62b`とasmdefのSHA256を検証し、`Packages/com.frj.unity-sensors`へembedded copyを作り、Editor用アセンブリだけをEditor対象にします。約500MBの生成物はGit管理の対象外です。センサーの実行時コードや走査データは変更しません。ROS側を含めて依存先を、既に使用していた同じコミットに固定しています。差分は`patches/unity-sensors-b1a-editor.patch`です。

Git LFSなしで取得したキャッシュでは、Livoxの`mid-360.obj`がモデル本体ではなくLFSの参照テキストだけになっていました。同じスクリプトは、この確認済みの参照だけを検出し、固定した元コミットから1,680,529バイトの本体を取得してSHA256（`ad16b6ac2f88f3a5bf0d33f1a25e5e53438ce3808dab349bca360567910b6f11`）を照合して復元します。元パッケージのキャッシュやGitのフィルター設定は変更しません。モデル本体が既にある場合は保持し、別の参照や不一致のダウンロードは拒否します。

ZX200・プレビューZX200・C30RのPrefabにあった、両方のリンクが空の衝突除外ペアも削除しました。参照先が有効なペアや、`CustomCollisionZX200`等による実際の衝突除外処理は保持しています。LiDARの未設定ScanPatternには、機種や走査仕様を推測して値を入れていません。

同じパッチには、Linux EditorでAndroidモジュールが未導入の場合にMeta XRのADB初期化がSDK要求ダイアログを出さないための修正も含まれます。デスクトップのビューアにはAndroid SDKは不要です。

ROSメッセージ生成設定の`Assets/msgbrowser_settings.asset`に重複していたWindows固有の入出力先を削除し、既存の`Other 3rd Party Series/msgs`と`Other 3rd Party Series/RosMessages`を使用するようにしました。Linuxでバックスラッシュを名前に含む別フォルダーへ同じC#が生成される問題を防ぎます。

`--open-only`を付けると、Play Modeに入らずにプロジェクトだけ開きます。既にこのプロジェクトのEditorが起動している場合、通常起動は既存Editorに指定した設定で開始を要求します。`--open-only`の場合は開始を要求しません。再生中の設定切替は行わず、停止してから再実行します。

`--open-only`はGPU描画で編集画面を開きます。このUbuntuでは、既存のデスクトップ起動と同じBox64ラッパーと既定のworker設定を使い、SampleScene、Hierarchy、Scene、Inspector、下部のProjectとメニュー操作を確認しました（2026-11-20）。以前の編集起動で追加していたソフトウェア描画やARM64用設定は、`--open-only`には自動適用しません。記録は `Logs/unity-editor-workspace/verification.json` にあります。

通常のPlay起動では、`glxinfo`でOpenGL 4.3未満かつCompute Shader拡張がないと確認できた場合、元の掘削・熱侵食を維持するためMesaのソフトウェア描画を自動選択します。現在のARM64 VMのGPUはこの条件に該当します。EditorをGPU描画で開いた後にPlayボタンを押しても描画方式は切り替わらないため、Compute Shaderを使う実行はPlayerで行うか、Editorを終了してから次のコマンドで起動してください。ソフトウェア描画では動作速度が低下し、このPCでのEditor操作は未確認です。

```bash
./operasimvr-unity-ubuntu --software-rendering
```

実行ファイルは`UNITY_EDITOR`で指定できます。`--open-only`では既存のデスクトップ起動と同じ`~/.local/bin/unity-editor-2022.3.4`を優先します。通常起動・ビルドでは、ARM64のこのPCに保存した比較用ラッパー`~/.local/opt/operasimvr-unity/bin/unity-editor-candidate-6f0df3e`を優先します。該当するラッパーがなければ`~/.local/bin/unity-editor-2022.3.4`、次に`~/Unity/Hub/Editor/2022.3.4f1/Editor/Unity`を探します。追加の引数はUnityに渡します。

## このPCのARM64用コンパイル環境

Editor本体と一部の補助プログラムには既存のBox64用ラッパーを使います。一方、ILPP RunnerはBox64上で停止したため、元のDLLをコピーし、Unity付属のCecilでx64用ReadyToRunコードを除いたILを生成して、ARM64の.NET 6.0.36で実行します。元のDLLと実行ファイルは保持しています。[ReadyToRunは対象の実行環境に依存する形式です](https://learn.microsoft.com/en-us/dotnet/core/deploying/ready-to-run)。

このPCに導入済みの`~/.local/opt/operasimvr-unity`配下のARM64 .NET・RoslynとUnityのインストールが前提です。Unityを終了した状態で、以下のコマンドからILPPの設定を再生成できます。Unity 2022.3.4f1の確認済みのDLL・依存定義以外には適用しません。

```bash
python3 tools/prepare-unity-arm64-ilpp.py
```

生成先は`~/.local/opt/operasimvr-unity/ilpp-arm64-portable`です。以前の起動ラッパーはUnityの`Data/Tools/ilpp/Unity.ILPP.Runner/Unity.ILPP.Runner.box64-wrapper`に保持します。一般的なUbuntu x86_64環境では、このARM64用処理は不要です。

`BuildPlayerDataGenerator.exe`も、このPCのARM64版.NETで実行します。Box64内でこのツールが異常終了すると、Playerの出力が完了してもビルドコマンドが失敗扱いになることがありました。既定のARM64ラッパーを使う`--build`では、`tools/prepare-unity-arm64-build-tools.py`が確認済みのUnity 2022.3.4f1のツールと既存ラッパーのSHA256を検証し、ネイティブ実行への振り分けを準備します。手動で準備する場合もUnityを終了してから実行してください。

```bash
python3 tools/prepare-unity-arm64-build-tools.py
```

変更前のラッパーはUnityの`Data/Tools/netcorerun/netcorerun.before-operasimvr-builddata`に保存します。再実行は変更を加えずに終了し、未対応の版や既存の手編集を検出した場合は書き換えを拒否します。

ARM64の通常起動・ビルドでは、プロジェクトの起動スクリプトが`BOX64_DYNAREC_INTERP_SIGNAL=0`と`GC_DISABLE_INCREMENTAL=1`を設定します。後者は[BDWGCの増分GCを無効にする環境変数](https://github.com/bdwgc/bdwgc/blob/master/docs/environment.md)で、GC自体は動作しますが、一回の停止時間が長くなる可能性があります。この設定と比較用Box64の組み合わせで、全アセットのインポートが完了しました。


ARM64のPlayerビルドでは、起動スクリプトが`MONO_ENV_OPTIONS`に`--interp`を追加します。Burst 1.8.12のコマンドライン用Mono JITはBox64上でクラッシュしたため、コンパイラーのホストだけをインタープリターで動かします。Burstによる最適化済みx86_64コードの生成は有効のままです。SSE2／AVX2向け44入口の実AOT処理を確認し、JIT＋Box64のコード変更追跡設定で生成したライブラリともSHA256が一致しました。Intel／AMDのビルドや、通常のEditor／Player起動にはこの追加設定を行いません。

さらに`BOX64_DYNAREC_NOHOSTEXT=lrcpc,lrcpc2,lrcpc3`でCPU拡張を使わない設定にすると、シーンを開く際のSIGBUSを通過し、Play Modeまで到達しました。この設定も起動スクリプトと比較用ラッパーに保存しています。URDF Importerが要求したx86_64版`libminizip.so.1`も、このPCの既存のプライベートライブラリ領域へ追加しました。

比較用の実行ファイルと起動ラッパーは、一時ディレクトリーとは別に以下へ保存しました。導入済みの通常版Box64は保持しています。

```text
~/.local/opt/operasimvr-unity/bin/box64-candidate-6f0df3e
~/.local/opt/operasimvr-unity/bin/unity-editor-candidate-6f0df3e
```

同じ画面なしのインポートを再現するには、Unityを終了した状態でプロジェクトのルートから実行します。

```bash
~/.local/opt/operasimvr-unity/bin/unity-editor-candidate-6f0df3e \
  -projectPath "$PWD" -batchmode -nographics -quit -job-worker-count 2 \
  -logFile "$PWD/Logs/ubuntu-unity-current-gc-batch.log"
```

## ROS接続

### OperaSimPhysXの実データをZX200ビューアで見る

PhysX側の10000番への接続はそのまま使用します。VR側を同じ10000番へ追加接続すると、この版のROS TCP EndpointではUnity向けの送信キューが新しい接続へ切り替わるため、VRには別の10001番を使用します。

今回の受信専用経路は次のコマンドで再現できます。PhysXと同じROS Domainを指定し、別のターミナルで中継とPlayerを起動します。出力先は未使用のディレクトリー名にします。

```bash
source /opt/ros/humble/setup.bash
export ROS_DOMAIN_ID=10
python3 tools/ros-viewer-bridge.py --port 10001 --output Logs/physx-live-new
```

```bash
OPERASIMVR_MODE=viewer OPERASIMVR_ROS_IP=127.0.0.1 OPERASIMVR_ROS_PORT=10001 \
  ./operasimvr-player-ubuntu --software-rendering
```

GPUを使用する場合は`--software-rendering`を省略します。`tools/ros-viewer-bridge.py`はPoseStampedとJointStateをROSで購読し、受信したCDRバイト列を変更せず転送します。VRからの建機指令・状態送信は転送しません。`/clock`は観測・記録用に購読します。10001番で別のEndpointを起動している場合は、同じポートを重ねて使わないでください。

明示的な`viewer`モードのLinux Playerでは、最初の位置データを受信した時点で機体を見渡す視点へ一度だけ移動します。データが届く前に仮の位置を作らず、元のシーンも保存し直しません。ヘッドセット稼働時にはこの視点移動を行いません。

### 通常のROS TCP Endpointを使用する場合

送信側OperaSimPhysXの既存Endpoint **10000番は維持**します。`--mode viewer`の例では別のEndpointを使います。必要な場合、別のターミナルで実行します。Unityの起動スクリプトはEndpointを自動起動しません。

```bash
source /opt/ros/humble/setup.bash
source /home/kasahara/ros2_ws/install/setup.bash
export ROS_DOMAIN_ID=10
ros2 run ros_tcp_endpoint default_server_endpoint --ros-args \
  -r __node:=UnityVRViewerEndpoint \
  -p ROS_IP:=127.0.0.1 -p ROS_TCP_PORT:=10001
```

ZX200の受信トピックは以下です。

| トピック | 型 | 用途 |
| --- | --- | --- |
| `/zx200/base_link/pose` | `geometry_msgs/msg/PoseStamped` | 車体の位置・姿勢（`world`座標） |
| `/zx200/joint_state` | `sensor_msgs/msg/JointState` | 旋回・ブーム・アーム・バケットの関節角 |

## 元のキーボード操作

降車状態（GetOff）でGameビューを選択すると、元の`PlayerKeyMover`の操作を使えます。

| キー | 操作 |
| --- | --- |
| ↑ / ↓ | 前進 / 後退 |
| ← / → | 左右に旋回 |
| 左Shift + ↑ / ↓ | 上昇 / 下降 |
| 左Shift + ← / → | 左右へ平行移動 |
| 右Shift + ↑ / ↓ | 視線を上下に回転 |

## 初期検証の記録（今回の再検証前）

以下は移植初期の障害と比較実験の記録です。現在の到達点は冒頭の状態を参照してください。今回のEditor再検証ログは`Logs/ubuntu-port-viewer.log`、最初のLinux Playerビルドのエラーは`Logs/ubuntu-port-linux-build.log`、修正後のビルドは`Logs/ubuntu-port-linux-build-fixed.log`です。最初のPlayerビルドで判明したUnitySensorsのEditor参照は、上記のパッチで修正しました。

未解決の問題として、Box64上のUnity本体が初回インポート中に終了する現象を確認しています。導入済みのBox64 v0.4.4（`2f130fa`）では、COLLADAモデルの読み込み中に終了コード139（SIGSEGV）になります。ソフトウェア描画、計算精度・メモリ同期を重視する設定、画面を出さない`-batchmode -nographics`でも再発しました。GDBではBox64の`emu2mctx`・`my_sigactionhandler_oldcode_64`内で例外処理が再帰的に失敗していることを確認しましたが、最初の異常を起こした原因は未特定です。

比較用に公式Box64の`6f0df3efe29aedae73e516277ce9cb2378779960`を別途ビルドしたところ、同じ`mst110cr-crawler.dae`のインポートは4.1秒で成功しました。その後のUnitySensorsのLiDAR設定アセットでMonoのネイティブクラッシュが再発し、`BOX64_DYNAREC_INTERP_SIGNAL=0`のみでは`Tele-15.asset`で終了コード134（SIGABRT）になりました。`GC_DISABLE_INCREMENTAL=1`を加えた実行では全インポートが完了し、終了コード0を確認しました。元のモデル、モデルのインポート設定、シーンは変更していません。

続く画面ありの起動では、llvmpipeによるOpenGL 4.5の初期化とC#の再コンパイルに成功しましたが、`Opening scene 'Assets/Scenes/SampleScene.unity'`の後に終了コード135（SIGBUS）で停止しました。EditorヘルパーのPlay Mode開始とROSへの接続には到達していません。

CPU拡張を使わない設定を加えた次の起動では、元のシーンでPlay Modeに入りました。状態JSONでZX200・4関節・元のカメラの存在、Compute Shader対応を確認し、Endpoint側でも`/zx200/base_link/pose`と`/zx200/joint_state`の購読登録を確認しました。ただし状態JSONは`frameCount: 1`、受信数は両方0のまま更新されず、画面は黒い状態でした。アセットインポートWorkerの接続タイムアウトが繰り返され、メモリ5.8GiBとスワップ4GiBがほぼ埋まる状態も確認しました。これだけで停止原因をメモリ不足に断定してはいません。

この時点の実行時エラーは、UnitySensorsの`LiDARSensor.Init`でScanPatternが未設定のNullReferenceExceptionと、URDF Importerの`UrdfRobot.CreateCollisionExceptions`のNullReferenceExceptionでした。後者の空の衝突除外ペアは今回修正しました。ScanPatternは走査仕様が不明なため未設定のままです。この初期検証では表示・追従は未確認で、検証用Editorを終了しました。

終了要求としてSIGTERMを送信した後にもネイティブ例外が発生し、プロセスは139で終了しました。正常終了が確認できたのは前述の画面なしインポートです。追加した`libminizip.so.1`の実ロードは、追加後の再起動でまだ確認していません。

初回とソフトウェア描画時のログは`Logs/ubuntu-unity-editor-first-import.log`と`Logs/ubuntu-unity-editor-software-import.log`、保守的な設定でのログは`Logs/ubuntu-unity-editor-conservative-import.log`、GDBの記録は`Logs/ubuntu-unity-gdb.log`にあります。画面を出さないインポートのログは`Logs/ubuntu-unity-batch-import.log`、比較用Box64のログは`Logs/ubuntu-unity-current-batch-import.log`と`Logs/ubuntu-unity-current-signal-batch.log`です。

正常終了した全インポートは`Logs/ubuntu-unity-current-gc-batch.log`、画面ありでのSIGBUSは`Logs/ubuntu-unity-editor-current-viewer.log`、追加のGDB記録は`Logs/ubuntu-unity-current-gdb.log`にあります。

Play Mode到達後のログは`Logs/ubuntu-unity-editor-nohostext-viewer.log`、1フレーム目の状態は`Logs/ubuntu-unity-status-first-frame.json`、Endpointの購読登録は`Logs/ubuntu-ros-endpoint.log`に保存しています。

2026-11-19、ARM64版ILPP Runnerによる元プロジェクトのC#コンパイル成功を確認しました。ILPP設定の再生成・再実行・ARM64プロセスのソケット待受、Meta XR修正の再適用・旧キャッシュの復元・未確認版と手編集済みファイルの拒否も確認しています。

実ROSデータは前回の確認で8秒間にPoseを133件、JointStateを200件受信しました。今回の再開後も両トピックの存在を確認しました。Unity上での表示・関節追従・描画機能は未確認です。Unity Editorの起動やROSの受信だけで、すべての機能が動作確認済みとは扱いません。
