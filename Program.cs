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
        }

        public class posinfo
        {
            public string symbol;
            public string units;
            public string price;
            public string side;
        }

        public class tradeinfo
        {
            public string symbol;
            public string price;
            public string units;
        }


        static public info datastore;
      
        static void Main(string[] args)
        {

            Macdstrat(args);       
        }

        static string[] Getaccounttoken()
        {
            string[] accounttoken = new string[9];

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

            accounttoken[0] = token;
            accounttoken[1] = accountnum;
            accounttoken[2] = stoptype;
            accounttoken[3] = apiurl;
            accounttoken[4] = timeframe;
            accounttoken[5] = symbol;
            accounttoken[6] = quantity;
            accounttoken[7] = target;
            accounttoken[8] = stoploss;

            return accounttoken;
        }

        static void Macdstrat(string[] args)
        {
            int trstat = 1;
            string[] account = Getaccounttoken();

            int lem = -1;

            while (trstat == 1)
            {

                //check if market is open
                Task<oandainfo> pricetask = Getprice();
                oandainfo priceinfo = pricetask.Result;

                if (priceinfo.tradeable.ToLower().Contains("true"))
                {

                    Task<posinfo> postask = Gettrades();
                    posinfo numpos = postask.Result;


                    //if currently holding a position                   
                    if (numpos.units != "0")
                    {

                        Task<oandainfo> pricetask2 = Getprice();
                        oandainfo priceinfo2 = pricetask2.Result;


                        if (numpos.side == "long")
                        {
                            double pal = Convert.ToDouble(numpos.price) - Convert.ToDouble(priceinfo2.bid);

                            if (Math.Round(pal, 5) >= 0.00050)
                            {
                                Task<tradeinfo> stoploss = oandabuysell((Convert.ToInt32(numpos.units) * -1), "EUR_USD", "FOK", "MARKET");
                                tradeinfo slr = stoploss.Result;
                                Console.WriteLine("Close long stop loss " + slr.price + " units:" + numpos.units + " P&L: " + Math.Round((Convert.ToDouble(slr.price) - Convert.ToDouble(numpos.price)), 5));

                            }


                            if (Math.Round(pal, 5) <= -0.00070)
                            {
                                Task<tradeinfo> stoploss = oandabuysell((Convert.ToInt32(numpos.units) * -1), "EUR_USD", "FOK", "MARKET");
                                tradeinfo slr = stoploss.Result;
                                Console.WriteLine("Close long target " + slr.price + " units:" + numpos.units + " P&L: " + Math.Round((Convert.ToDouble(slr.price) - Convert.ToDouble(numpos.price)), 5));

                            }

                        }

                        if (numpos.side == "short")
                        {
                            double pal = Convert.ToDouble(numpos.price) - Convert.ToDouble(priceinfo2.ask);


                            if (Math.Round(pal, 5) >= 0.00070)
                            {
                                Task<tradeinfo> stoploss = oandabuysell((Convert.ToInt32(numpos.units) * -1), "EUR_USD", "FOK", "MARKET");
                                tradeinfo slr = stoploss.Result;
                                Console.WriteLine("Close short target " + slr.price + " units:" + numpos.units + " P&L: " + Math.Round((Convert.ToDouble(numpos.price) - Convert.ToDouble(slr.price)), 5));
                            }


                            if (Math.Round(pal, 5) <= -0.00050)
                            {
                                Task<tradeinfo> stoploss = oandabuysell((Convert.ToInt32(numpos.units) * -1), "EUR_USD", "FOK", "MARKET");
                                tradeinfo slr = stoploss.Result;
                                Console.WriteLine("Close short stop loss " + slr.price + " units:" + numpos.units + " P&L: " + Math.Round((Convert.ToDouble(numpos.price) - Convert.ToDouble(slr.price)), 5));
                            }

                        }

                    }


                    //if no positions
                    if (numpos.units == "0")
                    {
                        DateTime dt = DateTime.Now;
                        int ms = dt.Millisecond;
                        int s = dt.Second;
                        int m = dt.Minute;


                        if (((s > 5)) && (m%5 == 0) && (m != lem))
                        {

                            string[] testarr = new string[2];
                            testarr[0] = "";
                            testarr[1] = "4000";

                            Task<info> datatask = getdataasync(testarr);
                            info data = datatask.Result;

                            //close data points
                            float[] test = new float[data.open.Length];
                            //date data points
                            string[] testd = new string[data.open.Length];

                            int xtmacd, ytmacd;
                            int xtsma, ytsma;
                            int xtrsi, ytrsi;

                            if (data.open.Length > 2)
                            {

                                for (int x = 0; x < data.open.Length; x++)
                                {
                                    test[x] = float.Parse(data.close[(data.close.Length - 1) - x]);
                                    testd[x] = data.time[(data.close.Length - 1) - x];
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

                                Console.WriteLine(test1macd[ytmacd - 1] + ";" + test2macdsig[ytmacd - 1] + ";" + test1macd[ytmacd - 2] + ";" + test2macdsig[ytmacd - 2] + ";" + dt.ToString("HH:mm:ss.fffff") + ";" + test[test.Length - 1]);


                                if ((test1macd[ytmacd - 1] > test2macdsig[ytmacd - 1]) && ((test1macd[ytmacd - 2] < test2macdsig[ytmacd - 2])))
                                {

                                    Task<tradeinfo> buy = oandabuysell(10000, "EUR_USD", "FOK", "MARKET");
                                    tradeinfo slr = buy.Result;
                                    Console.WriteLine("Open long " + slr.price + ";" + dt.ToString("HH:mm:ss.fffff"));

                                }

                                if ((test1macd[ytmacd - 1] < test2macdsig[ytmacd - 1]) && ((test1macd[ytmacd - 2] > test2macdsig[ytmacd - 2])))
                                {

                                    Task<tradeinfo> sell = oandabuysell(-10000, "EUR_USD", "FOK", "MARKET");
                                    tradeinfo slr = sell.Result;
                                    Console.WriteLine("Open short " + slr.price + ";" + dt.ToString("HH:mm:ss.fffff"));

                                }

                            }

                            lem = m;
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

                }

            }

            return tradeinfo;
        }


        static async Task<posinfo> Gettrades()
        {
            posinfo info = new posinfo();

            string[] account = Getaccounttoken();

            using (var httpClient = new HttpClient())
            {

                using (var request = new HttpRequestMessage(new HttpMethod("GET"), $"{account[3]}/accounts/{account[1]}/openPositions"))
                {
                    request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {account[0]}");

                    var response = await httpClient.SendAsync(request);
                    string responseBody = await response.Content.ReadAsStringAsync();
                    dynamic stuff4 = JObject.Parse(responseBody);


                    //no open positions
                    if (responseBody.Contains("\"positions\":[],\""))
                    {
                        //no open positions
                        //trade = 0;
                        info.symbol = "";
                        info.units = "0";
                        info.price = "0";
                        info.side = "";
                    }

                    else
                    {
                        var inst = stuff4.positions[0].instrument;
                        var lposi = stuff4.positions[0].@long.units;
                        var lprice = stuff4.positions[0].@long.averagePrice;
                        var sposi = stuff4.positions[0].@short.units;
                        var sprice = stuff4.positions[0].@short.averagePrice;


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

        static async Task<int> DownloadData(int datap)
        {
            string[] account = Getaccounttoken();
            using (var httpClient = new HttpClient())
            {
                using (var request = new HttpRequestMessage(new HttpMethod("GET"), $"{account[3]}/instruments/EUR_USD/candles?count=4001&price=M&granularity=M5"))  //5000 max
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

                    var test = stuff4.candles;
                    var test2 = stuff4.candles[stuff4.candles.Count - 1].mid.c;
                    var test4 = stuff4.candles[stuff4.candles.Count - 1].complete;
                   

                    string c = stuff4.candles[stuff4.candles.Count - 1].complete;

                    //store 4000 values, last candle element is the most recent price
                    for (int x = 1; x < datap + 1; x++)
                    {
                        //most recent data is in spot 0
                        //use last 4000 items including most recent candle
                        if (c.ToLower().Equals("true"))
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

        public static async Task<int> canceltradesoanda(string num)
        {
            string[] account = Getaccounttoken();
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

        public static async Task<int> gettradesoanda()
        {
            string[] account = Getaccounttoken();
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


        public static async Task<tradeinfo> oandabuysell(int units, string inst, string tif, string ordtype)
        {
            string[] account = Getaccounttoken();
            tradeinfo trade = new tradeinfo();

            string body = $"{{ \"order\": {{\"units\": \"{units}\",\"instrument\": \"{inst}\",\"timeInForce\": \"{tif}\",\"type\": \"{ordtype}\",\"positionFill\": \"DEFAULT\"}}}}";
            
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
            

                    trade.symbol = stuff4.orderFillTransaction.instrument;
                    trade.units = stuff4.orderFillTransaction.units;
                    trade.price = stuff4.orderFillTransaction.price;

                }
            }

            return trade;
        }

        public static async Task<info> getdataasync(string[] args)
        {
          
            info data = new info();

            data.open = new string[2];
            data.high = new string[2];
            data.low = new string[2];
            data.close = new string[2];
            data.time = new string[2];


            Task<int> task = DownloadData(Convert.ToInt32(args[1]));
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
       
    }
}
