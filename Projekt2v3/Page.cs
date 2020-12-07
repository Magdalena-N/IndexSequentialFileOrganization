using System;
using System.Collections.Generic;
using System.Text;

namespace Projekt2v3
{
    class Page
    {
        public const int PAGE_SIZE = 66;   // rozmiar strony podana w bajtach
        public const int BLOCKING_FACTOR = PAGE_SIZE / Record.SIZE; // wspolczynnik blokowania (srednia liczba rekordow na stronie) 435 / 29 = 15
        public int pageNumber;
        public byte[] diskBlock = new byte[PAGE_SIZE];
        public int position;
        public Page()
        {
            position = 0;
            pageNumber = 0;
        }
        
    }
}
