using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack;
using ServiceStack.Text;
using System.IO;
using System.Windows.Forms;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using TicTacTec.TA.Library;
using System.Net.Http.Headers;
using System.Threading;
using System.Xml;
using System.Data.SqlClient;


namespace oandatest
{

    class Program
    {
        public struct info
        {
            public string[] open;
            public string[] high;
            public string[] low;
            public string[] close;
            public string[] time;
        };

        public class oandainfo
        {
            public string bid;
            public string ask;
            public string cbid;
            public string cas;
            public string status;
            public string tradeable;
            public string time;
            public string error;
        }

        public class posinfo
        {
            public string symbol;
            public string units;
            public string price;
            public string side;
            public string error;
        }

        public class tradeinfo
        {
            public string symbol = "";
            public string price = "";
            public string units = "";
            public string time = "";
            public string error = "";
        }


        static public info datastore;
        public static double swinghigh;
        public static double swinglow;
        public static double longtarget;
        public static double shorttarget;
        public static double shortsl;
        public static double longsl;
        public static string lastaction = null;



        static void Main(string[] args)
        {

            Macdstrat(args);
          
        }

        static string[] Getaccounttoken()
        {
            string[] accounttoken = new string[16];

            XmlDocument xml = new XmlDocument();
            String exepath = AppDomain.CurrentDomain.BaseDirectory;
            xml.Load(exepath + @"config.xml");

            XmlNode tokenst = xml.SelectSingleNode("/configuration/token");
            string token = tokenst.InnerText;

            XmlNode account = xml.SelectSingleNode("/configuration/account");
            string accountnum = account.InnerText;

            XmlNode stl = xml.SelectSingleNode("/configuration/stoptype");
            string stoptype = stl.InnerText;

            XmlNode api = xml.SelectSingleNode("/configuration/apiurl");
            string apiurl = api.InnerText;

            XmlNode tf = xml.SelectSingleNode("/configuration/timeframe");
            string timeframe = tf.InnerText;

            XmlNode sym = xml.SelectSingleNode("/configuration/symbol");
            string symbol = sym.InnerText;

            XmlNode qu = xml.SelectSingleNode("/configuration/quantity");
            string quantity = qu.InnerText;

            XmlNode tg = xml.SelectSingleNode("/configuration/target");
            string target = tg.InnerText;

            XmlNode sl = xml.SelectSingleNode("/configuration/stoploss");
            string stoploss = sl.InnerText;

            XmlNode log = xml.SelectSingleNode("/configuration/sqllog");
            string logging = log.InnerText;

            XmlNode srv = xml.SelectSingleNode("/configuration/server");
            string server = srv.InnerText;

            XmlNode db = xml.SelectSingleNode("/configuration/db");
            string dba = db.InnerText;

            XmlNode user = xml.SelectSingleNode("/configuration/user");
            string usr = user.InnerText;

            XmlNode pasw = xml.SelectSingleNode("/configuration/passw");
            string pw = pasw.InnerText;

            XmlNode to = xml.SelectSingleNode("/configuration/timeout");
            string toval = to.InnerText;

            XmlNode table = xml.SelectSingleNode("/configuration/table");
            string tb = table.InnerText;

            accounttoken[0] = token;
            accounttoken[1] = accountnum;
            accounttoken[2] = stoptype;
            accounttoken[3] = apiurl;
            accounttoken[4] = timeframe;
            accounttoken[5] = symbol;
            accounttoken[6] = quantity;
            accounttoken[7] = target;
            accounttoken[8] = stoploss;
            accounttoken[9] = logging;
            accounttoken[10] = server;
            accounttoken[11] = dba;
            accounttoken[12] = usr;
            accounttoken[13] = pw;
            accounttoken[14] = toval;
            accounttoken[15] = tb;

            return accounttoken;
        }

