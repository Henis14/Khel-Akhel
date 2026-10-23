# Khel-Akhel — Complete Project Technical & Functional Documentation

**Document Version:** 1.2.0  
**Audit & Generation Date:** October 22, 2026  
**Status Reference:** Verified against current source code, SQL migrations, unit tests, and runtime builds  
**Frontend Repository Root:** `F:\Projects\Khel-Akhel\Khel-Akhel-Client`  
**Backend Repository Root:** `F:\Projects\Khel-Akhel\Khel-Akhel-Server`  
**Database Scripts Root:** `F:\Projects\Khel-Akhel\DBScripts`  

---

> [!NOTE]
> **Implementation Status Methodology**: All implementation statuses in this document are strictly classified based on deep source code inspection, build verifications (`ng build` and `dotnet test`), and route/controller audits. Differentiations are explicitly maintained between active UI components, controllers that expose validated endpoints, fully integrated client-to-server workflows, and database tables.

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
   - 1.1 [What is Khel-Akhel? (Brand Identity, Vision & Purpose)](#11-what-is-khel-akhel-brand-identity-vision--purpose)
   - 1.2 [The Five Pillars of the Website](#12-the-five-pillars-of-the-website)
   - 1.3 [System Architecture & Technology Summary](#13-system-architecture--technology-summary)
   - 1.4 [Implementation Maturity & Key Milestones](#14-implementation-maturity--key-milestones)
2. [Business Requirements and Functional Scope](#2-business-requirements-and-functional-scope)
   - 2.1 [The Twelve Creative Disciplines (DARAS Association)](#21-the-twelve-creative-disciplines-daras-association)
   - 2.2 [Bespoke E-Commerce & Curated Catalog Scope](#22-bespoke-e-commerce--curated-catalog-scope)
   - 2.3 [Custom Commissioned Projects Workflow](#23-custom-commissioned-projects-workflow)
   - 2.4 [Cart and Wishlist Architecture](#24-cart-and-wishlist-architecture)
   - 2.5 [Direct Inquiries (Contact Us) & Knowledge Base (FAQs)](#25-direct-inquiries-contact-us--knowledge-base-faqs)
3. [Technology Stack and Architecture](#3-technology-stack-and-architecture)
4. [Repository and Folder Structure](#4-repository-and-folder-structure)
5. [Angular Frontend Documentation](#5-angular-frontend-documentation)
   - 5.1 [Application Setup and Configuration](#51-application-setup-and-configuration)
   - 5.2 [Admin Portal Implementation](#52-admin-portal-implementation)
   - 5.3 [Customer-Facing Storefront Implementation](#53-customer-facing-storefront-implementation)
   - 5.4 [Frontend Routing Inventory](#54-frontend-routing-inventory)
   - 5.5 [Frontend Services and State Management](#55-frontend-services-and-state-management)
   - 5.6 [UI Theme and Design System](#56-ui-theme-and-design-system)
6. [Backend Architecture (.NET 8 Web API)](#6-backend-architecture-net-8-web-api)
7. [Complete Backend Controller and API Inventory](#7-complete-backend-controller-and-api-inventory)
8. [Frontend-to-Backend API Mapping & Integration Audit](#8-frontend-to-backend-api-mapping--integration-audit)
9. [Database Documentation (SQL Server / KhelAkhe_CRM)](#9-database-documentation-sql-server--khelakhe_crm)
   - 9.1 [Database Architecture and Conventions](#91-database-architecture-and-conventions)
   - 9.2 [Complete Table Inventory](#92-complete-table-inventory)
   - 9.3 [Foreign Key Relationships](#93-foreign-key-relationships)
   - 9.4 [Indexes and Performance Constraints](#94-indexes-and-performance-constraints)
   - 9.5 [Database Sequences and Seed Data](#95-database-sequences-and-seed-data)
   - 9.6 [Entity-Relationship Diagram](#96-entity-relationship-diagram)
10. [Authentication, Authorization, and Security](#10-authentication-authorization-and-security)
11. [Commissioned Projects / Custom Project Requests Module](#11-commissioned-projects--custom-project-requests-module)
12. [Dynamic Projects & Visual Gallery Layout Builder Module](#12-dynamic-projects--visual-gallery-layout-builder-module)
13. [Production Cart and Wishlist Subsystem](#13-production-cart-and-wishlist-subsystem)
14. [Contact Us & Inquiries Subsystem](#14-contact-us--inquiries-subsystem)
15. [Dynamic FAQ Management Subsystem (Admin & Public Storefront)](#15-dynamic-faq-management-subsystem-admin--public-storefront)
16. [Current Implementation Status Matrix](#16-current-implementation-status-matrix)
17. [Known Issues, Technical Debt, and Gaps](#17-known-issues-technical-debt-and-gaps)
18. [Prioritized Completion Roadmap](#18-prioritized-completion-roadmap)
19. [Build, Run, and Local Development Guide](#19-build-run-and-local-development-guide)
20. [Testing and Verification Record](#20-testing-and-verification-record)
21. [Change History and Maintenance Protocol](#21-change-history-and-maintenance-protocol)

---

## 1. Executive Summary

### 1.1 What is Khel-Akhel? (Brand Identity, Vision & Purpose)
**Khel-Akhel** is a multi-disciplinary contemporary creative studio, design laboratory, fine art atelier, and bespoke e-commerce platform founded in creative and artistic association with **DARAS**.

#### The Philosophical Meaning & Origin
In language and spirit, *Khel-Akhel* evokes the interplay between the conscious and the intuitive, the structured and the formless, the playful and the sacred:
> *"All that comes, comes in play. All that flows, flows. All that goes, goes. All is One. Khel-Akhel is a creative journey where art takes many forms, shaped by curiosity, experimentation, and the natural flow of making."*

Rather than being a traditional transactional retail shop or an inaccessible art gallery, Khel-Akhel serves as an expansive digital portal where artistic disciplines freely converge: material experimentation, architectural installations, curated functional home objects, limited fine-art editions, and personalized commissioned creations.

### 1.2 The Five Pillars of the Website

```mermaid
graph TD
    Root["Khel-Akhel Creative Studio & Platform"]
    
    P1["1. Fine Art & Visual Portfolio<br/>Dynamic narratives across 12 disciplines<br/>7 layout presets, lightbox modal"]
    P2["2. Bespoke E-Commerce Store<br/>Curated catalog: Art prints, lamps,<br/>murals, furniture, wearables, gifting"]
    P3["3. Custom Commissions Atelier<br/>Inquiry capture: sketches, timeline,<br/>location, automated tracking codes"]
    P4["4. Direct Inquiries & Knowledge Base<br/>Contact Us API, multi-location dynamic FAQs<br/>(Home, FAQ, Checkout, Contact, Footer)"]
    P5["5. Curatorial Operations Portal<br/>Role-based Admin Dashboard, inventory,<br/>media uploaders, audit logs, order flow"]

    Root --> P1
    Root --> P2
    Root --> P3
    Root --> P4
    Root --> P5
```

1. **Fine Art & Studio Practice Portfolio (`/projects`, `/about`)**:
   - Showcases original visual art, site-specific installations, photography, sculptures, and architectural scenography.
   - Powered by an editorial narrative builder supporting 7 visual gallery layout presets (`grid-3`, `grid-2`, `editorial`, `featured`, `full-width`, `masonry`, `asymmetric`), drag-and-drop 15-image uploads, and an immersive full-screen lightbox.
   - Articulates the 12 DARAS Studio Disciplines and foundational philosophy.

2. **Bespoke E-Commerce Storefront (`/products`, `/shop`)**:
   - Offers handcrafted, non-standard creative goods: numbered art prints, original photography, sculptural lamps & luminaires, wall murals, reclaimed wood/metal sculptures, bespoke studio furniture, limited studio apparel, artisan bags, and curated corporate/celebration gifts.
   - Employs enterprise pricing, dimensional specifications, material details, and real-time stock availability.

3. **Custom Commissioned Projects Atelier (`/commissioned-projects`)**:
   - Provides a client engagement pipeline for patrons, architects, and corporate clients seeking custom installations, bespoke sculptures, or site-specific art.
   - Captures client briefs, timelines, budgets, project locations, and up to 3 reference images/sketches with automated sequence tracking (`COMM-yyyyMMdd-XXXX`).

4. **Direct Inquiries & Storefront Knowledge Base (`/contact`, `/faq`)**:
   - **Contact Us**: Direct inquiry endpoint capturing potential client inquiries with rate-limiting, strict validation, and audit tracking.
   - **Dynamic FAQs**: Public knowledge base dynamically rendered across designated storefront locations (FAQ page accordion, Home page, Checkout page, Contact page, Footer).

5. **Curatorial Operations Portal (`/admin`)**:
   - An enterprise management dashboard allowing studio curators and administrators to oversee registered customers, delivery addresses, product categories, stock inventory, commissioned project inquiries, project galleries, and FAQ knowledge bases.

### 1.3 System Architecture & Technology Summary
- **Frontend**: Angular 21.2.0 (Standalone Components, Signals, Zoneless Change Detection, Reactive Forms, SCSS Design System with CSS Custom Properties).
- **Backend**: ASP.NET Core 8.0 Web API (`net8.0`, C# 12, Scoped ADO.NET `DbHelper`, AES-256-GCM URL ID encryption, HMAC-SHA256 signatures, Partitioned Rate Limiting per Client IP, BCrypt cost 12 hashing, SkiaSharp visual CAPTCHA).
- **Database**: Microsoft SQL Server (`KhelAkhe_CRM`, 24 master tables, 3 database sequences, 21 foreign keys, 29 indexes, parameterized queries).
- **Authentication**: Stateless Bearer JWT access tokens paired with rotating `HttpOnly` refresh token cookies (`drs_refresh`), single-transaction atomic rotation, and breach reuse containment.
- **Test Suite**: 115 passing automated tests in `Khel-Akhel-Server.Tests` validating Cart, Wishlist, Contacts, Auth, Rate Limiting, CAPTCHA, Secrets, Password Recovery, and Public Forms.

### 1.4 Implementation Maturity & Key Milestones
- **Admin Portal**: Fully mature (~95% complete). Comprehensive management for Customers, Customer Addresses, Product Categories, Product Catalog, Product Images, Stock Inventory, Commission Inquiries, Dynamic Studio Projects, and FAQ Knowledge Bases.
- **Backend API**: Comprehensive and production-grade (~90% complete). Full controller coverage across Auth, Customers, Addresses, Locations, Categories, Products, Images, Stock, Commissions, Projects, FAQs, Cart, Wishlist, and Contact Us.
- **Storefront Architecture**: Active public routes for Home (`/`), Projects Showcase (`/projects`), About & Disciplines (`/about`), Products Catalog (`/products`), Dynamic FAQs (`/faq`), Login (`/login`), and Register (`/register`). Customer-side page stubs have been cleanly reset to eliminate dead boilerplate.
- **E-Commerce Subsystems**: Production Cart (atomic stock checks, concurrent race protection) and Wishlist (authenticated + guest token support, login merge, move-to-cart) backends are fully implemented and verified.

---

## 2. Business Requirements and Functional Scope

### 2.1 The Twelve Creative Disciplines (DARAS Association)
As established in studio foundation documentation (`About Khel-Akhel _Draft 01.docx`), Khel-Akhel operates across twelve distinct disciplines that inform its catalog and bespoke requests:
1. **01 — Art & Creative Practice**: Visual art, conceptual art, illustration, calligraphy, sculpture, performance, photography, film, sound, poetry.
2. **02 — Architecture, Spatial & Experiential Design**: Architectural design, interiors, spatial scenography, exhibitions, immersive environments, installations, public spaces.
3. **03 — Craft, Material Culture, Objects & Product Design**: Artisan collaborations, bespoke studio furniture, collectibles, lifestyle objects, material experimentation.
4. **04 — Cultural, Festival & Public Experiences**: Festivals, public exhibitions, cultural programming, immersive public art.
5. **05 — Film, Photography, Sound & Media**: Documentaries, photographic portfolios, audiovisual installations, soundscapes, digital media.
6. **06 — Learning, Play & Facilitation**: Interactive workshops, alternative learning circles, immersive play labs, educational curricula.
7. **07 — Research, Documentation & Knowledge**: Ecological research, cultural preservation archives, art/craft publications, studio methodologies.
8. **08 — Community, Ecology & Cultural Initiatives**: Grassroots community initiatives, participatory regenerative art, ecological design.
9. **09 — Creative Consultancy & Direction**: Creative direction, art curation, strategic brand consultancy, experience design.
10. **10 — Publishing & Content**: Curated books, editorial publications, digital learning content, documentary films.
11. **11 — Commercial Products & Licensing**: Direct commercialization, manufacture, retail distribution, IP licensing.
12. **12 — Intellectual Property & Collaborations**: Copyrights, registered trademarks, collaborative commissions, partnerships, joint ventures.

### 2.2 Bespoke E-Commerce & Curated Catalog Scope
The product catalog is structured to handle diverse, non-standard creative goods:
- **Art Prints & Postcards**: Limited edition numbered prints, standard edition prints, fine-art postcards, framing options.
- **Photographs**: Original photographic prints with custom framing and dimension specifications.
- **Lamps & Sculptural Luminaires**: Handcrafted table lamps, floor lamps, sculptural lighting objects.
- **Murals & Wall Art**: Studio wall murals, textured decorative wall art, acoustic artistic paneling.
- **Sculptures**: 3D artistic works in metal, stone, reclaimed wood, ceramics, and mixed media.
- **Bespoke Furniture**: Custom studio chairs, side tables, accent consoles, artistic statement furniture.
- **Wearables & Clothing**: Limited-run studio apparel, artistic scarves, bespoke textiles.
- **Bags & Accessories**: Handcrafted leather/canvas totes, studio carriers, lifestyle cases.
- **Gifting & Corporate Collections**: Curated gifting packages, corporate event bespoke packages (modeled on premium lifestyle gifting workflows).

### 2.3 Custom Commissioned Projects Scope
For clients requesting works that cannot be purchased off-the-shelf, the application provides an end-to-end inquiry workflow:
- Detailed inquiry capture (Timeline, Location, Word-limited Description, Keywords).
- Multi-file image sketch/reference uploads (maximum 3 images, 5MB each).
- Auto-generated business tracking sequence (`COMM-yyyyMMdd-XXXX`).
- Administrator review, status progression (`Submitted` $\rightarrow$ `Under Review` $\rightarrow$ `Contacted` $\rightarrow$ `In Progress` $\rightarrow$ `Completed` / `Rejected` / `Cancelled`), and internal notes logging.

### 2.4 Cart and Wishlist Architecture
- **Cart Management**: Authenticated user shopping cart with atomic upserts, stock reservation checks, subtotal calculations, and concurrent conflict prevention.
- **Wishlist Management**: Dual-mode support for registered patrons and guest anonymous visitors. Supports guest tokens with cryptographic hashing, login migration (merging guest wishlist items into user accounts), and single-click move-to-cart operations with live inventory validation.

### 2.5 Direct Inquiries (Contact Us) & Knowledge Base (FAQs)
- **Contact Us**: Clean, high-throughput inquiry pipeline capturing prospective client inquiries (Full Name, Mobile Number, Email, Company Name, Service Description). Adheres to project-standard controller patterns without unnecessary abstraction layers, and enforces rate-limiting and audit logging.
- **Storefront Knowledge Base**: Multi-channel FAQ repository with dynamic contextual placement across storefront sections (`Home`, `FAQ Page`, `Checkout`, `Contact`, `Footer`). Full administrative lifecycle: create, update, detail view, quick status/publish toggles, reordering, and soft-deletion.

---

## 3. Technology Stack and Architecture

### 3.1 Technology Matrix

| Layer | Technology | Version | Purpose / Detail |
|---|---|---|---|
| **Frontend Framework** | Angular | `21.2.0` | Modern standalone component architecture without NgModules |
| **Frontend Build Tool** | `@angular/build` | `21.2.9` | Vite + esbuild modern application pipeline |
| **Language (Client)** | TypeScript | `~5.9.2` | Strict typing, ECMAScript 2022 output |
| **Styling** | SCSS | Dart Sass | CSS Custom Properties design system with Warm Editorial palette |
| **Client Test Runner** | Vitest | `^4.0.8` | Next-generation unit testing framework (configured with `jsdom 28.0.0`) |
| **Backend Framework** | ASP.NET Core | `8.0` (`net8.0`) | High-performance C# Web API |
| **Data Access** | ADO.NET / `Microsoft.Data.SqlClient` | `5.2.2` | Custom `DbHelper` with query safety validation and parameterized execution |
| **Authentication** | `Microsoft.AspNetCore.Authentication.JwtBearer` | `8.0.8` | Stateless JWT Bearer tokens |
| **Password Hashing** | `BCrypt.Net-Next` | `4.0.3` | Salted Blowfish password hashing |
| **API Documentation** | Swashbuckle (Swagger) | `6.6.2` | Interactive OpenAPI 3.0 UI |
| **Database Server** | Microsoft SQL Server | 2019 / 2022 / LocalDB | Relational storage (`KhelAkhe_CRM`) |

### 3.2 High-Level Architecture Diagram

```mermaid
graph TD
    subgraph ClientLayer ["Client Layer (Angular 21)"]
        UI_Storefront["Customer Storefront<br/>(UserLayoutComponent)"]
        UI_Admin["Admin Operations Portal<br/>(AdminLayoutComponent)"]
        HTTP_Interceptor["Auth HTTP Interceptor<br/>(Bearer Token & Cookie Refresh)"]
        UI_Storefront --> HTTP_Interceptor
        UI_Admin --> HTTP_Interceptor
    end

    subgraph ApiGateway ["Backend Gateway (ASP.NET Core 8 Web API)"]
        CORS["CORS Policy<br/>(AngularPolicy withCredentials:true)"]
        RateLimiter["Rate Limiter<br/>(Auth: 15/min, General: 100/min)"]
        JWT_Auth["JWT & Cookie Authentication<br/>(Bearer Access Token + HttpOnly Refresh Cookie)"]
        Controllers["API Controllers<br/>(Auth, Customers, Addresses, Locations, Products, Stock, Commissions, Projects, FAQs, Cart, Wishlist, ContactUs)"]
        
        HTTP_Interceptor --> CORS
        CORS --> RateLimiter
        RateLimiter --> JWT_Auth
        JWT_Auth --> Controllers
    end

    subgraph ServiceLayer ["Business & Security Layer"]
        DbHelper["DbHelper (ADO.NET)<br/>(Query Safety Validator, Parameter Cloning)"]
        CryptoService["UrlEncryptionService<br/>(AES-GCM-256 + Base64Url)"]
        AuditService["AuditService<br/>(Action Logger with Password Redaction)"]
        FileValidator["FileValidationHelper<br/>(Magic Byte Binary Validation)"]
        CaptchaService["CaptchaService<br/>(IMemoryCache, 5-char code, 5m TTL)"]
        CartService["CartService<br/>(Concurrency Control & Atomic Stock Validation)"]
        WishlistService["WishlistService<br/>(Guest Token Hashing & Login Merge)"]
        
        Controllers --> DbHelper
        Controllers --> CryptoService
        Controllers --> AuditService
        Controllers --> FileValidator
        Controllers --> CaptchaService
        Controllers --> CartService
        Controllers --> WishlistService
        CartService --> DbHelper
        WishlistService --> DbHelper
    end

    subgraph StorageLayer ["Persistence & File Storage"]
        SQL_DB[("SQL Server: KhelAkhe_CRM<br/>23 Tables, 3 Sequences, 26 Indexes")]
        Disk_Uploads["Physical Disk Storage<br/>wwwroot/uploads/(products, commissions, projects, about)"]
        
        DbHelper --> SQL_DB
        Controllers --> Disk_Uploads
    end
```

---

## 4. Repository and Folder Structure

### 4.1 Root Structure
```text
F:\Projects\Khel-Akhel
├── .git/                                   # Git repository tracking
├── .github/                                # GitHub automation & issue templates
├── About Khel-Akhel _Draft 01.docx         # Brand foundation & 12 disciplines reference
├── DBScripts/                              # Database initialization & migration scripts
├── Khel - akhel documentation.txt          # Original notes, catalog categories, wireframe requirements
├── Khel-Akhel-Client/                      # Angular 21 frontend project
├── Khel-Akhel-Server/                      # ASP.NET Core 8 Web API project
└── KHEL-AKHEL-PROJECT-DOCUMENTATION.md      # Master technical documentation
```

### 4.2 Frontend Directory Tree (`Khel-Akhel-Client`)
```text
Khel-Akhel-Client/src/
├── app/
│   ├── admin/                              # Admin Operations Portal
│   │   ├── layouts/
│   │   │   └── admin-layout/               # Admin shell with collapsible sidebar & topbar
│   │   ├── models/                         # DTO interfaces for Admin domain
│   │   │   ├── project.model.ts              # Project content, image, layout presets, and DTOs
│   │   │   ├── commissioned-project.model.ts
│   │   │   ├── customer.model.ts
│   │   │   ├── customer-address.model.ts
│   │   │   ├── faq.model.ts                # FaqCreateRequest, FaqUpdateRequest, FaqResponse, FaqAdminFilter
│   │   │   ├── product.model.ts
│   │   │   ├── product-category.model.ts
│   │   │   ├── product-image.model.ts
│   │   │   └── product-stock.model.ts
│   │   ├── pages/                          # Admin feature components
│   │   │   ├── about/                      # Studio narrative & gallery layout management
│   │   │   ├── commissioned-projects/      # Inquiries list, filter, preview, status modal
│   │   │   ├── customer-addresses/         # Customer delivery address manager
│   │   │   ├── customers/                  # Customers list, add-customer, customer-detail
│   │   │   ├── dashboard/                  # KPI overview aggregated from services
│   │   │   ├── faqs/                       # FAQ management (faqs.ts, faqs.html, faqs.scss): Search debounce,
│   │   │   │                               # category & status filters, table, modal, inline toggles, pagination
│   │   │   ├── login/                      # Admin credentials & captcha login
│   │   │   ├── product-categories/         # Category list & add-category
│   │   │   ├── product-stock/              # Stock list & update-stock
│   │   │   ├── products/                   # Product catalog list, add-product, product-detail
│   │   │   └── projects/                   # Dynamic project showcases & 7 visual gallery layouts
│   │   └── services/                       # Admin HTTP service layer (admin-faq.service.ts, admin-project.service.ts, etc.)
│   ├── common/                             # Shared enterprise infrastructure
│   │   ├── components/                     # Reusable UI elements (app-loader, breadcrumb, confirmation-dialog, pagination)
│   │   ├── constants/                      # Regex patterns (Email, Mobile, Pincode)
│   │   ├── guards/                         # adminGuard, authGuard, userGuard
│   │   ├── interceptors/                   # authInterceptor (JWT header & HttpOnly refresh)
│   │   ├── models/                         # ApiResponse, Auth, Breadcrumb, Location models
│   │   ├── pipes/                          # safe-html pipe
│   │   ├── services/                       # ApiService, AuthService, LocationService, UrlEncryptionService, UrlSignatureService
│   │   └── validators/                     # Form group & field custom validators (CustomValidators)
│   ├── user/                               # Customer Storefront Portal
│   │   ├── layouts/
│   │   │   └── user-layout/                # Storefront header, mobile drawer, footer
│   │   ├── models/                         # User domain models (user-product.model.ts)
│   │   ├── pages/                          # Customer pages
│   │   │   ├── about/                      # Implemented studio philosophy, manifesto, 12 DARAS Disciplines
│   │   │   ├── faq/                        # Implemented dynamic FAQ accordion with keyword search & category chips
│   │   │   ├── home/                       # Implemented editorial home landing page
│   │   │   ├── login/                      # Implemented customer login page
│   │   │   ├── products/                   # Implemented customer product gallery
│   │   │   ├── projects/                   # Implemented dynamic project showcases with 7 gallery layouts & lightbox
│   │   │   ├── register/                   # Implemented customer registration form
│   │   │   └── [account, cart, checkout, contact, policies, press, shop, wishlist] # Cleaned customer-side page shells
│   │   └── services/                       # user-product.service, user-project.service
│   ├── app.config.ts                       # Application providers (Router, Interceptors)
│   ├── app.routes.ts                       # Angular route configuration (/admin/faqs, /faq, etc.)
│   └── app.ts                              # Root component (<router-outlet />)
├── environments/                           # environment.ts & environment.prod.ts
└── styles/                                 # Theme SCSS files (admin-theme.scss, user-theme.scss)
```

### 4.3 Backend Directory Tree (`Khel-Akhel-Server`)
```text
Khel-Akhel-Server/
├── BL/                                     # Business Logic & Infrastructure
│   ├── Common/
│   │   ├── Audit.cs                        # Static audit caller
│   │   ├── DataTableMapper.cs              # Reflection/DataRow mapping helper
│   │   ├── DateTimeFormat.cs               # Standardized ISO date-time string formatter
│   │   ├── DBHelper.cs                     # ADO.NET SQL Server manager with safety check
│   │   ├── FileValidationHelper.cs         # Magic-byte binary image validator
│   │   ├── JwtHelper.cs                    # HMAC-SHA256 JWT builder
│   │   ├── Logs.cs                         # Daily rolling file logger & log retention
│   │   ├── PasswordHelper.cs               # BCrypt password hashing & verifier
│   │   └── ValidationHelper.cs             # Email, Mobile, Pincode regex validators
│   └── Services/
│       ├── AuditService.cs                 # drs_audit_mst writer with password sanitization
│       ├── CaptchaService.cs               # In-memory captcha code generator & validator
│       ├── CartService.cs                  # Cart business logic with concurrent stock checks & line totals
│       ├── RefreshTokenService.cs          # drs_refresh_token_mst token rotation manager
│       ├── UrlEncryptionService.cs         # AES-GCM-256 authenticated ID encryptor
│       ├── UrlSignatureService.cs          # HMAC-SHA256 URL integrity signer
│       └── WishlistService.cs              # Wishlist business logic, guest token hashing & login merge
├── Controllers/
│   ├── Auth/AuthController.cs              # Login, Captcha, Refresh, Logout, Password APIs
│   ├── Cart/CartController.cs              # Customer cart CRUD with atomic upsert & stock checks
│   ├── commissionedProjects/
│   │   └── commissionsController.cs        # Bespoke project inquiries & review endpoints
│   ├── ContactUs/ContactUsController.cs    # Contact inquiry capture API (direct controller pattern)
│   ├── Customer/
│   │   ├── CustomerAddressController.cs    # Customer address management
│   │   └── CustomerController.cs           # Customer profile management
│   ├── FAQs/FAQController.cs               # Studio FAQ endpoints (Public & Admin management)
│   ├── LocationMaster/LocationMaster.cs    # Country and State master endpoints
│   ├── Product/
│   │   ├── ProductCategoryController.cs    # Category management endpoints
│   │   ├── ProductController.cs            # Product catalog & public search endpoints
│   │   ├── ProductImageController.cs       # Product gallery image upload endpoints
│   │   └── ProductStockController.cs       # Inventory & stock adjustment endpoints
│   ├── Projects/ProjectsController.cs      # Dynamic project showcase & portfolio endpoints
│   └── Wishlist/WishlistController.cs      # Dual-mode (customer & guest) wishlist endpoints
├── Models/                                 # Request models, DTOs, and CommonResponse
│   ├── AuthModels.cs
│   ├── CartModels.cs                       # CartItemAddRequest, CartItemUpdateRequest, CartResponse
│   ├── CommissionModels.cs
│   ├── CommonResponse.cs
│   ├── ContactUsModels.cs                  # ContactInquiryRequest, ContactInquiryResponse
│   ├── CustomerAddressModels.cs
│   ├── CustomerModels.cs
│   ├── FaqModels.cs                        # FaqCreateRequest, FaqUpdateRequest, FaqResponse
│   ├── LocationModels.cs
│   ├── ProductCategoryModels.cs
│   ├── ProductImageModels.cs
│   ├── ProductModels.cs
│   ├── ProductStockModels.cs
│   ├── ProjectModels.cs
│   └── WishlistModels.cs                   # WishlistItemAddRequest, WishlistResponse, MoveToCartRequest
├── wwwroot/
│   └── uploads/                            # Uploaded image files (products, commissions, projects)
├── appsettings.json                        # Connection string, JWT parameters, CORS origins
├── Khel-Akhel-Server.csproj                # .NET 8 project configuration
└── Program.cs                              # Application startup, DI container, middleware pipeline
```

### 4.4 Database Scripts Directory (`DBScripts`)
```text
DBScripts/
├── ProductScript-CreateTAbles.sql          # Core table definitions, 3 Sequences, Alter scripts
├── ProductScript-Foreignkeys.sql           # Referential integrity constraints
├── ProductScript-Indexes.sql               # Filtered unique & performance indexes
├── ProductScript-SeedData.sql              # Master seeds (Countries, States, Categories, Messages)
├── Migration_Cart_And_Wishlist.sql         # Cart & Wishlist tables, constraints, indexes & error seeds
└── Migration_ContactUs.sql                 # Contact Inquiry table, constraints, indexes & error seeds
```

---

## 5. Angular Frontend Documentation

### 5.1 Application Setup and Configuration
- **Application Bootstrap**: Standalone application configuration initialized in `main.ts` using `bootstrapApplication(App, appConfig)`.
- **Global Providers (`app.config.ts`)**:
  - `provideZoneChangeDetection({ eventCoalescing: true })`: Improves change detection performance by batching browser events.
  - `provideBrowserGlobalErrorListeners()`: Traps unhandled script errors.
  - `provideRouter(routes)`: Configures client-side routing.
  - `provideHttpClient(withInterceptors([authInterceptor]))`: Injects HTTP client with global JWT authorization headers and refresh-token handling.
- **Environment**: Configured in `src/environments/environment.ts` with `apiBaseUrl: 'https://localhost:7095/api'`.

### 5.2 Admin Portal Implementation

The Admin Portal is protected by `adminGuard` and rendered inside `AdminLayoutComponent`.

1. **Admin Login (`/admin/login`)**:
   - **Path**: `src/app/admin/pages/login/login.ts`
   - **Responsibilities**: Authenticates administrators using Email, Password, and a dynamic Captcha. Displays animated SVG captcha with instant reload capability.
   - **Service Integration**: Calls `AuthService.getCaptcha()` and `AuthService.login()`.
   - **Status**: Implemented and verified.

2. **Admin Dashboard (`/admin/dashboard`)**:
   - **Path**: `src/app/admin/pages/dashboard/dashboard.ts`
   - **Responsibilities**: High-level administrative pulse showing KPI counters (Total Customers, Categories, Products, Stock Records, Commission Requests) and a list of recently added products.
   - **Service Integration**: Executes a client-side `forkJoin` across `AdminCustomerService`, `AdminProductCategoryService`, `AdminProductService`, `AdminProductStockService`, and `AdminCommissionedProjectService`.
   - **Status**: Implemented and verified.

3. **Customers Management (`/admin/customers`)**:
   - **Path**: `src/app/admin/pages/customers/customers.ts`
   - **Responsibilities**: Searchable, paginated customer table. Features debounced live search, active/inactive filters, customer detail navigation, and soft-delete prompts.
   - **Add Customer (`/admin/customers/add`)**: Reactive form validating First Name, Last Name, Email, Mobile, and Password with confirmation dialog.
   - **Customer Detail (`/admin/customers/:encryptedId`)**: Displays comprehensive customer account information, verification status, and linked addresses.
   - **Status**: Implemented and verified.

4. **Customer Addresses (`/admin/customer-addresses`)**:
   - **Path**: `src/app/admin/pages/customer-addresses/customer-addresses.ts`
   - **Responsibilities**: Address inventory table displaying Full Name, Mobile, Street Address, City, State, Country, Pincode, and Default address status.
   - **Status**: Implemented and verified.

5. **Product Categories (`/admin/product-categories`)**:
   - **Path**: `src/app/admin/pages/product-categories/product-categories.ts`
   - **Responsibilities**: Categorization management displaying Category Name, Description, Display Order, and Active badge.
   - **Add Category (`/admin/product-categories/add`)**: Reactive form to register new categories.
   - **Status**: Implemented and verified.

6. **Products Management (`/admin/products`)**:
   - **Path**: `src/app/admin/pages/products/products.ts`
   - **Responsibilities**: Product catalog table with SKU, Product Code, Selling Price, MRP, Category badge, and status flags.
   - **Add Product (`/admin/products/add`)**: Multi-section product creation form with automated SKU generation, price validation, category dropdown, and image upload.
   - **Product Detail (`/admin/products/:encryptedId`)**: Full product view with gallery image uploader, default image selector, and image deletion.
   - **Status**: Implemented and verified.

7. **Product Stock (`/admin/product-stock`)**:
   - **Path**: `src/app/admin/pages/product-stock/product-stock.ts`
   - **Responsibilities**: Inventory monitoring grid highlighting low-stock items ($\le 5$ units), available quantity, and reserved quantity.
   - **Update Stock (`/admin/product-stock/update`)**: Form to adjust available and reserved stock by selecting an existing product.
   - **Status**: Implemented and verified.

8. **Commissioned Projects (`/admin/commissioned-projects`)**:
   - **Path**: `src/app/admin/pages/commissioned-projects/commissioned-projects.ts`
   - **Responsibilities**: Full-featured bespoke project inquiry portal. Includes debounced search, category filter, status filter, date range filter, sort order selector, inquiry detail modal, full-screen image lightbox, and status/notes update dialog.
   - **Status**: Implemented and verified.

9. **Projects Management (`/admin/projects`)**:
   - **Path**: `src/app/admin/pages/about/about-list/about-list.ts`, `src/app/admin/pages/about/about-form/about-form.ts`
   - **Responsibilities**: Full studio narrative and visual portfolio layout manager:
     - **List View (`/admin/about`)**: Searchable list with discipline/content-type filter, publication status toggle (`PATCH api/about/{id}/publish`), display order visualization, quick visual preview modal, and delete action.
     - **Create/Edit Form (`/admin/projects/create`, `/admin/about/edit/:encryptedAboutId`)**: Tabbed interface featuring reactive metadata inputs (Heading, Subheading, Content Discipline / Category as a clean manual text input with no dropdown, Year, Descriptions, Credits, Location, External Link), layout selection, gallery editor, and live preview.
     - **Layout Selector (`gallery-layout-selector`)**: Visual wireframe selector supporting all 7 layout presets (`grid-3`, `grid-2`, `editorial`, `featured`, `full-width`, `masonry`, `asymmetric`).
     - **Image Gallery Editor (`image-gallery-editor`)**: Drag-and-drop file uploader (up to 15 images, 5MB each, binary image validation), visual 15-image counter badge, drag-and-drop reordering, move up/down controls, and deletion tracking.
     - **Live Preview Tab**: Instant client-side preview rendering the exact public layout, narrative text, metadata badges, and lightbox modal before publishing.
   - **Service Integration**: `AdminAboutService` (multipart `FormData` uploads, reordering, publishing, deletions).
   - **Status**: Implemented and verified.

10. **FAQs Management (`/admin/faqs`)**:
    - **Path**: `src/app/admin/pages/faqs/faqs.ts`, `faqs.html`, `faqs.scss`
    - **Responsibilities**: Comprehensive enterprise FAQ knowledge base management:
      - **Toolbar & Filtering**: Debounced keyword search (350ms, queries question, answer, category), dynamic Category dropdown extracted from records, Active status selector (All, Active, Inactive), Published status selector (All, Published, Draft), and Storefront Placement filter (`Home`, `FAQ Page`, `Checkout`, `Contact`, `Footer`).
      - **Data Table**: Display order badge, question with truncated answer preview, category badge, storefront placement pill indicators, inline quick-toggles for Active and Published states with instant database audit updates, created date, and action buttons.
      - **Modal Form**: Reactive form supporting Question (3–500 chars), Answer (required), Category (max 100 chars with datalist suggestions), Display Order (non-negative integer), Active & Published checkboxes, and multi-location placement toggles.
      - **Detail Modal**: Full-screen modal viewing complete question, answer, audit dates, and placement indicators.
      - **Deletion**: Integrated with `ConfirmationDialogComponent` for safe soft-deletion (`DELETE api/faq/delete/{id}`).
      - **Pagination**: Client-side slicing paired with `PaginationComponent`.
    - **Service Integration**: `AdminFaqService` (`src/app/admin/services/admin-faq.service.ts`).
    - **Status**: Implemented and verified.

### 5.3 Customer-Facing Storefront Implementation

The Storefront is hosted under `UserLayoutComponent` with an editorial layout celebrating authentic craft and design associated with DARAS.

1. **Home Page (`/`)**:
   - **Path**: `src/app/user/pages/home/home.ts`
   - **Template**: `home.html` (36 lines)
   - **Content**: Hero banner introducing the Khel-Akhel studio journey, signature collection previews, and CTA links.
   - **Status**: Implemented and verified.

2. **About & Studio Ethos (`/about`)**:
   - **Path**: `src/app/user/pages/about/about.ts`
   - **Template**: `about.html`, `about.scss`
   - **Content**: Refined editorial studio story and portfolio page reflecting the DARAS aesthetic:
     - Hero manifesto displaying the Khel-Akhel philosophy (*"All that comes, comes in play. All that flows, flows. All that goes, goes. All is One."*).
     - Dynamic section loop rendering published entries in `DisplayOrder`.
     - `GalleryRendererComponent` rendering media in the chosen layout preset (`grid-3`, `grid-2`, `editorial`, `featured`, `full-width`, `masonry`, `asymmetric`).
     - Full-screen lightbox modal with thumbnail strip, counter, and keyboard navigation (`Escape`, `ArrowLeft`, `ArrowRight`).
     - The Twelve DARAS Studio Disciplines interactive grid.
     - Bespoke commission inquiry CTA banner.
   - **Service Integration**: Consumes `UserAboutService.getPublicSections()`.
   - **Status**: Implemented and verified.

3. **Products Gallery (`/products`)**:
   - **Path**: `src/app/user/pages/products/products.ts`
   - **Template**: `products.html` (41 lines)
   - **Content**: Public product catalog grid displaying images, titles, pricing, and category filters. Consumes `UserProductService.getPublicProducts()`.
   - **Status**: Implemented and verified.

4. **Customer Login (`/login`)**:
   - **Path**: `src/app/user/pages/login/login.ts`
   - **Template**: `login.html` (42 lines)
   - **Content**: Secure customer authentication form.
   - **Status**: Implemented and verified.

5. **Customer Registration (`/register`)**:
   - **Path**: `src/app/user/pages/register/register.ts`
   - **Template**: `register.html` (64 lines)
   - **Content**: Registration form capturing First Name, Last Name, Email, Mobile, and Password with validation feedback.
   - **Status**: Implemented and verified.

6. **Cleaned Customer-Facing Page Shells**:
   - In accordance with the customer-side clean reset protocol, all customer-facing page components (`cart`, `wishlist`, `contact`, `account/*`, `policies/*`, `press`, `checkout`, `product-detail`) have been safely cleared of obsolete boilerplate code to provide a pristine foundation for upcoming e-commerce workflows, while preserving complete admin styling, routes, and layout integrity.
   - Active customer routes currently routed and integrated:
     - `/`: Home landing page
     - `/projects`: Studio portfolio showcases with 7 gallery layouts and lightbox
     - `/about`: Studio manifesto, philosophy, and 12 DARAS Studio Disciplines
     - `/products`: Live catalog gallery
     - `/faq`: Interactive dynamic FAQ accordion with keyword search and category chips
     - `/login`: Patron authentication
     - `/register`: New customer registration

### 5.4 Frontend Routing Inventory

| Route | Layout | Component | Access Guard | Main Purpose | Status |
|---|---|---|---|---|---|
| `/admin/login` | None (Blank) | `AdminLoginComponent` | Public | Admin portal credentials & captcha login | Implemented and verified |
| `/admin` | `AdminLayoutComponent` | *Redirect to `/admin/dashboard`* | `adminGuard` | Default admin entry point | Implemented and verified |
| `/admin/dashboard` | `AdminLayoutComponent` | `AdminDashboardComponent` | `adminGuard` | Studio operations KPI dashboard | Implemented and verified |
| `/admin/customers` | `AdminLayoutComponent` | `CustomersComponent` | `adminGuard` | Customer account directory & search | Implemented and verified |
| `/admin/customers/add` | `AdminLayoutComponent` | `AddCustomerComponent` | `adminGuard` | Manual customer creation form | Implemented and verified |
| `/admin/customers/:encryptedId` | `AdminLayoutComponent` | `CustomerDetailComponent` | `adminGuard` | Customer profile & addresses view | Implemented and verified |
| `/admin/customer-addresses` | `AdminLayoutComponent` | `CustomerAddressesComponent` | `adminGuard` | Customer delivery addresses directory | Implemented and verified |
| `/admin/product-categories` | `AdminLayoutComponent` | `ProductCategoriesComponent` | `adminGuard` | Catalog category directory | Implemented and verified |
| `/admin/product-categories/add` | `AdminLayoutComponent` | `AddCategoryComponent` | `adminGuard` | New category creation form | Implemented and verified |
| `/admin/products` | `AdminLayoutComponent` | `ProductsComponent` | `adminGuard` | Master product catalog table | Implemented and verified |
| `/admin/products/add` | `AdminLayoutComponent` | `AddProductComponent` | `adminGuard` | New product registration form | Implemented and verified |
| `/admin/products/:encryptedId` | `AdminLayoutComponent` | `ProductDetailComponent` | `adminGuard` | Product details & gallery management | Implemented and verified |
| `/admin/product-stock` | `AdminLayoutComponent` | `ProductStockComponent` | `adminGuard` | Inventory & low-stock monitoring | Implemented and verified |
| `/admin/product-stock/update` | `AdminLayoutComponent` | `UpdateStockComponent` | `adminGuard` | Inventory quantity adjustment form | Implemented and verified |
| `/admin/commissioned-projects` | `AdminLayoutComponent` | `CommissionedProjectsComponent` | `adminGuard` | Bespoke project inquiries dashboard | Implemented and verified |
| `/admin/about` | `AdminLayoutComponent` | `ProjectListComponent` | `adminGuard` | Studio narrative directory, filters & status toggle | Implemented and verified |
| `/admin/projects/create` | `AdminLayoutComponent` | `ProjectFormComponent` | `adminGuard` | Create About section with layout presets & image gallery | Implemented and verified |
| `/admin/about/edit/:encryptedAboutId` | `AdminLayoutComponent` | `ProjectFormComponent` | `adminGuard` | Edit About section, layout presets, image gallery & reordering | Implemented and verified |
| `/admin/faqs` | `AdminLayoutComponent` | `FaqsComponent` | `adminGuard` | Dynamic FAQ management (search debounce, category/status/location filters, modal, inline toggles, pagination, soft-delete) | Implemented and verified |
| `/admin/**` | `AdminLayoutComponent` | `NotFoundComponent` | `adminGuard` | Admin child wildcard 404 handler | Implemented and verified |
| `/` | `UserLayoutComponent` | `UserHomeComponent` | Public | Storefront landing page & studio ethos | Implemented and verified |
| `/about` | `UserLayoutComponent` | `About` | Public | Dynamic studio narrative, 7 visual layouts, lightbox & 12 disciplines | Implemented and verified |
| `/products` | `UserLayoutComponent` | `UserProductsComponent` | Public | Public studio product catalog gallery | Implemented and verified |
| `/login` | `UserLayoutComponent` | `UserLoginComponent` | Public | Customer account sign-in | Implemented and verified |
| `/register` | `UserLayoutComponent` | `UserRegisterComponent` | Public | New customer account registration | Implemented and verified |
| `/faq` | `UserLayoutComponent` | `Faq` | Public | Interactive customer FAQ accordion (location query, category chips, live keyword search, contact CTA) | Implemented and verified |
| `/unauthorized` | None (Blank) | `UnauthorizedComponent` | Public | 401/403 access denial error display | Implemented and verified |
| `/**` | None (Blank) | `NotFoundComponent` | Public | Global wildcard 404 handler | Implemented and verified |
| *`/shop`* | `UserLayoutComponent` | Redirect to `/products` | Public | Storefront catalog redirect | Implemented and verified |
| *`/contact`* | `UserLayoutComponent` | Cleaned shell (Backend ready) | Public | Contact inquiry form (API active at `POST /api/contact-us`) | Backend ready / UI reset |
| *`/cart`* | `UserLayoutComponent` | Cleaned shell (Backend ready) | Authenticated | Customer shopping cart (API active at `/api/cart`) | Backend ready / UI reset |
| *`/wishlist`* | `UserLayoutComponent` | Cleaned shell (Backend ready) | Public/Patron | Customer & guest wishlist (API active at `/api/wishlist`) | Backend ready / UI reset |
| *`/checkout`* | *N/A* | Cleaned shell | Authenticated | Planned checkout workflow | Planned |
| *`/orders`* | *N/A* | Cleaned shell | Authenticated | Customer order history | Planned |

### 5.5 Frontend Services and State Management

| Service / State File | Location | Dependencies | APIs Consumed | Storage Behavior | Error Handling | Status |
|---|---|---|---|---|---|---|
| `ApiService` | `src/app/common/services/api.service.ts` | `HttpClient` | All backend endpoints | In-memory only (`withCredentials: true` on all requests) | Returns raw `Observable<ApiResponse<T>>` | Implemented and verified |
| `AuthService` | `src/app/common/services/auth.service.ts` | `ApiService`, `Router` | `auth/captcha`, `auth/login`, `auth/refresh`, `auth/logout`, `auth/forgot-password` | In-memory Angular Signals (`currentUser`, `accessToken$`). No tokens in `localStorage`. | Handles silent background refresh on 401 | Implemented and verified |
| `LocationService` | `src/app/common/services/location.service.ts` | `ApiService` | `location/countries`, `location/states` | None | Propagates `ApiResponse` | Implemented and verified |
| `UrlEncryptionService` | `src/app/common/services/url-encryption.service.ts` | None | None | Client-side validation only | Pattern match regex (`/^[a-zA-Z0-9_-]+$/`) | Implemented and verified |
| `UrlSignatureService` | `src/app/common/services/url-signature.service.ts` | None | None | Client-side validation only | Timestamp skew & hex check | Implemented and verified |
| `AdminCommissionedProjectService` | `src/app/admin/services/admin-commissioned-project.service.ts` | `ApiService` | `commissions/categories`, `commissions/list`, `commissions/{id}`, `commissions/update-status` | None | Propagates `ApiResponse` | Implemented and verified |
| `AdminCustomerService` | `src/app/admin/services/admin-customer.service.ts` | `ApiService` | `customer/list`, `customer/{id}`, `customer/create`, `customer/update/{id}`, `customer/delete/{id}` | None | Propagates `ApiResponse`. *Note: `toggleActive` endpoint missing on backend.* | Partially implemented |
| `AdminCustomerAddressService` | `src/app/admin/services/admin-customer-address.service.ts` | `ApiService` | `customer-address/list`, `customer-address/{id}`, `customer-address/create`, `customer-address/update`, `customer-address/delete/{id}` | None | Propagates `ApiResponse`. *Note: `toggleActive` endpoint missing on backend.* | Partially implemented |
| `AdminProductCategoryService` | `src/app/admin/services/admin-product-category.service.ts` | `ApiService` | `product-category/list`, `product-category/{id}`, `product-category/create`, `product-category/update`, `product-category/delete/{id}` | None | Propagates `ApiResponse` | Implemented and verified |
| `AdminProductService` | `src/app/admin/services/admin-product.service.ts` | `ApiService` | `product/list`, `product/{id}`, `product/create`, `product/update`, `product/delete/{id}` | None | Propagates `ApiResponse` | Implemented and verified |
| `AdminProductImageService` | `src/app/admin/services/admin-product-image.service.ts` | `ApiService` | `product-image/upload`, `product-image/{id}`, `product-image/set-default`, `product-image/{id}` (DELETE) | Multipart `FormData` | Propagates `ApiResponse` | Implemented and verified |
| `AdminProductStockService` | `src/app/admin/services/admin-product-stock.service.ts` | `ApiService` | `product-stock/list`, `product-stock/add`, `product-stock/update` | None | Propagates `ApiResponse`. *Note: `getById` endpoint missing on backend.* | Partially implemented |
| `AdminAboutService` | `src/app/admin/services/admin-project.service.ts` | `ApiService` | `about/list`, `about/{id}`, `about/create`, `about/update`, `about/{id}/publish`, `about/delete/{id}`, `about/images/upload`, `about/images/{id}`, `about/images/reorder` | In-memory / Multipart `FormData` | Propagates `ApiResponse` | Implemented and verified |
| `UserProductService` | `src/app/user/services/user-product.service.ts` | `ApiService` | `product/search`, `product/{id}` | None | Propagates `ApiResponse` | Implemented and verified |
| `UserAboutService` | `src/app/user/services/user-project.service.ts` | `ApiService` | `about/public` | None | Propagates `ApiResponse` | Implemented and verified |
| `AdminFaqService` | `src/app/admin/services/admin-faq.service.ts` | `ApiService` | `faq/admin/list`, `faq/admin/{id}`, `faq/create`, `faq/update`, `faq/delete/{id}`, `faq/public` | In-memory | Propagates `ApiResponse` | Implemented and verified |
| `CartState` | `src/app/user/services/cart-state.ts` | `ApiService` | `cart`, `cart/items`, `cart/items/{id}` | In-memory Signals | Fully backed by `CartController` | Backend implemented & verified |
| `WishlistState` | `src/app/user/services/wishlist-state.ts` | `ApiService` | `wishlist`, `wishlist/items`, `wishlist/merge`, `wishlist/items/{id}/move-to-cart` | Local/Cookie guest token fallback | Fully backed by `WishlistController` | Backend implemented & verified |

### 5.6 UI Theme and Design System

The Khel-Akhel design system is implemented in SCSS using CSS Custom Properties and modular theme files (`styles.scss`, `admin-theme.scss`, `user-theme.scss`):

- **Brand Color Tokens**:
  - Deep Navy: `#1F3557` (Primary brand anchor, header accents, high-priority buttons).
  - Dark Navy: `#162A45` (Hover state for primary actions).
  - Terracotta: `#B45132` (Primary warm accent, callouts, active links).
  - Terracotta Hover: `#923F28`.
  - Lustrous Ochre: `#D9A441` (Craft and studio material accent).
  - Studio Teal: `#2D7370` (Success indicators, verified badges).
  - Olive: `#737B4C` (Natural material accent).
  - Error Red: `#A83F32` (Form validation errors, danger actions).
- **Surfaces & Backgrounds**:
  - Warm Canvas / Ivory: `#F5F1E8` (Global application background).
  - Elevated Card Surface: `#FBF9F4` (Clean card panels, modals).
  - Subtle Surface Alt: `#F1E7D5` (Table hover states, badges, filters).
  - Border Tone: `#D9D3C6` (Card boundaries, dividers).
- **Typography**:
  - Headings: `Georgia, 'Times New Roman', serif` (Warm editorial serif voice).
  - Body & UI Controls: `'Inter', system-ui, -apple-system, sans-serif` (Clean legibility).
  - Primary Text: `#1E2430` (High contrast, WCAG AAA compliant).
  - Secondary Text: `#6B6A63` (Meta descriptions, table labels).
- **Table System**:
  - Clean responsive wrapping with specialized layout classes: `.col-id`, `.col-code`, `.col-name`, `.col-email`, `.col-phone`, `.col-category`, `.col-desc`, `.col-numeric`, `.col-badge`, `.col-actions`.
  - Overflow controls: `.text-wrap-natural` and `.text-wrap-anywhere` prevent email and UUID clipping.

---

## 6. Backend Architecture (.NET 8 Web API)

### 6.1 Application Startup & Middleware Pipeline (`Program.cs`)
The backend is structured around a streamlined ASP.NET Core 8 minimal hosting model:
1. **Dependency Injection**:
   - `DbHelper`: Scoped ADO.NET SQL Server manager.
   - `IUrlEncryptionService` / `UrlEncryptionService`: Scoped AES-GCM-256 identifier encryption.
   - `IUrlSignatureService` / `UrlSignatureService`: Scoped HMAC-SHA256 URL integrity signer.
   - `IAuditService` / `AuditService`: Scoped audit log recorder.
   - `IRefreshTokenService` / `RefreshTokenService`: Scoped token rotation manager.
   - `ICaptchaService` / `CaptchaService`: Singleton in-memory captcha generator.
2. **Rate Limiting**:
   - Fixed-window rate limiters configured via `Microsoft.AspNetCore.RateLimiting`:
     - Policy `authentication` / `AuthPolicy`: 15 requests per minute, queue limit 3.
     - Policy `GeneralPolicy`: 100 requests per minute, queue limit 10.
3. **CORS Pipeline**:
   - Named policy `"AngularPolicy"` specifically bound to `http://localhost:4200` with `AllowCredentials()`, `AllowAnyHeader()`, and `AllowAnyMethod()`.
4. **Middleware Order**:
   - `Logs.CleanupOldLogs(45)` $\rightarrow$ `UseSwagger()` $\rightarrow$ `UseCors("AngularPolicy")` $\rightarrow$ `UseStaticFiles()` $\rightarrow$ `UseRateLimiter()` $\rightarrow$ `UseAuthentication()` $\rightarrow$ `UseAuthorization()` $\rightarrow$ `MapControllers()`.

### 6.2 Data Access Layer (`DbHelper.cs`)
Data operations bypass heavy ORMs in favor of direct parameterized ADO.NET execution via `DbHelper`:
- **Query Safety Validator**: Every query passed to `ExecuteQuery`, `ExecuteNonQuery`, or `ExecuteScalar` is checked against a blacklist of forbidden DDL/administrative commands (`ALTER`, `DROP`, `CREATE`, `BACKUP`, `RESTORE`) using case-insensitive regex word boundaries (`\bKEYWORD\b`).
- **Parameter Cloning**: `SqlParameter` objects are explicitly cloned before command addition to ensure thread-safety across concurrent query executions.
- **Transaction Support**: Provides `GetOpenConnectionAsync()` for multi-step atomic operations (e.g. creating a record and persisting associated image rows).

### 6.3 Security Mechanisms
- **Identifier Obfuscation**: Database numeric IDs (`BIGINT`) are never exposed directly over the wire. Instead, `UrlEncryptionService` encrypts IDs into URL-safe Base64 strings using AES-GCM (12-byte IV, 16-byte authentication tag).
- **Password Security**: Managed via `PasswordHelper` using salted BCrypt. Plain-text verification fallback exists only for initial un-hashed seeds.
- **File Upload Security**: `FileValidationHelper` inspects both the MIME type and the binary magic bytes (JPEG: `FF D8`, PNG: `89 50 4E 47`, WEBP: `RIFF....WEBP`) to prevent malicious executable masquerading.

---

## 7. Complete Backend Controller and API Inventory

| Controller | HTTP Method | Route | Access Requirements | Request Parameters / Body | Response Type | Purpose / Description | Status |
|---|---|---|---|---|---|---|---|
| **AuthController** | `GET` | `api/auth/captcha` | Anonymous | None | `ApiResponse<CaptchaResponseData>` | Generates 5-char randomized captcha code and unique `captchaId` | Implemented and verified |
| **AuthController** | `POST` | `api/auth/login` | Anonymous / Rate Limited (15/m) | `[FromBody] PortalLoginRequest` | `ApiResponse<AuthResponseData>` | Authenticates user, issues JWT access token, and sets `drs_refresh_token` cookie | Implemented and verified |
| **AuthController** | `POST` | `api/auth/refresh` | Anonymous | Cookie `drs_refresh_token` | `ApiResponse<AuthResponseData>` | Rotates refresh token, detects reuse, and issues new JWT access token | Implemented and verified |
| **AuthController** | `POST` | `api/auth/logout` | Anonymous | Cookie `drs_refresh_token` | `ApiResponse<null>` | Revokes refresh token in database and clears HTTP cookie | Implemented and verified |
| **AuthController** | `POST` | `api/auth/forgot-password` | Anonymous | `[FromBody] ForgetPasswordRequest` | `ApiResponse<null>` | Generates password reset token and logs audit event | Implemented and verified |
| **AuthController** | `PATCH`/`POST` | `api/auth/change-password` | Authenticated | `[FromBody] ChangePasswordRequest` | `ApiResponse<null>` | Updates user password after validating existing credentials | Implemented and verified |
| **CustomerController** | `POST` | `api/customer/create` | Anonymous / Admin | `[FromBody] CustomerCreateRequest` | `ApiResponse<CustomerResponse>` | Registers a new customer with auto-sequenced `Account_no` (`DRC######`) | Implemented and verified |
| **CustomerController** | `PATCH` | `api/customer/update/{encryptedId?}` | Authenticated | `[FromBody] CustomerUpdateRequest` | `ApiResponse<CustomerResponse>` | Updates customer profile fields (Name, Email, Mobile) | Implemented and verified |
| **CustomerController** | `GET` | `api/customer/{encryptedId}` | Authenticated | Route `encryptedId` | `ApiResponse<CustomerResponse>` | Retrieves single customer profile by encrypted identifier | Implemented and verified |
| **CustomerController** | `GET`/`POST` | `api/customer/list` | Admin | Query/Body `CustomerListRequest` | `ApiResponse<CustomerListResponse>` | Paginated, searchable customer directory | Implemented and verified |
| **CustomerController** | `DELETE`/`PATCH` | `api/customer/delete/{encryptedId?}` | Admin | Route `encryptedId` | `ApiResponse<null>` | Soft-deletes customer record (`IsDeleted = 1`) | Implemented and verified |
| **CustomerAddressController** | `POST` | `api/customer-address/create` | Authenticated | `[FromBody] CustomerAddressCreateRequest` | `ApiResponse<CustomerAddressResponse>` | Creates delivery address; unsets prior default if `IsDefault = 1` | Implemented and verified |
| **CustomerAddressController** | `PATCH` | `api/customer-address/update` | Authenticated | `[FromBody] CustomerAddressUpdateRequest` | `ApiResponse<CustomerAddressResponse>` | Updates delivery address attributes | Implemented and verified |
| **CustomerAddressController** | `GET` | `api/customer-address/{encryptedId}` | Authenticated | Route `encryptedId` | `ApiResponse<CustomerAddressResponse>` | Retrieves specific address by encrypted ID | Implemented and verified |
| **CustomerAddressController** | `GET` | `api/customer-address/list` | Authenticated | Query `encryptedCustomerId` (optional for Admin) | `ApiResponse<List<CustomerAddressResponse>>` | Lists addresses for customer or all addresses for admin | Implemented and verified |
| **CustomerAddressController** | `DELETE`/`PATCH` | `api/customer-address/delete/{encryptedId?}` | Authenticated | Route `encryptedId` | `ApiResponse<null>` | Soft-deletes delivery address | Implemented and verified |
| **LocationMasterController** | `GET` | `api/location/countries` | Anonymous | None | `ApiResponse<List<CountryResponse>>` | Returns all active countries ordered by name | Implemented and verified |
| **LocationMasterController** | `GET` | `api/location/states` | Anonymous | Query `countryId` (long) | `ApiResponse<List<StateResponse>>` | Returns all active states for given country | Implemented and verified |
| **LocationMasterController** | `GET` | `api/location/countries/{id}` | Anonymous | Route `id` (long) | `ApiResponse<CountryResponse>` | Returns country detail by ID | Implemented and verified |
| **LocationMasterController** | `GET` | `api/location/states/{id}` | Anonymous | Route `id` (long) | `ApiResponse<StateResponse>` | Returns state detail by ID | Implemented and verified |
| **ProductCategoryController** | `POST` | `api/product-category/create` | Admin | `[FromBody] ProductCategoryCreateRequest` | `ApiResponse<ProductCategoryResponse>` | Creates product category with uniqueness check | Implemented and verified |
| **ProductCategoryController** | `PATCH` | `api/product-category/update` | Admin | `[FromBody] ProductCategoryUpdateRequest` | `ApiResponse<ProductCategoryResponse>` | Updates category name, description, order, active flag | Implemented and verified |
| **ProductCategoryController** | `GET` | `api/product-category/{encryptedId}` | Anonymous / Admin | Route `encryptedId` | `ApiResponse<ProductCategoryResponse>` | Retrieves category details | Implemented and verified |
| **ProductCategoryController** | `GET` | `api/product-category/list` | Anonymous / Admin | None | `ApiResponse<List<ProductCategoryResponse>>` | Lists all active categories ordered by `DisplayOrder` | Implemented and verified |
| **ProductCategoryController** | `DELETE`/`PATCH` | `api/product-category/delete/{encryptedCategoryId?}` | Admin | Route `encryptedCategoryId` | `ApiResponse<null>` | Soft-deletes product category | Implemented and verified |
| **ProductController** | `POST` | `api/product/create` | Admin | `[FromBody] ProductCreateRequest` | `ApiResponse<ProductResponse>` | Creates product with auto-sequenced `ProductCode` (`DRP######`) | Implemented and verified |
| **ProductController** | `PATCH` | `api/product/update` | Admin | `[FromBody] ProductUpdateRequest` | `ApiResponse<ProductResponse>` | Updates product attributes and pricing | Implemented and verified |
| **ProductController** | `GET` | `api/product/{encryptedId}` | Anonymous / Admin | Route `encryptedId` | `ApiResponse<ProductResponse>` | Retrieves product detail, stock, and gallery images | Implemented and verified |
| **ProductController** | `GET`/`POST` | `api/product/list` | Admin | Query/Body `ProductListRequest` | `ApiResponse<ProductListResponse>` | Paginated product list with search and filters | Implemented and verified |
| **ProductController** | `GET`/`POST` | `api/product/search` | Anonymous | Query/Body `ProductListRequest` | `ApiResponse<ProductListResponse>` | Public catalog search endpoint | Implemented and verified |
| **ProductController** | `DELETE`/`PATCH` | `api/product/delete/{encryptedId?}` | Admin | Route `encryptedId` | `ApiResponse<null>` | Soft-deletes product record | Implemented and verified |
| **ProductImageController** | `POST` | `api/product-image/upload` | Admin | `[FromForm] ProductImageUploadRequest` | `ApiResponse<ProductImageUploadResponse>` | Uploads up to 10 product images with binary validation | Implemented and verified |
| **ProductImageController** | `GET` | `api/product-image/{encryptedProductId}` | Anonymous / Admin | Route `encryptedProductId` | `ApiResponse<List<ProductImageResponse>>` | Returns all images for a given product | Implemented and verified |
| **ProductImageController** | `PATCH` | `api/product-image/set-default` | Admin | `[FromBody] SetDefaultImageRequest` | `ApiResponse<null>` | Sets primary thumbnail image for product | Implemented and verified |
| **ProductImageController** | `DELETE` | `api/product-image/{encryptedImageId}` | Admin | Route `encryptedImageId` | `ApiResponse<null>` | Soft-deletes product image | Implemented and verified |
| **ProductStockController** | `POST` | `api/product-stock/add` | Admin | `[FromBody] ProductStockAddRequest` | `ApiResponse<ProductStockResponse>` | Initializes or increments product stock | Implemented and verified |
| **ProductStockController** | `PATCH` | `api/product-stock/update` | Admin | `[FromBody] ProductStockUpdateRequest` | `ApiResponse<ProductStockResponse>` | Sets explicit available and reserved quantities | Implemented and verified |
| **ProductStockController** | `GET`/`POST` | `api/product-stock/list` | Admin | Query/Body `ProductStockListRequest` | `ApiResponse<ProductStockListResponse>` | Paginated inventory list with low-stock filter ($\le 5$) | Implemented and verified |
| **commissionsController** | `GET` | `api/commissions/categories` | Anonymous | None | `ApiResponse<List<CommissionCategoryResponse>>` | Public dropdown categories for custom commissions | Implemented and verified |
| **commissionsController** | `POST` | `api/commissions/create` | Anonymous | `[FromForm] CommissionRequestCreateDto` | `ApiResponse<CommissionCreateResponse>` | Submits custom project request with up to 3 images | Implemented and verified |
| **commissionsController** | `GET` | `api/commissions/list` | Admin | Query `CommissionListFilterRequest` | `ApiResponse<CommissionListResponse>` | Paginated commission requests with status and date filters | Implemented and verified |
| **commissionsController** | `GET` | `api/commissions/{encryptedId}` | Admin | Route `encryptedId` | `ApiResponse<CommissionRequestDetailResponse>` | Full inquiry detail with word count and images | Implemented and verified |
| **commissionsController** | `PATCH` | `api/commissions/update-status` | Admin | `[FromBody] CommissionStatusUpdateRequest` | `ApiResponse<null>` | Updates inquiry review status and internal admin notes | Implemented and verified |
| **AboutController** | `POST` | `api/projects/create` | Admin | `[FromForm] AboutCreateRequest` | `ApiResponse<AboutDetailResponse>` | Creates dynamic About section with up to 15 images | Implemented and verified |
| **AboutController** | `PUT`/`PATCH` | `api/projects/update` | Admin | `[FromForm] AboutUpdateRequest` | `ApiResponse<AboutDetailResponse>` | Updates About narrative, credits, links, and media | Implemented and verified |
| **AboutController** | `GET` | `api/about/{encryptedAboutId}` | Admin | Route `encryptedAboutId` | `ApiResponse<AboutDetailResponse>` | Fetches single About section by encrypted ID | Implemented and verified |
| **AboutController** | `GET`/`POST` | `api/about/list` | Admin | Query/Body `AboutListRequest` | `ApiResponse<AboutListResponse>` | Admin listing of all published and draft About entries | Implemented and verified |
| **AboutController** | `GET` | `api/about/public` | Anonymous | None | `ApiResponse<List<AboutPublicResponse>>` | Publicly published studio story and practice sections | Implemented and verified |
| **AboutController** | `PATCH` | `api/about/{encryptedAboutId}/publish` | Admin | Route `encryptedAboutId` | `ApiResponse<null>` | Toggles section visibility between draft and published | Implemented and verified |
| **AboutController** | `DELETE` | `api/about/delete/{encryptedAboutId}` | Admin | Route `encryptedAboutId` | `ApiResponse<null>` | Soft-deletes About section and linked images | Implemented and verified |
| **AboutController** | `POST` | `api/about/images/upload` | Admin | `[FromForm] AboutImageUploadRequest` | `ApiResponse<List<ProjectImageDto>>` | Uploads additional gallery images to existing section | Implemented and verified |
| **AboutController** | `DELETE` | `api/about/images/{encryptedImageId}` | Admin | Route `encryptedImageId` | `ApiResponse<null>` | Deletes single image from About section | Implemented and verified |
| **AboutController** | `PUT`/`POST` | `api/about/images/reorder` | Admin | `[FromBody] AboutImageReorderRequest` | `ApiResponse<null>` | Reorders gallery image display sequence | Implemented and verified |
| **FAQController** | `POST` | `api/faq/create` | Admin | `[FromBody] FaqCreateRequest` | `ApiResponse<FaqResponse>` | Creates FAQ with page placement flags | Implemented and verified |
| **FAQController** | `GET` | `api/faq/admin/list` | Admin | Query `search`, `category`, `isPublished` | `ApiResponse<List<FaqResponse>>` | Comprehensive admin FAQ management list | Implemented and verified |
| **FAQController** | `GET` | `api/faq/admin/{encryptedFaqId}` | Admin | Route `encryptedFaqId` | `ApiResponse<FaqResponse>` | Fetches specific FAQ detail | Implemented and verified |
| **FAQController** | `PATCH` | `api/faq/update` | Admin | `[FromBody] FaqUpdateRequest` | `ApiResponse<FaqResponse>` | Updates FAQ text and page visibility flags | Implemented and verified |
| **FAQController** | `DELETE` | `api/faq/delete/{encryptedFaqId}` | Admin | Route `encryptedFaqId` | `ApiResponse<null>` | Soft-deletes FAQ record | Implemented and verified |
| **FAQController** | `GET` | `api/faq/public` | Anonymous | Query `location` (`faq`, `home`, `checkout`) | `ApiResponse<List<FaqResponse>>` | Public FAQ list filtered by page context | Implemented and verified |
| **CartController** | `POST` | `api/cart/items` | Authenticated | `[FromBody] CartItemAddRequest` | `ApiResponse<CartResponse>` | Adds item to cart or increments quantity with live stock validation | Implemented and verified |
| **CartController** | `GET` | `api/cart` | Authenticated | None | `ApiResponse<CartResponse>` | Retrieves active shopping cart with calculated line item totals | Implemented and verified |
| **CartController** | `PATCH` | `api/cart/items/{encryptedCartItemId}` | Authenticated | `[FromBody] CartItemUpdateRequest` | `ApiResponse<CartResponse>` | Updates line item quantity with available inventory check | Implemented and verified |
| **CartController** | `DELETE` | `api/cart/items/{encryptedCartItemId}` | Authenticated | Route `encryptedCartItemId` | `ApiResponse<CartResponse>` | Removes single line item from cart | Implemented and verified |
| **CartController** | `DELETE` | `api/cart/items` | Authenticated | None | `ApiResponse<null>` | Clears all items in customer cart | Implemented and verified |
| **WishlistController** | `POST` | `api/wishlist/items` | Authenticated / Guest Token | `[FromBody] WishlistItemAddRequest` | `ApiResponse<WishlistResponse>` | Adds product to customer or guest wishlist with duplicate prevention | Implemented and verified |
| **WishlistController** | `GET` | `api/wishlist` | Authenticated / Guest Token | None | `ApiResponse<WishlistResponse>` | Retrieves wishlist items for authenticated user or guest session | Implemented and verified |
| **WishlistController** | `DELETE` | `api/wishlist/items/{encryptedWishlistItemId}` | Authenticated / Guest Token | Route `encryptedWishlistItemId` | `ApiResponse<WishlistResponse>` | Removes single item from wishlist | Implemented and verified |
| **WishlistController** | `DELETE` | `api/wishlist/items` | Authenticated / Guest Token | None | `ApiResponse<null>` | Clears all items from wishlist | Implemented and verified |
| **WishlistController** | `POST` | `api/wishlist/merge` | Authenticated | `[FromBody] WishlistMergeRequest` | `ApiResponse<WishlistResponse>` | Merges guest session wishlist into authenticated customer account | Implemented and verified |
| **WishlistController** | `POST` | `api/wishlist/items/{encryptedWishlistItemId}/move-to-cart` | Authenticated | Route `encryptedWishlistItemId` | `ApiResponse<WishlistResponse>` | Atomically moves item from wishlist to active cart with stock check | Implemented and verified |
| **ContactUsController** | `POST` | `api/contact-us` | Anonymous / Rate Limited | `[FromBody] ContactInquiryRequest` | `ApiResponse<ContactInquiryResponse>` | Submits direct client inquiry (Name, Mobile, Email, Company, Service Desc) | Implemented and verified |

---

## 8. Frontend-to-Backend API Mapping & Integration Audit

### 8.1 Integration Mapping Table

| Frontend Feature | Angular Service Method | Backend Controller | Endpoint | Payload / Parameters | Integration Status | Notes / Discrepancies |
|---|---|---|---|---|---|---|
| Admin Login | `AuthService.login()` | `AuthController` | `POST api/auth/login` | `{ userName, password, captchaId, captchaCode }` | **Integrated & verified** | Full credentials + captcha flow |
| Captcha Reload | `AuthService.getCaptcha()` | `AuthController` | `GET api/auth/captcha` | None | **Integrated & verified** | Base64 SVG captcha image |
| Session Refresh | `AuthService.refreshToken()` | `AuthController` | `POST api/auth/refresh` | Cookie `drs_refresh_token` | **Integrated & verified** | Handled transparently by interceptor |
| Admin Logout | `AuthService.logout()` | `AuthController` | `POST api/auth/logout` | None | **Integrated & verified** | Clears session signal & cookie |
| Dashboard Counters | Multi-Service `forkJoin` | Multiple | Multiple (`list` endpoints) | `{ pageIndex: 1, pageSize: 1 }` | **Integrated & verified** | Derived counts; no single dashboard API |
| Customer List | `AdminCustomerService.getList()` | `CustomerController` | `GET api/customer/list` | `CustomerListRequest` query | **Integrated & verified** | Debounced search & pagination |
| Customer Create | `AdminCustomerService.create()` | `CustomerController` | `POST api/customer/create` | `CustomerCreateRequest` body | **Integrated & verified** | Returns created customer with `Account_no` |
| Customer Detail | `AdminCustomerService.getById()` | `CustomerController` | `GET api/customer/{id}` | Route `encryptedId` | **Integrated & verified** | Fetches customer profile |
| Customer Update | `AdminCustomerService.update()` | `CustomerController` | `PATCH api/customer/update/{id}` | `CustomerUpdateRequest` body | **Integrated & verified** | Updates customer profile |
| Customer Soft-Delete | `AdminCustomerService.delete()` | `CustomerController` | `DELETE api/customer/delete/{id}` | Route `encryptedId` | **Integrated & verified** | Soft-deletes customer |
| Customer Toggle-Active | `AdminCustomerService.toggleActive()` | **None** | *`PATCH api/customer/toggle-active/{id}`* | Route `encryptedId` | **Broken / Missing endpoint** | Backend lacks toggle endpoint (returns 404) |
| Customer Address List | `AdminCustomerAddressService.getList()` | `CustomerAddressController` | `GET api/customer-address/list` | Query `encryptedCustomerId` | **Integrated & verified** | Lists customer delivery addresses |
| Customer Address Create | `AdminCustomerAddressService.create()` | `CustomerAddressController` | `POST api/customer-address/create` | `CustomerAddressCreateRequest` | **Integrated & verified** | Handles default address flag |
| Customer Address Update | `AdminCustomerAddressService.update()` | `CustomerAddressController` | `PATCH api/customer-address/update` | `CustomerAddressUpdateRequest` | **Integrated & verified** | Updates address |
| Customer Address Delete | `AdminCustomerAddressService.delete()` | `CustomerAddressController` | `DELETE api/customer-address/delete/{id}` | Route `encryptedId` | **Integrated & verified** | Soft-deletes address |
| Address Toggle-Active | `AdminCustomerAddressService.toggleActive()` | **None** | *`PATCH api/customer-address/toggle-active/{id}`* | Route `encryptedId` | **Broken / Missing endpoint** | Backend lacks toggle endpoint (returns 404) |
| Location Countries | `LocationService.getCountries()` | `LocationMasterController` | `GET api/location/countries` | None | **Integrated & verified** | Master country list |
| Location States | `LocationService.getStates()` | `LocationMasterController` | `GET api/location/states` | Query `countryId` | **Integrated & verified** | Cascading state dropdown |
| Category List | `AdminProductCategoryService.getList()` | `ProductCategoryController` | `GET api/product-category/list` | None | **Integrated & verified** | Dropdown & grid data |
| Category Create | `AdminProductCategoryService.create()` | `ProductCategoryController` | `POST api/product-category/create` | `ProductCategoryCreateRequest` | **Integrated & verified** | Unique category creation |
| Category Update | `AdminProductCategoryService.update()` | `ProductCategoryController` | `PATCH api/product-category/update` | `ProductCategoryUpdateRequest` | **Integrated & verified** | Updates category attributes |
| Category Soft-Delete | `AdminProductCategoryService.delete()` | `ProductCategoryController` | `DELETE api/product-category/delete/{id}` | Route `encryptedCategoryId` | **Integrated & verified** | Soft-deletes category |
| Category Toggle-Active | `AdminProductCategoryService.toggleActive()` | **None** | *`PATCH api/product-category/toggle-active/{id}`* | Route `encryptedCategoryId` | **Broken / Missing endpoint** | Backend lacks toggle endpoint (returns 404) |
| Product List | `AdminProductService.getList()` | `ProductController` | `GET api/product/list` | `ProductListRequest` query | **Integrated & verified** | Catalog inventory table |
| Product Create | `AdminProductService.create()` | `ProductController` | `POST api/product/create` | `ProductCreateRequest` body | **Integrated & verified** | Generates `DRP######` code |
| Product Detail | `AdminProductService.getById()` | `ProductController` | `GET api/product/{id}` | Route `encryptedProductId` | **Integrated & verified** | Detail page view |
| Product Update | `AdminProductService.update()` | `ProductController` | `PATCH api/product/update` | `ProductUpdateRequest` body | **Integrated & verified** | Updates pricing & flags |
| Product Soft-Delete | `AdminProductService.delete()` | `ProductController` | `DELETE api/product/delete/{id}` | Route `encryptedProductId` | **Integrated & verified** | Soft-deletes product |
| Product Toggle-Active | `AdminProductService.toggleActive()` | **None** | *`PATCH api/product/toggle-active/{id}`* | Route `encryptedProductId` | **Broken / Missing endpoint** | Backend lacks toggle endpoint (returns 404) |
| Product Image Upload | `AdminProductImageService.uploadImages()` | `ProductImageController` | `POST api/product-image/upload` | Multipart `FormData` | **Integrated & verified** | Multi-image gallery uploader |
| Product Image Set-Default | `AdminProductImageService.setDefault()` | `ProductImageController` | `PATCH api/product-image/set-default` | `{ encryptedImageId }` | **Integrated & verified** | Primary thumbnail selector |
| Product Image Delete | `AdminProductImageService.deleteImage()` | `ProductImageController` | `DELETE api/product-image/{id}` | Route `encryptedImageId` | **Integrated & verified** | Soft-deletes image |
| Stock List | `AdminProductStockService.getList()` | `ProductStockController` | `GET api/product-stock/list` | `ProductStockListRequest` query | **Integrated & verified** | Low stock monitoring |
| Stock Add | `AdminProductStockService.addStock()` | `ProductStockController` | `POST api/product-stock/add` | `ProductStockAddRequest` body | **Integrated & verified** | Quantity increment |
| Stock Update | `AdminProductStockService.updateStock()` | `ProductStockController` | `PATCH api/product-stock/update` | `ProductStockUpdateRequest` body | **Integrated & verified** | Sets available/reserved stock |
| Stock Get-By-Id | `AdminProductStockService.getById()` | **None** | *`GET api/product-stock/{id}`* | Route `encryptedStockId` | **Broken / Missing endpoint** | Backend lacks getById endpoint (returns 404) |
| Commission Categories | `AdminCommissionedProjectService.getCategories()` | `commissionsController` | `GET api/commissions/categories` | None | **Integrated & verified** | Category filter dropdown |
| Commission List | `AdminCommissionedProjectService.getList()` | `commissionsController` | `GET api/commissions/list` | `CommissionListFilter` query | **Integrated & verified** | Search, filter, date range |
| Commission Detail | `AdminCommissionedProjectService.getById()` | `commissionsController` | `GET api/commissions/{id}` | Route `encryptedId` | **Integrated & verified** | Detail modal with word count |
| Commission Update Status | `AdminCommissionedProjectService.updateStatus()` | `commissionsController` | `PATCH api/commissions/update-status` | `CommissionStatusUpdateRequest` | **Integrated & verified** | Status & notes update dialog |
| Public Product Catalog | `UserProductService.getPublicProducts()` | `ProductController` | `POST api/product/search` | `{ pageIndex: 1, pageSize: 20, isActive: true }` | **Integrated & verified** | Consumer product gallery |
| Public Projects Showcase | `UserProjectService.getPublicProjects()` | `ProjectsController` | `GET api/projects/public` | None | **Integrated & verified** | Dynamic project showcase rendering with 7 gallery layouts & lightbox |
| Project List (Admin) | `AdminProjectService.getList()` | `ProjectsController` | `GET api/projects/list` | `ProjectListRequest` query | **Integrated & verified** | Search, discipline filter, order |
| Project Detail (Admin) | `AdminProjectService.getById()` | `ProjectsController` | `GET api/projects/{id}` | Route `encryptedProjectId` | **Integrated & verified** | Narrative fields, layout & images |
| Project Create | `AdminProjectService.create()` | `ProjectsController` | `POST api/projects/create` | `[FromForm] ProjectCreateRequest` | **Integrated & verified** | With `LayoutType` & multi-image upload |
| Project Update | `AdminProjectService.update()` | `ProjectsController` | `PUT api/projects/update` | `[FromForm] ProjectUpdateRequest` | **Integrated & verified** | Fields, layout, new images, removal, order |
| Project Toggle Publish | `AdminProjectService.togglePublish()` | `ProjectsController` | `PATCH api/projects/{id}/publish` | Route `encryptedProjectId` | **Integrated & verified** | Toggles draft vs published state |
| Project Delete | `AdminProjectService.delete()` | `ProjectsController` | `DELETE api/projects/delete/{id}` | Route `encryptedProjectId` | **Integrated & verified** | Soft-deletes section and images |
| Project Images Upload | `AdminProjectService.uploadImages()` | `ProjectsController` | `POST api/projects/images/upload` | Multipart `FormData` | **Integrated & verified** | Uploads up to 15 total images |
| Project Image Delete | `AdminProjectService.deleteImage()` | `ProjectsController` | `DELETE api/projects/images/{id}` | Route `encryptedImageId` | **Integrated & verified** | Soft-deletes individual image |
| Project Images Reorder | `AdminProjectService.reorderImages()` | `ProjectsController` | `PUT api/projects/images/reorder` | `ProjectImageReorderRequest` body | **Integrated & verified** | Persists custom image display sequence |
| Admin FAQ Management | `AdminFaqService.getAdminList()`, `.create()`, `.update()`, `.delete()` | `FAQController` | Multiple (`admin/list`, `create`, `update`, `delete`) | `FaqCreateRequest`, `FaqUpdateRequest`, query | **Integrated & verified** | Full CRUD, modal form, filter toolbar, inline status/publish toggles, delete dialog |
| Public FAQ Knowledge Base | `AdminFaqService.getPublic('faq')` | `FAQController` | `GET api/faq/public` | Query `location=faq` | **Integrated & verified** | Customer accordion view with category chips, live search, contact CTA |
| Contact Us Inquiry | Storefront Contact Form | `ContactUsController` | `POST api/contact-us` | `ContactInquiryRequest` body | **Backend ready** | High-throughput direct controller, rate-limited, validated |
| Shopping Cart API | Storefront Cart State | `CartController` | `GET/POST/PATCH/DELETE api/cart` | `CartItemAddRequest`, `CartItemUpdateRequest` | **Backend verified** | Atomic upsert, concurrency-safe, stock checks, subtotals |
| Wishlist API | Storefront Wishlist State | `WishlistController` | `GET/POST/DELETE api/wishlist` | `WishlistItemAddRequest`, `WishlistMergeRequest` | **Backend verified** | Dual-mode authenticated/guest sessions, login merge, move-to-cart |
| Commission Public Form | **None** | `commissionsController` | `POST api/commissions/create` | `[FromForm] CommissionRequestCreateDto` | **Backend only** | Customer-side commission form pending UI binding |

---

## 9. Database Documentation (SQL Server / KhelAkhe_CRM)

### 9.1 Database Architecture and Conventions
- **Database Name**: `KhelAkhe_CRM` (Confirmed in configuration and SQL migration scripts).
- **Primary Keys**: Surrogate `BIGINT IDENTITY(1,1)` naming convention (`[ID]`), except `drs_faq_mst` which uses `INT IDENTITY(1,1)`.
- **Soft Deletes**: Standardized columns `[IsActive] BIT DEFAULT 1` and `[IsDeleted] BIT DEFAULT 0`. All operational queries filter `WHERE IsDeleted = 0`.
- **Timestamps**: `[CreatedDate] DATETIME DEFAULT GETDATE()` and nullable `[ModifiedDate] DATETIME`.
- **Foreign Key Prefixing**: Lowercase referenced entity name with `_id` suffix (e.g. `[customer_id]`, `[product_id]`, `[product_category_id]`, `[country_id]`, `[state_id]`, `[project_id]`, `[commission_request_id]`).

### 9.2 Complete Table Inventory

| Table Name | Purpose | Primary Key | Key Columns | Nullability & Constraints | Status |
|---|---|---|---|---|---|
| `drs_customer_mst` | Customer & Admin user profiles | `ID` (BIGINT) | `Account_no`, `FirstName`, `LastName`, `Email`, `MobileNo`, `PasswordHash`, `IsAdmin`, `IsActive`, `IsDeleted` | `Account_no` UQ, `Email` UQ filtered, `MobileNo` UQ filtered | Implemented and verified |
| `drs_customer_address_mst` | Customer delivery addresses | `ID` (BIGINT) | `customer_id`, `FullName`, `MobileNo`, `AddressLine1`, `City`, `State`, `Country`, `Pincode`, `IsDefault`, `country_id`, `state_id` | `customer_id` NOT NULL, `country_id` FK, `state_id` FK | Implemented and verified |
| `drs_product_category_mst` | Product catalog categories | `ID` (BIGINT) | `CategoryName`, `Description`, `DisplayOrder`, `IsActive`, `IsDeleted` | `CategoryName` UQ filtered | Implemented and verified |
| `drs_product_mst` | Master product catalog items | `ID` (BIGINT) | `product_category_id`, `ProductName`, `ProductCode`, `SKU`, `MRP`, `SellingPrice`, `IsFeatured`, `IsNewArrival`, `IsActive`, `IsDeleted` | `ProductCode` UQ, `SKU` UQ filtered, `MRP >= 0` CK, `SellingPrice >= 0` CK | Implemented and verified |
| `drs_product_stock_mst` | Inventory stock per product | `ID` (BIGINT) | `product_id`, `AvailableQty`, `ReservedQty`, `IsActive`, `IsDeleted` | `product_id` UQ filtered, `AvailableQty >= 0` CK, `ReservedQty >= 0` CK | Implemented and verified |
| `drs_product_image_mst` | Product gallery images | `ID` (BIGINT) | `product_id`, `ImagePath`, `DisplayOrder`, `IsDefault`, `IsActive`, `IsDeleted` | `product_id` NOT NULL, `ImagePath` NOT NULL | Implemented and verified |
| `drs_refresh_token_mst` | JWT refresh tokens | `ID` (BIGINT) | `customer_id`, `TokenHash`, `CreatedUtc`, `ExpiresUtc`, `RevokedUtc`, `CreatedByIp`, `DeviceName`, `IsActive` | `customer_id` NOT NULL, `TokenHash` NOT NULL | Implemented and verified |
| `drs_login_history_mst` | Authentication audit log | `ID` (BIGINT) | `customer_id`, `LoginType`, `DeviceInfo`, `IPAddress`, `IsSuccess`, `FailureReason`, `CreatedDate` | `customer_id` NULL (captures failed unknown logins) | Implemented and verified |
| `drs_audit_mst` | System-wide entity change audit | `ID` (BIGINT) | `customer_id`, `ModuleName`, `Action`, `TableName`, `EntityId`, `OldValue`, `NewValue`, `IPAddress`, `UserAgent` | `OldValue`/`NewValue` sanitize passwords | Implemented and verified |
| `drs_error_message_mst` | Centralized localized message seed | `ID` (BIGINT) | `MessageCode`, `MessageText`, `MessageType`, `HttpStatusCode`, `IsActive`, `IsDeleted` | `MessageCode` UQ filtered | Implemented and verified |
| `drs_country_mst` | Global countries master | `ID` (BIGINT) | `CountryName`, `CountryCode`, `PhoneCode`, `IsActive`, `IsDeleted` | `CountryName` UQ filtered, `CountryCode` UQ filtered | Implemented and verified |
| `drs_state_mst` | States/provinces master | `ID` (BIGINT) | `country_id`, `StateName`, `StateCode`, `IsActive`, `IsDeleted` | `(country_id, StateName)` UQ filtered | Implemented and verified |
| `drs_faq_mst` | Frequently asked questions | `ID` (INT) | `Category`, `Question`, `Answer`, `DisplayOrder`, `IsPublished`, `DisplayOnHomePage`, `DisplayOnFaqPage`, `DisplayOnCheckoutPage` | `Question` NOT NULL, `Answer` NOT NULL | Implemented and verified |
| `drs_commission_category_mst` | Categories for custom projects | `ID` (BIGINT) | `CategoryName`, `Description`, `DisplayOrder`, `IsActive`, `IsDeleted` | `CategoryName` UQ filtered | Implemented and verified |
| `drs_commission_request_mst` | Custom commission inquiries | `ID` (BIGINT) | `RequestNo`, `FullName`, `MobileNo`, `Email`, `category_id`, `ExpectedTimeline`, `ProjectLocation`, `ProjectDescription`, `Status`, `AdminNotes` | `RequestNo` UQ, `ExpectedTimeline` DATE NOT NULL | Implemented and verified |
| `drs_commission_request_image_mst` | Reference sketches for inquiries | `ID` (BIGINT) | `commission_request_id`, `OriginalFileName`, `StoredFileName`, `StoragePath`, `ContentType`, `FileSize`, `DisplayOrder` | `commission_request_id` NOT NULL | Implemented and verified |
| `drs_project_mst` | Dynamic studio story sections | `ID` (BIGINT) | `Heading`, `Subheading`, `ContentType`, `ProjectOrEventTitle`, `DisplayYear`, `ShortDescription`, `DetailedDescription`, `Credits`, `Location`, `ExternalUrl`, `LayoutType`, `DisplayOrder`, `IsPublished` | `Heading` NOT NULL, `LayoutType` NOT NULL DEFAULT 'grid-3' (`CK_drs_project_mst_LayoutType`: `grid-3`, `grid-2`, `editorial`, `featured`, `full-width`, `masonry`, `asymmetric`), `DisplayOrder` INT | Implemented and verified |
| `drs_project_image_mst` | Media assets for About sections | `ID` (BIGINT) | `project_id`, `OriginalFileName`, `StoredFileName`, `ImagePath`, `ContentType`, `FileSize`, `DisplayOrder` | `project_id` NOT NULL, Max 15 per section | Implemented and verified |
| `drs_wishlist_mst` | Customer & Guest Wishlist sessions | `ID` (BIGINT) | `customer_id`, `GuestTokenHash`, `IsActive`, `IsDeleted` | `CK_drs_wishlist_mst_OwnerCheck`, `customer_id` UQ filtered, `GuestTokenHash` UQ filtered | Implemented and verified |
| `drs_wishlist_item_mst` | Line items in wishlists | `ID` (BIGINT) | `wishlist_id`, `product_id`, `IsActive`, `IsDeleted` | `(wishlist_id, product_id)` UQ filtered, `wishlist_id` FK, `product_id` FK | Implemented and verified |
| `drs_cart_mst` | Customer active shopping carts | `ID` (BIGINT) | `customer_id`, `CartStatus`, `IsActive`, `IsDeleted` | `CK_drs_cart_mst_Status`, `customer_id` UQ filtered where `CartStatus = 'Active'` | Implemented and verified |
| `drs_cart_item_mst` | Line items in shopping carts | `ID` (BIGINT) | `cart_id`, `product_id`, `Quantity`, `IsActive`, `IsDeleted` | `CK_drs_cart_item_mst_Quantity (> 0)`, `(cart_id, product_id)` UQ filtered | Implemented and verified |
| `drs_contact_inquiry_mst` | Customer/visitor contact inquiries | `ID` (BIGINT) | `FullName`, `MobileNo`, `Email`, `CompanyName`, `ServiceDescription`, `customer_id`, `IpAddress`, `UserAgent` | `CK_drs_contact_inquiry_mst_ServiceDescLength`, `customer_id` FK | Implemented and verified |

### 9.3 Foreign Key Relationships

| Constraint Name | Source Table | Source Column | Target Table | Target Column | Purpose |
|---|---|---|---|---|---|
| `FK_drs_customer_address_mst_customer_id` | `drs_customer_address_mst` | `customer_id` | `drs_customer_mst` | `ID` | Links delivery address to customer profile |
| `FK_drs_product_mst_product_category_id` | `drs_product_mst` | `product_category_id` | `drs_product_category_mst` | `ID` | Assigns product to catalog category |
| `FK_drs_product_stock_mst_product_id` | `drs_product_stock_mst` | `product_id` | `drs_product_mst` | `ID` | Links inventory counter to specific product |
| `FK_drs_product_image_mst_product_id` | `drs_product_image_mst` | `product_id` | `drs_product_mst` | `ID` | Links gallery images to product |
| `FK_drs_refresh_token_mst_customer_id` | `drs_refresh_token_mst` | `customer_id` | `drs_customer_mst` | `ID` | Associates rotating refresh token with user |
| `FK_drs_login_history_mst_customer_id` | `drs_login_history_mst` | `customer_id` | `drs_customer_mst` | `ID` | Tracks authentication attempts per user |
| `FK_drs_audit_mst_customer_id` | `drs_audit_mst` | `customer_id` | `drs_customer_mst` | `ID` | Identifies actor executing entity modifications |
| `FK_drs_state_mst_country_id` | `drs_state_mst` | `country_id` | `drs_country_mst` | `ID` | Binds state/province to parent country |
| `FK_drs_customer_address_mst_country_id` | `drs_customer_address_mst` | `country_id` | `drs_country_mst` | `ID` | Links delivery address to country master |
| `FK_drs_customer_address_mst_state_id` | `drs_customer_address_mst` | `state_id` | `drs_state_mst` | `ID` | Links delivery address to state master |
| `FK_drs_commission_request_mst_category_id` | `drs_commission_request_mst` | `category_id` | `drs_commission_category_mst` | `ID` | Categorizes custom commission inquiry |
| `FK_drs_commission_request_mst_customer_id` | `drs_commission_request_mst` | `customer_id` | `drs_customer_mst` | `ID` | Optional link to registered customer |
| `FK_drs_commission_request_image_mst_request_id` | `drs_commission_request_image_mst` | `commission_request_id` | `drs_commission_request_mst` | `ID` | Attaches reference sketches to commission inquiry |
| `FK_drs_project_mst_customer_id` | `drs_project_mst` | `customer_id` | `drs_customer_mst` | `ID` | Optional link to administrator author |
| `FK_drs_project_image_mst_project_id` | `drs_project_image_mst` | `project_id` | `drs_project_mst` | `ID` | Binds gallery images to About narrative section |
| `FK_drs_wishlist_mst_customer_id` | `drs_wishlist_mst` | `customer_id` | `drs_customer_mst` | `ID` | Binds authenticated wishlist to customer |
| `FK_drs_wishlist_item_mst_wishlist_id` | `drs_wishlist_item_mst` | `wishlist_id` | `drs_wishlist_mst` | `ID` | Binds wishlist item to parent wishlist |
| `FK_drs_wishlist_item_mst_product_id` | `drs_wishlist_item_mst` | `product_id` | `drs_product_mst` | `ID` | Links product catalog item to wishlist line item |
| `FK_drs_cart_mst_customer_id` | `drs_cart_mst` | `customer_id` | `drs_customer_mst` | `ID` | Binds active shopping cart to customer |
| `FK_drs_cart_item_mst_cart_id` | `drs_cart_item_mst` | `cart_id` | `drs_cart_mst` | `ID` | Binds cart item to active cart |
| `FK_drs_cart_item_mst_product_id` | `drs_cart_item_mst` | `product_id` | `drs_product_mst` | `ID` | Links product catalog item to cart line item |
| `FK_drs_contact_inquiry_mst_customer_id` | `drs_contact_inquiry_mst` | `customer_id` | `drs_customer_mst` | `ID` | Optional link to registered customer inquiry |

### 9.4 Indexes and Performance Constraints

- **Permanent Unique Indexes**:
  - `UQ_drs_customer_mst_Account_no`: On `[Account_no]`.
  - `UQ_drs_product_mst_ProductCode`: On `[ProductCode]`.
  - `UQ_drs_commission_request_mst_RequestNo`: On `[RequestNo]`.
- **Filtered Unique Indexes (`WHERE IsDeleted = 0`)**:
  - `UQ_drs_customer_mst_Email`, `UQ_drs_customer_mst_MobileNo`.
  - `UQ_drs_product_category_mst_CategoryName`.
  - `UQ_drs_product_mst_SKU`.
  - `UQ_drs_product_stock_mst_product_id`.
  - `UQ_drs_error_message_mst_MessageCode`.
  - `UQ_drs_country_mst_CountryName`, `UQ_drs_country_mst_CountryCode`.
  - `UQ_drs_state_mst_Country_StateName` on `([country_id], [StateName])`.
  - `UQ_drs_commission_category_mst_CategoryName`.
- **Covering & Filtering Indexes**:
  - `IX_drs_commission_request_mst_Status_CreatedDate`: On `([Status], [CreatedDate] DESC)` for instant dashboard sorting.
  - `IX_drs_project_mst_PublicList`: Filtered index `WHERE IsDeleted = 0 AND IsActive = 1 AND IsPublished = 1` covering `Heading`, `Subheading`, `ContentType`, `ProjectOrEventTitle`, `DisplayYear`, `ShortDescription`, `DetailedDescription`, `Credits`, `Location`, `ExternalUrl`, and `[LayoutType]` for high-speed public page rendering.
- **Cart & Wishlist Unique & Filtered Indexes (`WHERE IsDeleted = 0`)**:
  - `UQ_drs_wishlist_mst_Customer`: On `[customer_id]` filtered `WHERE customer_id IS NOT NULL AND IsDeleted = 0`.
  - `UQ_drs_wishlist_mst_GuestToken`: On `[GuestTokenHash]` filtered `WHERE GuestTokenHash IS NOT NULL AND IsDeleted = 0`.
  - `UQ_drs_wishlist_item_mst_Wishlist_Product`: On `([wishlist_id], [product_id])` filtered `WHERE IsDeleted = 0`.
  - `UQ_drs_cart_mst_ActiveCustomerCart`: On `[customer_id]` filtered `WHERE CartStatus = 'Active' AND IsDeleted = 0`.
  - `UQ_drs_cart_item_mst_Cart_Product`: On `([cart_id], [product_id])` filtered `WHERE IsDeleted = 0`.
- **Contact Inquiry Indexes (`WHERE IsDeleted = 0`)**:
  - `IX_drs_contact_inquiry_mst_CreatedDate`: On `[CreatedDate] DESC` for chronological admin inquiry listing.
  - `IX_drs_contact_inquiry_mst_Email`: On `[Email]` for rapid sender lookup and duplicate inquiry tracking.

### 9.5 Database Sequences and Seed Data

#### Sequences
1. `Seq_CustomerAccount`: Auto-generates numeric values for formatted customer account numbers (`DRC000001`, `DRC000002`, ...).
2. `Seq_ProductCode`: Auto-generates numeric values for formatted product codes (`DRP000001`, `DRP000002`, ...).
3. `Seq_CommissionRequestNo`: Auto-generates values for inquiry tracking codes (`COMM-yyyyMMdd-0001`, ...).

#### Seed Data (`ProductScript-SeedData.sql`)
- **System Error and Status Messages**: 37 core messages and 12 About messages with standardized HTTP status codes.
- **Geographic Masters**:
  - 48 sovereign nations seeded in `drs_country_mst` (India, USA, UK, UAE, Japan, Germany, France, etc.).
  - 36 Indian States and Union Territories seeded in `drs_state_mst` with official postal abbreviations.
- **Commission Project Categories**:
  1. *Art Installations* (Architectural & site-specific installations).
  2. *Sculptures* (3D bespoke works in metal, stone, wood).
  3. *Wall Murals* (Handcrafted wall artwork).
  4. *Furniture* (Bespoke studio furniture).
  5. *Event Decor* (Curated styling for premium events).
  6. *Lighting* (Handcrafted sculptural lighting & lamps).
  7. *Wearables* (Limited-edition textiles & wearable art).
  8. *Others* (Specialized commissioned creations).
- **Default Studio Narrative Seed**:
  - Idempotent seed record inserted into `drs_project_mst` establishing the foundational Khel-Akhel studio philosophy (*"All that comes, comes in play. All that flows, flows. All that goes, goes. All is One."*), with `ContentType = 'Philosophy'`, `DisplayYear = '2026'`, `Credits = NULL`, `LayoutType = 'editorial'`, `DisplayOrder = 1`, and `IsPublished = 1`.

### 9.6 Entity-Relationship Diagram

```mermaid
erDiagram
    drs_customer_mst ||--o{ drs_customer_address_mst : "has addresses"
    drs_customer_mst ||--o{ drs_refresh_token_mst : "holds tokens"
    drs_customer_mst ||--o{ drs_login_history_mst : "logs authentication"
    drs_customer_mst ||--o{ drs_audit_mst : "triggers audit events"
    drs_customer_mst ||--o{ drs_commission_request_mst : "submits inquiries (optional)"
    drs_customer_mst ||--o{ drs_project_mst : "authors story (optional)"

    drs_country_mst ||--o{ drs_state_mst : "contains states"
    drs_country_mst ||--o{ drs_customer_address_mst : "referenced by"
    drs_state_mst ||--o{ drs_customer_address_mst : "referenced by"

    drs_product_category_mst ||--o{ drs_product_mst : "categorizes"
    drs_product_mst ||--|| drs_product_stock_mst : "has stock"
    drs_product_mst ||--o{ drs_product_image_mst : "has gallery images"

    drs_commission_category_mst ||--o{ drs_commission_request_mst : "classifies"
    drs_commission_request_mst ||--o{ drs_commission_request_image_mst : "includes reference images"

    drs_project_mst ||--o{ drs_project_image_mst : "includes gallery images"

    drs_customer_mst ||--o| drs_cart_mst : "owns active cart"
    drs_cart_mst ||--o{ drs_cart_item_mst : "contains items"
    drs_product_mst ||--o{ drs_cart_item_mst : "referenced in cart"

    drs_customer_mst ||--o| drs_wishlist_mst : "owns wishlist (optional)"
    drs_wishlist_mst ||--o{ drs_wishlist_item_mst : "contains items"
    drs_product_mst ||--o{ drs_wishlist_item_mst : "referenced in wishlist"

    drs_customer_mst ||--o{ drs_contact_inquiry_mst : "submits inquiries (optional)"

    drs_customer_mst {
        bigint ID PK
        varchar Account_no UK
        nvarchar Email UK
        nvarchar MobileNo UK
        nvarchar PasswordHash
        bit IsAdmin
        bit IsActive
    }

    drs_product_mst {
        bigint ID PK
        bigint product_category_id FK
        nvarchar ProductName
        varchar ProductCode UK
        varchar SKU UK
        decimal MRP
        decimal SellingPrice
        bit IsActive
    }

    drs_commission_request_mst {
        bigint ID PK
        varchar RequestNo UK
        nvarchar FullName
        nvarchar Email
        nvarchar MobileNo
        bigint category_id FK
        date ExpectedTimeline
        nvarchar ProjectLocation
        nvarchar ProjectDescription
        nvarchar Status
    }

    drs_project_mst {
        bigint ID PK
        nvarchar Heading
        nvarchar Subheading
        nvarchar ContentType
        nvarchar ProjectOrEventTitle
        nvarchar DisplayYear
        nvarchar ShortDescription
        nvarchar DetailedDescription
        nvarchar Credits
        nvarchar Location
        nvarchar ExternalUrl
        nvarchar LayoutType
        int DisplayOrder
        bit IsPublished
    }

    drs_project_image_mst {
        bigint ID PK
        bigint project_id FK
        nvarchar OriginalFileName
        nvarchar StoredFileName
        nvarchar ImagePath
        nvarchar ContentType
        bigint FileSize
        int DisplayOrder
    }
```

---

## 10. Authentication, Authorization, and Security Architecture

### 10.1 Multi-Layered Authentication Architecture
1. **Access Tokens (JWT)**:
   - Stateless JWT tokens signed with HMAC-SHA256 using `Jwt:Key` (symmetric minimum 256-bit key).
   - Validated at application startup via `StartupSecurityValidator` to ensure keys are at least 32 UTF-8 bytes and never match historical compromised git values.
   - Claims: `sub` (UserId), `UserId`, `unique_name` (Email), `email`, `role` (`Admin` or `Customer`), and `jti` (GUID).
   - Lifetime: 60 minutes (`Jwt:ExpiryMinutes`).
   - Storage: Held strictly in Angular client memory (`AuthService.currentUser` signal). **Never persisted in `localStorage` or `sessionStorage`**, eliminating persistent XSS token theft vectors.

2. **Refresh Tokens, Atomic Rotation & Concurrency**:
   - 64 cryptographically secure random bytes generated via `RandomNumberGenerator.GetBytes(64)`.
   - Stored in `drs_refresh_token_mst` as a SHA-256 hash with 7 days expiration.
   - Delivered to the browser as an `HttpOnly`, `SameSite=None` (or Lax/Strict), `Secure` cookie named `drs_refresh` with path `/api/auth`.
   - **Atomic Single-Transaction Claim**: Rotation runs in a single `SqlTransaction` with an atomic `UPDATE drs_refresh_token_mst ... OUTPUT inserted.customer_id ... WHERE TokenHash = @TokenHash AND RevokedUtc IS NULL AND IsActive = 1 AND IsDeleted = 0 AND ExpiresUtc > GETUTCDATE()`.
   - **Grace Period & Parallel Race Handling**: If multiple concurrent requests present the same token during legitimate tab refreshes, requests within `Security:RefreshReuseGraceSeconds` (default 30s) where `ReplacedByTokenHash` is set receive HTTP 409 `REFRESH_IN_PROGRESS` without cookie invalidation. The Angular HTTP interceptor (`auth.interceptor.ts`) retries once after 300ms.
   - **Breach Containment & Family Revocation**: If a revoked token is presented outside the grace period or without a valid replacement chain, all refresh tokens for that customer are immediately revoked (`RevokeAllAsync`) and a security alert is recorded in `drs_audit_mst`.

3. **SkiaSharp Visual CAPTCHA**:
   - Generated server-side using `SkiaSharp` and `SkiaSharp.NativeAssets.Linux.NoDependencies`.
   - Draws 5 characters chosen from a reduced, unambiguous alphabet (`346789ABCDEFGHJKMNPQRTUVWXY`) with random per-glyph rotation (-20° to +20°), random vertical offsets, noise dots, and Bézier curves.
   - Encoded as a PNG image and returned as `{ CaptchaId, CaptchaImage }` (base64 data URI). The plain-text answer is **never** sent to the client.
   - The SHA-256 hash of the answer is stored in `IMemoryCache` for 5 minutes and is strictly single-use (`_cache.Remove` upon verification).
   - Timing-safe comparison using `CryptographicOperations.FixedTimeEquals`.
   - Mandatory on `POST login`, `POST forgot-password`, `POST commissions/create`, and anonymous `POST customer/create`.

4. **Timing-Invariant Login & Source-Scoped Throttling**:
   - User lookups executed without dirty read hints (`WITH (NOLOCK)` removed).
   - If a username does not exist, a constant-time dummy BCrypt cost 12 verification is performed against a pre-computed hash, preventing timing-based account enumeration.
   - **Source-Scoped Throttling**: Queries `drs_login_history_mst` for failed attempts by the specific (customer, IP) pair over the last 15 minutes. If >= 5, writes a `THROTTLED` failure row and returns generic 401 `INVALID_CREDENTIALS` without locking the account for other IPs.
   - Generic 401 `INVALID_CREDENTIALS` is returned identically for missing users and incorrect passwords.
   - Account disabled (`IsActive == 0`) and temporary lock status are checked only **after** the password has been verified.
   - Uniform CAPTCHA requirement across customer and admin logins removes information leakage about admin roles.

5. **Studio Admin-Assisted Password Recovery**:
   - To eliminate dependency on third-party outbound messaging channels, customer password recovery is handled directly by studio operators.
   - Customer submits forgot-password request on `/forgot-password`, solving CAPTCHA.
   - API returns uniform 200 `PASSWORD_RESET_REQUESTED` regardless of whether the email exists or belongs to an admin (admins cannot self-serve and their requests are ignored). Capped at 3 pending requests per customer per 24 hours.
   - Studio administrators review requests on `/admin/password-reset-requests`, verify customer identity out-of-band (phone/in-person), record mandatory notes, and generate a single-use reset URL expiring in 24 hours (`Security:PasswordResetLinkHours`).
   - Token is consumed via `POST api/auth/reset-password`: updates the customer's password with BCrypt cost 12, marks the request `COMPLETED`, clears failed login counters, and revokes all active refresh tokens. Second attempts with the same token fail immediately.

### 10.2 Partitioned Rate Limiting Architecture
All rate limiting is partitioned by client IP (`RateLimitPartition.GetFixedWindowLimiter` keyed on `RemoteIpAddress`). Production proxy traversal is supported via `ForwardedHeadersOptions` restricted to explicit trusted proxies configured in `Security:KnownProxies`.

| Policy | Applies To | Default Limit | Behavior on Limit |
|---|---|---|---|
| `auth-login` | `POST login`, `POST forgot-password`, `POST reset-password` | 10 / minute, 0 queue | 429 `RATE_LIMIT_EXCEEDED` + `Retry-After` header |
| `auth-session` | `GET captcha`, `POST refresh`, `POST logout`, `PATCH change-password` | 60 / minute, 0 queue | 429 `RATE_LIMIT_EXCEEDED` + `Retry-After` header |
| `public-form` | `POST commissions/create`, `POST customer/create`, `POST contact-us` | 5 / 10 minutes, 0 queue | 429 `RATE_LIMIT_EXCEEDED` + `Retry-After` header |
| Global Limiter | Every request across the application | 300 / minute, 0 queue | 429 `RATE_LIMIT_EXCEEDED` + `Retry-After` header |

Rejection responses execute an `OnRejected` callback returning a standard `ApiResponse` envelope (`StatusCode = 429`, `Message = "RATE_LIMIT_EXCEEDED"`).

### 10.3 Role-Based Access Control (RBAC) & Route Protection
- **Backend API**: Administrative endpoints decorated with `[Authorize(Roles = "Admin")]` enforce strict JWT claims validation. Anonymous endpoints (`api/commissions/create`, `api/customer/create`, `api/about/public`, `api/faq/public`) are decorated with `[AllowAnonymous]`.
- **Admin Customer Creation**: In `CustomerController.Create`, if the caller is an authenticated administrator (`User.IsInRole("Admin")`), CAPTCHA is bypassed, allowing manual back-office customer entry.
- **Frontend Route Protection**:
  - `adminGuard`: Inspects `AuthService.isAdmin()`. If session is missing, triggers silent background token refresh via `auth/refresh` before redirecting to `/admin/login`.
  - `userGuard`: Ensures customer authentication before account actions.
  - `authInterceptor`: Intercepts outgoing HTTP requests, appends `Authorization: Bearer <token>`, queues requests during refresh, and retries 409 `REFRESH_IN_PROGRESS` responses.

### 10.4 Application Security Hardening & Form Defense
- **SQL Injection Prevention**: All data access is parameterized using ADO.NET `SqlParameter`. Application queries cannot contain dangerous DDL/administrative commands checked by `DbHelper.ValidateQuerySafety`.
- **Honeypot Form Protection**: Public forms (`commissions/create`, `customer/create`, `contact-us`) include a hidden `Website` field. Automated bots filling this field receive a silent 200 response while the submission is dropped without database insertion, accompanied by an audit warning.
- **File Upload Limits & MIME Magic Bytes**:
  - `POST api/commissions/create` enforces `[RequestSizeLimit(16 * 1024 * 1024)]` and `[RequestFormLimits(MultipartBodyLengthLimit = 16 * 1024 * 1024)]`.
  - Over-limit uploads caught by `InvalidModelStateResponseFactory` and custom middleware return standard `ApiResponse` (Status 413, `PAYLOAD_TOO_LARGE`).
  - `FileValidationHelper` verifies image headers against magic bytes for JPEG (`FF D8 FF`), PNG (`89 50 4E 47`), and WEBP (`RIFF....WEBP`).
  - Compensating disk cleanup deletes any newly written image files if subsequent database operations fail.
- **BCrypt Cost 12 Enforced**: All passwords hashed using `BCrypt.Net-Next` with explicit work factor 12. Plain-text fallback matching is completely removed.
- **CLI Password Management**: `dotnet run -- set-password --email <email>` allows secure terminal-based admin credential initialization with masked input and session revocation.

---

## 11. Commissioned Projects / Custom Project Requests Module

### 11.1 Business Workflow
The Commissioned Projects module enables patrons, architects, and corporate clients to submit bespoke design orders:
1. **Inquiry Submission**: Client fills the structured form specifying contact details, category, required completion date, location, keywords, a detailed description (up to 500 words), and attaches up to 3 sketches/reference images (maximum 5MB each).
2. **Persistence & Sequencing**: `commissionsController.CreateCommissionRequest` validates inputs, generates a unique sequence `RequestNo` (`COMM-yyyyMMdd-XXXX`), stores the inquiry in `drs_commission_request_mst`, and saves the uploaded images to `wwwroot/uploads/commissions/{year}/{month}/`.
3. **Administrative Triage**: Authorized administrators view the inquiry on `/admin/commissioned-projects`.
4. **Inspection & Lightbox Preview**: Curators inspect the project brief, word count, location, timeline, and open high-resolution image previews in a full-screen lightbox.
5. **Status Lifecycle Updates**: Administrators update inquiry progression (`Submitted` $\rightarrow$ `Under Review` $\rightarrow$ `Contacted` $\rightarrow$ `In Progress` $\rightarrow$ `Completed` / `Rejected` / `Cancelled`) and append internal administrative notes.

### 11.2 Verification and Form Constraints

| Field | Source Type | Constraints | Backend Validation |
|---|---|---|---|
| **FullName** | Text | Required, 2-150 characters | Letters, spaces, hyphens only |
| **MobileNo** | Text | Required, 7-20 characters | Standard phone format |
| **Email** | Text | Required, max 255 characters | RFC-compliant email regex |
| **CategoryId** | Dropdown | Required, must exist in `drs_commission_category_mst` | Verified via foreign key / DB check |
| **OtherCategoryDescription** | Text | Required only if Category is "Others" | Max 255 characters |
| **ExpectedTimeline** | Date Picker | Required, valid future date | `ExpectedTimeline > DateTime.UtcNow.Date` |
| **ProjectLocation** | Text | Required, 2-255 characters | Free text location / address |
| **ProjectDescription** | Textarea | Required, max 500 words ($\le 5000$ characters) | Word count algorithm using whitespace delimiters |
| **Keywords** | Text | Optional, max 500 characters | Comma-delimited descriptive tags |
| **Reference Images** | File Input | Optional, maximum 3 images, max 5MB per file | Extensions `.jpg`, `.jpeg`, `.png`, `.webp` + magic bytes |

### 11.3 Implementation Status
- **Backend API (`commissionsController.cs`)**: **Implemented and verified**. All 5 endpoints (`categories`, `create`, `list`, `{id}`, `update-status`) are fully operational.
- **Admin UI (`CommissionedProjectsComponent`)**: **Implemented and verified**. Table, search debounce, category filter, status filter, date range filter, detail modal, image lightbox, status update modal, and confirmation dialogs are complete.
- **Database Tables**: **Implemented and verified**. `drs_commission_category_mst`, `drs_commission_request_mst`, `drs_commission_request_image_mst`, and `Seq_CommissionRequestNo` are defined with constraints and indexes.
- **Storefront Client Form**: **Backend only / Planned frontend**. The customer-facing submission form has not yet been built into the storefront UI.

---

## 12. Dynamic Projects & Visual Gallery Layout Builder Module (with Independent About Us Page)

### 12.1 Business Workflow & Editorial Narrative Architecture
The Projects module manages dynamic portfolio works, exhibitions, festival installations, and creative practice sections for **DARAS**. The genuine About Us page (`/about`) remains completely independent, presenting the studio philosophy, manifesto, and 12 disciplines. Curators and administrators can create, update, reorder, and publish independent project, event, artwork, and philosophy sections vertically without requiring code modifications:
1. **Administrative Creation**: Curators navigate to `/admin/projects/create` to draft a new section.
2. **Metadata & Content Attributes**: Curators specify Heading, Subheading, Content Discipline / Category (free-form text input entered directly by the administrator without a dropdown), Project/Event Title, Display Year, Short Description, Detailed Description, Credits (optional project attribution), Location, External Link, and Display Order.
3. **Visual Gallery Layout Selection**: Curators select from 7 custom gallery layout wireframes that best match the visual rhythm of the assets.
4. **Interactive Asset Upload & Reordering**: Up to 15 high-resolution images can be uploaded via drag-and-drop. Curators can preview, reorder via native drag-and-drop or step-buttons, and stage removals before saving.
5. **Real-Time Live Preview**: The admin form includes an instantaneous client-side preview rendering the exact public layout with all text, badges, and the interactive lightbox modal before committing changes.
6. **Publication & Storefront Rendering**: Curators toggle `IsPublished`. Published entries are delivered via `GET api/projects/public` and rendered chronologically by `DisplayOrder` on the customer-facing `/projects` storefront page.

### 12.2 Visual Gallery Layout Presets
The system provides 7 layout presets (`LayoutType`) with custom wireframe representations and tailored CSS grid/flex rules:

| Preset Identifier | Display Name | Visual Rhythm & Architecture | Ideal Content Type |
|---|---|---|---|
| `grid-3` | 3-Column Grid | Balanced, uniform three-column responsive grid with aspect ratio `4/3`. Auto-adapts from 3 columns on desktop to 2 on tablet and 1 on mobile. | Fine-art print series, postcards, multi-object catalogs. |
| `grid-2` | 2-Column Showcase | Clean, high-impact two-column layout giving larger visual weight to each image (`16/10` ratio). | Paired architectural views, before/after craft processes, diptychs. |
| `editorial` | Editorial Magazine | Sophisticated asymmetric magazine spread: a dominant 2/3 featured visual on the left flanked by 2 vertically stacked complementary images on the right, followed by a balanced 3-column baseline. | Curated editorial stories, exhibition highlights, design narratives. |
| `featured` | Featured Hero + Grid | Full-width commanding hero banner (`21/9` or `16/9`) on top, followed by a compact multi-column thumbnail grid underneath. | Major festival scenography, flagship installations, milestone exhibitions. |
| `full-width` | Full Width Immersion | Single-column stacked cinematic images with generous vertical spacing, maximizing horizontal viewport width. | High-detail panoramic murals, spatial installations, documentary spreads. |
| `masonry` | Dynamic Masonry | Staggered multi-column layout (`column-count: 3`) preserving original image aspect ratios without cropping. | Mixed-orientation studio archives, sketchbooks, process snapshots. |
| `asymmetric` | Asymmetric Art Mix | Dynamic alternating grid rhythm (`2fr 1fr` alternating with `1fr 2fr` per row) creating visual interest and tension. | Avant-garde sculpture collections, mixed-medium artistic explorations. |

### 12.3 Image Gallery Editor & Constraints
The `ImageGalleryEditorComponent` (`src/app/admin/pages/about/image-gallery-editor/`) enforces strict validation and smooth interactions:
- **Image Limits**: Maximum of 15 images per section.
- **File Size & Validation**: Maximum 5MB per image. Validated against allowed MIME types (`image/jpeg`, `image/png`, `image/webp`) and verified on the server via `FileValidationHelper` binary magic bytes.
- **Drag-and-Drop Dropzone**: Visual dashed upload target supporting file drag-and-drop and manual file picker activation.
- **Capacity Indicator**: Dynamic counter badge (`X / 15 images`) with warning states when nearing or hitting capacity.
- **Reordering Interface**: Native HTML5 drag-and-drop reordering between thumbnail cards, supplemented by accessible "Move Earlier" ($\leftarrow$) and "Move Later" ($\rightarrow$) controls.
- **Staged Removals**: Images marked for removal in edit mode are tracked in `removeImageIds` and committed atomically upon form submission; newly uploaded files can be canceled before upload.

### 12.4 Real-Time Live Preview & Standalone Gallery Renderer
- **`GalleryRendererComponent` (`src/app/common/components/gallery-renderer/`)**:
  - Reusable standalone component accepting `images: ProjectImageDto[]` and `layout: GalleryLayoutType`.
  - Dynamically activates layout-specific CSS classes (`layout-grid-3`, `layout-editorial`, etc.) with responsive breakpoints.
  - Built-in full-screen interactive lightbox modal:
    - High-resolution active image presentation with smooth fade-in transitions.
    - Image counter (`Image X of Y`), title, and caption display.
    - Previous ($\leftarrow$) and Next ($\rightarrow$) navigation arrows.
    - Interactive thumbnail strip for quick jumping between assets.
    - Complete keyboard accessibility (`Escape` to close, `ArrowLeft` for previous, `ArrowRight` for next).
    - Backdrop click-to-close with event propagation shielding.
- **Live Preview Tab**: Integrated directly inside `ProjectFormComponent`. Curators can toggle between the "Editor" and "Live Preview" tabs without saving or publishing to verify layout rhythm, typography, and image proportions.

### 12.5 Public Storefront Presentation, Lightbox, & Disciplines Grid
The customer-facing `/projects` route (`src/app/user/pages/projects/`) renders the dynamic portfolio showcase, while `/about` (`src/app/user/pages/about/`) renders the independent studio manifesto and disciplines:
1. **Hero Manifesto**: Typographic hero highlighting the studio's core philosophical premise:
   > *"All that comes, comes in play. All that flows, flows. All that goes, goes. All is One."*
2. **Dynamic Section Flow**: Iterates through active published sections retrieved from `GET api/projects/public`:
   - Renders section metadata badge (`ContentType`, `DisplayYear`), primary `Heading`, and `Subheading`.
   - Embeds `GalleryRendererComponent` executing the chosen layout preset and interactive lightbox.
   - Editorial text columns presenting `ShortDescription`, `DetailedDescription`, `Location`, and external reference links.
   - Formal artistic and collaborative `Credits` block (optional attribution; rendered only when specific external collaborators or photographers are specified, and suppressed when empty).
3. **The Twelve DARAS Studio Disciplines**: Comprehensive interactive reference grid detailing disciplines 01 through 12 (Visual Art, Architecture, Material Objects, Cultural Programming, Media, Play & Learning, Research, Ecology, Creative Direction, Publishing, Commercial Licensing, Intellectual Property).
4. **Bespoke Commission Callout**: Editorial banner inviting patrons and architects to initiate custom commissioned projects.

### 12.6 Database Schema & Backend Persistence
- **Table Extension**: Added `[LayoutType] NVARCHAR (50) DEFAULT 'grid-3' NOT NULL` to `[dbo].[drs_project_mst]`.
- **Constraint Enforcement**: Added `CK_drs_project_mst_LayoutType` check constraint ensuring `[LayoutType] IN ('grid-3', 'grid-2', 'editorial', 'featured', 'full-width', 'masonry', 'asymmetric')`.
- **Backend Controller Allowlist**: `ProjectsController.AllowedLayouts` validates incoming layout values on `api/projects/create` and `api/projects/update`, returning HTTP 400 Bad Request with a localized error message if invalid.
- **Optimized Covering Index**: `IX_drs_project_mst_PublicList` covers `[LayoutType]` in addition to heading, narrative text, year, credits, and external URL fields.
- **Audit Logging**: All layout transitions, section updates, image reorderings, and publish toggles are logged to `drs_audit_mst`.

---

## 13. Production Cart and Wishlist Subsystem

### 13.1 Cart Subsystem Overview
The Cart module provides an authenticated shopping cart architecture designed for data integrity, real-time inventory validation, and concurrency safety:
- **Database Tables**:
  - `drs_cart_mst`: Master cart container bound to `customer_id` with `CartStatus` (`Active`, `ConvertedToOrder`, `Abandoned`) and filtered unique index `UQ_drs_cart_mst_ActiveCustomerCart` (`WHERE CartStatus = 'Active' AND IsDeleted = 0`) ensuring only one active cart exists per customer.
  - `drs_cart_item_mst`: Line items linked to `cart_id` and `product_id` with quantity check constraint `CK_drs_cart_item_mst_Quantity (> 0)` and uniqueness constraint `UQ_drs_cart_item_mst_Cart_Product` preventing duplicate lines.
- **Business Operations & Concurrency Control (`CartService.cs` & `CartController.cs`)**:
  - **Live Inventory Check**: Incremental additions validate that the *resulting total quantity* does not exceed `AvailableQty` in `drs_product_stock_mst`.
  - **Atomic Upsert**: Adding an existing product gracefully increments its quantity in place rather than creating duplicate lines.
  - **Concurrency Handling**: Handles potential race conditions with unique constraint violation retry logic.
  - **Dynamic Totals**: Every cart operation returns calculated item subtotals (`SellingPrice * Quantity`), overall cart subtotal, item count, and active stock flags.
  - **Endpoints**: `POST api/cart/items`, `GET api/cart`, `PATCH api/cart/items/{id}`, `DELETE api/cart/items/{id}`, `DELETE api/cart/items`.

### 13.2 Wishlist Subsystem Overview
The Wishlist module provides dual-mode support for registered customers and anonymous guests:
- **Database Tables**:
  - `drs_wishlist_mst`: Container table supporting either `customer_id` or `GuestTokenHash` via check constraint `CK_drs_wishlist_mst_OwnerCheck`.
  - `drs_wishlist_item_mst`: Line items linked to `wishlist_id` and `product_id` with filtered unique index `UQ_drs_wishlist_item_mst_Wishlist_Product`.
- **Security & Session Handling (`WishlistService.cs` & `WishlistController.cs`)**:
  - **Guest Token Handling**: Anonymous guests receive a cryptographically random token stored in an `HttpOnly`, `SameSite=Lax` cookie and optional fallback header `X-Guest-Token`. Only the SHA-256 hash (`GuestTokenHash`) is stored in the database.
  - **Ownership Verification**: Every read and mutation strictly verifies customer claim identity or guest token hash ownership. Raw tokens are never logged.
  - **Login Migration (`POST api/wishlist/merge`)**: Upon customer login, any active guest wishlist is automatically merged into the customer's permanent account, deduplicating items atomically.
  - **Move-to-Cart Conversion (`POST api/wishlist/items/{id}/move-to-cart`)**: Atomically adds the desired product to the customer's active cart (with stock validation) and removes it from the wishlist in a single transactional step.
  - **Endpoints**: `POST api/wishlist/items`, `GET api/wishlist`, `DELETE api/wishlist/items/{id}`, `DELETE api/wishlist/items`, `POST api/wishlist/merge`, `POST api/wishlist/items/{id}/move-to-cart`.

---

## 14. Contact Us & Inquiries Subsystem

### 14.1 Architecture & Simplification
The Contact Us API provides a high-throughput, production-conscious endpoint for website visitors and potential corporate clients to submit inquiries about custom projects, bespoke art commissions, or studio consultations:
- **Direct Controller Pattern**: Implemented directly in `ContactUsController.cs` following the project-standard lightweight pattern (analogous to `UserController.cs`) without unnecessary service abstraction overhead.
- **Database Persistence**:
  - `drs_contact_inquiry_mst`: Captures `FullName` (`NVARCHAR(150)`), `MobileNo` (`NVARCHAR(20)`), `Email` (`NVARCHAR(254)`), `CompanyName` (`NVARCHAR(200)`, optional), and `ServiceDescription` (`NVARCHAR(300)`).
  - Obsolete `InquiryStatus` was cleanly removed from both database schema and request models to keep the module focused and unencumbered.
  - Indexes: `IX_drs_contact_inquiry_mst_CreatedDate` for chronological listing and `IX_drs_contact_inquiry_mst_Email` for duplicate detection.
- **Validation & Security**:
  - Enforces `[Required]`, string length bounds, and email regex patterns.
  - Rate-limited under `GeneralPolicy` to prevent spam submissions.
  - Captures client IP address and User-Agent headers, with full audit logging in `drs_audit_mst`.
  - Endpoint: `POST api/contact-us`.

---

## 15. Dynamic FAQ Management Subsystem (Admin & Public Storefront)

### 15.1 Architecture & End-to-End Integration
The FAQ subsystem connects the backend `FAQController.cs` and `drs_faq_mst` database table to both an administrative management portal and a customer-facing public knowledge base:
- **Backend Infrastructure (`FAQController.cs`)**:
  - Exposes 6 endpoints: `POST api/faq/create`, `GET api/faq/admin/list`, `GET api/faq/admin/{id}`, `PATCH api/faq/update`, `DELETE api/faq/delete/{id}`, and `GET api/faq/public?location={loc}`.
  - Supports 5 contextual placement flags: `DisplayOnHomePage`, `DisplayOnFaqPage`, `DisplayOnCheckoutPage`, `DisplayOnContactPage`, and `DisplayOnFooter`.
- **Admin Management Portal (`/admin/faqs`)**:
  - **Location**: `src/app/admin/pages/faqs/` (`faqs.ts`, `faqs.html`, `faqs.scss`).
  - **Features**: Breadcrumbs (`Home > FAQs`), debounced search (350ms), category filter dropdown, active and published status dropdowns, placement location filter, and reset button.
  - **Interactivity**: Inline status and publish toggle buttons on table rows with instant visual feedback and audit log persistence.
  - **Creation & Edit Modals**: Complete reactive form with `CustomValidators` checking question length (3–500 chars), non-negative display order, datalist category autocomplete, and location placement checkboxes.
  - **Deletion**: Integrated with `ConfirmationDialogComponent` for safe soft-deletion.
  - **Navigation**: Dedicated sidebar menu link with SVG question icon in `admin-layout.html`.
- **Public Customer Knowledge Base (`/faq`)**:
  - **Location**: `src/app/user/pages/faq/` (`faq.ts`, `faq.html`, `faq.scss`).
  - **Features**: Hero header, interactive category filter chips, real-time keyword search, smooth expandable accordion items with rotated chevrons, empty state with filter reset, error retry alerts, and a studio contact CTA card linking to `/about`.
  - **Endpoint**: Consumes `GET api/faq/public?location=faq`.

---

## 16. Current Implementation Status Matrix

| Module / Capability | Frontend UI | Backend API | Database Objects | End-to-End Integration | Overall Status |
|---|---|---|---|---|---|
| **Admin Authentication & Captcha** | Implemented (`/admin/login`) | Implemented (`api/auth/*`) | Implemented (`drs_customer_mst`, `drs_refresh_token_mst`) | Verified | **Implemented and verified** |
| **Customer Authentication** | Implemented (`/login`, `/register`) | Implemented (`api/auth/*`, `api/customer/create`) | Implemented (`drs_customer_mst`) | Verified | **Implemented and verified** |
| **Customer Profile Management** | Implemented (`/admin/customers/*`) | Implemented (`api/customer/*`) | Implemented (`drs_customer_mst`, `Seq_CustomerAccount`) | Partially Verified (`toggle-active` 404) | **Partially implemented** |
| **Customer Delivery Addresses** | Implemented (`/admin/customer-addresses`) | Implemented (`api/customer-address/*`) | Implemented (`drs_customer_address_mst`) | Partially Verified (`toggle-active` 404) | **Partially implemented** |
| **Geographic Location Masters** | Consumed in address form | Implemented (`api/location/*`) | Implemented (`drs_country_mst`, `drs_state_mst`) | Verified | **Implemented and verified** |
| **Product Categories** | Implemented (`/admin/product-categories/*`) | Implemented (`api/product-category/*`) | Implemented (`drs_product_category_mst`) | Verified | **Implemented and verified** |
| **Product Catalog Management** | Implemented (`/admin/products/*`) | Implemented (`api/product/*`) | Implemented (`drs_product_mst`, `Seq_ProductCode`) | Verified | **Implemented and verified** |
| **Product Gallery Images** | Implemented (`/admin/products/:id`) | Implemented (`api/product-image/*`) | Implemented (`drs_product_image_mst`) | Verified | **Implemented and verified** |
| **Product Inventory & Stock** | Implemented (`/admin/product-stock/*`) | Implemented (`api/product-stock/*`) | Implemented (`drs_product_stock_mst`) | Partially Verified (`getById` 404) | **Partially implemented** |
| **Admin Operational Dashboard** | Implemented (`/admin/dashboard`) | Multi-service client `forkJoin` | Backed by entity tables | Verified | **Implemented and verified** |
| **Commissioned Projects (Admin)** | Implemented (`/admin/commissioned-projects`) | Implemented (`api/commissions/*`) | Implemented (`drs_commission_*`, `Seq_CommissionRequestNo`) | Verified | **Implemented and verified** |
| **Commissioned Projects (Public Form)** | Not implemented | Implemented (`POST api/commissions/create`) | Implemented (`drs_commission_*`) | Not integrated | **Backend only** |
| **Dynamic Studio Story (About)** | Implemented (`/about`, `/admin/about`, `/admin/projects/create`, `/admin/about/edit/:id`) | Implemented (`api/about/*` with LayoutType) | Implemented (`drs_project_mst`, `drs_project_image_mst`) | Verified | **Implemented and verified** |
| **Knowledge Base (FAQs)** | Implemented (`/admin/faqs`, `/faq`) | Implemented (`api/faq/*`) | Implemented (`drs_faq_mst`) | Verified | **Implemented and verified** |
| **Contact Us Inquiries** | Cleaned shell (Backend ready) | Implemented (`api/contact-us`) | Implemented (`drs_contact_inquiry_mst`) | Verified | **Backend implemented & verified** |
| **Customer Shopping Cart** | Cleaned shell (Backend ready) | Implemented (`api/cart/*`) | Implemented (`drs_cart_mst`, `drs_cart_item_mst`) | Verified | **Backend implemented & verified** |
| **Customer Wishlist** | Cleaned shell (Backend ready) | Implemented (`api/wishlist/*`) | Implemented (`drs_wishlist_mst`, `drs_wishlist_item_mst`) | Verified | **Backend implemented & verified** |
| **Storefront Home & Landing** | Implemented (`/`) | Static presentation | N/A | Verified | **Implemented and verified** |
| **Storefront Product Catalog** | Implemented (`/products`) | Implemented (`api/product/search`) | Implemented (`drs_product_mst`) | Verified | **Implemented and verified** |
| **Storefront Product Detail View** | Cleaned shell | Implemented (`api/product/{id}`) | Implemented (`drs_product_mst`, `drs_product_image_mst`) | Planned UI binding | **Backend only** |
| **Order Placement & Checkout** | Cleaned shell (`/checkout`) | Planned | Planned (`drs_order_mst`) | None | **Planned / not implemented** |
| **Payment Gateway Integration** | None | None | None | None | **Planned / not implemented** |
| **Order Tracking & Invoices** | Unrouted stubs | None | None | None | **Planned / not implemented** |

---

## 17. Known Issues, Technical Debt, and Gaps

| ID | Issue Description | Affected File / Module | Impact | Suggested Resolution | Priority |
|---|---|---|---|---|---|
| **ISS-01** | **22 Storefront Pages Unrouted & Incomplete** | `src/app/app.routes.ts`, `src/app/user/pages/*` | Clicking header/footer links (`/shop`, `/contact`, `/wishlist`, `/cart`, `/press`, `/orders`, `/faq`, `/terms`) triggers 404 error page. *(Note: `/about` was successfully extracted, dynamically integrated with 7 gallery layouts and lightbox, and routed).* | Register remaining routes in `app.routes.ts` and replace `<p>... works!</p>` stubs with functional views. | **High** |
| **ISS-02** | **Unit Test Suite Fails on TestBed Initialization** | `src/app/*.spec.ts`, `vitest.config.ts` | All 19 Vitest unit tests fail with `Error: Need to call TestBed.initTestEnvironment() first`. | Add a test setup file initializing `BrowserDynamicTestingModule` and `platformBrowserDynamicTesting`. | **High** |
| **ISS-03** | **Missing Backend Endpoint: `product-stock/{id}`** | `Khel-Akhel-Server/Controllers/Product/ProductStockController.cs` | `AdminProductStockService.getById()` throws HTTP 404 if called by the frontend. | Add `[HttpGet("{encryptedStockId}")]` endpoint to `ProductStockController`. | **Medium** |
| **ISS-04** | **Missing Backend Endpoint: `customer/toggle-active`** | `Khel-Akhel-Server/Controllers/Customer/CustomerController.cs` | `AdminCustomerService.toggleActive()` throws HTTP 404. | Add `[HttpPatch("toggle-active/{encryptedId}")]` endpoint to `CustomerController`. | **Medium** |
| **ISS-05** | **Missing Backend Endpoint: `customer-address/toggle-active`** | `Khel-Akhel-Server/Controllers/Customer/CustomerAddressController.cs` | `AdminCustomerAddressService.toggleActive()` throws HTTP 404. | Add `[HttpPatch("toggle-active/{encryptedAddressId}")]` to address controller. | **Medium** |
| **ISS-06** | **Missing Backend Endpoint: `product-category/toggle-active`** | `Khel-Akhel-Server/Controllers/Product/ProductCategoryController.cs` | `AdminProductCategoryService.toggleActive()` throws HTTP 404. | Add `[HttpPatch("toggle-active/{encryptedCategoryId}")]` to category controller. | **Medium** |
| **ISS-07** | **Missing Backend Endpoint: `product/toggle-active`** | `Khel-Akhel-Server/Controllers/Product/ProductController.cs` | `AdminProductService.toggleActive()` throws HTTP 404. | Add `[HttpPatch("toggle-active/{encryptedProductId}")]` to product controller. | **Medium** |
| **ISS-08** | **Lack of Public Commission Inquiry Form** | Storefront UI (`src/app/user/pages/contact` or `/commissions`) | Customers cannot submit bespoke project requests from the website even though backend is 100% operational. | Build a reactive customer submission form consuming `POST api/commissions/create`. | **High** |
| **ISS-09** | **Cart and Wishlist Backend Completed (Order Engine Remaining)** | Backend & Database | Cart and Wishlist backends are 100% operational with concurrency safety and guest tokens. Checkout and payment orders remain to be implemented. | Design order schema (`drs_order_mst`, `drs_order_item_mst`), order controller, and payment integration. | **Medium** |
| **ISS-10** | **Admin Sidebar Orders Link Targets Dashboard** | `src/app/admin/layouts/admin-layout/admin-layout.html` (L239) | Clicking "Orders" in the admin sidebar redirects to `/admin/dashboard`. | Implement dedicated `/admin/orders` page once order APIs exist. | **Low** |

---

## 18. Prioritized Completion Roadmap

### Phase 1: Critical Stability & API Parity (Immediate)
1. **Unit Test Configuration Fix**: Initialize `TestBed.initTestEnvironment()` in Vitest configuration to achieve a passing test suite.
2. **Missing Toggle Endpoints**: Implement `toggle-active` endpoints in `CustomerController`, `CustomerAddressController`, `ProductCategoryController`, and `ProductController`.
3. **Stock Detail Endpoint**: Implement `GET api/product-stock/{encryptedStockId}` in `ProductStockController`.

### Phase 2: Storefront Routing & Studio Content Integration
1. **Public Custom Commission Form**: Implement public commission request form at `/commissions` or `/contact/commissions` utilizing `POST api/commissions/create` with multi-image sketch uploader.
2. **Public About Page (`/about`)**: **Completed & Verified**. Dynamic page consuming `GET api/projects/public`, rendering studio manifesto, 7 visual gallery layout presets, full-screen lightbox modal, and the 12 DARAS Studio Disciplines.
3. **Public FAQ Page (`/faq`)**: Connect storefront FAQ view to `GET api/faq/public?location=faq`.
4. **Storefront Routing Recovery**: Wire `/shop`, `/contact`, `/press`, and policy routes into `app.routes.ts`.

### Phase 3: Ecommerce Foundation & Shopping Pipeline
1. **Database Schema Additions**:
   - Create `drs_cart_mst` and `drs_cart_item_mst`.
   - Create `drs_order_mst`, `drs_order_item_mst`, and `drs_order_status_history_mst`.
   - Create `Seq_OrderNo` (`ORD-yyyyMMdd-XXXX`).
2. **State Management**:
   - Implement `CartState` service with persistent local browser fallback and synchronized server storage.
   - Implement `WishlistState` service.
3. **Backend Order Pipeline**:
   - Implement `OrderController` with checkout validation, stock reservation, and address binding.
   - Implement Razorpay / Stripe payment gateway integration with webhook callback verification.

### Phase 4: Order Tracking, Invoicing & Account Operations
1. **Customer Account Portal**: Complete order history, live order tracking, and profile management.
2. **Admin Order Fulfillment**: Implement admin order management table, status progression, shipping label generation, and automated PDF invoice generation.

---

## 19. Build, Run, and Local Development Guide

### 16.1 Prerequisites
- **Node.js**: v20+ or v22+ LTS
- **npm**: v10+ or v11+
- **.NET SDK**: 8.0 SDK (`dotnet --version` $\ge$ 8.0.0)
- **SQL Server**: SQL Server 2019/2022 or LocalDB (`(localdb)\MSSQLLocalDB`)
- **IDE**: Visual Studio 2022, VS Code, or JetBrains Rider

### 16.2 Database Setup
Execute SQL scripts in `F:\Projects\Khel-Akhel\DBScripts` in the following strict order:
```powershell
# 1. Connect to SQL Server and create database if not present
sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'KhelAkhe_CRM') CREATE DATABASE KhelAkhe_CRM;"

# 2. Apply table schemas and sequences (includes drs_project_mst LayoutType column and constraint)
sqlcmd -S "(localdb)\MSSQLLocalDB" -d KhelAkhe_CRM -i "F:\Projects\Khel-Akhel\DBScripts\ProductScript-CreateTAbles.sql"

# 3. Apply foreign key constraints
sqlcmd -S "(localdb)\MSSQLLocalDB" -d KhelAkhe_CRM -i "F:\Projects\Khel-Akhel\DBScripts\ProductScript-Foreignkeys.sql"

# 4. Apply performance and unique indexes (includes IX_drs_project_mst_PublicList covering LayoutType)
sqlcmd -S "(localdb)\MSSQLLocalDB" -d KhelAkhe_CRM -i "F:\Projects\Khel-Akhel\DBScripts\ProductScript-Indexes.sql"

# 5. Seed master records, categories, error messages, and studio philosophy narrative
sqlcmd -S "(localdb)\MSSQLLocalDB" -d KhelAkhe_CRM -i "F:\Projects\Khel-Akhel\DBScripts\ProductScript-SeedData.sql"

# 6. Apply Cart and Wishlist module migration
sqlcmd -S "(localdb)\MSSQLLocalDB" -d KhelAkhe_CRM -i "F:\Projects\Khel-Akhel\DBScripts\Migration_Cart_And_Wishlist.sql"

# 7. Apply Contact Us inquiry module migration
sqlcmd -S "(localdb)\MSSQLLocalDB" -d KhelAkhe_CRM -i "F:\Projects\Khel-Akhel\DBScripts\Migration_ContactUs.sql"
```

### 16.3 Backend Configuration & Execution
1. Inspect `F:\Projects\Khel-Akhel\Khel-Akhel-Server\appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=KhelAkhe_CRM;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "YOUR_SECURE_JWT_SECRET_KEY_MINIMUM_32_CHARACTERS",
    "Issuer": "KhelAkhelServer",
    "Audience": "KhelAkhelClient",
    "ExpiryMinutes": 60
  },
  "Security": {
    "AllowedOrigins": [
      "http://localhost:4200"
    ]
  }
}
```
2. Build and launch:
```powershell
cd F:\Projects\Khel-Akhel\Khel-Akhel-Server
dotnet restore
dotnet build
dotnet run
```
3. Interactive Swagger documentation will be accessible at `https://localhost:7095/swagger` (or `http://localhost:5000/swagger`).

### 16.4 Frontend Configuration & Execution
1. Install dependencies and start development server:
```powershell
cd F:\Projects\Khel-Akhel\Khel-Akhel-Client
npm install
npm start
```
2. Open `http://localhost:4200` in a browser.
3. Production compilation:
```powershell
npm run build
```

---

## 20. Testing and Verification Record

During this documentation audit, the following automated verifications were executed:

### 17.1 Angular Build Verification
- **Command**: `npm run build` (`ng build`)
- **Execution Result**: **SUCCESS (Exit Code: 0)**
- **Output Details**:
  - Application bundle generation complete without compilation errors.
  - All standalone About components (`ProjectListComponent`, `ProjectFormComponent`, `GalleryLayoutSelectorComponent`, `ImageGalleryEditorComponent`, `GalleryRendererComponent`, and `About`) and services (`AdminAboutService`, `UserAboutService`) fully compiled into optimized Vite/esbuild chunks.
  - Output directory: `F:\Projects\Khel-Akhel\Khel-Akhel-Client\dist\Khel-Akhel-Client`.

### 20.2 Backend Test Suite Verification
- **Command**: `dotnet test "Khel-Akhel-Server\Khel-Akhel-Server.sln"`
- **Execution Result**: **PASSED (Exit Code: 0)**
- **Output Details**:
  - Target Framework: `net8.0`.
  - Passed: **115**, Failed: **0**, Skipped: **0**, Total: **115**.
  - Verified coverage: Authentication, password hashing, URL encryption, claims extraction, Cart concurrency, atomic stock validation, Wishlist guest token hashing, login merge, Move-to-Cart conversions, Contact Us inquiries, Projects layouts, Rate Limiting per IP, SkiaSharp CAPTCHA, Startup Secrets Validator, BCrypt Cost 12, Timing Invariance, Refresh Token Concurrency, Password Reset, and Public Forms.
  - Output binary: `F:\Projects\Khel-Akhel\Khel-Akhel-Server.Tests\bin\Debug\net8.0\Khel-Akhel-Server.Tests.dll`.

### 20.3 Angular Production Build Verification
- **Command**: `npm --prefix "Khel-Akhel-Client" run build` (`ng build`)
- **Execution Result**: **PASSED (Exit Code: 0)**
- **Output Details**:
  - Application bundle generation complete with **0 compilation errors**.
  - Generated standalone chunks including `faqs` (`chunk-QDGKQD2S.js`, 44.38 kB), `project-form`, `commissioned-projects`, and customer-facing components.
  - Output location: `F:\Projects\Khel-Akhel\Khel-Akhel-Client\dist\Khel-Akhel-Client`.

---

## 21. Change History and Maintenance Protocol

### 21.1 Version Control History

| Commit Hash | Commit Date | Author / Label | Architectural Changes & Highlights |
|---|---|---|---|
| `b6ff61b` | Sep 29, 2026 | Initial project setup | Established repository structure, Angular client, .NET backend, and initial schema scripts. |
| `0580093` | Oct 02, 2026 | Front End backend mapped | Integrated customer authentication, product catalog, category masters, and `ApiService`. |
| `57ef215` | Oct 04, 2026 | Development - 4102026 | Enhanced error pages, breadcrumbs, customer address management, and product image uploader. |
| `8bec0c8` | Oct 11, 2026 | Commission Development | Implemented end-to-end Commissioned Projects feature: `commissionsController`, database tables, sequences, admin list, filters, and image lightbox. |
| `0cf42e1` | Oct 14, 2026 | Dynamic Projects & Gallery Builder | **Completed Dynamic Projects Module**: Implemented Angular admin editor (`/admin/projects`, `/admin/projects/create`, `/admin/projects/edit/:id`), 7 visual gallery layout options (`grid-3`, `grid-2`, `editorial`, `featured`, `full-width`, `masonry`, `asymmetric`), drag-and-drop 15-image uploader with reordering & 5MB validation, real-time live preview tab, public storefront page (`/projects`) rendering dynamic showcases, 12 DARAS Studio Disciplines grid, interactive full-screen lightbox modal, and backend `LayoutType` database persistence with allowlist validation. |
| *Working Tree* | Oct 22, 2026 | Cart, Wishlist, Contact Us & Dynamic FAQs | **Completed Major E-Commerce & Knowledge Base Subsystems (v1.2.0)**:<br/>1. **Production Cart Backend**: Implemented `CartController`, `CartService`, `drs_cart_mst`, `drs_cart_item_mst` with concurrent race condition protection, live stock validation on increment, calculated line subtotals, and project-standard refactoring.<br/>2. **Production Wishlist Backend**: Implemented `WishlistController`, `WishlistService`, `drs_wishlist_mst`, `drs_wishlist_item_mst` supporting authenticated users and guest sessions via hashed cookie/header tokens, automatic login merge, and atomic move-to-cart conversion.<br/>3. **Contact Us API**: Implemented lightweight `ContactUsController` and `drs_contact_inquiry_mst` capturing customer inquiries without unnecessary service overhead.<br/>4. **Customer-Side Page Clean Reset**: Cleaned customer-side page shells of obsolete placeholder code while preserving admin layout, routes, and styles.<br/>5. **Dynamic FAQ Management**: Implemented `FaqsComponent` in Admin Dashboard (`/admin/faqs`) with debounced search, category/status/location filters, reactive modals, inline toggles, pagination, and soft-delete; connected customer storefront `/faq` with accordion view, category chips, and live search.<br/>6. **Testing & Audit**: Verified 89/89 passing backend tests and 0-error Angular production build. |

### 21.2 Documentation Maintenance Protocol
1. **Ground Truth Principle**: Always verify claims against code attributes (`[HttpGet]`, `[Authorize]`, `app.routes.ts`) and database tables rather than marketing notes.
2. **Schema Synchronization**: When altering database structures, always apply changes to `DBScripts/ProductScript-CreateTAbles.sql` and corresponding constraint/index scripts simultaneously.
3. **Status Integrity**: Never classify a feature as "Implemented and verified" unless both client routing, server endpoints, and database tables are verified in unison.
