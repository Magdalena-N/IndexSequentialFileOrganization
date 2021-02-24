using System;
using System.Collections.Generic;
using System.Text;

namespace Projekt2v3
{
    class IndexRow
    {
        public const int ROW_SIZE = 8;
        public int key;
        public int page;

        public IndexRow()
        {
            this.key = 0;
            this.page = 0;
        }

        public IndexRow(int key, int page)
        {
            this.key = key;
            this.page = page;
        }

        public byte[] GetBytes()
        {
            byte[] key = BitConverter.GetBytes(this.key);
            byte[] page = BitConverter.GetBytes(this.page);
            byte[] row = new byte[key.Length + page.Length];
            Array.Copy(key, 0, row, 0, key.Length);
            Array.Copy(page, 0, row, key.Length, page.Length);
            return row;
        }

        public void FromBytes(byte[] row)
        {
            byte[] key = new byte[4];
            byte[] page = new byte[4];
            Array.Copy(row, 0, key, 0, key.Length);
            Array.Copy(row, key.Length, page, 0, page.Length);
            this.key = BitConverter.ToInt32(key);
            this.page = BitConverter.ToInt32(page);
        }

        public override string ToString()
        {
            string row = $"K: {this.key} P: {this.page}";
            return row;
        }

    }
}
