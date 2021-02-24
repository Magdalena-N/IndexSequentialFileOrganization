using System;
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
        public int primaryArea;    // liczba stron
        public int overflowArea;   // liczba stron
        private int previousPage;
        private int previousPosition;
        private double alpha;
        private double beta;
        public int savedAt;
        public int savedAtPosition;
        public int savedRecords;
        public double vnRatio;
        public int numberOfOverflows;
        public int numberOfPrimaryRecords;
        public int reads;
        public int writes;
        public int lastRead;
        public int lastWrite;
            

        public DataFile(string fileName, int primaryPages, int overflowPages, double alpha, double beta, double vnRatio)
        {
            this.primaryArea = primaryPages;
            this.overflowArea = overflowPages;
            this.alpha = alpha;
            this.beta = beta;
            this.vnRatio = vnRatio;
            this.savedRecords = 0;
            this.numberOfOverflows = 0;
            this.numberOfPrimaryRecords = 0;
            this.reads = 0;
            this.writes = 0; ;
            this.lastRead = -1;
            this.lastWrite = -1;

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
            this.reads++;
        }

        public void WritePage()
        {
            fs.Seek(page.pageNumber * Page.PAGE_SIZE, SeekOrigin.Begin);
            fs.Write(page.diskBlock);
            this.writes++;
        }

        private void InsertFirstRecord()
        {
            int[] num = new int[5];
            int key = -1;
            Record record = new Record(num, key);
            //InsertRecord(record);
            int page = 0;
            
            WriteRecord(page, record);
            indexFile.UpdateIndex(page, key);
        }

        public void InsertRecord(Record record)
        {
            //double ratio = (double)numberOfOverflows / savedRecords;
            double ratio = (double)numberOfOverflows / numberOfPrimaryRecords;

            if (ratio > beta || numberOfOverflows == overflowArea * Page.BLOCKING_FACTOR)
            {
                this.Reorganize();
            }

            this.reads = 0;
            this.writes = 0;
            this.indexFile.reads = 0;
            this.indexFile.writes = 0;

            int pageNumber = indexFile.FindPage(record.key);

            WriteRecord(pageNumber, record);

            if (savedAt != -1 && savedAt < primaryArea && savedAtPosition == 0)
            {
                indexFile.UpdateIndex(savedAt, record.key);
            }

            if (savedAt >= primaryArea)
            {
                numberOfOverflows++;
            }

            this.reads += indexFile.reads;
            this.writes += indexFile.writes;
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
            int oldLastRead = lastRead;

            ReadPageFromDisk(fromPage);
            page.position = position;
            byte[] recInBytes = new byte[Record.SIZE];
            Array.Copy(page.diskBlock, position * Record.SIZE, recInBytes, 0, Record.SIZE);
            Record record = new Record();
            record.FromBytes(recInBytes);

            ReadPageFromDisk(oldPage);
            page.position = oldPosition;
            lastRead = oldLastRead;
            return record;
        }

        public void ReadFile()
        {
            this.reads = 0;
            this.writes = 0;
            this.indexFile.reads = 0;
            this.indexFile.writes = 0;

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
                        Console.WriteLine($"{i}.{j} " + record.ToString());
                    }
                    else
                    {
                        Console.WriteLine($"{i}.{j} NULL");
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
                        Console.WriteLine($"{i + primaryArea}.{j} " + record.ToString());
                    }
                    else
                    {
                        Console.WriteLine($"{i + primaryArea}.{j} NULL");
                    }
                }
            }
            this.reads += indexFile.reads;
            this.writes += indexFile.writes;
        }

        public void ReadIndex()
        {
            this.reads = 0;
            this.writes = 0;
            this.indexFile.reads = 0;
            this.indexFile.writes = 0;
            indexFile.ReadFile();
            this.reads += indexFile.reads;
            this.writes += indexFile.writes;
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
                if (record.page != prevPage)
                {
                    ReadPage(record.page);
                }
                
                page.position = record.position;
                Record temp = GetRecordAt(record.page, record.position);
                if (temp.page != 0)
                {
                    WalkThroughChain(temp);
                }
                else
                {
                    Console.WriteLine(temp.ToString());
                }
                if (record.page != prevPage)
                {
                    ReadPage(prevPage);
                }
                page.position = prevPosition + 1;
            }
        }

        public void ReadFileInOrder()
        {
            this.reads = 0;
            this.writes = 0;
            this.indexFile.reads = 0;
            this.indexFile.writes = 0;

            Console.WriteLine("-------------");
            Console.WriteLine("File in order");
            Console.WriteLine("-------------");
            int pageN = 0;
            ReadPage(pageN);


            for (int i = 0; i < primaryArea * Page.BLOCKING_FACTOR; i++)
            {

                Record record = GetNextRecord();
                if (!(record is null))
                {
                    WalkThroughChain(record);
                }

            }

            this.reads += indexFile.reads;
            this.writes += indexFile.writes;

        }

        public void FindInChain(Record record, int key, ref bool found)
        {
            int prevPage;
            int prevPosition;
            int prevLastRead;

            if (record.key == key)
            {
                Console.WriteLine(record.ToString());
            }
            else
            {
                if (record.page != 0)
                {
                    prevPage = page.pageNumber;
                    prevPosition = page.position - 1;
                    prevLastRead = lastRead;
                    ReadPageFromDisk(record.page);
                    page.position = record.position;
                    Record temp = GetRecordAt(record.page, record.position);
                    if (temp.key == key)
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
                    if (!found)
                    {
                        ReadPageFromDisk(prevPage);
                        page.position = prevPosition + 1;
                        lastRead = prevLastRead;
                    }
                    
                }

            }

        }

        public void ReadRecord(int key)
        {
            this.reads = 0;
            this.writes = 0;
            this.indexFile.reads = 0;
            this.indexFile.writes = 0;

            Record record;
            int pageNumber = indexFile.FindPage(key);
            bool found = false;
            ReadPageFromDisk(pageNumber);

            for (int i = pageNumber * Page.BLOCKING_FACTOR; i < primaryArea * Page.BLOCKING_FACTOR; i++)
            {
                record = ReadNextRecord();
                if (!(record is null))
                {
                    if (record.key == key)
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
                if (found)
                {
                    break;
                }
            }
            if (!found)
            {
                Console.WriteLine("Record with the given key doesn't exist in the database.");
            }
            this.reads += indexFile.reads;
            this.writes += indexFile.writes;
        }

        public void LookingInChain(Record record, int key, ref bool found, bool delete, bool update, int[] newNumbers = null)
        {
            int prevPage;
            int prevPosition;
            int prevLastRead;

            if (record.page != 0)
            {
                prevPage = page.pageNumber;
                prevPosition = page.position - 1;
                prevLastRead = lastRead;
                ReadPageFromDisk(record.page);
                page.position = record.position;
                Record temp = GetRecordAt(record.page, record.position);
                if (temp.key == key)
                {
                    if (delete)
                    {
                        temp.flag = true;
                        byte[] recInBytes = temp.GetBytes();
                        Array.Copy(recInBytes, 0, page.diskBlock, record.position * Record.SIZE, recInBytes.Length);
                        WritePageToDisk();
                        found = true;
                        numberOfOverflows++;
                    }
                    else if (update)
                    {
                        if (temp.flag == false)
                        {
                            temp.numbers[0] = newNumbers[0];
                            temp.numbers[1] = newNumbers[1];
                            temp.numbers[2] = newNumbers[2];
                            temp.numbers[3] = newNumbers[3];
                            temp.numbers[4] = newNumbers[4];

                            byte[] recInBytes = temp.GetBytes();
                            Array.Copy(recInBytes, 0, page.diskBlock, record.position * Record.SIZE, recInBytes.Length);
                            WritePageToDisk();
                            found = true;
                        }
                        else
                        {
                            Console.WriteLine("You cannot modify deleted record.");
                        }
                    }

                }
                else
                {
                    if (temp.page != 0)
                    {
                        LookingInChain(temp, key, ref found, delete, update);
                    }

                }

                if (!found)
                {
                    ReadPageFromDisk(prevPage);
                    page.position = prevPosition + 1;
                    lastRead = prevLastRead;
                }
            }
        }

        public void DeleteRecord(int key)
        {
            if (key > 0)
            {
                this.reads = 0;
                this.writes = 0;
                this.indexFile.reads = 0;
                this.indexFile.writes = 0;

                Record record;
                int pageNumber = indexFile.FindPage(key);
                bool found = false;
                ReadPageFromDisk(pageNumber);

                for (int i = pageNumber * Page.BLOCKING_FACTOR; i < primaryArea * Page.BLOCKING_FACTOR; i++)
                {
                    record = ReadNextRecord();
                    if (!(record is null))
                    {
                        if (record.key == key)
                        {
                            record.flag = true;
                            byte[] recInBytes = record.GetBytes();
                            Array.Copy(recInBytes, 0, page.diskBlock, i % Page.BLOCKING_FACTOR * Record.SIZE, recInBytes.Length);
                            WritePageToDisk();
                            found = true;
                            numberOfOverflows++;
                            break;
                        }
                        else
                        {
                            LookingInChain(record, key, ref found, true, false);
                        }

                    }

                    if (found)
                    {
                        break;
                    }
                }
                if (!found)
                {
                    Console.WriteLine("Record with the given key doesn't exist in the database.");
                }
                this.reads += indexFile.reads;
                this.writes += indexFile.writes;
            }
            else
            {
                Console.WriteLine("You cannot delete this record.");
            }

        }

        public void WritePageToDisk()
        {
            this.writes++;
            fs.Seek(page.pageNumber * Page.PAGE_SIZE, SeekOrigin.Begin);
            fs.Write(page.diskBlock);
            lastWrite = -1;
            page.pageNumber++;
            Array.Clear(page.diskBlock, 0, page.diskBlock.Length);
            page.mode = Mode.Write;
        }

        public void Write(Record record)
        {
            if (lastWrite < Page.BLOCKING_FACTOR - 1)
            {
                lastWrite++;
                byte[] rec = record.GetBytes();
                Array.Copy(rec, 0, page.diskBlock, this.lastWrite * Record.SIZE, Record.SIZE);
            }
            else
            {
                WritePageToDisk();
                Write(record);
            }
        }

        public void WriteAt(Record record, int position)
        {
            byte[] rec = record.GetBytes();
            Array.Copy(rec, 0, page.diskBlock, position * Record.SIZE, Record.SIZE);
        }

        public void ReadPageFromDisk(int pageNumber)
        {
            if(pageNumber != page.pageNumber || page.mode == Mode.Write)
            {
                this.reads++;
                fs.Seek(pageNumber * Page.PAGE_SIZE, SeekOrigin.Begin);
                fs.Read(page.diskBlock);
                page.pageNumber = pageNumber;
                
            }
            page.mode = Mode.Read;
            page.position = 0;
            lastRead = -1;
        }

        public Record ReadNextRecord()
        {
            Record rec = new Record();
            bool exist;

            if (lastRead < Page.BLOCKING_FACTOR - 1)
            {
                this.lastRead++;
                byte[] recInBytes = new byte[Record.SIZE];
                Array.Copy(page.diskBlock, lastRead * Record.SIZE, recInBytes, 0, recInBytes.Length);
                exist = rec.FromBytes(recInBytes);
                if (exist)
                {
                    return rec;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                ReadPageFromDisk(this.page.pageNumber + 1);
                rec = ReadNextRecord();
                return rec;
            }
            
        }

        public void Reorganize()
        {
            // zaalokowanie miejsca na dysku dla glownej przestrzeni i przestrzeni nadmiarowej
            int space = (int)Math.Ceiling((double)savedRecords / (Page.BLOCKING_FACTOR * alpha));  //liczba stron
            int mainSpace = space;
            //int overflowSpace = (int)Math.Ceiling((double)vnRatio * space);
            int overflowSpace = (int)Math.Ceiling((double)beta * space);
            DataFile newDataFile = new DataFile("temp", mainSpace, overflowSpace, alpha, beta, vnRatio);
            newDataFile.ReadPageFromDisk(0);
            newDataFile.numberOfPrimaryRecords = 0;
            // zaalokowanie miejsca na dysku dla pliku indeksowego
            int indexSpace = mainSpace;
            IndexFile newIndex = new IndexFile("temp", indexSpace);

            // przepisanie rekordow
            ReadPageFromDisk(0);
            Record record;
            Record nullRecord = new Record();
            Record clone;
            int atPage = 0;
            int whichPage = 0;

            for (int i = 0; i < primaryArea * Page.BLOCKING_FACTOR; i++)
            {
                record = ReadNextRecord();
                if((!(record is null)))
                {
                    clone = record.DeepCopy();
                    clone.page = 0;
                    clone.position = 0;
                    if (clone.flag is false)
                    {
                        if (atPage == 0)
                        {
                            newIndex.UpdateIndex(whichPage, clone.key);
                        }
                        newDataFile.Write(clone);
                        newDataFile.numberOfPrimaryRecords++;
                        atPage++;
                    }

                    if (atPage >= alpha * Page.BLOCKING_FACTOR) 
                    {
                        for (int j = atPage; j < Page.BLOCKING_FACTOR; j++)
                        {
                            newDataFile.Write(nullRecord);
                        }
                        atPage = 0;
                        whichPage++;
                    }
                    while (record.page != 0)
                    {
                        record = GetRecordAt(record.page, record.position);
                        
                        clone = record.DeepCopy();
                        clone.page = 0;
                        clone.position = 0;
                        
                        if(clone.flag is false)
                        {
                            if (atPage == 0)
                            {
                                newIndex.UpdateIndex(whichPage, clone.key);
                            }
                            newDataFile.Write(clone);
                            newDataFile.numberOfPrimaryRecords++;
                            atPage++;
                        }
                        if (atPage >= alpha * Page.BLOCKING_FACTOR)
                        {
                            for (int j = atPage; j < Page.BLOCKING_FACTOR; j++)
                            {
                                newDataFile.Write(nullRecord);
                            }
                            atPage = 0;
                            whichPage++;
                        }
                        
                        
                        
                    }
                }
            }
            newDataFile.WritePageToDisk();

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
            this.numberOfPrimaryRecords = newDataFile.numberOfPrimaryRecords;
            this.lastRead = -1;
            this.lastWrite = -1;

            this.reads += indexFile.reads;
            this.writes += indexFile.writes;
        }

        public (int newPosition, int newPage) LookingForAFreePlace()
        {
            Record newTemp;
            (int newPosition, int newPage) result = (0,0);

            ReadPageFromDisk(primaryArea);  // wczytujemy pierwsza strone z overflow

            // szukamy nulla i aktualizujemy wskaznik 
            for (int k = 0; k < overflowArea * Page.BLOCKING_FACTOR; k++)
            {
                newTemp = ReadNextRecord();
                if (newTemp is null)
                {
                    result.newPosition = k % Page.BLOCKING_FACTOR;
                    result.newPage = page.pageNumber;
                    break;
                }
            }

            return result;
        }

        public void SaveRecord(Record record,int newPosition,int newPage)
        {
            WriteAt(record, newPosition);
            WritePageToDisk();
            savedAt = newPage;
            savedAtPosition = newPosition;
            savedRecords++;
            numberOfPrimaryRecords++;
        }

        public void SaveRecord2(Record record, int newPosition, int newPage)
        {
            WriteAt(record, newPosition);
            
            savedAt = newPage;
            savedAtPosition = newPosition;
            savedRecords++;
        }

        public void UpdateRecord(Record record, int nextPage, int nextPosition)
        {
            record.page = nextPage;
            record.position = nextPosition;
        }

        public void Update(int key, int[] numbers)
        {
            if (key > 0)
            {
                this.reads = 0;
                this.writes = 0;
                this.indexFile.reads = 0;
                this.indexFile.writes = 0;

                Record record;
                int pageNumber = indexFile.FindPage(key);
                bool found = false;
                ReadPageFromDisk(pageNumber);

                for (int i = pageNumber * Page.BLOCKING_FACTOR; i < primaryArea * Page.BLOCKING_FACTOR; i++)
                {
                    record = ReadNextRecord();
                    if (!(record is null))
                    {
                        if (record.key == key)
                        {
                            if (record.flag == false)
                            {
                                record.numbers[0] = numbers[0];
                                record.numbers[1] = numbers[1];
                                record.numbers[2] = numbers[2]; 
                                record.numbers[3] = numbers[3];
                                record.numbers[4] = numbers[4];
                                byte[] recInBytes = record.GetBytes();
                                Array.Copy(recInBytes, 0, page.diskBlock, i % Page.BLOCKING_FACTOR * Record.SIZE, recInBytes.Length);
                                WritePageToDisk();
                            }
                            else
                            {
                                Console.WriteLine("You cannot modify deleted record.");
                            }
                            found = true;
                            break;
                        }
                        else
                        {
                            LookingInChain(record, key, ref found, false, true, numbers);
                        }
                    }
                    if (found)
                    {
                        break;
                    }
                }
                if (!found)
                {
                    Console.WriteLine("Record with the given key doesn't exist in the database.");
                }
                this.reads += indexFile.reads;
                this.writes += indexFile.writes;
            }
            else
            {
                Console.WriteLine("You cannot modify this record.");
            }
        }

        public void UpdatePreviousRecord(int previousRecordPage, Record previousRecord, int newPage, int newPosition, int previousRecordPosition)
        {
            ReadPageFromDisk(previousRecordPage);
            UpdateRecord(previousRecord, newPage, newPosition);
            WriteAt(previousRecord, previousRecordPosition);
            WritePageToDisk();
        }

        public void WriteToOverflow(Record previousRecord, Record record, ref bool written, int previousRecordPage, int previousRecordPosition)
        {
            Record newTemp;
            int nextPage;
            int nextPosition;
            int newPosition;
            int newPage;
            // overflow
            if (previousRecord.page != 0)
            {
                // przejscie do miejsca wskazywanego
                ReadPageFromDisk(previousRecord.page);
                newTemp = GetRecordAt(previousRecord.position);

                if (newTemp.key > record.key)
                {
                    nextPage = page.pageNumber;
                    nextPosition = previousRecord.position;

                    (newPosition, newPage) = LookingForAFreePlace();

                    // zaktualizuj aktualny wskaznik
                    UpdateRecord(record, nextPage, nextPosition);
                    SaveRecord2(record, newPosition, newPage);
                    written = true;
                    if(newPage != previousRecordPage)
                    {
                        WritePageToDisk();
                    }
                    // zaktualizuj wskaznik poprzedniego
                    UpdatePreviousRecord(previousRecordPage, previousRecord, newPage, newPosition, previousRecordPosition);

                   
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
                            ReadPageFromDisk(newTemp.page);
                            Record newTemp2 = GetRecordAt(newTemp.position);
                            if (newTemp2.key > record.key)
                            {
                                // zapisz rekord
                                nextPage = page.pageNumber;
                                nextPosition = previousRecord.position;

                                (newPosition, newPage) = LookingForAFreePlace();
                                UpdateRecord(record, nextPage, nextPosition);
                                SaveRecord2(record, newPosition, newPage);
                                written = true;
                                if (newPage != previousRecordPage)
                                {
                                    WritePageToDisk();
                                }
                                // zaktualizuj wskaznik poprzedniego
                                UpdatePreviousRecord(previousRecordPage, previousRecord, newPage, newPosition, previousRecordPosition);
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
                            (newPosition, newPage) = LookingForAFreePlace();
                            // zapisz rekord
                            SaveRecord2(record, newPosition, newPage);
                            written = true;
                            if (newPage != previousRecordPage)
                            {
                                WritePageToDisk();
                            }
                            // zaktualizuj wskaznik poprzedniego
                            UpdatePreviousRecord(previousRecordPage, previousRecord, newPage, newPosition, previousRecordPosition);
                            break;

                        }
                    }

                }
                else if (newTemp.key == record.key)
                {
                    Console.WriteLine("Record with the same key already exists in the database. You cannot insert this record.");
                    written = true;
                    this.savedAt = -1;
                }
            }
            else
            {
                ReadPageFromDisk(primaryArea);
                (newPosition, newPage) = LookingForAFreePlace();

                // wpisz rekord
                SaveRecord2(record, newPosition, newPage);
                written = true;
                if (newPage != previousRecordPage)
                {
                    WritePageToDisk();
                }
                // zaktualizuj wskaznik poprzedniego
                UpdatePreviousRecord(previousRecordPage, previousRecord, newPage, newPosition, previousRecordPosition);
            }
        }

        public void WriteRecord(int pageNumber, Record record)
        {
            
            ReadPageFromDisk(pageNumber);
            Record previousRecord = GetRecordAt(0);
            int previousRecordPage = pageNumber;
            int previousRecordPosition = 0;
            int newPosition;
            int newPage;
            

            bool written = false;

            for (int i = pageNumber * Page.BLOCKING_FACTOR; i < primaryArea * Page.BLOCKING_FACTOR; i++)
            {
                Record temp = ReadNextRecord();
                if (temp is null)
                {
                    // wpisz rekord
                    newPosition = i % Page.BLOCKING_FACTOR;
                    newPage = page.pageNumber;
                    SaveRecord(record, newPosition, newPage);
                    written = true;
                    break;
                }
                else if (temp.key > record.key)
                {
                    WriteToOverflow(previousRecord, record, ref written, previousRecordPage, previousRecordPosition);
                    break;
                }
                else if (temp.key == record.key)
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
                WriteToOverflow(previousRecord, record, ref written, previousRecordPage, previousRecordPosition);
            }

        }

    }
}
