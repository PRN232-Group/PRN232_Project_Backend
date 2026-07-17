# Coding rules — InteriorStudio BE (reuse checklist)

Giữ đúng kiến trúc folder `Project` theo lab layered. Không nâng cấp sang MediatR / Clean Architecture mới / ASP.NET Identity full.

## Project naming (chuẩn)

`Com.FPTU.Prn232SE1819.Api.<Layer>`

| Layer | Folder / Project |
|-------|------------------|
| Web host | `Com.FPTU.Prn232SE1819.Api.WebApi` |
| Services | `Com.FPTU.Prn232SE1819.Api.Services` |
| Application | `Com.FPTU.Prn232SE1819.Api.Application` |
| Infrastructure | `Com.FPTU.Prn232SE1819.Api.Infrastructure` |
| Entity | `Com.FPTU.Prn232SE1819.Api.Entity` |
| Caching | `Com.FPTU.Prn232SE1819.Api.Caching` |
| Test (optional) | `Com.FPTU.Prn232SE1819.Api.Test` |

Solution: `Com.FPTU.Prn232SE1819.Api.slnx`

## Quy trình sau mỗi request (bắt buộc)

1. **Implement** đúng layer + pattern lab (UoW / `DataServiceBase` / cache khi CRUD entity).
2. **Không vượt kiến trúc** — không sửa nền `Repository` / `UnitOfWork` / `DataCached` trừ bug.
3. **Contract FE** — response/request khớp `Frontend_Project/docs/BE_FULL_SPEC.md` + field UI đang đọc.
4. **Double-check** checklist bên dưới + ghi `PROGRESS.md`.
5. **Smoke** endpoint chính (Swagger `/swagger` hoặc curl).

## Layers (không đảo)

| Layer | Việc được làm |
|-------|----------------|
| WebApi | Controllers mỏng, JWT/CORS/Swagger trong `Program.cs` |
| Services | Business + cache; extend `DataServiceBase` khi CRUD entity |
| Application | Interfaces + DTO (map tay) |
| Infrastructure | `Repository` / `UnitOfWork` / `DbFactoryContext` — **không sửa trừ bug** |
| Entity | EF models + `InteriorStudioDbContext` |
| Caching | `IDataCached` — tái dùng |

## Thêm domain mới (Categories, Products, …)

1. Scaffold **chỉ** bảng cần dùng vào `Models/` (không kéo cả DB).
2. Interface trong `Interfaces/Services/` — CRUD entity kế thừa `IDataService<T>`.
3. Service trong `Services/` kế thừa `DataServiceBase<T>` + cache keys `CachingCommonDefaults`.
4. Đăng ký **1 dòng** trong `AddDataServices()`.
5. Controller 1 file, gọi 1 service chính; DTO map tay; JSON camelCase.
6. Không copy-paste service dài nếu có thể tái dùng UoW + base.

## Auth rules

- `POST /api/auth/login` **chỉ** `email` + `password` — **không** nhận/gán `role` từ client.
- `role` trong response luôn lấy từ DB (`Users` → `Roles`).
- `CreatedAt` / `UpdatedAt` khi **ghi DB** dùng `VnDateTime.Now` (UTC+7), không dùng `DateTime.UtcNow`. JWT expiry vẫn UTC.
- Đăng ký: `register/request` (OTP) → `register/verify` (mới INSERT User). Không tạo User trước OTP.
- Quên MK: `forgot-password` → `forgot-password/verify` → `reset-password`.
- Email: `IEmailSender` / `SmtpEmailSender` — cấu hình `Smtp:*`; thiếu Password thì log OTP ra console.

## Packages được phép (Phase hiện tại)

- EF Core SqlServer (đã có)
- `BCrypt.Net-Next`, `JwtBearer` / `System.IdentityModel.Tokens.Jwt`
- `Swashbuckle.AspNetCore` (Swagger UI `/swagger`)
- Không thêm AutoMapper / MediatR / Identity trừ khi slide bắt buộc

## Double-check trước khi đóng task

- [ ] Không phá pattern UoW / cache lab
- [ ] Connection chỉ qua `InteriorStudioDbConn`
- [ ] Service đăng ký DI
- [ ] Cache invalidate khi Add/Update/Delete (entity CRUD)
- [ ] FE field khớp (camelCase)
- [ ] Cập nhật `PROGRESS.md`
