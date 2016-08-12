using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Xml;


namespace KGMConsole
{
    class Program
    {
        public static DataTable hataTablo;                        
        public static StringBuilder sb = new StringBuilder();     
        static void Main(string[] args)
        {
            hataTablo = new DataTable();
            hataTablo.Columns.Add("sıra", typeof(string));
            hataTablo.Columns.Add("hata", typeof(string));

            Bulten();
            KapalıYol();
            ÇalışmaYapılanYol();

            MailGonder();

        }
        static void Bulten()
        {
            #region Bölüm Açıklaması
            /* 
             Yapılanlar:
             sayac ve sayac 1 hata tablosu ıcın tanımlanmıstır.
             WebClient ile kgm sayfasına baglanılıyor. Ve belirtilen Html Tag'leri içinde istenen bilgiler alındı
             Tarih çekilerek Yol bilgisi DataTablenın başına yazılıdı
             Yol bilgisi devamına yazılmıştır.
             Sorun cıkması halinde hata tablosuna eklenerek mail atılacaktır.
             Thread programın donmaması ıcın gereklı.(Caslısırken mudahıl olabilmek için)
             region içinde BultenKaydet fonksıyonuna dataTable ıcındekı verıler gonderılıyor.
             DataTable'a ekleme:tek kolona satırlar halinde yazılıyor. 
             ÖNEMLİ ! Bulten günlük olarak çekilip Netdata da .tablolar sılınmeden arşiv şeklınde tutulmaktadır.(Her gün üzerinde yazılacak )
             */
            #endregion
            
            int sayac = 0;
            int sayac1 = 0;
            DataTable dt = new DataTable();
            dt.Columns.Add(new DataColumn("YolBilgisi", typeof(String)));

            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            string siteHtml = wc.DownloadString("http://www.kgm.gov.tr/Sayfalar/KGM/SiteTr/YolDanisma/GunlukYolDurumuBulteni.aspx");
            HtmlAgilityPack.HtmlDocument veri = new HtmlAgilityPack.HtmlDocument();
            veri.LoadHtml(siteHtml);

           // ClearNetdataTable("b202f561"); //Arşivleneceği için kullanılmayacak.(denemelerı silmek için kullanıldı)

            #region Veri çekimi
            foreach (HtmlNode hn in veri.DocumentNode.SelectNodes("//*[@id='ctl00_ctl56_g_c57603e9_909f_4479_8fd2_b894df7c2a32']/table/tr"))
            {
                sayac++;
                try
                {
                    string tarih = "";
                    tarih = Duzenle(hn.SelectNodes("td[2]/b")[0].InnerText);
                    DataRow dr = dt.NewRow();
                    dr["YolBilgisi"] = tarih;
                    dt.Rows.Add(dr);
                    break;
                }
                catch(Exception hata)
                {
                    DataRow dr = hataTablo.NewRow();
                    dr["sıra"] = sayac + ". Tarih Hatası";
                    dr["hata"] = hata.Message;
                    hataTablo.Rows.Add(dr);
                }
                
               

            }


            foreach (HtmlNode hn in veri.DocumentNode.SelectNodes("//*[@id='ctl00_ctl56_g_c57603e9_909f_4479_8fd2_b894df7c2a32']/table/tr"))
            {
                sayac1++;
                Thread.Sleep(100);

                try
                {
                    string ntext = Duzenle(hn.SelectNodes("td")[0].InnerText);
                    if (ntext != "")
                    {
                        DataRow dr = dt.NewRow();
                        dr["YolBilgisi"] = ntext;
                        dt.Rows.Add(dr);

                    }
                }
                catch (Exception hata)
                {
                    DataRow dr = hataTablo.NewRow();
                    dr["sıra"] = sayac1 + ".Yol Bilgiisi Hatası";
                    dr["hata"] = hata.Message;
                    hataTablo.Rows.Add(dr);
                }

            }
            #endregion
            
            Console.WriteLine("bulten cekıldı", dt);

            #region Kaydetme

            // bu yapı if else şeklındedır.
            string Tarih = dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : DateTime.Now.ToShortDateString();
            dt.Rows.RemoveAt(0);

            foreach (DataRow item in dt.Rows)
            {
                BultenKaydet(Tarih, item[0].ToString());
            }

            

            #endregion 

            StringBuilderDoldur("Bülten", hataTablo);
        }
        private static bool BultenKaydet(string Tarih, string YolBilgisi)
        {
            #region Bölüm Açıklaması
            /*
             Yapılanlar:
             netdata.com dan alınan apıkey ve AccPo kullanım dökümanı ıle netdata.com ' a KGM'den Bulten() fonksiyonuyla alınan veriler aktarılıyor.
             Buraya aktarma işlemi Bulten() fonksıyonundakı Kaydetme açıklamasından gelıyor. 
             netdata.com'a kaydetme işlemide tarih ve yol bilgisi olmak üzere iki sütun olarak yapılmaktadır.
             İşlemimiz bittiginde eklenebildiyse true eklenemedıyse false donderecektir.
            */
            #endregion

            string Result = "";
            string Content = @"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
 <soap:Body>
<InsertRecord xmlns=""http://tempuri.org/"">
      <APIKey>b202f561</APIKey>
      <InsertList>
        <AccPoKeyValuePair>
          <Key>dc_Tarih</Key>
          <Value>[0]</Value>
        </AccPoKeyValuePair>
        <AccPoKeyValuePair>
          <Key>dc_Yol_Bilgisi</Key>
          <Value>[1]</Value>
        </AccPoKeyValuePair>
     </InsertList>
  </InsertRecord>
  </soap:Body>
</soap:Envelope>";
            string url = "http://www.netdata.com/AccPo.asmx";
            string contentType = "text/xml; charset=utf-8";
            string method = "POST";
            string header = "SOAPAction: \"http://tempuri.org/InsertRecord\"";

            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = method;
            req.ContentType = contentType;
            req.Headers.Add(header);

            Stream strRequest = req.GetRequestStream();
            StreamWriter sw = new StreamWriter(strRequest);
            sw.Write(Content.Replace("[0]", Tarih).Replace("[1]", YolBilgisi));
            sw.Close();

            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
            Stream strResponse = resp.GetResponseStream();
            StreamReader sr = new StreamReader(strResponse, System.Text.Encoding.ASCII);
            Result = sr.ReadToEnd();
            sr.Close();

            if (Result.Contains("Eklendi"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        static void KapalıYol()
        {
            #region Bölüm Açıklaması
            /* 
             Yapılanlar:
             sayac  hata tablosu ıcın tanımlanmıstır.
             WebClient ile kgm sayfasına baglanılıyor. Ve belirtilen Html Tag'leri içinde istenen bilgiler alındı
             Kapalı yol bilgisi 11 sütunda DataTable'a yenı satırlar halinde yazıldı
             Sorun cıkması halinde hata tablosuna eklenerek mail atılacaktır.
             Thread programın donmaması ıcın gereklı.(Caslısırken mudahıl olabilmek için)
             region içinde KapalıYolKaydet fonksıyonuna dataTable ıcındekı verıler gonderılıyor.
             ÖNEMLİ ! Kapalı yollar hergun netdata.com'daki veriler silinip tekrar kaydedilmesi şeklinde uygulanır.
             */
            #endregion

            int sayac = 0;
            DataTable dtt = new DataTable();

            dtt.Columns.Add(new DataColumn("Sıra No", typeof(String)));
            dtt.Columns.Add(new DataColumn("Bölge No", typeof(String)));
            dtt.Columns.Add(new DataColumn("Şube No", typeof(String)));
            dtt.Columns.Add(new DataColumn("K.K. NO", typeof(String)));
            dtt.Columns.Add(new DataColumn("Kış Programı Durumu", typeof(String)));
            dtt.Columns.Add(new DataColumn("YOLUN ADI", typeof(String)));
            dtt.Columns.Add(new DataColumn("KAPANMA NEDENİ", typeof(String)));
            dtt.Columns.Add(new DataColumn("KAPANAN KESİM BAŞLANGIÇ (km)", typeof(String)));
            dtt.Columns.Add(new DataColumn("KAPANAN KESİM SON (km)", typeof(String)));
            dtt.Columns.Add(new DataColumn("KAPANMA TARİHİ GÜN", typeof(String)));
            dtt.Columns.Add(new DataColumn("KAPANMA TARİHİ SAAT", typeof(String)));

            WebClient wc2 = new WebClient();
            wc2.Encoding = Encoding.UTF8;
            string siteHtml = wc2.DownloadString("http://www.kgm.gov.tr/Sayfalar/KGM/SiteTr/YolDanisma/TrafigeKapaliYollar.aspx");
            HtmlAgilityPack.HtmlDocument veri2 = new HtmlAgilityPack.HtmlDocument();
            veri2.LoadHtml(siteHtml);

            ClearNetdataTable("458a41d8");// netdata.com'daki verilerin silinmesi için ( apıkey kendı apısıne aıt )

            #region Veri Çekimi
            //Burdaki for Html taglerindekileri sıra sıra alması ıcın 500 tane olmasının sebebı ıse verılerın 100'ü asmayacak sekılde olması gerektıgınde daha fazlasına ayarlanabılır
            for (int i = 2; i < 500; i++)
            {

                try
                {

                    foreach (HtmlNode hnn in veri2.DocumentNode.SelectNodes("//*[@id='ctl00_ctl56_g_7dbb8781_ce1a_4627_b93b_d679946f4296']/table[2]/tr[" + i.ToString() + "]"))
                    {
                        sayac++;
                        Thread.Sleep(100);
                        DataRow dr = dtt.NewRow();
                        try
                        {
                            string sira = "";
                            sira = Duzenle(hnn.SelectNodes("td")[0].InnerText);

                            dr["Sıra No"] = sira;

                        }
                        catch (Exception hata)
                        {
                            DataRow dr2 = hataTablo.NewRow();
                            dr2["sıra"] = sayac + ". Sıra Hatası";
                            dr2["hata"] = hata.Message;
                            hataTablo.Rows.Add(dr2);
                        }
                        try
                        {
                            string bolge = "";
                            bolge = Duzenle(hnn.SelectNodes("td")[1].InnerText);

                            dr["Bölge No"] = bolge;

                        }
                        catch (Exception hata)
                        {
                            DataRow dr2 = hataTablo.NewRow();
                            dr2["sıra"] = sayac + ". Bölge Hatası";
                            dr2["hata"] = hata.Message;
                            hataTablo.Rows.Add(dr2);
                        }
                        try
                        {
                            string sube = "";
                            sube = Duzenle(hnn.SelectNodes("td")[2].InnerText);

                            dr["Şube No"] = sube;

                        }
                        catch (Exception hata)
                        {
                            DataRow dr2 = hataTablo.NewRow();
                            dr2["sıra"] = sayac + ". Şube Hatası";
                            dr2["hata"] = hata.Message;
                            hataTablo.Rows.Add(dr2);
                        }
                        try
                        {
                            string kk = "";
                            kk = Duzenle(hnn.SelectNodes("td")[3].InnerText);

                            dr["K.K. NO"] = kk;

                        }
                        catch (Exception hata)
                        {
                            DataRow dr2 = hataTablo.NewRow();
                            dr2["sıra"] = sayac + ". K:K NO Hatası";
                            dr2["hata"] = hata.Message;
                            hataTablo.Rows.Add(dr2);
                        }
                        try
                        {
                            string kıs = "";
                            kıs = Duzenle(hnn.SelectNodes("td")[4].InnerText);

                            dr["Kış Programı Durumu"] = kıs;

                        }
                        catch (Exception hata)
                        {
                            DataRow dr2 = hataTablo.NewRow();
                            dr2["sıra"] = sayac + ". Kış Programı Durumu Hatası";
                            dr2["hata"] = hata.Message;
                            hataTablo.Rows.Add(dr2);
                        }
                        try
                        {
                            string ad = "";
                            ad = Duzenle(hnn.SelectNodes("td")[5].InnerText);

                            dr["YOLUN ADI"] = ad;

                        }
                        catch (Exception hata)
                        {
                            DataRow dr2 = hataTablo.NewRow();
                            dr2["sıra"] = sayac + ". YOLUN ADI Hatası";
                            dr2["hata"] = hata.Message;
                            hataTablo.Rows.Add(dr2);
                        }
                        try
                        {
                            string neden = "";
                            neden = Duzenle(hnn.SelectNodes("td")[6].InnerText);

                            dr["KAPANMA NEDENİ"] = neden;

                        }
                        catch (Exception hata)
                        {
                            DataRow dr2 = hataTablo.NewRow();
                            dr2["sıra"] = sayac + ". KAPANMA NEDENİ Hatası";
                            dr2["hata"] = hata.Message;
                            hataTablo.Rows.Add(dr2);
                        }
                        try
                        {
                            string km = "";
                            km = Duzenle(hnn.SelectNodes("td")[7].InnerText);

                            dr["KAPANAN KESİM BAŞLANGIÇ (km)"] = km;

                        }
                        catch (Exception hata)
                        {
                            DataRow dr2 = hataTablo.NewRow();
                            dr2["sıra"] = sayac + ". KAPANAN KESİM BAŞLANGIÇ (km) Hatası";
                            dr2["hata"] = hata.Message;
                            hataTablo.Rows.Add(dr2);
                        }
                        try
                        {
                            string km2 = "";
                            km2 = Duzenle(hnn.SelectNodes("td")[8].InnerText);

                            dr["KAPANAN KESİM SON (km)"] = km2;

                        }
                        catch (Exception hata)
                        {
                            DataRow dr2 = hataTablo.NewRow();
                            dr2["sıra"] = sayac + ". KAPANAN KESİM SON (km) Hatası";
                            dr2["hata"] = hata.Message;
                            hataTablo.Rows.Add(dr2);
                        }
                        try
                        {
                            string gun = "";
                            gun = Duzenle(hnn.SelectNodes("td")[9].InnerText);

                            dr["KAPANMA TARİHİ GÜN"] = gun;

                        }
                        catch (Exception hata)
                        {
                            DataRow dr2 = hataTablo.NewRow();
                            dr2["sıra"] = sayac + ". KAPANMA TARİHİ GÜN Hatası";
                            dr2["hata"] = hata.Message;
                            hataTablo.Rows.Add(dr2);
                        }
                        try
                        {
                            string saat = "";
                            saat = Duzenle(hnn.SelectNodes("td")[10].InnerText);

                            dr["KAPANMA TARİHİ SAAT"] = saat;

                        }
                        catch (Exception hata)
                        {
                            DataRow dr2 = hataTablo.NewRow();
                            dr2["sıra"] = sayac + ". KAPANMA TARİHİ SAAT Hatası";
                            dr2["hata"] = hata.Message;
                            hataTablo.Rows.Add(dr2);
                        }
                        dtt.Rows.Add(dr);

                    }
                }
                catch (Exception)
                {
                }

            #endregion

            } Console.WriteLine("Yol cekıldı", dtt);

            #region Kaydetme


            foreach (DataRow item in dtt.Rows)
            {
                KapalıYolKaydet(item[0].ToString(), item[1].ToString(), item[2].ToString(), item[3].ToString(), item[4].ToString(), item[5].ToString(), item[6].ToString(), item[7].ToString(), item[8].ToString(), item[9].ToString(), item[10].ToString());
            }


            
            #endregion

            StringBuilderDoldur("Kapalı Yol", hataTablo);
        }
        private static bool KapalıYolKaydet(string SıraNo, string BölgeNo, string SubeNo, string KKNo, string KışProgramıDurumu, string YOLUNADI, string KAPANMANEDENİ, string KAPANANKESİMBAŞLANGIÇ, string KAPANANKESİMSON, string KAPANMATARİHİGÜN, string KAPANMATARİHİSAAT)
        {
            
            #region Bölüm Açıklaması
            /*
             Yapılanlar:
             netdata.com dan alınan apıkey ve AccPo kullanım dökümanı ıle netdata.com ' a KGM'den KapalıYol() fonksiyonuyla alınan veriler aktarılıyor.
             Buraya aktarma işlemi KapalıYol() fonksıyonundakı Kaydetme açıklamasından gelıyor. 
             netdata.com'a kaydetme işlemide 11 sütun olarak yapılmaktadır.
             İşlemimiz bittiginde eklenebildiyse true eklenemedıyse false donderecektir.
            */
            #endregion

            string Result = "";
            string Content = @"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soap:Body>
    <InsertRecord xmlns=""http://tempuri.org/"">
      <APIKey>458a41d8</APIKey>
      <InsertList>
        <AccPoKeyValuePair>
          <Key>dc_Sira_No</Key>
          <Value>[0]</Value>
        </AccPoKeyValuePair>
        <AccPoKeyValuePair>
          <Key>dc_Bolge_No</Key>
          <Value>[1]</Value>
        </AccPoKeyValuePair>
        <AccPoKeyValuePair>
          <Key>dc_Sube_No</Key>
          <Value>[2]</Value>
        </AccPoKeyValuePair>
        <AccPoKeyValuePair>
          <Key>dc_K_K__No</Key>
          <Value>[3]</Value>
        </AccPoKeyValuePair>
        <AccPoKeyValuePair>
          <Key>dc_Kis_Programi_Durumu</Key>
          <Value>[4]</Value>
        </AccPoKeyValuePair>
        <AccPoKeyValuePair>
          <Key>dc_Yolun_adi</Key>
          <Value>[5]</Value>
        </AccPoKeyValuePair>
        <AccPoKeyValuePair>
          <Key>dc_Kapanma_Nedeni</Key>
          <Value>[6]</Value>
        </AccPoKeyValuePair>
        <AccPoKeyValuePair>
          <Key>dc_KAPANAN_KESIM___BASLANGIC__km_</Key>
          <Value>[7]</Value>
        </AccPoKeyValuePair>
        <AccPoKeyValuePair>
          <Key>dc_KAPANAN_KESIM___SON__km_</Key>
          <Value>[8]</Value>
        </AccPoKeyValuePair>
        <AccPoKeyValuePair>
          <Key>dc_KAPANMA_TARIHI___GUN</Key>
          <Value>[9]</Value>
        </AccPoKeyValuePair>
        <AccPoKeyValuePair>
          <Key>dc_KAPANMA_TARIHI____SAAT</Key>
          <Value>[10]</Value>
        </AccPoKeyValuePair>
     </InsertList>
   </InsertRecord>
</soap:Body>
</soap:Envelope>";
            string url = "http://www.netdata.com/AccPo.asmx";
            string contentType = "text/xml; charset=utf-8";
            string method = "POST";
            string header = "SOAPAction: \"http://tempuri.org/InsertRecord\"";

            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = method;
            req.ContentType = contentType;
            req.Headers.Add(header);

            Stream strRequest = req.GetRequestStream();
            StreamWriter sw = new StreamWriter(strRequest);
            sw.Write(Content.Replace("[0]", SıraNo).Replace("[1]", BölgeNo).Replace("[2]", SubeNo).Replace("[3]", KKNo).Replace("[4]", KışProgramıDurumu).Replace("[5]", YOLUNADI).Replace("[6]", KAPANMANEDENİ).Replace("[7]", KAPANANKESİMBAŞLANGIÇ).Replace("[8]", KAPANANKESİMSON).Replace("[9]", KAPANMATARİHİGÜN).Replace("[10]", KAPANMATARİHİSAAT));
            sw.Close();

            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
            Stream strResponse = resp.GetResponseStream();
            StreamReader sr = new StreamReader(strResponse, System.Text.Encoding.ASCII);
            Result = sr.ReadToEnd();
            sr.Close();

            if (Result.Contains("Eklendi"))
            {
                return true;
            }
            else
            {
                return false;
            }

        }
        static void ÇalışmaYapılanYol()
        {
            #region Bölüm Açıklaması
            /* 
             Yapılanlar:
             sayac ve sayac1  hata tablosu ıcın tanımlanmıstır.
             WebClient ile kgm sayfasına baglanılıyor. Ve belirtilen Html Tag'leri içinde istenen bilgiler alındı
             Çalışma yapılan yol bilgisi öncelikle (1) ile belirtilen foreach döngüsünde belirtilen linkten bölge müdürlüklerinin linkeleri alınıyor.
             Bunlar dttt dataTable'ında tek sutuna yenı satırlar seklınde kaydediliyor.
             (2) ile belirtilen foreach dongusunde ise dttt deki tüm linklere sıra sıra gidip orda bulunan veriler çekiliyor
             Çalışma yapılan yol bilgisi 4 sütunda dta DataTable'ına yenı satırlar halinde yazıldı
             Sorun cıkması halinde hata tablosuna eklenerek mail atılacaktır.
             Thread programın donmaması ıcın gereklı.(Caslısırken mudahıl olabilmek için)
             region içinde ÇalışmaYolKaydet fonksıyonuna dta dataTable'ı ıcındekı verıler gonderılıyor.
             ÖNEMLİ ! Çalışma yaoılan yollar hergun netdata.com'daki veriler silinip tekrar kaydedilmesi şeklinde uygulanır.
             */
            #endregion

            int sayac = 0;
            int sayac1 = 0;

            DataTable dttt = new DataTable();
            DataTable dta = new DataTable();

            dttt.Columns.Add(new DataColumn("YolLinki", typeof(String)));
            dta.Columns.Add(new DataColumn("Bölge Müdürlüğü", typeof(String)));
            dta.Columns.Add(new DataColumn("Yol Kontrol Kesim No", typeof(String)));
            dta.Columns.Add(new DataColumn("Bilgi", typeof(String)));
            dta.Columns.Add(new DataColumn("Bildirim Tarihi", typeof(String)));

            WebClient wc3 = new WebClient();
            wc3.Encoding = Encoding.UTF8;
            string siteHtml = wc3.DownloadString("http://www.kgm.gov.tr/Sayfalar/KGM/SiteTr/YolDanisma/CalismaYapilanYollar.aspx");
            HtmlAgilityPack.HtmlDocument veri3 = new HtmlAgilityPack.HtmlDocument();
            veri3.LoadHtml(siteHtml);

            ClearNetdataTable("f5fcaacb");// netdata.com'daki verilerin silinmesi için ( apı key kendı apısıne aıt )

            #region Link Verisi Çekimi
            for (int i = 1; i < 30; i++)//Burdaki for Html taglerindekileri sıra sıra alması ıcın 30 tane olmasının sebebı ıse verılerın 30'u asmayacak sekılde olması.(18 Bölge mudurlugu var)
            {
                try
                {
                    //(1)
                    foreach (HtmlNode hnnn in veri3.DocumentNode.SelectNodes("//*[@id='ctl00_ctl00_PlaceHolderMainBase_PlaceHolderMain_ctl00__ControlWrapper_RichHtmlField']/table/tbody/tr[2]/td/div[" + i.ToString() + "]/table/tbody/tr"))
                    {
                        sayac++;
                        Thread.Sleep(100);

                        try
                        {
                            string link = "";
                            link = Duzenle(hnnn.SelectNodes("td[2]/a")[0].Attributes[0].Value);

                            DataRow dr = dttt.NewRow();
                            dr["YolLinki"] = link;
                            dttt.Rows.Add(dr);
                        }
                        catch (Exception hata)
                        {
                            DataRow dr = hataTablo.NewRow();
                            dr["sıra"] = sayac + ". link Hatası";
                            dr["hata"] = hata.Message;
                            hataTablo.Rows.Add(dr);
                        }    
                        break;
                   
                    }
                }
                catch (Exception)
                {

                }

            }
            #endregion

            #region Veri Çekimi
            //(2)
            foreach (DataRow item in dttt.Rows)
            {
               
                WebClient wc4 = new WebClient();
                wc4.Encoding = Encoding.UTF8;
                string siteHtml2 = wc4.DownloadString("http://www.kgm.gov.tr/Sayfalar/KGM/SiteTr/YolDanisma/CalismaYapilanYollarYeni.aspx?" + item[0].ToString());
                HtmlAgilityPack.HtmlDocument veri4 = new HtmlAgilityPack.HtmlDocument();
                veri4.LoadHtml(siteHtml2);

                for (int k = 3; k < 100; k++)
                {
                    DataRow dr = dta.NewRow();
                    //Bölge müdürlükleri için foreach
                    foreach (HtmlNode hne in veri4.DocumentNode.SelectNodes("//*[@id='ctl00_ctl00_ctl59_g_1995d091_332b_4644_8229_74d58414f456']/table/tr[1]"))
                    {  
                        sayac1++;
                        try
                        {
                            string bolge = "";
                            bolge = Duzenle(hne.SelectNodes("td")[0].InnerText);
                            dr["Bölge Müdürlüğü"] = bolge;
                            break;
                        }
                        catch (Exception hata)
                        {
                            DataRow dr2 = hataTablo.NewRow();
                            dr2["sıra"] = sayac + ". Bölge Müdürlüğü Hatası";
                            dr2["hata"] = hata.Message;
                            hataTablo.Rows.Add(dr2);
                        }
                    }
                    try
                    {
                        //Tablodaki diğer veriler için foreach
                        foreach (HtmlNode hne in veri4.DocumentNode.SelectNodes("//*[@id='ctl00_ctl00_ctl59_g_1995d091_332b_4644_8229_74d58414f456']/table/tr[" + k.ToString() + "]"))
                        {

                           try
                            {
                                string kesim = "";
                                kesim = Duzenle(hne.SelectNodes("td[1]")[0].InnerText);
                                dr["Yol Kontrol Kesim No"] = kesim;

                            }
                            catch (Exception hata)
                            {
                                DataRow dr2 = hataTablo.NewRow();
                                dr2["sıra"] = sayac + ". Yol Kontrol Kesim No Hatası";
                                dr2["hata"] = hata.Message;
                                hataTablo.Rows.Add(dr2);
                            }
                            try
                            {
                                string bilgi = "";
                                bilgi = Duzenle(hne.SelectNodes("td[2]")[0].InnerText);
                                dr["bilgi"] = bilgi;

                            }
                            catch (Exception hata)
                            {
                                DataRow dr2 = hataTablo.NewRow();
                                dr2["sıra"] = sayac + ". bilgi Hatası";
                                dr2["hata"] = hata.Message;
                                hataTablo.Rows.Add(dr2);
                            }
                            try
                            {
                                string tarih = "";
                                tarih = Duzenle(hne.SelectNodes("td[3]")[0].InnerText);
                                dr["Bildirim Tarihi"] = tarih;

                            }
                            catch (Exception hata)
                            {
                                DataRow dr2 = hataTablo.NewRow();
                                dr2["sıra"] = sayac + ". Bildirim Tarihi Hatası";
                                dr2["hata"] = hata.Message;
                                hataTablo.Rows.Add(dr2);
                            }

                        } dta.Rows.Add(dr);


                    }
                    catch (Exception)
                    {

                    }

                }
            } 
            #endregion
           
            Console.WriteLine("Çalısmalı Yol cekıldı", dta);

            #region Kaydetme
            foreach (DataRow item in dta.Rows)
            {
                ÇalışmaYolKaydet(item[0].ToString(), item[1].ToString(), item[2].ToString(), item[3].ToString());
            }

            
            #endregion 

            StringBuilderDoldur("Çalışma Yapılan Yol", hataTablo);
        }
        private static bool ÇalışmaYolKaydet(string BölgeMüdürlüğü,string YolKontrolKesimNo,string Bilgi,string BildirimTarihi)
        {
            #region Bölüm Açıklaması
            /*
             Yapılanlar:
             netdata.com dan alınan apıkey ve AccPo kullanım dökümanı ıle netdata.com ' a KGM'den ÇalışmaYapılanYol() fonksiyonuyla alınan veriler aktarılıyor.
             Buraya aktarma işlemi ÇalışmaYapılanYol() fonksıyonundakı Kaydetme açıklamasından gelıyor. 
             netdata.com'a kaydetme işlemide 4 sütun olarak yapılmaktadır.
             İşlemimiz bittiginde eklenebildiyse true eklenemedıyse false donderecektir.
            */
            #endregion

            string Result = "";
            string Content = @"<?xml version=""1.0"" encoding=""utf-8""?>
                <soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soap:Body>
    <InsertRecord xmlns=""http://tempuri.org/"">
      <APIKey>f5fcaacb</APIKey>
      <InsertList>
        <AccPoKeyValuePair>
          <Key>dc_Bolge_Mudurlugu</Key>
          <Value>[0]</Value>
        </AccPoKeyValuePair>
        <AccPoKeyValuePair>
          <Key>dc_Yol_Kontrol_Kesim_No</Key>
          <Value>[1]</Value>
        </AccPoKeyValuePair>
        <AccPoKeyValuePair>
          <Key>dc_Bilgi</Key>
          <Value>[2]</Value>
        </AccPoKeyValuePair>
        <AccPoKeyValuePair>
          <Key>dc_Bildirim_Tarihi</Key>
          <Value>[3]</Value>
        </AccPoKeyValuePair>
     </InsertList>
    </InsertRecord>
</soap:Body>
</soap:Envelope>";
            string url = "http://www.netdata.com/AccPo.asmx";
            string contentType = "text/xml; charset=utf-8";
            string method = "POST";
            string header = "SOAPAction: \"http://tempuri.org/InsertRecord\"";

            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = method;
            req.ContentType = contentType;
            req.Headers.Add(header);

            Stream strRequest = req.GetRequestStream();
            StreamWriter sw = new StreamWriter(strRequest);
            sw.Write(Content.Replace("[0]", BölgeMüdürlüğü).Replace("[1]", YolKontrolKesimNo).Replace("[2]", Bilgi).Replace("[3]", BildirimTarihi));
            sw.Close();

            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
            Stream strResponse = resp.GetResponseStream();
            StreamReader sr = new StreamReader(strResponse, System.Text.Encoding.ASCII);
            Result = sr.ReadToEnd();
            sr.Close();

            if (Result.Contains("Eklendi"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static string Duzenle(string text)
        {
            //Metinlerin yazım hatası oluşturmaması için yazılan fonksıyon
            string duzenlenmisText =
                text.Replace("\n", "")
                .Replace("\r", "")
                .Replace("\t", "")
                .Replace("  ", " ")
                .Replace("&nbsp;", " ")
                .Replace("&#287", "ğ")
                .Replace("&ouml", "ö")
                .Replace("&Ouml", "Ö")
                .Replace("&Ccedil", "Ç")
                .Replace("&ocire", "a")
                .Replace("&#146", "'")
                .Replace("&ndash", "-")
                .Replace("&Uuml", "Ü")
                .Replace("&#350", "Ş")
                .Replace("&icire", "a")
                .Replace("&rdquo", "\"")
                .Replace("&#147", "\"")
                .Replace("&#148", "\"")
                .Replace("&iacute", "i")
                .Replace("&rsquo", "'")
                .Replace("&ldquo", "\"")
                .Replace("&lsquo", "'")
                .Replace("&#304", "İ")
                .Replace("&ccedil", "ç")
                .Replace("&#351", "ş")
                .Replace("&uuml", "ü")
                .Replace("&#305", "ı")
                .Replace("&#252", "ü")
                .Replace("&#220", "Ü")
                .Replace("&#231", "ç")
                .Replace("&#246", "ö")
                .Replace("&#214", "Ö")
                .Replace("&#039", "'")
                .Replace("&quot", "\"")
                .Replace("&amp", "&")
                .Replace("&#8217", "'")
                .Replace("&#8220", "\"")
                .Replace("&#8221", "\"")
                .Replace("&#8211", "-")
                .Replace("&#240", "d")
                .Replace("&#8230", "...")
                .Replace("&#8216", "'")
                .Replace("&#8242", "'")
                .Replace("&#8442", "")
                .Replace("&#65533", ",")
                .Replace("&#160", " ")
                .Replace("&#345", "ř")
                .Replace("&#283", "ě")
                .Replace("&#269", "č")
                .Replace("&#367", "ů")
                .Replace("&#328", "á")
                .Replace("&#324", "ń")
                .Replace("&#322", "ł")
                .Replace("&#333", "ō")
                .Replace("&#321", "Ł")
                .Trim();
            return duzenlenmisText;
        }
        private static bool ClearNetdataTable(string ApiKey)
        {
            //Alınan apıkey ile ait oldugu apıdekı verılerı sılmek ıcın yazılan fonksıyon.
            //(Çalısma yapılan yollar ve kapalı yollar günlük değişmediği için ve arsıv seklınde tutulmayacagı ıcın bunu kullanıyor)
            string Result = "";
            string envelope =
             @"<soap:Envelope xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:soap='http://schemas.xmlsoap.org/soap/envelope/'>
                <soap:Body>
                <CustomDelete xmlns='http://tempuri.org/'>
                    <APIKey>[ApiKey]</APIKey>
                    <DeleteConditionsList>
                    <WhereList>
                        <Key>ID</Key>
                        <Operation>GREATER</Operation>
                        <Value>0</Value>
                    </WhereList>
                    </DeleteConditionsList>
                </CustomDelete>
                </soap:Body>
            </soap:Envelope>";

            string url = "http://www.netdata.com/AccPo.asmx";
            string contentType = "text/xml; charset=utf-8";
            string method = "POST";
            string header = "SOAPAction: \"http://tempuri.org/CustomDelete\"";

            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = method;
            req.ContentType = contentType;
            req.Headers.Add(header);

            Stream strRequest = req.GetRequestStream();
            StreamWriter sw = new StreamWriter(strRequest);
            sw.Write(envelope.Replace("[ApiKey]", ApiKey));
            sw.Close();
            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
            Stream strResponse = resp.GetResponseStream();
            StreamReader sr = new StreamReader(strResponse, System.Text.Encoding.ASCII);
            sr.Close();
            sr.Dispose();


            if (Result.Contains("Silindi"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private static void StringBuilderDoldur(string ListeAdi, DataTable hataTablo)
        {
            //Hata tablosunu maıl olarak göndermek ıcın , doldurdugumuz yer
            if (hataTablo.Rows.Count > 0)
            {
                sb.Append("<h2>" + ListeAdi + " Listesi Kayıt Bilgileri</h2>");
                sb.Append("<table style='border: 1px solid black; width:100%;'>");
                sb.Append("<tr style='border: 1px solid black;'>");
                sb.Append("<td style='border: 1px solid black; padding:5px;'><b>Sıra / Hata</b></td>");
                sb.Append("<td style='border: 1px solid black; padding:5px;'><b>Hata Mesajı</b></td>");
                sb.Append("</tr>");
                foreach (DataRow item in hataTablo.Rows)
                {
                    sb.Append("<tr style='border: 1px solid black;'>");
                    sb.Append("<td style='border: 1px solid black; padding:5px;'>" + item["sıra"].ToString() + "</td>");
                    sb.Append("<td style='border: 1px solid black; padding:5px;'>" + item["hata"].ToString() + "</td>");
                    sb.Append("</tr>");
                }
                sb.Append("</table>");
            }
            else
            {
                sb.Append("<h2>" + ListeAdi + " listesi başarıyla Netdata projesine aktarıldı.</h2>");
            }

            sb.Append("<hr/>");
            hataTablo.Clear();
        }
        private static void MailGonder()
        {
            //Maılın gönderıldıgı yer
            //Gönderen:imdbtoplist@gmail.com (şifre 28552855) , Gönderılen:admin@netdata.com ,Kullanılan Host:smtp.gmail.com (port:587)
            //gonderılen maıl programda hata vermemesı ıcın bu maıl adresı kullanılmıstır.
            SmtpClient sc = new SmtpClient();
            sc.Port = 587;
            sc.Host = "smtp.gmail.com";
            sc.EnableSsl = true;

            sc.Credentials = new NetworkCredential("imdbtoplist@gmail.com", "28552855");

            MailMessage mail = new MailMessage();

            mail.From = new MailAddress("imdbtoplist@gmail.com", "İbrahim Şahan");

            mail.To.Add("admin@netdata.com");

            mail.Subject = "Netdata - KGM";
            mail.IsBodyHtml = true;
            mail.Body = sb.ToString();

            sc.Send(mail);
            Console.WriteLine("Mail atıldı");
        }
    }
}