        static void Macdstrat(string[] args)
        {
            int trstat = 1;
            //double trailstoplossp = -1;
            double cls = -1;
            string[] account = Getaccounttoken();

            int lem = -1;

          
            
            SqlConnection myConnection = new SqlConnection("server=" + account[10] +";" +
                                          "Initial Catalog=" + account[11] + ";" +
                                          "User ID=" + account[12] + ";" +
                                          "Password=" + account[13] + ";" +
                                          "connection timeout=" + account[14]);
           

            try
            {
                myConnection.Open();
            }

            catch (SqlException ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }



            if (lastaction == null)
            {
                Task<posinfo> postask = Gettrades();
                posinfo numpos = postask.Result;


                while (numpos.error == "true")
                {
                    Task<posinfo> postaskrerun = Gettrades();
                    numpos = postaskrerun.Result;
                }

                if (numpos.units != "0")
                {
                    lastaction = "open";
                }

                if (numpos.units == "0")
                {
                    lastaction = "close";
                }


            }

            while (trstat == 1)
            {

                //check if market is open
                Task<oandainfo> pricetask = Getprice();
                oandainfo priceinfo = pricetask.Result;

                while (priceinfo.error == "true")
                {
                    Task<oandainfo> pricetaskrerun = Getprice();
                    priceinfo = pricetaskrerun.Result;
                }


                if (priceinfo.tradeable.ToLower().Contains("true"))
                {

                    Task<posinfo> postask = Gettrades();
                    posinfo numpos = postask.Result;


                    while (numpos.error == "true")
                    {
                        Task<posinfo> postaskrerun = Gettrades();
                        numpos = postaskrerun.Result;
                    }


                    //if currently holding a position                   
                    if ((numpos.units != "0") && (lastaction == "open"))
                    {
                        //get price data
                        Task<oandainfo> pricetask2 = Getprice();
                        oandainfo priceinfo2 = pricetask2.Result;

                        while (priceinfo2.error == "true")
                        {
                            Task<oandainfo> pricetask2rerun = Getprice();
                            priceinfo2 = pricetask2rerun.Result;
                        }

                        //get candlestick data 
                        string[] testarr = new string[2];
                        testarr[0] = "";
                        testarr[1] = "10";
                        Task<info> datatask = getdataasync(testarr, 0);
                        info data = datatask.Result;

                        while (data.open.Length <= 2)
                        {
                            Task<info> datataskrerun = getdataasync(testarr, 0);
                            data = datataskrerun.Result;
                        }


                        //if in trade and no swinghigh/swinglow longtarget/shorttarget set
                        if ((swinghigh == 0) && (swinglow == 0))
                        {
                            //numpos.price priceinfo2.bid priceinfo2.ask data.low[0] data.high[0]
                            if (numpos.side == "long")
                            {
                                swinglow = Convert.ToDouble(numpos.price) - 0.0005;
                                longtarget = Convert.ToDouble(numpos.price) + 0.0005;

                            }

                            if (numpos.side == "short")
                            {
                                swinghigh = Convert.ToDouble(numpos.price) + 0.0005;
                                shorttarget = Convert.ToDouble(numpos.price) - 0.0005;
                            }

                        }

                       
                        if (account[2] == "fixed")
                        {

                            if (numpos.side == "long")
                            {

                                double pal = Convert.ToDouble(numpos.price) - Convert.ToDouble(priceinfo2.bid);

                                if (Convert.ToDouble(data.low[0]) <= swinglow)
                                {
                                    //add exception handling
                                    Task<tradeinfo> stoploss = oandabuysell((Convert.ToInt32(numpos.units) * -1), "EUR_USD", "FOK", "MARKET");
                                    tradeinfo slr = stoploss.Result;

                                    //slr.price slr.units
                                    if (slr.error == "false")
                                    {
                                        try
                                        {
                                           
                                            SqlCommand myCommand = new SqlCommand("INSERT INTO " + account[15] + " (TDESC, PRICE, UNITS, TTIME, SWINGV, PL, TGT) " +
                                            "Values ('Close long stop loss'," + "'" + slr.price + "'" + "," + "'" + numpos.units + "'" + "," + "'" + slr.time + "'" + "," + "'" + "" + "'" + "," + "'" + Math.Round((Convert.ToDouble(slr.price) - Convert.ToDouble(numpos.price)), 5) + "'" + "," + "'" + "" + "'" + ")", myConnection);
                                            myCommand.ExecuteNonQuery();
                                        }

                                        catch (SqlException ex)
                                        {
                                            Console.WriteLine("Error: " + ex.Message);
                                        }

                                        Console.WriteLine("Close long stop loss " + slr.price + " units:" + numpos.units + " P&L: " + Math.Round((Convert.ToDouble(slr.price) - Convert.ToDouble(numpos.price)), 5) + " time:" + slr.time);
                                        numpos.units = "0";
                                        lastaction = "close";
                                    }

                                }

                                else if (Convert.ToDouble(data.high[0]) >= longtarget)
                                {
                                    //add exception handling
                                    Task<tradeinfo> target = oandabuysell((Convert.ToInt32(numpos.units) * -1), "EUR_USD", "FOK", "MARKET");
                                    tradeinfo slr = target.Result;

                                    //slr.price slr.units
                                    if (slr.error == "false")
                                    {
                                        try
                                        {
                                            SqlCommand myCommand = new SqlCommand("INSERT INTO " + account[15] + " (TDESC, PRICE, UNITS, TTIME, SWINGV, PL, TGT) " +
                                            "Values ('Close long target'," + "'" + slr.price + "'" + "," + "'" + numpos.units + "'" + "," + "'" + slr.time + "'" + "," + "'" + "" + "'" + "," + "'" + Math.Round((Convert.ToDouble(slr.price) - Convert.ToDouble(numpos.price)), 5) + "'" + "," + "'" + "" + "'" + ")", myConnection);
                                            myCommand.ExecuteNonQuery();
                                        }

                                        catch (SqlException ex)
                                        {
                                            Console.WriteLine("Error: " + ex.Message);
                                        }


                                        Console.WriteLine("Close long target " + slr.price + " units:" + numpos.units + " P&L: " + Math.Round((Convert.ToDouble(slr.price) - Convert.ToDouble(numpos.price)), 5) + " time:" + slr.time);
                                        numpos.units = "0";
                                        lastaction = "close";
                                    }

                                }

                            }

                            if (numpos.side == "short")
                            {
                                double pal = Convert.ToDouble(numpos.price) - Convert.ToDouble(priceinfo2.ask);

                                if (Convert.ToDouble(data.low[0]) <= shorttarget)
                                {
                                    //add exception handling
                                    Task<tradeinfo> target = oandabuysell((Convert.ToInt32(numpos.units) * -1), "EUR_USD", "FOK", "MARKET");
                                    tradeinfo slr = target.Result;

                                    //slr.price slr.units
                                    if (slr.error == "false")
                                    {
                                        try
                                        {

                                            SqlCommand myCommand = new SqlCommand("INSERT INTO " + account[15] + " (TDESC, PRICE, UNITS, TTIME, SWINGV, PL, TGT) " +
                                             "Values ('Close short target'," + "'" + slr.price + "'" + "," + "'" + numpos.units + "'" + "," + "'" + slr.time + "'" + "," + "'" + "" + "'" + "," + "'" + Math.Round((Convert.ToDouble(numpos.price) - Convert.ToDouble(slr.price)), 5) + "'" + "," + "'" + "" + "'" + ")", myConnection);
                                            myCommand.ExecuteNonQuery();
                                        }

                                        catch (SqlException ex)
                                        {
                                            Console.WriteLine("Error: " + ex.Message);
                                        }

                                        Console.WriteLine("Close short target " + slr.price + " units:" + numpos.units + " P&L: " + Math.Round((Convert.ToDouble(numpos.price) - Convert.ToDouble(slr.price)), 5) + " time:" + slr.time);
                                        numpos.units = "0";
                                        lastaction = "close";
                                    }
                                }

                                else if (Convert.ToDouble(data.high[0]) >= swinghigh)
                                {
                                    //add exception handling
                                    Task<tradeinfo> stoploss = oandabuysell((Convert.ToInt32(numpos.units) * -1), "EUR_USD", "FOK", "MARKET");
                                    tradeinfo slr = stoploss.Result;

                                    //slr.price slr.units
                                    if (slr.error == "false")
                                    {
                                        try
                                        {
                                                                                    
                                            SqlCommand myCommand = new SqlCommand("INSERT INTO " + account[15] + " (TDESC, PRICE, UNITS, TTIME, SWINGV, PL, TGT) " +
                                             "Values ('Close short stop loss'," + "'" + slr.price + "'" + "," + "'" + numpos.units + "'" + "," + "'" + slr.time + "'" + "," + "'" + "" + "'" + "," + "'" + Math.Round((Convert.ToDouble(numpos.price) - Convert.ToDouble(slr.price)), 5) + "'" + "," + "'" + "" + "'" + ")", myConnection);
                                            myCommand.ExecuteNonQuery();                               
                                        }

                                        catch (SqlException ex)
                                        {
                                            Console.WriteLine("Error: " + ex.Message);
                                        }

                                        Console.WriteLine("Close short stop loss " + slr.price + " units:" + numpos.units + " P&L: " + Math.Round((Convert.ToDouble(numpos.price) - Convert.ToDouble(slr.price)), 5) + " time:" + slr.time);
                                        numpos.units = "0";
                                        lastaction = "close";
                                    }
                                }

                            }
                        }

                    }

                    //if no positions
                    if ((numpos.units == "0") && (lastaction == "close"))
                    {
                        //get price and get time data
                        Task<oandainfo> pricetasktime = Getprice();
                        oandainfo priceinfotime = pricetasktime.Result;

                        while (priceinfotime.error == "true")
                        {
                            Task<oandainfo> pricetasktimererun = Getprice();
                            priceinfotime = pricetasktimererun.Result;
                        }

                        //Console.WriteLine(priceinfotime.time);
                        int ind1 = priceinfotime.time.IndexOf(":");
                        int ind2 = priceinfotime.time.LastIndexOf(":");
                        int ind3 = priceinfotime.time.IndexOf(".");
                        int ind4 = priceinfotime.time.IndexOf("T");

                        string wdate = priceinfotime.time.Substring(0, ind4);
                        string min = priceinfotime.time.Substring(ind1 + 1, ind2 - ind1 - 1);
                        string sec = priceinfotime.time.Substring(ind2 + 1, ind3 - ind2 - 1);
                        string hr = priceinfotime.time.Substring(ind4 + 1, ind1 - ind4 - 1);

                        string[] dparts = wdate.Split('-');
                        string yr = dparts[0];
                        string mth = dparts[1];
                        string year = dparts[2];

                        if (sec == "00")
                        {
                            sec = "0";
                        }

                        else
                        {
                            sec = sec.TrimStart('0');
                        }

                        if (min == "00")
                        {
                            min = "0";
                        }

                        else
                        {
                            min = min.TrimStart('0');
                        }

                        DateTime dt = DateTime.Now;
                        int ms = dt.Millisecond;
                        int s = dt.Second;
                        int m = dt.Minute;

                        //Console.WriteLine(sec);
                        string error = "false";

                        if (((Convert.ToInt32(sec) > 6)) /*&& (Convert.ToInt32(min) % 5 == 0)*/ && (Convert.ToInt32(min) != lem))
                        //if (((s > 8)) /*&& (m%5 == 0)*/ && (m != lem))
                        {

                            string[] testarr = new string[2];
                            testarr[0] = "";
                            testarr[1] = "4000";

                            Task<info> datatask = getdataasync(testarr, 1);
                            info data = datatask.Result;

                            while (data.open.Length <= 2)
                            {
                                Task<info> datataskrerun = getdataasync(testarr, 1);
                                data = datataskrerun.Result;
                            }


                            //close data points
                            float[] test = new float[data.close.Length];
                            //open data points
                            float[] testo = new float[data.open.Length];
                            //high data ponts
                            float[] testh = new float[data.high.Length];
                            //low data ponts
                            float[] testl = new float[data.low.Length];
                            //date data points
                            string[] testd = new string[data.time.Length];

                            int xtmacd, ytmacd;
                            int xtsma, ytsma;
                            int xtrsi, ytrsi;

                            if (data.open.Length > 2)
                            {

                                for (int x = 0; x < data.open.Length; x++)
                                {
                                    test[x] = float.Parse(data.close[(data.close.Length - 1) - x]);
                                    testo[x] = float.Parse(data.open[(data.open.Length - 1) - x]);
                                    testh[x] = float.Parse(data.high[(data.high.Length - 1) - x]);
                                    testl[x] = float.Parse(data.low[(data.low.Length - 1) - x]);
                                    testd[x] = data.time[(data.time.Length - 1) - x];
                                }


                            }

                            else
                            {
                                Console.WriteLine("error in downloading");
                            }


                            if (data.open.Length > 2)
                            {
                                int macdlb = Core.MacdLookback(12, 26, 9);
                                int smalb = Core.SmaLookback(10);
                                int rsilb = Core.RsiLookback(14);


                                double[] test1macd = new double[data.open.Length - macdlb];
                                double[] test2macdsig = new double[data.open.Length - macdlb];
                                double[] test3macdhist = new double[data.open.Length - macdlb];

                                double[] test3sma = new double[data.open.Length - smalb];
                                double[] test3rsi = new double[data.open.Length - rsilb];

                                //date data points
                                string[] testdmacd = new string[data.open.Length];
                                string[] testdsma = new string[data.open.Length];
                                string[] testdrsi = new string[data.open.Length];

                                //set date data points
                                for (int x = 0; x < test1macd.Length; x++)
                                {
                                    testdmacd[x] = testd[x + macdlb];
                                }

                                for (int x = 0; x < test3sma.Length; x++)
                                {
                                    testdsma[x] = testd[x + smalb];
                                }

                                for (int x = 0; x < test3rsi.Length; x++)
                                {
                                    testdrsi[x] = testd[x + rsilb];
                                }


                                Core.RetCode macdret = Core.Macd(0, test.Length - 1, test, 12, 26, 9, out xtmacd, out ytmacd, test1macd, test2macdsig, test3macdhist);
                                Core.RetCode smaret = Core.Sma(0, test.Length - 1, test, 10, out xtsma, out ytsma, test3sma);
                                Core.RetCode rsiret = Core.Rsi(0, test.Length - 1, test, 14, out xtrsi, out ytrsi, test3rsi);

                                //Console.WriteLine(test1macd[ytmacd - 1] + ";" + test2macdsig[ytmacd - 1] + ";" + test1macd[ytmacd - 2] + ";" + test2macdsig[ytmacd - 2] + ";" + dt.ToString("HH:mm:ss.fffff") + ";" + test[test.Length - 1]);
                                Console.WriteLine(test1macd[ytmacd - 1] + ";" + test2macdsig[ytmacd - 1] + ";" + test1macd[ytmacd - 2] + ";" + test2macdsig[ytmacd - 2] + ";" + hr + ":" + min + ":" + sec + ";" + test[test.Length - 1]);

                                if ((test1macd[ytmacd - 1] > test2macdsig[ytmacd - 1]) && ((test1macd[ytmacd - 2] < test2macdsig[ytmacd - 2])))
                                {

                                    swinglow = testl[testl.Length - 1];
                                    for (int x = 2; x < 11; x++)
                                    {
                                        if (testl[testl.Length - x] < swinglow)
                                        {
                                            swinglow = testl[testl.Length - x];
                                        }

                                    }

                                    //get ask price and compare to swinglow only enter if ask price above swinglow
                                    //get price data
                                    Task<oandainfo> pricetask2 = Getprice();
                                    oandainfo priceinfo2 = pricetask2.Result;

                                    while (priceinfo2.error == "true")
                                    {
                                        Task<oandainfo> pricetask2rerun = Getprice();
                                        priceinfo2 = pricetask2rerun.Result;
                                    }

                                    if (Math.Round(Convert.ToDouble(priceinfo2.ask), 5) > Math.Round(swinglow, 5))
                                    {
                                        //add exception handling
                                        Task<tradeinfo> buy = oandabuysell(10000, "EUR_USD", "FOK", "MARKET");
                                        tradeinfo slr = buy.Result;

                                     
                                        //slr.price slr.units
                                        if (slr.error == "false")
                                        {
                                            double diff = Convert.ToDouble(slr.price) - swinglow;
                                            longtarget = Math.Round(Convert.ToDouble(slr.price) + diff, 5);

                                            try
                                            {
                                                                                              
                                                SqlCommand myCommand = new SqlCommand("INSERT INTO " + account[15] + " (TDESC, PRICE, UNITS, TTIME, SWINGV, PL, TGT) " +
                                                "Values ('Open long'," + "'" + slr.price + "'" + "," + "'" + 10000 + "'" + "," + "'" + slr.time + "'" + "," + "'" + Math.Round(swinglow, 5) + "'" + "," + "'" + "" + "'" + "," + "'" + longtarget + "'" + ")", myConnection);
                                                myCommand.ExecuteNonQuery();
                                             
                                            }

                                            catch (SqlException ex)
                                            {
                                                Console.WriteLine("Error: " + ex.Message);
                                            }




                                            Console.WriteLine("Open long " + slr.price + ";" + Math.Round(swinglow, 5) + ";" + longtarget + ";" + dt.ToString("HH:mm:ss.fffff") + ";" + slr.time);
                                            lastaction = "open";
                                            error = "false";
                                        }

                                        else
                                        {
                                            error = "true";
                                        }
                                    }
                                }

                                else if ((test1macd[ytmacd - 1] < test2macdsig[ytmacd - 1]) && ((test1macd[ytmacd - 2] > test2macdsig[ytmacd - 2])))
                                {

                                    swinghigh = testh[testh.Length - 1];
                                    for (int x = 2; x < 11; x++)
                                    {
                                        if (testh[testh.Length - x] > swinghigh)
                                        {
                                            swinghigh = testh[testh.Length - x];
                                        }

                                    }

                                    //get bid price and compare to swinghigh only enter if bid price below swinghigh
                                    //get price data
                                    Task<oandainfo> pricetask2 = Getprice();
                                    oandainfo priceinfo2 = pricetask2.Result;

                                    while (priceinfo2.error == "true")
                                    {
                                        Task<oandainfo> pricetask2rerun = Getprice();
                                        priceinfo2 = pricetask2rerun.Result;
                                    }



                                    if (Math.Round(Convert.ToDouble(priceinfo2.bid), 5) < Math.Round(swinghigh, 5))
                                    {

                                        //add exception handling
                                        Task<tradeinfo> sell = oandabuysell(-10000, "EUR_USD", "FOK", "MARKET");
                                        tradeinfo slr = sell.Result;

                                      
                                        //slr.price slr.units
                                        if (slr.error == "false")
                                        {

                                            double diff = swinghigh - Convert.ToDouble(slr.price);
                                            shorttarget = Math.Round(Convert.ToDouble(slr.price) - diff, 5);

                                            try
                                            {
                                              
                                                SqlCommand myCommand = new SqlCommand("INSERT INTO " + account[15] + " (TDESC, PRICE, UNITS, TTIME, SWINGV, PL, TGT) " +
                                                "Values ('Open short'," + "'" + slr.price + "'" + "," + "'" + 10000 + "'" + "," + "'" + slr.time + "'" + "," + "'" + Math.Round(swinghigh, 5) + "'" + "," + "'" + "" + "'" + "," + "'" + shorttarget + "'" + ")", myConnection);
                                                myCommand.ExecuteNonQuery();
                                   
                                            }

                                            catch (SqlException ex)
                                            {
                                                Console.WriteLine("Error: " + ex.Message);
                                            }


                                            Console.WriteLine("Open short " + slr.price + ";" + Math.Round(swinghigh, 5) + ";" + shorttarget + ";" + dt.ToString("HH:mm:ss.fffff") + ";" + slr.time);
                                            lastaction = "open";
                                            error = "false";
                                        }

                                        else
                                        {
                                            error = "true";
                                        }

                                    }
                                }

                                else
                                {
                                    error = "false";
                                }

                            }

                            if (error == "false")
                            {
                                lem = Convert.ToInt32(min);
                            }

                            //lem = m;
                            //Thread.Sleep(60000 - (1000 * (s - 3)));

                        }

                    }

                }

                else
                {

                }
            }

        }

        
        static async Task<oandainfo> Getprice()
        {
            oandainfo tradeinfo = new oandainfo();

            string[] account = Getaccounttoken();

            try
            {
                using (var httpClient = new HttpClient())
                {

                    using (var request = new HttpRequestMessage(new HttpMethod("GET"), $"{account[3]}/accounts/{account[1]}/pricing?instruments=EUR_USD"))
                    {
                        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {account[0]}");

                        var response = await httpClient.SendAsync(request);
                        string responseBody = await response.Content.ReadAsStringAsync();
                        dynamic stuff4 = JObject.Parse(responseBody);
                        var prices = stuff4.prices[0];
                        var bid = stuff4.prices[0].bids[0].price;
                        var ask = stuff4.prices[0].asks[0].price;
                        var cbid = stuff4.prices[0].closeoutBid;
                        var cas = stuff4.prices[0].closeoutAsk;
                        var status = stuff4.prices[0].status;
                        var tradeable = stuff4.prices[0].tradeable;
                        var time = stuff4.prices[0].time;


                        tradeinfo.bid = bid;
                        tradeinfo.ask = ask;
                        tradeinfo.cbid = cbid;
                        tradeinfo.cas = cas;
                        tradeinfo.status = status;
                        tradeinfo.tradeable = tradeable;
                        tradeinfo.time = time;
                        tradeinfo.error = "false";
                    }

                }

                return tradeinfo;
            }

            catch(Exception e)
            {
                Console.WriteLine("{0} Exception caught.", e);
                tradeinfo.error = "true";
                return tradeinfo;
            }


        }


