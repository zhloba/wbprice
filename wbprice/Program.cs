﻿using AngleSharp.Parser.Html;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace wbprice
{
    class Program
    {
        //private static decimal price = 0;
        static void Main(string[] args)
        {

            var items = new List<WbItem>()
            {
                new WbItem { Url =  $"", Id = 1, CharacteristicId = 1, Name = "1" }
            };
            //Create a (re-usable) parser front-end
            var parser = new HtmlParser();

            // Telegram bot settings
            var bot = new TelegramBotClient("");
            List<int> recipients = new List<int>();

            Timer timer = new Timer(15 * 60 * 1000);
            timer.Elapsed += async (sender, e) => await HandleTimer1(bot, recipients, parser, items, timer);            
            timer.AutoReset = false;
            timer.Start();
            Console.WriteLine("Press any key to exit... ");            
            Console.ReadKey();                 
        }

        private static async Task HandleTimer(TelegramBotClient bot, List<int> recipients, HtmlParser parser, List<WbItem> items, Timer timer)
        {
            try
            {
                foreach(var item in items)
                {
                    Console.Write(">{0}", item.Url);
                    //decimal price = 0;
                    using (WebClient client = new WebClient()) // WebClient class inherits IDisposable
                    {                        
                        // get the file content without saving it:               
                        var data = await client.DownloadDataTaskAsync(new Uri(item.Url));
                        string html = System.Text.Encoding.UTF8.GetString(data);
                        Console.Write(".");

                        //Parse source to document
                        var document = parser.Parse(html);
                        Console.Write(".");

                        //Do something with document like the following
                        var priceElement = document.QuerySelectorAll("meta[itemprop=\"price\"]").FirstOrDefault();
                        var nameElement = document.QuerySelectorAll("h1[itemprop=\"name\"]").FirstOrDefault();
                        Console.Write(".");

                        var culture = System.Globalization.CultureInfo.GetCultureInfo("be-BY");
                        decimal newPrice = 0;
                        if (!string.IsNullOrEmpty(priceElement?.Attributes["content"]?.Value))
                            newPrice = Convert.ToDecimal(priceElement.Attributes["content"].Value, culture);

                        item.Name = nameElement.TextContent.Trim('\n').Trim();
                        Console.Write(">{0}", newPrice);

                        if (item.Price != -1 && newPrice != item.Price)
                        {
                            Console.Write(" !!! {0}", newPrice > item.Price ? "UP" : "DOWN");

                            int iconHex = newPrice == 0 ? 0x1f440 : item.Price == -1 ? 0x1f449 : newPrice > item.Price ? 0x1f44e : 0x1f44d;
                            string priceText = newPrice == 0 ? "Out of stock" : string.Format("{0} -> {1}", item.Price <= 0 ? "..." : item.Price.ToString("c2", culture), newPrice.ToString("c2", culture));
                            string text = string.Format("{0}\n{1} {2}\n{3}", item.Name, char.ConvertFromUtf32(iconHex), priceText, item.Url);
                            foreach (int recipient in recipients)
                                await bot.SendTextMessageAsync(new ChatId(recipient), text);
                        }
                        item.Price = newPrice;                        
                        Console.WriteLine();
                    }
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

        private static async Task HandleTimer1(TelegramBotClient bot, List<int> recipients, HtmlParser parser, List<WbItem> items, Timer timer)
        {
            try
            {
                foreach (var item in items)
                {
                    Console.Write(">{0}", item.Url);

                    using (HttpClient client = new HttpClient())
                    {                       
                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));               
                        client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
                        string jsonStr = JsonConvert.SerializeObject(item);
                        var content = new StringContent(jsonStr, Encoding.UTF8, "application/json");
                        var response = await client.PostAsync("https://www.example.com", content);                       
                        var responseString = await response.Content.ReadAsStringAsync();
                        dynamic info = JsonConvert.DeserializeObject(responseString);
                        Console.Write(">...");

                        var culture = System.Globalization.CultureInfo.GetCultureInfo("be-BY");
                        decimal newPrice = 0;
                        if (info != null && info.Value != null && info.Value.promoInfo != null && info.Value.promoInfo.PriceWithCouponAndDiscount != 0)
                            newPrice = Convert.ToDecimal(info.Value.promoInfo.PriceWithCouponAndDiscount / 10000m);
                        
                        Console.Write(">{0}", newPrice);

                        if (item.Price != -1 && newPrice != item.Price)
                        {
                            Console.Write(" !!! {0}", newPrice > item.Price ? "UP" : "DOWN");

                            int iconHex = newPrice == 0 ? 0x1f440 : item.Price == -1 ? 0x1f449 : newPrice > item.Price ? 0x1f44e : 0x1f44d;
                            string priceDelta = item.Price <= 0 ? "" : string.Format("({0})", (newPrice - item.Price).ToString("+c2", culture));
                            string priceText = newPrice == 0 ? "Out of stock" : string.Format("{0} -> {1} {2}", 
                                item.Price <= 0 ? "..." : item.Price.ToString("c2", culture), 
                                newPrice.ToString("c2", culture), 
                                priceDelta);
                            string text = string.Format("{0}\n{1} {2}\n{3}", item.Name, char.ConvertFromUtf32(iconHex), priceText, item.Url);
                            foreach (int recipient in recipients)
                                await bot.SendTextMessageAsync(new ChatId(recipient), text);
                        }
                        item.Price = newPrice;
                        Console.WriteLine();
                    }                  
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

    public class WbItem
    {
        [JsonIgnore]
        public string Url { get; set; }
        [JsonIgnore]
        public decimal Price { get; set; } = -1;
        [JsonIgnore]
        public string Name { get; set; }
        [JsonProperty("cod1s")]
        public int Id { get; set; }
        [JsonProperty("characteristicId")]
        public int CharacteristicId { get; set; }
    }
}
