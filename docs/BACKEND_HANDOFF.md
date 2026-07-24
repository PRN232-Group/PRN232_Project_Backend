# Backend Handoff — Interior Studio (PRN232)

Tài liệu phân task backend: **luồng nghiệp vụ**, **API contract**, **JSON mẫu**, **SQL schema**. Frontend đã chuẩn hóa base URL qua `VITE_API_BASE_URL` + `src/infrastructure/http/apiClient.js`.

---

## 0. Tổng quan hệ thống

| Thành phần | Mô tả |
|---|---|
| Sản phẩm | Nội thất / thiết kế nội thất (Interior Studio) |
| Roles | `Customer`, `Sales`, `Production`, `Manager`, `Admin` |
| Auth | JWT Bearer (FE đọc `accessToken` từ localStorage) |
| Base URL FE | `VITE_API_BASE_URL` (vd `https://localhost:5001`) |
| Convention | REST JSON, status HTTP chuẩn, lỗi `{ "message": "..." }` |

### Ma trận quyền nhanh

| Module | Customer | Sales | Production | Manager | Admin |
|---|---|---|---|---|---|
| Catalog / Cart / Checkout | ✓ | xem | — | CRUD SP | danh mục/CMS |
| Đơn hàng | của mình | cập nhật TT KD | SX / giao | giám sát | — |
| Báo giá / thiết kế | tạo yêu cầu | xử lý / duyệt | — | — | — |
| Chat CSKH | ✓ | ✓ | — | — | — |
| Analytics / doanh thu | — | dashboard KD | dashboard SX | ✓ | dashboard HT |
| Users / Roles / Logs | — | — | — | — | ✓ |

---

## 1. Luồng nghiệp vụ (Flows)

### F1 — Đăng ký / Đăng nhập / Quên mật khẩu
1. Customer `POST /api/auth/register` → tạo User role Customer.
2. Mọi role `POST /api/auth/login` → `{ accessToken, user }`.
3. `POST /api/auth/forgot-password` → gửi email reset (hoặc token).

### F2 — Duyệt catalog & mua hàng (Customer)
1. `GET /api/products` / `GET /api/products/{id}` / `GET /api/products/search`.
2. `POST /api/cart` → thêm SP.
3. `GET|PUT|DELETE /api/cart(/{id})`.
4. `POST /api/orders/checkout` → tạo Order + OrderItems, xóa/giảm cart, trừ tồn (transaction).

### F3 — Theo dõi đơn & đánh giá
1. `GET /api/orders` (mine), `GET /api/orders/{id}`.
2. Sau Completed: `POST /api/products/{id}/reviews`.

### F4 — Sales xử lý đơn / báo giá / thiết kế / chat
1. Đơn: `GET /api/sales/orders`, `PUT /api/sales/orders/{id}`.
2. Yêu cầu báo giá: `GET /api/quotation-requests`, reply/update.
3. Báo giá: `GET /api/quotations`, `PUT` duyệt.
4. Design request: `GET|PUT /api/design-requests/{id}`.
5. Chat: `GET /api/chat/customers`, messages, `POST /api/chat/send`.

### F5 — Production
1. Dashboard + danh sách lệnh SX.
2. Cập nhật status lệnh: `PUT /api/production/orders/{id}/status`.
3. Theo dõi tiến độ: `GET /api/production/progress`.
4. Giao hàng: `GET|PUT /api/delivery/orders(/{id})`.

### F6 — Manager
1. CRUD sản phẩm, cập nhật giá.
2. Giám sát đơn + đổi status hệ thống: `PUT /api/orders/{id}/status`.
3. Best-selling + revenue report.

### F7 — Admin
1. Users, Roles, Categories, Contents (CMS), System logs.
2. Dashboard thống kê tổng.

### F8 — Thiết kế nội thất (Customer browse)
1. `GET /api/interior-designs` (gallery / template).

### F9 — Blog / Nội dung trang chủ
1. `GET /api/Blog` hoặc thống nhất `GET /api/contents?type=Blog`.

---

## 2. API cần dựng (checklist theo team FR)

> Gắn gợi ý theo README team: FR1,2,13 Trung · FR3,4 Đức · FR5,6,8 Hoàng · FR9,10,11 Hiệp · FR12,14 Phú.

