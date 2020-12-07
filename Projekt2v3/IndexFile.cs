using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Projekt2v3
{
    class IndexFile
    {
        private FileStream fs;
        public string filePath;
        private int numberOfPages;
        private IndexPage page;
        private int lastRead;
        private int lastWrite;
        public int reads;
        public int writes;

        public IndexFile(string fileName, int numberOfPages)
        {
            filePath = "./../../../data/" + fileName + ".index";
            this.numberOfPages = numberOfPages;
            this.fs = File.Create(filePath);
            this.fs.SetLength((int)Math.Ceiling((double)numberOfPages/(double)IndexPage.BLOCKING_FACTOR) * IndexPage.PAGE_SIZE);
            this.fs.Close();
            this.lastWrite = -1;
            this.lastRead = -1;
            this.page = new IndexPage();
            this.reads = 0;
            this.writes = 0;
            FillIndex2();
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

        public void WritePage()
        {
            this.writes++;
            fs.Seek(page.pageNumber * IndexPage.PAGE_SIZE, SeekOrigin.Begin);
            fs.Write(page.diskBlock);
            lastWrite = -1;
            page.pageNumber++;
            Array.Clear(page.diskBlock,0,page.diskBlock.Length);
        }

        public void Write(IndexRow indexRow)
        {
            if (lastWrite < IndexPage.BLOCKING_FACTOR - 1) 
            {
                lastWrite++;
                byte[] row = indexRow.GetBytes();
                Array.Copy(row, 0, page.diskBlock, this.lastWrite * IndexRow.ROW_SIZE, IndexRow.ROW_SIZE);
            }
            else
            {
                WritePage();
                Write(indexRow);
            }
        }

        public void ReadPage(int pageNumber)
        {
            this.reads++;
            fs.Seek(pageNumber * IndexPage.PAGE_SIZE,SeekOrigin.Begin);
            fs.Read(page.diskBlock);
            page.pageNumber = pageNumber;
            page.position = 0;
            lastRead = -1;
        }

        public IndexRow ReadNextRow()
        {
            IndexRow row = new IndexRow();
            if (lastRead < IndexPage.BLOCKING_FACTOR - 1)
            {
                this.lastRead++;
                byte[] rowInBytes = new byte[IndexRow.ROW_SIZE];
                Array.Copy(page.diskBlock, lastRead * IndexRow.ROW_SIZE, rowInBytes, 0, rowInBytes.Length);
                row.FromBytes(rowInBytes);
            }
            else
            {
                ReadPage(this.page.pageNumber + 1);
                row = ReadNextRow();
            }
            return row;
        }

        public void FillIndex2()
        {
            fs = File.Open(this.filePath, FileMode.Open);
            fs.Seek(0, SeekOrigin.Begin);
            IndexRow indexRow;
            for (int i = 0; i < numberOfPages; i++)
            {
                indexRow = new IndexRow(0, i);
                Write(indexRow);
            }
            WritePage();
            fs.Close();
        }

        public void UpdateIndex(int pageNumber, int key)
        {
            using (BinaryWriter writer = new BinaryWriter(File.Open(filePath, FileMode.OpenOrCreate)))
            {
                writer.Seek(pageNumber * IndexRow.ROW_SIZE, SeekOrigin.Begin);
                writer.Write(key);
            }
        }

        public void UpdateIndex2(int pageNumber, int key)
        {
            this.reads = 0;
            this.writes = 0;

            fs = File.Open(this.filePath, FileMode.Open);
            int page = (int) Math.Ceiling((double)pageNumber / (double)IndexPage.BLOCKING_FACTOR);
            ReadPage(page);
            IndexRow row = ReadNextRow();
            while(row.page != pageNumber)
            {
                row = ReadNextRow();
            }
            row.key = key;
            lastWrite = lastRead - 1;
            Write(row);
            WritePage();
            fs.Close();
        }

        public int FindPage(int key)
        {
            int tempKey;
            int pageNumber = 0;
            
            using (BinaryReader reader = new BinaryReader(new FileStream(filePath, FileMode.Open, FileAccess.Read)))
            {
                while (reader.BaseStream.Position != numberOfPages * IndexRow.ROW_SIZE)
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

        public int FindPage2(int key)
        {
            this.reads = 0;
            this.writes = 0;

            fs = File.Open(filePath, FileMode.Open);

            int pageNumber = 0;
            int indexPage = 0;
            
            ReadPage(indexPage);
            IndexRow row;

            for(int i = 0; i < numberOfPages;i++)
            {
                row = ReadNextRow();
                if(row.key == 0)
                {
                    break;
                }
                if (row.key > key)
                {
                    break;
                }
                pageNumber = row.page;
            }
            fs.Close();
            return pageNumber;
        }
        
        public void ReadFile()
        {
            this.reads = 0;
            this.writes = 0;

            fs = File.Open(filePath, FileMode.Open);

            IndexRow row;

            Console.WriteLine("------------");
            Console.WriteLine("Index File");
            Console.WriteLine("------------");

            ReadPage(0);

            for (int i = 0; i < numberOfPages; i++)
            {
                row = ReadNextRow();
                Console.WriteLine(row.ToString());
            }

            fs.Close();
        }

    }
}
