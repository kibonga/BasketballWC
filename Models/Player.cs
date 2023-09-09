namespace BasketballWC.Models;

public class Player {
    public int Id { get; set;}
    public string FirstName { get; set;} = "";
    public string LastName { get; set;} = "";
    public int Age { get; set; }
    public string Position {get;set;} = "";
    public Team Team { get; set; }
    public string Club { get; set; } = "";
}