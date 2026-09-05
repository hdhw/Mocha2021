using Microsoft.AspNetCore.Mvc;
using Mocha2021.Classes;
using Mocha2021.Models;

namespace Mocha2021.Controllers;

[Controller]
public class Config : ControllerBase
{
    private readonly PlayerDB _playerDb;
    private readonly ServerState _state;
    private readonly IWebHostEnvironment _env;
    private readonly List<Dictionary<string, object?>> _gameConfigs;

    public Config(PlayerDB playerDb, ServerState state, IWebHostEnvironment env)
    {
        _playerDb = playerDb;
        _state = state;
        _env = env;
        var path = Path.Combine(env.ContentRootPath, "Seed", "GameConfigs.json");
        _gameConfigs = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object?>>>(System.IO.File.ReadAllText(path))!;
    }

    private static IActionResult Ok2(Dictionary<string, object?>? extra = null)
    {
        var d = new Dictionary<string, object?> { ["status"] = "ok" };
        if (extra != null) foreach (var kv in extra) d[kv.Key] = kv.Value;
        return new OkObjectResult(d);
    }

    [HttpGet("/")]
    public IActionResult Root()
    {
        var b = ServerConfig.ServerBaseUrl;
        return new OkObjectResult(new Dictionary<string, object?>
        {
            ["Auth"] = b, ["API"] = b, ["WWW"] = b, ["Notifications"] = b, ["Images"] = $"{b}/img",
            ["CDN"] = b, ["Commerce"] = b, ["Matchmaking"] = b, ["Storage"] = b, ["Chat"] = $"{b}/chat",
            ["Leaderboard"] = b, ["Accounts"] = b, ["Link"] = b, ["RoomComments"] = b, ["Clubs"] = b,
            ["Rooms"] = $"{b}/roomserver", ["PlatformNotifications"] = $"{b}/pn", ["Moderation"] = $"{b}/mo",
            ["DataCollection"] = $"{b}/dc", ["BugReporting"] = $"{b}/br", ["Discovery"] = $"{b}/disc",
            ["Econ"] = b, ["CMS"] = $"{b}/cms", ["GameLogs"] = $"{b}/gl", ["Lists"] = $"{b}/li",
            ["PlayerSettings"] = b, ["Strings"] = $"{b}/s", ["StringsCDN"] = $"{b}/scdn", ["Studio"] = $"{b}/st",
        });
    }

    [HttpPost("/api/CampusCard/v1/UpdateAndGetSubscription")]
    public IActionResult CampusCard() => new OkObjectResult(new Dictionary<string, object?> { ["IsActive"] = false, ["ExpirationDate"] = "9999-12-31T00:00:00Z" });

    [HttpGet("/api/versioncheck/v4")]
    public IActionResult VersionCheck() => new OkObjectResult(new Dictionary<string, object?> { ["VersionStatus"] = 0 });

    [HttpGet("/api/versioncheck/islandedversions")]
    public IActionResult IslandedVersions() => Ok2(new() { ["versions"] = new List<object>(), ["isCurrentVersionIslanded"] = false });

    [HttpGet("/api/config/v2")]
    public IActionResult AppConfig()
    {
        var dailyObjectivesPath = Path.Combine(_env.ContentRootPath, "Seed", "DailyObjectives.json");
        var dailyObjectives = System.Text.Json.JsonSerializer.Deserialize<object>(System.IO.File.ReadAllText(dailyObjectivesPath));
        var levelProgression = _playerDb.LevelProgression.Select(l => new Dictionary<string, object?>
        {
            ["Level"] = l.Level, ["RequiredXp"] = l.RequiredXp, ["GiftDropId"] = l.GiftDropId,
        }).ToList();

        return new OkObjectResult(new Dictionary<string, object?>
        {
            ["ShareBaseUrl"] = ServerConfig.ShareBaseUrl,
            ["LevelProgressionMaps"] = levelProgression,
            ["DailyObjectives"] = dailyObjectives,
            ["ServerMaintenance"] = new Dictionary<string, object?> { ["StartsInMinutes"] = _state.MaintenanceMinutesRemaining() },
            ["AutoMicMutingConfig"] = new Dictionary<string, object?>
            {
                ["MicSpamVolumeThreshold"] = 1.125, ["MicVolumeSampleInterval"] = 0.25,
                ["MicVolumeSampleRollingWindowLength"] = 7, ["MicSpamSamplePercentageForWarning"] = 0.8,
                ["MicSpamSamplePercentageForWarningToEnd"] = 0.2, ["MicSpamSamplePercentageForForceMute"] = 0.8,
                ["MicSpamSamplePercentageForForceMuteToEnd"] = 0.2, ["MicSpamWarningStateVolumeMultiplier"] = 0.25,
            },
            ["RoomKeyConfig"] = new Dictionary<string, object?> { ["MaxKeysPerRoom"] = 100 },
        });
    }

