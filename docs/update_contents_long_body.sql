/* Refresh body dài cho 3 bài Contents — chạy được nhiều lần */
SET NOCOUNT ON;

UPDATE Contents
SET Body = N'<p>Năm 2026, xu hướng nội thất tiếp tục nghiêng về không gian “thở được”: ít đồ nhưng chọn đúng chất liệu, ánh sáng ấm và bố cục mở. Japandi — giao thoa tối giản Nhật Bản với sự ấm áp Scandinavian — vẫn là lựa chọn an toàn cho căn hộ đô thị Việt Nam.</p>
<h3>Vật liệu nên ưu tiên</h3>
<ul>
<li>Gỗ sồi / gỗ thông FSC, hoàn thiện dầu cứng hoặc sơn water-based.</li>
<li>Vải linen, cotton, nỉ mềm — tránh bóng nhựa quá lạnh.</li>
<li>Kim loại brushed brass hoặc đen mờ làm điểm nhấn đèn, tay nắm.</li>
<li>Sơn khoáng matte tone ivory, clay, sand thay vì trắng lạnh gắt.</li>
</ul>
<p>Ánh sáng nên giữ khoảng 2700–3000K cho phòng khách và phòng ngủ. Kết hợp ambient (chung), task (đọc/làm việc) và accent (điểm nhấn tường/kệ) để buổi tối không bị phẳng.</p>
<h3>Gợi ý từ Interior Studio</h3>
<p>Bắt đầu từ một concept phòng khách hoặc phòng ngủ trên trang Concept, sau đó gắn các sản phẩm catalog có sẵn (sofa, bàn trà, đèn sàn). Cách này giúp bạn hình dung ngân sách thực tế và đặt hàng theo từng món thay vì mua “full set” ngay từ đầu.</p>
<p>Nếu đang cải tạo căn hộ 40–70 m², hãy giữ lối đi tối thiểu 80–90 cm, ưu tiên sofa chân cao và kệ thấp để sàn vẫn nhìn thấy — phòng sẽ trông rộng hơn rõ rệt.</p>',
    UpdatedAt = SYSUTCDATETIME()
WHERE Slug = N'xu-huong-noi-that-2026';

UPDATE Contents
SET Body = N'<p>Căn hộ nhỏ dễ bị “nuốt” bởi sofa quá to hoặc đặt sát tường. Mục tiêu là chọn form vừa vặn, chân cao để sàn thở, và màu sáng để phản xạ ánh sáng tự nhiên.</p>
<h3>Kích thước &amp; bố cục</h3>
<ul>
<li>Đo tường chính trước: sofa dài khoảng 2/3 bề ngang tường thường cân hơn.</li>
<li>Lối đi quanh sofa tối thiểu 80–90 cm; trước bàn trà còn 35–40 cm để ngồi thoải mái.</li>
<li>Sofa góc chỉ nên dùng khi phòng thực sự có góc chết — nhiều căn studio nên chọn sofa thẳng 2–3 chỗ.</li>
</ul>
<h3>Form &amp; màu</h3>
<p>Ưu tiên lưng thấp–trung bình, chân kim loại hoặc gỗ cao 12–18 cm. Tone be, kem, xám ấm, trắng ngà dễ phối với rèm linen và sàn gỗ. Tránh tay vịn quá dày nếu cửa sổ nhỏ — sẽ làm khung nhìn hẹp lại.</p>
<h3>Checklist nhanh trước khi mua</h3>
<ul>
<li>Đã đo cửa thang máy / cửa chính chưa?</li>
<li>Có chỗ đặt đèn đọc sách cạnh sofa không?</li>
<li>Nệm ngồi có độ lún vừa (không quá mềm khiến khó đứng dậy)?</li>
<li>Vải có chống bám bẩn cơ bản nếu nhà có trẻ / thú cưng?</li>
</ul>
<p>Trên Interior Studio, bạn có thể mở concept phòng khách rồi lọc sản phẩm liên quan để so giá studio với giá thị trường trước khi thêm vào giỏ.</p>',
    UpdatedAt = SYSUTCDATETIME()
WHERE Slug = N'chon-sofa-can-ho-nho';

UPDATE Contents
SET Body = N'<p>Một căn phòng chỉ có một lớp đèn trần thường sẽ hoặc quá tối, hoặc quá phẳng. Công thức “3 lớp ánh sáng” giúp không gian ấm, linh hoạt theo giờ trong ngày và dễ thay đổi mood mà không cần sửa điện lớn.</p>
<h3>1. Ambient — ánh sáng chung</h3>
<p>Đây là nền sáng của phòng: đèn âm trần, đèn chùm nhẹ, hoặc dải LED gián tiếp trên trần/thanh ray. Nên chọn nhiệt độ màu khoảng 3000K cho phòng khách, 2700K cho phòng ngủ. Tránh một bóng công suất cao chiếu thẳng xuống giữa phòng — dễ tạo bóng cứng trên mặt.</p>
<h3>2. Task — ánh sáng làm việc</h3>
<p>Phục vụ đọc sách, nấu ăn, làm việc: đèn bàn, đèn kẹp đầu giường, đèn dưới tủ bếp, đèn treo thấp trên bàn ăn. Ánh sáng task nên đủ rõ nhưng không chói mắt — đặt hơi lệch phía sau vai khi đọc là lý tưởng.</p>
<h3>3. Accent — điểm nhấn</h3>
<p>Làm nổi bức tường, kệ trang trí, tranh hoặc chất liệu gỗ: đèn sàn chiếu lên, spotlight nhỏ, hoặc đèn tường. Accent giúp căn phòng “có chiều sâu” và đẹp hơn trên ảnh cũng như ngoài đời.</p>
<h3>Gợi ý setup thực tế</h3>
<ul>
<li>Phòng khách: 4–6 đèn âm trần dim được + 1 đèn sàn brass + 1 đèn bàn cạnh sofa.</li>
<li>Phòng ngủ: đèn âm trần yếu + 2 đèn đầu giường + rèm 2 lớp để kiểm soát sáng ngày.</li>
<li>Góc làm việc: task lamp 4000K cục bộ, còn lại giữ ấm để mắt đỡ mỏi buổi tối.</li>
</ul>
<p>Khi xem concept trên Interior Studio, hãy chú ý mục thông số chiếu sáng và các sản phẩm đèn liên quan — đó thường là lớp accent/task giúp concept “sống” hơn chỉ với ảnh render.</p>',
    UpdatedAt = SYSUTCDATETIME()
WHERE Slug = N'anh-sang-3-lop-trong-nha';

SELECT Slug, LEN(Body) AS BodyLen FROM Contents WHERE Slug IN (
  N'xu-huong-noi-that-2026',
  N'chon-sofa-can-ho-nho',
  N'anh-sang-3-lop-trong-nha'
);
