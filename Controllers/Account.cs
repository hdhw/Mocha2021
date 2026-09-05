using Microsoft.AspNetCore.Mvc;
using Mocha2021.Classes;
using Mocha2021.Hub;
using Mocha2021.Models;

namespace Mocha2021.Controllers;

[Controller]
public class Account : ControllerBase
{
    private readonly PlayerDB _playerDb;
    private readonly SessionManager _sessions;
    private readonly HubState _hub;

    public Account(PlayerDB playerDb, SessionManager sessions, HubState hub)
    {
        _playerDb = playerDb;
        _sessions = sessions;
        _hub = hub;
    }

    private int CurrentAccountId() => _sessions.CurrentAccountId(Request.Headers.Authorization.ToString());

    private static Dictionary<string, object?> AccountToJson(Models.Account acct, bool full = false)
    {
        var d = new Dictionary<string, object?>
        {
            ["accountId"] = acct.AccountId,
            ["username"] = acct.Username,
            ["displayName"] = acct.DisplayName,
            ["profileImage"] = acct.ProfileImage,
            ["isJunior"] = acct.IsJunior,
            ["platforms"] = full ? -1 : PlayerDB.PlatformEnum.GetValueOrDefault(acct.Platform ?? "", 0),
            ["createdAt"] = acct.CreatedAt.ToString("o"),
            ["personalPronouns"] = 0,
            ["identityFlags"] = 0,
            ["displayEmoji"] = null,
        };
        if (full)
        {
            d["email"] = acct.Email;
            d["phone"] = null;
            d["juniorState"] = 0;
            d["parentAccountId"] = null;
            d["availableUsernameChanges"] = acct.AvailableUsernameChanges;
            d["birthday"] = acct.Birthday?.ToString("o");
        }
        return d;
    }

    [HttpGet("/account/me")]
    public IActionResult Me()
    {
        var acct = _playerDb.GetAccount(CurrentAccountId());
        if (acct == null) return new OkObjectResult(null);
        return new OkObjectResult(AccountToJson(acct, full: true));
    }

    [HttpPut("/account/me/birthday")]
    public IActionResult SetBirthday()
    {
        var accountId = CurrentAccountId();
        string? birthdayRaw = Request.HasFormContentType ? Request.Form["birthday"].ToString() : null;
        if (string.IsNullOrEmpty(birthdayRaw))
        {
            var body = ReadJsonBody();
            birthdayRaw = body?.GetValueOrDefault("birthday")?.ToString();
        }
        if (string.IsNullOrEmpty(birthdayRaw))
            return RecNetResult(false, error: "birthday is required", status: 400);

        if (!DateTime.TryParse(birthdayRaw.Replace("Z", ""), out var birthDate))
            return RecNetResult(false, error: "birthday is invalid", status: 400);

        var today = DateTime.UtcNow;
        var age = today.Year - birthDate.Year;
        if ((today.Month, today.Day).CompareTo((birthDate.Month, birthDate.Day)) < 0) age--;
        var isJunior = age < 13;

        var acct = _playerDb.GetAccount(accountId);
        if (acct == null) return RecNetResult(false, error: "not_found", status: 404);
        acct.Birthday = birthDate;
        acct.IsJunior = isJunior;
        _playerDb.SaveAccount(acct);

        if (isJunior) _playerDb.AddRole(accountId, "junior");
        else _playerDb.RemoveRole(accountId, "junior");
        _playerDb.SetAccountCreationFlags(accountId, hasFinished: true);

        acct = _playerDb.GetAccount(accountId)!;
        return RecNetResult(true, value: AccountCreatePayload(acct));
    }

    private static Dictionary<string, object?> AccountCreatePayload(Models.Account acct) => new()
    {
        ["accountId"] = acct.AccountId,
        ["profileImage"] = acct.ProfileImage ?? "DefaultImgCOLOR",
        ["isJunior"] = acct.IsJunior,
        ["platforms"] = -1,
        ["username"] = acct.Username,
        ["displayName"] = acct.DisplayName,
        ["createdAt"] = acct.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.ffffff") + "0Z",
        ["personalPronouns"] = 0,
        ["identityFlags"] = 0,
    };

    [HttpGet("/account/{accountId:int}/bio")]
    public IActionResult GetBio(int accountId)
    {
        var acct = _playerDb.GetAccount(accountId);
        if (acct == null) return new ObjectResult(new Dictionary<string, object?> { ["error"] = "not_found", ["error_description"] = "no account with that id" }) { StatusCode = 404 };
        return new OkObjectResult(new Dictionary<string, object?> { ["accountId"] = acct.AccountId, ["bio"] = acct.Bio ?? "" });
    }

