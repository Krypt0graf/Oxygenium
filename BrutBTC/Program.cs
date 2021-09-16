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
using System.Security.Cryptography;

namespace BrutBTC
{
    class Program
    {
        #region prop
        public static double[] speeds; // Массив скоростей работы
        public static Byte[] arr = new Byte[32];//Рандом массив
        public static RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
        public static Dictionary<char, Dictionary<char, Dictionary<char, Dictionary<char, List<string>>>>> wallets 
            = new Dictionary<char, Dictionary<char, Dictionary<char, Dictionary<char, List<string>>>>>(); // "Горячая" база кошельков, четырехуровневая
        #endregion
        static void Main(string[] args)
        {
            Console.CursorVisible = false;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Title = "Oxygenium\\BrutBTC";


            string file = string.Empty;
            if (args.Length > 0)
            {
                file = args[0]; // Путь к фалу можно задать первым аргументом
            }
            else
            {
                Console.Write("  Укажите относительный путь к файлу адресов: ");
                file = Console.ReadLine(); // Иначе указываем
            }
            
            Console.WriteLine("  Загрузка базы кошельков");
            var timeload = loadbase(file); // Грузим базу
            Console.Clear();

            Console.WriteLine($"  База загружена за {timeload} сек");
            var count = Environment.ProcessorCount; // Узнаем максимальное количество потоков
            Console.WriteLine();
            Console.WriteLine("|   Поток   |   Скорость   |");
            Console.WriteLine("----------------------------");
            speeds = new double[count];

            for (int i = 0; i < count; i++)
            {
                int j = i;
                Thread thr = new Thread(() => brut(j)); // Создаем поток, задаем ему индекс массива, куда он будет писать свою скорость работы
                thr.Priority = ThreadPriority.Highest; // Выставялем максимальный приоритет
                thr.Start();
                //Task.Run(() => brut(j)); // Можно стартануть как таск
                Console.WriteLine($"| Thread #{i}".PadRight(12) + "|          w/s |");
            }
            Console.WriteLine("----------------------------");
            while (true)
            {
                int end = 0;
                for (int i = 0; i < count; i++)
                {
                    Console.SetCursorPosition(13, 4 + i);
                    Console.WriteLine($" { speeds[i] }".PadRight(9)); // Обновляем инфу каждые 0,5 сек
                    end = i;
                }
                Console.SetCursorPosition(0, end + 6);
                Console.WriteLine($"  Total {speeds.Sum()} w/s"); // Суммарная скорость
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

                    wallets[str[1]][str[2]][str[3]][str[4]].Add(str); // Указателями на вложеные словари являются первые 4 буквы ('1' пропускаем) адреса соответственно
                }
                timer.Stop();
                return Math.Round((double)timer.ElapsedMilliseconds / 1000, 2); // Подсчитали время загрузки базы
            }
        }
        public static void brut(int taskIndex)
        {
            
            int i = 0;
            var t = Stopwatch.StartNew();
            while (true)
            {
                rng.GetBytes(arr); // Заполняем рандомно массив
                var secret = new Key(arr, -1, false).GetBitcoinSecret(Network.Main); // Получаем из него секрет в WIF формате
                var address = secret.GetAddress(ScriptPubKeyType.Legacy).ToString(); // из секрета получаем адресс
                try
                {
                    if (wallets[address[1]][address[2]][address[3]][address[4]].Any(w => w == address)) // Проверяем
                    {
                        using (StreamWriter w = new StreamWriter($"{address}.txt", true)) // Записываем в файл (имя файла - адрес, содержимое - секрет)
                        {
                            w.WriteLine(secret);
                        }
                    }
                }
                catch { } // Если на каком то уровне отсутствует ключ для словаря
                if (i++ == 200) // Ждем проверки нескольких адресов, для усреднения результата скорости
                {
                    i = 0;
                    t.Stop();
                    var f = Math.Round(1000 / ((double)t.ElapsedMilliseconds / 200), 2);
                    speeds[taskIndex] = f; // Записываем скорость в свою ячейку
                    t = Stopwatch.StartNew();
                };
            }
        }
        /// <summary>
        /// Вывод массива байтов в строку
        /// </summary>
        /// <param name="obj">Массив байтов</param>
        /// <param name="reverse">true для обратного порядка строки</param>
        /// <returns></returns>
        static public string ToHex(byte[] obj, bool reverse = false)
        {
            if (reverse)
                obj = obj.Reverse().ToArray();
            string g = "";
            foreach (var item in obj)
            {
                g += item.ToString().PadRight(4, ' ');
            }
            return g;
        }
        /// <summary>
        /// Переводит hex-строку в массив байтов. Строка должна иметь четное количество символов и содержать только символы цифр (0-9) и буквы (A-F).
        /// </summary>
        /// <param name="data">hex-строка</param>
        /// <param name="reverse">true - инвертировать результат (обратный порядок байт в массиве)</param>
        /// <returns></returns>
        static public byte[] ToBytes(string data, bool reverse = false)
        {
            if (data.Length % 2 != 0)
                return null;
            byte[] arr = new byte[data.Length / 2];
            int j = 0;
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = Convert.ToByte(data[j].ToString() + data[j + 1].ToString(), 16);
                j += 2;
            }
            if (reverse)
                arr = arr.Reverse().ToArray();
            return arr;
        }
        /// <summary>
        /// Метод получения закрытого ключа из массива исходных байтов
        /// </summary>
        /// <param name="bytes">массив байтов, длинной 32</param>
        /// <returns></returns>
        static public string getKey(byte[] bytes)
        {
            var full = new byte[bytes.Length + 1];
            full[0] = 0x80;                           // Префикс основной (main) сети блокчейна
            /*for (int i = 0; i < bytes.Length; i++)
            {
                full[i + 1] = Convert.ToByte(bytes[i]);
            }*/
            for (int i = full.Length - 1; i > 1; i--)
            {
                full[i] = Convert.ToByte(bytes[i - 1]);
            }
            return Encoders.Base58Check.EncodeData(full); 
        }
        public static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }
    }
}
