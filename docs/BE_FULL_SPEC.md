# Interior Studio — DB + API Backend Spec (đồng bộ Frontend)

> Rà toàn project: `services` → `mock/handlers` → pages.  
> File kèm: `docs/MSSQL_SCHEMA.md` (script CREATE).  
> Wire format: **JSON camelCase**. DB: **PascalCase**. Roles FE: Customer · Sales · Manager · Admin.

---

# PHẦN 1 — CƠ SỞ DỮ LIỆU (MSSQL)

## 1.1 Quy chuẩn

| Quy tắc | Chi tiết |
|---------|----------|
| PK | `Id INT IDENTITY(1,1)` (SystemLogs: `BIGINT`) |
| FK | `XxxId` + `REFERENCES` + index |
| Soft delete | `IsDeleted BIT DEFAULT 0` (Products, Users) |
| Audit | `CreatedAt DATETIME2 DEFAULT SYSUTCDATETIME()`, `UpdatedAt` nullable |
| Tiền | `DECIMAL(18,2)` |
| Unique | Email, Slug, (CartId+ProductId), (ProductId+UserId) review |
| Snapshot | OrderItems giữ tên/giá lúc checkout (không join live giá) |
| Transaction | Checkout: Order + Items + trừ Stock + clear Cart (+ ProductionOrder) |

### Enum chuẩn (chuỗi NVARCHAR hoặc bảng lookup)

| Enum | Giá trị |
|------|---------|
| Role | Customer, Sales, Manager, Admin |
| OrderStatus | Pending, Processing, Shipping, Completed, Cancelled |
| QuotationRequestStatus | Pending, Replied |
| QuotationStatus | PendingApproval, Approved, Rejected |
| DesignRequestStatus | New, InReview, Quoted, Done |
| InteriorCategory | Living, Bedroom, Workspace, Kitchen |
| ProductionStatus | Queued, InProgress, Done, Blocked |
| ProgressStatus | PENDING, PREPARING, PACKING, SHIPPING, DELIVERED |
| DeliveryStatus | Preparing, OutForDelivery, Delivered, Failed |
| ChatSenderRole | Customer, Sales |
| ContentType | Blog, Guide, News |

---

## 1.2 Sơ đồ liên kết

```
Roles 1 ─── * Users
Users 1 ─── 1 Carts 1 ─── * CartItems * ─── 1 Products
Users 1 ─── * Orders 1 ─── * OrderItems * ─── 1 Products
Users 1 ─── * ProductReviews * ─── 1 Products
Categories 1 ─── * Products 1 ─── 0..1 ProductSpecs
Products 1 ─── * ProductImages

Users 1 ─── * QuotationRequests * ─── * Products (QuotationRequestProducts)
QuotationRequests 0..1 ─── * Quotations * ─── * Products (QuotationProducts)

Users 1 ─── * DesignRequests * ─── 0..1 InteriorDesigns
InteriorDesigns 1 ─── * (Images | Highlights | Specs | Materials | Packages)
InteriorDesigns * ─── * Products (InteriorDesignProducts)
DesignRequests * ─── * Products (DesignRequestProducts)

Users 1 ─── 1 ChatThreads 1 ─── * ChatMessages
Orders 1 ─── 0..1 ProductionOrders
Orders 1 ─── 0..1 Deliveries
Users 0..1 ─── * SystemLogs
```

**Cardinality**
- 1–1: User↔Cart, Product↔ProductSpecs, Order↔Delivery  
- 1–N: Category→Products, Order→OrderItems, Thread→Messages  
- N–N: Quotation↔Product, InteriorDesign↔Product, DesignRequest↔Product  

---

## 1.3 Chi tiết từng bảng

### A. Identity

#### Roles
| Cột | Kiểu | Null | Ghi chú |
|-----|------|------|---------|
| Id | INT IDENTITY | PK | |
| Name | NVARCHAR(50) | NOT NULL UNIQUE | Customer/Sales/Manager/Admin |
| Description | NVARCHAR(255) | NULL | |
| CreatedAt | DATETIME2 | NOT NULL | |