    [HttpPut("/account/me/profileimage")]
    public async Task<IActionResult> SetProfileImage()
    {
        var accountId = CurrentAccountId();
        string? imageName = Request.HasFormContentType ? (Request.Form["imageName"].FirstOrDefault() ?? Request.Form["ImageName"].FirstOrDefault()) : null;
        if (string.IsNullOrEmpty(imageName))
        {
            var body = ReadJsonBody();
            imageName = body?.GetValueOrDefault("imageName")?.ToString() ?? body?.GetValueOrDefault("ImageName")?.ToString();
        }
        if (string.IsNullOrEmpty(imageName))
            return new ObjectResult(new Dictionary<string, object?> { ["error"] = "invalid_request", ["error_description"] = "no imageName provided" }) { StatusCode = 400 };

        var acct = _playerDb.GetAccount(accountId);
        if (acct == null) return RecNetResult(false);
        acct.ProfileImage = imageName;
        _playerDb.SaveAccount(acct);

        await _hub.PushToAccountAsync(accountId, "AccountUpdate", new Dictionary<string, object?>
        {
            ["accountId"] = acct.AccountId,
            ["createdAt"] = acct.CreatedAt.ToString("o"),
            ["displayName"] = acct.DisplayName,
            ["username"] = acct.Username,
            ["profileImage"] = imageName,
            ["isJunior"] = acct.IsJunior ?? false,
            ["platformMask"] = 0,
        });
        return RecNetResult(true);
    }

    [HttpPut("/account/me/displayname")]
    public async Task<IActionResult> SetDisplayName()
    {
        var accountId = CurrentAccountId();
        string? displayName = Request.HasFormContentType ? Request.Form["displayName"].ToString() : null;
        if (string.IsNullOrEmpty(displayName))
        {
            var body = ReadJsonBody();
            displayName = body?.GetValueOrDefault("displayName")?.ToString();
        }

        var acct = _playerDb.GetAccount(accountId);
        if (acct == null) return RecNetResult(false);
        acct.DisplayName = displayName ?? acct.DisplayName;
        _playerDb.SaveAccount(acct);

        var payload = new Dictionary<string, object?>
        {
            ["accountId"] = acct.AccountId,
            ["createdAt"] = acct.CreatedAt.ToString("o"),
            ["displayName"] = displayName,
            ["username"] = acct.Username,
            ["profileImage"] = acct.ProfileImage,
            ["isJunior"] = acct.IsJunior ?? false,
            ["platformMask"] = 0,
        };
        await _hub.PushToAccountAsync(accountId, "AccountUpdate", payload);
        await _hub.PushToAccountAsync(accountId, "SelfAccountUpdate", payload);
        return RecNetResult(true);
    }

    [HttpPut("/account/me/username")]
    public async Task<IActionResult> SetUsername()
    {
        var accountId = CurrentAccountId();
        string? newUsername = Request.HasFormContentType ? Request.Form["username"].ToString() : null;
        if (string.IsNullOrEmpty(newUsername))
        {
            var body = ReadJsonBody();
            newUsername = body?.GetValueOrDefault("username")?.ToString();
        }
        if (string.IsNullOrEmpty(newUsername)) return RecNetResult(false, error: "invalid_request");

        var account = _playerDb.GetAccount(accountId);
        if (account == null) return RecNetResult(false, error: "not_found");
        if (account.AvailableUsernameChanges <= 0) return RecNetResult(false, error: "no_username_changes_remaining");

        var taken = _playerDb.GetAccountByUsername(newUsername);
        if (taken != null && taken.AccountId != accountId) return RecNetResult(false, error: "username_taken");

        account.Username = newUsername;
        account.AvailableUsernameChanges -= 1;
        _playerDb.SaveAccount(account);
        _playerDb.SetAccountCreationFlags(accountId, hasStarted: true, hasChosenUsername: true);

        account = _playerDb.GetAccount(accountId)!;
        var payload = new Dictionary<string, object?>
        {
            ["email"] = null,
            ["phone"] = null,
            ["juniorState"] = 0,
            ["parentAccountId"] = null,
            ["availableUsernameChanges"] = account.AvailableUsernameChanges,
            ["birthday"] = null,
            ["accountId"] = accountId,
            ["profileImage"] = account.ProfileImage ?? "DefaultImgCOLOR",
            ["isJunior"] = account.IsJunior,
            ["platforms"] = PlayerDB.PlatformEnum.GetValueOrDefault(account.Platform ?? "", -1),
            ["username"] = newUsername,
            ["displayName"] = account.DisplayName,
            ["createdAt"] = account.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.ffffff"),
            ["personalPronouns"] = account.PersonalPronouns,
            ["identityFlags"] = account.IdentityFlags,
        };
        await _hub.PushToAccountAsync(accountId, "AccountUpdate", payload);
        await _hub.PushToAccountAsync(accountId, "SelfAccountUpdate", payload);
        return RecNetResult(true, value: payload);
    }