    [HttpGet("/api/config/v1/amplitude")]
    public IActionResult Amplitude() => new OkObjectResult(new Dictionary<string, object?>
    {
        ["AmplitudeKey"] = "93941036bd6a7243bf5c628535f41d63", ["UseRudderStack"] = false,
        ["RudderStackKey"] = "23NiJHIgu3koaGNCZIiuYvIQNCu", ["UseStatSig"] = true,
        ["StatSigKey"] = "client-SBZkOrjD3r1Cat3f3W8K6sBd11WKlXZXIlCWj6l4Aje", ["StatSigEnvironment"] = 0,
    });

    [HttpGet("/api/gameconfigs/v1/all")]
    public IActionResult GameConfigs() => new OkObjectResult(_gameConfigs);

    [HttpGet("/config/LoadingScreenTipData")]
    public IActionResult LoadingScreenTips() => new OkObjectResult(new List<Dictionary<string, object?>>
    {
        Tip("Rec Room Tokens", "Redeem your Rec Room Tokens for all kinds of fun rewards! You can shop at the Rec Center Merch Booth or the Store section of your Watch Menu.", "TokenBin"),
        Tip("Become a Star!", "Use #Emerald on your Instagram and Twitter posts or in the Discord server for a chance to make it onto our Community Board!", "Star"),
        Tip("Find Your Style", "Personalize your outfit and appearance in your Dorm Room.", "Style"),
        Tip("Room Cheers", "Cheer and Favorite any room in the This Room section of your Watch Menu.", "RoomCheers"),
        Tip("Daily Challenges", "Check out the Challenges section in your watch for fun ways to earn in-game rewards.", "DailyChallenges"),
        Tip("Welcome to Emerald", "Emerald is a revival of 2021 Rec Room where you can create and play games with friends. It's a faithful continuation of the original game, kept alive after it shut down in June 2026!", "Welcome"),
        Tip("Join the Emerald Community!", "Chat with other like-minded community members by joining the Discord server!", "Community"),
        Tip("Laser Tag Merch", "You earn tickets for every game of Laser Tag. Redeem them for awesome Laser Tag gear!", "TagMerch"),
        Tip("We're all on Rec.Net!", "Log into your Rec.Net profile to stay in touch with your friends any time!", "RecNet"),
        Tip("Check out clubs!", "Create or join a club. Visit all the clubhouses and even set one as your spawn point!", "Clubs"),
        Tip("Hair Dye Patterns", "Express yourself by mixing and matching colors with the new hair dye patterns!", "HairDye"),
        Tip("Georgie..", "We're looking for brisket.", "Brisket"),
        Tip("Did you know..", "Your account password in Emerald is secured using bcrypt, so nobody, not even staff can access your account!", "Locked"),
        Tip("cool loading screen", "check this out", ""),
    });

    private static Dictionary<string, object?> Tip(string title, string message, string image) => new()
    {
        ["PlatformMask"] = -1, ["Title"] = title, ["Message"] = message, ["RoomNames"] = new List<object>(), ["ImageName"] = image,
    };

    [HttpGet("/api/testcasemanagement/v1/testpasssummary")]
    public IActionResult TestPassSummary() => new OkObjectResult(new List<object>());

    [HttpGet("/api/testcasemanagement/v1/testpass/{testPassId:int}")]
    public IActionResult TcmGetPass(int testPassId) => new NotFoundObjectResult(new Dictionary<string, object?> { ["error"] = "not_found", ["error_description"] = "test pass not found" });

    [HttpGet("/api/testcasemanagement/v1/testcase/{testCaseId}")]
    public IActionResult TcmGetCase(string testCaseId) => new NotFoundObjectResult(new Dictionary<string, object?> { ["error"] = "not_found", ["error_description"] = "test case not found" });

