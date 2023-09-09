using BasketballWC.Data;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace BasketballWC.Services;

public class PlayersService : PlayerService.PlayerServiceBase {
    private readonly AppDbContext context;

    public PlayersService(AppDbContext context)
    {
        this.context = context;
    }

    public override async Task<CreatePlayerResponse> CreatePlayer(CreatePlayerRequest request, ServerCallContext callContext) {
        Models.Player player = new()
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Age = request.Age,
            Position = request.Position,
            Club = request.Club
        };

        Models.Team team = await context.Teams.FirstOrDefaultAsync(x => x.Id == request.TeamId) ?? throw new ArgumentNullException("Invaild id given");
        player.Team = team;

        await context.AddAsync(player);
        await context.SaveChangesAsync();

        return new CreatePlayerResponse {Id = player.Id};
    }

    public override async Task<ReadPlayerResponse> ReadPlayer(ReadPlayerRequest request, ServerCallContext callContext) {
        Models.Player player = await context.Players
                                    .Include(player => player.Team)
                                    .FirstOrDefaultAsync(x => x.Id == request.Id)
                                    ?? throw new RpcException(new Status(StatusCode.NotFound, "There is no player with given Id"));
    
        ReadPlayerResponse response = ConvertPlayerModelToProto(player);

        return response;                   
    }

    public override async Task<ReadAllPlayersResponse> ReadAllPlayers(ReadAllPlayersRequest request, ServerCallContext callContext) {
        List<Models.Player> players = await context.Players
                                            .Include(x => x.Team)
                                            .ToListAsync();

        ReadAllPlayersResponse response = new();
        foreach(Models.Player p in players) {
            response.Players.Add(ConvertPlayerModelToProto(p));
        } 

        return response;
    }

    public override async Task<UpdatePlayerResponse> UpdatePlayer(UpdatePlayerRequest request, ServerCallContext callContext) {
        Models.Player player = await context.Players
                                        .Include(x => x.Team)
                                        .FirstOrDefaultAsync(x => x.Id == request.Id)
                                        ?? throw new RpcException(new Status(StatusCode.NotFound, "There is no player with given Id"));

        if(request.TeamId > 0 && request.TeamId != player.Team.Id) {
            bool isTeam = await context.Teams.AnyAsync(x => x.Id == request.TeamId);

            if(!isTeam) {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid teamId provided"));
            }

            Models.Team team = await context.Teams
                                    .FirstOrDefaultAsync(x => x.Id == request.TeamId)
                                    ?? throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid teamId provided"));
        
            player.Team = team;
        }

        player.FirstName = request.FirstName;
        player.LastName = request.LastName;
        player.Age = request.Age;
        player.Position = request.Position;
        player.Club = request.Club;

        await context.SaveChangesAsync();

        return await Task.FromResult(
            new UpdatePlayerResponse {
                Id = request.Id
            }
        );
    }

    public override async Task<DeletePlayerResponse> DeletePlayer(DeletePlayerRequest request, ServerCallContext callContext) {
        if(request.Id == 0) {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Player id is mandatory"));
        }
        
        bool isPlayer = await context.Players.AnyAsync(x => x.Id == request.Id);

        if(!isPlayer) {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid player id provided"));
        }

        Models.Player player= await context.Players.FirstAsync(x => x.Id == request.Id);

        context.Players.Remove(player);
        await context.SaveChangesAsync();

        return new DeletePlayerResponse {
            Id = request.Id
        };
    }

    public static ReadPlayerResponse ConvertPlayerModelToProto(Models.Player player) {
        return new ReadPlayerResponse {
            Id = player.Id,
            FirstName = player.FirstName,
            LastName = player.LastName,
            Age = player.Age,
            Position = player.Position,
            Club = player.Club,
            Team = new Team {
                Id = player.Team.Id,
                Name = player.Team.Name
            }
        };
    }
}