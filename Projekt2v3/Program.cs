using System;
using System.IO;

namespace Projekt2v3
{
    class Program
    {
        static DataFile dataFile;

        static void Main(string[] args)
        {
            // stworzenie pliku poczatkowego z 1 strona na obszar danych i jedna strona obszaru nadmiernego
            dataFile = new DataFile("test", 1, 1, 0.5, 0.4, 0.2);

            int option;
            int[] numbers = new int[5];
            int key;

            ShowMainMenu();
            option = Convert.ToInt32(Console.ReadLine());
            switch (option)
            {
                case 0:
                    break;
                case 1:
                    ShowMenu();
                    option = Convert.ToInt32(Console.ReadLine());
                    while (option != 0)
                    {
                        switch (option)
                        {
                            case 1:
                                Console.WriteLine("Key: ");
                                key = Convert.ToInt32(Console.ReadLine());
                                Console.WriteLine("Numbers[0]: ");
                                numbers[0] = Convert.ToInt32(Console.ReadLine());
                                Console.WriteLine("Numbers[1]: ");
                                numbers[1] = Convert.ToInt32(Console.ReadLine());
                                Console.WriteLine("Numbers[2]: ");
                                numbers[2] = Convert.ToInt32(Console.ReadLine());
                                Console.WriteLine("Numbers[3]: ");
                                numbers[3] = Convert.ToInt32(Console.ReadLine());
                                Console.WriteLine("Numbers[4]: ");
                                numbers[4] = Convert.ToInt32(Console.ReadLine());
                                Insert(key, numbers);
                                break;
                            case 2:
                                ReadFile();
                                break;
                            case 3:
                                ReadIndex();
                                break;
                            case 4:
                                ReadInOrder();
                                break;
                            case 5:
                                Reorganize();
                                break;
                            case 6:
                                Console.WriteLine("Key: ");
                                key = Convert.ToInt32(Console.ReadLine());
                                ShowRecord(key);
                                break;
                            case 7:
                                Console.WriteLine("Key: ");
                                key = Convert.ToInt32(Console.ReadLine());
                                DeleteRecord(key);
                                break;
                            case 8:
                                Console.WriteLine("Key: ");
                                key = Convert.ToInt32(Console.ReadLine());
                                Console.WriteLine("New Numbers[0]: ");
                                numbers[0] = Convert.ToInt32(Console.ReadLine());
                                Console.WriteLine("New Numbers[1]: ");
                                numbers[1] = Convert.ToInt32(Console.ReadLine());
                                Console.WriteLine("New Numbers[2]: ");
                                numbers[2] = Convert.ToInt32(Console.ReadLine());
                                Console.WriteLine("New Numbers[3]: ");
                                numbers[3] = Convert.ToInt32(Console.ReadLine());
                                Console.WriteLine("New Numbers[4]: ");
                                numbers[4] = Convert.ToInt32(Console.ReadLine());
                                UpdateRecord(key, numbers);
                                break;
                        }
                        ShowMenu();
                        option = Convert.ToInt32(Console.ReadLine());
                    }
                    break;
                case 2:
                    Console.WriteLine("Specify the path to the test file");
                    string filePath = Console.ReadLine();
                    ParseCommands(filePath);
                    break;
            }
            
            dataFile.Close();

        }

        static void ShowMenu()
        {
            Console.WriteLine("Choose option:");
            Console.WriteLine("0. Exit");
            Console.WriteLine("1. Insert record");
            Console.WriteLine("2. Read file");
            Console.WriteLine("3. Read index");
            Console.WriteLine("4. Read file in order");
            Console.WriteLine("5. Reorganize file");
            Console.WriteLine("6. Show record");
            Console.WriteLine("7. Delete record");
            Console.WriteLine("8. Update record");
        }

        static void ShowMainMenu()
        {
            Console.WriteLine("Choose option:");
            Console.WriteLine("0. Exit");
            Console.WriteLine("1. My commands");
            Console.WriteLine("2. Test file");
        }

        static void ShowInfo()
        {
            Console.WriteLine($"INFO: Reads:{dataFile.reads} Writes:{dataFile.writes}");
        }

        static void ParseCommands(string filePath)
        {
            string path = Path.GetFullPath(filePath);
            string ext = Path.GetExtension(path);

            if (File.Exists(path) && ext.Equals(".txt"))
            {
                string[] lines = File.ReadAllLines(filePath);
                string[] words;
                
                foreach (string line in lines)
                {
                    int key;
                    int[] numbers = new int[5];
                    words = line.Split(" ");
                    switch (words[0])
                    {
                        case "I":
                            key = Convert.ToInt32(words[1]);
                            numbers[0] = Convert.ToInt32(words[2]);
                            numbers[1] = Convert.ToInt32(words[3]);
                            numbers[2] = Convert.ToInt32(words[4]);
                            numbers[3] = Convert.ToInt32(words[5]);
                            numbers[4] = Convert.ToInt32(words[6]); 
                            Insert(key, numbers);
                            break;
                        case "F":
                            ReadFile();
                            break;
                        case "X":
                            ReadIndex();
                            break;
                        case "O":
                            ReadInOrder();
                            break;
                        case "R":
                            Reorganize();
                            break;
                        case "S":
                            key = Convert.ToInt32(words[1]);
                            ShowRecord(key);
                            break;
                        case "D":
                            key = Convert.ToInt32(words[1]);
                            DeleteRecord(key);
                            break;
                        case "U":
                            key = Convert.ToInt32(words[1]);
                            numbers[0] = Convert.ToInt32(words[2]);
                            numbers[1] = Convert.ToInt32(words[3]);
                            numbers[2] = Convert.ToInt32(words[4]);
                            numbers[3] = Convert.ToInt32(words[5]);
                            numbers[4] = Convert.ToInt32(words[6]);
                            UpdateRecord(key, numbers);
                            break;
                    }
                }
                
            }
            else
            {
                Console.WriteLine("File with specified path doesn't exist.");
            }
        }

        static void Insert(int key, int[] numbers)
        {
            if (key > 0)
            {
                
                Record record = new Record(numbers, key);
                dataFile.InsertRecord(record);
            }
            else
            {
                Console.WriteLine("Invalid key. Key must be greater than zero.");
            }
            ShowInfo();
        }

        static void ReadFile()
        {
            dataFile.ReadFile();
            ShowInfo();
        }

        static void ReadIndex()
        {
            dataFile.ReadIndex();
            ShowInfo();
        }

        static void ReadInOrder()
        {
            dataFile.ReadFileInOrder();
            ShowInfo();
        }

        static void Reorganize()
        {
            dataFile.Reorganize();
            ShowInfo();
        }

        static void ShowRecord(int key)
        {
            dataFile.ReadRecord(key);
            ShowInfo();
        }

        static void DeleteRecord(int key)
        {
            dataFile.DeleteRecord(key);
            ShowInfo();
        } 

        static void UpdateRecord(int key, int[] numbers)
        {
            dataFile.Update(key,numbers);
            ShowInfo();
        }

    }

}
