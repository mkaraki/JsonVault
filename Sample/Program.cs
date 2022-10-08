using System.Text;

var vault = new JsonVault.JsonVault();

if (File.Exists("vault.json"))
    await vault.LoadVauldAsync("vault.json");
else
    await vault.CreateVaultAsync("vault.json", "files");

Console.WriteLine("\"exit\" to exit");

while (true)
{
    Console.Write("> ");
    var cmd = Console.ReadLine();

    if (cmd == "exit")
        break;

    if (cmd == "help")
    {
        Console.WriteLine("@identifier filepath\t\tAdd file to vault\n%identifier\t\tRead file in vault");
        continue;
    }

    if (cmd == "save")
    {
        await vault.SaveVaultAsync();
        continue;
    }

    var acmd = cmd.TrimStart('@', '%').Split(' ');

    if (cmd.StartsWith('@'))
    {
        if (acmd.Length != 2) continue;

        await vault.AddAsync(acmd[0], File.ReadAllText(acmd[1], Encoding.UTF8), Encoding.UTF8);

        Console.WriteLine("OK");

        continue;
    }
    else if (cmd.StartsWith('%'))
    {
        var file = await vault.GetAsync(acmd[0], Encoding.UTF8);

        if (file == null) Console.WriteLine("No file found");
        else Console.WriteLine(file);

        continue;
    }
            
}

await vault.SaveVaultAsync();
