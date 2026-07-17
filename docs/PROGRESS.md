# InteriorStudio BE — Progress

## Phase 1 — Auth / Users / Roles ✅
## Phase 2 — Catalog / Cart / Orders / Reviews ✅

## Auth OTP (Register + Forgot password) ✅

**Date:** 2026-07-16

### Done

- [x] Bảng `EmailOtps` + entity EF
- [x] `IEmailSender` / `SmtpEmailSender` (SMTP Gmail; thiếu password → log OTP console)
- [x] Register: `POST /api/auth/register/request` → OTP → `register/verify` (mới INSERT User)
- [x] Forgot: `forgot-password` → `forgot-password/verify` → `reset-password`
- [x] FE: register OTP steps, forgot OTP, `/reset-password`, link quên MK trên login
- [x] Smoke: reset `ngthanhtrung302005@gmail.com` → `Admin@1234`; register OTP không tạo user trước verify

### SMTP & secrets

Giá trị thật (connection string, JWT key, SMTP password) đặt trong **`appsettings.Development.json`** — không commit file này.

Lần đầu clone: copy `appsettings.Development.example.json` → `appsettings.Development.json` rồi điền thông tin máy bạn.

`Smtp:LogOtpToConsole=true` để test OTP không cần SMTP (password để trống cũng được).

### Admin hiện tại

| Email | Password |
|-------|----------|
| ngthanhtrung302005@gmail.com | **Admin@1234** |

## Role page permissions (2026-07-17)

- Bảng `AppPages` + `RolePermissions` (seed theo menu FE hiện tại).
- Bỏ API thêm/sửa/xóa Role; trang `/admin/roles` = matrix phân quyền trang.
- API: `GET/PUT /api/permissions/*`; login trả `permissions[]`.
- FE: guard + menu theo `user.permissions`; luôn mở `/`, `/admin`, `/manager`, `/sales`.
- Script: [`seed_role_permissions.sql`](seed_role_permissions.sql)

## Rename & cleanup (2026-07-17)

- Xóa `Com.FPTU.Prn232SE1918.WebApi` (template WeatherForecast, không dùng).
- Chuẩn hóa tên: `Com.FPTU.Prn232SE1819.Api.<Layer>`; Web host = `WebApi`.
- Solution mới: `Com.FPTU.Prn232SE1819.Api.slnx`.
- Build OK (chỉ warning nullable sẵn có trong Infrastructure lab — **không sửa** nền UoW/Repository).
- Startup: `cd Com.FPTU.Prn232SE1819.Api.WebApi && dotnet run --launch-profile http`

## Phase 3 — InteriorDesigns ✅ (2026-07-17)

- EF: `InteriorDesign` + Images / Highlights / Specs / Materials / Packages / Products
- API: `GET/POST/PUT/DELETE /api/interior-designs` (public list lọc `isPublished`; Manager/Admin CRUD full)
- Contract khớp FE: `gallery`, `priceCompare`, `relatedProductIds`, `relatedProducts` (detail)
- Seed từ mock: [`seed_designs_from_mock.py`](seed_designs_from_mock.py) (5 concept)
- FE: `DesignManagementPage` lỗi API rõ hơn + reload detail khi sửa; `InteriorDesignPage` filter `useMemo`

## Contents / Blog storefront (2026-07-17)

- BE: `Contents` CRUD (`/api/contents`, by-slug); Admin ghi, public chỉ bài published
- Seed: [`seed_contents.sql`](seed_contents.sql) (3 bài + cover Unsplash)
- FE: trang chủ **Concept nổi bật** (đổi title), **Góc cảm hứng**, `/blog` + `/blog/:slug`
- Stats trang chủ: số concept thật; dòng sản phẩm theo ngưỡng `5+ / 10+ / …`

## Quotations / QuotationRequests (2026-07-17)

- EF: `QuotationRequest`, `QuotationRequestProduct`, `Quotation`, `QuotationProduct`
- API:
  - `GET/POST /api/quotation-requests` (+ `/mine`, `PUT /{id}/reply`, `PUT /{id}`)
  - `GET/POST /api/quotations` (+ `/mine`, `PUT /{id}` duyệt/từ chối)
- Customer tạo YCBG; Sales tạo bản báo giá + duyệt
- Seed mẫu: [`seed_quotations.sql`](seed_quotations.sql)
- FE: hub Sales 1 menu **Báo giá** (2 tab); khách gửi từ SP/giỏ + `/my-quotations`

## Phase 3 còn lại

DesignRequests, Chat, SystemLogs, Analytics

## Rules

[`CODING_RULES.md`](CODING_RULES.md)
