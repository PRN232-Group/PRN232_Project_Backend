# InteriorStudio Backend (PRN232)

API ASP.NET cho dự án Interior Studio (FE: repo `PRN232_Project_Frontend`).

## Git — đừng push nhầm repo

| | Backend (repo này) | Frontend |
|---|-------------------|----------|
| **GitHub** | https://github.com/PRN232-Group/PRN232_Project_Backend.git | https://github.com/PRN232-Group/PRN232_Project_Frontend.git |
| **Nhánh làm việc** | `backend` | `main` |
| **Thư mục local** | `d:\PRN232\Project` | `d:\PRN232\Frontend_Project` |

```bash
# Clone BE
git clone https://github.com/PRN232-Group/PRN232_Project_Backend.git
cd PRN232_Project_Backend
git checkout backend
```

---

## Setup nhanh (lần đầu)

### 1. Yêu cầu

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server hoặc `(localdb)\MSSQLLocalDB`
- SSMS / Azure Data Studio

### 2. Cấu hình secret (chỉ máy dev)

**Không** đặt password / JWT / SMTP thật trong `appsettings.json` (file này được commit).

```bash
cd Com.FPTU.Prn232SE1819.Api.WebApi
copy appsettings.Development.example.json appsettings.Development.json
```

Sửa `appsettings.Development.json`:

| Key | Ý nghĩa | Ví dụ |
|-----|---------|--------|
| `ConnectionStrings:InteriorStudioDbConn` | Chuỗi kết nối SQL | `Server=(localdb)\MSSQLLocalDB;Database=InteriorStudio;User Id=sa;Password=***;TrustServerCertificate=True` |
| `Jwt:Key` | Secret ký JWT (≥ 32 ký tự) | Chuỗi random dài |
| `Jwt:Issuer` / `Jwt:Audience` | Issuer/audience token | `InteriorStudio` / `InteriorStudio.Client` |
| `Smtp:User` | Gmail gửi OTP | `your@gmail.com` |
| `Smtp:Password` | [Gmail App Password](https://myaccount.google.com/apppasswords) | 16 ký tự app password |
| `Smtp:LogOtpToConsole` | `true` = in OTP ra console khi chưa cấu SMTP | `true` |

File `appsettings.Development.json` đã nằm trong `.gitignore` — **không push lên GitHub**.

### 3. Database

Tạo DB và seed theo thứ tự trong [docs/DATABASE_SETUP.md](docs/DATABASE_SETUP.md):

1. `docs/create_InteriorStudio.sql` — schema + roles  
2. `docs/seed_admin.sql` — admin đăng nhập  
3. `docs/seed_mock_catalog.sql` — sản phẩm mẫu  
4. `docs/seed_role_permissions.sql` — phân quyền trang  
5. `docs/seed_contents.sql` — blog  
6. `docs/seed_quotations.sql` — báo giá demo  

Admin mặc định sau seed: `ngthanhtrung302005@gmail.com` / `Admin@123` (đổi sau khi vào hệ thống).

### 4. Chạy API

```bash
cd Com.FPTU.Prn232SE1819.Api.WebApi
dotnet run --launch-profile http
```

| | URL |
|---|-----|
| API | http://localhost:5259 |
| Swagger | http://localhost:5259/swagger |

`launchSettings.json` đặt `ASPNETCORE_ENVIRONMENT=Development` → tự load `appsettings.Development.json`.

### 5. Nối Frontend

Trong repo FE, file `.env`:

```env
VITE_API_BASE_URL=http://localhost:5259
VITE_USE_MOCK=false
```

```bash
cd Frontend_Project
npm install
npm run dev
```

---

## Solution & layers

- Solution: `Com.FPTU.Prn232SE1819.Api.slnx`  
- Startup: `Com.FPTU.Prn232SE1819.Api.WebApi`

```
Com.FPTU.Prn232SE1819.Api.WebApi          → Controllers / Program / JWT / Swagger
Com.FPTU.Prn232SE1819.Api.Services        → Business + cache
Com.FPTU.Prn232SE1819.Api.Application     → Interfaces + DTOs
Com.FPTU.Prn232SE1819.Api.Infrastructure  → Repository / UnitOfWork
Com.FPTU.Prn232SE1819.Api.Entity          → EF models + DbContext
Com.FPTU.Prn232SE1819.Api.Caching         → IDataCached
```

## Tài liệu thêm

- [docs/PROGRESS.md](docs/PROGRESS.md) — tiến độ tính năng  
- [docs/CODING_RULES.md](docs/CODING_RULES.md) — quy ước code  
- [docs/DATABASE_SETUP.md](docs/DATABASE_SETUP.md) — thứ tự chạy SQL  
