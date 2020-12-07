using System;

namespace Projekt2v3
{
    class Program
    {
        static void Main(string[] args)
        {
            // stworzenie pliku poczatkowego z 1 strona na obszar danych i jedna strona obszaru nadmiernego
            DataFile dataFile = new DataFile("test", 2, 1, 0.5, 0.4, 0.2);

            int option;
            int[] numbers = new int[5];
            int key;

            ShowMenu();
            
            option = Convert.ToInt32(Console.ReadLine());
            while (option != 0) 
            {
                switch (option)
                {
                    case 1:
                        Console.WriteLine("Key: ");
                        key = Convert.ToInt32(Console.ReadLine());
                        if (key > 0)
                        {
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
                            Record record = new Record(numbers, key);
                            dataFile.InsertRecord(record);
                            break;
                        }
                        else
                        {
                            Console.WriteLine("Invalid key. Key must be greater than zero.");
                            break;
                        }
                        
                    case 2:
                        dataFile.ReadFile();
                        break;
                    case 3:
                        dataFile.ReadIndex();
                        break;
                    case 4:
                        dataFile.ReadFileInOrder();
                        break;
                    case 5:
                        dataFile.Reorganize();
                        break;
                    case 6:
                        Console.WriteLine("Key: ");
                        key = Convert.ToInt32(Console.ReadLine());
                        dataFile.ReadRecord(key);
                        break;
                }
                ShowMenu();
                option = Convert.ToInt32(Console.ReadLine());
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
        }
    }
}
