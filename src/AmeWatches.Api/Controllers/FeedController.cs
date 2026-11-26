using AmeWatches.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AmeWatches.Api.Controllers;

[ApiController]
public class FeedController : ControllerBase
{
    private readonly IMetaFeedService _feed;
    private readonly IConfiguration _config;

    public FeedController(IMetaFeedService feed, IConfiguration config)
    {
        _feed = feed;
        _config = config;
    }

    /// <summary>Lista publike e orëve për Meta Commerce Manager (Data sources → Data feed → Use a URL).</summary>
    [HttpGet("api/feed/meta.csv")]
    public async Task<IActionResult> Meta()
    {
        // Domeni publik i faqes (te serveri vendoset me Email__PublicBaseUrl); rezervë: adresa e kërkesës.
        var baseUrl = _config["Email:PublicBaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
            baseUrl = $"{Request.Scheme}://{Request.Host}";

        Response.Headers.CacheControl = "public, max-age=300";
        return Content(await _feed.BuildCsv(baseUrl), "text/csv; charset=utf-8");
    }
}
