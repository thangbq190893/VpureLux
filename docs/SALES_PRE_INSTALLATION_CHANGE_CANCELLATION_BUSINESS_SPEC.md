# VPureLux ERP

## Tài liệu nghiệp vụ điều chỉnh và hủy đơn bán trước lắp đặt

### 1. Trạng thái tài liệu

- Phiên bản: V1 Business Target.
- Phạm vi đánh giá: source snapshot `release-2026-08-24-warranty-notifications`.
- Trạng thái: **Đã thống nhất nghiệp vụ, chưa phải đặc tả kỹ thuật và chưa cho phép code/migration**.
- Mục tiêu: xử lý linh hoạt trường hợp Sale nhập sai hoặc khách đổi ý sau khi đơn đã xác nhận nhưng chưa lắp đặt, đồng thời không phá vỡ Sales, FIFO, thanh toán và lịch sử máy đang ổn định.

### 2. Kết luận được phê duyệt

VPureLux V1 giữ nguyên boundary hiện tại:

> **Xác nhận đơn = xác nhận và xuất kho FIFO.**

Không bổ sung Reservation, Delivery Order, Fulfillment Aggregate hoặc chuỗi 6-8 trạng thái Sales. Thay vào đó, hệ thống bổ sung một lớp xử lý nhẹ sau xác nhận gồm:

- điều chỉnh theo phần thay đổi (delta revision);
- giữ nguyên thanh toán đã ghi nhận và tính lại công nợ;
- hủy đơn có hiệu lực ngay, còn hoàn hàng/hoàn tiền là các việc cần xử lý tiếp;
- khóa điều chỉnh/hủy khi có máy đầu tiên được xác nhận lắp đặt;
- UI chỉ hiển thị trạng thái chính và các việc còn chờ.

### 3. Nguyên tắc nền

1. Không sửa trực tiếp chứng từ Sales, Inventory hoặc Payment đã post.
2. Không hard-delete giao dịch, máy chờ lắp hoặc lịch sử phát sinh.
3. Line không thay đổi phải giữ nguyên allocation FIFO và cost snapshot.
4. Chỉ phần thay đổi mới tạo issue/reversal mới.
5. Payment gắn với Order và được carry-forward qua revision.
6. `Void` khác `Refund`: tiền đã thực thu phải hoàn bằng giao dịch riêng.
7. Revision thất bại không được làm mất hiệu lực đơn hiện tại hoặc thay đổi kho một phần.
8. Quyền trên UI không thay thế kiểm tra quyền tại Application/API.
9. Mọi thao tác phải có lý do, audit, idempotency và concurrency guard.
10. Hệ thống hướng dẫn người dùng xử lý việc còn thiếu thay vì khóa cứng bằng lỗi chung.

### 4. Mô hình trạng thái người dùng

Người dùng Sales V1 chỉ cần hiểu ba trạng thái:

| Trạng thái | Ý nghĩa |
|---|---|
| Nháp | Sale được tạo và sửa theo quyền hiện tại. |
| Đã xác nhận - Chờ lắp | Đơn đã xuất FIFO và chốt snapshot; Manager có thể điều chỉnh/hủy nếu chưa có máy lắp. |
| Đã hủy | Hủy đã có hiệu lực; có thể vẫn còn việc hoàn hàng hoặc hoàn tiền. |

Không thêm `Đã lắp - Đã khóa` hoặc `Completed` vào `SalesOrderStatus` ở V1. Trên UI, hệ thống hiển thị thêm thông tin tổng hợp:

- `Đã có máy lắp - Không thể điều chỉnh/hủy`;
- `Đã hủy · Còn chờ hoàn kho`;
- `Đã hủy · Cần hoàn khách 500.000đ`;
- `Đã hủy · Đã xử lý xong`.

Các quy trình nội bộ như active revision, cancellation process, stock return hoặc refund không được biến thành một loạt trạng thái Sales cho người vận hành.

