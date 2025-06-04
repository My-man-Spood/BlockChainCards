using System.Reflection;
using Microsoft.VisualBasic;
using Spood.BlockChainCards;
using Spood.BlockChainCards.Commands;

var commandTypes = Assembly.GetExecutingAssembly()
    .GetTypes()
    .Where(t => typeof(ICommand).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

var commands = commandTypes
    .Select(t => (ICommand)Activator.CreateInstance(t)!)
    .ToDictionary(cmd => cmd.Name, StringComparer.OrdinalIgnoreCase);

string command = "";
while (command != "exit")
{
    command = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(command))
        continue;

    var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    var cmdName = parts[0];
    var cmdArgs = parts.Skip(1).ToArray();

    if (cmdName.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }
    else if (cmdName.Equals("help", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Available commands: " + string.Join(", ", commands.Keys) + ", exit, help");
    }
    else if (commands.TryGetValue(cmdName, out var cmd))
    {
        cmd.Execute(cmdArgs);
    }
    else
    {
        Console.WriteLine($"Unknown command: {cmdName}");
    }
}

/*
var jer = Guid.Parse("F480B982-CF69-4401-8B5F-46783EC8018B");
var joe = Guid.Parse("17A944F5-5410-4361-8503-B01C2C04F309");
var jen = Guid.Parse("DF34B17C-A727-4D71-A65D-C5606E245643");

var transaction1 = new BCTransaction(jer, joe, [Guid.Parse("1501B536-0EDB-47D5-9D24-754A8B11A606")]);
var transaction2 = new BCTransaction(joe, jen, [Guid.Parse("F0FE3F91-D2C8-4B0D-BFA8-863F242B413B")]);
var transaction3 = new BCTransaction(jen, jer, [Guid.Parse("1620D22A-9C12-48C0-AA4C-3EE1DDD3C59D")]);
var transaction4 = new BCTransaction(jer, jen, [Guid.Parse("A0DB7D22-0F25-4DEF-BEB9-6B558E2E673D")]);
var transaction5 = new BCTransaction(joe, jer, [Guid.Parse("905F20F1-142C-4307-AA96-5BDB5609A483")]);
var transaction6 = new BCTransaction(jen, joe, [Guid.Parse("1B9A28FF-7244-4E61-ACC6-E44C9E25ABB1")]);

var block1 = new BCCardBlock(Enumerable.Repeat((byte)9, 32).ToArray(), [transaction1, transaction2]);
var block2 = new BCCardBlock(block1.Hash, [transaction3, transaction4]);
var block3 = new BCCardBlock(block2.Hash, [transaction5, transaction6]);

Console.WriteLine($"Block 1 Data: {BitConverter.ToString(block1.BlockData).Replace("-", "")}");
Console.WriteLine($"Block 1 Hash: {BitConverter.ToString(block1.Hash).Replace("-", "")}");
Console.WriteLine($"Block 2 Data: {BitConverter.ToString(block2.BlockData).Replace("-", "")}");
Console.WriteLine($"Block 2 Hash: {BitConverter.ToString(block2.Hash).Replace("-", "")}");
Console.WriteLine($"Block 3 Data: {BitConverter.ToString(block3.BlockData).Replace("-", "")}");
Console.WriteLine($"Block 3 Hash: {BitConverter.ToString(block3.Hash).Replace("-", "")}");
*/

