# SkillHive — API Contracts

> Auto-generated on 2026-10-04T17:50:42.217Z  
> Server: http://localhost:5167

**140 endpoints** · **57 DTOs** · **19 enums** · **70/140 endpoints with response shapes**

## Response Envelope

Every API response uses this envelope. Success or failure is determined by the "success" boolean.

**Success:**
```json
{
  "success": true,
  "statusCode": 200,
  "message": "Success",
  "data": "<endpoint-specific payload>"
}
```

**Error:**
```json
{
  "success": false,
  "statusCode": 400,
  "message": "Human-readable error description",
  "data": null
}
```

- HTTP status code matches statusCode in body
- Validation errors return 400 with a descriptive message
- Unauthorized → 401, Forbidden → 403, Not Found → 404, Server Error → 500
- data is null for errors

## Authentication

- **Scheme:** JWT Bearer (with httpOnly cookie bridge)
- **Token lifetime:** 7 days
- **Cookie name:** AuthToken
- **Refresh endpoint:** None

- Login returns { token, user }. The token is also set as an httpOnly cookie (7-day expiry).
- CookieAuthMiddleware copies the AuthToken cookie into the Authorization header automatically — so cookie-based browser clients work without sending the header explicitly.
- There is NO token refresh endpoint. When a token expires, the user must log in again.
- Roles: SUPER_ADMIN, ACADEMY_OWNER, INSTRUCTOR, MODERATOR, STUDENT.

---

## Enums

### `CertificateStatus`

- `ISSUED`
- `REVOKED`
- `REISSUED`

### `CommentStatus`

- `PUBLISHED`
- `HIDDEN`
- `FLAGGED`
- `DELETED`

### `CommentTargetType`

- `COURSE`
- `LESSON`

### `CourseLevel`

- `BEGINNER`
- `INTERMEDIATE`
- `ADVANCED`

### `CourseStatus`

- `DRAFT`
- `PENDING_REVIEW`
- `PUBLISHED`
- `REJECTED`
- `ARCHIVED`

### `CourseVisibility`

- `PUBLIC`
- `PRIVATE`
- `UNLISTED`

### `EnrollmentStatus`

- `ACTIVE`
- `COMPLETED`
- `DROPPED`
- `EXPIRED`

### `FileType`

- `PDF`
- `DOCX`
- `PPTX`
- `EXCEL`
- `IMAGE`
- `VIDEO`
- `TEXT`
- `OTHER`

### `InvoiceStatus`

- `DRAFT`
- `UNPAID`
- `PAID`
- `PAST_DUE`
- `VOID`
- `REFUNDED`

### `NotificationType`

- `WELCOME`
- `OTP_VERIFICATION`
- `EMAIL_VERIFICATION`
- `PASSWORD_RESET`
- `STAFF_INVITE`
- `COURSE_PUBLISHED`
- `COURSE_REJECTED`
- `ENROLLMENT_CONFIRMED`
- `CERTIFICATE_ISSUED`
- `CERTIFICATE_REISSUE_REQUEST`
- `CERTIFICATE_REISSUE_APPROVED`
- `PAYMENT_RECEIVED`
- `INVOICE_ISSUED`
- `INVOICE_OVERDUE`
- `SUBSCRIPTION_EXPIRING`
- `GENERAL`

### `PaymentProvider`

- `PAYSTACK`

### `PaymentPurpose`

- `SUBSCRIPTION`
- `COURSE_PURCHASE`

### `PaymentStatus`

- `PENDING`
- `SUCCESS`
- `FAILED`
- `REFUNDED`

### `ReviewStatus`

- `PUBLISHED`
- `HIDDEN`
- `FLAGGED`

### `SubscriptionInterval`

- `MONTHLY`
- `QUARTERLY`
- `ANNUAL`

### `SubscriptionStatus`

- `TRIAL`
- `ACTIVE`
- `PAST_DUE`
- `GRACE`
- `EXPIRED`
- `CANCELLED`
- `SUSPENDED`

### `UserRole`

- `SUPER_ADMIN`
- `ACADEMY_OWNER`
- `INSTRUCTOR`
- `MODERATOR`
- `STUDENT`

### `UserStatus`

- `INVITED`
- `ACTIVE`
- `INACTIVE`
- `LOCKED`
- `SUSPENDED`

### `VerificationTokenType`

- `OTP`
- `EMAIL_VERIFICATION`
- `PASSWORD_RESET`
- `INVITE`

---

## Endpoints

### Academies

#### `GET /api/academies/{slug}`

**Auth:** unknown

**Parameters:**
- `slug` — path, string **(required)**

**Response:**

```json
{
  "academyId": "<expression>",
  "name": "<expression>",
  "slug": "<expression>",
  "description": "<expression>",
  "logoUrl": "<expression>",
  "bannerUrl": "<expression>",
  "isVerified": "<expression>",
  "createdAt": "<expression>"
}
```

#### `GET /api/academies/me`

**Auth:** unknown

**Response:**

```json
{
  "academyId": "<expression>",
  "name": "<expression>",
  "slug": "<expression>",
  "description": "<expression>",
  "logoUrl": "<expression>",
  "bannerUrl": "<expression>",
  "email": "<expression>",
  "phone": "<expression>",
  "address": "<expression>",
  "ownerId": "<expression>",
  "isVerified": "<expression>",
  "isActive": "<expression>",
  "createdAt": "<expression>",
  "updatedAt": "<expression>"
}
```

#### `PATCH /api/academies/me`

**Auth:** unknown

**Request Body:** `UpdateAcademyDto`

```json
{
  "Name": null,
  "Description": null,
  "LogoUrl": null,
  "BannerUrl": null,
  "Email": null,
  "Phone": null,
  "Address": null
}
```

**Response:**

```json
{
  "academyId": "<expression>",
  "name": "<expression>",
  "slug": "<expression>",
  "description": "<expression>",
  "logoUrl": "<expression>",
  "bannerUrl": "<expression>",
  "email": "<expression>",
  "phone": "<expression>",
  "address": "<expression>",
  "ownerId": "<expression>",
  "isVerified": "<expression>",
  "isActive": "<expression>",
  "createdAt": "<expression>",
  "updatedAt": "<expression>"
}
```

### Analytics

#### `GET /api/analytics/academy/courses`

**Auth:** unknown

**Parameters:**
- `dateFrom` — query, string
- `dateTo` — query, string