    [HttpPut("/account/me/bio")]
    public IActionResult SetBio()
    {
        var accountId = CurrentAccountId();
        string? bio = Request.HasFormContentType ? Request.Form["bio"].ToString() : null;
        if (bio == null)
        {
            var body = ReadJsonBody();
            bio = body?.GetValueOrDefault("bio")?.ToString();
        }
        var acct = _playerDb.GetAccount(accountId);
        if (acct != null)
        {
            acct.Bio = bio;
            _playerDb.SaveAccount(acct);
        }
        return RecNetResult(true);
    }

    [HttpGet("/account/bulk")]
    public IActionResult Bulk([FromQuery(Name = "id")] List<string>? id)
    {
        var ids = id ?? new List<string>();
        if (ids.Count == 0) return new OkObjectResult(new List<object>());
        var accounts = _playerDb.GetAccountsByIds(ids.Select(int.Parse)).ToDictionary(a => a.AccountId);
        var result = ids.Select(idStr =>
        {
            var aid = int.Parse(idStr);
            if (accounts.TryGetValue(aid, out var row)) return AccountToJson(row);
            return new Dictionary<string, object?>
            {
                ["accountId"] = aid, ["profileImage"] = null, ["isJunior"] = false,
                ["platforms"] = -1, ["username"] = $"Player{aid}", ["displayName"] = $"Player{aid}",
                ["createdAt"] = "2021-01-01T00:00:00", ["personalPronouns"] = 0,
                ["identityFlags"] = 0, ["displayEmoji"] = null,
            };
        }).ToList();
        return new OkObjectResult(result);
    }

    [HttpGet("/account/me/haspassword")]
    public IActionResult HasPassword() => new OkObjectResult(new Dictionary<string, object?> { ["hasPassword"] = true });

    [HttpGet("/Accounts/parentalcontrol/me")]
    public IActionResult ParentalControlMe() =>
        new OkObjectResult(new Dictionary<string, object?> { ["status"] = "ok", ["controls"] = new Dictionary<string, object?> { ["chatEnabled"] = true, ["voiceEnabled"] = true, ["maxRating"] = "Everyone" } });

    [HttpGet("/parentalcontrol/me")]
    public IActionResult ParentalControl() => new OkObjectResult(new Dictionary<string, object?> { ["accountId"] = 1, ["disallowInAppPurchases"] = false });

    [HttpGet("/subscription/details/{id:int}")]
    public IActionResult SubscriptionDetails(int id) =>
        new OkObjectResult(new Dictionary<string, object?> { ["accountId"] = id, ["clubId"] = 0, ["subscriberCount"] = 0 });

    [HttpGet("/role/moderator/{accountId:int}")]
    public IActionResult RoleModerator(int accountId) => new OkObjectResult(_playerDb.HasRole(accountId, "moderator"));

    [HttpGet("/role/developer/{accountId:int}")]
    public IActionResult RoleDeveloper(int accountId) => new OkObjectResult(_playerDb.HasRole(accountId, "developer"));

    private Dictionary<string, object?>? ReadJsonBody()
    {
        try
        {
            Request.Body.Position = 0;
        }
        catch { }
        using var reader = new StreamReader(Request.Body);
        var text = reader.ReadToEndAsync().GetAwaiter().GetResult();
        if (string.IsNullOrWhiteSpace(text)) return null;
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(text);
        }
        catch { return null; }
    }

    private IActionResult RecNetResult(bool success, string? error = null, object? value = null, int status = 200)
    {
        var body = new Dictionary<string, object?> { ["success"] = success, ["error"] = error ?? "" };
        if (value != null) body["value"] = value;
        return new ObjectResult(body) { StatusCode = status };
    }
}
