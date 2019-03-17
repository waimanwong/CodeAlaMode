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
    private readonly string _message = string.Empty;

    protected Command(string command, Position p)
    {
        _command = command;
        _p = p;
    }

    protected Command(string command, Position p, string message)
    {
        _command = command;
        _p = p;
        _message = message;
    }

    public override string ToString()
    {
        return $"{_command} {_p.ToString()} {_message}";
    }
}

public class Position
{
    public int X, Y;
    public Position(int x, int y)
    {
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
    public MoveCommand(Position p) : base("MOVE", p) { }
}

public class UseCommand : Command
{
    public UseCommand(Position p) : base("USE", p, string.Empty) { }
    public UseCommand(Position p, string message) : base("USE", p, message) { }
}

public class WaitCommand : Command
{
    string _message = string.Empty;

    public WaitCommand() : base("WAIT", null)
    {
    }

    public WaitCommand(string message) : base("WAIT", null)
    {
        _message = message;
    }

    public override string ToString()
    {
        return $"WAIT {_message}";
    }
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
    public Player(Position position, Items items)
    {
        Position = position;
        Items = items;
    }
    public void Update(Position position, Items items)
    {
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
    public Items(string content)
    {
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
        int fromX = from.X;
        int fromY = from.Y;

        for (int radius = 1; radius <= 2; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    if (_game.TryGetTableAt(fromX + x, fromY + y, out Table neighborTable))
                    {
                        if (neighborTable.HasFunction == false && neighborTable.HasItems == false)
                        {
                            return neighborTable;
                        }
                    }
                }
            }
        }
        throw new ArgumentException();
    }

    private bool PrepareTart(out Command command)
    {
        command = null;

        var myChef = _game.Players[0];
        int requiredTartCount = _game.CustomerOrders.Count(order => order.Items.Contains("TART"));
        bool availableTartInOven = _game.OventContents == "TART";

        int availableTartCount =
            (availableTartInOven ? 1 : 0) +
            _game.Tables.Count(table => table.HasItems && table.Items.Content.Contains("TART")) +
            _game.Players.Count(p => p.Items.Content.Contains("-TART") || p.Items.Content == "TART");

        MainClass.LogDebug($"Prepare Tart : {availableTartCount}/{requiredTartCount}");

        if (availableTartCount < requiredTartCount)
        {
            if (_game.OventContents != "NONE")
            {
                if (myChef.Items.Content == "NONE")
                    command = new UseCommand(_game.Oven.Position);
                else
                {
                    var closestEmptyTable = GetClosestEmptyTable(myChef.Position);
                    command = new UseCommand(closestEmptyTable.Position);
                }
            }
            else
            {
                if (myChef.Items.Content == "NONE")
                    command = new UseCommand(_game.Dough.Position);
                else if (myChef.Items.Content == "DOUGH")
                    command = new UseCommand(_game.ChoppingBoard.Position);
                else if (myChef.Items.Content == "CHOPPED_DOUGH")
                    command = new UseCommand(_game.Blueberry.Position);
                else if (myChef.Items.Content == "RAW_TART")
                    command = new UseCommand(_game.Oven.Position);
                else if (myChef.Items.Content == "TART")
                {
                    var closestEmptyTable = GetClosestEmptyTable(myChef.Position);
                    command = new UseCommand(closestEmptyTable.Position);
                }
            }
        }
        else
        {
            if (myChef.Items.Content == "TART")
            {
                var closestEmptyTable = GetClosestEmptyTable(myChef.Position);
                command = new UseCommand(closestEmptyTable.Position);
            }
        }


        return command != null;
    }

    private bool PrepareCroissant(out Command command)
    {
        command = null;

        var myChef = _game.Players[0];

        int requiredCroissantCount = _game.CustomerOrders.Count(order => order.Items.Contains("CROISSANT"));
        int availableCroissantCount = _game.Tables.Count(table => table.HasItems && table.Items.Content.Contains("CROISSANT")) +
                                    _game.Players.Count(p => p.Items.Content.Contains("CROISSANT"));

        MainClass.LogDebug($"PrepareCroissant : {availableCroissantCount}/{requiredCroissantCount}");

        if (availableCroissantCount < requiredCroissantCount)
        {
            MainClass.LogDebug("Let's cook a croissant");

            if (_game.OventContents != "NONE")
            {
                if (myChef.Items.Content == "NONE")
                    command = new UseCommand(_game.Oven.Position);
                else
                {
                    var closestEmptyTable = GetClosestEmptyTable(myChef.Position);
                    command = new UseCommand(closestEmptyTable.Position);
                }
            }
            else
            {
                if (myChef.Items.Content == "NONE")
                {
                    command = new UseCommand(_game.Dough.Position);
                }
                else if (myChef.Items.Content == "DOUGH")
                {
                    command = new UseCommand(_game.Oven.Position);
                }
                else if (myChef.Items.Content == "CROISSANT")
                {
                    var closestEmptyTable = GetClosestEmptyTable(myChef.Position);
                    command = new UseCommand(closestEmptyTable.Position);
                }
            }
        }
        else
        {
            if (myChef.Items.Content == "CROISSANT")
            {
                var closestEmptyTable = GetClosestEmptyTable(myChef.Position);
                command = new UseCommand(closestEmptyTable.Position);
            }
        }


        return command != null;
    }

