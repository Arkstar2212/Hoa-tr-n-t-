# Unity MCP & Antigravity Speed & Workflow Rules

## 1. Luật Tốc độ & Giới hạn Thời gian (Speed Guardrails)
- **CẤM TUYỆT ĐỐI** gọi các MCP tool lắt nhắt từng object riêng lẻ (như `set_transform`, `instantiate`, `find_object` lặp lại nhiều lần). Mọi tác vụ dàn dựng Scene, sửa asset hoặc xử lý hàng loạt **bắt buộc phải viết gom vào 1 file C# Editor Script duy nhất** trong `Assets/Editor/` (hoặc `Assets/Scripts/Editor/`) và gọi chạy qua `[MenuItem("Tools/...")]`.
- **CẤM DÙNG** lệnh Terminal/PowerShell/Bash bên ngoài để tìm kiếm file hoặc quét thư mục (dễ gây treo terminal). Thay vào đó, dùng `AssetDatabase.FindAssets()` trực tiếp bên trong C# Script của Unity.
- Toàn bộ quá trình từ lúc nhận lệnh đến khi hoàn thành Scene hoặc Script **không được vượt quá 1 đến 2 phút**.

## 2. Quy tắc Self-Correction (Tự kiểm tra bằng hình ảnh)
- Khi cần chụp ảnh màn hình để tự kiểm tra (Vision), chỉ render ở độ phân giải tối đa **720p (1280x720)** hoặc Panorama **1920x600**, định dạng `.jpg` (quality 80) để tối ưu hóa tốc độ đọc file và token.

## 3. Nguyên tắc Bảo Toàn Scene Overhaul (Chỉ Thêm Mới - Additive Only)
- **BẢO TỒN NGUYÊN TRẠNG SCENE**: Tuyệt đối không xóa, di dời, ghi đè hoặc làm biến đổi cấu trúc, vị trí, cơ chế hiện có trong `New Scene 1_Overhaul.unity`. Toàn bộ scene hiện trạng là chuẩn mực của người dùng.
- **CHỈ THÊM MỚI (ADDITIVE ONLY)**: Mọi nhiệm vụ dàn dựng tiếp theo chỉ được phép bổ sung/làm thêm các thành phần mới (new props, foliage, VFX, audio, triggers...) vào các nhóm/parent riêng biệt rõ ràng mà không được tác động tiêu cực đến các vật thể gốc đã có sẵn.
