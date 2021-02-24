using System;
using System.Collections.Generic;
using System.Text;

namespace Projekt2v3
{
    public enum Mode
    {
        None,
        Read,
        Write
    }

    class Page
    {
        public const int PAGE_SIZE = 132;   // rozmiar strony podana w bajtach
        public const int BLOCKING_FACTOR = PAGE_SIZE / Record.SIZE; // wspolczynnik blokowania (srednia liczba rekordow na stronie) 
        public int pageNumber;
        public byte[] diskBlock = new byte[PAGE_SIZE];
        public int position;
        public Mode mode; 

        public Page()
        {
            position = 0;
            pageNumber = -1;
            mode = Mode.None;
        }
        
    }
}
