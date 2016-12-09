using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace CacheLoader
{
    class Program
    {
        private static Dictionary<string, List<Tuple<int, int>>> load_config(string config_path)
        {
            string config_content = File.ReadAllText(config_path);
            string[] config_lines = config_content.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            Dictionary<string, List<Tuple<int, int>>> chunks = new Dictionary<string, List<Tuple<int, int>>>();

            for (int i = 1; i < config_lines.Length; ++i)
            {
                string line = config_lines[i];
                string[] parts = line.Split(new char[] { ',' });

                string file = parts[0];
                string offset = parts[1];
                string size = parts[2];

                int o = int.Parse(offset);
                int s = int.Parse(size);

                if (!chunks.ContainsKey(file))
                    chunks.Add(file, new List<Tuple<int, int>>());

                chunks[file].Add(new Tuple<int, int>(o, o + s));
            }

            foreach (string file in chunks.Keys)
            {
                List<Tuple<int, int>> fc = chunks[file];

                for (int j = 0; j < fc.Count; ++j)
                {
                    Tuple<int, int> a = fc[j];

                    for (int k = j + 1; k < fc.Count; ++k)
                    {
                        Tuple<int, int> b = fc[k];

                        int i1 = a.Item1;
                        int i2 = a.Item2;

                        if (b.Item2 > a.Item2 && b.Item1 <= a.Item2)
                        {
                            i2 = b.Item2;
                        }

                        if (b.Item1 < a.Item1 && b.Item2 >= a.Item1)
                        {
                            i1 = b.Item1;
                        }

                        if (a.Item1 != i1 || a.Item2 != i2)
                        {
                            fc[j] = new Tuple<int, int>(i1, i2);
                            fc.RemoveAt(k);
                            k -= 1;
                        }
                    }
                }

                chunks[file].Sort((a, b) => b.Item1 - a.Item1);
            }

            return chunks;
        }

        public static void Main(string[] args)
        {
            Console.Write("WoW install directory: ");
            string wow_path = Console.ReadLine().Trim();
            string data_path = wow_path + @"\Data\data\";

            string config_path = @"chunks.csv";
            int block_size = 512;

            Dictionary<string, List<Tuple<int, int>>> chuncks = load_config(config_path);

            Dictionary<string, FileStream> streams = new Dictionary<string, FileStream>();

            byte[] b = new byte[block_size];

            for (bool run = true; run;)
            {
                int total_read_size = 0;
                foreach (string file in chuncks.Keys)
                {
                    string file_path = data_path + file;


                    if (!streams.ContainsKey(file))
                        streams.Add(file, new FileStream(file_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));

                    FileStream fs = streams[file];

                    FileInfo fi = new FileInfo(file_path);

                    int file_read_size = 0;
                    foreach (Tuple<int, int> t in chuncks[file])
                    {
                        int offset = t.Item2;
                        while (offset >= t.Item1 && offset > 0)
                        {
                            if (block_size > offset)
                                offset = 0;
                            else
                                offset -= block_size;

                            fs.Seek(offset, SeekOrigin.Begin);
                            file_read_size += fs.Read(b, 0, block_size);
                        }
                    }

                    Console.WriteLine(file + " cached for " + file_read_size + " bytes");
                    total_read_size += file_read_size;
                }

                Console.WriteLine("caching done for " + total_read_size + " bytes");
                Console.WriteLine("press 'r' to refresh cache or 'q' to quit");

                for (bool valid_key = false; !valid_key;)
                {
                    ConsoleKeyInfo k = Console.ReadKey();
                    Console.WriteLine();

                    if (k.KeyChar == 'q')
                    {
                        run = false;
                        valid_key = true;
                    }
                    else if (k.KeyChar == 'r')
                    {
                        valid_key = true;
                    }
                }
            }

            foreach (FileStream fs in streams.Values)
                fs.Close();
        }
    }
}