**Index:** `UQ_Roles_Name`

#### Users
| Cột | Kiểu | Null | Ghi chú |
|-----|------|------|---------|
| Id | INT IDENTITY | PK | |
| RoleId | INT | FK→Roles | |
| Email | NVARCHAR(256) | NOT NULL UNIQUE | |
| PasswordHash | NVARCHAR(512) | NOT NULL | |
| FullName | NVARCHAR(150) | NOT NULL | FE: name |
| Phone | NVARCHAR(30) | NULL | |
| AvatarUrl | NVARCHAR(500) | NULL | |
| Address | NVARCHAR(500) | NULL | profile |
| IsLocked | BIT | NOT NULL DEFAULT 0 | Admin khóa |
| IsActive | BIT | NOT NULL DEFAULT 1 | |
| IsDeleted | BIT | NOT NULL DEFAULT 0 | |
| CreatedAt / UpdatedAt | DATETIME2 | | |

**Index:** `IX_Users_RoleId`, `UQ_Users_Email`, `IX_Users_IsLocked`

#### RefreshTokens / PasswordResetTokens
| Bảng | Cột chính |
|------|-----------|
| RefreshTokens | Id, UserId FK, Token, ExpiresAt, CreatedAt, RevokedAt |
| PasswordResetTokens | Id, UserId FK, Token, ExpiresAt, UsedAt |

**Index:** `IX_RefreshTokens_UserId`, `IX_PasswordReset_Token`

---

### B. Catalog

#### Categories
| Cột | Kiểu | Ghi chú |
|-----|------|---------|
| Id, Name, Description, IsActive, CreatedAt | | Sofa/Bàn/Ghế… |

#### Products
| Cột | Kiểu | Ghi chú |
|-----|------|---------|
| Id | INT PK | |
| CategoryId | INT FK NULL | |
| Name | NVARCHAR(200) | |
| Description | NVARCHAR(MAX) | |
| Price | DECIMAL(18,2) | giá studio |
| MarketPrice | DECIMAL(18,2) NULL | so sánh / % giảm |
| Stock | INT ≥ 0 | |
| ImageUrl | NVARCHAR(500) | cover |
| IsActive | BIT | hiện storefront |
| IsDeleted | BIT | soft delete |
| CreatedAt, UpdatedAt | | |

**Index:** `IX_Products_CategoryId`, `IX_Products_IsActive` (filter IsDeleted=0), `IX_Products_Name`

#### ProductSpecs (1–1)
| Cột | Kiểu |
|-----|------|
| ProductId | PK/FK CASCADE |
| Dimensions, Material, Origin, Finish | NVARCHAR |
| WeightKg | DECIMAL(10,2) |
| WarrantyMonths | INT |

#### ProductImages (1–N)
| Cột | Kiểu |
|-----|------|
| Id, ProductId FK, Url, SortOrder | |

---

### C. Cart / Order / Review

#### Carts / CartItems
| Bảng | Cột | Ràng buộc |
|------|-----|-----------|
| Carts | Id, UserId UNIQUE FK, UpdatedAt | 1 cart / user |
| CartItems | Id, CartId FK, ProductId FK, Quantity > 0 | UQ (CartId, ProductId) |

**Index:** `IX_CartItems_ProductId`

#### Orders
| Cột | Kiểu | Ghi chú |
|-----|------|---------|
| Id | PK | |
| CustomerId | FK Users | |
| Status | NVARCHAR(30) | OrderStatus enum |
| TotalPrice | DECIMAL(18,2) | |
| ShippingAddress | NVARCHAR(500) | snapshot |
| Phone | NVARCHAR(30) | snapshot |
| CustomerName | NVARCHAR(150) | snapshot lúc checkout |
| CustomerEmail | NVARCHAR(256) | snapshot |
| Note | NVARCHAR(500) | |
| CreatedAt, UpdatedAt | | |

**Index:** `IX_Orders_CustomerId`, `IX_Orders_Status`, `IX_Orders_CreatedAt DESC`

