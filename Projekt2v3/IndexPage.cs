using System;
using System.Collections.Generic;
using System.Text;

namespace Projekt2v3
{
    class IndexPage
    {
        public const int PAGE_SIZE = 16;   // rozmiar strony podana w bajtach
        public const int BLOCKING_FACTOR = PAGE_SIZE / IndexRow.ROW_SIZE; // wspolczynnik blokowania (srednia liczba wierszy na stronie) 16 / 8 = 2
        public int pageNumber;
        public byte[] diskBlock = new byte[PAGE_SIZE];
        public int position;

        public IndexPage()
        {
            position = 0;
            pageNumber = 0;
        }
    }
}
