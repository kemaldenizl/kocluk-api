# Girş ve Önsöz
Bu proje .net 9 ile clean architecture yapıda geliştirilmiştir. 
Amacı projelerin security microservicesi olmaktır.
Mevcut olarak email ve şifre fieldlarını içermektedir
Bu rehbere bakarak yeni field oluşturulabilir.

# Yeni Profil Alanı Ekleme Rehberi

Bu servisi yeni bir projeye microservice olarak ekledikten sonra, 
yeni bir kullanıcı alanı (ör. `PhoneNumber`, `FirstName`, `LastName`) 
eklemek için izlenecek adımlar.

Örnek olarak `PhoneNumber` (nullable) alanı ekliyoruz. Diğer alanlar için aynı adımları tekrarla.

Dokunulan katmanlar: **Domain → Infrastructure (EF + Migration) → Application → API**

---

## 1) Domain — `User` aggregate'ine alanı ekle

`Security.Domain/Users/User.cs`

```csharp
// 1a. Property (private setter, nullable)
public string? PhoneNumber { get; private set; }

// 1b. Güncelleme metodu (partial update: sadece gönderilenler değişir, null = temizle)
public void UpdateProfile(string? phoneNumber)
{
    PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
}
```

> Not: Birden çok alanı tek metotta güncelleyeceksen, imzayı genişlet:
> `UpdateProfile(string? firstName, string? lastName, string? phoneNumber)`.

---

## 2) Infrastructure — EF konfigürasyonu

`Security.Infrastructure/Persistence/Configurations/Users/UserConfiguration.cs`

`Configure` metodunun içine ekle:

```csharp
builder.Property(x => x.PhoneNumber)
    .HasMaxLength(32); // nullable olduğu için IsRequired() YOK
```

---

## 3) Veritabanı — Migration oluştur ve uygula

Çözüm kök dizininden çalıştır:

```bash
# Migration üret
dotnet ef migrations add AddUserPhoneNumber \
  --project Security.Infrastructure \
  --startup-project Security.API \
  --output-dir Persistence/Migrations

# Veritabanına uygula
dotnet ef database update \
  --project Security.Infrastructure \
  --startup-project Security.API
```

> Yeni projeye ilk kurulumda (boş DB) sadece `database update` çalıştırman yeterli;
> mevcut tüm migration'lar sırayla uygulanır.

---

## 4) Application — Command / Handler / Validator / DTO

### 4a. Response DTO
`Security.Application/Users/UpdateProfile/Dtos/UpdateProfileResponse.cs`

```csharp
namespace Security.Application.Users.UpdateProfile.Dtos;

public sealed record UpdateProfileResponse(Guid Id, string? PhoneNumber);
```

### 4b. Command
`Security.Application/Users/UpdateProfile/UpdateProfileCommand.cs`

```csharp
using MediatR;
using Security.Application.Common.Results;
using Security.Application.Users.UpdateProfile.Dtos;

namespace Security.Application.Users.UpdateProfile;

public sealed record UpdateProfileCommand(
    Guid UserId,
    string? PhoneNumber
) : IRequest<Result<UpdateProfileResponse>>;
```

### 4c. Validator
`Security.Application/Users/UpdateProfile/UpdateProfileCommandValidator.cs`

```csharp
using FluentValidation;

namespace Security.Application.Users.UpdateProfile;

public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(32)
            .Matches(@"^\+?[0-9]{7,15}$")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
    }
}
```

### 4d. Handler
`Security.Application/Users/UpdateProfile/UpdateProfileCommandHandler.cs`

