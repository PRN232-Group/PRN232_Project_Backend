
- Về role team: team 5 người thì 1 front, 4 back (do front không bị hỏi kiến trúc và code nên ko cần chú tâm quá hoặc bá hơn gánh back cũng đc =)) )

- Về thiết kế: tầm trước slot thứ 2 tuần sau phải có bản figma giao diện sẵn (slot thứ 2 tức thứ 5 ấy)

- Về backend FR:
FR 1,2,13: Trung
FR 3,4: Đức
FR 5,6,8: Hoàng
FR 9,10,11: Hiệp
FR 12,14: Phú
(Riêng FR 7 sẽ cân nhắc/Nội dung chi tiết FR là gì xem cái ảnh gửi trên nhóm đã ghim)

- Về frontend: 
Trung =)) => đợi Phú có bản thiết kế và làm liền cần data hiện gì sẽ báo cho team làm api đúng

- Về kiến trúc:
Cơ bản thôi nhưng phải theo thầy môn prn này => phải tối ưu entity, cái gì ra cái đó, có handle khi loss connection db bảo toàn tránh lỗi data

- Về git push lên:

Link git:

quy tắc dùng git cmd (không cần làm theo nhưng sai thì phải chịu):
+) khi pull về lần đầu dùng cmd:
	git init
	git remote add origin <link github>
	git pull origin main 
+) khi pull về các lần sau đã có nhánh đã push:
	git pull origin <tên nhánh mấy bro hoặc nhánh mấy sếp khác nếu muốn gộp chung> => lưu ý dễ bị conflict
+) khi push lên:
        git add .
	git commit -m "Feat (<Tên thành viên làm>): <nội dung đã làm>"
	git push origin master:<tên nhánh của mấy bro đã tạo trên github>

quy tắc dùng tại visual studio luôn:
+) lần đầu khi pull về thì giống bên git cmd
+) lần sau thì code xong vào mục git phía trên cùng chọn commit 
	ghi nội dung commit tại ô commit Feat (<tên thành viên làm>): <nội dung đã làm>"
	xác nhận 
	cũng tại mục git trên cùng => chọn push (cái này chỉ đc khi mn đăng kí push tại đâu trong github)
