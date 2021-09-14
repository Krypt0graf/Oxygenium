using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using NBitcoin;
using NBitcoin.DataEncoders;
using System.Diagnostics;

namespace BrutBTC
{
    class Program
    {
        #region prop
        public static double[] speeds;
        public static Byte[] arr = new Byte[32];
        public static Dictionary<char, Dictionary<char, Dictionary<char, Dictionary<char, List<string>>>>> wallets 
            = new Dictionary<char, Dictionary<char, Dictionary<char, Dictionary<char, List<string>>>>>();
        #endregion
        static void Main(string[] args)
        {
            
            Console.CursorVisible = false;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Загрузка базы кошельков");
            var timeload = loadbase("../list.txt");
            Console.Clear();
            Console.WriteLine($"База загружена за {timeload} сек");
            Console.Write("Задайте количество потоков: ");
            var count = Convert.ToInt32(Console.ReadLine());
            Console.WriteLine();

            speeds = new double[count];

            for (int i = 0; i < count; i++)
            {
                int j = i;
                Thread thr = new Thread(() => brut(j));
                thr.Priority = ThreadPriority.Highest;
                thr.Start();
                //Task.Run(() => brut(j));
                Console.WriteLine($"Thread #{i}: 0 wallets/seconds");
            }

            while (true)
            {
                int end = 0;
                for (int i = 0; i < count; i++)
                {
                    Console.SetCursorPosition(11, 3 + i);
                    Console.WriteLine($"{ speeds[i] } wallets/seconds");
                    end = i;
                }
                Console.SetCursorPosition(1, end + 5);
                Console.WriteLine($"Total {speeds.Sum()}");
                Thread.Sleep(500);
            }
        }
        public static double loadbase(string path)
        {
            using (StreamReader r = new StreamReader(path))
            {
                var timer = Stopwatch.StartNew();
                while (!r.EndOfStream)
                {
                    var str = r.ReadLine();
                    if (!wallets.ContainsKey(str[1]))
                        wallets.Add(str[1], new Dictionary<char, Dictionary<char, Dictionary<char, List<string>>>>());

                    if (!wallets[str[1]].ContainsKey(str[2]))
                        wallets[str[1]].Add(str[2], new Dictionary<char, Dictionary<char, List<string>>>());

                    if (!wallets[str[1]][str[2]].ContainsKey(str[3]))
                        wallets[str[1]][str[2]].Add(str[3], new Dictionary<char, List<string>>());

                    if (!wallets[str[1]][str[2]][str[3]].ContainsKey(str[4]))
                        wallets[str[1]][str[2]][str[3]].Add(str[4], new List<string>());

                    wallets[str[1]][str[2]][str[3]][str[4]].Add(str);
                }
                timer.Stop();
                return Math.Round((double)timer.ElapsedMilliseconds / 1000, 2);
            }
        }
        public static void brut(int taskIndex)
        {
            Random rnd = new Random();
            int i = 0;
            var t = Stopwatch.StartNew();
            while (true)
            {
                rnd.NextBytes(arr);
                var secret = getKey(arr);
                var address = new BitcoinSecret(secret, Network.Main).GetAddress(ScriptPubKeyType.Legacy).ToString();
                try
                {
                    if (wallets[address[1]][address[2]][address[3]][address[4]].Any(w => w == address))
                    {
                        using (StreamWriter w = new StreamWriter($"{address}.txt", true))
                        {
                            w.WriteLine(secret);
                        }
                    }
                }
                catch { }
                if (i++ == 200)
                {
                    i = 0;
                    t.Stop();
                    var f = Math.Round(1000 / ((double)t.ElapsedMilliseconds / 200), 2);
                    speeds[taskIndex] = f;
                    t = Stopwatch.StartNew();
                };
            }
        }
        static public string getKey(byte[] bytes)
        {
            var full = new byte[bytes.Length + 1];
            full[0] = 0x80;                           // Префикс основной (main) сети блокчейна
            for (int i = 0; i < bytes.Length; i++)
            {
                full[i + 1] = Convert.ToByte(bytes[i]);
            }
            return Encoders.Base58Check.EncodeData(full); 
        }
    }
}