**Response:**

```json
{
  "revenueInRange": "<variable>",
  "period": {},
  "total": 0,
  "items": "<variable>",
  "courseId": "<expression>",
  "title": "<expression>",
  "slug": "<expression>",
  "status": "ENUM_VALUE",
  "visibility": "ENUM_VALUE",
  "isFree": "<expression>",
  "price": "<expression>",
  "totalLessons": "<expression>",
  "totalEnrollments": "<expression>",
  "averageRating": "<expression>",
  "totalReviews": "<expression>",
  "publishedAt": "<expression>",
  "updatedAt": "<expression>",
  "revenue": "<unknown>",
  "count": "<unknown>"
}
```

#### `GET /api/analytics/academy/enrollments`

**Auth:** unknown

**Parameters:**
- `dateFrom` — query, string
- `dateTo` — query, string

**Response:**

```json
{
  "period": {},
  "totalInRange": 0,
  "courseTitle": "<expression>",
  "studentName": "<expression>",
  "date": "<unknown>",
  "count": "<unknown>",
  "enrollmentId": "<expression>",
  "enrolledAt": "<expression>",
  "status": "ENUM_VALUE",
  "progressPercentage": "<expression>"
}
```

#### `GET /api/analytics/academy/overview`

**Auth:** unknown

**Parameters:**
- `dateFrom` — query, string
- `dateTo` — query, string

**Response:**

```json
{
  "period": {},
  "courses": {},
  "students": {},
  "enrollments": {},
  "revenue": {},
  "ratings": {},
  "Status": "<expression>",
  "Count": "<unknown>"
}
```

#### `GET /api/analytics/academy/revenue`

**Auth:** unknown

**Parameters:**
- `dateFrom` — query, string
- `dateTo` — query, string

**Response:**

```json
{
  "period": {},
  "currency": "<unknown>",
  "totalInRange": "<variable>",
  "transactionCount": 0,
  "date": "<unknown>",
  "revenue": "<unknown>",
  "count": "<unknown>"
}
```

#### `GET /api/analytics/academy/top-courses`

**Auth:** unknown

**Parameters:**
- `dateFrom` — query, string
- `dateTo` — query, string

**Response:**

```json
{
  "courseId": "<expression>",
  "title": "<unknown>",
  "slug": "<unknown>",
  "revenue": "<expression>",
  "period": {},
  "totalEnrollments": "<expression>",
  "averageRating": "<expression>"
}
```

#### `GET /api/analytics/platform/academies`

**Auth:** unknown

**Response:**

```json
{
  "byStatus": "<unknown>",
  "planName": "<expression>",
  "count": "<unknown>",
  "status": "<expression>",
  "month": "<unknown>"
}
```

#### `GET /api/analytics/platform/overview`

**Auth:** unknown

**Response:**

```json
{
  "academies": {},
  "students": {},
  "courses": {},
  "enrollments": {},
  "certificates": {},
  "Status": "<expression>",
  "Count": "<unknown>"
}
```

#### `GET /api/analytics/platform/revenue`

**Auth:** unknown

**Parameters:**
- `dateFrom` — query, string
- `dateTo` — query, string

**Response:**

```json
{
  "period": {},
  "currency": "string",
  "transactionCount": 0,
  "mrr": "<unknown>",
  "activeSubscriptions": 0,
  "date": "<unknown>",
  "revenue": "<unknown>",
  "count": "<unknown>"
}
```

#### `GET /api/analytics/platform/signups`

**Auth:** unknown

**Parameters:**
- `dateFrom` — query, string
- `dateTo` — query, string

**Response:**

```json
{
  "period": {},
  "totalNewAcademies": 0,
  "totalNewStudents": 0,
  "date": "<unknown>",
  "count": "<unknown>"
}
```

#### `GET /api/analytics/platform/top-academies`

**Auth:** unknown

**Parameters:**
- `dateFrom` — query, string
- `dateTo` — query, string

**Response:**

```json
{
  "academyId": "<expression>",
  "name": "<unknown>",
  "slug": "<unknown>",
  "revenue": "<expression>",
  "studentCount": "<expression>",
  "period": {}
}
```

#### `GET /api/analytics/student/certificates`

**Auth:** unknown

**Response:**

```json
{
  "total": 0,
  "issued": "<unknown>",
  "revoked": "<unknown>",
  "items": "<variable>",
  "certificateId": "<expression>",
  "verificationCode": "<expression>",
  "status": "ENUM_VALUE",
  "issuedAt": "<expression>",
  "course": {},
  "academy": {}
}
```

#### `GET /api/analytics/student/enrollments`

**Auth:** unknown

**Response:**

```json
{
  "items": "<variable>",
  "enrollmentId": "<expression>",
  "status": "ENUM_VALUE",
  "progressPercentage": "<expression>",
  "enrolledAt": "<expression>",
  "completedAt": "<expression>",
  "lastAccessedAt": "<expression>",
  "course": {},
  "count": "<unknown>"
}
```

#### `GET /api/analytics/student/overview`

**Auth:** unknown

**Response:**

```json
{
  "totalEnrollments": "<unknown>",
  "droppedEnrollments": "<unknown>",
  "Status": "<expression>",
  "Count": "<unknown>"
}
```

### Audit

#### `GET /api/audit/{id}`

**Auth:** unknown

**Parameters:**
- `id` — path, integer **(required)**

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

#### `GET /api/audit/academy`

**Auth:** unknown

**Parameters:**
- `userId` — query, integer
- `action` — query, string
- `page` — query, integer
- `pageSize` — query, integer

**Response:**

```json
{
  "pagination": {},
  "auditId": "<expression>",
  "userId": "<expression>",
  "userName": "<expression>",
  "userEmail": "<expression>",
  "academyId": "<expression>",
  "academyName": "<unknown>",
  "action": "<expression>",
  "targetType": "<expression>",
  "targetId": "<expression>",
  "metadata": "<expression>",
  "ipAddress": "<expression>",
  "createdAt": "<expression>"
}
```

#### `GET /api/audit/all`

**Auth:** unknown

**Parameters:**
- `userId` — query, integer
- `academyId` — query, integer
- `action` — query, string
- `targetType` — query, string
- `targetId` — query, integer
- `dateFrom` — query, string
- `dateTo` — query, string
- `page` — query, integer
- `pageSize` — query, integer