### Auth
| Method | Path | Body / Query | Response | Ai làm |
|---|---|---|---|---|
| POST | `/api/auth/register` | RegisterDto | UserDto | Auth |
| POST | `/api/auth/login` | `{ email, password }` | `{ accessToken, refreshToken?, id, name, email, role, phone? }` | Auth |
| POST | `/api/auth/forgot-password` | `{ email }` | `{ message }` | Auth |
| POST | `/api/auth/reset-password` | `{ token, newPassword }` | `{ message }` | Auth |

### Products / Categories
| Method | Path | Notes | Ai làm |
|---|---|---|---|
| GET | `/api/products` | list + filter?categoryId | Catalog |
| GET | `/api/products/{id}` | detail + images | Catalog |
| GET | `/api/products/search?keyword=` | | Catalog |
| POST | `/api/products` | Manager | Catalog |
| PUT | `/api/products/{id}` | Manager | Catalog |
| DELETE | `/api/products/{id}` | soft-delete khuyến nghị | Catalog |
| PUT | `/api/products/{id}/price` | `{ price }` | Catalog |
| CRUD | `/api/categories` | Admin | Catalog |

### Cart / Orders
| Method | Path | Notes |
|---|---|---|
| GET | `/api/cart` | theo user hiện tại |
| POST | `/api/cart` | `{ productId, quantity }` |
| PUT | `/api/cart/{id}` | `{ quantity }` |
| DELETE | `/api/cart/{id}` | |
| POST | `/api/orders/checkout` | `{ shippingAddress, phone, note? }` |
| GET | `/api/orders` | Customer = mine; Manager = all (hoặc query `scope`) |
| GET | `/api/orders/{id}` | |
| PUT | `/api/orders/{id}/status` | `{ status }` Manager |
| GET | `/api/sales/orders` | Sales list + items |
| PUT | `/api/sales/orders/{id}` | `{ status }` |

**Order.status (chuẩn FE):** `Pending` | `Processing` | `Shipping` | `Completed` | `Cancelled`

### Reviews / Profile / Users
| Method | Path |
|---|---|
| GET/POST | `/api/products/{id}/reviews` |
| GET/PUT | `/api/users/profile` |
| GET/DELETE | `/api/users` (Admin) |
| CRUD | `/api/roles` |

### Quotations / Design / Interior
| Method | Path |
|---|---|
| GET | `/api/quotation-requests` |
| PUT | `/api/quotation-requests/{id}/reply` |
| PUT | `/api/quotation-requests/{id}` |
| GET/PUT | `/api/quotations` , `/api/quotations/{id}` |
| GET/PUT | `/api/design-requests/{id}` |
| GET | `/api/interior-designs` |

### Chat
| Method | Path |
|---|---|
| GET | `/api/chat/messages` | Customer thread |
| GET | `/api/chat/customers` | Sales inbox |
| GET | `/api/chat/messages/{customerId}` | |
| POST | `/api/chat/send` | `{ content, customerId? }` |

### Production / Delivery
| Method | Path |
|---|---|
| GET | `/api/production/dashboard` |
| GET | `/api/production/orders` | list (FE đã tách list/detail) |
| GET | `/api/production/orders/{id}` | |
| PUT | `/api/production/orders/{id}/status` | `{ status }` |
| GET | `/api/production/progress` | |
| GET | `/api/delivery/orders` | |
| PUT | `/api/delivery/orders/{orderId}` | `{ deliveryStatus, note?, deliveredAt? }` |

### Analytics / CMS / Logs
| Method | Path |
|---|---|
| GET | `/api/analytics/dashboard` | Manager |
| GET | `/api/analytics/best-selling-products` | |
| GET | `/api/revenue/report` | query `from`,`to` khuyến nghị |
| GET | `/api/sales/dashboard` | |
| CRUD | `/api/contents` | Admin CMS |
| GET | `/api/Blog` | hoặc merge vào contents |
| GET | `/api/systemlogs` | Admin |

---

## 3. JSON mẫu (DTO)

### LoginResponse
```json
{
  "accessToken": "eyJhbGciOi...",
  "refreshToken": "optional",
  "id": 12,
  "name": "An Nguyen",
  "email": "an@example.com",
  "role": "Customer",
  "phone": "0901234567"
}
```

### ProductDto
```json
{
  "id": 1,
  "name": "Ghế Sofa Nordic",
  "description": "Gỗ sồi, nệm vải",
  "price": 12500000,
  "stock": 20,
  "imageUrl": "https://...",
  "categoryId": 3,
  "categoryName": "Sofa",
  "isActive": true
}
```

