# Phase 1 Retrospective: Kiến thức Cốt lõi & Thiết kế Auth Service

Tài liệu này tổng hợp câu trả lời chi tiết cho các câu hỏi nền tảng về bảo mật và thiết kế hệ thống trong Phase 1.

---

## 1. Tại sao dùng Refresh Token?

- **Vấn đề của Access Token ngắn hạn:** Để hạn chế rủi ro khi Access Token bị lộ (vì nó là stateless), thời gian sống của Access Token thường rất ngắn (15 phút). Nếu bắt người dùng đăng nhập lại sau mỗi 15 phút sẽ gây ra trải nghiệm tệ (UX kém).
- **Giải pháp của Refresh Token:** Refresh Token có thời gian sống dài hơn (ví dụ: 7 ngày) và được lưu trong database. Khi Access Token hết hạn, ứng dụng client gửi Refresh Token lên endpoint `/refresh` để nhận về cặp Access + Refresh Token mới (Token Rotation).
- **Lợi ích:**
  - Giảm thiểu rủi ro khi Access Token bị hack (nó sẽ tự hết hiệu lực sau tối đa 15 phút).
  - Có khả năng thu hồi phiên đăng nhập (Revocation) bằng cách vô hiệu hóa Refresh Token trong database.

---

## 2. Access Token nên sống bao lâu?

- **Thời gian đề xuất:** **15 phút**.
- **Lý do:** Khoảng thời gian này cân bằng giữa:
  - **Bảo mật:** Đủ ngắn để giảm thiểu thiệt hại nếu Access Token bị đánh cắp. Kẻ tấn công chỉ có tối đa 15 phút để sử dụng token đó trước khi nó vô giá trị.
  - **Hiệu năng:** Đủ dài để giảm số lượng request refresh token lên server, tránh quá tải hệ thống.

---

## 3. Tại sao JWT là Stateless?

- **Định nghĩa:** JWT (JSON Web Token) chứa toàn bộ thông tin (claims) của người dùng (ID, email, roles...) và được ký số bằng mã khóa bí mật (Private Key RS256).
- **Tại sao là Stateless:** Server không cần lưu trạng thái của Access Token trong database hay RAM session. Khi nhận một request có JWT, server chỉ cần dùng Public Key để giải mã và xác minh chữ ký số:
  - Nếu chữ ký hợp lệ và token chưa hết hạn → Token đó được tin tưởng hoàn toàn.
  - Không tốn chi phí truy vấn database (I/O) để kiểm tra token có hợp lệ không.

---

## 4. Tại sao không lưu JWT (Access Token) trong Database?

- Nếu lưu JWT trong database → Hệ thống mất đi tính chất **Stateless**. Server lại phải query database ở mỗi request để kiểm tra token → Làm chậm hệ thống (Bottleneck).
- Việc lưu JWT trong database là dư thừa vì tính toàn vẹn và hợp lệ của JWT đã được đảm bảo bằng chữ ký mã hóa (cryptographic signature).

---

## 5. Cookie hay LocalStorage?

Tùy thuộc vào môi trường sử dụng:

- **LocalStorage:**
  - *Ưu điểm:* Dễ cài đặt, dễ sử dụng cho các ứng dụng Single Page App (SPA) đa miền.
  - *Nhược điểm:* Rất dễ bị tấn công **XSS (Cross-Site Scripting)**. Nếu hacker tiêm được script độc hại vào client, chúng có thể đọc trực tiếp token từ LocalStorage.

- **HttpOnly, Secure Cookie:**
  - *Ưu điểm:* Chống tấn công XSS hiệu quả (JavaScript không thể đọc được cookie HttpOnly).
  - *Nhược điểm:* Dễ bị tấn công **CSRF (Cross-Site Request Forgery)**. Cần triển khai thêm các biện pháp chống CSRF (như SameSite=Strict/Lax hoặc CSRF Tokens).

- **Best Practice:** Lưu Access Token trong bộ nhớ ứng dụng (Memory/State) và lưu Refresh Token trong HttpOnly Secure Cookie với SameSite=Lax/Strict.