**Response:**

```json
{
  "pagination": {},
  "auditId": "<expression>",
  "userId": "<expression>",
  "userName": "<expression>",
  "userEmail": "<expression>",
  "academyId": "<expression>",
  "academyName": "<unknown>",
  "action": "<expression>",
  "targetType": "<expression>",
  "targetId": "<expression>",
  "metadata": "<expression>",
  "ipAddress": "<expression>",
  "createdAt": "<expression>"
}
```

#### `GET /api/audit/me`

**Auth:** unknown

**Parameters:**
- `page` — query, integer
- `pageSize` — query, integer

**Response:**

```json
{
  "pagination": {},
  "auditId": "<expression>",
  "userId": "<expression>",
  "userName": "<expression>",
  "userEmail": "<expression>",
  "academyId": "<expression>",
  "academyName": "<unknown>",
  "action": "<expression>",
  "targetType": "<expression>",
  "targetId": "<expression>",
  "metadata": "<expression>",
  "ipAddress": "<expression>",
  "createdAt": "<expression>"
}
```

### Auth

#### `POST /api/auth/accept-invite`

**Auth:** unknown

**Request Body:** `AcceptInviteDto`

```json
{
  "Token": "string",
  "Password": "string"
}
```

**Response:**

```json
{
  "token": "<variable>",
  "user": {}
}
```

#### `POST /api/auth/forgot-password`

**Auth:** unknown

**Request Body:** `ForgotPasswordDto`

```json
{
  "Email": "string"
}
```

**Response:**

```json
{
  "message": "string"
}
```

#### `POST /api/auth/login`

**Auth:** unknown

**Request Body:** `LoginDto`

```json
{
  "UsernameOrEmail": "string",
  "Password": "string"
}
```

**Response:**

```json
{
  "user": {}
}
```

#### `POST /api/auth/logout`

**Auth:** unknown

**Response:**

```json
{
  "message": "string",
  "userId": "<expression>",
  "emailVerified": false
}
```

#### `POST /api/auth/register/academy`

**Auth:** unknown

**Request Body:** `RegisterAcademyDto`

```json
{
  "AcademyName": "string",
  "AcademyDescription": null,
  "OwnerFullName": "string",
  "OwnerEmail": "string",
  "OwnerUsername": "string",
  "OwnerPhone": null,
  "Password": "string"
}
```

**Response:**

```json
{
  "userId": "<expression>",
  "academyId": "<expression>",
  "fullName": "<expression>",
  "email": "<expression>",
  "username": "<expression>",
  "role": "ENUM_VALUE",
  "emailVerified": false,
  "message": "string"
}
```

#### `POST /api/auth/register/student`

**Auth:** unknown

**Request Body:** `RegisterStudentDto`

```json
{
  "FullName": "string",
  "Username": "string",
  "Email": "string",
  "Phone": null,
  "Password": "string"
}
```

**Response:**

```json
{
  "userId": "<expression>",
  "fullName": "<expression>",
  "email": "<expression>",
  "username": "<expression>",
  "role": "ENUM_VALUE",
  "emailVerified": false,
  "message": "string"
}
```

#### `POST /api/auth/resend-verification`

**Auth:** unknown

**Request Body:** `ResendVerificationDto`

```json
{
  "Email": "string"
}
```

**Response:**

```json
{
  "message": "string"
}
```

#### `POST /api/auth/reset-password`

**Auth:** unknown

**Request Body:** `ResetPasswordDto`

```json
{
  "Token": "string",
  "NewPassword": "string"
}
```

**Response:**

```json
{
  "message": "string"
}
```

#### `POST /api/auth/verify-email`

**Auth:** unknown

**Request Body:** `VerifyEmailDto`

```json
{
  "Token": "string"
}
```

**Response:**

```json
{
  "userId": "<expression>",
  "emailVerified": false,
  "message": "string"
}
```

#### `POST /api/auth/verify-otp`

**Auth:** unknown

**Request Body:** `VerifyOtpDto`

```json
{
  "Email": "string",
  "Otp": "string"
}
```

**Response:**

```json
{
  "message": "string",
  "userId": "<expression>",
  "emailVerified": false
}
```

### Categories

#### `GET /api/categories`

**Auth:** unknown

**Parameters:**
- `academySlug` — query, string

**Response:**

```json
{
  "categoryId": "<expression>",
  "name": "<expression>",
  "slug": "<expression>",
  "isGlobal": "<expression>",
  "academyId": "<expression>",
  "professions": "<unknown>",
  "professionId": "<expression>"
}
```

#### `POST /api/categories`

**Auth:** unknown

**Request Body:** `CreateCategoryDto`

```json
{
  "Name": "string",
  "IsGlobal": false
}
```

**Response:**

```json
{
  "categoryId": "<expression>",
  "name": "<expression>",
  "slug": "<expression>",
  "isGlobal": "<expression>",
  "academyId": "<expression>",
  "isActive": "<expression>",
  "createdAt": "<expression>"
}
```

#### `DELETE /api/categories/{id}`

**Auth:** unknown

**Parameters:**
- `id` — path, integer **(required)**

**Response:**

```json
{
  "categoryId": "<expression>",
  "isActive": false,
  "message": "string"
}
```

#### `PATCH /api/categories/{id}`

**Auth:** unknown

**Parameters:**
- `id` — path, integer **(required)**

**Request Body:** `UpdateCategoryDto`

```json
{
  "Name": null
}
```

**Response:**

```json
{
  "categoryId": "<expression>",
  "name": "<expression>",
  "slug": "<expression>",
  "isGlobal": "<expression>",
  "academyId": "<expression>",
  "previousNames": "<expression>",
  "isActive": "<expression>"
}
```

#### `GET /api/categories/professions`

**Auth:** unknown

**Parameters:**
- `academySlug` — query, string
- `categoryId` — query, integer

**Response:**

```json
{
  "professionId": "<expression>",
  "name": "<expression>",
  "slug": "<expression>",
  "categoryId": "<expression>",
  "isGlobal": "<expression>",
  "academyId": "<expression>"
}
```

#### `POST /api/categories/professions`

**Auth:** unknown

**Request Body:** `CreateProfessionDto`

```json
{
  "Name": "string",
  "CategoryId": 0,
  "IsGlobal": false
}
```

**Response:**