#### OrderItems
| Cột | Kiểu | Ghi chú |
|-----|------|---------|
| Id, OrderId FK CASCADE, ProductId FK | | |
| ProductName | NVARCHAR(200) | **snapshot** |
| UnitPrice | DECIMAL(18,2) | **snapshot** = FE `price` |
| Quantity | INT > 0 | |

**Index:** `IX_OrderItems_OrderId`

#### ProductReviews
| Cột | Kiểu | Ghi chú |
|-----|------|---------|
| Id, ProductId FK, UserId FK | | |
| Rating | TINYINT 1–5 | |
| Comment | NVARCHAR(1000) | |
| CreatedAt | | |
| | | **UQ (ProductId, UserId)** |

---

### D. Quotation / Design request

#### QuotationRequests
| Cột | Ghi chú |
|-----|---------|
| Id, CustomerId FK, Title, Description | |
| Status | Pending / Replied |
| Reply | NVARCHAR(MAX) |
| HandledById | FK Users NULL |
| CreatedAt, UpdatedAt | |

#### QuotationRequestProducts (N–N)
PK `(QuotationRequestId, ProductId)`

#### Quotations
| Cột | Ghi chú |
|-----|---------|
| Id, QuotationRequestId FK NULL, CustomerId FK | |
| Title, Amount, Status, Notes | |
| CreatedById, ApprovedById | FK Users NULL |
| CreatedAt, UpdatedAt | |

#### QuotationProducts (N–N + qty)
`(QuotationId, ProductId)`, Quantity, UnitPrice NULL

#### DesignRequests
| Cột | Ghi chú |
|-----|---------|
| Id, CustomerId FK, InteriorDesignId FK NULL | |
| Title, Style, Budget, Status, Notes | |
| AssignedToId FK NULL | |
| CreatedAt, UpdatedAt | |

#### DesignRequestProducts / DesignRequestAttachments
Junction SP · file URL

---

### E. Interior designs (concept)

#### InteriorDesigns
| Cột | Ghi chú |
|-----|---------|
| Title, Category, Style, ImageUrl, Description | |
| AreaSqm, BudgetFrom, BudgetTo, TimelineWeeks | |
| StudioPrice, MarketAvgPrice | = priceCompare |
| IsPublished | BIT |
| CreatedAt, UpdatedAt | |

#### Child 1–N
| Bảng | Cột |
|------|-----|
| InteriorDesignImages | Url, SortOrder |
| InteriorDesignHighlights | Text, SortOrder |
| InteriorDesignSpecs | Label, Value, SortOrder |
| InteriorDesignMaterials | Name, Origin, Finish, Care |
| InteriorDesignPackages | Name, Price, Includes |
| InteriorDesignProducts | N–N với Products |

**Index:** `IX_InteriorDesigns_Category`, `IX_InteriorDesigns_IsPublished`

---

### F. Chat / Production / CMS

| Bảng | Cột chính | Index |
|------|-----------|-------|
| ChatThreads | CustomerId UNIQUE FK | UQ |
| ChatMessages | ThreadId FK, SenderId FK, SenderRole, Content, CreatedAt | IX_ThreadId, IX_CreatedAt |
| ProductionOrders | OrderId FK, Status, ProgressStatus, ProgressPercent, Deadline, AssignedToId | IX_OrderId, IX_Status |
| Deliveries | OrderId UNIQUE FK, DeliveryStatus, Note, DeliveredAt | UQ OrderId |
| Contents | Title, Slug UNIQUE, Type, Body, CoverUrl, IsPublished, PublishedAt | UQ Slug |
| SystemLogs | ActorUserId FK NULL, Action, Entity, EntityId, Detail, CreatedAt | IX_CreatedAt DESC, IX_Actor |

---

## 1.4 Coverage check (FE ↔ DB)