    [HttpGet("/ai/v1/status")]
    public IActionResult AiStatus() => Ok2(new() { ["available"] = true, ["features"] = new[] { "avatarGen", "roomAssist" } });

    [HttpGet("/ns/v1/resolve")]
    public IActionResult NsResolve([FromQuery] string? name)
    {
        name ??= "";
        return Ok2(new() { ["name"] = name, ["resolved"] = true, ["target"] = $"^rr~{name}" });
    }

    [HttpGet("/api/search/v1/")]
    public IActionResult Search() => new OkObjectResult(new Dictionary<string, object?> { ["Results"] = new List<object>(), ["TotalResults"] = 0 });

    [HttpGet("/api/inventions/v2/mine")]
    public IActionResult InventionsMine() => new OkObjectResult(new List<object>());

    [HttpGet("/api/inventions/v1/room")]
    public IActionResult InventionsRoom() => new OkObjectResult(new List<object>());

    [HttpGet("/api/inventions/inventionsbycreators/{creator}/{page}/{count}")]
    public IActionResult InventionsByCreators(string creator, string page, string count) => new OkObjectResult(new List<object>());

    [HttpGet("/api/communityboard/v2/current")]
    public IActionResult CommunityBoardCurrent() => new OkObjectResult(new Dictionary<string, object?>
    {
        ["FeaturedPlayer"] = new Dictionary<string, object?> { ["Id"] = 1, ["TitleOverride"] = null, ["UrlOverride"] = null },
        ["FeaturedRoomGroup"] = new Dictionary<string, object?>
        {
            ["FeaturedRoomGroupId"] = 0, ["Name"] = "Featured Rooms",
            ["Rooms"] = new List<Dictionary<string, object?>> { new() { ["RoomName"] = "RecCenter", ["RoomId"] = 7716, ["ImageName"] = "RecCenter" } },
        },
        ["CurrentAnnouncement"] = new Dictionary<string, object?>
        {
            ["Message"] = "Welcome to Emerald! Have fun and be sure to follow the rules.", ["MoreInfoUrl"] = "",
        },
        ["InstagramImages"] = new List<Dictionary<string, object?>>
        {
            new() { ["ImageName"] = "ComeOutside", ["ImageUrl"] = $"{ServerConfig.ServerBaseUrl}/img/ComeOutside" },
            new() { ["ImageName"] = "borse", ["ImageUrl"] = $"{ServerConfig.ServerBaseUrl}/img/borse" },
        },
        ["Videos"] = new List<Dictionary<string, object?>>
        {
            new()
            {
                ["BlobName"] = "VideoData24f0908d-9edc-4dcb-b853-cd6c820e25726943679983027471793",
                ["Title"] = "Emerald - Welcome!",
                ["Description"] = "The revival for 2021 Rec Room where you can play, create, and connect.",
                ["ThumbnailBlobName"] = "ImageData350f571e-8323-4498-ad34-2d9678d5b5841454021500552003911",
                ["SourceUrl"] = "https://youtu.be/uo_m8hJUQVU",
            },
            new()
            {
                ["BlobName"] = "endcredits",
                ["Title"] = "Rec Room End Credits",
                ["Description"] = "Rec Room closed down on June 1st 2026 at noon Pacific time, after ten years of providing a fun and welcoming place for players from all around the world to play, create, and hang out together.",
                ["ThumbnailBlobName"] = "maxresdefault",
                ["SourceUrl"] = "https://www.youtube.com/watch?v=VWBgDwo59Po",
            },
        },
    });

    [HttpGet("/api/announcement/v1/get")]
    public IActionResult AnnouncementGet()
    {
        var rows = _playerDb.GetAnnouncements();
        return new OkObjectResult(rows.Select(r => new Dictionary<string, object?>
        {
            ["AnnouncementId"] = r.AnnouncementId, ["AnnouncementType"] = r.AnnouncementType, ["Title"] = r.Title,
            ["Body"] = r.Body, ["ImageName"] = r.ImageName ?? "", ["LinkType"] = r.LinkType, ["LinkName"] = r.LinkName,
            ["LinkUri"] = r.LinkUri, ["Platform"] = r.Platform, ["CreatedAt"] = r.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.ffffff") + "7+00:00",
        }).ToList());
    }

    [HttpPost("/api/announcement/v1/create")]
    public IActionResult CreateAnnouncement() => new OkObjectResult(new Dictionary<string, object?> { ["status"] = "success" });
}
