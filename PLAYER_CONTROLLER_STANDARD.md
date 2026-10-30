# Quy Chuẩn Nhân Vật Chính & Xử Lý Controller Thừa Sau Khi `git pull`

Tài liệu này là **Nguồn Sự Thật Duy Nhất (Single Source of Truth)** cho nhân vật chính của dự án **Hoa trên đất đỏ**. Mọi thành viên trong nhóm và AI Agent **BẮT BUỘC** đọc và tuân thủ tài liệu này trước khi chỉnh sửa nhân vật hoặc ngay sau khi thực hiện `git pull` / `git merge`.

---

## 1. Nhân Vật Chính & Bộ File Điều Khiển Chuẩn (Canonical Player Stack)

Toàn bộ dự án chỉ sử dụng **DUY NHẤT** một cấu hình nhân vật chính dưới đây (chuẩn theo object `02_Gameplay/PF_Player_Minh` trong `Assets/Scenes/New Scene 1_Overhaul.unity`):

| Thành phần | Đường dẫn chuẩn duy nhất | Vai trò |
| :--- | :--- | :--- |
| **Prefab Nhân vật chính** | `Assets/Prefabs/Characters/PF_Player_Minh.prefab` | Prefab gốc gắn trên `02_Gameplay/PF_Player_Minh` (`Scale = 1.15, 1.15, 1.15`, `Tag = Player`). |
| **3D Model & Avatar** | `Assets/TripoModels/Boy_Character/Boy_Character.fbx` (`Boy_CharacterAvatar`) | Mesh `tripo_node_518b8c94`, Rig Humanoid chuẩn có sẵn sub-clip `climb`. |
| **Animator Controller (`.controller`)** | **`Assets/Animations/Controllers/AC_Player_An.controller`** | **Animator Controller DUY NHẤT** cho nhân vật chính (cả 2.5D và 3D). Bắt buộc bật `iKPass = true` ở Layer 0 và `applyRootMotion = false`. |
| **Script Điều khiển Gameplay (2.5D)** | `Assets/Scripts/Gameplay/PlayerController.cs` | Điều khiển di chuyển ngang, nhảy, chạy, ngồi/bò, bám gờ, đu dây, kéo thùng, mang/ném vật. |
| **Các Handler phụ trợ (SOLID)** | `Assets/Scripts/Gameplay/LedgeGrabDetector.cs`<br>`Assets/Scripts/Gameplay/Player/PlayerLedgeClimber.cs`<br>`Assets/Scripts/Gameplay/Player/PlayerPostureHandler.cs`<br>`Assets/Scripts/Gameplay/Player/PlayerRopeHandler.cs`<br>`Assets/Scripts/Gameplay/Player/PlayerInteractionHandler.cs`<br>`Assets/Scripts/Gameplay/Player/PlayerAudioController.cs` | Tách biệt từng cơ chế vật lý, raycast bám gờ, quỹ đạo leo vách, collider ngồi/bò và âm thanh bước chân. |
| **Script Điều khiển Animation & IK** | **`Assets/Scripts/Gameplay/AnAnimationController.cs`** | **Script Animation DUY NHẤT** cho Player 2.5D: điều phối State, khóa In-Place Hips X/Z, IK bám tay gờ đá/dây thừng, khóa bàn chân khi leo gờ và LookAt IK khi ngồi. |
| **Script Điều khiển 3D (chỉ cho map 3D)** | `Assets/Scripts/Test3D/Player3DController.cs` | Dùng cho nhân vật `An_Player` (`Boy_Character_Model.prefab` + `CharacterController` + `AC_Player_An.controller`) trong `Assets/Scenes/CaMau_1954_Village.unity`. |

