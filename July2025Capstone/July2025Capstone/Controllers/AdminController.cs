using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using July2025Capstone.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace July2025Capstone.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IWebHostEnvironment _env;

    public AdminController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IWebHostEnvironment env)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
        _env = env;
    }

    // ========= DTOs =========
    public record OverviewDto(
        int totalUsers,
        int activeToday,
        string uptime,
        int apiLatencyMs,
        string chatbotModel,
        int dailyLimit,
        int todayRequests);

    public record UserRow(
        string Id,
        string Username,
        string Email,
        string Role,
        DateTime? LastLoginUtc,
        string Status);

    public record LogRow(DateTime WhenUtc, string Message, string Level);
    public record DayPoint(DateTime day, int requests);
    public record ErrorBucket(string code, int count);
    public record MetricsDto(List<DayPoint> usage, List<ErrorBucket> errors);

    public record StatusDto(string status);
    public record RoleDto(string role);

    // ========= HELPERS =========
    private static string NormalizeStatus(ApplicationUser u)
        => string.IsNullOrWhiteSpace(u.Status)
            ? (u.EmailConfirmed ? "Active" : "Suspended")
            : u.Status;

    private async Task<bool> IsLastAdminAsync(string userId)
    {
        var adminRole = await _roleManager.FindByNameAsync("Admin");
        if (adminRole == null) return false;

        var adminIds = await _db.UserRoles
            .Where(ur => ur.RoleId == adminRole.Id)
            .Select(ur => ur.UserId)
            .ToListAsync();

        // if only one admin and it's this user -> last admin
        return adminIds.Count == 1 && adminIds[0] == userId;
    }

    // ========= READ ENDPOINTS =========

    [HttpGet("overview")]
    public async Task<ActionResult<OverviewDto>> GetOverview()
    {
        var todayUtc = DateTime.UtcNow.Date;
        var totalUsers = await _db.Users.CountAsync();

        var activeToday = await _db.Users
            .Where(u => u.LastLoginUtc != null &&
                        u.LastLoginUtc.Value.Date == todayUtc)
            .CountAsync();

        var dto = new OverviewDto(
            totalUsers: totalUsers,
            activeToday: activeToday,
            uptime: "99.9%",
            apiLatencyMs: 320,
            chatbotModel: "gpt-4o-mini",
            dailyLimit: 500,
            todayRequests: Math.Min(500, activeToday * 3)
        );

        return Ok(dto);
    }

    [HttpGet("users")]
    public async Task<ActionResult<IEnumerable<UserRow>>> GetUsers()
    {
        var roles = await (from ur in _db.UserRoles
                           join r in _db.Roles on ur.RoleId equals r.Id
                           select new { ur.UserId, r.Name })
                           .ToListAsync();

        var firstRole = roles
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.First().Name);

        var users = await _db.Users
            .AsNoTracking()
            .OrderBy(u => u.UserName)
            .ToListAsync();

        var result = users.Select(u => new UserRow(
            u.Id,
            u.UserName ?? string.Empty,
            u.Email ?? string.Empty,
            firstRole.TryGetValue(u.Id, out var role) ? (role ?? "User") : "User",
            u.LastLoginUtc,
            NormalizeStatus(u)
        ));

        return Ok(result);
    }

    [HttpGet("logs")]
    public async Task<ActionResult<IEnumerable<LogRow>>> GetLogs()
    {
        var recent = await _db.Users
            .AsNoTracking()
            .Where(u => u.LastLoginUtc != null)
            .OrderByDescending(u => u.LastLoginUtc)
            .Take(15)
            .Select(u => new LogRow(
                u.LastLoginUtc!.Value,
                "User " + (u.Email ?? u.UserName ?? "(unknown)") + " logged in.",
                "Info"))
            .ToListAsync();

        if (recent.Count == 0)
        {
            var now = DateTime.UtcNow;
            recent = new List<LogRow>
            {
                new LogRow(now.AddMinutes(-5),  "User admin@admin.com logged in.", "Info"),
                new LogRow(now.AddMinutes(-20), "Chat API 401 (missing key).",     "Warn"),
                new LogRow(now.AddMinutes(-35), "Records uploaded by test.",       "Info"),
                new LogRow(now.AddMinutes(-60), "DB connection pool spike.",       "Warn")
            };
        }

        return Ok(recent);
    }

    [HttpGet("metrics")]
    public ActionResult<MetricsDto> GetMetrics()
    {
        var usage = Enumerable.Range(0, 7)
            .Select(i => new DayPoint(DateTime.UtcNow.Date.AddDays(-i), 120 + (i * 7)))
            .OrderBy(p => p.day)
            .ToList();

        var errors = new List<ErrorBucket>
        {
            new ErrorBucket("200 OK", 912),
            new ErrorBucket("4xx", 37),
            new ErrorBucket("5xx", 8)
        };

        return Ok(new MetricsDto(usage, errors));
    }

    // ========= WRITE ENDPOINTS (needed by your Admin.razor) =========

    // DELETE /api/admin/users/{id}
    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var currentUserId = _userManager.GetUserId(User);
        if (currentUserId == user.Id)
            return BadRequest("You cannot delete your own account.");

        if (await IsLastAdminAsync(user.Id))
            return BadRequest("You cannot delete the last admin.");

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
            return Problem(string.Join("; ", result.Errors.Select(e => e.Description)), statusCode: 400);

        return NoContent();
    }

    // POST /api/admin/users/{id}/status { status: "Active" | "Suspended" }
    [HttpPost("users/{id}/status")]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] StatusDto body)
    {
        if (string.IsNullOrWhiteSpace(body?.status))
            return BadRequest("Status is required.");

        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        user.Status = body.status.Trim();
        await _db.SaveChangesAsync();
        return Ok(new { user.Id, user.Status });
    }

    // POST /api/admin/users/{id}/role { role: "Admin" | "User" }
    [HttpPost("users/{id}/role")]
    public async Task<IActionResult> UpdateRole(string id, [FromBody] RoleDto body)
    {
        var requestedRole = body?.role?.Trim();
        if (string.IsNullOrWhiteSpace(requestedRole))
            return BadRequest("Role is required.");

        if (!await _roleManager.RoleExistsAsync(requestedRole))
            return BadRequest($"Role '{requestedRole}' does not exist.");

        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var currentRoles = await _userManager.GetRolesAsync(user);

        // Prevent demoting the last admin
        if (currentRoles.Contains("Admin") && requestedRole != "Admin" && await IsLastAdminAsync(user.Id))
            return BadRequest("You cannot demote the last admin.");

        // Remove all known roles we care about, then add the requested one
        foreach (var r in currentRoles)
            await _userManager.RemoveFromRoleAsync(user, r);

        var addResult = await _userManager.AddToRoleAsync(user, requestedRole);
        if (!addResult.Succeeded)
            return Problem(string.Join("; ", addResult.Errors.Select(e => e.Description)), statusCode: 400);

        return Ok(new { user.Id, role = requestedRole });
    }

    // ===== DEV ONLY: seed demo activity =====
    [HttpPost("debug/seed-activity")]
    public async Task<ActionResult<object>> SeedActivityForDev()
    {
        if (!_env.IsDevelopment())
            return Forbid();

        var now = DateTime.UtcNow;
        var users = await _db.Users.ToListAsync();
        int updated = 0;

        foreach (var u in users)
        {
            bool changed = false;

            if (u.LastLoginUtc == null)
            {
                u.LastLoginUtc = now.AddMinutes(-5 - updated * 3);
                changed = true;
            }

            var normalized = NormalizeStatus(u);
            if (!string.Equals(u.Status, normalized, StringComparison.Ordinal))
            {
                u.Status = normalized;
                changed = true;
            }

            if (changed) updated++;
        }

        if (updated > 0)
            await _db.SaveChangesAsync();

        return Ok(new { updated });
    }
}
