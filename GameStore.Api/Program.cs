using GameStore.Api.DTO;


const string EndpointName = "GetGame";
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

//ctrl + . 
var games = new List<GameDto>
{
    new GameDto
    {
        Id = 1,
        Name = "Street Fighter II",
        Genre = "Fighting",
        Price = 19.99m,
        ReleaseDate = new DateOnly(1992, 2, 29)
    },
    new GameDto
    {
        Id = 2,
        Name = "Street Fighter II",
        Genre = "Fighting",
        Price = 19.99m,
        ReleaseDate = new DateOnly(1992, 2, 29)
    },
    new GameDto
    {
        Id = 3,
        Name = "Street Fighter II",
        Genre = "Fighting",
        Price = 19.99m,
        ReleaseDate = new DateOnly(1992, 2, 29)
    },
    
    
};

app.MapGet("/games", () => games);

// GET/games/1

app.MapGet("/games/{id}", (int id) => games.Find(game => game.Id == id)).WithName(EndpointName);

// POST /games
app.MapPost("/games",(CreateGameDto newGame) =>
{
    GameDto game = new GameDto
    {
        Id = games.Count + 1,
        Name = newGame.Name,
        Price = newGame.price,
        Genre = newGame.Genre,
        ReleaseDate = newGame.ReleaseDate
    };
    games.Add(game);

    return Results.CreatedAtRoute(EndpointName, new {id = game.Id}, game);
});


app.Run();


 