### 5. Vai trò và quyền

| Vai trò | Hành động |
|---|---|
| Sale | Tạo/sửa đơn Nháp; không điều chỉnh/hủy đơn đã xác nhận. |
| Manager | Mở và áp dụng revision; hủy đơn; bắt buộc nhập lý do. |
| Kho | Xác nhận hàng đã quay về và tình trạng được phép nhập lại. |
| Kế toán/Thu quỹ | Void bản ghi sai hoặc ghi nhận Refund cho tiền thực thu. |
| Kỹ thuật | Xác nhận lắp đặt; bị chặn khi đơn có revision/cancellation đang xử lý. |
| CSKH | Theo dõi máy và lịch sử; không sửa chứng từ Sales. |

Một người có thể được cấp nhiều quyền nếu mô hình nhân sự nhỏ, nhưng mỗi hành động vẫn được audit theo đúng vai trò nghiệp vụ.

### 6. Xác nhận đơn

Luồng xác nhận hiện tại được giữ nguyên:

1. Kiểm tra Customer, Warehouse, Product, BOM và tồn kho.
2. Xuất vật tư theo FIFO.
3. Chốt snapshot sản phẩm, BOM, giá bán, giá vốn và lợi nhuận.
4. Đưa đơn vào báo cáo quản trị đơn đã xác nhận và công nợ.
5. Cho phép ghi nhận thanh toán.
6. Tiến trình CustomerCare tạo máy chờ lắp đối với sản phẩm được đánh dấu là máy.

Nhãn thao tác UI nên là **Xác nhận và xuất kho** để người dùng hiểu đúng tác động. Không thiết kế lại boundary Confirm/Reserve/Issue trong V1.

### 7. Điều kiện mở revision

Manager được mở revision khi:

1. Đơn đang `Đã xác nhận - Chờ lắp`.
2. Đơn có ít nhất một sản phẩm được đánh dấu là máy.
3. Chưa có bất kỳ máy nào thuộc đơn được xác nhận lắp đặt.
4. Không có revision hoặc cancellation process khác đang mở.

Không bắt buộc hàng đã quay về hoặc payment bằng 0 để **mở** revision. Các điều kiện đó chỉ được kiểm tra theo tác động khi **áp dụng** revision.

### 8. Phạm vi được điều chỉnh

Manager có thể điều chỉnh:

- giá bán và lý do override;
- số lượng;
- thêm/xóa/đổi Product;
- Warehouse nếu toàn bộ tác động kho liên quan được xử lý hợp lệ;
- ghi chú hoặc thông tin liên hệ/snapshot không làm thay đổi danh tính khách hàng.

Không cho đổi `CustomerId` sau Confirm. Nếu chọn nhầm khách hàng, dùng **Hủy và tạo đơn mới**:

- copy dữ liệu thuận tiện nhập liệu: Warehouse, dòng hàng, số lượng, giá và ghi chú;
- không copy Payment, InventoryTransaction, Asset, Status, idempotency key, snapshot ID hoặc các ID kỹ thuật từ đơn cũ.

`OrderNo` của đơn hiện tại không thay đổi. Revision có số phiên bản và lịch sử riêng.

### 9. Ma trận tác động delta

| Thay đổi | Tác động kho | Điều kiện áp dụng | Tác động tài chính |
|---|---|---|---|
| Sửa ghi chú/thông tin | Không | Không có xung đột | Ghi audit. |
| Sửa giá bán | Không | Kiểm tra quyền override | Tính lại tổng đơn/công nợ. |
| Tăng số lượng | Issue phần tăng | Đủ tồn kho FIFO | Tăng doanh số/giá vốn/còn phải thu. |
| Giảm số lượng | Reverse phần giảm | Hàng giảm đã ở trong quyền kiểm soát của Kho và đủ điều kiện nhập lại | Giảm doanh số/giá vốn; có thể phát sinh tiền cần hoàn. |
| Đổi Product A thành B | Reverse A + issue B | Hàng A đủ điều kiện hoàn và vật tư B đủ tồn | Chốt snapshot/cost mới cho dòng thay đổi. |
| Xóa dòng | Reverse đúng dòng bị xóa | Hàng đủ điều kiện hoàn | Loại dòng khỏi revision hiệu lực. |
| Thêm dòng | Issue dòng mới | Đủ tồn kho | Thêm snapshot mới. |
| Dòng không thay đổi | Không | Không yêu cầu | Giữ nguyên allocation và cost snapshot. |

