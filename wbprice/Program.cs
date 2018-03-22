using AngleSharp.Parser.Html;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Timers;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace wbprice
{
    class Program
    {
        private static decimal price = 0;
        static void Main(string[] args)
        {
            var url = $"";
            //Create a (re-usable) parser front-end
            var parser = new HtmlParser();

            // Telegram bot settings
            var bot = new TelegramBotClient("");
            List<int> recipients = new List<int>() {  };

            Timer timer = new Timer(10 * 60 * 1000);
            timer.Elapsed += async (sender, e) => await HandleTimer(bot, recipients, parser, url, timer);            
            timer.AutoReset = false;
            timer.Start();
            Console.WriteLine("Press any key to exit... ");            
            Console.ReadKey();                 
        }

        private static async Task HandleTimer(TelegramBotClient bot, List<int> recipients, HtmlParser parser, string url, Timer timer)
        {
            try
            {
                //foreach (int recipient in recipients)
                //    await bot.SendTextMessageAsync(new ChatId(recipient), string.Format($"testing..."));

                Console.Write(">");

                //var me = await bot.GetMeAsync();
                //Console.WriteLine($"My name is {me.FirstName}");  

                //decimal price = 0;
                using (WebClient client = new WebClient()) // WebClient class inherits IDisposable
                {
                    // get the file content without saving it:               
                    var data = await client.DownloadDataTaskAsync(new Uri(url));
                    string html = System.Text.Encoding.UTF8.GetString(data);
                    Console.Write(".");

                    //Parse source to document
                    var document = parser.Parse(html);
                    Console.Write(".");

                    //Do something with document like the following
                    var priceElement = document.QuerySelectorAll("meta[itemprop=\"price\"]").FirstOrDefault();
                    Console.Write(".");

                    var culture = System.Globalization.CultureInfo.GetCultureInfo("ru-Ru");
                    decimal newPrice = Convert.ToDecimal(priceElement.Attributes["content"].Value, culture);
                    Console.Write(">{0}", newPrice);

                    if (price != 0 && newPrice != price)
                    {
                        Console.Write(" !!! {0}", newPrice > price ? "UP" : "DOWN");
                        foreach (int recipient in recipients)
                            await bot.SendTextMessageAsync(new ChatId(recipient), string.Format($"{price} -> {newPrice}"));
                    }
                    price = newPrice;

                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                timer.Start();
            }
        }
    }
}
