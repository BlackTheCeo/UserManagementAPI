using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 1. Global Exception-Handling Middleware (Added via Copilot debugging)
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Unhandled exception occurred while processing request.");
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error = "Internal server error." });
    }
});

// 2. Custom Request/Response Logging Middleware
app.Use(async (context, next) =>
{
    var startTime = DateTime.UtcNow;
    app.Logger.LogInformation("Incoming Request: {Method} {Path}", context.Request.Method, context.Request.Path);

    await next();

    var duration = DateTime.UtcNow - startTime;
    app.Logger.LogInformation(
        "Outgoing Response: {Method} {Path} -> {StatusCode} ({Duration}ms)",
        context.Request.Method,
        context.Request.Path,
        context.Response.StatusCode,
        duration.TotalMilliseconds);
});

// 3. Simple API Key / Token Authentication Middleware (Optional header check for mutating routes)
app.Use(async (context, next) =>
{
    // Allow Swagger and GET requests without token for easy peer testing; require X-Api-Key for write operations
    if (HttpMethods.IsPost(context.Request.Method) ||
        HttpMethods.IsPut(context.Request.Method) ||
        HttpMethods.IsDelete(context.Request.Method))
    {
        if (!context.Request.Headers.TryGetValue("X-Api-Key", out var apiKey) || string.IsNullOrWhiteSpace(apiKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Missing or invalid X-Api-Key header." });
            return;
        }
    }

    await next();
});

// Thread-safe in-memory store (Fixed race condition from plain List<User> during Copilot debugging)
var users = new ConcurrentDictionary<int, User>();
users.TryAdd(1, new User { Id = 1, Name = "Alice Johnson", Email = "alice@example.com", Role = "Admin" });
users.TryAdd(2, new User { Id = 2, Name = "Bob Smith", Email = "bob@example.com", Role = "Developer" });
var nextId = 2;

// Validation helper
static List<string> ValidateUser(UserDto dto)
{
    var errors = new List<string>();

    if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Trim().Length < 2)
        errors.Add("Name is required and must be at least 2 characters.");

    if (string.IsNullOrWhiteSpace(dto.Email) || !Regex.IsMatch(dto.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        errors.Add("A valid Email address is required.");

    if (string.IsNullOrWhiteSpace(dto.Role))
        errors.Add("Role is required.");

    return errors;
}

// CRUD ENDPOINTS

// GET: Retrieve all users
app.MapGet("/users", () =>
{
    return Results.Ok(users.Values.OrderBy(u => u.Id));
});

// GET: Retrieve a single user by ID
app.MapGet("/users/{id:int}", (int id) =>
{
    return users.TryGetValue(id, out var user)
        ? Results.Ok(user)
        : Results.NotFound(new { error = $"User with ID {id} not found." });
});

// POST: Create a new user
app.MapPost("/users", (UserDto newUser) =>
{
    var validationErrors = ValidateUser(newUser);
    if (validationErrors.Count > 0)
        return Results.BadRequest(new { errors = validationErrors });

    if (users.Values.Any(u => u.Email.Equals(newUser.Email.Trim(), StringComparison.OrdinalIgnoreCase)))
        return Results.Conflict(new { error = "A user with this email already exists." });

    var id = Interlocked.Increment(ref nextId);
    var createdUser = new User
    {
        Id = id,
        Name = newUser.Name.Trim(),
        Email = newUser.Email.Trim(),
        Role = newUser.Role.Trim()
    };

    users[id] = createdUser;
    return Results.Created($"/users/{id}", createdUser);
});

// PUT: Update an existing user
app.MapPut("/users/{id:int}", (int id, UserDto updatedUser) =>
{
    if (!users.ContainsKey(id))
        return Results.NotFound(new { error = $"User with ID {id} not found." });

    var validationErrors = ValidateUser(updatedUser);
    if (validationErrors.Count > 0)
        return Results.BadRequest(new { errors = validationErrors });

    var user = new User
    {
        Id = id,
        Name = updatedUser.Name.Trim(),
        Email = updatedUser.Email.Trim(),
        Role = updatedUser.Role.Trim()
    };

    users[id] = user;
    return Results.Ok(user);
});

// DELETE: Remove a user by ID
app.MapDelete("/users/{id:int}", (int id) =>
{
    return users.TryRemove(id, out _)
        ? Results.NoContent()
        : Results.NotFound(new { error = $"User with ID {id} not found." });
});

app.Run();

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class UserDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty;
}
