using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

/**
 * Auto-generated code below aims at helping you parse
 * the standard input according to the problem statement.
 **/
class Player
{
    static void Main(string[] args)
    {
        string[] inputs;
        int numAllCustomers = int.Parse(Console.ReadLine());
        for (int i = 0; i < numAllCustomers; i++)
        {
            inputs = Console.ReadLine().Split(' ');
            string customerItem = inputs[0]; // the food the customer is waiting for
            int customerAward = int.Parse(inputs[1]); // the number of points awarded for delivering the food
        }
        for (int i = 0; i < 7; i++)
        {
            string kitchenLine = Console.ReadLine();
        }

        // game loop
        while (true)
        {
            int turnsRemaining = int.Parse(Console.ReadLine());
            inputs = Console.ReadLine().Split(' ');
            int playerX = int.Parse(inputs[0]);
            int playerY = int.Parse(inputs[1]);
            string playerItem = inputs[2];
            inputs = Console.ReadLine().Split(' ');
            int partnerX = int.Parse(inputs[0]);
            int partnerY = int.Parse(inputs[1]);
            string partnerItem = inputs[2];
            int numTablesWithItems = int.Parse(Console.ReadLine()); // the number of tables in the kitchen that currently hold an item
            for (int i = 0; i < numTablesWithItems; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                int tableX = int.Parse(inputs[0]);
                int tableY = int.Parse(inputs[1]);
                string item = inputs[2];
            }
            inputs = Console.ReadLine().Split(' ');
            string ovenContents = inputs[0]; // ignore until wood 1 league
            int ovenTimer = int.Parse(inputs[1]);
            int numCustomers = int.Parse(Console.ReadLine()); // the number of customers currently waiting for food
            for (int i = 0; i < numCustomers; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                string customerItem = inputs[0];
                int customerAward = int.Parse(inputs[1]);
            }

            // Write an action using Console.WriteLine()
            // To debug: Console.Error.WriteLine("Debug messages...");


            // MOVE x y
            // USE x y
            // WAIT
            Console.WriteLine("WAIT");
        }
    }
}