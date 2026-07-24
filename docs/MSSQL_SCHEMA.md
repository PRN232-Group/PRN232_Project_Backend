# MSSQL Schema — Interior Studio (đồng bộ Frontend mock)

> Nguồn: luồng UI → `application/services` → `mock/handlers` + `mock/data`.  
> Naming: PascalCase, PK `Id`, soft-delete `IsDeleted`, audit `CreatedAt`/`UpdatedAt`.  
> Roles hiện tại FE: **Customer · Sales · Manager · Admin** (không còn Production trên UI).

---

## A. Luồng UI → Service (tóm tắt)

```
┌─────────────┐     axios apiClient      ┌──────────────────┐
│ Pages / UI  │ ───────────────────────► │ application/     │
│ (role gate) │                          │ services/*.js    │
└─────────────┘                          └────────┬─────────┘
                                                  │
                     ┌────────────────────────────▼────────────┐
                     │  GET/POST/PUT/DELETE /api/...           │
                     │  Mock: handlers.js  |  Prod: MSSQL API  │
                     └─────────────────────────────────────────┘
```

| Role | Màn hình chính | Services chính |
|------|----------------|----------------|
| **Customer** | Home, Products, Design, Cart, Checkout, Orders, Profile, Chat, Review | product, cart, order, interiorDesign, user, chat, review, auth |
| **Sales** | Dashboard, Orders, Báo giá, Duyệt BG, Design requests, Chat | analytics, order, quotation, designRequest, chat, product |
| **Manager** | Dashboard, Products, Concept thiết kế, Categories, Prices, Best-selling, Revenue | product, category, interiorDesign, analytics |
| **Admin** | Dashboard + full Sales/Manager + Users, Roles, Contents, Logs | user, role, content, systemLog + manager/sales |

### Checkout (ràng buộc quan trọng)
`Cart` → `POST /api/orders/checkout` → tạo `Orders` + `OrderItems` + (optional) `ProductionOrders`, trừ `Products.Stock`, xóa cart — **1 transaction**.

---

## B. Danh sách bảng (chi tiết)

### 1. Identity / Auth

| Bảng | Mục đích | Cột chính |
|------|----------|-----------|
| **Roles** | Catalog role | Id, Name (unique), Description |
| **Users** | Tài khoản | Id, RoleId FK, Email unique, PasswordHash, FullName, Phone, AvatarUrl, **IsLocked**, IsActive, IsDeleted |
| **RefreshTokens** | JWT refresh | Id, UserId FK, Token, ExpiresAt, RevokedAt |
| **PasswordResetTokens** | Quên MK | Id, UserId FK, Token, ExpiresAt, UsedAt |

> FE: khóa user = `IsLocked=1` (status Locked). Không soft-delete user khi Admin “khóa”.

---

### 2. Catalog

| Bảng | Mục đích | Cột chính |
|------|----------|-----------|
| **Categories** | Danh mục SP | Id, Name, Description, IsActive |
| **Products** | Sản phẩm | Id, CategoryId FK, Name, Description, **Price**, **MarketPrice**, Stock, ImageUrl, IsActive, IsDeleted |
| **ProductSpecs** | Thông số 1–1 | ProductId PK/FK, Dimensions, Material, Origin, Finish, WeightKg, WarrantyMonths |
| **ProductImages** | Gallery SP (optional) | Id, ProductId FK, Url, SortOrder |

---

### 3. Cart / Order / Review

| Bảng | Mục đích | Cột chính |
|------|----------|-----------|
| **Carts** | Giỏ theo user | Id, UserId unique FK |
| **CartItems** | Dòng giỏ | Id, CartId FK, ProductId FK, Quantity (UQ CartId+ProductId) |
| **Orders** | Đơn hàng | Id, CustomerId FK, Status, TotalPrice, ShippingAddress, Phone, Note, CreatedAt |
| **OrderItems** | Snapshot dòng đơn | Id, OrderId FK, ProductId FK, ProductName, UnitPrice, Quantity |
| **ProductReviews** | Đánh giá | Id, ProductId FK, UserId FK, Rating 1–5, Comment (UQ ProductId+UserId) |