```json
{
  "professionId": "<expression>",
  "name": "<expression>",
  "slug": "<expression>",
  "categoryId": "<expression>",
  "isGlobal": "<expression>",
  "academyId": "<expression>",
  "isActive": "<expression>"
}
```

#### `DELETE /api/categories/professions/{id}`

**Auth:** unknown

**Parameters:**
- `id` — path, integer **(required)**

**Response:**

```json
{
  "professionId": "<expression>",
  "isActive": false,
  "message": "string"
}
```

#### `PATCH /api/categories/professions/{id}`

**Auth:** unknown

**Parameters:**
- `id` — path, integer **(required)**

**Request Body:** `UpdateProfessionDto`

```json
{
  "Name": null
}
```

**Response:**

```json
{
  "professionId": "<expression>",
  "name": "<expression>",
  "slug": "<expression>",
  "categoryId": "<expression>",
  "isGlobal": "<expression>",
  "academyId": "<expression>",
  "previousNames": "<expression>",
  "isActive": "<expression>"
}
```

### Certificates

#### `GET /api/certificates/{id}`

**Auth:** unknown

**Parameters:**
- `id` — path, integer **(required)**

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

#### `POST /api/certificates/{id}/reissue`

**Auth:** unknown

**Parameters:**
- `id` — path, integer **(required)**

**Request Body:** `ReissueCertificateDto`

```json
{
  "Reason": null
}
```

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

#### `POST /api/certificates/{id}/revoke`

**Auth:** unknown

**Parameters:**
- `id` — path, integer **(required)**

**Request Body:** `RevokeCertificateDto`

```json
{
  "Reason": "string"
}
```

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

#### `GET /api/certificates/academy`

**Auth:** unknown

**Parameters:**
- `courseId` — query, integer

**Response:**

```json
{
  "certificateId": "<expression>",
  "verificationCode": "<expression>",
  "status": "ENUM_VALUE",
  "issuedAt": "<expression>",
  "revokedAt": "<expression>",
  "student": {},
  "course": {}
}
```

#### `GET /api/certificates/my`

**Auth:** unknown

**Response:**

```json
{
  "certificateId": "<expression>",
  "verificationCode": "<expression>",
  "status": "ENUM_VALUE",
  "issuedAt": "<expression>",
  "pdfUrl": "<unknown>",
  "course": {},
  "academy": {}
}
```

#### `POST /api/certificates/regenerate/{enrollmentId}`

**Auth:** unknown

**Parameters:**
- `enrollmentId` — path, integer **(required)**

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

#### `GET /api/certificates/verify/{code}`

**Auth:** unknown

**Parameters:**
- `code` — path, string **(required)**

**Response:**

```json
{
  "verified": "<unknown>",
  "status": "ENUM_VALUE",
  "verificationCode": "<expression>",
  "studentName": "<expression>",
  "courseTitle": "<expression>",
  "academyName": "<expression>",
  "issuedAt": "<expression>",
  "revokedAt": "<expression>",
  "revokedReason": "<expression>"
}
```

### Comments

#### `GET /api/comments`

**Auth:** unknown

**Parameters:**
- `targetType` — query, string
- `targetId` — query, integer

**Response:**

```json
{
  "commentId": "<expression>",
  "body": "<expression>",
  "isPinned": "<expression>",
  "createdAt": "<expression>",
  "updatedAt": "<expression>",
  "author": {},
  "replies": "<unknown>"
}
```

#### `POST /api/comments`

**Auth:** unknown

**Request Body:** `CreateCommentDto`

```json
{
  "TargetType": "<CommentTargetType>",
  "TargetId": 0,
  "Body": "string",
  "ParentCommentId": 0
}
```

**Response:**

```json
{
  "commentId": "<expression>",
  "targetType": "ENUM_VALUE",
  "targetId": "<expression>",
  "courseId": "<expression>",
  "parentCommentId": "<expression>",
  "body": "<expression>",
  "status": "ENUM_VALUE",
  "isPinned": "<expression>",
  "createdAt": "<expression>"
}
```

#### `DELETE /api/comments/{id}`

**Auth:** unknown

**Parameters:**
- `id` — path, integer **(required)**

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

#### `GET /api/comments/{id}`

**Auth:** unknown

**Parameters:**
- `id` — path, integer **(required)**

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

#### `PATCH /api/comments/{id}`

**Auth:** unknown

**Parameters:**
- `id` — path, integer **(required)**

**Request Body:** `UpdateCommentDto`

```json
{
  "Body": "string"
}
```

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

#### `POST /api/comments/{id}/hide`

**Auth:** unknown

**Parameters:**
- `id` — path, integer **(required)**

**Request Body:** `HideCommentDto`

```json
{
  "Reason": "string"
}
```

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

#### `POST /api/comments/{id}/pin`

**Auth:** unknown

**Parameters:**
- `id` — path, integer **(required)**

**Request Body:** `PinCommentDto`

```json
{
  "IsPinned": false
}
```

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

#### `POST /api/comments/{id}/unhide`

**Auth:** unknown

**Parameters:**
- `id` — path, integer **(required)**

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

### Courses

#### `GET /api/courses`

**Auth:** unknown

**Parameters:**
- `categoryId` — query, integer
- `professionId` — query, integer
- `academyId` — query, integer
- `level` — query, unknown
- `isFree` — query, boolean
- `search` — query, string
- `page` — query, integer
- `pageSize` — query, integer

**Response:**

```json
{
  "pagination": {},
  "courseId": "<expression>",
  "title": "<expression>",
  "slug": "<expression>",
  "description": "<expression>",
  "coverImageUrl": "<expression>",
  "isFree": "<expression>",
  "price": "<expression>",
  "discountPrice": "<expression>",
  "currency": "<expression>",
  "level": "ENUM_VALUE",
  "language": "<expression>",
  "totalLessons": "<expression>",
  "totalEnrollments": "<expression>",
  "averageRating": "<expression>",
  "totalReviews": "<expression>",
  "publishedAt": "<expression>",
  "academy": {},
  "instructor": {},
  "profession": {}
}
```

#### `POST /api/courses`

**Auth:** unknown

**Request Body:** `CreateCourseDto`

```json
{
  "Title": "string",
  "Description": null,
  "LongDescription": null,
  "ProfessionId": 0,
  "IsFree": false,
  "Price": 0,
  "DiscountPrice": 0,
  "CoverImageUrl": null,
  "PromoVideoUrl": null,
  "Level": "<CourseLevel>",
  "Language": "string",
  "EstimatedDurationMinutes": 0,
  "Prerequisites": null
}
```

