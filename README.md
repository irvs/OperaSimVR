# OperaSimVR(PhysX)
Simulator on Unity + PhysX communicating with ROS

<<<<<<< Updated upstream
=======
## コーン自動配置

フィールド上で頂点と接続を指定し、線や閉じたエリアの境界にコーンを配置できます。内周・外周を別グループにすると、二重の囲いも作れます。

### Gameビュー / Linux Playerでの操作

1. Unityで `SampleScene` を開いてPlayするか、ビルド済みのLinux Playerを `./operasimvr-player-ubuntu` で起動します。
2. **F8** または右上の **コーン自動配置** を押して、俯瞰編集画面を開きます。**右ドラッグ** で移動、**ホイール** で拡大・縮小できます。
3. **地面をクリック** して頂点を追加します。各頂点に `P1`, `P2`, … のIDが自動で付きます。
4. **頂点のID表示をクリック** して選び、右側の **隣接ID** に接続先の数字を0～2個入力して **隣接関係を適用** を押します。例えば `P1` と `P2` を接続するには、`P1` の隣接ID欄に `2` を入力します。接続は双方向になり、蛍光色の線が表示されます。空欄は未接続です。
5. 接続に応じてグループと間隔フォームが自動生成されます。端点のある接続は「線」、閉じた接続は「エリア」、未接続の頂点は「孤立点」です。
6. **グループごとの間隔 (m)** を入力し、**コーンを配置 / 更新** を押します。各フォームの「適用」を個別に押さなくても、入力中の間隔が反映されます。角・端点にもコーンを置き、辺の最後の余りは指定間隔以下になります。
7. **F8** または **Esc** で編集を終了し、元のカメラに戻ります。

### 二重の囲い・折れ線を作る

内周と外周を、それぞれ独立した閉路として接続します。例えば内周を `1–2–3–4–1`、外周を `5–6–7–8–5` にすると、別グループとして異なる間隔を指定できます。内周の `P1` では隣接IDに `2` と `4` を指定します。二周目の位置と列間距離は、頂点の位置で指定してください。

折れ線は `9–10–11` のように接続し、両端はつなぎません。囲いと折れ線は同じフィールドに配置できます。

### 配置の変更・保存・読込

- 頂点のID表示を選び、**次のクリック位置へ移動** または **頂点削除** で配置線を編集できます。
- 頂点・接続・間隔を変更したら、**コーンを配置 / 更新** で再配置します。**配置を解除** はコーンだけを取り除きます。
- **配置データ (JSON)** 欄で保存先のパスを指定し、**保存** を押すと頂点・接続・グループ間隔を保存できます。**読込** で復元した後は、**コーンを配置 / 更新** を押してください。
- **Play中の変更は停止すると戻るため、必要な配置は停止前にJSON保存してください。**

すぐに試す場合は、[二重の囲い＋折れ線のサンプルJSON](docs/examples/cone-layout-double-boundary.json) の絶対パスをJSON欄に入力し、**読込 → コーンを配置 / 更新 → 配置線の全体を表示** の順に操作します。

### Sceneビューで編集・シーンに保存

Unityの **OperaSimVR → Cone Placement → 配置線を編集** を開き、**このシーンにコーン配置機能を追加** を押します。**Sceneビューでクリック編集を有効にする** を有効にすると、Sceneビューでも頂点・接続・間隔を編集できます。シーンを保存すると配置データも保存されます。Undo / Redoに対応します。

Terrainはそのまま地面として使えます。Planeやメッシュの地面にはColliderと **ConePlacementSurface** を追加してください。

詳細な仕様、動作画面、検証手順は [コーン自動配置の操作方法と検証結果](docs/CONE_PLACEMENT.md) を参照してください。

## Ubuntu

With Unity 2022.3.4f1 installed, run `./operasimvr-unity-ubuntu` to start the original scene with its saved mode, topics, and ROS settings. Use `--mode viewer` for the ZX200 ROS viewer, or `--mode simulator`, `--mode preview`, `--mode preview-and-play`, and `--mode preview-ar` for the existing modes. `--ros-ip` and `--ros-port` provide optional connection overrides. `--build` builds a Linux x86_64 player; launch it with `./operasimvr-player-ubuntu`. See [the Ubuntu setup and verification notes](docs/UNITY_UBUNTU.md) for controls, setup, and the experimental ARM64 configuration.
The Linux x86_64 player builds with Burst enabled. On the current ARM64 machine, the original terrain and machines render and the ZX200 follows loopback pose/joint data, using experimental Box64 and slow software rendering. Quest 3 Wi-Fi support uses an opt-in SteamVR/OpenVR backend (`--build --vr`, then `./operasimvr-player-ubuntu --vr`) with ALVR; headset operation and native Intel/AMD hardware still require device testing. See the verification limits in the notes.
Actual OperaSimPhysX integration was also verified: simulator drive/turn commands changed its ROS state, and the Ubuntu viewer followed that state through `tools/ros-viewer-bridge.py`. This optional observation bridge forwards the original ROS bytes and discards Player publications; it generates no pose/joint states. See the notes for the separate ports, real integration recording, and first-pose camera framing.
After building, `python3 tools/verify-ubuntu-player.py --output Logs/player-check` exercises rendering, two ROS TCP pose/joint states, and normal Player shutdown. It requires a graphical desktop and the ROS 2 Python message packages plus Pillow. See the notes for the known unconfigured LiDAR error, software rendering, and the November 20 verification results.

>>>>>>> Stashed changes
## Explanation
- This system serves as the cyber component of ros2-tms for construction(https://github.com/irvs/ros2_tms_for_construction), a cyber-physical system (CPS) designed for earthwork construction sites.
- This system is an enhanced version of OperaSim-PhysX(https://github.com/pwri-opera/OperaSim-PhysX), the official simulator of the autonomous construction technology development platform OPERA (Open Platform for Earth work with Robotics and Autonomy).
- This system has "nomal mode" and "play mode", "control mode", "prev mode". "Normal mode" is a simulator similar to OperaSim. In "Play mode," the system receives topics such as position, orientation, and joint angles from the actual machine, allowing its movements to be visualized within the server environment. In "Control mode," the system can remotely operate the actual machine by sending command topics. In "Preview mode," the system allows for remote operation of the actual machine while simultaneously performing motion simulation using the simulator's functionalities.