        static async Task<posinfo> Gettrades()
        {
            posinfo info = new posinfo();

            string[] account = Getaccounttoken();

            try
            {
                using (var httpClient = new HttpClient())
                {

                    using (var request = new HttpRequestMessage(new HttpMethod("GET"), $"{account[3]}/accounts/{account[1]}/openPositions"))
                    {
                        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {account[0]}");

                        var response = await httpClient.SendAsync(request);
                        string responseBody = await response.Content.ReadAsStringAsync();
                        dynamic stuff4 = JObject.Parse(responseBody);
                        //Console.WriteLine(responseBody);

                        //no open positions
                        if (responseBody.Contains("\"positions\":[],\""))
                        {
                            //no open positions
                            //trade = 0;
                            info.symbol = "";
                            info.units = "0";
                            info.price = "0";
                            info.side = "";
                            info.error = "false";
                            //Console.WriteLine("no positions");
                        }

                        else
                        {
                            var inst = stuff4.positions[0].instrument;
                            var lposi = stuff4.positions[0].@long.units;
                            var lprice = stuff4.positions[0].@long.averagePrice;
                            var sposi = stuff4.positions[0].@short.units;
                            var sprice = stuff4.positions[0].@short.averagePrice;
                            info.error = "false";

                            if (lposi != "0")
                            {
                                info.symbol = inst;
                                info.units = lposi;
                                info.price = lprice;
                                info.side = "long";
                            }

                            else
                            {
                                info.symbol = inst;
                                info.units = sposi;
                                info.price = sprice;
                                info.side = "short";
                            }

                        }
                    }


                }

                return info;
            }

            catch (Exception e)
            {
                Console.WriteLine("{0} Exception caught.", e);
                info.error = "true";
                return info;
            }

        }

