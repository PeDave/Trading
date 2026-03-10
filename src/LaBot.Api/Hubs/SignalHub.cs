using LaBot.Api.Models.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;

namespace LaBot.Api.Hubs;

[Authorize]
public class SignalHub : Hub
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<SignalHub> _logger;

    public SignalHub(UserManager<ApplicationUser> userManager, ILogger<SignalHub> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (userId != null)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, GetTierGroupName(user.SubscriptionTier));
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");

                // Add to all lower tier groups as well (Pro gets Free content, ProPlus gets Free+Pro)
                if (user.SubscriptionTier >= SubscriptionTier.Pro)
                    await Groups.AddToGroupAsync(Context.ConnectionId, GetTierGroupName(SubscriptionTier.Free));
                if (user.SubscriptionTier >= SubscriptionTier.ProPlus)
                    await Groups.AddToGroupAsync(Context.ConnectionId, GetTierGroupName(SubscriptionTier.Pro));

                // Admins get everything
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("Admin"))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, "admin");
                    foreach (SubscriptionTier tier in Enum.GetValues<SubscriptionTier>())
                        await Groups.AddToGroupAsync(Context.ConnectionId, GetTierGroupName(tier));
                }

                _logger.LogInformation("User {UserId} (tier: {Tier}) connected to SignalHub", userId, user.SubscriptionTier);
            }
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        _logger.LogInformation("User {UserId} disconnected from SignalHub", userId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinTierGroup(string tierName)
    {
        if (Enum.TryParse<SubscriptionTier>(tierName, out var tier))
        {
            var userId = Context.UserIdentifier;
            if (userId != null)
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null && user.SubscriptionTier >= tier)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, GetTierGroupName(tier));
                    await Clients.Caller.SendAsync("JoinedGroup", GetTierGroupName(tier));
                }
            }
        }
    }

    public async Task LeaveTierGroup(string tierName)
    {
        if (Enum.TryParse<SubscriptionTier>(tierName, out var tier))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetTierGroupName(tier));
            await Clients.Caller.SendAsync("LeftGroup", GetTierGroupName(tier));
        }
    }

    // Called by server to broadcast a signal update to appropriate tier groups
    public static string GetTierGroupName(SubscriptionTier tier) => $"tier:{tier.ToString().ToLower()}";
}