| Module FE | Covered? | Ghi chú |
|-----------|----------|---------|
| Auth login/register/forgot | ✅ | Register FE chưa gọi API — BE vẫn cần |
| Products + specs + marketPrice | ✅ | |
| Categories CRUD | ✅ | |
| Cart + Checkout + Orders | ✅ | Snapshot bắt buộc |
| Reviews | ✅ | |
| Profile + Users lock/role | ✅ | CreateUser nên có Password |
| Roles / Contents / Logs | ✅ | |
| Chat 2 phía | ✅ | |
| Quotation list/reply/approve | ✅ | **Thiếu POST create** từ Customer |
| Design request Sales update | ✅ | **Thiếu POST create** từ Customer |
| Interior design CRUD giàu | ✅ | |
| Analytics dashboards | ✅ | View/query từ Orders |
| Production/Delivery pages | ⚠️ | Có trong service; UI production **không route** — vẫn nên có bảng |
| Avatar upload | ❌ | FE chỉ preview local |
| Payment gateway | ❌ | Chỉ COD note trên checkout |

**Kết luận:** Schema trên **cover đủ** mock + UI hiện tại; bổ sung 3 API create (register wire, quotation-request, design-request) cho nghiệp vụ đầy đủ.

---

# PHẦN 2 — CONTROLLERS + SERVICES + DTO (In / Out)

## 2.1 Tổng quan Controller

| # | Controller | Prefix | Service |
|---|------------|--------|---------|
| 1 | AuthController | `api/auth` | IAuthService |
| 2 | ProductsController | `api/products` | IProductService, IReviewService |
| 3 | CartController | `api/cart` | ICartService |
| 4 | OrdersController | `api/orders` | IOrderService |
| 5 | SalesOrdersController | `api/sales/orders` | IOrderService |
| 6 | UsersController | `api/users` | IUserService |
| 7 | RolesController | `api/roles` | IRoleService |
| 8 | CategoriesController | `api/categories` | ICategoryService |
| 9 | ContentsController | `api/contents` | IContentService |
| 10 | SystemLogsController | `api/systemlogs` | ISystemLogService |
| 11 | ChatController | `api/chat` | IChatService |
| 12 | QuotationRequestsController | `api/quotation-requests` | IQuotationRequestService |
| 13 | QuotationsController | `api/quotations` | IQuotationService |
| 14 | DesignRequestsController | `api/design-requests` | IDesignRequestService |
| 15 | InteriorDesignsController | `api/interior-designs` | IInteriorDesignService |
| 16 | ProductionController | `api/production` | IProductionService |
| 17 | DeliveryController | `api/delivery/orders` | IDeliveryService |
| 18 | AnalyticsController | `api/analytics`, `api/revenue`, `api/sales/dashboard` | IAnalyticsService |

---

## 2.2 Chi tiết API (Method · In · Out)

### 1) AuthController + IAuthService

| Method | Path | Auth | Request (In) | Response (Out) |
|--------|------|------|--------------|----------------|
| POST | `/login` | — | `LoginRequest`: email, password, role? | `LoginResponse`: accessToken, id, name, email, role, phone, avatarUrl? |
| POST | `/register` | — | `RegisterRequest`: name, email, password, phone? | `RegisterResponse`: id, message **hoặc** LoginResponse |
| POST | `/forgot-password` | — | `ForgotPasswordRequest`: email | `{ message }` |

> BE: **không tin** `role` từ client lúc login — lấy từ DB. Role chỉ dùng mock FE.

---

### 2) ProductsController + IProductService / IReviewService

| Method | Path | In | Out |
|--------|------|----|-----|
| GET | `/` | — | `ProductDto[]` |
| GET | `/{id}` | id | `ProductDto` |
| GET | `/search?keyword=` | keyword | `ProductDto[]` |
| POST | `/` | `ProductUpsertDto` | `ProductDto` |
| PUT | `/{id}` | `ProductUpsertDto` | `ProductDto` |
| PUT | `/{id}/price` | `{ price }` | `ProductDto` |
| DELETE | `/{id}` | — | `{ success }` |
| GET | `/{id}/reviews` | — | `ReviewDto[]` |
| POST | `/{id}/reviews` | `CreateReviewDto` | `ReviewDto` |

**ProductDto / ProductUpsertDto**
```
id?, name, description, price, marketPrice, stock,
categoryId, categoryName?, imageUrl, isActive,
specs: { dimensions, material, origin, finish, weightKg, warrantyMonths }
```

