# InteriorStudio — Database setup

Chạy script trong **SSMS / Azure Data Studio** trên SQL Server (LocalDB hoặc instance riêng).

## Thứ tự bắt buộc

| Bước | File | Mô tả |
|------|------|--------|
| 1 | `create_InteriorStudio.sql` | Tạo database `InteriorStudio`, toàn bộ bảng, seed 4 role, bảng `AppPages` / `RolePermissions` (khung) |
| 2 | `seed_admin.sql` | User Admin (`ngthanhtrung302005@gmail.com` / `Admin@123`) — BCrypt hash thật |
| 3 | `seed_mock_catalog.sql` | 6 category + 6 sản phẩm mẫu (khớp mock FE) |
| 4 | `seed_role_permissions.sql` | Danh sách trang FE + gán quyền theo role |
| 5 | `seed_contents.sql` | Bài blog / guide mẫu |
| 6 | `seed_quotations.sql` | Yêu cầu báo giá + báo giá chính thức demo |

## Tùy chọn (sửa dữ liệu đã seed)

| File | Khi nào dùng |
|------|----------------|
| `fix_vn_product_seed.sql` | Sửa tên/mô tả SP tiếng Việt |
| `update_contents_long_body.sql` | Nội dung blog dài hơn |
| `fix_quotation_unicode.sql` | Sửa lỗi Unicode trên báo giá (nếu có) |

## Lưu ý

- `create_InteriorStudio.sql` có block seed admin placeholder (`REPLACE_WITH_BCRYPT_HASH`) — **luôn chạy `seed_admin.sql` sau** để có hash đăng nhập đúng.
- `seed_quotations.sql` cần Products id 1–6 và user Sales/Customer — chạy sau bước 2–3.
- Connection string BE: `ConnectionStrings:InteriorStudioDbConn` trong `appsettings.Development.json`.

## Đồng bộ với FE

Schema mô tả thêm tại repo Frontend: `docs/MSSQL_SCHEMA.md`, `docs/BE_FULL_SPEC.md`.  
File `create_InteriorStudio.sql` ở cả hai repo nên giữ cùng nội dung.