**Response:**

```json
{
  "courseId": "<expression>",
  "title": "<expression>",
  "slug": "<expression>",
  "status": "ENUM_VALUE",
  "visibility": "ENUM_VALUE",
  "isFree": "<expression>",
  "price": "<expression>",
  "createdAt": "<expression>"
}
```

#### `PATCH /api/courses/{id}`

**Auth:** unknown

**Parameters:**
- `id` — path, integer **(required)**

**Request Body:** `UpdateCourseDto`

```json
{
  "Title": null,
  "Description": null,
  "LongDescription": null,
  "ProfessionId": 0,
  "IsFree": false,
  "Price": 0,
  "DiscountPrice": 0,
  "CoverImageUrl": null,
  "PromoVideoUrl": null,
  "Level": "<CourseLevel>",
  "Language": null,
  "EstimatedDurationMinutes": 0,
  "Prerequisites": null,
  "Visibility": "<CourseVisibility>"
}
```

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

#### `PATCH /api/courses/{id}/approve`

**Auth:** unknown

**Parameters:**
- `id` — path, integer **(required)**

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

#### `PATCH /api/courses/{id}/archive`

**Auth:** unknown

**Parameters:**
- `id` — path, integer **(required)**

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

#### `PATCH /api/courses/{id}/reject`

**Auth:** unknown

**Parameters:**
- `id` — path, integer **(required)**

**Request Body:** `RejectCourseDto`

```json
{
  "Reason": "string"
}
```

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

#### `PATCH /api/courses/{id}/submit`

**Auth:** unknown

**Parameters:**
- `id` — path, integer **(required)**

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

#### `GET /api/courses/{slug}`

**Auth:** unknown

**Parameters:**
- `slug` — path, string **(required)**

**Response:**

```json
{
  "courseId": "<expression>",
  "title": "<expression>",
  "slug": "<expression>",
  "description": "<expression>",
  "longDescription": "<expression>",
  "coverImageUrl": "<expression>",
  "promoVideoUrl": "<expression>",
  "isFree": "<expression>",
  "price": "<expression>",
  "discountPrice": "<expression>",
  "currency": "<expression>",
  "level": "ENUM_VALUE",
  "language": "<expression>",
  "estimatedDurationMinutes": "<expression>",
  "prerequisites": "<expression>",
  "status": "ENUM_VALUE",
  "visibility": "ENUM_VALUE",
  "publishedAt": "<expression>",
  "totalLessons": "<expression>",
  "totalEnrollments": "<expression>",
  "averageRating": "<expression>",
  "totalReviews": "<expression>",
  "createdAt": "<expression>",
  "updatedAt": "<expression>",
  "academy": {},
  "instructor": {},
  "profession": {}
}
```

#### `GET /api/courses/academy/mine`

**Auth:** unknown

**Response:**

```json
{
  "courseId": "<expression>",
  "title": "<expression>",
  "slug": "<expression>",
  "status": "ENUM_VALUE",
  "visibility": "ENUM_VALUE",
  "isFree": "<expression>",
  "price": "<expression>",
  "totalLessons": "<expression>",
  "totalEnrollments": "<expression>",
  "averageRating": "<expression>",
  "totalReviews": "<expression>",
  "publishedAt": "<expression>",
  "updatedAt": "<expression>",
  "instructor": {},
  "profession": {}
}
```

### EmailTest

#### `POST /api/email/test`

**Auth:** unknown

**Request Body:** `TestEmailRequest`

⚠️ *Response shape could not be extracted (method-not-found). Check the controller + service manually.*

### Enrollments

#### `POST /api/enrollments`

**Auth:** unknown

**Request Body:** `EnrollDto`

```json
{
  "CourseId": 0
}
```

**Response:**

```json
{
  "enrollmentId": "<expression>",
  "studentId": "<expression>",
  "courseId": "<expression>",
  "status": "ENUM_VALUE",
  "enrolledAt": "<expression>",
  "progressPercentage": "<expression>"
}
```

#### `DELETE /api/enrollments/{id}`

**Auth:** unknown

**Parameters:**
- `id` — path, integer **(required)**

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

#### `GET /api/enrollments/{id}`

**Auth:** unknown

**Parameters:**
- `id` — path, integer **(required)**

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

#### `GET /api/enrollments/academy`

**Auth:** unknown

**Parameters:**
- `courseId` — query, integer

**Response:**

```json
{
  "enrollmentId": "<expression>",
  "status": "ENUM_VALUE",
  "progressPercentage": "<expression>",
  "enrolledAt": "<expression>",
  "completedAt": "<expression>",
  "student": {},
  "course": {}
}
```

#### `DELETE /api/enrollments/follows`

**Auth:** unknown

**Request Body:** `UnfollowAcademyDto`

```json
{
  "AcademyId": 0
}
```

**Response:**

```json
{
  "academyId": "<expression>",
  "message": "string"
}
```

#### `GET /api/enrollments/follows`

**Auth:** unknown

**Response:**

```json
{
  "academyId": "<expression>",
  "name": "<expression>",
  "slug": "<expression>",
  "logoUrl": "<expression>",
  "notifyOnNewCourse": "<expression>",
  "followedAt": "<expression>"
}
```

#### `POST /api/enrollments/lessons/complete`

**Auth:** unknown

**Request Body:** `MarkLessonCompleteDto`

```json
{
  "LessonId": 0
}
```

**Response:**

```json
{
  "enrollmentId": "<expression>",
  "lessonId": "<expression>",
  "progressPercentage": "<expression>",
  "status": "<expression>",
  "completedAt": "<expression>"
}
```

#### `GET /api/enrollments/me`

**Auth:** unknown

**Response:**

```json
{
  "enrollmentId": "<expression>",
  "status": "ENUM_VALUE",
  "progressPercentage": "<expression>",
  "enrolledAt": "<expression>",
  "completedAt": "<expression>",
  "lastAccessedAt": "<expression>",
  "certificateIssuedAt": "<expression>",
  "course": {}
}
```

### Invoices

#### `POST /api/invoices`

**Auth:** unknown

**Request Body:** `CreateInvoiceDto`

