using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Klient
{
    class Protokol
    {
        public const string PURCHASE_INIT = "#Inicjuj_Zakupy";
        public const string PURCHASE_INIT_RES = "#Inicjuj_Zakupy_Odpowiedz";
        public const string PURCHASE_REQ = "#Zapytanie_Zakupy";
        public const string PURCHASE_REQ_RES = "#Zapytanie_Zakupy_Odpowiedz";
        public const string BLAD = "#Błąd_w_Komunikacji";
        public const string AUTHORIZATION_REQ = "#Zapytanie_o_Autoryzację";
        public const string AUTHORIZATION_RES = "#Odpowiedz_o_Autoryzację";
        public const string PAYMENT_REQ = "#Zapytanie_o_Platnosc";
        public const string PAYMENT_RES = "#Odpowiedz_o_Platnosc";
    }
}
