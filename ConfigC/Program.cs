

using System.Data;

class Program
{


    public static void Main()
    {
        while (true)
        {
            string userDataPath = GetSteamUserDataPath();
            string[] userFolders = Directory.GetDirectories(path: userDataPath);

            for (int i = 0; i < userFolders.Length; i++)
            {
                string aktuellerOrdner = userFolders[i];
                Console.WriteLine((i + 1) + ": " + aktuellerOrdner);
            }
            Console.WriteLine("Pick a folder you want to Copy the files from");

            string inputFromUser = Console.ReadLine();
            int sourceFolderIndex = Int32.Parse(inputFromUser) - 1;

            if (IsIndexValid(index: sourceFolderIndex, arraySize: userFolders.Length) == false)
            {
                Console.WriteLine("No valid number");
                return;
            }
            Console.WriteLine("Pick a folder you want the files in");
            inputFromUser = Console.ReadLine();
            int destFolderIndex = Int32.Parse(inputFromUser) - 1;

            if (IsIndexValid(index: destFolderIndex, arraySize: userFolders.Length) == false)
            {
                Console.WriteLine("No valid number");
                return;
            }

            string selectedUserFolder = userFolders[sourceFolderIndex];
            string selecteDestUserFolder = userFolders[destFolderIndex];

            Console.WriteLine("You've selected following folder: " + selectedUserFolder);

            CopyFilesRecursively(selectedUserFolder, selecteDestUserFolder);

            Console.WriteLine("It's all done, files copied. Over and out");
            Console.WriteLine("Press Enter to continue");
            Console.ReadLine();
            Console.Clear();
        }

    }

    private static bool IsIndexValid(int index, int arraySize)
    {
        if (index >= arraySize || index < 0)
        {
            Console.WriteLine("No valid number");
            return false;
        }
        return true;
    }

    private static string GetSteamUserDataPath()
    {
        return @"C:\Program Files (x86)\Steam\userdata";
    }

    private static void CopyFilesRecursively(string sourcePath, string targetPath)
    {
        foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
        }

        foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
        {
            File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
        }
    }

}