### CartItemDto
```json
{
  "id": 9,
  "productId": 1,
  "productName": "Ghế Sofa Nordic",
  "imageUrl": "https://...",
  "unitPrice": 12500000,
  "quantity": 2,
  "lineTotal": 25000000
}
```

### CheckoutRequest
```json
{
  "shippingAddress": "12 Nguyễn Huệ, Q1, HCM",
  "phone": "0901234567",
  "note": "Giao buổi sáng"
}
```

### OrderDto
```json
{
  "id": 101,
  "customerId": 12,
  "customerName": "An Nguyen",
  "customerEmail": "an@example.com",
  "customerPhone": "0901234567",
  "shippingAddress": "12 Nguyễn Huệ",
  "status": "Pending",
  "totalPrice": 25000000,
  "createdAt": "2026-07-15T08:00:00Z",
  "items": [
    {
      "productId": 1,
      "productName": "Ghế Sofa Nordic",
      "quantity": 2,
      "price": 12500000
    }
  ]
}
```

### ReviewDto
```json
{
  "id": 1,
  "productId": 1,
  "userId": 12,
  "userName": "An",
  "rating": 5,
  "comment": "Rất đẹp",
  "createdAt": "2026-07-15T10:00:00Z"
}
```

### QuotationRequestDto
```json
{
  "id": 5,
  "customerId": 12,
  "customerName": "An Nguyen",
  "title": "Báo giá phòng khách 20m2",
  "description": "Sofa + bàn trà + kệ TV",
  "status": "Pending",
  "reply": null,
  "createdAt": "2026-07-14T09:00:00Z"
}
```

### DesignRequestDto
```json
{
  "id": 3,
  "customerId": 12,
  "title": "Thiết kế căn hộ 2PN",
  "style": "Japandi",
  "budget": 80000000,
  "status": "InReview",
  "notes": "Ưu tiên gỗ sáng",
  "attachments": ["https://..."]
}
```

### ChatMessageDto
```json
{
  "id": 44,
  "customerId": 12,
  "senderRole": "Customer",
  "senderId": 12,
  "content": "Cho mình hỏi về sofa...",
  "createdAt": "2026-07-15T11:00:00Z"
}
```

### ProductionOrderDto
```json
{
  "id": 20,
  "orderId": 101,
  "status": "InProgress",
  "progressPercent": 45,
  "assignedTo": "Toan SX",
  "deadline": "2026-07-25",
  "updatedAt": "2026-07-15T12:00:00Z"
}
```

### DeliveryUpdateRequest
```json
{
  "deliveryStatus": "OutForDelivery",
  "note": "Ship Shopee Express",
  "deliveredAt": null
}
```

### ContentDto (CMS)
```json
{
  "id": 1,
  "title": "Xu hướng nội thất 2026",
  "slug": "xu-huong-2026",
  "type": "Blog",
  "body": "<p>...</p>",
  "coverUrl": "https://...",
  "isPublished": true,
  "publishedAt": "2026-07-01T00:00:00Z"
}
```

### SystemLogDto
```json
{
  "id": 9001,
  "actorUserId": 2,
  "action": "UPDATE_ORDER_STATUS",
  "entity": "Order",
  "entityId": "101",
  "detail": "Pending → Processing",
  "createdAt": "2026-07-15T12:30:00Z"
}
```

---

## 4. SQL Server — schema đề xuất

> Naming: PascalCase table, PK `Id`, soft delete `IsDeleted`, audit `CreatedAt`/`UpdatedAt`. Dùng transaction cho checkout & đổi tồn kho.

