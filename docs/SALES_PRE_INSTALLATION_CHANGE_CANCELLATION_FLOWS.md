# Mô hình luồng V1 điều chỉnh và hủy đơn trước lắp đặt

Tài liệu này minh họa business target trong `SALES_PRE_INSTALLATION_CHANGE_CANCELLATION_BUSINESS_SPEC.md`.

## 1. Mô hình người dùng tối giản

```mermaid
flowchart TD
    A["Nháp"] -->|"Xác nhận và xuất kho"| B["Đã xác nhận - Chờ lắp"]
    A -->|"Hủy đơn Nháp"| C["Đã hủy"]

    B --> D{"Phát sinh hành động nào?"}
    D -->|"Manager điều chỉnh"| E["Revision nội bộ"]
    E -->|"Apply thành công"| B
    E -->|"Apply thất bại hoặc bỏ revision"| B

    D -->|"Manager hủy"| C
    C --> F["Hiển thị việc còn chờ: hoàn kho / hoàn tiền"]

    D -->|"Kỹ thuật lắp máy đầu tiên"| G["Giữ trạng thái Sales hiện tại"]
    G --> H["Khóa Điều chỉnh và Hủy"]
```
Người dùng chỉ thấy `Nháp`, `Đã xác nhận - Chờ lắp`, `Đã hủy` và thông tin khóa/việc còn chờ. Revision và cancellation process là dữ liệu nội bộ.

## 2. Luồng điều chỉnh theo delta

```mermaid
flowchart TD
    A["Manager chọn Điều chỉnh đơn"] --> B{"Đã có máy Installed?"}
    B -->|"Có"| C["Từ chối và nêu ngày/máy đã lắp"]
    B -->|"Chưa"| D{"Có process xung đột?"}
    D -->|"Có"| E["Mở process hiện tại"]
    D -->|"Không"| F["Tạo revision từ bản hiệu lực"]
    F --> G["Manager chỉnh dữ liệu"]
    G --> H["So sánh Original và Revision tại database"]
    H --> I["Phân loại impact theo từng dòng"]
    I --> J["Kiểm tra prerequisite theo impact"]
    J --> K{"Tất cả điều kiện đạt?"}
    K -->|"Không"| L["Giữ đơn cũ; hiển thị việc cần xử lý"]
    K -->|"Có"| M["Apply delta atomic"]
    M --> N["Carry-forward payment và tính lại công nợ"]
    N --> O["Đồng bộ phần máy chờ lắp bị ảnh hưởng"]
    O --> P["Revision mới có hiệu lực"]
```

## 3. Ma trận xử lý impact

```mermaid
flowchart LR
    A["Một thay đổi trong revision"] --> B{"Loại thay đổi"}
    B -->|"Giá / ghi chú"| C["Không động kho"]
    B -->|"Tăng quantity"| D["Issue phần tăng"]
    B -->|"Giảm quantity"| E["Reverse phần giảm"]
    B -->|"Đổi Product"| F["Reverse Product cũ + Issue Product mới"]
    B -->|"Thêm dòng"| G["Issue dòng mới"]
    B -->|"Xóa dòng"| H["Reverse dòng bị xóa"]
    B -->|"Không đổi"| I["Giữ allocation và cost snapshot"]

    D --> J{"Đủ tồn FIFO?"}
    E --> K{"Hàng đủ điều kiện nhập lại?"}
    F --> J
    F --> K
```

## 4. Payment carry-forward

```mermaid
flowchart TD
    A["Revision tính NewTotal"] --> B["Giữ nguyên NetPaid của Order"]
    B --> C{"So sánh NetPaid và NewTotal"}
    C -->|"NetPaid < NewTotal"| D["Remaining = NewTotal - NetPaid"]
    C -->|"NetPaid = NewTotal"| E["Đã thanh toán đủ"]
    C -->|"NetPaid > NewTotal"| F["Overpaid / Tiền cần hoàn"]
    F --> G["Kế toán tạo Refund khi thực trả"]
```

