using System;
using System.Collections.Generic;
using System.Text;

namespace Projekt2v3
{ 
    class Record
    {
        
        public const int SIZE = 33; // rozmiar rekordu podany w bajtach 8 * 4 + 1 = 33

        public int key;    // 4 bajty
        public int[] numbers = new int[5];  // 5 * 4 = 20 bajtow
        public int page;  // 4 bajty
        public int position;  // 4 bajty
        public bool flag;   // 1 bajt

        public Record()
        {

        }
        public Record(int[] num)
        {
            numbers = num;
            key = ComputeDivisors();
        }
        public Record(int[] num, int key)
        {
            numbers = num;
            this.key = key;
        }
        public int ComputeDivisors()
        {
            int divisors = 0;
            long product = 1;
            foreach (int number in numbers)
            {
                product *= number;
            }
            if (product == 0)
            {
                divisors = 0;
            }
            else
            {
                for (long i = 1; i * i <= product; i++)
                {
                    if (product % i == 0)
                    {
                        if (product / i != i)
                        {
                            divisors += 2;
                        }
                        else
                        {
                            divisors++;
                        }

                    }
                }
            }

            return divisors;
        }

        public byte[] GetBytes()
        {
            List<byte> record = new List<byte>();
            byte[] key = BitConverter.GetBytes(this.key);
            foreach (byte k in key)
            {
                record.Add(k);
            }
            foreach (int number in numbers)
            {
                byte[] num = BitConverter.GetBytes(number);
                foreach (byte n in num)
                {
                    record.Add(n);
                }

            }
            byte[] page = BitConverter.GetBytes(this.page);
            foreach (byte p in page)
            {
                record.Add(p);
            }
            byte[] position = BitConverter.GetBytes(this.position);
            foreach (byte p in position)
            {
                record.Add(p);
            }
            byte[] flag = BitConverter.GetBytes(this.flag);
            foreach (byte f in flag)
            {
                record.Add(f);
            }

            byte[] rec = record.ToArray();
            return rec;
        }

        public bool FromBytes(byte[] record)
        {
            int i = 0;
            byte[] k = new byte[4];
            Array.Copy(record, i, k, 0, 4);
            key = BitConverter.ToInt32(k);
            if (key == 0)
            {
                return false;
            }
            i += 4;
            byte[] num1 = new byte[4];
            Array.Copy(record, i, num1, 0, 4);
            numbers[0] = BitConverter.ToInt32(num1);
            i += 4;
            byte[] num2 = new byte[4];
            Array.Copy(record, i, num2, 0, 4);
            numbers[1] = BitConverter.ToInt32(num2);
            i += 4;
            byte[] num3 = new byte[4];
            Array.Copy(record, i, num3, 0, 4);
            numbers[2] = BitConverter.ToInt32(num3);
            i += 4;
            byte[] num4 = new byte[4];
            Array.Copy(record, i, num4, 0, 4);
            numbers[3] = BitConverter.ToInt32(num4);
            i += 4;
            byte[] num5 = new byte[4];
            Array.Copy(record, i, num5, 0, 4);
            numbers[4] = BitConverter.ToInt32(num5);
            i += 4;
            byte[] p = new byte[4];
            Array.Copy(record, i, p, 0, 4);
            page = BitConverter.ToInt32(p);
            i += 4;
            byte[] pos = new byte[4];
            Array.Copy(record, i, pos, 0, 4);
            position = BitConverter.ToInt32(pos);
            i += 4;
            byte[] f = new byte[1];
            Array.Copy(record, i, f, 0, 1);
            flag = BitConverter.ToBoolean(f);
            return true;
        }
        public override string ToString()
        {
            string record = $"K: {this.key} Numbers: { this.numbers[0]}," +
                        $" {this.numbers[1]}, {this.numbers[2]}, {this.numbers[3]}, {this.numbers[4]}, " +
                        $"Pointer: {this.page}.{this.position} Deleted: {this.flag}";
            return record;
        }
        public Record DeepCopy()
        {
            Record temp = (Record)this.MemberwiseClone();
            temp.key = this.key;
            Array.Copy(this.numbers, temp.numbers, 5);
            temp.page = this.page;
            temp.position = this.position;
            temp.flag = this.flag;
            return temp;
        }
    }
}
