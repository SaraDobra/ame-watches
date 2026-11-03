using Microsoft.AspNetCore.Identity;

namespace AmeWatches.Api.Data.Entities;

public class AppUser : IdentityUser<int>
{
    public string FullName { get; set; } = default!;
}