Kho nhận lại hàng chưa đồng nghĩa hàng tự động trở thành `Available`:

- hàng đạt: post reversal và tăng tồn khả dụng;
- hàng hỏng: chuyển xử lý hỏng/cách ly, không tăng tồn khả dụng;
- hàng thiếu: ghi nhận ngoại lệ, không tạo tồn giả.

V1 không cần xây dựng kho cách ly hoàn chỉnh nếu chưa có nhu cầu; UI chỉ cần cho Kho xác nhận `Đủ điều kiện nhập lại` hoặc `Không đủ điều kiện - xử lý ngoại lệ`.

### 10. Áp dụng revision

Khi Manager bấm **Áp dụng điều chỉnh**:

1. Hệ thống so sánh Original với Revision tại database.
2. Phân loại từng thay đổi và xác định prerequisite tương ứng.
3. Kiểm tra lại chưa có máy lắp, không có cancellation, quyền, row version và tồn kho.
4. Với phần cần hoàn, kiểm tra xác nhận hàng/tình trạng từ Kho.
5. Thực hiện reversal/issue delta và cập nhật snapshot trong một transaction.
6. Đồng bộ máy chờ lắp chỉ cho phần bị ảnh hưởng; máy cũ chuyển Superseded/Cancelled, không bị xóa.
7. Carry-forward toàn bộ payment của Order và tính lại Paid/Remaining/Overpaid.
8. Ghi audit trước/sau, revision, lý do, người thao tác và chứng từ liên quan.

Nếu bất kỳ prerequisite nào chưa đạt hoặc tồn kho mới không đủ:

- không áp dụng revision;
- đơn và snapshot hiện tại vẫn có hiệu lực;
- không post reversal/issue một phần;
- UI nói rõ việc cần xử lý, ví dụ `Còn 2 vật tư chưa xác nhận hoàn kho`.

### 11. Quy tắc thanh toán qua revision

- Payment tiếp tục gắn với Order, không reset theo revision.
- Nếu `NetPaid <= NewTotal`, cho phép áp dụng và tính `Remaining = NewTotal - NetPaid`.
- Nếu `NetPaid > NewTotal`, revision vẫn có thể có hiệu lực; hệ thống hiển thị `Tiền cần hoàn` bằng phần chênh lệch.
- V1 ưu tiên Refund cho phần trả dư; Customer Credit là khả năng mở rộng sau, không bắt buộc trong scope này.
- `Void` chỉ dùng khi bản ghi thu tiền sai và tiền không thực sự phát sinh.
- Tiền đã thực thu phải tạo Refund riêng, giữ nguyên payment gốc.
- Không hard-delete payment đã post.

### 12. Hủy đơn trước lắp đặt

Manager được hủy khi:

1. Đơn đang `Đã xác nhận - Chờ lắp`.
2. Chưa có máy nào thuộc đơn được xác nhận lắp.
3. Không có revision/cancellation process xung đột.

Khi Manager xác nhận **Hủy đơn**:

- hủy có hiệu lực ngay;
- trạng thái Sales hiển thị là `Đã hủy`;
- chặn mọi lắp đặt và revision mới;
- đơn không còn là active sale và không tiếp tục fulfillment;
- máy chờ lắp chuyển Cancelled/Superseded, không hard-delete và không tạo reminder;
- inventory và payment vẫn phản ánh đúng thực tế cho đến khi từng nhánh được xử lý.