---

## 6. Tại sao Password Hash dùng Argon2/Bcrypt/PBKDF2?

- **Tính chất đặc biệt:** Các thuật toán này được thiết kế theo dạng **Adaptive Hashing** (hàm băm có thể điều chỉnh độ khó bằng cấu hình số vòng lặp/bộ nhớ).
- **Chống Brute Force / Rainbow Tables:**
  - Các thuật toán băm thông thường (MD5, SHA256) chạy cực nhanh (chỉ mất nano-giây), kẻ xấu có thể thử hàng tỷ mật khẩu mỗi giây bằng GPU.
  - Argon2/Bcrypt/PBKDF2 cố tình chạy chậm (mất khoảng 100ms - 200ms cho 1 lần băm) và tích hợp sẵn cơ chế Salt tự động. Điều này khiến việc tấn công Brute Force quy mô lớn trở nên bất khả thi về mặt chi phí và thời gian.
- ASP.NET Identity mặc định dùng PBKDF2 (PasswordHasher<TUser>), đây là chuẩn bảo mật production tin cậy.

---

## 7. Authentication khác Authorization thế nào?

- **Authentication (Xác thực):** Trả lời câu hỏi **"Bạn là ai?"**
  - Quá trình kiểm tra thông tin đăng nhập (email + password) để xác định danh tính người dùng. Kết quả thường là cấp một Access Token.

- **Authorization (Phân quyền):** Trả lời câu hỏi **"Bạn có quyền làm gì?"**
  - Quá trình kiểm tra xem người dùng đã được xác thực có quyền truy cập vào tài nguyên cụ thể hay không (ví dụ: Admin mới có quyền truy cập `/api/admin`).

---

## 8. ASP.NET Identity hoạt động ra sao?

- ASP.NET Identity là một framework quản lý người dùng và quyền truy cập tích hợp sẵn trong .NET.
- Nó quản lý các thực thể core (`IdentityUser`, `IdentityRole`, `IdentityUserClaim`, v.v.).
- Sử dụng các lớp Manager chuyên biệt (`UserManager<TUser>`, `RoleManager<TRole>`) để xử lý các nghiệp vụ đăng ký, đổi mật khẩu, xác thực email, cấp token bảo mật mà lập trình viên không cần tự viết code SQL thô.

---

## 9. Policy Authorization là gì?

- Trong ASP.NET Core, Policy-based Authorization tách biệt hoàn toàn giữa logic phân quyền và code trong controller.
- Thay vì hardcode trực tiếp `[Authorize(Roles = "Admin")]`, ta định nghĩa một Policy (ví dụ: `RequireVerifiedEmail` hoặc `RequireAdmin`) trong `Program.cs`.
- Trong controller, chỉ cần khai báo `[Authorize(Policy = "RequireVerifiedEmail")]`. Nếu sau này điều kiện phân quyền thay đổi, ta chỉ cần sửa ở một nơi duy nhất tại file cấu hình `Program.cs`.

---

## 10. JWT Middleware hoạt động như thế nào?

1. Khi có request gửi tới server → JWT Bearer Middleware bắt request đó.
2. Trích xuất JWT từ tiêu đề `Authorization: Bearer <token>`.
3. Xác minh chữ ký số của token bằng **Public Key (RS256)** được cấu hình.
4. Kiểm tra các điều kiện hiệu lực khác (Issuer, Audience, thời gian Expiration).
5. Nếu hợp lệ, giải mã claims và ánh xạ chúng thành một đối tượng `ClaimsPrincipal`, gán vào `HttpContext.User`. Kể từ thời điểm này, các controller có thể đọc được thông tin người dùng qua `User.Identity`.

---

## Kết luận

Phase 1 đã giúp chúng ta hiểu sâu về:
- Cơ chế xác thực hiện đại với JWT và Refresh Token
- Các best practices về bảo mật (password hashing, token storage, rate limiting)
- Clean Architecture và ASP.NET Identity
- Testing và CI/CD pipeline

Những kiến thức này sẽ là nền tảng vững chắc cho các Phase tiếp theo.