**Order.Status:** `Pending` \| `Processing` \| `Shipping` \| `Completed` \| `Cancelled`

---

### 4. Sales — Báo giá / Yêu cầu thiết kế

| Bảng | Mục đích | Cột chính |
|------|----------|-----------|
| **QuotationRequests** | Yêu cầu BG từ khách | Id, CustomerId FK, Title, Description, Status, Reply, HandledById FK?, CreatedAt |
| **QuotationRequestProducts** | SP gắn YC | QuotationRequestId FK, ProductId FK (PK ghép) |
| **Quotations** | Báo giá / duyệt | Id, QuotationRequestId FK?, CustomerId FK, Title, Amount, Status, Notes, CreatedById?, ApprovedById? |
| **QuotationProducts** | SP trong BG | QuotationId FK, ProductId FK, Quantity, UnitPrice? |
| **DesignRequests** | YC thiết kế | Id, CustomerId FK, Title, Style, InteriorDesignId FK?, Budget, Status, Notes, AssignedToId? |
| **DesignRequestProducts** | SP liên quan YC | DesignRequestId FK, ProductId FK |
| **DesignRequestAttachments** | File đính kèm | Id, DesignRequestId FK, Url |

**QuotationRequest.Status:** `Pending` \| `Replied`  
**Quotation.Status:** `PendingApproval` \| `Approved` \| `Rejected`  
**DesignRequest.Status:** `New` \| `InReview` \| `Quoted` \| `Done`

---

### 5. Concept thiết kế (storefront + Manager CRUD)

| Bảng | Mục đích | Cột chính |
|------|----------|-----------|
| **InteriorDesigns** | Concept | Id, Title, Category, Style, ImageUrl, Description, AreaSqm, BudgetFrom, BudgetTo, TimelineWeeks, StudioPrice, MarketAvgPrice, IsPublished |
| **InteriorDesignImages** | Gallery | Id, InteriorDesignId FK, Url, SortOrder |
| **InteriorDesignHighlights** | Bullet nổi bật | Id, InteriorDesignId FK, Text, SortOrder |
| **InteriorDesignSpecs** | Spec label/value | Id, InteriorDesignId FK, Label, Value, SortOrder |
| **InteriorDesignMaterials** | Vật liệu | Id, InteriorDesignId FK, Name, Origin, Finish, Care |
| **InteriorDesignPackages** | Gói giá | Id, InteriorDesignId FK, Name, Price, Includes |
| **InteriorDesignProducts** | SP liên quan | InteriorDesignId FK, ProductId FK |

**InteriorDesigns.Category:** `Living` \| `Bedroom` \| `Workspace` \| `Kitchen`

---

### 6. Chat CSKH

| Bảng | Mục đích | Cột chính |
|------|----------|-----------|
| **ChatThreads** | 1 thread / customer | Id, CustomerId unique FK |
| **ChatMessages** | Tin nhắn | Id, ThreadId FK, SenderId FK, SenderRole (`Customer`/`Sales`), Content, CreatedAt |

---

### 7. Production / Delivery (FE còn service; role Production đã gỡ khỏi UI)

| Bảng | Mục đích | Cột chính |
|------|----------|-----------|
| **ProductionOrders** | Lệnh SX từ Order | Id, OrderId FK, Status, ProgressStatus?, ProgressPercent, Deadline, AssignedToId? |
| **Deliveries** | Giao hàng | Id, OrderId unique FK, DeliveryStatus, Note, DeliveredAt |

**ProductionOrders.Status:** `Queued` \| `InProgress` \| `Done` \| `Blocked`  
**Deliveries.DeliveryStatus:** `Preparing` \| `OutForDelivery` \| `Delivered` \| `Failed`

---

### 8. CMS / Logs

| Bảng | Mục đích | Cột chính |
|------|----------|-----------|
| **Contents** | Blog / CMS | Id, Title, Slug unique, Type, Body, CoverUrl, IsPublished, PublishedAt |
| **SystemLogs** | Audit Admin | Id BIGINT, ActorUserId FK?, Action, Entity, EntityId, Detail, CreatedAt |