```json
{
  "SubscriptionId": 0,
  "AmountDue": 0,
  "Currency": "string",
  "PeriodStart": "2026-01-01T00:00:00Z",
  "PeriodEnd": "2026-01-01T00:00:00Z",
  "DueDate": "2026-01-01T00:00:00Z"
}
```

⚠️ *Response shape could not be extracted (method-not-found). Check the controller + service manually.*

#### `GET /api/invoices/{id}`

**Auth:** unknown

**Parameters:**
- `id` — path, integer **(required)**

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

#### `POST /api/invoices/{id}/mark-paid`

**Auth:** unknown

**Parameters:**
- `id` — path, integer **(required)**

**Request Body:** `MarkInvoicePaidDto`

```json
{
  "Reference": null,
  "Reason": "string"
}
```

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

#### `POST /api/invoices/{id}/void`

**Auth:** unknown

**Parameters:**
- `id` — path, integer **(required)**

**Request Body:** `VoidInvoiceDto`

```json
{
  "Reason": "string"
}
```

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

#### `GET /api/invoices/all`

**Auth:** unknown

**Parameters:**
- `status` — query, string
- `academyId` — query, integer

⚠️ *Response shape could not be extracted (method-not-found). Check the controller + service manually.*

#### `POST /api/invoices/generate/{subscriptionId}`

**Auth:** unknown

**Parameters:**
- `subscriptionId` — path, integer **(required)**

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

#### `GET /api/invoices/me`

**Auth:** unknown

⚠️ *Response shape could not be extracted (method-not-found). Check the controller + service manually.*

#### `GET /api/invoices/subscription/{subscriptionId}`

**Auth:** unknown

**Parameters:**
- `subscriptionId` — path, integer **(required)**

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

### Lessons

#### `POST /api/courses/{courseId}/lessons`

**Auth:** unknown

**Parameters:**
- `courseId` — path, integer **(required)**

**Request Body:** `CreateLessonDto`

```json
{
  "Title": "string",
  "Description": null,
  "Content": null,
  "VideoUrl": null,
  "Order": 0,
  "DurationMinutes": 0,
  "IsPreview": false
}
```

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

#### `GET /api/courses/{courseSlug}/lessons`

**Auth:** unknown

**Parameters:**
- `courseSlug` — path, string **(required)**

**Response:**

```json
{
  "lessonId": "<expression>",
  "title": "<expression>",
  "description": "<expression>",
  "order": "<expression>",
  "durationMinutes": "<expression>",
  "isPreview": "<expression>",
  "isActive": "<expression>",
  "createdAt": "<expression>",
  "updatedAt": "<expression>"
}
```

#### `DELETE /api/lessons/{id}`

**Auth:** unknown

**Parameters:**
- `id` — path, integer **(required)**

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

#### `GET /api/lessons/{id}`

**Auth:** unknown

**Parameters:**
- `id` — path, integer **(required)**

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

#### `PATCH /api/lessons/{id}`

**Auth:** unknown

**Parameters:**
- `id` — path, integer **(required)**

**Request Body:** `UpdateLessonDto`

```json
{
  "Title": null,
  "Description": null,
  "Content": null,
  "VideoUrl": null,
  "DurationMinutes": 0,
  "IsPreview": false
}
```

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

#### `PATCH /api/lessons/{id}/reorder`

**Auth:** unknown

**Parameters:**
- `id` — path, integer **(required)**

**Request Body:** `ReorderLessonDto`

```json
{
  "NewOrder": 0
}
```

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

### Materials

#### `GET /api/lessons/{lessonId}/materials`

**Auth:** unknown

**Parameters:**
- `lessonId` — path, integer **(required)**

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

#### `POST /api/lessons/{lessonId}/materials`

**Auth:** unknown

**Parameters:**
- `lessonId` — path, integer **(required)**

**Request Body:** `CreateMaterialDto`

```json
{
  "Title": "string",
  "FileUrl": "string",
  "FileType": "string",
  "FileSizeBytes": 0,
  "Order": 0
}
```

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

#### `DELETE /api/materials/{id}`

**Auth:** unknown

**Parameters:**
- `id` — path, integer **(required)**

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

#### `PATCH /api/materials/{id}`

**Auth:** unknown

**Parameters:**
- `id` — path, integer **(required)**

**Request Body:** `UpdateMaterialDto`

```json
{
  "Title": null,
  "Order": 0
}
```

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

#### `POST /api/materials/upload`

**Auth:** unknown

**Response:**

```json
{
  "fileUrl": "<expression>",
  "publicId": "<expression>",
  "fileSizeBytes": "<expression>",
  "fileType": "ENUM_VALUE",
  "originalFileName": "<variable>",
  "formattedSize": "<unknown>"
}
```

### Notifications

#### `GET /api/notifications`

**Auth:** unknown

**Parameters:**
- `unreadOnly` — query, boolean

⚠️ *Response shape could not be extracted (method-not-found). Check the controller + service manually.*

#### `PATCH /api/notifications/{id}/read`

**Auth:** unknown

**Parameters:**
- `id` — path, integer **(required)**

⚠️ *Response shape could not be extracted (method-not-found). Check the controller + service manually.*

#### `PATCH /api/notifications/read-all`

**Auth:** unknown

⚠️ *Response shape could not be extracted (method-not-found). Check the controller + service manually.*

#### `GET /api/notifications/unread-count`

**Auth:** unknown

⚠️ *Response shape could not be extracted (method-not-found). Check the controller + service manually.*

### Payments

#### `POST /api/payments/course/initialize`

**Auth:** unknown

**Request Body:** `InitializeCoursePaymentDto`

```json
{
  "CourseId": 0
}
```

**Response:**

```json
{
  "authorizationUrl": "<expression>",
  "purpose": "string",
  "courseId": "<expression>",
  "courseTitle": "<expression>"
}
```

#### `POST /api/payments/subscription/initialize`

**Auth:** unknown

**Request Body:** `InitializeSubscriptionPaymentDto`

```json
{
  "InvoiceId": 0
}
```

**Response:**

```json
{
  "authorizationUrl": "<expression>",
  "purpose": "string",
  "invoiceId": "<expression>"
}
```

#### `POST /api/payments/verify`

**Auth:** unknown

**Request Body:** `VerifyPaymentDto`

```json
{
  "Reference": "string"
}
```

⚠️ *Response shape could not be extracted (method-not-found). Check the controller + service manually.*

