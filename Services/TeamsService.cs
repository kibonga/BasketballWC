using Grpc.Core;
using BasketballWC.Data;
using Microsoft.EntityFrameworkCore;
using Google.Protobuf.Collections;


namespace BasketballWC.Services;

public class TeamsService : TeamService.TeamServiceBase {
    private readonly AppDbContext context;

    public TeamsService(AppDbContext context)
    {
        this.context = context;
    }

    public override async Task<CreateTeamResponse> CreateTeam(CreateTeamRequest request, ServerCallContext callContext) {
        Models.Team team = new()
        {
            Name = request.Name,
            Ranking = request.Ranking,
            Players = ConvertProtoToPlayers(request.Players),
        };

        await context.AddAsync(team);
        await context.SaveChangesAsync();

        return await Task.FromResult(new CreateTeamResponse {
            Id = team.Id,
        });
    }

    public override async Task<ReadAllTeamsResponse> ReadAllTeams(ReadAllTeamsRequest request, ServerCallContext callContext){
        List<Models.Team> teams = await context.Teams
                                        .Include(team => team.Players)
                                        .ToListAsync();

        ReadAllTeamsResponse response = new();

        foreach(Models.Team team in teams) {
            response.Teams.Add(new ReadTeamResponse {
                Id = team.Id,
                Name = team.Name,
                Ranking = team.Ranking,
                Players = { team.Players.Select(p => new Player {
                    Id = p.Id,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    Age = p.Age,
                    Position = p.Position,

                }) }
            });
        }

        return response;
    }

    public override async Task<ReadTeamResponse> ReadTeam(ReadTeamRequest request, ServerCallContext callContext) {
        Models.Team team = await context.Teams
                                        .Include(team => team.Players)
                                        .FirstOrDefaultAsync(x => x.Id == request.Id)
                                        ?? throw new RpcException(new Status(StatusCode.NotFound, "There is no team with given id"));
        
        ReadTeamResponse response = new()
        {
            Id = team.Id,
            Name = team.Name,
            Ranking = team.Ranking,
            Players = { ConvertPlayersToProto(team.Players) }
        };

        return response;
    }

    public override async Task<UpdateTeamResponse> UpdateTeam(UpdateTeamRequest request, ServerCallContext callContext) {
        Models.Team team = await context.Teams
                                .Include(x => x.Players)
                                .FirstOrDefaultAsync(x => x.Id == request.Id)
                                ?? throw new RpcException(new Status(StatusCode.NotFound, "There is no team with given Id"));       

        if(request.Players.Any()) {
            team.Players.AddRange(ConvertProtoToPlayers(request.Players));
        }

        team.Name = request.Name;
        team.Ranking = request.Ranking;

        await context.SaveChangesAsync();

        return await Task.FromResult(new UpdateTeamResponse { 
            Id = request.Id 
        });
    }

    public override async Task<DeleteTeamResponse> DeleteTeam(DeleteTeamRequest request, ServerCallContext callContext) {
        Models.Team team = await context.Teams
                                    .Include(x => x.Players)
                                    .FirstOrDefaultAsync(x => x.Id == request.Id)
                                    ?? throw new RpcException(new Status(StatusCode.NotFound, "There is no team with given Id"));
        if(team.Players.Any()) {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Cannot delete, team contains players, remove them first"));
        }

        context.Teams.Remove(team);
        await context.SaveChangesAsync();

        return await Task.FromResult(
            new DeleteTeamResponse {
                Id = request.Id
            }
        );
    }

    private static RepeatedField<Player> ConvertPlayersToProto(List<Models.Player> players) {
        RepeatedField<Player> result = new();

        foreach(var p in players) {
            result.Add(
                new Player {
                    Id = p.Id,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    Age = p.Age,
                    Position = p.Position,
                    Club = p.Club
                }
            );
        }

        return result;
    }

    private static List<Models.Player> ConvertProtoToPlayers(RepeatedField<Player> players) {
        List<Models.Player> result = new();

        foreach(var p in players) {
            result.Add(
                new Models.Player {
                    Id = p.Id,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    Age = p.Age,
                    Position = p.Position,
                    Club = p.Club
                }
            );
        }

        return result;
    }
}