using System.Collections.Generic;
using Steamworks;

namespace SCPCB_MultiplayerMod_CentralServer
{
    public struct Result
    {
        public SteamId Owner;
        public AuthResponse Status;
        public Result(SteamId owner, AuthResponse status)
        {
            Owner = owner;
            Status = status;
        }
    }
    public class Steam
    {

        public static Dictionary<SteamId, Result> lastResult;
        public static void SteamServerOnOnValidateAuthTicketResponse(SteamId steamID, SteamId ownerId, AuthResponse status)
        {
            lastResult[steamID] = new Result{Owner = steamID, Status = status};
        }

    }
}