        static async Task<int> DownloadData(int datap, int complete)
        {
            string[] account = Getaccounttoken();

            try
            {
                using (var httpClient = new HttpClient())
                {
                    using (var request = new HttpRequestMessage(new HttpMethod("GET"), $"{account[3]}/instruments/EUR_USD/candles?count=4001&price=M&granularity=M1"))  //5000 max
                    {
                        datastore.open = new string[datap];
                        datastore.high = new string[datap];
                        datastore.low = new string[datap];
                        datastore.close = new string[datap];
                        datastore.time = new string[datap];

                        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {account[0]}");

                        var response = await httpClient.SendAsync(request);
                        string responseBody = await response.Content.ReadAsStringAsync();
                        dynamic stuff4 = JObject.Parse(responseBody);

                        string c = stuff4.candles[stuff4.candles.Count - 1].complete;
                        string t = stuff4.candles[stuff4.candles.Count - 1].time;
                        int ind1 = t.IndexOf(":");
                        int ind2 = t.LastIndexOf(":");
                        string min = t.Substring(ind1 + 1, ind2 - ind1 - 1);

                        if (min == "00")
                        {
                            min = "0";
                        }

                        else
                        {
                            min = min.TrimStart('0');
                        }


                        //store 4000 values, last candle element is the most recent price
                        for (int x = 1; x < datap + 1; x++)
                        {
                            //most recent data is in spot 0
                            //use last 4000 items including most recent candle
                            //set override to allow return last 4000 items including most recent candle
                            if ((c.ToLower().Equals("true")) || (complete == 0))
                            {
                              
                                datastore.open[x - 1] = stuff4.candles[stuff4.candles.Count - x].mid.o;
                                datastore.high[x - 1] = stuff4.candles[stuff4.candles.Count - x].mid.h;
                                datastore.low[x - 1] = stuff4.candles[stuff4.candles.Count - x].mid.l;
                                datastore.close[x - 1] = stuff4.candles[stuff4.candles.Count - x].mid.c;
                                datastore.time[x - 1] = stuff4.candles[stuff4.candles.Count - x].time;

                            }

                            //use first 4000 values, throw away most recent price if not complete(complete = false)
                            else
                            {
                        
                                datastore.open[x - 1] = stuff4.candles[stuff4.candles.Count - x - 1].mid.o;
                                datastore.high[x - 1] = stuff4.candles[stuff4.candles.Count - x - 1].mid.h;
                                datastore.low[x - 1] = stuff4.candles[stuff4.candles.Count - x - 1].mid.l;
                                datastore.close[x - 1] = stuff4.candles[stuff4.candles.Count - x - 1].mid.c;
                                datastore.time[x - 1] = stuff4.candles[stuff4.candles.Count - x - 1].time;

                            }

                        }
                    }

                }
                return 1;
            }

            catch(Exception e)
            {
                Console.WriteLine("{0} Exception caught.", e);
                return -1;
            }
        }

