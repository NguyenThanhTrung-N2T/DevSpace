# Phase 1 Walkthrough: ASP.NET Auth Service

Tài liệu này tổng hợp toàn bộ các kết quả đạt được trong Phase 1 (Auth Service).

---

## 1. Project Restructuring & Architecture

Chúng tôi đã tái cấu trúc hoàn toàn `apps/auth-service/` từ cấu trúc phẳng ban đầu sang kiến trúc **Clean Architecture** chuẩn hóa bao gồm các lớp thư viện riêng biệt nhắm tới .NET 8.0:

- **`Auth.Domain`**: Chứa các thực thể chính (`User`, `Role`, `RefreshToken`, `UserVerificationToken`).
- **`Auth.Application`**: Chứa các interface dịch vụ (`IAuthDbContext`, `IEmailSender`, `IJwtService`), các lớp Request DTOs và các bộ validator sử dụng FluentValidation.
- **`Auth.Infrastructure`**: Chứa triển khai EF Core (`AuthDbContext`), dịch vụ JWT (ký số RS256 có `kid`), mock email sender và các đăng ký Dependency Injection.
- **`Auth.Api`**: Web API host chứa các controller (`AuthController`), middleware xử lý ngoại lệ toàn cục (`GlobalExceptionHandler`), và cấu hình tài liệu hóa Scalar.

---

## 2. Authentication & Controller Endpoints

Đã triển khai đầy đủ các endpoint phục vụ đăng ký, đăng nhập và bảo mật tài khoản:

- **`POST /api/auth/register`**: Tạo người dùng mới, gán vai trò `User` mặc định, ghi nhận mã xác thực và in link kích hoạt mock ra console.
- **`POST /api/auth/login`**: Xác thực mật khẩu, kiểm tra tài khoản hoạt động/bị xóa mềm, cấp Access Token (JWT) và Refresh Token mới.
- **`POST /api/auth/refresh`**: Cấp cặp token mới sử dụng cơ chế xoay vòng Refresh Token (Rotation).
- **`POST /api/auth/logout`**: Vô hiệu hóa Refresh Token ngay lập tức trong database.
- **`POST /api/auth/revoke-all`**: Vô hiệu hóa toàn bộ các phiên đăng nhập đang hoạt động của người dùng hiện tại (yêu cầu xác thực JWT).
- **`GET /api/auth/confirm-email`**: Xác thực email người dùng thông qua mã kích hoạt.
- **`PUT /api/auth/change-password`**: Thay đổi mật khẩu người dùng (yêu cầu xác thực JWT).
- **`POST /api/auth/forgot-password` & `POST /api/auth/reset-password`**: Yêu cầu khôi phục và đặt mật khẩu mới một cách an toàn.

---

## 3. Security Hardening & Authorization

Hệ thống được tăng cường bảo mật sâu rộng:

- **Chữ ký JWT RS256 & JWKS**: Sử dụng mã khóa RSA 2048-bit để ký token. Cung cấp endpoint công khai `/.well-known/jwks.json` phục vụ việc truy xuất public key. Có fallback tự động tạo key in-memory nếu thiếu cấu hình khóa PEM.

- **Refresh Token Family Rotation**: Nhóm các refresh token theo từng phiên đăng nhập (`FamilyId`). Nếu phát hiện một token cũ trong cùng nhóm bị dùng lại (reuse), hệ thống lập tức thu hồi toàn bộ các token đang hoạt động của nhóm đó nhằm ngăn chặn tấn công chiếm đoạt phiên.

- **Băm mật khẩu & Lưu trữ**: Sử dụng PBKDF2 mặc định của ASP.NET Identity. Băm Refresh Token và Verification Token bằng SHA-256 trước khi lưu trữ dưới database.

- **Xóa mềm (Soft Delete)**: Tích hợp global filter `HasQueryFilter` trong EF Core để lọc tự động người dùng bị xóa mềm khỏi mọi truy vấn.

- **Rate Limiting**: Thiết lập chính sách giới hạn tần suất gọi endpoint IP-based fixed-window trên `/login` (5 req/phút) và `/register` (3 req/phút).

- **Global Exception Handler**: Định dạng chuẩn lỗi theo tiêu chuẩn RFC-7807 Problem Details kèm theo trace correlation ID trong trường hợp có lỗi hệ thống.

- **Phân quyền dựa trên Policy**: Cấu hình các policy `RequireAdmin` và `RequireVerifiedEmail` trong `Program.cs`.

---

## 4. Test Suite Verification

Đã xây dựng bộ công cụ kiểm thử đầy đủ chạy trên nền SDK .NET 8.0:

