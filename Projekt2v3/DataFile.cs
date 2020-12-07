﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Projekt2v3
{
    class DataFile
    {
        private string filePath;
        private Page page = new Page();
        private IndexFile indexFile;
        private FileStream fs;
        private int primaryArea;    // liczba stron
        private int overflowArea;   // liczba stron
        private int previousPage;
        private int previousPosition;
        private double alpha;
        private double beta;
        public int savedAt;
        public int savedAtPosition;
        public int savedRecords;
        public double vnRatio;
        public int numberOfOverflows;

        public DataFile(string fileName, int primaryPages, int overflowPages, double alpha,double beta, double vnRatio)
        {
            this.primaryArea = primaryPages;
            this.overflowArea = overflowPages;
            this.alpha = alpha;
            this.beta = beta;
            this.vnRatio = vnRatio;
            this.savedRecords = 0;
            this.numberOfOverflows = 0;

            filePath = "./../../../data/" + fileName + ".bin";
            // stworz nowy plik o rozmiarze 2 * PAGE_SIZE
            fs = File.Create(filePath);
            fs.SetLength(primaryPages * Page.PAGE_SIZE + overflowPages * Page.PAGE_SIZE);
            // stworzenie indeksu
            indexFile = new IndexFile(fileName, primaryPages);
            // wpisz pierwszy rekord o niemozliwym kluczu
            InsertFirstRecord();

        }

        public void Close()
        {
            fs.Close();
        }

        public void ReadPage(int pageNumber)
        {

            fs.Seek(pageNumber * Page.PAGE_SIZE, SeekOrigin.Begin);
            fs.Read(page.diskBlock);
            page.pageNumber = pageNumber;
            page.position = 0;
        }

        public void WritePage()
        {
            fs.Seek(page.pageNumber * Page.PAGE_SIZE, SeekOrigin.Begin);
            fs.Write(page.diskBlock);
        }

        private void InsertFirstRecord()
        {
            int[] num = new int[5];
            int key = -1;
            Record record = new Record(num, key);
            //InsertRecord(record);
            int page = 0;
            int startPosition = 0;
            //WriteRecord2(ref page, record, ref startPosition);
            WriteRecord2(page, record);
            indexFile.UpdateIndex(page, key);
        }

        public void InsertRecord(Record record)
        {
            double ratio = (double)numberOfOverflows / savedRecords;

            if (ratio > beta || numberOfOverflows == overflowArea * Page.BLOCKING_FACTOR) 
            {
                this.Reorganize();
            }
            
            int pageNumber = indexFile.FindPage(record.key);
            //int startPosition = 0;

            //WriteRecord2(ref pageNumber, record, ref startPosition);
            WriteRecord2(pageNumber, record);

            if (savedAt != -1 && savedAt < primaryArea && savedAtPosition == 0)
            {
                indexFile.UpdateIndex(savedAt, record.key);
            }

            if(savedAt >= primaryArea)
            {
                numberOfOverflows++;
            }
        }

        public Record GetNextRecord()
        {
            if (page.position < Page.BLOCKING_FACTOR)
            {
                byte[] recInBytes = new byte[Record.SIZE];
                Array.Copy(page.diskBlock, page.position * Record.SIZE, recInBytes, 0, Record.SIZE);
                Record record = new Record();
                bool exist = record.FromBytes(recInBytes);
                page.position++;
                if (exist)
                {
                    return record;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                previousPage = page.pageNumber;
                previousPosition = page.position;
                ReadPage(previousPage + 1);
                return GetNextRecord();
            }

        }

        public Record GetRecordAt(int position)
        {
            if (position < Page.BLOCKING_FACTOR)
            {
                int oldPosition = page.position;
                page.position = position;
                byte[] recInBytes = new byte[Record.SIZE];
                Array.Copy(page.diskBlock, position * Record.SIZE, recInBytes, 0, Record.SIZE);
                Record record = new Record();
                bool exist = record.FromBytes(recInBytes);
                page.position = oldPosition;
                if (exist)
                {
                    return record;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
            
        }

        public Record GetRecordAt(int fromPage, int position)
        {
            int oldPage = page.pageNumber;
            int oldPosition = page.position;

            ReadPage(fromPage);
            page.position = position;
            byte[] recInBytes = new byte[Record.SIZE];
            Array.Copy(page.diskBlock, position * Record.SIZE, recInBytes, 0, Record.SIZE);
            Record record = new Record();
            record.FromBytes(recInBytes);

            ReadPage(oldPage);
            page.position = oldPosition;

            return record;
        }

        public void WriteRecord(ref int pageN, Record record, ref int startPosition, Record previousRecord = null)
        {

            bool written = false;
            ReadPage(pageN);
            
            while (!written)
            {
                // przegladamy cala strone w poszukiwaniu miejsca
                for (int i = startPosition; i < Page.BLOCKING_FACTOR; i++)
                {
                    Record temp = GetNextRecord();
                    // zapis na stronie
                   
                    if (temp is null)
                    {
                        byte[] recInBytes = record.GetBytes();
                        Array.Copy(recInBytes, 0, page.diskBlock, i * Record.SIZE, Record.SIZE);
                        WritePage();
                        written = true;
                        startPosition = i;
                        pageN = page.pageNumber;
                        savedAt = page.pageNumber;
                        savedRecords++;
                        if(!(previousRecord is null))
                        {
                            previousRecord.page = pageN;
                            previousRecord.position = i;
                        }
                        break;
                    }
                    // zapis w overflow
                    else if (temp.key > record.key)
                    {
                        previousRecord = GetRecordAt(i-1);
                        //szukanie w lancuchu
                        if (previousRecord.page != 0)
                        {
                            previousPage = pageN;
                            previousPosition = i;
                            int previousRecordPage = previousRecord.page;
                            previousRecord = GetRecordAt(previousRecord.page, previousRecord.position);
                            WriteRecord(ref previousRecordPage, record, ref previousRecord.position, previousRecord);
                            ReadPage(previousRecordPage);
                            byte[] recInBytes = previousRecord.GetBytes();
                            Array.Copy(recInBytes, 0, page.diskBlock, (previousPosition - 1) * Record.SIZE, Record.SIZE);
                            WritePage();
                            written = true;
                            break;
                        }
                        else
                        {
                            
                            previousPage = pageN;
                            previousPosition = i;
                            int startPage = primaryArea;
                            WriteRecord(ref startPage, record, ref previousRecord.position,previousRecord);
                            pageN = startPage;
                            ReadPage(previousPage);
                            //previousRecord.page = startPage;
                            byte[] recInBytes = previousRecord.GetBytes();
                            Array.Copy(recInBytes, 0, page.diskBlock, (previousPosition - 1) * Record.SIZE, Record.SIZE);
                            WritePage();
                            written = true;
                            break;
                        }
                    }


                }

            }


        }

        public void WriteRecord2(int pageNumber, Record record)
        {
            ReadPage(pageNumber);
            Record previousRecord = GetRecordAt(0);
            int previousRecordPage = pageNumber;
            int previousRecordPosition = 0;
            bool written = false;
            for (int i = pageNumber * Page.BLOCKING_FACTOR; i < primaryArea * Page.BLOCKING_FACTOR; i++) 
            {
                Record temp = GetNextRecord();
                if (temp is null)
                {
                    // wpisz rekord
                    int newPosition = i % Page.BLOCKING_FACTOR;
                    int newPage = page.pageNumber;
                    byte[] recInBytes = record.GetBytes();
                    Array.Copy(recInBytes, 0, page.diskBlock, newPosition * Record.SIZE, Record.SIZE);
                    WritePage();
                    savedAt = newPage;
                    savedAtPosition = newPosition;
                    savedRecords++;
                    written = true;
                    break;
                }
                else if (temp.key > record.key )
                {
                    // overflow
                    if(previousRecord.page != 0)
                    {
                        // przejscie do miejsca wskazywanego
                        ReadPage(previousRecord.page);
                        Record newTemp = GetRecordAt(previousRecord.position);
                        
                        if (newTemp.key > record.key)
                        {
                            int nextPage = page.pageNumber;
                            int nextPosition = previousRecord.position;

                            // szukamy nulla i aktualizujemy wskaznik 
                            for (int k = 0; k < overflowArea * Page.BLOCKING_FACTOR; k++)
                            {
                                newTemp = GetNextRecord();
                                if (newTemp is null)
                                {
                                    // wpisz rekord
                                    int newPosition = k % Page.BLOCKING_FACTOR;
                                    int newPage = page.pageNumber;
                                    // zaktualizuj aktualny wskaznik
                                    record.page = nextPage;
                                    record.position = nextPosition;
                                    byte[] recInBytes = record.GetBytes();
                                    Array.Copy(recInBytes, 0, page.diskBlock, newPosition * Record.SIZE, Record.SIZE);
                                    WritePage();
                                    savedAt = newPage;
                                    savedAtPosition = newPosition;
                                    savedRecords++;
                                    written = true;
                                    // zaktualizuj wskaznik poprzedniego
                                    ReadPage(previousRecordPage);
                                    previousRecord.page = newPage;
                                    previousRecord.position = newPosition;
                                    byte[] prevInBytes = previousRecord.GetBytes();
                                    Array.Copy(prevInBytes, 0, page.diskBlock, previousRecordPosition * Record.SIZE, Record.SIZE);
                                    WritePage();

                                    
                                    
                                    break;
                                }
                            }
                        }
                        else if(newTemp.key < record.key)
                        {
                            
                            while (true)
                            {
                                previousRecordPage = page.pageNumber;
                                previousRecordPosition = previousRecord.position;
                                previousRecord = newTemp.DeepCopy();
                                if (newTemp.page != 0)
                                {
                                    // przejscie do miejsca wskazywanego
                                    ReadPage(newTemp.page);
                                    Record newTemp2 = GetRecordAt(newTemp.position);
                                    if (newTemp2.key > record.key)
                                    {
                                        // szukamy nulla i aktualizujemy wskaznik newTemp
                                        for (int k = 0; k < overflowArea * Page.BLOCKING_FACTOR; k++) 
                                        {
                                            newTemp = GetNextRecord();
                                            if(newTemp is null)
                                            {
                                                // wpisz rekord
                                                int newPosition = k % Page.BLOCKING_FACTOR;
                                                int newPage = page.pageNumber;
                                                byte[] recInBytes = record.GetBytes();
                                                Array.Copy(recInBytes, 0, page.diskBlock, newPosition * Record.SIZE, Record.SIZE);
                                                WritePage();
                                                savedAt = newPage;
                                                savedAtPosition = newPosition;
                                                savedRecords++;
                                                written = true;
                                                // zaktualizuj wskaznik poprzedniego
                                                ReadPage(previousRecordPage);
                                                previousRecord.page = newPage;
                                                previousRecord.position = newPosition;
                                                byte[] prevInBytes = previousRecord.GetBytes();
                                                Array.Copy(prevInBytes, 0, page.diskBlock, previousRecordPosition * Record.SIZE, Record.SIZE);
                                                WritePage();
                                                break;
                                            }
                                        }
                                        break;
                                    }
                                    else if (newTemp2.key < record.key)
                                    {
                                        newTemp = newTemp2.DeepCopy();
                                    }
                                    else
                                    {
                                        written = true;
                                        this.savedAt = -1;
                                        Console.WriteLine("Record with the same key already exists in the database. You cannot insert this record.");
                                        break;
                                    }
                                }
                                else
                                {
                                    // szukamy nulla i aktualizujemy wskaznik newTemp
                                    for (int k = 0; k < overflowArea * Page.BLOCKING_FACTOR; k++)
                                    {
                                        newTemp = GetNextRecord();
                                        if (newTemp is null)
                                        {
                                            // wpisz rekord
                                            int newPosition = k % Page.BLOCKING_FACTOR;
                                            int newPage = page.pageNumber;
                                            byte[] recInBytes = record.GetBytes();
                                            Array.Copy(recInBytes, 0, page.diskBlock, newPosition * Record.SIZE, Record.SIZE);
                                            WritePage();
                                            savedAt = newPage;
                                            savedAtPosition = newPosition;
                                            savedRecords++;
                                            written = true;
                                            // zaktualizuj wskaznik poprzedniego
                                            ReadPage(previousRecordPage);
                                            previousRecord.page = newPage;
                                            previousRecord.position = newPosition;
                                            byte[] prevInBytes = previousRecord.GetBytes();
                                            Array.Copy(prevInBytes, 0, page.diskBlock, previousRecordPosition * Record.SIZE, Record.SIZE);
                                            WritePage();
                                            break;
                                        }

                                    }
                                    break;
                                }
                            }
                            
                        }
                        else if(newTemp.key == record.key)
                        {
                            Console.WriteLine("Record with the same key already exists in the database. You cannot insert this record.");
                            written = true;
                            this.savedAt = -1;
                        }
                    }
                    else
                    {
                        ReadPage(primaryArea);
                        
                        // szukamy nulla i aktualizujemy wskaznik 
                        for (int k = 0; k < overflowArea * Page.BLOCKING_FACTOR; k++)
                        {
                            Record newTemp = GetNextRecord();
                            if (newTemp is null)
                            {
                                // wpisz rekord
                                int newPosition = k % Page.BLOCKING_FACTOR;
                                int newPage = page.pageNumber;
                                byte[] recInBytes = record.GetBytes();
                                Array.Copy(recInBytes, 0, page.diskBlock, newPosition * Record.SIZE, Record.SIZE);
                                WritePage();
                                savedAt = newPage;
                                savedAtPosition = newPosition;
                                savedRecords++;
                                written = true;
                                // zaktualizuj wskaznik poprzedniego
                                ReadPage(previousRecordPage);
                                previousRecord.page = newPage;
                                previousRecord.position = newPosition;
                                byte[] prevInBytes = previousRecord.GetBytes();
                                Array.Copy(prevInBytes, 0, page.diskBlock, previousRecordPosition * Record.SIZE, Record.SIZE);
                                WritePage();
                                break;
                            }
                        }
                    }
                    break;
                }
                else if(temp.key == record.key)
                {
                    Console.WriteLine("Record with the same key already exists in the database. You cannot insert this record.");
                    written = true;
                    this.savedAt = -1;
                    break;
                }
                
                previousRecord = temp.DeepCopy();
                previousRecordPage = page.pageNumber;
                previousRecordPosition = i % Page.BLOCKING_FACTOR;
            }
            if (!written)
            {
                if(previousRecord.page != 0)
                {
                    // przejscie do miejsca wskazywanego
                    ReadPage(previousRecord.page);
                    Record newTemp = GetRecordAt(previousRecord.position);

                    if (newTemp.key > record.key)
                    {
                        int nextPage = page.pageNumber;
                        int nextPosition = previousRecord.position;

                        // szukamy nulla i aktualizujemy wskaznik 
                        for (int k = 0; k < overflowArea * Page.BLOCKING_FACTOR; k++)
                        {
                            newTemp = GetNextRecord();
                            if (newTemp is null)
                            {
                                // wpisz rekord
                                int newPosition = k % Page.BLOCKING_FACTOR;
                                int newPage = page.pageNumber;
                                // zaktualizuj aktualny wskaznik
                                record.page = nextPage;
                                record.position = nextPosition;
                                byte[] recInBytes = record.GetBytes();
                                Array.Copy(recInBytes, 0, page.diskBlock, newPosition * Record.SIZE, Record.SIZE);
                                WritePage();
                                savedAt = newPage;
                                savedAtPosition = newPosition;
                                savedRecords++;
                                written = true;
                                // zaktualizuj wskaznik poprzedniego
                                ReadPage(previousRecordPage);
                                previousRecord.page = newPage;
                                previousRecord.position = newPosition;
                                byte[] prevInBytes = previousRecord.GetBytes();
                                Array.Copy(prevInBytes, 0, page.diskBlock, previousRecordPosition * Record.SIZE, Record.SIZE);
                                WritePage();



                                break;
                            }
                        }
                    }
                    else if (newTemp.key < record.key)
                    {

                        while (true)
                        {
                            previousRecordPage = page.pageNumber;
                            previousRecordPosition = previousRecord.position;
                            previousRecord = newTemp.DeepCopy();
                            if (newTemp.page != 0)
                            {
                                // przejscie do miejsca wskazywanego
                                ReadPage(newTemp.page);
                                Record newTemp2 = GetRecordAt(newTemp.position);
                                if (newTemp2.key > record.key)
                                {
                                    int nextPage = page.pageNumber;
                                    int nextPosition = previousRecord.position;
                                    // szukamy nulla i aktualizujemy wskaznik newTemp
                                    for (int k = 0; k < overflowArea * Page.BLOCKING_FACTOR; k++)
                                    {
                                        newTemp = GetNextRecord();
                                        if (newTemp is null)
                                        {
                                            // wpisz rekord
                                            int newPosition = k % Page.BLOCKING_FACTOR;
                                            int newPage = page.pageNumber;
                                            record.page = nextPage;
                                            record.position = nextPosition;
                                            byte[] recInBytes = record.GetBytes();
                                            Array.Copy(recInBytes, 0, page.diskBlock, newPosition * Record.SIZE, Record.SIZE);
                                            WritePage();
                                            savedAt = newPage;
                                            savedAtPosition = newPosition;
                                            savedRecords++;
                                            written = true;
                                            // zaktualizuj wskaznik poprzedniego
                                            ReadPage(previousRecordPage);
                                            previousRecord.page = newPage;
                                            previousRecord.position = newPosition;
                                            byte[] prevInBytes = previousRecord.GetBytes();
                                            Array.Copy(prevInBytes, 0, page.diskBlock, previousRecordPosition * Record.SIZE, Record.SIZE);
                                            WritePage();
                                            break;
                                        }
                                    }
                                    break;
                                }
                                else if (newTemp2.key < record.key)
                                {
                                    newTemp = newTemp2.DeepCopy();
                                }
                                
                                else
                                {
                                    written = true;
                                    this.savedAt = -1;
                                    Console.WriteLine("Record with the same key already exists in the database. You cannot insert this record.");
                                    break;
                                }
                                
                            }
                            else
                            {
                                // szukamy nulla i aktualizujemy wskaznik newTemp
                                for (int k = 0; k < overflowArea * Page.BLOCKING_FACTOR; k++)
                                {
                                    newTemp = GetNextRecord();
                                    if (newTemp is null)
                                    {
                                        // wpisz rekord
                                        int newPosition = k % Page.BLOCKING_FACTOR;
                                        int newPage = page.pageNumber;
                                        byte[] recInBytes = record.GetBytes();
                                        Array.Copy(recInBytes, 0, page.diskBlock, newPosition * Record.SIZE, Record.SIZE);
                                        WritePage();
                                        savedAt = newPage;
                                        savedAtPosition = newPosition;
                                        savedRecords++;
                                        written = true;
                                        // zaktualizuj wskaznik poprzedniego
                                        ReadPage(previousRecordPage);
                                        previousRecord.page = newPage;
                                        previousRecord.position = newPosition;
                                        byte[] prevInBytes = previousRecord.GetBytes();
                                        Array.Copy(prevInBytes, 0, page.diskBlock, previousRecordPosition * Record.SIZE, Record.SIZE);
                                        WritePage();
                                        break;
                                    }

                                }
                                break;
                            }
                        }

                    }
                    else if(newTemp.key == record.key)
                    {
                        Console.WriteLine("Record with the same key already exists in the database. You cannot insert this record.");
                        written = true;
                        this.savedAt = -1;
                    }
                }
                else
                {
                    ReadPage(primaryArea);
                    // szukamy nulla i aktualizujemy wskaznik 
                    for (int k = 0; k < overflowArea * Page.BLOCKING_FACTOR; k++)
                    {
                        Record newTemp = GetNextRecord();
                        if (newTemp is null)
                        {
                            // wpisz rekord
                            int newPosition = k % Page.BLOCKING_FACTOR;
                            int newPage = page.pageNumber;
                            byte[] recInBytes = record.GetBytes();
                            Array.Copy(recInBytes, 0, page.diskBlock, newPosition * Record.SIZE, Record.SIZE);
                            WritePage();
                            savedAt = newPage;
                            savedAtPosition = newPosition;
                            savedRecords++;
                            // zaktualizuj wskaznik poprzedniego
                            ReadPage(previousRecordPage);
                            previousRecord.page = newPage;
                            previousRecord.position = newPosition;
                            byte[] prevInBytes = previousRecord.GetBytes();
                            Array.Copy(prevInBytes, 0, page.diskBlock, previousRecordPosition * Record.SIZE, Record.SIZE);
                            WritePage();
                            break;
                        }
                        //previousRecord = newTemp.DeepCopy();
                        //previousRecordPage = page.pageNumber;
                        //previousRecordPosition = k % Page.BLOCKING_FACTOR;

                    }
                }
            }
                
        }

        public void ReadFile()
        {
            Console.WriteLine("------------");
            Console.WriteLine("Primary Area");
            Console.WriteLine("------------");
            for (int i = 0; i < primaryArea; i++)
            {
                ReadPage(i);
                for (int j = 0; j < Page.BLOCKING_FACTOR; j++)
                {
                    Record record = GetNextRecord();
                    if (!(record is null))
                    {
                        /*Console.WriteLine($"{i}.{j} K:{record.key} Numbers: {record.numbers[0]}," +
                        $" {record.numbers[1]}, {record.numbers[2]}, {record.numbers[3]}, {record.numbers[4]}, " +
                        $"Pointer: {record.page}.{record.position} Flag: {record.flag}");*/

                        Console.WriteLine($"{i}.{j} " + record.ToString());
                    }
                }
            }
            Console.WriteLine("------------");
            Console.WriteLine("OverflowArea");
            Console.WriteLine("------------");
            for (int i = 0; i < overflowArea; i++)
            {
                ReadPage(primaryArea + i);
                for (int j = 0; j < Page.BLOCKING_FACTOR; j++)
                {
                    Record record = GetNextRecord();
                    if (!(record is null))
                    {
                        /*Console.WriteLine($"{i+primaryArea}.{j} K:{record.key} Numbers: {record.numbers[0]}," +
                        $" {record.numbers[1]}, {record.numbers[2]}, {record.numbers[3]}, {record.numbers[4]}, " +
                        $"Pointer: {record.page}.{record.position} Flag: {record.flag}");*/
                        Console.WriteLine($"{i + primaryArea}.{j} " + record.ToString());
                    }
                }
            }
            //ReadIndex();
        }

        public void ReadIndex()
        {
            indexFile.ReadFile();
        }

        public void WalkThroughChain(Record record)
        {
            int prevPage;
            int prevPosition;
            Console.WriteLine(record.ToString());
            if (record.page != 0)
            {
                prevPage = page.pageNumber;
                prevPosition = page.position - 1;
                ReadPage(record.page);
                page.position = record.position;
                Record temp = GetRecordAt(record.page,record.position);
                if (temp.page != 0) 
                {
                    WalkThroughChain(temp);
                }
                else
                {
                    Console.WriteLine(temp.ToString());
                }
                ReadPage(prevPage);
                page.position = prevPosition + 1;
            }
        }

        public void ReadFileInOrder()
        {
            Console.WriteLine("-------------");
            Console.WriteLine("File in order");
            Console.WriteLine("-------------");
            int pageN = 0;
            ReadPage(pageN);
            
            
            for(int i = 0; i < primaryArea * Page.BLOCKING_FACTOR; i++)
            {
                
                Record record = GetNextRecord();
                if (!(record is null))
                {
                    WalkThroughChain(record);
                }
                
            }
            

        }

        public Record GetNextInOrderRecord(Record previous)
        {
            if(previous is null)
            {
                return null;
            }
            else
            {
                Record next;

                if(previous.page != 0)
                {
                    next = GetRecordAt(previous.page, previous.position);
                }
                else
                {
                    next = GetNextRecord();
                }

                return next;
            }
        }

        public void ReadFileInOrder2()
        {
            ReadPage(0);
            Record previous = GetNextRecord();

            if(!(previous is null))
            {
                Console.WriteLine(previous.ToString());
            }

            Record record;

            for (int i = 0; i < (primaryArea + overflowArea) * Page.BLOCKING_FACTOR - 1; i++) 
            {
                record = GetNextInOrderRecord(previous);

                if (!(record is null))
                {
                    Console.WriteLine(record.ToString());
                }

                previous = record;
                
            }
        }

        public void FindInChain(Record record, int key, ref bool found)
        {
            int prevPage;
            int prevPosition;
            if(record.key == key)
            {
                Console.WriteLine(record.ToString());
            }
            else
            {
                if (record.page != 0)
                {
                    prevPage = page.pageNumber;
                    prevPosition = page.position - 1;
                    ReadPage(record.page);
                    page.position = record.position;
                    Record temp = GetRecordAt(record.page, record.position);
                    if(temp.key == key)
                    {
                        Console.WriteLine(temp.ToString());
                        found = true;
                    }
                    else
                    {
                        if (temp.page != 0)
                        {
                            FindInChain(temp, key, ref found);
                        }
                        
                    }
                    
                    ReadPage(prevPage);
                    page.position = prevPosition + 1;
                }
                
            }
            
        }

        public void ReadRecord(int key)
        {
            Record record;
            int pageNumber = indexFile.FindPage(key);
            bool found = false;
            ReadPage(pageNumber);

            for (int i = pageNumber * Page.BLOCKING_FACTOR; i < primaryArea * Page.BLOCKING_FACTOR; i++)
            {
                record = GetNextRecord();
                if (!(record is null))
                {
                    if(record.key == key)
                    {
                        Console.WriteLine(record.ToString());
                        found = true;
                        break;
                    }
                    else
                    {
                        FindInChain(record, key, ref found);
                    }
                    
                }
            }
            if (!found)
            {
                Console.WriteLine("Record with the given key doesn't exist in the database.");
            }
        }

        public void Reorganize()
        {
            
            // zaalokowanie miejsca na dysku dla glownej przestrzeni i przestrzeni nadmiarowej
            int space = (int) Math.Ceiling((double)savedRecords / (Page.BLOCKING_FACTOR * alpha));  //liczba stron
            int mainSpace = space;
            int overflowSpace = (int) Math.Ceiling((double) vnRatio * space);
            DataFile newDataFile = new DataFile("temp", mainSpace, overflowSpace, alpha, beta, vnRatio);
            newDataFile.fs.Seek(0, SeekOrigin.Begin);

            // zaalokowanie miejsca na dysku dla pliku indeksowego
            int indexSpace = mainSpace;
            IndexFile newIndex = new IndexFile("temp", indexSpace);

            // przepisanie rekordow
            int pageN = 0;
            ReadPage(pageN);

            int atPage = 0;
            int whichPage = 0;
            for (int i = 0; i < primaryArea * Page.BLOCKING_FACTOR; i++)
            {
                
                Record record = GetNextRecord();
                if (!(record is null))
                {
                    if(atPage == 0)
                    {
                        newIndex.UpdateIndex(whichPage, record.key);
                    }
                    Record clone = record.DeepCopy();
                    clone.page = 0;
                    clone.position = 0;
                    newDataFile.fs.Write(clone.GetBytes());
                    atPage++;
                    if(atPage>= alpha * Page.BLOCKING_FACTOR)
                    {
                        for(int j = atPage; j < Page.BLOCKING_FACTOR; j++)
                        {
                            newDataFile.fs.Seek(Record.SIZE, SeekOrigin.Current);
                        }
                        atPage = 0;
                        whichPage++;
                    }
                    while(record.page != 0)
                    {
                        record = GetRecordAt(record.page, record.position);
                        clone = record.DeepCopy();
                        clone.page = 0;
                        clone.position = 0;
                        if (atPage == 0)
                        {
                            newIndex.UpdateIndex(whichPage, record.key);
                        }
                        newDataFile.fs.Write(clone.GetBytes());
                        atPage++;
                        if (atPage >= alpha * Page.BLOCKING_FACTOR)
                        {
                            for (int j = atPage; j < Page.BLOCKING_FACTOR; j++)
                            {
                                newDataFile.fs.Seek(Record.SIZE, SeekOrigin.Current);
                            }
                            atPage = 0;
                            whichPage++;
                        }
                    }
                }
                else
                {
                    //break;
                }
            }

            File.Delete(this.indexFile.filePath);
            File.Move(newDataFile.indexFile.filePath, this.indexFile.filePath);
            newIndex.filePath = this.indexFile.filePath;

            this.fs.Close();
            newDataFile.fs.Close();
            File.Delete(this.filePath);
            File.Move(newDataFile.filePath, this.filePath);
            newDataFile.filePath = this.filePath;
            this.fs = File.Open(newDataFile.filePath, FileMode.OpenOrCreate);
            this.filePath = newDataFile.filePath;
            this.alpha = newDataFile.alpha;
            this.beta = newDataFile.beta;
            this.indexFile = newIndex;
            this.overflowArea = newDataFile.overflowArea;
            this.page = newDataFile.page;
            this.previousPage = newDataFile.previousPage;
            this.previousPosition = newDataFile.previousPosition;
            this.primaryArea = newDataFile.primaryArea;
            this.savedAt = newDataFile.savedAt;
            this.vnRatio = newDataFile.vnRatio;
            this.numberOfOverflows = newDataFile.numberOfOverflows;


        }

    }
}