        public static async Task<int> canceltradesoanda(string num)
        {
            string[] account = Getaccounttoken();

            try
            {
                using (var httpClient = new HttpClient())
                {
                    using (var request = new HttpRequestMessage(new HttpMethod("PUT"), $"{account[3]}/accounts/{account[1]}/orders/" + num + "/cancel"))
                    {
                        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {account[0]}");

                        var response = await httpClient.SendAsync(request);
                        string responseBody = await response.Content.ReadAsStringAsync();
                        dynamic stuff4 = JObject.Parse(responseBody);
                        Console.WriteLine(responseBody);
                    }
                }

                return 1;
            }

            catch (Exception e)
            {
                Console.WriteLine("{0} Exception caught.", e);
                return -1;
            }
        }

        public static async Task<int> gettradesoanda()
        {
            string[] account = Getaccounttoken();

            try
            {
                using (var httpClient = new HttpClient())
                {
                    using (var request = new HttpRequestMessage(new HttpMethod("GET"), $"{account[3]}/accounts/{account[1]}/orders?instrument=EUR_USD"))
                    {
                        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {account[0]}");

                        var response = await httpClient.SendAsync(request);
                        string responseBody = await response.Content.ReadAsStringAsync();
                        dynamic stuff4 = JObject.Parse(responseBody);
                        Console.WriteLine(responseBody);

                    }
                }

                return 1;
            }

            catch (Exception e)
            {
                Console.WriteLine("{0} Exception caught.", e);
                return -1;
            }

        }