```sql
-- ========== IDENTITY / AUTH ==========
CREATE TABLE Roles (
  Id            INT IDENTITY PRIMARY KEY,
  Name          NVARCHAR(50) NOT NULL UNIQUE, -- Customer, Sales, Production, Manager, Admin
  Description   NVARCHAR(255) NULL,
  CreatedAt     DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE Users (
  Id            INT IDENTITY PRIMARY KEY,
  RoleId        INT NOT NULL REFERENCES Roles(Id),
  Email         NVARCHAR(256) NOT NULL UNIQUE,
  PasswordHash  NVARCHAR(512) NOT NULL,
  FullName      NVARCHAR(150) NOT NULL,
  Phone         NVARCHAR(30) NULL,
  AvatarUrl     NVARCHAR(500) NULL,
  IsActive      BIT NOT NULL DEFAULT 1,
  IsDeleted     BIT NOT NULL DEFAULT 0,
  CreatedAt     DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  UpdatedAt     DATETIME2 NULL
);

CREATE TABLE RefreshTokens (
  Id          INT IDENTITY PRIMARY KEY,
  UserId      INT NOT NULL REFERENCES Users(Id),
  Token       NVARCHAR(512) NOT NULL,
  ExpiresAt   DATETIME2 NOT NULL,
  CreatedAt   DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  RevokedAt   DATETIME2 NULL
);

CREATE TABLE PasswordResetTokens (
  Id        INT IDENTITY PRIMARY KEY,
  UserId    INT NOT NULL REFERENCES Users(Id),
  Token     NVARCHAR(128) NOT NULL,
  ExpiresAt DATETIME2 NOT NULL,
  UsedAt    DATETIME2 NULL
);

-- ========== CATALOG ==========
CREATE TABLE Categories (
  Id          INT IDENTITY PRIMARY KEY,
  Name        NVARCHAR(120) NOT NULL,
  Description NVARCHAR(500) NULL,
  IsActive    BIT NOT NULL DEFAULT 1,
  CreatedAt   DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE Products (
  Id           INT IDENTITY PRIMARY KEY,
  CategoryId   INT NULL REFERENCES Categories(Id),
  Name         NVARCHAR(200) NOT NULL,
  Description  NVARCHAR(MAX) NULL,
  Price        DECIMAL(18,2) NOT NULL CHECK (Price >= 0),
  Stock        INT NOT NULL DEFAULT 0 CHECK (Stock >= 0),
  ImageUrl     NVARCHAR(500) NULL,
  IsActive     BIT NOT NULL DEFAULT 1,
  IsDeleted    BIT NOT NULL DEFAULT 0,
  CreatedAt    DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  UpdatedAt    DATETIME2 NULL
);

CREATE TABLE ProductImages (
  Id         INT IDENTITY PRIMARY KEY,
  ProductId  INT NOT NULL REFERENCES Products(Id),
  Url        NVARCHAR(500) NOT NULL,
  SortOrder  INT NOT NULL DEFAULT 0
);

-- ========== CART / ORDER ==========
CREATE TABLE Carts (
  Id         INT IDENTITY PRIMARY KEY,
  UserId     INT NOT NULL UNIQUE REFERENCES Users(Id),
  UpdatedAt  DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE CartItems (
  Id         INT IDENTITY PRIMARY KEY,
  CartId     INT NOT NULL REFERENCES Carts(Id) ON DELETE CASCADE,
  ProductId  INT NOT NULL REFERENCES Products(Id),
  Quantity   INT NOT NULL CHECK (Quantity > 0),
  CONSTRAINT UQ_Cart_Product UNIQUE (CartId, ProductId)
);

CREATE TABLE Orders (
  Id               INT IDENTITY PRIMARY KEY,
  CustomerId       INT NOT NULL REFERENCES Users(Id),
  Status           NVARCHAR(30) NOT NULL, -- Pending/Processing/Shipping/Completed/Cancelled
  TotalPrice       DECIMAL(18,2) NOT NULL,
  ShippingAddress  NVARCHAR(500) NOT NULL,
  Phone            NVARCHAR(30) NOT NULL,
  Note             NVARCHAR(500) NULL,
  CreatedAt        DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  UpdatedAt        DATETIME2 NULL
);

CREATE TABLE OrderItems (
  Id          INT IDENTITY PRIMARY KEY,
  OrderId     INT NOT NULL REFERENCES Orders(Id) ON DELETE CASCADE,
  ProductId   INT NOT NULL REFERENCES Products(Id),
  ProductName NVARCHAR(200) NOT NULL, -- snapshot
  UnitPrice   DECIMAL(18,2) NOT NULL,
  Quantity    INT NOT NULL CHECK (Quantity > 0)
);

CREATE TABLE ProductReviews (
  Id         INT IDENTITY PRIMARY KEY,
  ProductId  INT NOT NULL REFERENCES Products(Id),
  UserId     INT NOT NULL REFERENCES Users(Id),
  Rating     TINYINT NOT NULL CHECK (Rating BETWEEN 1 AND 5),
  Comment    NVARCHAR(1000) NULL,
  CreatedAt  DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  CONSTRAINT UQ_Review UNIQUE (ProductId, UserId)
);

-- ========== SALES: QUOTATION / DESIGN ==========
CREATE TABLE QuotationRequests (
  Id           INT IDENTITY PRIMARY KEY,
  CustomerId   INT NOT NULL REFERENCES Users(Id),
  Title        NVARCHAR(200) NOT NULL,
  Description  NVARCHAR(MAX) NULL,
  Status       NVARCHAR(30) NOT NULL DEFAULT 'Pending',
  Reply        NVARCHAR(MAX) NULL,
  HandledById  INT NULL REFERENCES Users(Id),
  CreatedAt    DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  UpdatedAt    DATETIME2 NULL
);

CREATE TABLE Quotations (
  Id                  INT IDENTITY PRIMARY KEY,
  QuotationRequestId  INT NULL REFERENCES QuotationRequests(Id),
  CustomerId          INT NOT NULL REFERENCES Users(Id),
  Amount              DECIMAL(18,2) NOT NULL,
  Status              NVARCHAR(30) NOT NULL, -- Draft/PendingApproval/Approved/Rejected
  Notes               NVARCHAR(1000) NULL,
  CreatedById         INT NOT NULL REFERENCES Users(Id),
  ApprovedById        INT NULL REFERENCES Users(Id),
  CreatedAt           DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  UpdatedAt           DATETIME2 NULL
);

CREATE TABLE DesignRequests (
  Id           INT IDENTITY PRIMARY KEY,
  CustomerId   INT NOT NULL REFERENCES Users(Id),
  Title        NVARCHAR(200) NOT NULL,
  Style        NVARCHAR(100) NULL,
  Budget       DECIMAL(18,2) NULL,
  Status       NVARCHAR(30) NOT NULL DEFAULT 'New',
  Notes        NVARCHAR(MAX) NULL,
  AssignedToId INT NULL REFERENCES Users(Id),
  CreatedAt    DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  UpdatedAt    DATETIME2 NULL
);

CREATE TABLE DesignRequestAttachments (
  Id               INT IDENTITY PRIMARY KEY,
  DesignRequestId  INT NOT NULL REFERENCES DesignRequests(Id) ON DELETE CASCADE,
  Url              NVARCHAR(500) NOT NULL
);

CREATE TABLE InteriorDesigns (
  Id          INT IDENTITY PRIMARY KEY,
  Title       NVARCHAR(200) NOT NULL,
  Category    NVARCHAR(100) NULL,
  ImageUrl    NVARCHAR(500) NULL,
  Description NVARCHAR(MAX) NULL,
  IsPublished BIT NOT NULL DEFAULT 1,
  CreatedAt   DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

-- ========== CHAT ==========
CREATE TABLE ChatThreads (
  Id          INT IDENTITY PRIMARY KEY,
  CustomerId  INT NOT NULL UNIQUE REFERENCES Users(Id),
  CreatedAt   DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE ChatMessages (
  Id          INT IDENTITY PRIMARY KEY,
  ThreadId    INT NOT NULL REFERENCES ChatThreads(Id) ON DELETE CASCADE,
  SenderId    INT NOT NULL REFERENCES Users(Id),
  Content     NVARCHAR(MAX) NOT NULL,
  CreatedAt   DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

-- ========== PRODUCTION / DELIVERY ==========
CREATE TABLE ProductionOrders (
  Id               INT IDENTITY PRIMARY KEY,
  OrderId          INT NOT NULL REFERENCES Orders(Id),
  Status           NVARCHAR(30) NOT NULL, -- Queued/InProgress/Done/Blocked
  ProgressPercent  INT NOT NULL DEFAULT 0 CHECK (ProgressPercent BETWEEN 0 AND 100),
  AssignedToId     INT NULL REFERENCES Users(Id),
  Deadline         DATE NULL,
  CreatedAt        DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  UpdatedAt        DATETIME2 NULL
);

CREATE TABLE Deliveries (
  Id              INT IDENTITY PRIMARY KEY,
  OrderId         INT NOT NULL UNIQUE REFERENCES Orders(Id),
  DeliveryStatus  NVARCHAR(30) NOT NULL, -- Preparing/OutForDelivery/Delivered/Failed
  Note            NVARCHAR(500) NULL,
  DeliveredAt     DATETIME2 NULL,
  UpdatedAt       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

-- ========== CMS / LOGS ==========
CREATE TABLE Contents (
  Id           INT IDENTITY PRIMARY KEY,
  Title        NVARCHAR(250) NOT NULL,
  Slug         NVARCHAR(250) NOT NULL UNIQUE,
  Type         NVARCHAR(50) NOT NULL, -- Blog, Banner, Page
  Body         NVARCHAR(MAX) NULL,
  CoverUrl     NVARCHAR(500) NULL,
  IsPublished  BIT NOT NULL DEFAULT 0,
  PublishedAt  DATETIME2 NULL,
  CreatedAt    DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  UpdatedAt    DATETIME2 NULL
);

CREATE TABLE SystemLogs (
  Id           BIGINT IDENTITY PRIMARY KEY,
  ActorUserId  INT NULL REFERENCES Users(Id),
  Action       NVARCHAR(100) NOT NULL,
  Entity       NVARCHAR(100) NULL,
  EntityId     NVARCHAR(50) NULL,
  Detail       NVARCHAR(MAX) NULL,
  CreatedAt    DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

-- Seed roles
INSERT INTO Roles (Name, Description) VALUES
 (N'Customer', N'Khách hàng'),
 (N'Sales', N'Nhân viên kinh doanh'),
 (N'Production', N'Nhân viên sản xuất'),
 (N'Manager', N'Quản lý'),
 (N'Admin', N'Quản trị hệ thống');
```