```csharp
using MediatR;
using Security.Application.Abstractions.Auditing;
using Security.Application.Abstractions.Persistence;
using Security.Application.Abstractions.UnitOfWork;
using Security.Application.Common.Auditing;
using Security.Application.Common.Errors;
using Security.Application.Common.Results;
using Security.Application.Users.UpdateProfile.Dtos;
using Security.Domain.Auditing;

namespace Security.Application.Users.UpdateProfile;

public sealed class UpdateProfileCommandHandler(
    IUserRepository userRepository,
    IAuditLogRepository auditLogRepository,
    IAuditLogFactory auditLogFactory,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateProfileCommand, Result<UpdateProfileResponse>>
{
    public async Task<Result<UpdateProfileResponse>> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return Result<UpdateProfileResponse>.Failure(AuthErrors.UserNotFound);
        }

        user.UpdateProfile(request.PhoneNumber);

        var auditLog = auditLogFactory.Create(
            AuditActionType.ProfileUpdated,
            AuditPayloadBuilder.Build(new
            {
                @event = "profile_updated",
                userId = user.Id
            }),
            user.Id);

        await auditLogRepository.AddAsync(auditLog, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<UpdateProfileResponse>.Success(new UpdateProfileResponse(user.Id, user.PhoneNumber));
    }
}
```

> `AuthErrors.UserNotFound` yoksa uygun bir hata kullan/ekle
> (`Security.Application/Common/Errors/AuthError.cs`).
> Yeni audit tipi için `AuditActionType` enum'ına `ProfileUpdated = 34,` ekle
> (`Security.Domain/Auditing/AuditActionType.cs`).

---

## 5) API — Contract + Endpoint

### 5a. Request contract
`Security.API/Contracts/Users/UpdateProfileRequest.cs`

```csharp
namespace Security.API.Contracts.Users;

public sealed record UpdateProfileRequest(string? PhoneNumber);
```

### 5b. Endpoint
`Security.API/Endpoints/UserEndpoints.cs` — `/api/users` grubu zaten `RequireAuthorization()`.

`MapUserEndpoints` içine, `/me` route'unun altına ekle:

```csharp
group.MapPut("/me/profile", UpdateProfileAsync)
    .RequireRateLimiting(RateLimitPolicyNames.Admin)
    .WithName("UpdateProfile")
    .WithSummary("Updates the authenticated user's profile fields.")
    .WithDescription("Updates non-credential profile fields (e.g. phone number).")
    .Accepts<UpdateProfileRequest>("application/json")
    .Produces<UpdateProfileResponse>(StatusCodes.Status200OK)
    .ProducesValidationProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status401Unauthorized)
    .ProducesProblem(StatusCodes.Status429TooManyRequests)
    .WithOpenApi();
```

Aynı sınıfa handler metodu (mevcut `RequestPasswordChangeAsync` deseni):

```csharp
private static async Task<IResult> UpdateProfileAsync(
    UpdateProfileRequest request,
    HttpContext httpContext,
    ISender sender,
    CancellationToken cancellationToken)
{
    var currentUser = httpContext.User.ToCurrentUser();

    var command = new UpdateProfileCommand(currentUser.UserId, request.PhoneNumber);
    var result = await sender.Send(command, cancellationToken);

    return httpContext.ToApiResult(result);
}
```

Gerekli `using` satırlarını dosyanın başına ekle
(`Security.Application.Users.UpdateProfile`, `Security.Application.Users.UpdateProfile.Dtos`).

---

## 6) Kayıt ve `/me` çıktısına yansıtma

- Yeni alanın kayıt sırasında da alınması için : `RegisterCommand`, `RegisterCommandHandler`,
  `RegisterRequest` ve `User` constructor'ını güncelle.
- Kullanıcı bilgisinde dönmesini istersen: `UserDto` (`Security.Application/Auth/Dtos/UserDto.cs`)
  ve ilgili response'lara alanı ekle.

---

## Hızlı Kontrol Listesi

- [ ] `User.cs`: property + `UpdateProfile` metodu
- [ ] `UserConfiguration.cs`: `builder.Property(...)`
- [ ] `dotnet ef migrations add ...` + `dotnet ef database update`
- [ ] Command + Validator + Handler + Response DTO
- [ ] `AuditActionType.ProfileUpdated` (gerekliyse)
- [ ] API Contract (`UpdateProfileRequest`) + endpoint + handler metodu
- [ ] `dotnet build` ile derleme kontrolü

> Her yeni alan için: **Domain property → EF config → migration → command/handler alanı → contract alanı.**
> Alanlar birbirinden bağımsız; sadece ilgili katmanlara tek satır ekleyip migration üretmen yeterli.