    private bool PrepareChopStrawberries(out Command command)
    {
        command = null;

        var myChef = _game.Players[0];

        int requiredChoppedStrawBerries = _game.CustomerOrders.Count(order => order.Items.Contains("CHOPPED_STRAWBERRIES"));
        int availableChoppedStrawberries = _game.Tables.Count(table => table.HasItems && table.Items.Content.Contains("CHOPPED_STRAWBERRIES"));
        int chefHolding = _game.Players.Count(p => p.Items.Content.Contains("CHOPPED_STRAWBERRIES"));

        if (availableChoppedStrawberries + chefHolding < requiredChoppedStrawBerries)
        {
            MainClass.LogDebug("Let's chop some strawberries");

            if (myChef.Items.Content == "NONE")
                command = new UseCommand(_game.Strawberry.Position);
        }

        if (myChef.Items.Content == "STRAWBERRIES")
            command = new UseCommand(_game.ChoppingBoard.Position);
        else if (myChef.Items.Content == "CHOPPED_STRAWBERRIES")
        {
            var closestEmptyTable = GetClosestEmptyTable(myChef.Position);
            command = new UseCommand(closestEmptyTable.Position);
        }

        return command != null;
    }

    private List<CustomerOrder> GetMatchingCustomerOrders()
    {
        var myChef = _game.Players[0];
        var myChefContent = myChef.Items.Content;

        return _game.CustomerOrders
            .Where(order => order.Items.StartsWith(myChefContent)).ToList();
    }

    private bool AllIngredientsAreAvailableOnTable(CustomerOrder order, string myChefContent, List<string> remainingItems)
    {
        var items = order.Items.Split('-').ToList();

        var myChefContentItems = myChefContent.Split('-').ToList();
        myChefContentItems.ForEach(i => items.Remove(i));


        foreach (var item in items)
        {
            if (item == "CROISSANT" || item == "CHOPPED_STRAWBERRIES" || item == "TART")
            {

                if (remainingItems.Contains(item))
                {
                    //item available
                    remainingItems.Remove(item);
                }
                else
                {
                    //Missing item
                    return false;
                }
            }
        }

        return true;
    }

    private List<CustomerOrder> GetCandidateCustomerOrders()
    {
        var customerOrders = _game.CustomerOrders;
        var candidates = new List<CustomerOrder>();
        var myChefContent = _game.Players[0].Items.Content;

        var allIngredientsOnTable = _game.Tables
            .Where(table => table.HasItems)
            .Select(table => table.Items.Content)
            .ToList();

        foreach (var customerOrder in customerOrders)
        {
            if (AllIngredientsAreAvailableOnTable(customerOrder, myChefContent, allIngredientsOnTable))
            {
                candidates.Add(customerOrder);
            }
        }

        //Check what the chef is preparing
        //var otherChefContent = _game.Players[1].Items.Content;
        //if(otherChefContent.Contains("DISH-"))
        //{
        //    var otherChefContentItems = otherChefContent.Split('-').ToHashSet();
        //    foreach(var candidate in candidates.ToArray())
        //    {
        //        var candidateItems = candidate.Items.Split('-').ToHashSet();
        //        var otherChefIsPreparing = true;
        //        foreach(var otherChefItem in otherChefContentItems)
        //        {
        //            if(candidateItems.Contains(otherChefItem) == false)
        //            {
        //                otherChefIsPreparing = false;
        //                break;
        //            }
        //        }

        //        if( otherChefIsPreparing)
        //        {
        //            candidates.Remove(candidate);
        //        }
        //    }

        //}

        return candidates;
    }

    private bool MyChefContentMatchesExactly(CustomerOrder order, string[] items)
    {
        var orderItems = order.Items.Split('-').OrderBy(x => x).ToArray();

        if (orderItems.Length != items.Length)
        {
            return false;
        }

        var sortedItems = items.OrderBy(x => x).ToArray();

        for (int i = 0; i < orderItems.Length; i++)
        {
            if (orderItems[i] != sortedItems[i])
                return false;
        }

        return true;
    }

    private bool MyChefCompletedCustomerOrder(out Command finishComand)
    {
        finishComand = null;
        var myChefContentItems = _game.Players[0].Items.Content.Split('-');

        foreach (var customerOrder in _game.CustomerOrders)
        {
            if (MyChefContentMatchesExactly(customerOrder, myChefContentItems))
                finishComand = new UseCommand(_game.Window.Position);
        }

        return finishComand != null;
    }