**ReviewDto:** `id, productId, userId, userName, rating, comment, createdAt`  
**CreateReviewDto:** `rating, comment` (+ productId từ route)

---

### 3) CartController + ICartService

| Method | Path | In | Out |
|--------|------|----|-----|
| GET | `/` | user từ JWT | `CartItemDto[]` |
| POST | `/` | `{ productId, quantity }` | `CartItemDto` |
| PUT | `/{id}` | `{ quantity }` | `CartItemDto` |
| DELETE | `/{id}` | — | `{ success }` |

**CartItemDto:** `id, productId, productName, imageUrl, price, quantity`

---

### 4) OrdersController + IOrderService

| Method | Path | In | Out |
|--------|------|----|-----|
| POST | `/checkout` | `CheckoutRequest` | `OrderDto` |
| GET | `/` | JWT → Customer=mine / Staff=all | `OrderDto[]` |
| GET | `/{id}` | — | `OrderDto` |
| PUT | `/{id}/status` | `{ status }` | `OrderDto` |

**CheckoutRequest**
```
shippingAddress, phone, note?,
customerInfo?: { fullName, phone, address, note, paymentMethod },
items?: [{ productId, quantity, price }],  // optional; ưu tiên cart server
totalPrice?
```

**OrderDto** (chuẩn hóa 1 contract cho list + detail)
```
id, customerId, customerName, customerEmail, customerPhone,
shippingAddress, phone, address?, status, totalPrice, createdAt,
items: [{ productId, productName, quantity, price }]
```
> Alias FE cũ: `fullName`←customerName, item.`name`←productName.

---

### 5) SalesOrdersController

| Method | Path | In | Out |
|--------|------|----|-----|
| GET | `/` | — | `OrderDto[]` |
| PUT | `/{id}` | `{ status }` | `OrderDto` |

---

### 6) UsersController + IUserService

| Method | Path | In | Out |
|--------|------|----|-----|
| GET | `/profile` | JWT | `UserProfileDto` |
| PUT | `/profile` | `UpdateProfileDto` | `UserProfileDto` |
| GET | `/` | Admin | `UserDto[]` |
| POST | `/` | `CreateUserDto` | `UserDto` |
| PUT | `/{id}/role` | `{ role }` | `UserDto` |
| PUT | `/{id}/lock` | `{ isLocked }` | `UserDto` |

**UserProfileDto:** `id, name, fullName, email, phone, address, role, avatarUrl`  
**UpdateProfileDto:** `name, fullName, phone, address`  
**UserDto:** `id, name, email, phone, role, status, isLocked`  
**CreateUserDto:** `name, email, phone?, role, password?`  

Rule: không đổi role/khóa **chính mình** hoặc user **rank ≥ actor**.

---

### 7–10) Roles / Categories / Contents / SystemLogs

| Controller | Methods | In | Out |
|------------|---------|----|-----|
| Roles | GET, POST, PUT/{id}, DELETE/{id} | `{ name, description }` | `RoleDto` / `{success}` |
| Categories | CRUD | `{ name, description }` | `CategoryDto` |
| Contents | CRUD | `{ title, slug, type, body, coverUrl, isPublished, publishedAt? }` | `ContentDto` |
| SystemLogs | GET | — | `SystemLogDto[]`: id, actorUserId, actorName?, action, entity, entityId, detail, createdAt |

---

### 11) ChatController + IChatService

| Method | Path | In | Out |
|--------|------|----|-----|
| GET | `/messages` | JWT Customer | `ChatMessageDto[]` |
| GET | `/customers` | Sales+ | `ChatCustomerDto[]` |
| GET | `/messages/{customerId}` | Sales+ | `ChatMessageDto[]` |
| POST | `/send` | `{ content, customerId?, senderRole? }` | `ChatMessageDto` |

**ChatMessageDto:** `id, customerId, senderRole, content, createdAt`  
**ChatCustomerDto:** `id, name, email`

---

### 12) QuotationRequestsController + IQuotationRequestService

