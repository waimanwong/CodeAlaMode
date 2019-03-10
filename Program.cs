using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

#region almost stable
public abstract class Command
{
    private readonly string _command;
    private readonly Position _p;

    protected Command(string command, Position p) 
    {
        _command = command;
        _p = p;
    }

    public override string ToString() 
    {
        return $"{_command} {_p.ToString()}";
    }
}

public class Position
{
    public int X, Y;
    public Position(int x, int y){
        X = x;
        Y = y;
    }

    public int Manhattan(Position p2) => Math.Abs(X - p2.X) + Math.Abs(Y - p2.Y);

    public override string ToString()
    {
        return X + " " + Y;
    }
}


public class MoveCommand : Command
{
    public MoveCommand(Position p) : base("MOVE", p) {}   
}

public class UseCommand : Command
{
    public UseCommand(Position p) : base("USE", p) {}
}
#endregion

public class CustomerOrder
{
    public string Items;
    public int Reward;
    public CustomerOrder(string items, int reward)
    {
        this.Items = items;
        this.Reward = reward;
    }
}

public class Table
{
    public Position Position;
    public bool HasFunction;
    public Items Items;

    public bool HasItems => Items != null;

    public override string ToString() 
    {
        return $"({Position.X},{Position.Y}) : {((Items == null) ? "" : Items.Content)}";
    }
}


public class Player
{
    public Position Position;
    public Items Items;
    public Player(Position position, Items items){
        Position = position;
        Items = items;
    }
    public void Update(Position position, Items items){
        Position = position;
        Items = items;
    }
}

public class Game
{
    public Player[] Players = new Player[2];
    public Table Dishwasher;
    public Table Window;
    public Table ChoppingBoard;
    public Table Blueberry;
    public Table IceCream;
    public Table Strawberry;
    public Table Dough;
    public Table Oven;

    public string OventContents;

    public List<Table> Tables = new List<Table>();

    public List<CustomerOrder> CustomerOrders = new List<CustomerOrder>();

    public void UpdateCustomerOrders(IEnumerable<CustomerOrder> customerOrders)
    {
        this.CustomerOrders.Clear();
        this.CustomerOrders.AddRange(customerOrders);
    }

    public bool TryGetTableAt(int x, int y, out Table foundTable)
    {
        foundTable = Tables.SingleOrDefault(table => table.Position.X == x && table.Position.Y == y);
        return foundTable != null;
    }
}

public class Items
{
    public string Content;
    public bool HasPlate;
    public Items(string content){
        Content = content;
        HasPlate = Content.Contains(MainClass.Dish);
    }
}

public interface IGameAI 
{
    Command ComputeCommand();
}

public class GameAI : IGameAI
{
    private readonly Game _game;

    public GameAI(Game game)
    {
        _game = game;
    }

    private Table GetClosestEmptyTable(Position from)
    {
        MainClass.LogDebug("GetClosestEmptyTable");

        int fromX = from.X;
        int fromY = from.Y;
        int radius = 2;
        for(int x=-radius; x <= radius; x++)
        {
            for(int y=-radius; y <= radius ; y++)
            {
                if(_game.TryGetTableAt(fromX + x, fromY + y, out Table neighborTable))
                {
                    if(neighborTable.HasFunction == false && neighborTable.HasItems == false)
                    {
                        return neighborTable;
                    }
                }
            }
        }
        throw new ArgumentException();
    }

    private bool MayCookCroissant(out Command command)
    {
        command = null;

        var myChef = _game.Players[0];

        int requiredCroissant =_game.CustomerOrders.Count(order => order.Items.Contains("CROISSANT"));
        bool availableCroissant = _game.OventContents == "DOUGH" || _game.OventContents == "CROISSANT";

        if(0 < requiredCroissant && availableCroissant == false)
        {
            //Let's cook croissant
            if(myChef.Items.Content == "NONE")
                command = new UseCommand(_game.Dough.Position);
            else if(myChef.Items.Content == "DOUGH")
                command = new UseCommand(_game.Oven.Position);
        }

        return command != null;
    }

    private bool MayChopStrawberries(out Command command) 
    {
        command = null;

        var myChef = _game.Players[0];

        int requiredChoppedStrawBerries =_game.CustomerOrders.Count(order => order.Items.Contains("CHOPPED_STRAWBERRIES"));
        int availableChoppedStrawberries = _game.Tables.Count(table => table.HasItems && table.Items.Content.Contains("CHOPPED_STRAWBERRIES"));

        if(availableChoppedStrawberries < requiredChoppedStrawBerries)
        {
            //Let's chop some strawberries
            if(myChef.Items.Content == "NONE")
                command = new UseCommand(_game.Strawberry.Position);
            else if(myChef.Items.Content == "STRAWBERRIES")
                command = new UseCommand(_game.ChoppingBoard.Position);
            else if(myChef.Items.Content == "CHOPPED_STRAWBERRIES")
            {
                var closestEmptyTable = GetClosestEmptyTable(myChef.Position);
                command = new UseCommand(closestEmptyTable.Position);
            }
        }

        return command != null;
    }