Hủy có hiệu lực **không đồng nghĩa** hệ thống tự động hoàn kho và hoàn tiền trong cùng một thao tác.

### 13. Xử lý sau hủy

Hai nhánh chạy độc lập và có thể hoàn thành theo bất kỳ thứ tự nào.

#### 13.1. Hoàn hàng

- Nếu hàng chưa thực sự rời quyền kiểm soát, không cần bước thu hồi vật lý; Kho vẫn phải xác nhận tình trạng và hệ thống vẫn post reversal để đảo SalesIssue đã ghi nhận.
- Nếu hàng ở kỹ thuật/đang vận chuyển, hiển thị `Còn chờ hoàn kho`.
- Kho xác nhận hàng quay về và tình trạng.
- Hàng đạt mới tạo reversal theo allocation/cost gốc.
- Hàng hỏng hoặc thiếu chuyển ngoại lệ, không tăng tồn khả dụng sai.

#### 13.2. Thanh toán

- Chưa thu tiền: không có việc cần xử lý.
- Ghi nhận nhầm, tiền chưa thực thu: Void có lý do.
- Đã thực thu: Refund có ngày, số tiền, phương thức, tham chiếu và lý do.
- Trong thời gian chờ, UI hiển thị `Cần hoàn khách ...`.

Cancellation process được đóng khi không còn việc hoàn hàng hoặc hoàn tiền. Việc đóng process là trạng thái nội bộ, không cần thêm Sales status mới.

### 14. Khóa sau lắp đặt

Rule V1:

> **Có ít nhất một máy của đơn đã Installed thì khóa Điều chỉnh và Hủy cho toàn bộ đơn.**

Đây là modification lock, không có nghĩa toàn bộ đơn đã `Completed`. Các máy còn lại vẫn tiếp tục quy trình lắp đặt trong CustomerCare.

Rule này là giới hạn có chủ đích để giữ V1 đơn giản và tránh xung đột theo từng line/máy. Khi thực sự có nhu cầu vận hành, hệ thống mới xem xét line-level lock.

Mở revision, hủy và xác nhận lắp phải tranh chấp trên cùng `SalesOrderId`. Mỗi hành động phải đọc lại trạng thái mới nhất trong transaction, không chỉ dựa vào màn hình đã tải trước đó.

### 15. Đơn không có máy

Trong V1, đơn không chứa sản phẩm là máy vẫn bất biến sau Confirm như hiện tại. Đây là giới hạn phạm vi, không phải quy tắc ERP tổng quát.

Nếu sau này cần sửa/hủy linh hoạt cho đơn không có máy, phải xác định một boundary khác như giao hàng/hoàn tất giao hàng. Không đưa nội dung đó vào scope hiện tại.

### 16. Reporting và công nợ

- `Đã hủy` bị loại khỏi danh sách active sale ngay khi cancellation có hiệu lực.
- Báo cáo/chi tiết vẫn phải cho thấy nghĩa vụ chưa xong: hoàn hàng và tiền cần hoàn.
- Giao dịch reversal/refund vẫn nằm trong báo cáo đối soát và audit.
- Revision chỉ cập nhật giá trị hiệu lực của Order; lịch sử revision cũ không bị che lấp.
- Nhãn báo cáo hiện tại nên được hiểu là doanh số quản trị của đơn đã xác nhận. Không mở rộng scope V1 sang chuẩn ghi nhận doanh thu kế toán.

### 17. Trải nghiệm người dùng

Chi tiết đơn chỉ bổ sung hai hành động cho Manager:

1. **Điều chỉnh đơn**: mở màn hình giống Edit hiện tại, hiển thị tác động dự kiến theo từng dòng.
2. **Hủy đơn**: ABP modal nhập lý do và hiển thị các việc sẽ phát sinh.