| Method | Path | In | Out |
|--------|------|----|-----|
| GET | `/` | Sales+ | `QuotationRequestDto[]` |
| **POST** | `/` | Customer — **nên có** | `CreateQuotationRequestDto` → Dto |
| PUT | `/{id}/reply` | `{ reply, replyNote, status }` | Dto |
| PUT | `/{id}` | partial update | Dto |

**QuotationRequestDto**
```
id, customerId, customerName, title, description, productIds[],
status, reply, replyNote, createdAt,
products?: ProductDto[], estimateTotal?, marketTotal?
```
**CreateQuotationRequestDto:** `title, description, productIds[]`

---

### 13) QuotationsController + IQuotationService

| Method | Path | In | Out |
|--------|------|----|-----|
| GET | `/` | Sales+ | `QuotationDto[]` |
| **POST** | `/` | Sales — **nên có** | `CreateQuotationDto` → Dto |
| PUT | `/{id}` | `{ status, notes, note? }` | Dto |

**QuotationDto**
```
id, quotationRequestId, customerId, customerName, customerEmail,
title, amount, totalPrice?, productIds[], status, notes, createdAt,
items?: [{ productId, productName, quantity, price, marketPrice, stock, imageUrl, specs }],
catalogTotal?, marketTotal?, savings?
```
**CreateQuotationDto:** `quotationRequestId, title?, amount, productIds[], notes?`  
**UpdateQuotationDto:** `status` (Approved|Rejected), `notes`

---

### 14) DesignRequestsController + IDesignRequestService

| Method | Path | In | Out |
|--------|------|----|-----|
| GET | `/` | Sales+ | `DesignRequestDto[]` |
| GET | `/{id}` | — | Dto |
| **POST** | `/` | Customer — **nên có** | `CreateDesignRequestDto` |
| PUT | `/{id}` | `{ status, note, notes }` | Dto |

**DesignRequestDto:** `id, customerId, customerName, title, style, interiorDesignId, relatedProductIds[], budget, status, note, notes, attachments[], createdAt`  
**CreateDesignRequestDto:** `title, style?, interiorDesignId?, relatedProductIds[], budget?, note?`

---

### 15) InteriorDesignsController + IInteriorDesignService

| Method | Path | In | Out |
|--------|------|----|-----|
| GET | `/` | Public/Manager | `InteriorDesignListDto[]` (filter isPublished cho khách) |
| GET | `/{id}` | — | `InteriorDesignDetailDto` (+ relatedProducts) |
| POST | `/` | Manager+ | `InteriorDesignUpsertDto` |
| PUT | `/{id}` | UpsertDto | DetailDto |
| DELETE | `/{id}` | — | `{ success }` |

**InteriorDesignUpsertDto / DetailDto**
```
title, category, style, imageUrl, gallery[], description,
areaSqm, budgetFrom, budgetTo, timelineWeeks,
priceCompare: { studio, marketAvg },
relatedProductIds[], highlights[],
specs: [{ label, value }],
materials: [{ name, origin, finish, care }],
packages: [{ name, price, includes }],
isPublished
+ detail: relatedProducts: ProductDto[]
```

---

### 16–17) Production / Delivery

| Method | Path | In | Out |
|--------|------|----|-----|
| GET | `/api/production/dashboard` | — | `{ totalOrders, pending, preparing, shipping, delivered }` |
| GET | `/api/production/orders` | — | `ProductionOrderDto[]` |
| GET | `/api/production/orders/{id}` | — | Detail + items |
| PUT | `/api/production/orders/{id}/status` | `{ status }` | Dto |
| GET | `/api/production/progress` | — | `{ id, orderId, customerName, status, progressPercent }[]` |
| GET | `/api/delivery/orders` | — | `DeliveryOrderDto[]` |
| PUT | `/api/delivery/orders/{orderId}` | `{ status, note?, deliveredAt? }` | Dto |

**ProductionOrderDto:** `id, orderId, customerName, address, shippingAddress, status, progressStatus, progressPercent, deadline, createdAt, items[]`

---

### 18) AnalyticsController + IAnalyticsService

