using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Projekt2v3
{
    class IndexFile
    {
        //private FileStream fs;
        public const int ROW_SIZE = 8;  //liczba bajtow jednego wpisu 4 bajty (int) - key 3 bajty (int) - page
        public string filePath;
        private int numberOfPages;
        public IndexFile(string fileName, int numberOfPages)
        {
            filePath = "./../../../data/" + fileName + ".index";
            // stworz nowy plik o rozmiarze (4+4)*numberOfPages
            //FileStream fs = File.Create(filePath);
            //fs.SetLength(2 * sizeof(int) * numberOfPages);
            //fs.Close();
            this.numberOfPages = numberOfPages;
            FillIndex();
        }
        public void FillIndex()
        {
            using (BinaryWriter writer = new BinaryWriter(File.Open(filePath, FileMode.OpenOrCreate)))
            {
                for (int i = 0; i < numberOfPages; i++)
                {
                    writer.Write(0);
                    writer.Write(i);
                }
            }
        }
        // metoda zwraca numer strony, w ktorej nalezy szukac rekordu
        public short FindPage2(int key)
        {
            short pageNumber = 0;
            int filePosition = 0;
            int tempKey;
            while (true)
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    fs.Seek(filePosition, SeekOrigin.Begin);
                    using (BinaryReader reader = new BinaryReader(fs))
                    {
                        tempKey = reader.ReadInt32();
                        // porownaj klucze jesli wiekszy skoncz szukac
                        if (tempKey > key)
                        {
                            break;
                        }
                        filePosition += 8;
                        if (filePosition == fs.Length)
                        {
                            break;
                        }
                        pageNumber++;
                    }
                }

            }

            return pageNumber;
        }
        public void UpdateIndex(int pageNumber, int key)
        {
            using (BinaryWriter writer = new BinaryWriter(File.Open(filePath, FileMode.OpenOrCreate)))
            {
                writer.Seek(pageNumber * ROW_SIZE, SeekOrigin.Begin);
                writer.Write(key);
            }
        }
        public int FindPage(int key)
        {
            int tempKey;
            int pageNumber = 0;
            
            using (BinaryReader reader = new BinaryReader(new FileStream(filePath, FileMode.Open, FileAccess.Read)))
            {
                while (reader.BaseStream.Position != numberOfPages * ROW_SIZE)
                {
                    tempKey = reader.ReadInt32();
                    if(tempKey == 0)
                    {
                        break;
                    }
                    if (tempKey > key)
                    {
                        break;
                    }
                    pageNumber = reader.ReadInt32(); 
                }
            } 
            
            return pageNumber;
        }
        
        public void ReadFile()
        {
            int key;
            int page;

            Console.WriteLine("------------");
            Console.WriteLine("Index File");
            Console.WriteLine("------------");
            using (BinaryReader reader = new BinaryReader(new FileStream(filePath, FileMode.Open, FileAccess.Read)))
            {
                for (int i = 0; i < numberOfPages; i++)
                {

                    key = reader.ReadInt32();
                    page = reader.ReadInt32();
                    Console.WriteLine($"K: {key} P: {page}");
                }
                
            }
        }
        
    }
}