### Index gợi ý
```sql
CREATE INDEX IX_Products_Category ON Products(CategoryId);
CREATE INDEX IX_Orders_Customer ON Orders(CustomerId);
CREATE INDEX IX_Orders_Status ON Orders(Status);
CREATE INDEX IX_OrderItems_Order ON OrderItems(OrderId);
CREATE INDEX IX_ChatMessages_Thread ON ChatMessages(ThreadId);
CREATE INDEX IX_SystemLogs_Created ON SystemLogs(CreatedAt DESC);
```

---

## 5. Quy tắc backend (Clean Architecture .NET)

Khuyến nghị theo yêu cầu môn PRN:

```
API (Controllers)
  → Application (Services / UseCases / DTOs / Validators)
    → Domain (Entities, Enums, Domain rules)
      → Infrastructure (EF Core DbContext, Repositories, Email, JWT)
```

- **Không** để Controller gọi DbContext trực tiếp.
- Checkout / cập nhật stock: **transaction** + kiểm tra tồn; khi mất kết nối DB → rollback, trả 503/`message`.
- Phân quyền bằng `[Authorize(Roles = "...")]` khớp FE `RequireAuth`.
- Thống nhất status enum với FE (`Pending|Processing|Shipping|Completed|Cancelled`).
- CORS cho Vite (`http://localhost:5173`).
- Swagger + seed data (1 user mỗi role).