        public static async Task<tradeinfo> oandabuysell(int units, string inst, string tif, string ordtype)
        {
            string[] account = Getaccounttoken();
            tradeinfo trade = new tradeinfo();

            string body = $"{{ \"order\": {{\"units\": \"{units}\",\"instrument\": \"{inst}\",\"timeInForce\": \"{tif}\",\"type\": \"{ordtype}\",\"positionFill\": \"DEFAULT\"}}}}";

            try
            {
                using (var httpClient = new HttpClient())
                {
                    using (var request = new HttpRequestMessage(new HttpMethod("POST"), $"{account[3]}/accounts/{account[1]}/orders"))
                    {
                        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {account[0]}");

                        request.Content = new StringContent(body);
                        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

                        var response = await httpClient.SendAsync(request);
                        string responseBody = await response.Content.ReadAsStringAsync();
                        dynamic stuff4 = JObject.Parse(responseBody);
                        var order = stuff4.orderFillTransaction;
                        //Console.WriteLine(order);

                        trade.symbol = stuff4.orderFillTransaction.instrument;
                        trade.units = stuff4.orderFillTransaction.units;
                        trade.price = stuff4.orderFillTransaction.price;
                        trade.time = stuff4.orderFillTransaction.time;
                        trade.error = "false";
                    }
                }

                return trade;
            }

            catch (Exception e)
            {
                Console.WriteLine("{0} Exception caught.", e);
                trade.error = "true";
                return trade;
            }
        }

        public static async Task<info> getdataasync(string[] args, int m)
        {
          
            info data = new info();

            data.open = new string[2];
            data.high = new string[2];
            data.low = new string[2];
            data.close = new string[2];
            data.time = new string[2];

            try
            {

                Task<int> task = DownloadData(Convert.ToInt32(args[1]), m);
                int x = await task;

                if (x == 1)
                {
                    return datastore;
                }

                else
                {
                    return data;
                }
            }

            catch (Exception e)
            {
                Console.WriteLine("{0} Exception caught.", e);
                return data;

            }
        }

    }
}