Không hoàn tiền rồi thu lại chỉ vì revision thay đổi tổng đơn.

## 5. Hủy có hiệu lực sớm

```mermaid
flowchart TD
    A["Đã xác nhận - Chờ lắp"] -->|"Manager Hủy đơn + lý do"| B{"Đã có máy Installed?"}
    B -->|"Có"| C["Từ chối hủy"]
    B -->|"Chưa"| D["Hủy có hiệu lực ngay"]

    D --> E["Sales hiển thị Đã hủy"]
    D --> F["Chặn installation và revision"]
    D --> G["Loại khỏi active sale"]
    D --> H["Vô hiệu hóa máy chờ lắp"]

    D --> I["Nhánh hoàn hàng nếu cần"]
    D --> J["Nhánh payment nếu cần"]
    I --> K{"Còn việc tồn?"}
    J --> K
    K -->|"Có"| L["UI hiển thị việc còn chờ"]
    K -->|"Không"| M["Đóng cancellation process nội bộ"]
```

## 6. Hai nhánh sau hủy

```mermaid
sequenceDiagram
    actor M as Manager
    participant S as Sales
    participant T as Kỹ thuật
    participant W as Kho
    participant F as Kế toán
    participant C as CustomerCare

    M->>S: Hủy đơn + lý do
    S->>S: Kiểm tra chưa có máy Installed
    S->>T: Chặn xác nhận lắp
    S->>C: Cancel/Supersede máy chờ lắp
    S-->>M: Đơn Đã hủy có hiệu lực

    par Xử lý hàng
        S->>W: Tạo việc hoàn hàng nếu cần
        W->>W: Xác nhận hàng quay về và tình trạng
        alt Hàng đạt
            W->>S: Post reversal theo allocation/cost gốc
        else Hỏng hoặc thiếu
            W->>S: Ghi nhận ngoại lệ, không tăng Available
        end
    and Xử lý tiền
        S->>F: Tạo việc xử lý tiền nếu có
        alt Bản ghi sai, tiền chưa thực thu
            F->>F: Void có lý do
        else Tiền đã thực thu
            F->>F: Tạo Refund
        end
    end

    S->>S: Đóng process khi không còn việc tồn
```

## 7. Sai khách hàng

```mermaid
flowchart TD
    A["Manager phát hiện sai Customer sau Confirm"] --> B["Không sửa CustomerId"]
    B --> C["Hủy đơn cũ"]
    C --> D["Chọn Hủy và tạo đơn mới"]
    D --> E["Copy Warehouse, Lines, Quantity, Price, Note"]
    E --> F["Không copy Payment, Inventory, Asset, Status, technical IDs"]
    F --> G["Đơn mới ở trạng thái Nháp"]
```

## 8. Khóa tranh chấp

```mermaid
flowchart LR
    A["Manager mở/apply revision"] --> L["Khóa theo SalesOrderId"]
    B["Manager hủy đơn"] --> L
    C["Kỹ thuật xác nhận lắp"] --> L
    L --> D{"Đọc trạng thái mới nhất trong transaction"}
    D -->|"Có revision/cancellation"| E["Chặn installation"]
    D -->|"Đã có máy Installed"| F["Chặn revision và hủy"]
    D -->|"Không xung đột"| G["Cho phép đúng một hành động"]
```

## 9. Ranh giới V1

```mermaid
flowchart TD
    A{"Đơn có sản phẩm là máy?"}
    A -->|"Không"| B["Confirmed bất biến như hiện tại"]
    A -->|"Có"| C{"Có máy Installed?"}
    C -->|"Có"| D["Khóa Điều chỉnh và Hủy toàn đơn"]
    C -->|"Chưa"| E["Manager có thể Điều chỉnh/Hủy"]
    D --> F["Các máy còn lại tiếp tục installation"]
```