#### `POST /api/payments/webhook`

**Auth:** unknown

**Response:**

```json
{
  "status": "string",
  "message": "string"
}
```

### Quizzes

#### `POST /api/quizzes`

**Auth:** unknown

**Request Body:** `CreateQuizDto`

```json
{
  "Title": "string",
  "Description": null,
  "CourseId": 0,
  "LessonId": 0,
  "PassingScore": 0,
  "TimeLimitMinutes": 0,
  "MaxAttempts": 0,
  "CooldownMinutes": 0
}
```

**Response:**

```json
{
  "quizId": "<expression>",
  "courseId": "<expression>",
  "lessonId": "<expression>",
  "title": "<expression>",
  "passingScore": "<expression>",
  "maxAttempts": "<expression>",
  "cooldownMinutes": "<expression>",
  "createdAt": "<expression>"
}
```

#### `DELETE /api/quizzes/{id}`

**Auth:** unknown

**Parameters:**
- `id` — path, integer **(required)**

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

#### `GET /api/quizzes/{id}`

**Auth:** unknown

**Parameters:**
- `id` — path, integer **(required)**

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

#### `PATCH /api/quizzes/{id}`

**Auth:** unknown

**Parameters:**
- `id` — path, integer **(required)**

**Request Body:** `UpdateQuizDto`

```json
{
  "Title": null,
  "Description": null,
  "PassingScore": 0,
  "TimeLimitMinutes": 0,
  "MaxAttempts": 0,
  "CooldownMinutes": 0
}
```

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

#### `GET /api/quizzes/{quizId}/my-attempts`

**Auth:** unknown

**Parameters:**
- `quizId` — path, integer **(required)**

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

#### `POST /api/quizzes/{quizId}/questions`

**Auth:** unknown

**Parameters:**
- `quizId` — path, integer **(required)**

**Request Body:** `CreateQuestionDto`

```json
{
  "QuestionText": "string",
  "Points": 0,
  "Order": 0,
  "Options": []
}
```

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

#### `POST /api/quizzes/{quizId}/submit`

**Auth:** unknown

**Parameters:**
- `quizId` — path, integer **(required)**

**Request Body:** `SubmitQuizDto`

```json
{
  "Answers": []
}
```

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

#### `GET /api/quizzes/by-lesson/{lessonId}`

**Auth:** unknown

**Parameters:**
- `lessonId` — path, integer **(required)**

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

#### `DELETE /api/quizzes/questions/{questionId}`

**Auth:** unknown

**Parameters:**
- `questionId` — path, integer **(required)**

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

#### `PATCH /api/quizzes/questions/{questionId}`

**Auth:** unknown

**Parameters:**
- `questionId` — path, integer **(required)**

**Request Body:** `UpdateQuestionDto`

```json
{
  "QuestionText": null,
  "Points": 0,
  "Order": 0,
  "Options": []
}
```

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

### Reviews

#### `POST /api/reviews`

**Auth:** unknown

**Request Body:** `CreateReviewDto`

```json
{
  "CourseId": 0,
  "Rating": 0,
  "Title": null,
  "Body": "string"
}
```

**Response:**

```json
{
  "reviewId": "<expression>",
  "courseId": "<expression>",
  "rating": "<expression>",
  "title": "<expression>",
  "body": "<expression>",
  "isVerifiedPurchase": "<expression>",
  "status": "ENUM_VALUE",
  "createdAt": "<expression>"
}
```

#### `GET /api/reviews/{id}`

**Auth:** unknown

**Parameters:**
- `id` — path, integer **(required)**

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

#### `PATCH /api/reviews/{id}`

**Auth:** unknown

**Parameters:**
- `id` — path, integer **(required)**

**Request Body:** `UpdateReviewDto`

```json
{
  "Rating": 0,
  "Title": null,
  "Body": null
}
```

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

#### `POST /api/reviews/{id}/hide`

**Auth:** unknown

**Parameters:**
- `id` — path, integer **(required)**

**Request Body:** `HideReviewDto`

```json
{
  "Reason": "string"
}
```

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

#### `POST /api/reviews/{id}/unhide`

**Auth:** unknown

**Parameters:**
- `id` — path, integer **(required)**

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

#### `GET /api/reviews/academy`

**Auth:** unknown

**Parameters:**
- `courseId` — query, integer
- `status` — query, string

**Response:**

```json
{
  "reviewId": "<expression>",
  "rating": "<expression>",
  "title": "<expression>",
  "body": "<expression>",
  "status": "ENUM_VALUE",
  "isVerifiedPurchase": "<expression>",
  "helpfulCount": "<expression>",
  "createdAt": "<expression>",
  "student": {},
  "course": {}
}
```

#### `GET /api/reviews/course/{courseId}`

**Auth:** unknown

**Parameters:**
- `courseId` — path, integer **(required)**
- `page` — query, integer
- `pageSize` — query, integer

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

#### `GET /api/reviews/my`

**Auth:** unknown

**Response:**

```json
{
  "reviewId": "<expression>",
  "rating": "<expression>",
  "title": "<expression>",
  "body": "<expression>",
  "status": "ENUM_VALUE",
  "helpfulCount": "<expression>",
  "createdAt": "<expression>",
  "updatedAt": "<expression>",
  "course": {}
}
```

### Staff

#### `GET /api/staff`

**Auth:** unknown

**Response:**

```json
{
  "userId": "<expression>",
  "fullName": "<expression>",
  "username": "<expression>",
  "email": "<expression>",
  "phone": "<expression>",
  "role": "ENUM_VALUE",
  "status": "ENUM_VALUE",
  "emailVerified": "<expression>",
  "invitedByUserId": "<expression>",
  "lastLogin": "<expression>",
  "createdAt": "<expression>"
}
```

#### `PATCH /api/staff/{id}`

**Auth:** unknown

**Parameters:**
- `id` — path, integer **(required)**

**Request Body:** `UpdateStaffDto`

```json
{
  "FullName": null,
  "Phone": null,
  "Role": null
}
```

**Response:**

```json
{
  "userId": "<expression>",
  "fullName": "<expression>",
  "email": "<expression>",
  "username": "<expression>",
  "phone": "<expression>",
  "role": "ENUM_VALUE",
  "status": "ENUM_VALUE",
  "updatedAt": "<expression>"
}
```

#### `PATCH /api/staff/{id}/deactivate`

