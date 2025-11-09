---
name: C# Expert (from Nick and Sauber)
description: C# Expert Guide — .NET Maintainability, Security, and Practices
---

# C# Expert Guide — .NET Maintainability, Security, and Practices

> “Every system tends toward complexity, slowness, and difficulty. Staying simple, fast, and easy to maintain is a battle fought every single day.”

## 🧠 Philosophy

- Optimize for **change** — design for maintainability.
- Keep things **cohesive**: related logic lives together.
- Keep things **simple**: prefer clarity over abstraction.
- Keep things **consistent**: naming, structure, and patterns should feel predictable.

---

## 📁 Folder Structure: Feature Folders

Group code **by feature**, not by technical layer.

```
/Features
 ├── MyProfile/
 │    ├── MyProfileController.cs
 │    ├── MyProfileViewModel.cs
 │    ├── MyProfileService.cs
 │    ├── MyProfileTests.cs
```

**Benefits:**
- High cohesion, easier deletion/refactor.
- Reduces “navigation friction.”
- Mirrors user features and stories.

Projects:
- `Core` → domain logic, rules, services  
- `Api` / `Web` / `Worker` → entry point  

---

## ⚠️ Treat Warnings as Errors

```xml
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
```

Start this **from project creation**.

---

## 🪵 Logging, Metrics & Auditing

### Structured Logging with Serilog

✅ Use:
```csharp
_logger.LogInformation("User {UserId} logged in", id);
```

### Logging Levels

| Level | Purpose |
|--------|----------|
| Debug | Step-by-step tracing |
| Information | Request summary |
| Warning | Repeated but non-fatal issues |
| Error | Real failure |
| Critical | App boot failure |

### Logs ≠ Metrics ≠ Audits

| Type | Purpose | Storage |
|------|----------|----------|
| Logs | Dev troubleshooting | Log Analytics |
| Metrics | KPIs, CPU, requests | App Insights |
| Audits | Who did what, when | Database |

---

## 🔒 Security

### Global Fallback Policy
```csharp
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
```

### Remove Server Header
```csharp
builder.WebHost.ConfigureKestrel(o => o.AddServerHeader = false);
```

---

## 🧾 Validation

Use **FluentValidation** instead of DataAnnotations.

```csharp
public class RegisterUserValidator : AbstractValidator<RegisterViewModel>
{
    public RegisterUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).MinimumLength(8);
    }
}
```

---

## ⚙️ Configuration

Avoid injecting `IOptions<T>` directly:

```csharp
builder.Services.Configure<AppSettings>(config.GetSection("App"));
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IOptions<AppSettings>>().Value);
```

---

## 💡 Code Design & Smells

### Early Returns → Happy Path at Bottom
```csharp
if (!ModelState.IsValid) return Page();
if (!await _userService.CreateAsync(user)) return Error();
await _emailService.SendWelcomeEmailAsync(user);
return RedirectToPage("Success");
```

**Rule:** more indentation → less maintainable.

### Method/Class Length
- Methods > 20 lines → refactor
- Classes > 200 lines → split

---

## 🧪 Automated Testing

- Prefer **xUnit v3**.
- Apply **Czechov’s Gun** — every line matters.

```csharp
var customer = CreateValidCustomer();
customer.LastName = "";
var result = _validator.Validate(customer);
result.Errors.Should().Contain(e => e.PropertyName == "LastName");
```

---

## 🧰 Dependency Injection Hygiene

```csharp
builder.Services.BuildServiceProvider(new ServiceProviderOptions
{
    ValidateScopes = true,
    ValidateOnBuild = true
});
```

Catches captive dependency issues early.

---

## 🌳 Git & Deployment

### Trunk-Based Development
- One long-lived branch: `main`
- Merge early, deploy often

### Build Once, Deploy Many
- Publish once → promote same artifact
- Secrets differ by environment

### CI/CD Pipelines
Use “confident green” deployments — every green build should be production-ready.

---

## 🧩 Miscellaneous Tips

- **Central Package Management**
```xml
<ItemGroup>
  <PackageVersion Include="xunit" Version="2.6.6" />
</ItemGroup>
```
- **Entity Framework SQL**
```json
"Microsoft.EntityFrameworkCore.Database.Command": "Information"
```
(Local only)

---

## ✅ Principles Recap

- Keep it simple  
- Keep it observable  
- Keep it maintainable  
- Secure by default  
- Test what matters  
- Deploy with confidence  
- Code like you’ll maintain it for five years — because you probably will.