    private bool TryCompleteOrder(CustomerOrder candidateOrder, out Command command)
    {
        command = null;

        var myChef = _game.Players[0];


        //All items
        var remainingItems = candidateOrder.Items.Split('-').ToList();

        //Remove already picked items
        var myChefItems = myChef.Items.Content.Split('-').ToList();
        myChefItems.ForEach(it => remainingItems.Remove(it));

        foreach (var remainingItem in remainingItems)
        {
            if (remainingItem == "CROISSANT" || remainingItem == "TART" || remainingItem == "CHOPPED_STRAWBERRIES")
            {
                if (myChef.Items.Content == "NONE")
                {
                    var tableWithRemainingItem = _game.Tables.FirstOrDefault(table => table.HasItems && table.Items.Content == remainingItem);
                    if (tableWithRemainingItem != null)
                    {
                        command = new UseCommand(tableWithRemainingItem.Position);
                        break;
                    }

                    if (_game.OventContents == remainingItem)
                    {
                        command = new UseCommand(_game.Oven.Position);
                        break;
                    }
                }
                else
                {
                    if (remainingItems.Contains("DISH"))
                    {
                        command = new UseCommand(_game.Dishwasher.Position);
                        break;
                    }
                    else
                    {
                        var tableWithRemainingItem = _game.Tables.FirstOrDefault(table => table.HasItems && table.Items.Content == remainingItem);
                        if (tableWithRemainingItem != null)
                        {
                            command = new UseCommand(tableWithRemainingItem.Position);
                            break;
                        }

                        if (_game.OventContents == remainingItem)
                        {
                            command = new UseCommand(_game.Oven.Position);
                            break;
                        }
                    }
                }
            }
            else if (remainingItem == "BLUEBERRIES")
            {
                if (myChef.Items.Content == "NONE")
                {
                    command = new UseCommand(_game.Blueberry.Position);
                    break;
                }
                else
                {
                    if (remainingItems.Contains("DISH"))
                    {
                        command = new UseCommand(_game.Dishwasher.Position);
                        break;
                    }
                    else
                    {
                        command = new UseCommand(_game.Blueberry.Position);
                        break;
                    }
                }
            }
            else if (remainingItem == "ICE_CREAM")
            {
                if (myChef.Items.Content == "NONE")
                {
                    command = new UseCommand(_game.IceCream.Position);
                    break;
                }
                else
                {
                    if (remainingItems.Contains("DISH"))
                    {
                        command = new UseCommand(_game.Dishwasher.Position);
                        break;
                    }
                    else
                    {
                        command = new UseCommand(_game.IceCream.Position);
                        break;
                    }
                }
            }
        }

        return command != null;
    }

    private bool MyChefIsHoldingIrrelevantStuff(CustomerOrder order, out Command command)
    {
        command = null;

        var myChef = _game.Players[0];

        if (myChef.Items.Content != "NONE")
        {
            var myChefItems = myChef.Items.Content.Split('-');
            var orderItems = order.Items.Split('-').ToHashSet();

            foreach (var holdingItem in myChefItems)
            {
                if (orderItems.Contains(holdingItem) == false)
                {
                    var closestEmptyTable = GetClosestEmptyTable(myChef.Position);
                    command = new UseCommand(closestEmptyTable.Position, "MyChefIsHoldingIrrelevantStuff");
                }
            }
        }

        return command != null;
    }

    public Command ComputeCommand()
    {
        var myChef = _game.Players[0];

        if (MyChefCompletedCustomerOrder(out Command finishCommand))
            return finishCommand;


        var candidateOrders = GetCandidateCustomerOrders();
        MainClass.LogDebug("*******************");
        MainClass.LogDebug("CandidateOrders");
        candidateOrders.ForEach(o => MainClass.LogDebug($"{o.Items}({o.Reward.ToString()}"));

        if (candidateOrders.Count == 0)
        {
            MainClass.LogDebug("Let's prepare a ingredient");
            if (PrepareChopStrawberries(out Command chopStrawberryCommand))
            {
                return chopStrawberryCommand;
            }

            if (PrepareCroissant(out Command croissantCommand))
            {
                return croissantCommand;
            }

            if (PrepareTart(out Command tartCommand))
            {
                return tartCommand;
            }
        }

        var candidateOrder = candidateOrders.OrderByDescending(o => o.Reward).FirstOrDefault();


        if (candidateOrder != null)
        {
            MainClass.LogDebug("*********");
            MainClass.LogDebug($"Let's prepare a candidate customer order:{candidateOrder.Items}");

            if (MyChefIsHoldingIrrelevantStuff(candidateOrder, out Command dropCommand))
            {
                return dropCommand;
            }

            if (TryCompleteOrder(candidateOrder, out Command command))
            {
                return command;
            }
        }

        return new WaitCommand("DO NOT WHAT TO DO !!");

    }
}

public class MainClass
{
    public static bool Debug = true;
    public const string Dish = "DISH";

    public static Game ReadGame()
    {
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

    private static string ReadLine()
    {
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
            foreach (var t in game.Tables)
            {
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
                if (game.TryGetTableAt(x, y, out Table table))
                {
                    table.Items = new Items(content);
                }
            }
            LogDebug("");
            LogDebug("*** Oven contents ***");
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