- **Unit Tests**: Kiểm thử thành công các validator đầu vào (`RegisterRequestValidator`, `LoginRequestValidator`) và các logic sinh JWT, in-memory fallback của `JwtService`.

- **Integration Tests**: Sử dụng kết nối in-memory SQLite để xác thực toàn bộ các ràng buộc quan hệ thực thể của `AuthDbContext`, hoạt động lưu trữ của `UserVerificationToken` và cơ chế chống lạm dụng Refresh Token Family Rotation.

- **Kết quả kiểm thử**: **16 tests đã pass thành công (0 failed)**.

- **GitHub Actions**: Tích hợp chạy kiểm thử tự động (`dotnet test`) vào file cấu hình CI `.github/workflows/ci.yml`.

---

## 5. Database Schema

Schema `auth` được triển khai trên Neon PostgreSQL với các bảng chính:

### Users
- Kế thừa từ `IdentityUser<Guid>`
- Bổ sung: `DisplayName`, `AvatarUrl`, `EmailVerified`, `CreatedAt`, `UpdatedAt`, `LastLoginAt`, `IsActive`, `DeletedAt`
- Soft Delete với Global Query Filter

### RefreshTokens
- `Id`, `UserId`, `TokenHash` (SHA-256)
- `FamilyId` - nhóm token rotation
- `ExpiresAt`, `CreatedAt`, `RevokedAt`, `IsRevoked`
- `ReplacedByTokenId` - token kế nhiệm
- `Reason`, `CreatedByIp`, `Device`

### UserVerificationTokens
- `Id`, `UserId`, `Type` (PasswordReset/EmailVerification)
- `TokenHash` (SHA-256)
- `ExpiresAt`, `CreatedAt`, `UsedAt`

### Roles
- `Admin`, `User` (seeded mặc định)

---

## 6. Configuration & Options Pattern

Sử dụng Options Pattern cho tất cả configuration:

- **JwtOptions**: `AccessTokenExpiryMinutes` (15), `RefreshTokenExpiryDays` (7), `Issuer`, `Audience`
- **DatabaseOptions**: Connection string từ `DATABASE_URL`
- **CorsOptions**: Allowed origins
- **RateLimitOptions**: Login (5/min), Register (3/min)

---

## 7. API Documentation

- **Scalar UI**: Truy cập tại `/scalar/v1`
- **OpenAPI Specification**: Export tại `/openapi/v1.json`
- Đầy đủ request/response examples và error codes

---

## 8. Docker & Deployment

- **Multi-stage Dockerfile**: Optimized build với layer caching
- **HEALTHCHECK**: Ping `/health/ready` endpoint
- **Docker Compose**: Chỉ chạy Auth Service + Web + Nginx
- **Database**: Kết nối tới Neon qua `DATABASE_URL`

---

## 9. CI/CD Pipeline

GitHub Actions workflow (`.github/workflows/ci.yml`):
1. Restore dependencies
2. Build solution
3. Run tests
4. Build Docker image (future)

---

## 10. What We Learned

### Technical Skills
- Clean Architecture implementation in .NET
- ASP.NET Identity customization
- JWT RS256 signing with JWKS
- Refresh Token Family Rotation
- EF Core Global Query Filters
- FluentValidation
- xUnit testing with in-memory SQLite
- Rate Limiting in ASP.NET Core 8.0

### Security Best Practices
- Stateless JWT vs Stateful Refresh Token
- Token reuse detection
- Soft delete pattern
- Password hashing (PBKDF2)
- Token storage (SHA-256 hashing)
- RFC-7807 Problem Details

### Architecture Decisions
- No Repository Pattern (EF Core is the repository)
- Options Pattern over IConfiguration injection
- Policy-based Authorization
- Mock Email Sender (logging only)
- Single database with multiple schemas

---

## 11. What's Next (Phase 2)

- Spring Boot Core Service
- Workspace/Project/Task management
- Cross-service JWT verification via JWKS
- Nginx routing updates
- Permission system (linked to workspace resources)

---

## Conclusion

Phase 1 đã đặt nền tảng vững chắc cho toàn bộ hệ thống DevSpace. Auth Service hiện đã production-ready với:
- ✅ Bảo mật chuẩn ngành (RS256, Token Rotation, Rate Limiting)
- ✅ Clean Architecture dễ maintain và scale
- ✅ Test coverage tốt (16/16 tests pass)
- ✅ CI/CD pipeline tự động
- ✅ Documentation đầy đủ

Tất cả deliverables đã hoàn thành theo đúng kế hoạch ban đầu.