    public Command ComputeCommand()
    {
        var myChef = _game.Players[0];

        if(MayChopStrawberries(out Command command))
        {
            return command;
        }

        var lowestRewardOrder =_game.CustomerOrders.OrderBy(o => o.Reward).First();
        var requiredItems = lowestRewardOrder.Items.Split('-');

        if(myChef.Items.Content == lowestRewardOrder.Items)
        {
            return new UseCommand(_game.Window.Position);
        }
        else
        {
            foreach(var requiredItem in requiredItems)
            {
                if(myChef.Items.Content.Contains(requiredItem) == false)
                {
                    if(requiredItem == "DISH")
                        return new UseCommand(_game.Dishwasher.Position);
                    else if(requiredItem == "ICE_CREAM")
                        return new UseCommand(_game.IceCream.Position);
                    else if(requiredItem == "BLUEBERRIES")
                        return new UseCommand(_game.Blueberry.Position);
                    else if(requiredItem == "CHOPPED_STRAWBERRIES")
                    {
                        var targetTable =_game.Tables.FirstOrDefault(t => t.HasItems && t.Items.Content == "CHOPPED_STRAWBERRIES");
                        if(targetTable != null)
                            return new UseCommand(targetTable.Position);
                    }
                }
            }
        }
        return new UseCommand(_game.Dishwasher.Position);
    }
}

public class MainClass
{
    public static bool Debug = true;
    public const string Dish = "DISH";

    public static Game ReadGame(){
        var game = new Game();
        game.Players[0] = new Player(null, null);
        game.Players[1] = new Player(null, null);

        for (int i = 0; i < 7; i++)
        {
            string kitchenLine = ReadLine();
            for (var x = 0; x < kitchenLine.Length; x++)
            {
                if (kitchenLine[x] == 'W') game.Window = new Table { Position = new Position(x, i), HasFunction = true };
                if (kitchenLine[x] == 'D') game.Dishwasher = new Table { Position = new Position(x, i), HasFunction = true };
                if (kitchenLine[x] == 'I') game.IceCream = new Table { Position = new Position(x, i), HasFunction = true };
                if (kitchenLine[x] == 'B') game.Blueberry = new Table { Position = new Position(x, i), HasFunction = true };
                if (kitchenLine[x] == 'S') game.Strawberry = new Table { Position = new Position(x, i), HasFunction = true };
                if (kitchenLine[x] == 'C') game.ChoppingBoard = new Table { Position = new Position(x, i), HasFunction = true };
                if (kitchenLine[x] == 'H') game.Dough = new Table { Position = new Position(x, i), HasFunction = true };
                if (kitchenLine[x] == 'O') game.Oven = new Table { Position = new Position(x, i), HasFunction = true };
                if (kitchenLine[x] == '#') game.Tables.Add(new Table { Position = new Position(x, i) });
            }
        }



        return game;
    }

    private static string ReadLine(){
        var s = Console.ReadLine();
        
        LogDebug(s);

        return s;
    }

    public static void LogDebug(string message)
    {
        if (Debug)
            Console.Error.WriteLine(message);
    }

    static void Main()
    {
        string[] inputs;

        // ALL CUSTOMERS INPUT: to ignore until Bronze
        int numAllCustomers = int.Parse(ReadLine());
        for (int i = 0; i < numAllCustomers; i++)
        {
            inputs = ReadLine().Split(' ');
            string customerItem = inputs[0]; // the food the customer is waiting for
            int customerAward = int.Parse(inputs[1]); // the number of points awarded for delivering the food
        }

        // KITCHEN INPUT
        var game = ReadGame();

        while (true)
        {
            int turnsRemaining = int.Parse(ReadLine());

            // PLAYERS INPUT
            inputs = ReadLine().Split(' ');
            game.Players[0].Update(new Position(int.Parse(inputs[0]), int.Parse(inputs[1])), new Items(inputs[2]));
            inputs = ReadLine().Split(' ');
            game.Players[1].Update(new Position(int.Parse(inputs[0]), int.Parse(inputs[1])), new Items(inputs[2]));

            //Clean other tables
            foreach(var t in game.Tables){
                t.Items = null;
            }

            LogDebug("");
            LogDebug("*** Tables with item ***");
            int numTablesWithItems = int.Parse(ReadLine()); // the number of tables in the kitchen that currently hold an item
            for (int i = 0; i < numTablesWithItems; i++)
            {
                inputs = ReadLine().Split(' ');
                int x = int.Parse(inputs[0]);
                int y = int.Parse(inputs[1]);
                string content = inputs[2];
                if(game.TryGetTableAt(x, y, out Table table))
                {
                    table.Items = new Items(content);
                }
            }

            inputs = ReadLine().Split(' ');
            string ovenContents = inputs[0]; // ignore until bronze league
            int ovenTimer = int.Parse(inputs[1]);
            game.OventContents = ovenContents;

            LogDebug("");
            LogDebug("*** Customers are waiting ***");
            int numCustomers = int.Parse(ReadLine()); // the number of customers currently waiting for food
            CustomerOrder[] customerOrders = new CustomerOrder[numCustomers];
            for (int i = 0; i < numCustomers; i++)
            {
                inputs = ReadLine().Split(' ');
                string items = inputs[0];
                int award = int.Parse(inputs[1]);
                customerOrders[i] = new CustomerOrder(items, award);
            }
            game.UpdateCustomerOrders(customerOrders);

            var gameAI = new GameAI(game);
            var command = gameAI.ComputeCommand();

            Console.WriteLine(command.ToString());


        }
    }
}