| Method | Path | Out |
|--------|------|-----|
| GET | `/api/analytics/dashboard` | `ManagerDashboardDto`: totalRevenue, totalOrders, totalProducts, totalCustomers, totalUsers, bestSellingProduct |
| GET | `/api/analytics/best-selling-products` | `{ id, productId?, name, sold\|soldQuantity, revenue }[]` |
| GET | `/api/revenue/report` | `{ id, date, orderCode, amount }[]` |
| GET | `/api/sales/dashboard` | `SalesDashboardDto`: stats{totalRequests, pendingRequests, totalQuotations, approvedQuotations}, recentRequests[], recentQuotations[] |

---

## 2.3 Service layer — method signatures (C#)

```csharp
IAuthService:
  LoginAsync(LoginRequestDto) → LoginResponseDto
  RegisterAsync(RegisterRequestDto) → RegisterResponseDto
  ForgotPasswordAsync(ForgotPasswordRequestDto) → void

IProductService:
  GetAllAsync() / GetByIdAsync(id) / SearchAsync(keyword)
  CreateAsync(ProductUpsertDto) / UpdateAsync(id, dto)
  UpdatePriceAsync(id, price) / DeleteAsync(id)

IReviewService:
  GetByProductAsync(productId) / CreateAsync(productId, userId, CreateReviewDto)

ICartService:
  GetAsync(userId) / AddAsync(userId, AddCartItemDto)
  UpdateAsync(userId, cartItemId, UpdateCartItemDto) / RemoveAsync(...)

IOrderService:
  GetMineAsync(userId) / GetAllAsync() / GetByIdAsync(id)
  CheckoutAsync(userId, CheckoutRequestDto)  // TRANSACTION
  UpdateStatusAsync(id, UpdateOrderStatusDto)

IUserService:
  GetProfileAsync / UpdateProfileAsync
  GetAllAsync / CreateAsync(dto, actor)
  UpdateRoleAsync(id, dto, actor) / SetLockedAsync(id, dto, actor)

IRoleService / ICategoryService / IContentService: CRUD
ISystemLogService: GetAllAsync()
IChatService: GetMyMessages / GetCustomers / GetCustomerMessages / Send
IQuotationRequestService: GetAll / Create / Reply / Update
IQuotationService: GetAll / Create / Update
IDesignRequestService: GetAll / GetById / Create / Update
IInteriorDesignService: GetAll / GetById / Create / Update / Delete
IProductionService: GetDashboard / GetOrders / GetOrder / UpdateStatus / GetProgress
IDeliveryService: GetOrders / Update
IAnalyticsService: GetManagerDashboard / GetBestSelling / GetRevenueReport / GetSalesDashboard
```

---

## 2.4 Ma trận quyền API (tóm tắt)

| Area | Customer | Sales | Manager | Admin |
|------|----------|-------|---------|-------|
| Catalog read | ✓ | ✓ | ✓ | ✓ |
| Product/Category write | — | — | ✓ | ✓ |
| Cart / Checkout / My orders | ✓ | — | — | — |
| Sales orders / Quotes / Design req | — | ✓ | — | ✓ |
| Interior design write | — | — | ✓ | ✓ |
| Chat | own | inbox | — | ✓ |
| Users / Roles / Contents / Logs | — | — | — | ✓ |
| Analytics manager | — | — | ✓ | ✓ |
| Analytics sales | — | ✓ | — | ✓ |

---

## 2.5 Ưu tiên implement BE

1. **P0:** Auth, Users, Categories, Products(+Specs), Cart, Orders(+checkout TX), Reviews  
2. **P1:** InteriorDesigns CRUD, DesignRequests (+Create), QuotationRequests (+Create), Quotations (+Create/Approve), Chat  
3. **P2:** Analytics, Contents, Roles, SystemLogs, User lock/role hierarchy  
4. **P3:** ProductionOrders, Deliveries (nếu team còn module)

---

*Tài liệu này + `docs/MSSQL_SCHEMA.md` đủ để scaffold ASP.NET Controllers/Services và tạo DB MSSQL cover toàn FE hiện tại.*