---

## C. Quan hệ (ER rút gọn)

```
Roles 1──* Users
Users 1──1 Carts 1──* CartItems *──1 Products
Users 1──* Orders 1──* OrderItems *──1 Products
Users 1──* ProductReviews *──1 Products
Categories 1──* Products 1──0..1 ProductSpecs

Users 1──* QuotationRequests 1──* QuotationRequestProducts *──1 Products
QuotationRequests 0..1──* Quotations 1──* QuotationProducts *──1 Products

Users 1──* DesignRequests *──0..1 InteriorDesigns
InteriorDesigns 1──* (Images|Highlights|Specs|Materials|Packages)
InteriorDesigns *──* Products (InteriorDesignProducts)

Users 1──1 ChatThreads 1──* ChatMessages
Orders 1──0..1 ProductionOrders
Orders 1──0..1 Deliveries
```

---

## D. Script CREATE (MSSQL) — bản cập nhật theo FE

```sql
/* ===== IDENTITY ===== */
CREATE TABLE Roles (
  Id          INT IDENTITY(1,1) PRIMARY KEY,
  Name        NVARCHAR(50)  NOT NULL UNIQUE, -- Customer, Sales, Manager, Admin
  Description NVARCHAR(255) NULL,
  CreatedAt   DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE Users (
  Id           INT IDENTITY(1,1) PRIMARY KEY,
  RoleId       INT           NOT NULL REFERENCES Roles(Id),
  Email        NVARCHAR(256) NOT NULL UNIQUE,
  PasswordHash NVARCHAR(512) NOT NULL,
  FullName     NVARCHAR(150) NOT NULL,
  Phone        NVARCHAR(30)  NULL,
  AvatarUrl    NVARCHAR(500) NULL,
  IsLocked     BIT           NOT NULL DEFAULT 0, -- FE: khóa/mở khóa
  IsActive     BIT           NOT NULL DEFAULT 1,
  IsDeleted    BIT           NOT NULL DEFAULT 0,
  CreatedAt    DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
  UpdatedAt    DATETIME2     NULL
);

CREATE TABLE RefreshTokens (
  Id        INT IDENTITY(1,1) PRIMARY KEY,
  UserId    INT           NOT NULL REFERENCES Users(Id),
  Token     NVARCHAR(512) NOT NULL,
  ExpiresAt DATETIME2     NOT NULL,
  CreatedAt DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
  RevokedAt DATETIME2     NULL
);

CREATE TABLE PasswordResetTokens (
  Id        INT IDENTITY(1,1) PRIMARY KEY,
  UserId    INT           NOT NULL REFERENCES Users(Id),
  Token     NVARCHAR(128) NOT NULL,
  ExpiresAt DATETIME2     NOT NULL,
  UsedAt    DATETIME2     NULL
);

/* ===== CATALOG ===== */
CREATE TABLE Categories (
  Id          INT IDENTITY(1,1) PRIMARY KEY,
  Name        NVARCHAR(120) NOT NULL,
  Description NVARCHAR(500) NULL,
  IsActive    BIT           NOT NULL DEFAULT 1,
  CreatedAt   DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE Products (
  Id          INT IDENTITY(1,1) PRIMARY KEY,
  CategoryId  INT            NULL REFERENCES Categories(Id),
  Name        NVARCHAR(200)  NOT NULL,
  Description NVARCHAR(MAX)  NULL,
  Price       DECIMAL(18,2)  NOT NULL CHECK (Price >= 0),       -- giá studio
  MarketPrice DECIMAL(18,2)  NULL CHECK (MarketPrice IS NULL OR MarketPrice >= 0),
  Stock       INT            NOT NULL DEFAULT 0 CHECK (Stock >= 0),
  ImageUrl    NVARCHAR(500)  NULL,
  IsActive    BIT            NOT NULL DEFAULT 1, -- hiện storefront
  IsDeleted   BIT            NOT NULL DEFAULT 0,
  CreatedAt   DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
  UpdatedAt   DATETIME2      NULL
);

CREATE TABLE ProductSpecs (
  ProductId       INT            NOT NULL PRIMARY KEY REFERENCES Products(Id) ON DELETE CASCADE,
  Dimensions      NVARCHAR(120)  NULL,
  Material        NVARCHAR(200)  NULL,
  Origin          NVARCHAR(100)  NULL,
  Finish          NVARCHAR(120)  NULL,
  WeightKg        DECIMAL(10,2)  NULL,
  WarrantyMonths  INT            NULL
);

CREATE TABLE ProductImages (
  Id        INT IDENTITY(1,1) PRIMARY KEY,
  ProductId INT           NOT NULL REFERENCES Products(Id) ON DELETE CASCADE,
  Url       NVARCHAR(500) NOT NULL,
  SortOrder INT           NOT NULL DEFAULT 0
);

/* ===== CART / ORDER ===== */
CREATE TABLE Carts (
  Id        INT IDENTITY(1,1) PRIMARY KEY,
  UserId    INT       NOT NULL UNIQUE REFERENCES Users(Id),
  UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE CartItems (
  Id        INT IDENTITY(1,1) PRIMARY KEY,
  CartId    INT NOT NULL REFERENCES Carts(Id) ON DELETE CASCADE,
  ProductId INT NOT NULL REFERENCES Products(Id),
  Quantity  INT NOT NULL CHECK (Quantity > 0),
  CONSTRAINT UQ_Cart_Product UNIQUE (CartId, ProductId)
);

CREATE TABLE Orders (
  Id              INT IDENTITY(1,1) PRIMARY KEY,
  CustomerId      INT            NOT NULL REFERENCES Users(Id),
  Status          NVARCHAR(30)   NOT NULL, -- Pending/Processing/Shipping/Completed/Cancelled
  TotalPrice      DECIMAL(18,2)  NOT NULL,
  ShippingAddress NVARCHAR(500)  NOT NULL,
  Phone           NVARCHAR(30)   NOT NULL,
  Note            NVARCHAR(500)  NULL,
  CreatedAt       DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
  UpdatedAt       DATETIME2      NULL
);

CREATE TABLE OrderItems (
  Id          INT IDENTITY(1,1) PRIMARY KEY,
  OrderId     INT            NOT NULL REFERENCES Orders(Id) ON DELETE CASCADE,
  ProductId   INT            NOT NULL REFERENCES Products(Id),
  ProductName NVARCHAR(200)  NOT NULL, -- snapshot
  UnitPrice   DECIMAL(18,2)  NOT NULL,
  Quantity    INT            NOT NULL CHECK (Quantity > 0)
);

CREATE TABLE ProductReviews (
  Id        INT IDENTITY(1,1) PRIMARY KEY,
  ProductId INT            NOT NULL REFERENCES Products(Id),
  UserId    INT            NOT NULL REFERENCES Users(Id),
  Rating    TINYINT        NOT NULL CHECK (Rating BETWEEN 1 AND 5),
  Comment   NVARCHAR(1000) NULL,
  CreatedAt DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
  CONSTRAINT UQ_Review UNIQUE (ProductId, UserId)
);

/* ===== QUOTATION / DESIGN REQUEST ===== */
CREATE TABLE QuotationRequests (
  Id          INT IDENTITY(1,1) PRIMARY KEY,
  CustomerId  INT           NOT NULL REFERENCES Users(Id),
  Title       NVARCHAR(200) NOT NULL,
  Description NVARCHAR(MAX) NULL,
  Status      NVARCHAR(30)  NOT NULL DEFAULT N'Pending', -- Pending/Replied
  Reply       NVARCHAR(MAX) NULL,
  HandledById INT           NULL REFERENCES Users(Id),
  CreatedAt   DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
  UpdatedAt   DATETIME2     NULL
);

CREATE TABLE QuotationRequestProducts (
  QuotationRequestId INT NOT NULL REFERENCES QuotationRequests(Id) ON DELETE CASCADE,
  ProductId          INT NOT NULL REFERENCES Products(Id),
  PRIMARY KEY (QuotationRequestId, ProductId)
);

CREATE TABLE Quotations (
  Id                 INT IDENTITY(1,1) PRIMARY KEY,
  QuotationRequestId INT            NULL REFERENCES QuotationRequests(Id),
  CustomerId         INT            NOT NULL REFERENCES Users(Id),
  Title              NVARCHAR(200)  NULL,
  Amount             DECIMAL(18,2)  NOT NULL,
  Status             NVARCHAR(30)   NOT NULL, -- PendingApproval/Approved/Rejected
  Notes              NVARCHAR(1000) NULL,
  CreatedById        INT            NULL REFERENCES Users(Id),
  ApprovedById       INT            NULL REFERENCES Users(Id),
  CreatedAt          DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
  UpdatedAt          DATETIME2      NULL
);

CREATE TABLE QuotationProducts (
  QuotationId INT            NOT NULL REFERENCES Quotations(Id) ON DELETE CASCADE,
  ProductId   INT            NOT NULL REFERENCES Products(Id),
  Quantity    INT            NOT NULL DEFAULT 1 CHECK (Quantity > 0),
  UnitPrice   DECIMAL(18,2)  NULL,
  PRIMARY KEY (QuotationId, ProductId)
);

CREATE TABLE InteriorDesigns (
  Id             INT IDENTITY(1,1) PRIMARY KEY,
  Title          NVARCHAR(200)  NOT NULL,
  Category       NVARCHAR(50)   NULL, -- Living/Bedroom/Workspace/Kitchen
  Style          NVARCHAR(100)  NULL,
  ImageUrl       NVARCHAR(500)  NULL,
  Description    NVARCHAR(MAX)  NULL,
  AreaSqm        DECIMAL(10,2)  NULL,
  BudgetFrom     DECIMAL(18,2)  NULL,
  BudgetTo       DECIMAL(18,2)  NULL,
  TimelineWeeks  INT            NULL,
  StudioPrice    DECIMAL(18,2)  NULL,
  MarketAvgPrice DECIMAL(18,2)  NULL,
  IsPublished    BIT            NOT NULL DEFAULT 1,
  CreatedAt      DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
  UpdatedAt      DATETIME2      NULL
);

CREATE TABLE InteriorDesignImages (
  Id                INT IDENTITY(1,1) PRIMARY KEY,
  InteriorDesignId  INT           NOT NULL REFERENCES InteriorDesigns(Id) ON DELETE CASCADE,
  Url               NVARCHAR(500) NOT NULL,
  SortOrder         INT           NOT NULL DEFAULT 0
);

CREATE TABLE InteriorDesignHighlights (
  Id               INT IDENTITY(1,1) PRIMARY KEY,
  InteriorDesignId INT            NOT NULL REFERENCES InteriorDesigns(Id) ON DELETE CASCADE,
  Text             NVARCHAR(500)  NOT NULL,
  SortOrder        INT            NOT NULL DEFAULT 0
);

CREATE TABLE InteriorDesignSpecs (
  Id               INT IDENTITY(1,1) PRIMARY KEY,
  InteriorDesignId INT            NOT NULL REFERENCES InteriorDesigns(Id) ON DELETE CASCADE,
  Label            NVARCHAR(120)  NOT NULL,
  Value            NVARCHAR(300)  NOT NULL,
  SortOrder        INT            NOT NULL DEFAULT 0
);

CREATE TABLE InteriorDesignMaterials (
  Id               INT IDENTITY(1,1) PRIMARY KEY,
  InteriorDesignId INT            NOT NULL REFERENCES InteriorDesigns(Id) ON DELETE CASCADE,
  Name             NVARCHAR(150)  NOT NULL,
  Origin           NVARCHAR(100)  NULL,
  Finish           NVARCHAR(150)  NULL,
  Care             NVARCHAR(300)  NULL
);

CREATE TABLE InteriorDesignPackages (
  Id               INT IDENTITY(1,1) PRIMARY KEY,
  InteriorDesignId INT            NOT NULL REFERENCES InteriorDesigns(Id) ON DELETE CASCADE,
  Name             NVARCHAR(150)  NOT NULL,
  Price            DECIMAL(18,2)  NOT NULL,
  Includes         NVARCHAR(500)  NULL
);

CREATE TABLE InteriorDesignProducts (
  InteriorDesignId INT NOT NULL REFERENCES InteriorDesigns(Id) ON DELETE CASCADE,
  ProductId        INT NOT NULL REFERENCES Products(Id),
  PRIMARY KEY (InteriorDesignId, ProductId)
);

CREATE TABLE DesignRequests (
  Id               INT IDENTITY(1,1) PRIMARY KEY,
  CustomerId       INT            NOT NULL REFERENCES Users(Id),
  InteriorDesignId INT            NULL REFERENCES InteriorDesigns(Id),
  Title            NVARCHAR(200)  NOT NULL,
  Style            NVARCHAR(100)  NULL,
  Budget           DECIMAL(18,2)  NULL,
  Status           NVARCHAR(30)   NOT NULL DEFAULT N'New', -- New/InReview/Quoted/Done
  Notes            NVARCHAR(MAX)  NULL,
  AssignedToId     INT            NULL REFERENCES Users(Id),
  CreatedAt        DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
  UpdatedAt        DATETIME2      NULL
);

CREATE TABLE DesignRequestProducts (
  DesignRequestId INT NOT NULL REFERENCES DesignRequests(Id) ON DELETE CASCADE,
  ProductId       INT NOT NULL REFERENCES Products(Id),
  PRIMARY KEY (DesignRequestId, ProductId)
);

CREATE TABLE DesignRequestAttachments (
  Id              INT IDENTITY(1,1) PRIMARY KEY,
  DesignRequestId INT           NOT NULL REFERENCES DesignRequests(Id) ON DELETE CASCADE,
  Url             NVARCHAR(500) NOT NULL
);

/* ===== CHAT ===== */
CREATE TABLE ChatThreads (
  Id         INT IDENTITY(1,1) PRIMARY KEY,
  CustomerId INT       NOT NULL UNIQUE REFERENCES Users(Id),
  CreatedAt  DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE ChatMessages (
  Id         INT IDENTITY(1,1) PRIMARY KEY,
  ThreadId   INT           NOT NULL REFERENCES ChatThreads(Id) ON DELETE CASCADE,
  SenderId   INT           NOT NULL REFERENCES Users(Id),
  SenderRole NVARCHAR(30)  NOT NULL, -- Customer / Sales
  Content    NVARCHAR(MAX) NOT NULL,
  CreatedAt  DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME()
);

/* ===== PRODUCTION / DELIVERY (optional module) ===== */
CREATE TABLE ProductionOrders (
  Id              INT IDENTITY(1,1) PRIMARY KEY,
  OrderId         INT          NOT NULL REFERENCES Orders(Id),
  Status          NVARCHAR(30) NOT NULL, -- Queued/InProgress/Done/Blocked
  ProgressStatus  NVARCHAR(30) NULL,    -- PENDING/PREPARING/PACKING/SHIPPING/DELIVERED
  ProgressPercent INT          NOT NULL DEFAULT 0 CHECK (ProgressPercent BETWEEN 0 AND 100),
  AssignedToId    INT          NULL REFERENCES Users(Id),
  Deadline        DATE         NULL,
  CreatedAt       DATETIME2    NOT NULL DEFAULT SYSUTCDATETIME(),
  UpdatedAt       DATETIME2    NULL
);

CREATE TABLE Deliveries (
  Id             INT IDENTITY(1,1) PRIMARY KEY,
  OrderId        INT           NOT NULL UNIQUE REFERENCES Orders(Id),
  DeliveryStatus NVARCHAR(30)  NOT NULL, -- Preparing/OutForDelivery/Delivered/Failed
  Note           NVARCHAR(500) NULL,
  DeliveredAt    DATETIME2     NULL,
  UpdatedAt      DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME()
);

/* ===== CMS / LOGS ===== */
CREATE TABLE Contents (
  Id          INT IDENTITY(1,1) PRIMARY KEY,
  Title       NVARCHAR(250) NOT NULL,
  Slug        NVARCHAR(250) NOT NULL UNIQUE,
  Type        NVARCHAR(50)  NOT NULL, -- Blog, Guide, News
  Body        NVARCHAR(MAX) NULL,
  CoverUrl    NVARCHAR(500) NULL,
  IsPublished BIT           NOT NULL DEFAULT 0,
  PublishedAt DATETIME2     NULL,
  CreatedAt   DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
  UpdatedAt   DATETIME2     NULL
);

CREATE TABLE SystemLogs (
  Id          BIGINT IDENTITY(1,1) PRIMARY KEY,
  ActorUserId INT            NULL REFERENCES Users(Id),
  Action      NVARCHAR(100)  NOT NULL,
  Entity      NVARCHAR(100)  NULL,
  EntityId    NVARCHAR(50)   NULL,
  Detail      NVARCHAR(MAX)  NULL,
  CreatedAt   DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME()
);

/* ===== SEED ROLES ===== */
INSERT INTO Roles (Name, Description) VALUES
 (N'Customer', N'Khách hàng'),
 (N'Sales',    N'Nhân viên kinh doanh'),
 (N'Manager',  N'Quản lý catalog / doanh thu'),
 (N'Admin',    N'Quản trị hệ thống');

/* ===== INDEXES ===== */
CREATE INDEX IX_Users_RoleId ON Users(RoleId);
CREATE INDEX IX_Products_Category ON Products(CategoryId);
CREATE INDEX IX_Products_IsActive ON Products(IsActive) WHERE IsDeleted = 0;
CREATE INDEX IX_Orders_Customer ON Orders(CustomerId);
CREATE INDEX IX_Orders_Status ON Orders(Status);
CREATE INDEX IX_OrderItems_Order ON OrderItems(OrderId);
CREATE INDEX IX_QuotationRequests_Customer ON QuotationRequests(CustomerId);
CREATE INDEX IX_Quotations_Status ON Quotations(Status);
CREATE INDEX IX_DesignRequests_Status ON DesignRequests(Status);
CREATE INDEX IX_ChatMessages_Thread ON ChatMessages(ThreadId);
CREATE INDEX IX_SystemLogs_CreatedAt ON SystemLogs(CreatedAt DESC);
CREATE INDEX IX_InteriorDesigns_Category ON InteriorDesigns(Category);
```