**Auth:** unknown

**Parameters:**
- `id` — path, integer **(required)**

**Response:**

```json
{
  "userId": "<expression>",
  "status": "ENUM_VALUE",
  "message": "string"
}
```

#### `PATCH /api/staff/{id}/reactivate`

**Auth:** unknown

**Parameters:**
- `id` — path, integer **(required)**

**Response:**

```json
{
  "userId": "<expression>",
  "status": "ENUM_VALUE",
  "message": "string"
}
```

#### `POST /api/staff/invite`

**Auth:** unknown

**Request Body:** `InviteStaffDto`

```json
{
  "FullName": "string",
  "Email": "string",
  "Phone": null,
  "Role": "string"
}
```

**Response:**

```json
{
  "userId": "<expression>",
  "fullName": "<expression>",
  "email": "<expression>",
  "username": "<expression>",
  "role": "ENUM_VALUE",
  "status": "ENUM_VALUE",
  "message": "string"
}
```

### Subscriptions

#### `POST /api/subscriptions/{id}/cancel`

**Auth:** unknown

**Parameters:**
- `id` — path, integer **(required)**

**Request Body:** `CancelSubscriptionDto`

```json
{
  "Reason": "string"
}
```

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

#### `POST /api/subscriptions/{id}/change-plan`

**Auth:** unknown

**Parameters:**
- `id` — path, integer **(required)**

**Request Body:** `ChangePlanDto`

```json
{
  "NewPlanId": 0,
  "Reason": null
}
```

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

#### `POST /api/subscriptions/{id}/reactivate`

**Auth:** unknown

**Parameters:**
- `id` — path, integer **(required)**

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

#### `POST /api/subscriptions/{id}/suspend`

**Auth:** unknown

**Parameters:**
- `id` — path, integer **(required)**

**Request Body:** `SuspendSubscriptionDto`

```json
{
  "Reason": "string"
}
```

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

#### `GET /api/subscriptions/academy/{academyId}`

**Auth:** unknown

**Parameters:**
- `academyId` — path, integer **(required)**

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

#### `POST /api/subscriptions/assign`

**Auth:** unknown

**Request Body:** `AssignSubscriptionDto`

```json
{
  "AcademyId": 0,
  "PlanId": 0,
  "StartDate": "2026-01-01T00:00:00Z",
  "EndDate": "2026-01-01T00:00:00Z",
  "AutoRenew": false,
  "GraceUntil": "2026-01-01T00:00:00Z"
}
```

⚠️ *Response shape could not be extracted (method-not-found). Check the controller + service manually.*

#### `GET /api/subscriptions/me`

**Auth:** unknown

⚠️ *Response shape could not be extracted (method-not-found). Check the controller + service manually.*

#### `GET /api/subscriptions/plans`

**Auth:** unknown

⚠️ *Response shape could not be extracted (method-not-found). Check the controller + service manually.*

#### `POST /api/subscriptions/plans`

**Auth:** unknown

**Request Body:** `CreatePlanDto`

```json
{
  "Name": "string",
  "Slug": null,
  "Description": null,
  "Price": 0,
  "Currency": "string",
  "Interval": "<SubscriptionInterval>",
  "MaxCourses": 0,
  "MaxStaff": 0,
  "MaxStudentsPerCourse": 0,
  "CanChargeCourses": false,
  "CanUseCertificates": false,
  "CanUseCustomBranding": false,
  "IsActive": false,
  "IsPublic": false
}
```

⚠️ *Response shape could not be extracted (method-not-found). Check the controller + service manually.*

#### `DELETE /api/subscriptions/plans/{id}`

**Auth:** unknown

**Parameters:**
- `id` — path, integer **(required)**

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

#### `PATCH /api/subscriptions/plans/{id}`

**Auth:** unknown

**Parameters:**
- `id` — path, integer **(required)**

**Request Body:** `UpdatePlanDto`

```json
{
  "Name": null,
  "Description": null,
  "Price": 0,
  "Currency": null,
  "Interval": "<SubscriptionInterval>",
  "MaxCourses": 0,
  "MaxStaff": 0,
  "MaxStudentsPerCourse": 0,
  "CanChargeCourses": false,
  "CanUseCertificates": false,
  "CanUseCustomBranding": false,
  "IsActive": false,
  "IsPublic": false
}
```

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

#### `GET /api/subscriptions/plans/{slug}`

**Auth:** unknown

**Parameters:**
- `slug` — path, string **(required)**

⚠️ *Response shape could not be extracted (method-not-found). Check the controller + service manually.*

#### `GET /api/subscriptions/plans/all`

**Auth:** unknown

⚠️ *Response shape could not be extracted (method-not-found). Check the controller + service manually.*

#### `GET /api/subscriptions/plans/id/{id}`

**Auth:** unknown

**Parameters:**
- `id` — path, integer **(required)**

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

### SystemAdmin

#### `GET /api/system/health`

**Auth:** SUPER_ADMIN

**Response:**

```json
{
  "status": "<unknown>",
  "uptime": "<unknown>",
  "timestamp": "2026-01-01T00:00:00Z"
}
```

#### `GET /api/system/info`

**Auth:** unknown

⚠️ *Response shape could not be extracted (no controller mapping). Check the controller + service manually.*

### Users

#### `GET /api/users/me`

**Auth:** unknown

**Response:**

```json
{
  "userId": "<expression>",
  "fullName": "<expression>",
  "username": "<expression>",
  "email": "<expression>",
  "phone": "<expression>",
  "role": "ENUM_VALUE",
  "academyId": "<expression>",
  "status": "ENUM_VALUE",
  "emailVerified": "<expression>",
  "lastLogin": "<expression>",
  "createdAt": "<expression>"
}
```

#### `PATCH /api/users/me`

**Auth:** unknown

**Request Body:** `UpdateProfileDto`

```json
{
  "FullName": null,
  "Phone": null
}
```

**Response:**

```json
{
  "userId": "<expression>",
  "fullName": "<expression>",
  "username": "<expression>",
  "email": "<expression>",
  "phone": "<expression>",
  "updatedAt": "<expression>"
}
```

#### `PATCH /api/users/me/password`

**Auth:** unknown

**Request Body:** `ChangePasswordDto`

```json
{
  "CurrentPassword": "string",
  "NewPassword": "string"
}
```

**Response:**

```json
{
  "message": "string"
}
```