Người dùng không phải hiểu revision entity, reversal transaction hoặc các enum nội bộ. UI dùng thông báo hành động được:

- `Không đủ tồn cho phần tăng: MAT-001 thiếu 2 Cái`;
- `Còn 1 dòng giảm số lượng chưa được Kho xác nhận hàng quay về`;
- `Đơn đã hủy, còn chờ hoàn kho`;
- `Đơn đã hủy, cần hoàn khách 500.000đ`;
- `Máy đã lắp ngày 26/08/2026 nên đơn không thể điều chỉnh hoặc hủy`.

### 18. Kịch bản mẫu

#### 18.1. Sửa giá, đã thu một phần

- Đơn 10 triệu, đã thu 5 triệu, chưa lắp.
- Manager sửa tổng thành 12 triệu.
- Không động kho; payment 5 triệu được giữ.
- Còn phải thu mới là 7 triệu.

#### 18.2. Giảm số lượng

- Đơn đã xuất 2 máy, chưa lắp; Manager giảm còn 1.
- Chỉ máy/vật tư của phần giảm được hoàn theo allocation gốc.
- Phần còn lại giữ nguyên cost snapshot.

#### 18.3. Đổi sản phẩm

- Đổi máy A thành máy B trước lắp.
- Hoàn phần A sau khi Kho xác nhận tình trạng; issue B theo FIFO hiện tại.
- Nếu B thiếu tồn, revision thất bại và đơn A cũ vẫn có hiệu lực.

#### 18.4. Hủy khi đã thu tiền

- Manager hủy: đơn lập tức `Đã hủy`, kỹ thuật không thể lắp.
- Kho xử lý hoàn hàng độc lập.
- Kế toán hoàn tiền độc lập.
- UI hiển thị cả hai việc còn chờ cho đến khi hoàn tất.

#### 18.5. Nhập sai khách hàng

- Không đổi CustomerId trên đơn đã xác nhận.
- Manager chọn `Hủy và tạo đơn mới`.
- Đơn mới chỉ được copy dữ liệu nhập liệu; không copy payment, kho, asset, status hoặc ID kỹ thuật.

### 19. Tiêu chí nghiệm thu nghiệp vụ

1. Sale và kỹ thuật không thể điều chỉnh/hủy đơn đã xác nhận.
2. Manager mở revision khi chưa lắp mà không bị ép xử lý payment/hàng trước.
3. Line không đổi giữ nguyên allocation và cost snapshot.
4. Price-only revision không tạo giao dịch kho.
5. Quantity/Product revision chỉ post phần delta.
6. Revision thất bại giữ nguyên đơn, kho và máy hiện hành.
7. Payment được carry-forward và công nợ được tính lại.
8. Overpaid tạo thông tin tiền cần hoàn, không ép reset payment.
9. Hủy có hiệu lực ngay và chặn lắp đặt.
10. Hoàn kho và hoàn tiền chạy độc lập, không làm sai trạng thái thực tế.
11. Đơn đã hủy có thể hiển thị các nghĩa vụ còn chờ mà không cần thêm Sales status.
12. Máy đầu tiên Installed khóa Điều chỉnh/Hủy toàn bộ đơn nhưng không đánh dấu Sales Completed.
13. Sai Customer dùng Hủy và tạo đơn mới, không copy dữ liệu kỹ thuật.
14. Tất cả thao tác có audit, permission, idempotency và concurrency coverage.

### 20. Ngoài scope V1

- Reservation/Allocation trước Issue.
- Delivery Order hoặc Fulfillment Aggregate.
- Sales `Completed` status.
- Line-level modification lock sau khi một phần máy đã lắp.
- Customer Credit wallet.
- Sửa/hủy linh hoạt cho non-machine order sau Confirm.
- Rewrite core Inventory hoặc thay đổi thời điểm Confirm hiện tại.