---

## E. Checklist map FE field → cột DB

| FE (mock) | DB |
|-----------|-----|
| `product.marketPrice` | `Products.MarketPrice` |
| `product.specs.*` | `ProductSpecs` |
| `user.isLocked` / `status=Locked` | `Users.IsLocked` |
| `interiorDesign.gallery[]` | `InteriorDesignImages` |
| `interiorDesign.highlights[]` | `InteriorDesignHighlights` |
| `interiorDesign.specs[]` | `InteriorDesignSpecs` |
| `interiorDesign.materials[]` | `InteriorDesignMaterials` |
| `interiorDesign.packages[]` | `InteriorDesignPackages` |
| `interiorDesign.priceCompare.studio/marketAvg` | `StudioPrice` / `MarketAvgPrice` |
| `relatedProductIds` / `productIds` | bảng junction `*Products` |
| `order.items[].price` | `OrderItems.UnitPrice` |
| Chat `senderRole` | `ChatMessages.SenderRole` |

---

## F. Thứ tự tạo DB gợi ý

1. Roles → Users → tokens  
2. Categories → Products → ProductSpecs / ProductImages  
3. Carts → CartItems  
4. Orders → OrderItems → Reviews  
5. InteriorDesigns + child tables + InteriorDesignProducts  
6. Quotation* / DesignRequest*  
7. Chat*  
8. ProductionOrders / Deliveries (nếu cần)  
9. Contents / SystemLogs  

File này: `docs/MSSQL_SCHEMA.md`. Có thể copy block SQL sang SSMS / Azure Data Studio để chạy.