### Quy chuẩn các Animation Clip quan trọng trong `AC_Player_An.controller`:
- **`CrouchIdle`** (Ngồi yên): `Assets/Animations/Hero/Stealth/Hero_Stealth_Crouch_Idle_Loop.fbx` (`Based Upon Feet`, `heightFromFeet = true`, `iKOnFeet = true`).
- **`Crouch`** (Ngồi đi): `Assets/Animations/Hero/Stealth/Hero_Stealth_Crouch_Walk_Fwd_IP.fbx` (`Based Upon Feet`, `heightFromFeet = true`, `iKOnFeet = true`).
- **`Crawl`** (Bò trườn): `Assets/Animations/Hero/Stealth/Hero_Stealth_Crawl_Fwd_IP.fbx` (`Based Upon Feet`, `loopTime = true`, `iKOnFeet = true`).
- **`Climb`** (Bám gờ & leo lên đỉnh vách): Sub-clip `climb` bên trong `Assets/TripoModels/Boy_Character/Boy_Character.fbx` (`loopTime = false`, `iKOnFeet = false`). **TUYỆT ĐỐI KHÔNG** gán `Hero_Traverse_Braced_Hang_To_Crouch.fbx` vào state `Climb` (gây lỗi gập cổ chui vào áo và quỳ lơ lửng ngoài mép đá).
- **TUYỆT ĐỐI KHÔNG** sinh clip `.anim` thủ công (`An_Crouch_Idle.anim`, `An_Crouch_Walk.anim`) đè lên state `CrouchIdle` và `Crouch`.

---

## 2. Danh Sách Các Controller Bị Thừa (CẤM DÙNG / PHẢI XÓA NẾU XUẤT HIỆN LẠI)

Các file dưới đây là tàn dư từ các nhánh thử nghiệm cũ. Nếu xuất hiện lại sau khi `git pull`, chúng sẽ gây xung đột Animator, mất `iKPass`, hoặc làm sai lệch động tác Ngồi / Leo tường:

### 2.1. Các file `.controller` thừa của Player (PHẢI LOẠI BỎ):
1. `Assets/TripoModels/Boy_Character/Boy_AnimatorController.controller` *(bản sao trùng lặp với `AC_Player_An.controller`)*
2. `Assets/Animations/Controllers/AC_Player3D_An.controller` *(controller 4 state thiếu hụt)*
3. `Assets/Animations/Controllers/AC_Player3D_Interaction.controller` *(controller test cũ 2 state)*
4. `Assets/Animations/Controllers/AC_Hero_GapBung.controller` *(controller test gập bụng)*
5. `Assets/Animations/Controllers/AC_Hero_GapBungPlayer.controller` *(controller test gập bụng)*

> **LƯU Ý PHÂN BIỆT (KHÔNG XÓA CONTROLLER CỦA NPC / MENU CHILL):**
> Các controller dành cho NPC và cảnh Menu nền như `AC_Lana_LivingVillager.controller`, `AC_NPC_ElderlyMan.controller`, `AC_NPC_Female.controller`, `AC_NPC_Male.controller`, `AC_NPC_Social_Waving.controller`, `AC_ChillMenu_An.controller`, `AC_ChillMenu_NPC.controller` **KHÔNG PHẢI** controller điều khiển Player nên được giữ nguyên.

### 2.2. Các Script & Prefab điều khiển Player bị trùng lặp (PHẢI LOẠI BỎ):
1. `Assets/Scripts/Gameplay/MeshyBoyAnimationController.cs` *(bản sao cũ của `AnAnimationController.cs`, tự động ép gán `Boy_AnimatorController.controller`)*
2. `Assets/Scripts/Gameplay/GapbungPlayerSetup.cs` *(script test cũ tự gắn `PlayerController` đè lên nhân vật)*
3. `Assets/Scripts/Editor/SetupChaRiggedPlayerMenu.cs` *(menu tạo lại `AC_Hero_GapBungPlayer.controller`)*
4. `Assets/Prefabs/Characters/PF_Player_MeshyBoy.prefab` *(prefab trùng lặp với `PF_Player_Minh.prefab`)*

### 2.3. Các Animation Clip bị thừa / lỗi / trùng lặp (PHẢI LOẠI BỎ):
1. `Assets/Animations/Hero/Custom/An_Crouch_Idle.anim` & `An_Crouch_Walk.anim` *(clip `.anim` sinh thủ công cũ gây lỗi lún chân khi ngồi; giữ lại 3 clip đu dây `An_Rope_*.anim`)*
2. `Assets/TripoModels/Boy_Character/Hero_Loco_Idle_Loop_IP.anim`, `Hero_Loco_Walk_Fwd_IP.anim`, `Hero_Loco_Run_Fwd_IP.anim`, `Hero_Traverse_Ledge_ClimbUp.anim` *(4 bản `.anim` trích xuất trùng lặp nặng ~50.4 MB từ thời `Boy_AnimatorController` cũ)*
3. `Assets/Animations/Hero/Stealth/Crouch Walk.fbx`, `Crouched Walking.fbx`, `Crouched Walking (1).fbx`, `Crouching Idle (1).fbx` *(các bản tải thô / trùng lặp `(1)` trên Mixamo của `Hero_Stealth_Crouch_Idle_Loop.fbx` và `Hero_Stealth_Crouch_Walk_Fwd_IP.fbx`)*
4. `Assets/Animations/Hero/Traverse/Hero_Traverse_Braced_Hang_To_Crouch.fbx` *(clip leo tường Mixamo cũ gây lỗi gập cổ chui vào áo; đã thay bằng `Boy_Character.fbx :: climb`)*
5. `Assets/Animations/Hero/Y Bot.fbx` & `Assets/MeshyImports/Boy Character Rig_20260926_073036/Meshy_AI_Boy_Character_Rig_biped_Animation_Archery_Shot_2_withSkin.fbx` *(dummy T-pose Mixamo và clip AI test bắn cung không thuộc bối cảnh dự án)*