---

## 6. Phân task backend (gợi ý board)

| # | Task | API / DB | Owner gợi ý |
|---|---|---|---|
| B1 | Auth + JWT + Roles seed | Auth tables | Trung (FR1,2) |
| B2 | Categories + Products CRUD + search | Catalog | Đức (FR3,4) |
| B3 | Cart + Checkout + Orders | Cart/Order | Hoàng (FR5,6) |
| B4 | Reviews + Profile | Reviews/Users profile | Hoàng (FR8) |
| B5 | Sales orders + Quotation + Design | Sales modules | Hiệp (FR9–11) |
| B6 | Chat CSKH | Chat | Hiệp |
| B7 | Production + Delivery | Production/Delivery | Phú (FR12,14) |
| B8 | Analytics / Revenue / Best-selling | Analytics | Phú / Manager |
| B9 | Admin Users/Roles/Contents/Logs | Admin | Trung (FR13) |
| B10 | System resilience (retry, tx, logging) | cross-cutting | cả team |

---

## 7. Env FE cần khi nối backend

```
VITE_API_BASE_URL=https://localhost:5001
```

Sau khi API sẵn sàng, login thật sẽ đi qua `authService.login` → nếu 401/ network sẽ fallback mock (có thể tắt mock khi production).

---

## 8. Mapping màn hình FE → API

| Route FE | Role | API chính |
|---|---|---|
| `/login` `/register` `/forgot-password` | Public | `/api/auth/*` |
| `/` `/products` `/products/:id` `/search` | Public/Customer | products, Blog/contents |
| `/cart` `/checkout` | Customer | cart, orders/checkout |
| `/orders` `/orders/:id` | Customer | orders |
| `/review` `/products/:id/review` | Customer | reviews |
| `/design` | Customer | interior-designs |
| `/chat` | Customer | chat/messages, send |
| `/profile` | All logged-in | users/profile |
| `/admin/*` | Admin | users, roles, categories, contents, systemlogs |
| `/manager/*` | Manager | products, orders, analytics, revenue |
| `/sales/*` | Sales | sales/orders, quotations, design-requests, chat |
| `/production/*` | Production | production/*, delivery/* |

---

*File này là nguồn sự thật tạm thời giữa FE và BE — cập nhật khi contract đổi.*
