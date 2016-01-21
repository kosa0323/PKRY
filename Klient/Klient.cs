using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.X509;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Klient
{
    public class  Klient
    {
        #region Certyfikaty,KLucze
        //Wczytywanie klucza prywatnego Sprzedającego
        StreamReader readerKluczPrywatny = null;
        PemReader pemReaderKluczPrywatny = null;
        AsymmetricCipherKeyPair keyPairPriv = null;
        AsymmetricKeyParameter kluczPrywatnyKlienta = null;

        //Wczytywanie klucza publicznego bramy przeznaczonego tylko dla klienta
        StreamReader readerKluczPublicznyBramyTylkoDlaKlienta = null;
        PemReader pemReaderkluczPublicznyBramyTylkoDlaKlienta = null;
        AsymmetricKeyParameter kluczPublicznyBramyTylkoDlaKlienta = null;

        //Wczytywanie certyfikatu Klienta
        string sciezkaCertyfikatuKlienta = "CertyfikatKlienta.crt";
        X509Certificate2 certyfikatKlienta = null;
        #endregion

        AsymmetricKeyParameter kluczPublicznySprzedawcy = null;
        string hashOdebranaJawnaOdpowiedz = null;
        string odkodowanaShashowanaPierwszaWiadomoscRes = null;
        public void WczytajPoczatkoweParametry()
        {
            //Wczytywanie klucza prywatnego Sprzedającego
            readerKluczPrywatny = new StreamReader("KluczPrywatnyKlienta.pem");
            pemReaderKluczPrywatny = new PemReader(readerKluczPrywatny);
            keyPairPriv = (AsymmetricCipherKeyPair)pemReaderKluczPrywatny.ReadObject();
            kluczPrywatnyKlienta = keyPairPriv.Private;

            //Wczytywanie klucza publicznego bramy przeznaczonego tylko dla klienta
            readerKluczPublicznyBramyTylkoDlaKlienta = new StreamReader("KluczPublicznyBramyTylkoDlaKlienta.pem");
            pemReaderkluczPublicznyBramyTylkoDlaKlienta = new PemReader(readerKluczPublicznyBramyTylkoDlaKlienta);
            kluczPublicznyBramyTylkoDlaKlienta = (AsymmetricKeyParameter)pemReaderkluczPublicznyBramyTylkoDlaKlienta.ReadObject();

            //Wczytywanie certyfikatu Klienta
            sciezkaCertyfikatuKlienta = "CertyfikatKlienta.crt";
            certyfikatKlienta = new X509Certificate2(sciezkaCertyfikatuKlienta);
        }

        public string SHA1(string dohashowania)
        {
            SHA1CryptoServiceProvider SHA1 = new SHA1CryptoServiceProvider();
            SHA1.ComputeHash(ASCIIEncoding.ASCII.GetBytes(dohashowania));
            byte[] Re = SHA1.Hash;
            StringBuilder StringBuilder = new StringBuilder();
            foreach (byte b in Re)
            {
                StringBuilder.Append(b.ToString("x2"));
            }
            return StringBuilder.ToString();
        }
        public string RsaEncrypt(string clearText, AsymmetricKeyParameter prywatny)
        {
            AsymmetricKeyParameter key = prywatny;
            var bytesToEncrypt = Encoding.UTF8.GetBytes(clearText);
            var encryptEngine = new Pkcs1Encoding(new RsaEngine());
            encryptEngine.Init(true, key);
            var encrypted = Convert.ToBase64String(encryptEngine.ProcessBlock(bytesToEncrypt, 0, bytesToEncrypt.Length));
            return encrypted;
        }
        public string RsaDecrypt(string base64Input, AsymmetricKeyParameter publiczny)
        {
            AsymmetricKeyParameter key = publiczny;
            var bytesToDecrypt = Convert.FromBase64String(base64Input);
            var decryptEngine = new Pkcs1Encoding(new RsaEngine());
            decryptEngine.Init(false, key);
            string decrypted = Encoding.UTF8.GetString(decryptEngine.ProcessBlock(bytesToDecrypt, 0, bytesToDecrypt.Length));
            return decrypted;
        }
       

        public  void StworzKlienta()
        {
            byte[] bufor = new byte[1024];
            Kupujacy Kupujacy = new Kupujacy();

            WczytajPoczatkoweParametry();

            try
            {
                Socket KlientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                try
                {
                    KlientSocket.Connect("127.0.0.1", 1234);
                    NetworkStream stream = new NetworkStream(KlientSocket);
                    BinaryWriter bw = new BinaryWriter(stream);
                    BinaryReader br = new BinaryReader(stream);
                   
                    //rozpoczecie Komunikacji - wysłanie pierwszej Wiadomosci
                    Console.WriteLine("* Wysyłam Wiadomość PURCHASE_INIT do Sprzedawcy");
                    Console.WriteLine("   - Nazwa marki karty klienta");
                    Console.WriteLine("   - Zapytanie o certyfikat");
                    Console.WriteLine("");

                    bw.Write(Protokol.PURCHASE_INIT);
                    string[] pierwszaWiadomosc = { Kupujacy.NazwaMarkiKarty, "Podaj Certyfikaty" };
                    foreach (string s in pierwszaWiadomosc)
                    {
                        bw.Write(s);
                    }
                    bw.Flush();

                    bool czy = true;
                    //Odczytanie Pierwszej wiadomości
                    while (czy)
                    {
                        string Odbierz = br.ReadString();

                        switch(Odbierz)
                        {
                            case Protokol.PURCHASE_INIT_RES:
                                PurchaseInitResp(bw, br);
                                //czy = false;
                                break;
                            case Protokol.PURCHASE_REQ_RES:
                                PurchaseInitResp(bw, br);
                                czy = false;
                                break;
                            default:
                                bw.Write(Protokol.BLAD);
                                Console.WriteLine("* BŁĄD W KOMUNIKACJI!");
                                czy = false;
                                break;
                        }
                    }
                    //KlientSocket.Shutdown(SocketShutdown.Both);
                    //KlientSocket.Close();
                }
                catch (ArgumentNullException ane)
                {
                    Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                }
                catch (SocketException se)
                {
                    Console.WriteLine("SocketException : {0}", se.ToString());
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public void PurchaseInitResp(BinaryWriter bw, BinaryReader br)
        {
            Console.WriteLine("* Odebrałem wiadomość PURCHASE_INIT_RES od Sprzedawcy");
            Console.WriteLine("");
            string[] odebranaPierwszaWiadomoscRes = { br.ReadString(), br.ReadString() };
            int dlCS = br.ReadInt32();
            byte[] daneCertyfikatuSprzedawcy = br.ReadBytes(dlCS);
            int dlCB = br.ReadInt32();
            byte[] daneCertyfikatuBramy = br.ReadBytes(dlCB);

            X509Certificate2 certyfikatSprzedawcy = new X509Certificate2();
            certyfikatSprzedawcy.Import(daneCertyfikatuSprzedawcy);
            X509Certificate2 certyfikatBramy = new X509Certificate2();
            certyfikatBramy.Import(daneCertyfikatuBramy);

            //Uzyskanie klucza publicznego sprzedawcy z certyfikatu
            X509CertificateParser certyfikatParser = new X509CertificateParser();
            Org.BouncyCastle.X509.X509Certificate certyfikatBouncy = certyfikatParser.ReadCertificate(certyfikatSprzedawcy.GetRawCertData());
            kluczPublicznySprzedawcy = certyfikatBouncy.GetPublicKey();

            //sprawdzenie czy poprawnie przesłano wiadomość
            hashOdebranaJawnaOdpowiedz = SHA1(odebranaPierwszaWiadomoscRes[0]);
            odkodowanaShashowanaPierwszaWiadomoscRes = RsaDecrypt(odebranaPierwszaWiadomoscRes[1], kluczPublicznySprzedawcy);
            if (hashOdebranaJawnaOdpowiedz == odkodowanaShashowanaPierwszaWiadomoscRes)
            {
                Console.WriteLine("* Wiadomość PURCHASE_INIT_RES poprawnie odkodowana");
                Console.WriteLine("");

            }
            else
            {
                bw.Write(Protokol.BLAD);
                Console.WriteLine("* BŁĄD W KOMUNIKACJI!");
            }

            //tu wysyła drugą wiadomość
            string identyfikatorTransakcji = "1111";


            string drugaWiadomosc_OIsum = "OrderInformation" + " " + identyfikatorTransakcji;
            string drugaWiadomosc_PIsum = new Kupujacy().NumerKarty + " " + identyfikatorTransakcji;

            string hashDrugaWiadomosc_OI = SHA1(drugaWiadomosc_OIsum);
            string hashDrugaWiadomosc_PI = SHA1(drugaWiadomosc_PIsum);

            string DrugaWiadomoscOPI = hashDrugaWiadomosc_OI + hashDrugaWiadomosc_PI;
            string hashDrugaWiadomoscOPI = SHA1(DrugaWiadomoscOPI);

            string zakodowanaHashDrugaWiadomoscOPI = RsaEncrypt(hashDrugaWiadomoscOPI, kluczPrywatnyKlienta); //dla Klienta

            string zakodowanaHashDrugaWiadomoscOPIDlaBramy = RsaEncrypt(hashDrugaWiadomoscOPI, kluczPublicznyBramyTylkoDlaKlienta);
            string WiadomoscPlatniczaDlaBramy = drugaWiadomosc_PIsum + " " + zakodowanaHashDrugaWiadomoscOPIDlaBramy;

            byte[] certyfikatKlientaDoWyslania = certyfikatKlienta.GetRawCertData();
            int dlCK = certyfikatKlientaDoWyslania.Length;

            bw.Write(Protokol.PURCHASE_REQ);

            Console.WriteLine("* Wysyłam Wiadomość PURCHASE_REQ do Sprzedawcy");
            Console.WriteLine("   - Jawna wiadomość dotycząca  zamówienia");
            Console.WriteLine("   - Hash wiadomości dotyczącej płatności");
            Console.WriteLine("   - Zakodowany hash wiadomości dotyczącej płatności i zamówienia");
            Console.WriteLine("   - Zakodowana wiadomość platnicza dla bramy");
            Console.WriteLine("   - Certyfikat Klienta");
            Console.WriteLine("");

            bw.Write(WiadomoscPlatniczaDlaBramy);
            bw.Write(hashDrugaWiadomosc_PI);
            bw.Write(drugaWiadomosc_OIsum);
            bw.Write(zakodowanaHashDrugaWiadomoscOPI);//dla sprzedawcy
            bw.Write(dlCK);
            bw.Write(certyfikatKlientaDoWyslania);
        }

        public void PurchaseReqResp(BinaryWriter bw, BinaryReader br)
        {
            Console.WriteLine("* Odebrałem wiadomość PURCHASE_REQ_RES od Sprzedawcy");
            Console.WriteLine("");
            string[] odebranaDrugaWiadomoscRes = { br.ReadString(), br.ReadString() };

            string hashOdebranaJawnaOdpowiedzDruga = SHA1(odebranaDrugaWiadomoscRes[0]);
            string odkodowanaShashowanaDrugaWiadomoscRes = RsaDecrypt(odebranaDrugaWiadomoscRes[1], kluczPublicznySprzedawcy);
            if (hashOdebranaJawnaOdpowiedz == odkodowanaShashowanaPierwszaWiadomoscRes)
            {
                Console.WriteLine("* Wiadomość PURCHASE_REQ_RES poprawnie odkodowana");
                Console.WriteLine("");
                Console.WriteLine("* Koniec transakcji");

            }
            else
            {
                bw.Write(Protokol.BLAD);
                Console.WriteLine("* BŁĄD W KOMUNIKACJI!");
            }
        }
    }
}
