using Grpc.Core;
using Userservice;
using ZOVserver.Shared.Contracts.Helper;
using ZOVserver.Shared.TitanRemnants.Mathem.Shared;

namespace ZOVserver.AdminPanel.Backend.Services;

public class UserManagementService(ILogger<UserManagementService> logger) : UserManagement.UserManagementBase
{
    public override async Task<UserResponse> BanUser(BanUserRequest request, ServerCallContext context)
    {
        var playerId = new LogicLong(request.HighLowId.High, request.HighLowId.Low);
        var playerSession = ClientHelper.GetPlayerSession(playerId);

        if (request.UnbanTimestamp == 0)
            request.UnbanTimestamp = DateTimeOffset.UtcNow.AddYears(300).ToUnixTimeSeconds();

        var endTime = DateTimeOffset.FromUnixTimeSeconds(request.UnbanTimestamp).DateTime;

        await playerSession.BanAccountAsync(request.Reason, endTime);

        logger.LogInformation("Ban user: {user}, reason: {reason}, end time: {end}.", request.UserId, request.Reason,
            endTime);

        return new UserResponse
        {
            Success = true,
            Message = $"User {request.UserId} banned successfully until {endTime}!"
        };
    }

    public override async Task<LockUserResponse> LockUserAccount(LockUserRequest request, ServerCallContext context)
    {
        var playerId = new LogicLong(request.HighLowId.High, request.HighLowId.Low);
        var playerSession = ClientHelper.GetPlayerSession(playerId);

        var recoveryCode = string.IsNullOrEmpty(request.RecoveryCode) || request.RecoveryCode.Length != 12
            ? GenerateRecoveryCode()
            : request.RecoveryCode;

        await playerSession.LockAccountAsync(recoveryCode);

        logger.LogInformation("Lock user: {user}, unlock code: {code}.", request.UserId, recoveryCode);

        return new LockUserResponse
        {
            Success = true,
            Message = $"User account {request.UserId} locked successfully!",
            RecoveryCode = recoveryCode
        };
    }

    public override async Task<UserResponse> UnlockUserAccount(UserIdRequest request, ServerCallContext context)
    {
        var playerId = new LogicLong(request.HighLowId.High, request.HighLowId.Low);
        var playerSession = ClientHelper.GetPlayerSession(playerId);

        await playerSession.UnbanAndUnlockAccount();
        logger.LogInformation("Unban And Unlock user account: {user}.", request.UserId);

        return new UserResponse
        {
            Success = true,
            Message = $"User account {request.UserId} unbanned and unlocked successfully!"
        };
    }

    private static string GenerateRecoveryCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, 12).Select(s => s[Random.Shared.Next(s.Length)]).ToArray());
    }
}