---

## 3. Quy Trình Xử Lý Chuẩn Khi `git pull` Về Có Dính File Controller Thừa

Mỗi khi thực hiện `git pull` hoặc gộp nhánh từ thành viên khác mà thấy xuất hiện lại bất kỳ file nào trong **Mục 2**, hãy xử lý theo đúng 3 bước sau:

### Bước 1: Kiểm tra xem có Script C# nào vừa pull về đang gọi tên class/path thừa không
- Nếu có file C# gọi `MeshyBoyAnimationController` -> đổi toàn bộ sang `AnAnimationController`.
- Nếu có file C# gọi `Boy_AnimatorController.controller`, `AC_Player3D_An.controller`, `AC_Hero_GapBung*.controller`, hoặc `AC_Player3D_Interaction.controller` -> đổi đường dẫn về `"Assets/Animations/Controllers/AC_Player_An.controller"`.
- Nếu có Editor script dùng `importer.defaultClipAnimations` trong `[InitializeOnLoadMethod]` -> phải đọc `importer.clipAnimations` trước để không làm mất cấu hình `Based Upon Feet` của `CrouchIdle` và `Crouch`.

### Bước 2: Chạy Công Cụ Dọn Dẹp & Đồng Bộ Tự Động Trong Unity Menu (1 Click)
Trong Unity Editor, bấm menu:
> **`Tools -> Hoa Tren Dat Do -> Enforce Canonical Player Controller (Post-Pull Cleanup)`**
*(Method: `HoaVaMau.Editor.FixCrouchLocomotionSync.EnforceCanonicalPlayerAndCleanRedundant()` trong `Assets/Editor/FixCrouchLocomotionSync.cs`)*

Công cụ này sẽ tự động thực hiện toàn bộ quy trình an toàn:
1. Quét các Prefab và Scene phụ (tuyệt đối **bảo toàn nguyên trạng** `Assets/Scenes/New Scene 1_Overhaul.unity` theo `AGENTS.md` Mục 4.3), tự động chuyển (remap) mọi GUID tham chiếu từ các `.controller` thừa sang GUID chuẩn của `AC_Player_An.controller` (`d77968aa378c4e84fb44d1a1bd7a4dc6`) để không bao giờ bị `Missing (Runtime Animator Controller)`.
2. Xóa sạch toàn bộ các file `.controller`, `.cs`, `.prefab` thừa liệt kê ở Mục 2 cùng file `.meta` tương ứng thông qua `AssetDatabase.DeleteAsset`.
3. Gỡ bỏ mọi `Missing Script` tồn đọng trên Prefab nhân vật.
4. Chuẩn hóa lại `Based Upon Feet` cho `CrouchIdle` / `Crouch` / `Crawl`, gán chuẩn clip `Boy_Character.fbx :: climb` vào state `Climb`, bật `iKPass = true` trên `AC_Player_An.controller`, và đồng bộ `PF_Player_Minh.prefab`.

### Bước 3: Kiểm tra xác nhận sau khi dọn
1. Mở `Console` xác nhận **0 lỗi biên dịch (0 Compilation Errors)** và **0 lỗi Runtime**.
2. Kiểm tra `02_Gameplay/PF_Player_Minh` trong `New Scene 1_Overhaul.unity`:
   - `Animator.Controller` = `AC_Player_An`
   - `Animator.Avatar` = `Boy_CharacterAvatar`
   - Các script đang bật: `PlayerController`, `AnAnimationController`, `LedgeGrabDetector`, `PlayerAudioController`.
